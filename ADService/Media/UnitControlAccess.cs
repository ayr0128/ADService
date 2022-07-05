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
        /// 搜尋時找尋的資料
        /// </summary>
        protected static readonly string[] BASE_PROPERTIES = new string[] {
            ATTRIBUTE_CONTROLACCESS_GUID,
            ATTRIBUTE_CONTROLACCESS_PROPERTY,
            ATTRIBUTE_CONTROLACCESS_APPLIESTO,
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

                            // 對外提供預計對外提供的資料
                            UnitControlAccess unitControlAccess = new UnitControlAccess(one.Properties);
                            // 加入作為對外提供的項目之一
                            unitControlAccesses.Add(unitControlAccess);
                        }
                        // 對外提供內部項目
                        return unitControlAccesses.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// 取得擴展權限
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="extendedRightGUIDLower">目標 GUID</param>
        /// <returns>額外權限結構</returns>
        internal static UnitControlAccess GetWithGUID(in LDAPConfigurationDispatcher dispatcher, in string extendedRightGUIDLower)
        {
            // 新建立藍本入口物件
            using (DirectoryEntry extendedRight = dispatcher.ByDistinguisedName($"{CONTEXT_CONTROLACCESS},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = LDAPConfiguration.GetORFiliter(ATTRIBUTE_CONTROLACCESS_GUID, extendedRightGUIDLower);
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(extendedRight, filiter, BASE_PROPERTIES))
                {
                    // 取得指定物件
                    SearchResult one = searcher.FindOne();
                    // 簡易防呆
                    if (one == null)
                    {
                        // 無法找到資料交由外部判斷是否錯誤
                        return null;
                    }

                    // 返回查詢到的資料
                    return new UnitControlAccess(one.Properties);
                }
            }
        }

        /// <summary>
        /// 透過額外全縣
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="dictionaryAttributeNameWithValues">須查詢的類別ＧＵＩＤ</param>
        /// <returns>額外權限結構</returns>
        internal static UnitControlAccess[] GetWithPropertySet(in LDAPConfigurationDispatcher dispatcher, in Dictionary<string, HashSet<string>> dictionaryAttributeNameWithValues)
        {
            // 新建立藍本入口物件
            using (DirectoryEntry entry = dispatcher.ByDistinguisedName($"{CONTEXT_CONTROLACCESS},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 帶組合的字串
                List<string> listFiliterString = new List<string>(dictionaryAttributeNameWithValues.Count);
                // 組層搜尋字串: 此字典預計絕對不會為空
                foreach (KeyValuePair<string, HashSet<string>> pair in dictionaryAttributeNameWithValues)
                {
                    // 使用遍歷的鍵值與數值組成子搜尋字串
                    string subFiliter = LDAPConfiguration.GetORFiliter(pair.Key, pair.Value);
                    // 推入帶組合字串中
                    listFiliterString.Add(subFiliter);
                }

                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"(&{string.Join("", listFiliterString)})";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(entry, filiter, BASE_PROPERTIES))
                {
                    // 取得所有查詢到的項目
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 大小為找到的所有項目
                        List<UnitControlAccess> listUnitExtendedRight = new List<UnitControlAccess>(all.Count);
                        // 取得指定物件
                        foreach (SearchResult one in all)
                        {
                            // 簡易防呆
                            if (one == null)
                            {
                                // 無法找到資料交由外部判斷是否錯誤
                                continue;
                            }

                            // 對外提供預計對外提供的資料
                            UnitControlAccess unitExtendedRight = new UnitControlAccess(one.Properties);
                            // 加入作為對外提供的項目之一
                            listUnitExtendedRight.Add(unitExtendedRight);
                        }
                        // 對外提供內部項目
                        return listUnitExtendedRight.ToArray();
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
        /// <param name="duration"></param>
        /// <returns></returns>
        internal bool IsExpired(in TimeSpan duration) => (DateTime.UtcNow - EnableTime) >= duration;

        /// <summary>
        /// 實作額外權限結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        internal UnitControlAccess(in ResultPropertyCollection properties)
        {
            Name = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_CONTROLACCESS_PROPERTY, properties);
            GUID = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_CONTROLACCESS_GUID, properties);

            // 避免意外情況先改成統一小寫
            string[] appliesToGUID = LDAPConfiguration.ParseMutipleValue<string>(ATTRIBUTE_CONTROLACCESS_APPLIESTO, properties);
            // 宣告容器大小
            AppliesTo = new HashSet<string>(appliesToGUID.Length);
            // 轉乘小血後推入 HashSet 中
            Array.ForEach(appliesToGUID, (schemaGUID) => AppliesTo.Add(schemaGUID.ToLower()));
        }
    }
}
