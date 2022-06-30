using ADService.Environments;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;
using System.Text;

namespace ADService.Media
{
    /// <summary>
    /// 此藍本物件相關資料
    /// </summary>
    internal abstract class UnitSchema
    {
        #region 查詢相關資料
        // 取得空的 SID
        protected static string EmptySID = new SecurityIdentifier(WellKnownSidType.NullSid, null).ToString();

        /// <summary>
        /// 藍本的 DN 組合字尾
        /// </summary>
        protected const string CONTEXT_SCHEMA = "CN=Schema";
        /// <summary>
        /// 藍本的 GUID 欄位名稱
        /// </summary>
        protected const string ATTRIBUTE_SCHEMA_GUID = "schemaIDGUID";
        /// <summary>
        /// 與額外權限連結的 GUID 欄位名稱
        /// </summary>
        protected const string ATTRIBUTE_SCHEMA_SECURITY_GUID = "attributeSecurityGUID";
        /// <summary>
        /// 藍本的搜尋目標
        /// </summary>
        protected const string ATTRIBUTE_SCHEMA_PROPERTY = "ldapDisplayName";
        /// <summary>
        /// 是否為屬性: 不是屬性就是類別
        /// </summary>
        protected const string ATTRIBUTE_SCHEMA_ATTRIBUTEID = "attributeID";
        /// <summary>
        /// 此藍本結構是否僅儲存一筆
        /// </summary>
        protected const string ATTRIBUTE_SCHEMA_IS_SINGLEVALUED = "isSingleValued";
        /// <summary>
        /// 搜尋時找尋的資料
        /// </summary>
        protected static readonly string[] PROPERTIES = new string[] {
            ATTRIBUTE_SCHEMA_PROPERTY,
            ATTRIBUTE_SCHEMA_GUID,
            ATTRIBUTE_SCHEMA_SECURITY_GUID,
            ATTRIBUTE_SCHEMA_ATTRIBUTEID,
            ATTRIBUTE_SCHEMA_IS_SINGLEVALUED,
        };

