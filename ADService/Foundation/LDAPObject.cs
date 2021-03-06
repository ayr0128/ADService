using ADService.Details;
using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace ADService.Foundation
{
    /// <summary>
    /// 最基底的物件類型: 採用化學中的受質作為名稱代表其特性
    /// </summary>
    public class LDAPObject : LDAPProperties
    {
        /// <summary>
        /// 搜尋物件時使用的特性鍵值
        /// </summary>
        internal static string[] PropertiesToLoad => new string[] {
            Properties.C_DISTINGUISHEDNAME, // 物件區分名稱
        };

        #region 創建物件以及創建描述
        /// <summary>
        /// ADService 物件的工廠模式
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="dispatcher">入口物件創建器</param>
        /// <returns>使用 object 封裝對外提供的物件</returns>
        /// <exception cref="LDAPExceptions">物件未實作工廠模式或實作過程中出現錯誤時丟出例外</exception>
        internal static LDAPObject ToObject(in DirectoryEntry entry, in LDAPConfigurationDispatcher dispatcher)
        {
            // 取得物件驅動類型名稱
            string[] driveClassNames = LDAPConfiguration.ParseMutipleValue<string>(Properties.C_OBJECTCLASS, entry.Properties);
            // 從類型分配氣中找到物件類別, 此驅動物件類型會經過排序, 最後一個就是目前物件的類別
            UnitSchemaClass[] unitSchemaClasses = dispatcher.GetDriveClasses(driveClassNames);
            // 使用物件類型製作對應的物件
            UnitSchemaClass unitSchemaClass = unitSchemaClasses.Last();
            // 根據主要驅動物件類型名稱逕行動作
            switch (unitSchemaClass.Name)
            {
                case LDAPCategory.CLASS_CONTAINER: // 容器
                    {
                        // 對外提供 '容器'
                        return new LDAPContainer(entry, dispatcher);
                    }
                case LDAPCategory.CLASS_DOMAINDNS: // 網域
                    {
                        // 對外提供 '網域' 結構
                        return new LDAPDomainDNS(entry, dispatcher);
                    }
                case LDAPCategory.CLASS_ORGANIZATIONUNIT: // 組織單位
                    {
                        // 對外提供 '組織單位' 結構
                        return new LDAPOrganizationUnit(entry, dispatcher);
                    }
                case LDAPCategory.CLASS_FOREIGNSECURITYPRINCIPALS: // 內部安全性群組
                case LDAPCategory.CLASS_GROUP:                     // 群組
                    {
                        // 對外提供 '群組' 結構
                        return new LDAPGroup(entry, dispatcher);
                    }
                case LDAPCategory.CLASS_PERSON: // 成員
                    {
                        // 對外提供 '成員' 結構
                        return new LDAPPerson(entry, dispatcher);
                    }
                default:
                    {
                        // 對外丟出例外: 未實作
                        throw new LDAPExceptions($"目標物件類型:{unitSchemaClass.Name} 尚未實作工廠模式, 無法轉換", ErrorCodes.LOGIC_ERROR);
                    }
            }
        }

        /// <summary>
        /// 內部使用: 取得主要隸屬群組
        /// </summary>
        /// <param name="dispatcher">入口物件創建器</param>
        /// <param name="primaryGroupToken">主要隸屬群組的 Token</param>
        internal static Dictionary<string, LDAPRelationship> ToRelationshipByToken(in LDAPConfigurationDispatcher dispatcher, in int primaryGroupToken)
        {
            // 使用根目錄
            using (DirectoryEntry root = dispatcher.DomainRoot())
            {
                // 加密避免 LDAP 注入式攻擊: 透過主要隸屬群組關聯的物件必定圍成員
                string encoderFiliter = $"(&{LDAPConfiguration.GetORFiliter(Properties.C_OBJECTCLASS, LDAPCategory.CLASS_PERSON)}({Properties.C_PRIMARYGROUPID}={primaryGroupToken}))";

                // 搜尋主要隸屬群組關聯的物件
                using (DirectorySearcher searcher = new DirectorySearcher(root, encoderFiliter, PropertiesToLoad))
                {
                    // 取得搜尋結果
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 不存在搜尋結果
                        if (all == null)
                        {
                            // 提供空的字典
                            return new Dictionary<string, LDAPRelationship>(0);
                        }

                        // 使用找尋的物件長度作為容器大小
                        Dictionary<string, LDAPRelationship> dictionaryDNWithObject = new Dictionary<string, LDAPRelationship>(all.Count);
                        // 遍歷結果轉換
                        foreach (SearchResult one in all)
                        {
                            // 轉換為入口物件
                            using (DirectoryEntry entry = one.GetDirectoryEntry())
                            {
                                // 取得物件驅動類型名稱
                                string[] driveClassNames = LDAPConfiguration.ParseMutipleValue<string>(Properties.C_OBJECTCLASS, entry.Properties);
                                // 從類型分配氣中找到物件類別, 此驅動物件類型會經過排序, 最後一個就是目前物件的類別
                                UnitSchemaClass[] unitSchemaClasses = dispatcher.GetDriveClasses(driveClassNames);
                                // 使用物件類型製作對應的物件
                                UnitSchemaClass unitSchemaClass = unitSchemaClasses.Last();

                                // 轉換成基礎物件
                                LDAPRelationship relationship = new LDAPRelationship(entry, true, unitSchemaClass.Name);
                                // 推入字典
                                dictionaryDNWithObject.Add(relationship.DistinguishedName, relationship);
                            }
                        }
                        // 對外提供搜尋結果
                        return dictionaryDNWithObject;
                    }
                }
            }
        }

        /// <summary>
        /// 內部使用: 取得主要隸屬群組
        /// </summary>
        /// <param name="dispatcher">入口物件創建器</param>
        /// <param name="primaryGroupSID">主要隸屬群組的 SID</param>
        internal static LDAPRelationship ToRelationshipBySID(in LDAPConfigurationDispatcher dispatcher, in string primaryGroupSID)
        {
            // 使用 SID 取得指定物件
            using (DirectoryEntry entry = dispatcher.BySID(primaryGroupSID))
            {
                // 取得物件驅動類型名稱
                string[] driveClassNames = LDAPConfiguration.ParseMutipleValue<string>(Properties.C_OBJECTCLASS, entry.Properties);
                // 從類型分配氣中找到物件類別, 此驅動物件類型會經過排序, 最後一個就是目前物件的類別
                UnitSchemaClass[] unitSchemaClasses = dispatcher.GetDriveClasses(driveClassNames);
                // 使用物件類型製作對應的物件
                UnitSchemaClass unitSchemaClass = unitSchemaClasses.Last();

                // 主要隸屬的物件一定是群組
                return new LDAPRelationship(entry, true, unitSchemaClass.Name);
            }
        }

        /// <summary>
        /// 內部使用: 取得指定區分名稱
        /// </summary>
        /// <param name="dispatcher">入口物件創建器</param>
        /// <param name="distinguishedNames">所有欲找尋的區分名稱 (memberOf)</param>
        /// <returns>指定區分名稱的物件, 結構如右: Dictionary '區分名稱, 物件' </returns>
        internal static Dictionary<string, LDAPRelationship> ToRelationshipByDNs(in LDAPConfigurationDispatcher dispatcher, params string[] distinguishedNames)
        {
            // 沒有隸屬群組時
            if (distinguishedNames == null || distinguishedNames.Length == 0)
            {
                // 對外提供空陣列
                return new Dictionary<string, LDAPRelationship>(0);
            }

            // 找尋指定區分名稱時需要從根目錄開始找尋
            using (DirectoryEntry root = dispatcher.DomainRoot())
            {
                // [TODO] 應使用加密字串避免注入式攻擊
                string encoderFiliter = LDAPConfiguration.GetORFiliter(Properties.C_DISTINGUISHEDNAME, distinguishedNames);
                // 找尋指定目標
                using (DirectorySearcher searcher = new DirectorySearcher(root, encoderFiliter, PropertiesToLoad, SearchScope.Subtree))
                {
                    // 將指定目標過濾出來
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 取代用的新字典
                        Dictionary<string, LDAPRelationship> dictionaryDNWithGroup = new Dictionary<string, LDAPRelationship>(all.Count);
                        // 遍歷取得的結果
                        foreach (SearchResult one in all)
                        {
                            // 將取得物件轉換為入口物件
                            using (DirectoryEntry entry = one.GetDirectoryEntry())
                            {
                                // 區分名稱
                                string distinguishedName = LDAPConfiguration.ParseSingleValue<string>(Properties.C_DISTINGUISHEDNAME, entry.Properties);
                                // 取得物件驅動類型名稱
                                string[] driveClassNames = LDAPConfiguration.ParseMutipleValue<string>(Properties.C_OBJECTCLASS, entry.Properties);
                                // 從類型分配氣中找到物件類別, 此驅動物件類型會經過排序, 最後一個就是目前物件的類別
                                UnitSchemaClass[] unitSchemaClasses = dispatcher.GetDriveClasses(driveClassNames);
                                // 使用物件類型製作對應的物件
                                UnitSchemaClass unitSchemaClass = unitSchemaClasses.Last();

                                // 推入字典
                                dictionaryDNWithGroup.Add(distinguishedName, new LDAPRelationship(entry, false, unitSchemaClass.Name));
                            }
                        }
                        // 返還
                        return dictionaryDNWithGroup;
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// 取得目前物件的父層組織單位
        /// </summary>
        /// <param name="OrganizationUnit">父層組織單位</param>
        /// <returns>是否有父層</returns>
        public bool GetOrganizationUnit(out string OrganizationUnit)
        {
            // 預設回傳空字串: 代表本身是根目錄
            OrganizationUnit = string.Empty;
            // 是否為網域跟目錄
            if (DriveClassName == LDAPCategory.CLASS_DOMAINDNS)
            {
                // 網域跟目錄不持有父曾
                return false;
            }

            // 找到名稱的位置: 必定能找到
            int index = DistinguishedName.IndexOf(Name);
            // 切割字串取得目標所在的組織單位
            OrganizationUnit = DistinguishedName.Substring(index + Name.Length + 1);
            // 對外返回成功
            return true;
        }

        /// <summary>
        /// 此物件是否可被移動
        /// </summary>
        public bool IsEnbleMove => (SystemFlag & SystemFlags.MOVE_DISABLE) == SystemFlags.NONE;
        /// <summary>
        /// 是否可重新命盟
        /// </summary>
        public bool IsEnableReName => (SystemFlag & SystemFlags.RENAME_DISABLE) == SystemFlags.NONE;
        /// <summary>
        /// 是否可移除
        /// </summary>
        public bool IsEnableDelete => (SystemFlag & SystemFlags.DELETE_DISABLE) == SystemFlags.NONE;

        /// <summary>
        /// 此物件的名稱
        /// </summary>
        internal SystemFlags SystemFlag
        {
            get
            {
                // 取得內部儲存的類型
                int storedSystemFlags = GetPropertySingle<int>(Properties.C_SYSTEMFLAGS);
                // 強制轉型並取得系統旗標
                return (SystemFlags)Enum.ToObject(typeof(SystemFlags), storedSystemFlags);
            }
        }

        /// <summary>
        /// 此物件的名稱
        /// </summary>
        public string Name => GetPropertySingle<string>(Properties.P_NAME);
        /// <summary>
        /// 此物件的展示名稱
        /// </summary>
        public string DisplayName => GetPropertySingle<string>(Properties.P_DISPLAYNAME);
        /// <summary>
        /// 此物件的區分名稱
        /// </summary>
        public string DistinguishedName => GetPropertySingle<string>(Properties.C_DISTINGUISHEDNAME);
        /// <summary>
        /// 此物件的全域唯一標識符
        /// </summary>
        public string GUID => GetPropertyGUID(Properties.C_OBJECTGUID);
        /// <summary>
        /// 容器類型
        /// </summary>
        [Obsolete("即將棄用, 請改用新屬性: DriveClassName 判斷物件類型")]
        public CategoryTypes Type => LDAPCategory.GetCategoryTypes(DriveClassName);
        /// <summary>
        /// 驅動類型名稱
        /// </summary>
        public string DriveClassName => driveUnitSchemaClasses.Last().Name;

        /// <summary>
        /// 目標物建物件類型: 驅動
        /// </summary>
        internal UnitSchemaClass[] driveUnitSchemaClasses;
        /// <summary>
        /// 目標物建物件類型: 輔助
        /// </summary>
        internal UnitSchemaClass[] auxiliaryUnitSchemaClasses;
        /// <summary>
        /// 取得可支援的屬性名稱
        /// </summary>
        internal string[] AllowedAttributeNames
        {
            get
            {
                // 宣告一個新的陣列來存放輔助類別與需求類別
                List<UnitSchemaClass> unitSchemaClasses = new List<UnitSchemaClass>(driveUnitSchemaClasses.Length + auxiliaryUnitSchemaClasses.Length);
                // 增加原始類別
                unitSchemaClasses.AddRange(driveUnitSchemaClasses);
                // 增加輔助類別
                unitSchemaClasses.AddRange(auxiliaryUnitSchemaClasses);
                // 取得所有允許的屬性
                return UnitSchemaClass.UniqueAttributeNames(unitSchemaClasses);
            }
        }

        /// <summary>
        /// 使用鍵值參數初始化
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="dispatcher">入口物件創建器</param>
        /// <exception cref="LDAPExceptions">解析鍵值不符合規則時對外丟出</exception>
        internal LDAPObject(in DirectoryEntry entry, in LDAPConfigurationDispatcher dispatcher) : base(dispatcher, entry)
        {
            // 取得物件持有類別
            string[] classNames = GetPropertyMultiple<string>(Properties.C_OBJECTCLASS);
            // 透過物件持有類別取得所有可用屬性以及所有可用子類別
            driveUnitSchemaClasses = dispatcher.GetDriveClasses(classNames);
            // 取得輔助類別
            auxiliaryUnitSchemaClasses = dispatcher.GetAuxiliaryClasses(driveUnitSchemaClasses);
        }

        /// <summary>
        /// 從提供的基礎物件中將特性鍵值轉換給呼叫者
        /// </summary>
        /// <param name="newObject">指定的基礎物件</param>
        internal virtual LDAPObject SwapFrom(in LDAPObject newObject)
        {
            /* 下述條件任一成立時不進行替換
                 - 指定基礎物件不存在
                 - 物件 GUID 不同
           */
            if (newObject == null || GUID != newObject.GUID)
            {
                // 不執行任何動作
                return newObject;
            }

            // 交換解析完成的目前持有屬性
            dictionaryNameWithPropertyDetail = newObject.dictionaryNameWithPropertyDetail;
            // 交換解析完成的目前持有存取規則
            accessRuleSets = newObject.accessRuleSets;
            // 交換物件類別
            driveUnitSchemaClasses = newObject.driveUnitSchemaClasses;
            // 通知外部替換成功
            return this;
        }
    }
}
