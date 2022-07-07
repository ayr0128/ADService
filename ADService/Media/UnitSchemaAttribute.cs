using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;

namespace ADService.Media
{
    /// <summary>
    /// 此藍本物件相關資料
    /// </summary>
    internal sealed class UnitSchemaAttribute : UnitSchema
    {
        /// <summary>
        /// 藍本的搜尋目標
        /// </summary>
        internal const string SCHEMA_ATTRIBUTE = "attributeSchema";
        
        /// <summary>
        /// 與額外權限連結的 GUID 欄位名稱
        /// </summary>
        private const string SCHEMA_ATTRIBUTE_SECURITYGUID = "attributeSecurityGUID";
        /// <summary>
        /// 此藍本結構是否僅儲存一筆
        /// </summary>
        private const string SCHEMA_ATTRIBUTE_ISSINGLEVALUED = "isSingleValued";

        /// <summary>
        /// 取得使用目標安全性 GUID 的藍本
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="unitControlAccessGUID">存取權限的 GUID</param>
        /// <returns>藍本結構</returns>
        internal static UnitSchemaAttribute[] GetWithControlAccessGUID(in LDAPConfigurationDispatcher dispatcher, in Guid unitControlAccessGUID)
        {
            // 藍本入口物件不存在
            using (DirectoryEntry entrySchema = dispatcher.ByDistinguisedName($"{CONTEXT_SCHEMA},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 限制找尋的物件類型應為物件類型
                string subSearchCategory = LDAPConfiguration.GetORFiliter(Properties.C_OBJECTCATEGORY, SCHEMA_ATTRIBUTE);
                // 使用文字串流來推入 GUID
                StringBuilder sb = new StringBuilder();
                // 遍歷位元組
                foreach (byte convertRequired in unitControlAccessGUID.ToByteArray())
                {
                    // 轉化各位元組至十六進位
                    sb.Append($"\\{convertRequired:X2}");
                }
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"(&(|({SCHEMA_GUID}={sb})({SCHEMA_ATTRIBUTE_SECURITYGUID}={sb}))({subSearchCategory}))";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(entrySchema, filiter, BASE_PROPERTIES))
                {
                    // 找到所有查詢
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 數量必定為找尋到的物件數量
                        List<UnitSchemaAttribute> unitSchemaAttributes = new List<UnitSchemaAttribute>(all.Count);
                        // 取得指定物件
                        foreach (SearchResult one in all)
                        {
                            // 簡易防呆: 不可能出現
                            if (one == null)
                            {
                                // 無法找到資料交由外部判斷是否錯誤
                                continue;
                            }

                            // 轉換成入口物件
                            using (DirectoryEntry entry = one.GetDirectoryEntry())
                            {
                                // 對外提供的基底結構
                                UnitSchemaAttribute unitSchemaAttribute = new UnitSchemaAttribute(entry.Properties);
                                // 對外提供描述名稱
                                unitSchemaAttributes.Add(unitSchemaAttribute);
                            }
                        }
                        // 返回查詢到的資料
                        return unitSchemaAttributes.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// 取得藍本
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="lDAPDisplayNames">展示名稱</param>
        /// <returns>attributeNames</returns>
        internal static UnitSchemaAttribute[] GetWithLDAPDisplayNames(in LDAPConfigurationDispatcher dispatcher, in IEnumerable<string> lDAPDisplayNames)
        {
            // 新建立藍本入口物件
            using (DirectoryEntry entrySchema = dispatcher.ByDistinguisedName($"{CONTEXT_SCHEMA},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = LDAPConfiguration.GetORFiliter(SCHEMA_PROPERTY, lDAPDisplayNames);
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(entrySchema, filiter, BASE_PROPERTIES))
                {
                    // 遍歷取得的所有項目
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 對外提供的項目
                        List<UnitSchemaAttribute> unitSchemaAttributes = new List<UnitSchemaAttribute>(all.Count);
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
                                UnitSchemaAttribute unitSchemaAttribute = new UnitSchemaAttribute(entry.Properties);
                                // 對外提供描述名稱
                                unitSchemaAttributes.Add(unitSchemaAttribute);
                            }
                        }
                        // 轉換成陣列對外圖供
                        return unitSchemaAttributes.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// 查看提供的存取權限 GUID 是否為群組組合
        /// </summary>
        /// <param name="unitControlAccessGUIDLower">存取權限 GUID, 記得轉小寫</param>
        /// <returns>是否為群組設定</returns>
        internal bool IsPropertySet(in string unitControlAccessGUIDLower) => unitControlAccessGUIDLower == SecurityGUID.ToLower();

        /// <summary>
        /// 使用欄位 <see cref="SCHEMA_ATTRIBUTE_ISSINGLEVALUED"> 是否一筆 </see> 取得的相關字串
        /// </summary>
        internal readonly bool IsSingleValued;
        /// <summary>
        /// 使用欄位 <see cref="SCHEMA_ATTRIBUTE_SECURITYGUID"> GUID </see> 取得的相關字串
        /// </summary>
        internal readonly string SecurityGUID;

        /// <summary>
        /// 實作藍本結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        internal UnitSchemaAttribute(in PropertyCollection properties) : base(properties)
        {
            IsSingleValued = LDAPConfiguration.ParseSingleValue<bool>(SCHEMA_ATTRIBUTE_ISSINGLEVALUED, properties);
            SecurityGUID = LDAPConfiguration.ParseGUID(SCHEMA_ATTRIBUTE_SECURITYGUID, properties);
        }
    }
}
