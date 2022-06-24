using ADService.Media;
using System;
using System.DirectoryServices;

namespace ADService.Configuration
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
        private const string CONTEXT_EXTENDEDRIGHT = "CN=Extended-Rights";
        /// <summary>
        /// 額外權限的 GUID 欄位名稱
        /// </summary>
        private const string ATTRIBUTE_EXTENDEDRIGHT_GUID = "rightsGuid";
        /// <summary>
        /// 額外權限的搜尋目標
        /// </summary>
        private const string ATTRIBUTE_EXTENDEDRIGHT_PROPERTY = "displayName";

        /// <summary>
        /// 取得擴展權限的指定欄位名稱
        /// </summary>
        /// <param name="entries">入口物件製作器</param>
        /// <param name="value">目標 GUID</param>
        /// <param name="configuration">設定位置</param>
        /// <returns>額外權限結構</returns>
        internal static UnitExtendedRight Get(in LDAPEntriesMedia entries, in Guid value, in string configuration)
        {
            // 是空的 GUID
            if (value.Equals(Guid.Empty))
            {
                // 空 GUID
                return null;
            }

            // 新建立藍本入口物件
            using (DirectoryEntry extendedRight = entries.ByDistinguisedName($"{CONTEXT_EXTENDEDRIGHT},{configuration}"))
            {
                // 輸出成 GUID 格式字串
                string valueGUID = value.ToString("D");
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"({ATTRIBUTE_EXTENDEDRIGHT_GUID}={valueGUID})";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(extendedRight, filiter, new string[] { LDAPAttributes.C_DISTINGGUISHEDNAME }))
                {
                    // 取得指定物件
                    SearchResult one = searcher.FindOne();
                    // 簡易防呆
                    if (one == null)
                    {
                        // 無法找到資料交由外部判斷是否錯誤
                        return null;
                    }

                    // 轉換成入口物件
                    using (DirectoryEntry objectEntry = one.GetDirectoryEntry())
                    {
                        // 對外提供預計對外提供的資料
                        return new UnitExtendedRight(objectEntry.Properties);
                    }
                }
            }
        }

        /// <summary>
        /// 取得藍本的 GUID
        /// </summary>
        /// <param name="entries">入口物件製作器</param>
        /// <param name="value">展示名稱</param>
        /// <param name="configuration">設定位置</param>
        /// <returns>額外權限結構</returns>
        internal static UnitExtendedRight Get(in LDAPEntriesMedia entries, in string value, in string configuration)
        {
            // 是空的字串
            if (string.IsNullOrWhiteSpace(value))
            {
                // 返回空字串
                return null;
            }

            // 新建立藍本入口物件
            using (DirectoryEntry entry = entries.ByDistinguisedName($"{CONTEXT_EXTENDEDRIGHT},{configuration}"))
            {
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"({ATTRIBUTE_EXTENDEDRIGHT_PROPERTY}={value})";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(entry, filiter, new string[] { LDAPAttributes.C_DISTINGGUISHEDNAME }))
                {
                    // 取得指定物件
                    SearchResult one = searcher.FindOne();
                    // 簡易防呆
                    if (one == null)
                    {
                        // 無法找到資料交由外部判斷是否錯誤
                        return null;
                    }

                    // 轉換成入口物件
                    using (DirectoryEntry objectEntry = one.GetDirectoryEntry())
                    {
                        // 對外提供預計對外提供的資料
                        return new UnitExtendedRight(objectEntry.Properties);
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
        internal readonly string RightsGUID;

        /// <summary>
        /// 實作額外權限結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        internal UnitExtendedRight(in PropertyCollection properties)
        {
            Name       = LDAPAttributes.ParseSingleValue<string>(ATTRIBUTE_EXTENDEDRIGHT_PROPERTY, properties);
            // 這個 GUID 使用字串儲存
            RightsGUID = LDAPAttributes.ParseSingleValue<string>(ATTRIBUTE_EXTENDEDRIGHT_GUID, properties);
        }
    }
}
