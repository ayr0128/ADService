﻿using System;
using System.DirectoryServices;
using System.Text;

namespace ADService.Media
{
    /// <summary>
    /// 此藍本物件相關資料
    /// </summary>
    internal sealed class UnitSchema
    {
        #region 查詢相關資料
        /// <summary>
        /// 藍本的 DN 組合字尾
        /// </summary>
        private const string CONTEXT_SCHEMA = "CN=Schema";
        /// <summary>
        /// 藍本的 GUID 欄位名稱
        /// </summary>
        private const string ATTRIBUTE_SCHEMA_GUID = "schemaIDGUID";
        /// <summary>
        /// 與額外權限連結的 GUID 欄位名稱
        /// </summary>
        private const string ATTRIBUTE_SCHEMA_SECURITY_GUID = "attributeSecurityGUID";
        /// <summary>
        /// 藍本的搜尋目標
        /// </summary>
        private const string ATTRIBUTE_SCHEMA_PROPERTY = "ldapDisplayName";
        /// <summary>
        /// 此藍本結構是否僅儲存一筆
        /// </summary>
        private const string ATTRIBUTE_SCHEMA_IS_SINGLEVALUED = "isSingleValued";
        /// <summary>
        /// 搜尋時找尋的資料
        /// </summary>
        private static string[] PROPERTIES = new string[] {
            ATTRIBUTE_SCHEMA_PROPERTY,
            ATTRIBUTE_SCHEMA_GUID,
            ATTRIBUTE_SCHEMA_SECURITY_GUID,
            ATTRIBUTE_SCHEMA_IS_SINGLEVALUED,
        };

        /// <summary>
        /// 取得藍本
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="value">目標 GUID</param>
        /// <returns>藍本結構</returns>
        internal static UnitSchema Get(in LDAPConfigurationDispatcher dispatcher, in Guid value)
        {
            // 藍本入口物件不存在
            using (DirectoryEntry entrySchema = dispatcher.ByDistinguisedName($"{CONTEXT_SCHEMA},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 使用文字串流
                StringBuilder sb = new StringBuilder();
                // 遍歷位元組
                foreach (byte convertRequired in value.ToByteArray())
                {
                    // 轉化各位元組至十六進位
                    sb.Append($"\\{convertRequired:X2}");
                }

                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"({ATTRIBUTE_SCHEMA_GUID}={sb})";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(entrySchema, filiter, PROPERTIES))
                {
                    // 取得指定物件
                    SearchResult one = searcher.FindOne();
                    // 簡易防呆
                    if (one == null)
                    {
                        // 無法找到資料交由外部判斷是否錯誤
                        return null;
                    }

                    // 對外提供描述名稱
                    return new UnitSchema(one.Properties);
                }
            }
        }

        /// <summary>
        /// 取得藍本
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="value">展示名稱</param>
        /// <returns>藍本結構</returns>
        internal static UnitSchema Get(in LDAPConfigurationDispatcher dispatcher, in string value)
        {
            // 新建立藍本入口物件
            using (DirectoryEntry entrySchema = dispatcher.ByDistinguisedName($"{CONTEXT_SCHEMA},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"({ATTRIBUTE_SCHEMA_PROPERTY}={value})";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(entrySchema, filiter, PROPERTIES))
                {
                    // 取得指定物件
                    SearchResult one = searcher.FindOne();
                    // 簡易防呆
                    if (one == null)
                    {
                        // 無法找到資料交由外部判斷是否錯誤
                        return null;
                    }

                    // 對外提供描述名稱
                    return new UnitSchema(one.Properties);
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
        /// 使用欄位 <see cref="ATTRIBUTE_SCHEMA_SECURITY_GUID"> GUID </see> 取得的相關字串
        /// </summary>
        internal readonly string SecurityGUID;

        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_SCHEMA_IS_SINGLEVALUED"> 是否一筆 </see> 取得的相關字串
        /// </summary>
        internal readonly bool IsSingleValued;

        /// <summary>
        /// 啟用時間
        /// </summary>
        internal DateTime EnableTime;

        /// <summary>
        /// 實作藍本結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        internal UnitSchema(in ResultPropertyCollection properties)
        {
            // 將名稱轉換成小寫
            Name = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_SCHEMA_PROPERTY, properties).ToLower();

            // 將 GUID 轉換成小寫
            SchemaGUID = LDAPConfiguration.ParseGUID(ATTRIBUTE_SCHEMA_GUID, properties).ToLower();
            SecurityGUID = LDAPConfiguration.ParseGUID(ATTRIBUTE_SCHEMA_SECURITY_GUID, properties).ToLower();

            IsSingleValued = LDAPConfiguration.ParseSingleValue<bool>(ATTRIBUTE_SCHEMA_IS_SINGLEVALUED, properties);

            EnableTime = DateTime.UtcNow;
        }
    }
}