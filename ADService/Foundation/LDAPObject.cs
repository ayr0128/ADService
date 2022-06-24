using ADService.Configuration;
using ADService.Details;
using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Foundation
{
    /// <summary>
    /// 最基底的物件類型: 採用化學中的受質作為名稱代表其特性
    /// </summary>
    public class LDAPObject
    {
        /// <summary>
        /// 搜尋物件時使用的特性鍵值
        /// </summary>
        internal static string[] PropertiesToLoad => new string[] { Properties.C_DISTINGGUISHEDNAME, "allowedAttributes", "allowedChildClasses" };

        #region 創建物件以及創建描述
        /// <summary>
        /// ADService 物件的工廠模式
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="entriesMedia">入口物件創建器</param>
        /// <param name="propertiesResult">透過找尋取得字的屬性</param>
        /// <returns>使用 object 封裝對外提供的物件</returns>
        /// <exception cref="LDAPExceptions">物件未實作工廠模式或實作過程中出現錯誤時丟出例外</exception>
        internal static LDAPObject ToObject(in DirectoryEntry entry, in LDAPEntriesMedia entriesMedia, in ResultPropertyCollection propertiesResult)
        {
            // 解析物件類型
            CategoryTypes type = LDAPEntries.ParseCategory(entry.Properties);
            // 使用物件類型製作對應的物件
            switch (type)
            {
                case CategoryTypes.CONTAINER: // 容器
                    {
                        // 對外提供 '容器'
                        return new LDAPContainer(entry, entriesMedia, propertiesResult);
                    }
                case CategoryTypes.DOMAIN_DNS: // 網域
                    {
                        // 對外提供 '網域' 結構
                        return new LDAPDomainDNS(entry, entriesMedia, propertiesResult);
                    }
                case CategoryTypes.ORGANIZATION_UNIT: // 組織單位
                    {
                        // 對外提供 '組織單位' 結構
                        return new LDAPOrganizationUnit(entry, entriesMedia, propertiesResult);
                    }
                case CategoryTypes.ForeignSecurityPrincipals: // 內部安全性群組
                case CategoryTypes.GROUP:                     // 群組
                    {
                        // 對外提供 '群組' 結構
                        return new LDAPGroup(entry, entriesMedia, propertiesResult);
                    }
                case CategoryTypes.PERSON: // 成員
                    {
                        // 對外提供 '成員' 結構
                        return new LDAPPerson(entry, entriesMedia, propertiesResult);
                    }
                default:
                    {
                        // 對外丟出例外: 未實作
                        throw new LDAPExceptions($"目標物件類型:{type} 尚未實作工廠模式, 無法轉換", ErrorCodes.LOGIC_ERROR);
                    }
            }
        }

        /// <summary>
        /// 內部使用: 取得主要隸屬群組
        /// </summary>
        /// <param name="entriesMedia">入口物件創建器</param>
        /// <param name="primaryGroupToken">主要隸屬群組的 Token</param>
        internal static Dictionary<string, LDAPRelationship> ToRelationshipByToken(in LDAPEntriesMedia entriesMedia, in int primaryGroupToken)
        {
            // 找到須限制的物件類型
            Dictionary<CategoryTypes, string> dictionaryLimitedCategory = LDAPCategory.GetValuesByTypes(CategoryTypes.PERSON);
            // 使用根目錄
            using (DirectoryEntry root = entriesMedia.DomainRoot())
            {
                // 加密避免 LDAP 注入式攻擊: 透過主要隸屬群組關聯的物件必定圍成員
                string encoderFiliter = $"(&{LDAPEntries.GetORFiliter(Properties.C_OBJECTCATEGORY, dictionaryLimitedCategory.Values)}({Properties.C_PRIMARYGROUPID}={primaryGroupToken}))";

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
                                // 轉換成基礎物件
                                LDAPRelationship relationship = new LDAPRelationship(entry, true);
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
        /// <param name="entriesMedia">入口物件創建器</param>
        /// <param name="primaryGroupSID">主要隸屬群組的 SID</param>
        internal static LDAPRelationship ToRelationshipBySID(in LDAPEntriesMedia entriesMedia, in string primaryGroupSID)
        {
            // 使用 SID 取得指定物件
            using (DirectoryEntry entry = entriesMedia.BySID(primaryGroupSID))
            {
                // 主要隸屬的物件一定是群組
                return new LDAPRelationship(entry, true);
            }
        }

        /// <summary>
        /// 內部使用: 取得指定區分名稱
        /// </summary>
        /// <param name="entriesMedia">入口物件創建器</param>
        /// <param name="distinguishedNames">所有欲找尋的區分名稱 (memberOf)</param>
        /// <returns>指定區分名稱的物件, 結構如右: Dictionary '區分名稱, 物件' </returns>
        internal static Dictionary<string, LDAPRelationship> ToRelationshipByDNs(in LDAPEntriesMedia entriesMedia, params string[] distinguishedNames)
        {
            // 沒有隸屬群組時
            if (distinguishedNames == null)
            {
                // 對外提供空陣列
                return new Dictionary<string, LDAPRelationship>(0);
            }

            // 取代用的新字典
            Dictionary<string, LDAPRelationship> dictionaryDNWithGroup = new Dictionary<string, LDAPRelationship>(distinguishedNames.Length);
            // 遍歷指定的區分名稱
            foreach (string distinguishedName in distinguishedNames)
            {
                // 指定區分名稱取得物件
                using (DirectoryEntry entry = entriesMedia.ByDistinguisedName(distinguishedName))
                {
                    // 推入字典
                    dictionaryDNWithGroup.Add(distinguishedName, new LDAPRelationship(entry, false));
                }
            }
            // 提供給外部
            return dictionaryDNWithGroup;
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
            // 根據類型決定如何解析出父層組織單位
            switch (Type)
            {
                case CategoryTypes.CONTAINER:                 // 容器
                case CategoryTypes.PERSON:                    // 成員
                case CategoryTypes.ForeignSecurityPrincipals: // 內部安全群組
                case CategoryTypes.GROUP:                     // 群組
                    {
                        // 重新命名用的結構
                        string nameFormat = $"{Properties.P_CN.ToUpper()}={Name}";
                        // 找到名稱的位置: 必定能找到
                        int index = DistinguishedName.IndexOf(nameFormat);
                        // 切割字串取得目標所在的組織單位
                        OrganizationUnit = DistinguishedName.Substring(index + nameFormat.Length + 1);
                        // 對外返回成功
                        return true;
                    }
                case CategoryTypes.ORGANIZATION_UNIT:
                    {
                        // 重新命名用的結構
                        string nameFormat = $"{Properties.P_OU.ToUpper()}={Name}";
                        // 找到名稱的位置: 必定能找到
                        int index = DistinguishedName.IndexOf(nameFormat);
                        // 切割字串取得目標所在的組織單位
                        OrganizationUnit = DistinguishedName.Substring(index + nameFormat.Length + 1);
                        // 對外返回成功
                        return true;
                    }
                case CategoryTypes.DOMAIN_DNS:
                default:
                    {
                        // 回傳: 沒有父層組織單位
                        return false;
                    }
            }
        }

        /// <summary>
        /// 設定鍵值儲存與解析功能
        /// </summary>
        internal LDAPProperties StoredProperties;

        /// <summary>
        /// 此物件的名稱
        /// </summary>
        public string Name => StoredProperties.GetPropertySingle<string>(Properties.P_NAME);
        /// <summary>
        /// 此物件的區分名稱
        /// </summary>
        public string DistinguishedName => StoredProperties.GetPropertySingle<string>(Properties.C_DISTINGGUISHEDNAME);
        /// <summary>
        /// 此物件的全域唯一標識符
        /// </summary>
        public string GUID => StoredProperties.GetPropertyGUID(Properties.C_OBJECTGUID);
        /// <summary>
        /// 容器類型
        /// </summary>
        public CategoryTypes Type
        {
            get
            {
                // 取得 類別 不存在應丟出例外
                string storedValue = StoredProperties.GetPropertySingle<string>(Properties.C_OBJECTCATEGORY);
                // 轉換物件類型
                return LDAPEntries.GetObjectType(storedValue);
            }
        }

        /// <summary>
        /// 使用鍵值參數初始化
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="entriesMedia">入口物件創建器</param>
        /// <param name="propertiesResult">透過找尋取得字的屬性</param>
        /// <exception cref="LDAPExceptions">解析鍵值不符合規則時對外丟出</exception>
        internal LDAPObject(in DirectoryEntry entry, in LDAPEntriesMedia entriesMedia, in ResultPropertyCollection propertiesResult)
        {
            // 腳本物件找尋
            LDAPConfiguration configuration = new LDAPConfiguration(entriesMedia);
            // 初始化可用屬性
            StoredProperties = new LDAPProperties(configuration, entry, propertiesResult);
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

            // 執行至此能保證 GUID 相同, 替換內部特性鍵值內容
            StoredProperties = newObject.StoredProperties;
            // 通知外部替換成功
            return this;
        }
    }
}
