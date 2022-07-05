using ADService.Environments;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Media
{
    /// <summary>
    /// 此藍本物件相關資料
    /// </summary>
    internal sealed class UnitSchemaClass : UnitSchema
    {
        #region 找尋物件類型
        /// <summary>
        /// 藍本的搜尋目標
        /// </summary>
        internal const string SCHEMA_CLASS = "classSchema";

        /// <summary>
        /// 何者的子物件
        /// </summary>
        private const string SCHEMA_CLASS_SUBCLASSOF = "subClassOf";
        /// <summary>
        /// 使用的輔助物件
        /// </summary>
        private const string SCHEMA_CLASS_AUXILIARYCLASS = "auxiliaryClass";
        /// <summary>
        /// 使用的系統輔助物件
        /// </summary>
        private const string SCHEMA_CLASS_SYSTEMAUXILIARYCLASS = "systemAuxiliaryClass";

        /// <summary>
        /// 必定持有的屬性
        /// </summary>
        private const string SCHEMA_CLASS_MUSTCONTAIN = "mustContain";
        /// <summary>
        /// 系統要求必定持有的屬性
        /// </summary>
        private const string SCHEMA_CLASS_SYSTEMMUSTCONTAIN = "systemMustContain";
        /// <summary>
        /// 可能持有的屬性
        /// </summary>
        private const string SCHEMA_CLASS_MAYCONTAIN = "mayContain";
        /// <summary>
        /// 系統要求可能持有的屬性
        /// </summary>
        private const string SCHEMA_CLASS_SYSTEMMAYCONTAIN = "systemMayContain";

        /// <summary>
        /// 可以做為上層物件的類型
        /// </summary>
        private const string SCHEMA_CLASS_POSSSUPERIORS = "possSuperiors";
        /// <summary>
        /// 系統要求可以做為上層物件的類型
        /// </summary>
        private const string SCHEMA_CLASS_SYSTEMPOSSSUPERIORS = "systemPossSuperiors";

        /// <summary>
        /// 取得物件藍本
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="ldapDisplayNames">展示名稱</param>
        /// <returns>符合物件名稱的物件藍本</returns>
        internal static UnitSchemaClass[] GetWithLDAPDisplayNames(in LDAPConfigurationDispatcher dispatcher, in IEnumerable<string> ldapDisplayNames)
        {
            // 新建立藍本入口物件
            using (DirectoryEntry root = dispatcher.ByDistinguisedName($"{CONTEXT_SCHEMA},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 主要找尋的展示命成
                string subSearchMian = LDAPConfiguration.GetORFiliter(SCHEMA_PROPERTY, ldapDisplayNames);
                // 限制找尋的物件類型應為物件類型
                string subSearchCategory = LDAPConfiguration.GetORFiliter(Properties.C_OBJECTCATEGORY, SCHEMA_CLASS);
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"(&{subSearchMian}{subSearchCategory})";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(root, filiter, BASE_PROPERTIES))
                {
                    // 遍歷取得的所有項目
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 對外提供的項目
                        List<UnitSchemaClass> unitSchemas = new List<UnitSchemaClass>(all.Count);
                        // 取得指定物件
                        foreach (SearchResult one in all)
                        {
                            // 簡易防呆
                            if (one == null)
                            {
                                // 無法找到資料交由外部判斷是否錯誤
                                continue;
                            }

                            // 轉換成入口物件
                            using (DirectoryEntry entry = one.GetDirectoryEntry())
                            {
                                // 對外提供的基底結構
                                UnitSchemaClass unitSchemaClass = new UnitSchemaClass(entry.Properties);
                                // 對外提供描述名稱
                                unitSchemas.Add(unitSchemaClass);
                            }
                        }
                        // 轉換成陣列對外圖供
                        return unitSchemas.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// 取得物件藍本
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="superiorLDAPDisplayNames">可作為父層的屬性名稱</param>
        /// <returns>符合物件名稱的物件藍本</returns>
        internal static UnitSchemaClass[] GetWithSuperiorLDAPDisplayNames(in LDAPConfigurationDispatcher dispatcher, in IEnumerable<string> superiorLDAPDisplayNames)
        {
            // 新建立藍本入口物件
            using (DirectoryEntry root = dispatcher.ByDistinguisedName($"{CONTEXT_SCHEMA},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 主要找尋的展示集成
                string subSearchMian = LDAPConfiguration.GetORFiliter(SCHEMA_CLASS_POSSSUPERIORS, superiorLDAPDisplayNames);
                // 系統找尋的展示集成
                string subSearchMianSystem = LDAPConfiguration.GetORFiliter(SCHEMA_CLASS_SYSTEMPOSSSUPERIORS, superiorLDAPDisplayNames);
                // 限制找尋的物件類型應為物件類型
                string subSearchCategory = LDAPConfiguration.GetORFiliter(Properties.C_OBJECTCATEGORY, SCHEMA_CLASS);
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"(&(|{subSearchMian}{subSearchMianSystem}){subSearchCategory})";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(root, filiter, BASE_PROPERTIES))
                {
                    // 遍歷取得的所有項目
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 對外提供的項目
                        List<UnitSchemaClass> unitSchemas = new List<UnitSchemaClass>(all.Count);
                        // 取得指定物件
                        foreach (SearchResult one in all)
                        {
                            // 簡易防呆
                            if (one == null)
                            {
                                // 無法找到資料交由外部判斷是否錯誤
                                continue;
                            }

                            // 轉換成入口物件
                            using (DirectoryEntry entry = one.GetDirectoryEntry())
                            {
                                // 對外提供的基底結構
                                UnitSchemaClass unitSchemaClass = new UnitSchemaClass(entry.Properties);
                                // 對外提供描述名稱
                                unitSchemas.Add(unitSchemaClass);
                            }
                        }
                        // 轉換成陣列對外圖供
                        return unitSchemas.ToArray();
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// 將傳入的藍本物件中所有驅動類型名稱以不重複方式對外提供
        /// </summary>
        /// <param name="unitSchemaClasses">找尋的物件類型</param>
        /// <returns>不重複的驅動類型物件名稱</returns>
        internal static HashSet<string> DrivedClassNames(params UnitSchemaClass[] unitSchemaClasses)
        {
            // 不重複的物件類型名稱
            HashSet<string> drivedClassNames = new HashSet<string>();
            // 遍歷要求的物件類型
            foreach (UnitSchemaClass unitSchemaClass in unitSchemaClasses)
            {
                // 推入父層類別
                drivedClassNames.Add(unitSchemaClass.SubClassOf);
                // 推入輔助類別
                Array.ForEach(unitSchemaClass.AuxiliaryClasses, className => drivedClassNames.Add(className));
                // 推入系統輔助類別
                Array.ForEach(unitSchemaClass.SystemAuxiliaryClasses, className => drivedClassNames.Add(className));
            }
            // 遍歷要求的物件類型移除自身
            Array.ForEach(unitSchemaClasses, unitSchemaClass => drivedClassNames.Remove(unitSchemaClass.Name));
            // 對外提供所有名稱
            return drivedClassNames;
        }

        /// <summary>
        /// 將傳入的藍本物件中所有屬性名稱名稱以不重複方式對外提供
        /// </summary>
        /// <param name="unitSchemaClasses">找尋的物件類型</param>
        internal static string[] UniqueAttributeNames(in IEnumerable<UnitSchemaClass> unitSchemaClasses)
        {
            // 不重複的物件類型名稱
            HashSet<string> uniqueAttributeNames = new HashSet<string>();
            // 遍歷要求的物件類型
            foreach (UnitSchemaClass unitSchemaClass in unitSchemaClasses)
            {
                // 推入必定存在的屬性名稱
                Array.ForEach(unitSchemaClass.MustContain, attributeName => uniqueAttributeNames.Add(attributeName));
                // 推入系土要求必定存在的屬性名稱
                Array.ForEach(unitSchemaClass.SystemMustContain, attributeName => uniqueAttributeNames.Add(attributeName));
                // 推入必可能在的屬性名稱
                Array.ForEach(unitSchemaClass.MayContain, attributeName => uniqueAttributeNames.Add(attributeName));
                // 推入系土要求可能存在的屬性名稱
                Array.ForEach(unitSchemaClass.SystemMayContain, attributeName => uniqueAttributeNames.Add(attributeName));
            }

            // 宣告容器
            string[] attributeNames = new string[uniqueAttributeNames.Count];
            // 複製
            uniqueAttributeNames.CopyTo(attributeNames);
            // 對外提供
            return attributeNames;
        }

        /// <summary>
        /// 檢查傳入的額外權限何者可以套用至指定類型物件藍本的內容物
        /// </summary>
        /// <param name="unitSchemaClass">指定藍本物件</param>
        /// <param name="unitControlAccesses">傳入的存取權限</param>
        /// <returns>可套用至指定藍本物件的存取權限</returns>
        internal static string[] WhichAppliedWith(in UnitSchemaClass unitSchemaClass, params UnitControlAccess[] unitControlAccesses)
        {
            // 依賴存取權限大小最大微傳入物件的長度
            List<string> unitControlAccessGUIDs = new List<string>(unitControlAccesses.Length);
            // 將屬性 GUID 轉乘小寫
            string schemaGUIDLower = unitSchemaClass.SchemaGUID.ToLower();
            // 遍歷所有存取權限
            foreach (UnitControlAccess unitControlAccess in unitControlAccesses)
            {
                // 檢查是否依賴於此類別藍本
                if (!unitControlAccess.IsAppliedWith(schemaGUIDLower))
                {
                    // 不依賴則跳出
                    continue;
                }

                // 將依賴的額外權限 GUID 轉為小寫
                string unitControlAccessGUIDLower = unitControlAccess.GUID.ToLower();
                // 加入關聯項目
                unitControlAccessGUIDs.Add(unitControlAccessGUIDLower);
            }
            // 絕對不會重複
            return unitControlAccessGUIDs.ToArray();
        }
        /// <summary>
        /// 檢查傳入的物件類型藍本何者可以作為指定類型物件藍本的內容物
        /// </summary>
        /// <param name="unitSchemaClass">指定藍本物件</param>
        /// <param name="unitSchemaClasses">傳入的物件類型藍本</param>
        /// <returns>可做為內容物的物件 GUID </returns>
        internal static string[] WhichChildrenWith(in UnitSchemaClass unitSchemaClass, params UnitSchemaClass[] unitSchemaClasses)
        {
            // 依賴存取權限大小最大微傳入物件的長度
            List<string> unitSchemaClassGUIDs = new List<string>(unitSchemaClasses.Length);
            // 將屬性 GUID 轉乘小寫
            string unitSchemaClassNameLower = unitSchemaClass.Name.ToLower();
            // 遍歷所有存取權限
            foreach (UnitSchemaClass childrenUnitSchemaClass in unitSchemaClasses)
            {
                // 檢查是否依賴於此類別藍本
                if (!childrenUnitSchemaClass.IsChildrenWith(unitSchemaClassNameLower))
                {
                    // 不依賴則跳出
                    continue;
                }

                // 將依賴的額外權限 GUID 轉為小寫
                string unitSchemaClassGUIDLower = childrenUnitSchemaClass.SchemaGUID.ToLower();
                // 加入關聯項目
                unitSchemaClassGUIDs.Add(unitSchemaClassGUIDLower);
            }
            // 絕對不會重複
            return unitSchemaClassGUIDs.ToArray();
        }

        /// <summary>
        /// 藍本物件的遊河種類別衍伸
        /// </summary>
        private readonly string SubClassOf;
        /// <summary>
        /// 輔助物件列表
        /// </summary>
        private readonly string[] AuxiliaryClasses;
        /// <summary>
        /// 輔助物件列表: 僅系統可見
        /// </summary>
        private readonly string[] SystemAuxiliaryClasses;

        /// <summary>
        /// 必定持有的屬性
        /// </summary>
        private readonly string[] MustContain;
        /// <summary>
        /// 系統要求必定持有的屬性
        /// </summary>
        private readonly string[] SystemMustContain;
        /// <summary>
        /// 可能持有的屬性
        /// </summary>
        private readonly string[] MayContain;
        /// <summary>
        /// 系統要求可能持有的屬性
        /// </summary>
        private readonly string[] SystemMayContain;

        /// <summary>
        /// 可以做為上層物件的類型
        /// </summary>
        private readonly HashSet<string> PossSuperiors;
        /// <summary>
        /// 系統要求可以做為上層物件的類型
        /// </summary>
        private readonly HashSet<string> SystemPossSuperiors;

        /// <summary>
        /// 實作藍本結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        internal UnitSchemaClass(in PropertyCollection properties) : base(properties)
        {
            SubClassOf = LDAPConfiguration.ParseSingleValue<string>(SCHEMA_CLASS_SUBCLASSOF, properties);
            AuxiliaryClasses = LDAPConfiguration.ParseMutipleValue<string>(SCHEMA_CLASS_AUXILIARYCLASS, properties);
            SystemAuxiliaryClasses = LDAPConfiguration.ParseMutipleValue<string>(SCHEMA_CLASS_SYSTEMAUXILIARYCLASS, properties);

            MustContain = LDAPConfiguration.ParseMutipleValue<string>(SCHEMA_CLASS_MUSTCONTAIN, properties);
            SystemMustContain = LDAPConfiguration.ParseMutipleValue<string>(SCHEMA_CLASS_SYSTEMMUSTCONTAIN, properties);
            MayContain = LDAPConfiguration.ParseMutipleValue<string>(SCHEMA_CLASS_MAYCONTAIN, properties);
            SystemMayContain = LDAPConfiguration.ParseMutipleValue<string>(SCHEMA_CLASS_SYSTEMMAYCONTAIN, properties);

            PossSuperiors = new HashSet<string>(LDAPConfiguration.ParseMutipleValue<string>(SCHEMA_CLASS_POSSSUPERIORS, properties));
            SystemPossSuperiors = new HashSet<string>(LDAPConfiguration.ParseMutipleValue<string>(SCHEMA_CLASS_SYSTEMPOSSSUPERIORS, properties));
        }

        /// <summary>
        /// 檢查傳入的類型物件藍本是否為此物件類型藍本的父層
        /// </summary>
        /// <param name="unitSchemaClasNameLower">指定物件蕾型藍本的名稱</param>
        /// <returns>是否可作為指定藍本物件的內容物</returns>
        internal bool IsChildrenWith(in string unitSchemaClasNameLower) => PossSuperiors.Contains(unitSchemaClasNameLower) || SystemPossSuperiors.Contains(unitSchemaClasNameLower);
    }
}
