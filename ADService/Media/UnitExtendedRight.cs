using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Media
{
    /// <summary>
    /// 額萬權限用資料格式
    /// </summary>
    internal class UnitExtendedRight
    {
        #region 查詢相關資料
        /// <summary>
        /// 額外權限的 DN 組合字尾
        /// </summary>
        protected const string CONTEXT_EXTENDEDRIGHT = "CN=Extended-Rights";
        /// <summary>
        /// 額外權限的 GUID 欄位名稱
        /// </summary>
        internal const string ATTRIBUTE_EXTENDEDRIGHT_GUID = "rightsGuid";
        /// <summary>
        /// 額外權限的搜尋目標
        /// </summary>
        protected const string ATTRIBUTE_EXTENDEDRIGHT_PROPERTY = "displayName";
        /// <summary>
        /// 額外權限的被盜用至哪些類別
        /// </summary>
        internal const string ATTRIBUTE_EXTENDEDRIGHT_APPLIESTO = "appliesTo";

        /// <summary>
        /// 搜尋時找尋的資料
        /// </summary>
        protected static readonly string[] PROPERTIES = new string[] {
            ATTRIBUTE_EXTENDEDRIGHT_GUID,
            ATTRIBUTE_EXTENDEDRIGHT_PROPERTY,
            ATTRIBUTE_EXTENDEDRIGHT_APPLIESTO,
        };

        /// <summary>
        /// 取得擴展權限
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="convertedGUIDs">目標 GUID</param>
        /// <returns>額外權限結構</returns>
        internal static UnitExtendedRight[] GetWithGUID(in LDAPConfigurationDispatcher dispatcher, in IEnumerable<string> convertedGUIDs)
        {
            // 新建立藍本入口物件
            using (DirectoryEntry extendedRight = dispatcher.ByDistinguisedName($"{CONTEXT_EXTENDEDRIGHT},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = LDAPConfiguration.GetORFiliter(ATTRIBUTE_EXTENDEDRIGHT_GUID, convertedGUIDs);
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(extendedRight, filiter, PROPERTIES))
                {
                    // 找到所有查詢
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 數量必定為找尋到的物件數量
                        List<UnitExtendedRight> unitExtendedRights = new List<UnitExtendedRight>(all.Count);
                        // 取得指定物件
                        foreach (SearchResult one in all)
                        {
                            // 簡易防呆
                            if (one == null)
                            {
                                // 無法找到資料交由外部判斷是否錯誤
                                continue;
                            }

                            // 強型別宣告: 方便閱讀
                            UnitExtendedRight unitExtendedRight = new UnitExtendedRight(one.Properties);
                            // 推入對外提供的陣列
                            unitExtendedRights.Add(unitExtendedRight);
                        }
                        // 返回查詢到的資料
                        return unitExtendedRights.ToArray();
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
        internal static UnitExtendedRight GetWithGUID(in LDAPConfigurationDispatcher dispatcher, in string extendedRightGUIDLower)
        {
            // 新建立藍本入口物件
            using (DirectoryEntry extendedRight = dispatcher.ByDistinguisedName($"{CONTEXT_EXTENDEDRIGHT},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = LDAPConfiguration.GetORFiliter(ATTRIBUTE_EXTENDEDRIGHT_GUID, extendedRightGUIDLower);
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(extendedRight, filiter, PROPERTIES))
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
                    return new UnitExtendedRight(one.Properties);
                }
            }
        }

        /// <summary>
        /// 透過額外全縣
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="dictionaryAttributeNameWithValues">須查詢的類別ＧＵＩＤ</param>
        /// <returns>額外權限結構</returns>
        internal static UnitExtendedRight[] GetWithPropertySet(in LDAPConfigurationDispatcher dispatcher, in Dictionary<string, HashSet<string>> dictionaryAttributeNameWithValues)
        {
            // 新建立藍本入口物件
            using (DirectoryEntry entry = dispatcher.ByDistinguisedName($"{CONTEXT_EXTENDEDRIGHT},{dispatcher.ConfigurationDistinguishedName}"))
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
                using (DirectorySearcher searcher = new DirectorySearcher(entry, filiter, PROPERTIES))
                {
                    // 取得所有查詢到的項目
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 大小為找到的所有項目
                        List<UnitExtendedRight> listUnitExtendedRight = new List<UnitExtendedRight>(all.Count);
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
                            UnitExtendedRight unitExtendedRight = new UnitExtendedRight(one.Properties);
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
        /// 使用欄位 <see cref="ATTRIBUTE_EXTENDEDRIGHT_PROPERTY"> 展示名稱 </see> 取得的相關字串
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_EXTENDEDRIGHT_GUID"> GUID </see> 取得的相關字串
        /// </summary>
        internal readonly string GUID;

        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_EXTENDEDRIGHT_APPLIESTO"> GUID </see> 取得的相關字串
        /// </summary>
        private readonly HashSet<string> AppliesTo;
        /// <summary>
        /// 物件類型是否支援
        /// </summary>
        /// <param name="appliedGUID">藍本 GUID</param>
        /// <returns>是否套用</returns>
        internal bool WasAppliedWith(in string appliedGUID) => AppliesTo.Contains(appliedGUID.ToLower());

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
        internal UnitExtendedRight(in ResultPropertyCollection properties)
        {
            Name = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_EXTENDEDRIGHT_PROPERTY, properties);
            GUID = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_EXTENDEDRIGHT_GUID, properties);

            // 避免意外情況先改成統一小寫
            string[] appliesToGUID = LDAPConfiguration.ParseMutipleValue<string>(ATTRIBUTE_EXTENDEDRIGHT_APPLIESTO, properties);
            // 宣告容器大小
            AppliesTo = new HashSet<string>(appliesToGUID.Length);
            // 轉乘小血後推入 HashSet 中
            Array.ForEach(appliesToGUID, (schemaGUID) => AppliesTo.Add(schemaGUID.ToLower()));
        }
    }
}
