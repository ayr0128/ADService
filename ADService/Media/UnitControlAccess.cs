using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Media
{
    /// <summary>
    /// 額萬權限用資料格式
    /// </summary>
    internal class UnitControlAccess
    {
        #region 查詢相關資料
        /// <summary>
        /// 額外權限的 DN 組合字尾
        /// </summary>
        protected const string CONTEXT_CONTROLACCESS = "CN=Extended-Rights";
        /// <summary>
        /// 額外權限的 GUID 欄位名稱
        /// </summary>
        internal const string ATTRIBUTE_CONTROLACCESS_GUID = "rightsGuid";
        /// <summary>
        /// 額外權限的搜尋目標
        /// </summary>
        protected const string ATTRIBUTE_CONTROLACCESS_PROPERTY = "displayName";
        /// <summary>
        /// 額外權限的被盜用至哪些類別
        /// </summary>
        internal const string ATTRIBUTE_CONTROLACCESS_APPLIESTO = "appliesTo";
        /// <summary>
        /// 此權限英特用的規則
        /// </summary>
        internal const string ATTRIBUTE_CONTROLACCESS_VALIDACCESSES = "validAccesses";

        /// <summary>
        /// 搜尋時找尋的資料
        /// </summary>
        protected static readonly string[] BASE_PROPERTIES = new string[] {
            Properties.C_DISTINGUISHEDNAME,
        };

        /// <summary>
        /// 透過額外全縣
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="unitSchemaClassGUIDs">需取得的控制權限</param>
        /// <returns>額外權限結構</returns>
        internal static UnitControlAccess[] GetAppliedTo(in LDAPConfigurationDispatcher dispatcher, in IEnumerable<string> unitSchemaClassGUIDs)
        {
            // 新建立藍本入口物件
            using (DirectoryEntry root = dispatcher.ByDistinguisedName($"{CONTEXT_CONTROLACCESS},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 將傳入的資料轉乘小寫 GUID 陣列

                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"(&{LDAPConfiguration.GetORFiliter(ATTRIBUTE_CONTROLACCESS_APPLIESTO, unitSchemaClassGUIDs)})";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(root, filiter, BASE_PROPERTIES))
                {
                    // 取得所有查詢到的項目
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 大小為找到的所有項目
                        List<UnitControlAccess> unitControlAccesses = new List<UnitControlAccess>(all.Count);
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
                                // 對外提供預計對外提供的資料
                                UnitControlAccess unitControlAccess = new UnitControlAccess(entry.Properties);
                                // 加入作為對外提供的項目之一
                                unitControlAccesses.Add(unitControlAccess);
                            }
                        }
                        // 對外提供內部項目
                        return unitControlAccesses.ToArray();
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_CONTROLACCESS_PROPERTY"> 展示名稱 </see> 取得的相關字串
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_CONTROLACCESS_GUID"> GUID </see> 取得的相關字串
        /// </summary>
        internal readonly string GUID;
        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_CONTROLACCESS_VALIDACCESSES"> 可用用權限 </see> 取得的相關字串
        /// </summary>
        internal ActiveDirectoryRights AccessRuleControl { get; private set; }

        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_CONTROLACCESS_APPLIESTO"> GUID </see> 取得的相關字串
        /// </summary>
        private readonly HashSet<string> AppliesTo;
        /// <summary>
        /// 物件類型是否支援
        /// </summary>
        /// <param name="appliedGUID">藍本 GUID</param>
        /// <returns>是否套用</returns>
        internal bool IsAppliedWith(in string appliedGUID) => AppliesTo.Contains(appliedGUID.ToLower());

        /// <summary>
        /// 啟用時間
        /// </summary>
        private readonly DateTime EnableTime = DateTime.UtcNow;
        /// <summary>
        /// 是否已經超過保留時間
        /// </summary>
        /// <param name="duration">經過多久後過期</param>
        /// <returns>是否過期</returns>
        internal bool IsExpired(in TimeSpan duration) => (DateTime.UtcNow - EnableTime) >= duration;

        /// <summary>
        /// 實作額外權限結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        internal UnitControlAccess(in PropertyCollection properties)
        {
            Name = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_CONTROLACCESS_PROPERTY, properties);
            GUID = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_CONTROLACCESS_GUID, properties);

            // 取得內部儲存的類型
            int storedActiveDirectoryRights = LDAPConfiguration.ParseSingleValue<int>(ATTRIBUTE_CONTROLACCESS_VALIDACCESSES, properties);
            // 強制轉型並取得系統旗標
            AccessRuleControl = (ActiveDirectoryRights)Enum.ToObject(typeof(ActiveDirectoryRights), storedActiveDirectoryRights);

            // 取得內部儲存的類型
            int storedActiveDirectoryRights = LDAPConfiguration.ParseSingleValue<int>(ATTRIBUTE_CONTROLACCESS_VALIDACCESSES, properties);
            // 強制轉型並取得系統旗標
            AccessRuleControl = (ActiveDirectoryRights)Enum.ToObject(typeof(ActiveDirectoryRights), storedActiveDirectoryRights);

            // 避免意外情況先改成統一小寫
            string[] appliesToGUID = LDAPConfiguration.ParseMutipleValue<string>(ATTRIBUTE_CONTROLACCESS_APPLIESTO, properties);
            // 宣告容器大小
            AppliesTo = new HashSet<string>(appliesToGUID.Length);
            // 轉乘小血後推入 HashSet 中
            Array.ForEach(appliesToGUID, schemaGUID => AppliesTo.Add(schemaGUID.ToLower()));
        }
    }
}