        /// <summary>
        /// 取得使用目標安全性 GUID 的藍本
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="securityGUID">使用此安全性群組關聯</param>
        /// <returns>藍本結構</returns>
        internal static UnitSchema[] GetWithSecurityGUID(in LDAPConfigurationDispatcher dispatcher, in Guid securityGUID)
        {
            // 藍本入口物件不存在
            using (DirectoryEntry entrySchema = dispatcher.ByDistinguisedName($"{CONTEXT_SCHEMA},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 使用文字串流來推入 GUID
                StringBuilder sb = new StringBuilder();
                // 遍歷位元組
                foreach (byte convertRequired in securityGUID.ToByteArray())
                {
                    // 轉化各位元組至十六進位
                    sb.Append($"\\{convertRequired:X2}");
                }
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"(|({ATTRIBUTE_SCHEMA_SECURITY_GUID}={sb})({ATTRIBUTE_SCHEMA_GUID}={sb}))";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(entrySchema, filiter, PROPERTIES))
                {
                    // 找到所有查詢
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 數量必定為找尋到的物件數量
                        List<UnitSchema> unitSchemas = new List<UnitSchema>(all.Count);
                        // 取得指定物件
                        foreach (SearchResult one in all)
                        {
                            // 簡易防呆: 不可能出現
                            if (one == null)
                            {
                                // 無法找到資料交由外部判斷是否錯誤
                                continue;
                            }

                            // 取得 屬性ID
                            string AttributeID = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_SCHEMA_ATTRIBUTEID, one.Properties);
                            // 對外提供的基底結構
                            UnitSchema unitSchema;
                            // 根據是否為屬性決定提辜結構
                            if (string.IsNullOrEmpty(AttributeID))
                            {
                                // 對外提供類別
                                unitSchema = new UnitSchemaClass(one.Properties);
                            }
                            else
                            {
                                // 對外提供屬性
                                unitSchema = new UnitSchemaAttribute(one.Properties, AttributeID);
                            }
                            // 對外提供描述名稱
                            unitSchemas.Add(unitSchema);
                        }
                        // 返回查詢到的資料
                        return unitSchemas.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// 取得藍本
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="convertedGUIDs">專換為成查詢用格式的 GUID</param>
        /// <returns>藍本結構</returns>
        internal static UnitSchema[] GetWithGUID(in LDAPConfigurationDispatcher dispatcher, in IEnumerable<string> convertedGUIDs)
        {
            // 藍本入口物件不存在
            using (DirectoryEntry entrySchema = dispatcher.ByDistinguisedName($"{CONTEXT_SCHEMA},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = LDAPConfiguration.GetORFiliter(ATTRIBUTE_SCHEMA_GUID, convertedGUIDs);
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(entrySchema, filiter, PROPERTIES))
                {
                    // 找到所有查詢
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 數量必定為找尋到的物件數量
                        List<UnitSchema> unitSchemas = new List<UnitSchema>(all.Count);
                        // 取得指定物件
                        foreach (SearchResult one in all)
                        {
                            // 簡易防呆: 不可能出現
                            if (one == null)
                            {
                                // 無法找到資料交由外部判斷是否錯誤
                                continue;
                            }

                            // 取得 屬性ID
                            string AttributeID = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_SCHEMA_ATTRIBUTEID, one.Properties);
                            // 對外提供的基底結構
                            UnitSchema unitSchema;
                            // 根據是否為屬性決定提辜結構
                            if (string.IsNullOrEmpty(AttributeID))
                            {
                                // 對外提供類別
                                unitSchema = new UnitSchemaClass(one.Properties);
                            }
                            else
                            {
                                // 對外提供屬性
                                unitSchema = new UnitSchemaAttribute(one.Properties, AttributeID);
                            }
                            // 對外提供描述名稱
                            unitSchemas.Add(unitSchema);
                        }
                        // 返回查詢到的資料
                        return unitSchemas.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// 取得藍本
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="attributeNames">展示名稱</param>
        /// <returns>attributeNames</returns>
        internal static UnitSchema[] GetWithName(in LDAPConfigurationDispatcher dispatcher, in IEnumerable<string> attributeNames)
        {
            // 新建立藍本入口物件
            using (DirectoryEntry entrySchema = dispatcher.ByDistinguisedName($"{CONTEXT_SCHEMA},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = LDAPConfiguration.GetORFiliter(ATTRIBUTE_SCHEMA_PROPERTY, attributeNames);
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(entrySchema, filiter, PROPERTIES))
                {
                    // 遍歷取得的所有項目
                    using (SearchResultCollection all = searcher.FindAll())
                    {
                        // 對外提供的項目
                        List<UnitSchema> unitSchemas = new List<UnitSchema>(all.Count);
                        // 取得指定物件
                        foreach (SearchResult one in all)
                        {
                            // 簡易防呆
                            if (one == null)
                            {
                                // 無法找到資料交由外部判斷是否錯誤
                                return null;
                            }


                            // 取得 屬性ID
                            string AttributeID = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_SCHEMA_ATTRIBUTEID, one.Properties);
                            // 對外提供的基底結構
                            UnitSchema unitSchema;
                            // 根據是否為屬性決定提辜結構
                            if (string.IsNullOrEmpty(AttributeID))
                            {
                                // 對外提供類別
                                unitSchema = new UnitSchemaClass(one.Properties);
                            }
                            else
                            {
                                // 對外提供屬性
                                unitSchema = new UnitSchemaAttribute(one.Properties, AttributeID);
                            }
                            // 對外提供描述名稱
                            unitSchemas.Add(unitSchema);
                        }
                        // 轉換成陣列對外圖供
                        return unitSchemas.ToArray();
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_SCHEMA_PROPERTY"> 展示名稱 </see> 取得的相關字串
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_SCHEMA_GUID"> GUID </see> 取得的相關字串
        /// </summary>
        internal readonly string SchemaGUID;

        /// <summary>
        /// 啟用時間
        /// </summary>
        private readonly DateTime EnableTime = DateTime.UtcNow;

        /// <summary>
        /// 是否已經超過保留時間
        /// </summary>
        /// <param name="duration"></param>
        /// <returns>是否過期</returns>
        internal bool IsExpired(in TimeSpan duration) => (DateTime.UtcNow - EnableTime) >= duration;

        /// <summary>
        /// 將搜尋需求的參數提供致陣列字典, 根據繼承類別不同會寫入不同的條件
        /// <list type="table|bullet">
        ///     <item> <term><see cref="UnitSchemaClass">物件類型</see></term> 寫入鍵值: <see cref="UnitExtendedRight.ATTRIBUTE_EXTENDEDRIGHT_APPLIESTO">可套用</see> 須包含此藍本的 GUID </item>
        ///     <item> <term><see cref="UnitSchemaAttribute">屬性類型</see></term> 寫入鍵值: <see cref="UnitExtendedRight.ATTRIBUTE_EXTENDEDRIGHT_GUID">GUID</see> 須等於藍本的安全 GUID </item>
        /// </list>
        /// </summary>
        /// <param name="dictionaryAttributeNameWithValues">將被寫入資料的字典</param>
        internal abstract void CombineFiliter(ref Dictionary<string, HashSet<string>> dictionaryAttributeNameWithValues);

        /// <summary>
        /// 檢查此額外權限是否為此藍本物件的屬性組別
        /// </summary>
        /// <param name="unitExtendedRight">額外權限</param>
        /// <returns>2對於目標額萬權限而言, 此藍本為何種用途</returns>
        internal abstract PropertytFlags GetPorpertyType(in UnitExtendedRight unitExtendedRight);

        /// <summary>
        /// 實作藍本結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        internal UnitSchema(in ResultPropertyCollection properties)
        {
            // 將名稱轉換成小寫
            Name = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_SCHEMA_PROPERTY, properties);
            SchemaGUID = LDAPConfiguration.ParseGUID(ATTRIBUTE_SCHEMA_GUID, properties);
        }
    }
}
