﻿using System;
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
        /// 搜尋時找尋的資料
        /// </summary>
        private static string[] PROPERTIES = new string[] {
            ATTRIBUTE_EXTENDEDRIGHT_GUID,
            ATTRIBUTE_EXTENDEDRIGHT_PROPERTY,
        };

        /// <summary>
        /// 取得擴展權限
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="value">目標 GUID</param>
        /// <returns>額外權限結構</returns>
        internal static UnitExtendedRight Get(in LDAPConfigurationDispatcher dispatcher, in Guid value)
        {
            // 是空的 GUID
            if (LDAPConfiguration.IsGUIDEmpty(value))
            {
                // 空 GUID
                return null;
            }

            // 新建立藍本入口物件
            using (DirectoryEntry extendedRight = dispatcher.ByDistinguisedName($"{CONTEXT_EXTENDEDRIGHT},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 輸出成 GUID 格式字串
                string valueGUID = value.ToString("D");
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"({ATTRIBUTE_EXTENDEDRIGHT_GUID}={valueGUID})";
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

                    // 對外提供預計對外提供的資料
                    return new UnitExtendedRight(one.Properties);
                }
            }
        }

        /// <summary>
        /// 透過額外全縣
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="value">展示名稱</param>
        /// <returns>額外權限結構</returns>
        internal static UnitExtendedRight Get(in LDAPConfigurationDispatcher dispatcher, in string value)
        {
            // 是空的字串
            if (string.IsNullOrWhiteSpace(value))
            {
                // 返回空字串
                return null;
            }

            // 新建立藍本入口物件
            using (DirectoryEntry entry = dispatcher.ByDistinguisedName($"{CONTEXT_EXTENDEDRIGHT},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"({ATTRIBUTE_EXTENDEDRIGHT_PROPERTY}={value})";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(entry, filiter, PROPERTIES))
                {
                    // 取得指定物件
                    SearchResult one = searcher.FindOne();
                    // 簡易防呆
                    if (one == null)
                    {
                        // 無法找到資料交由外部判斷是否錯誤
                        return null;
                    }

                    // 對外提供預計對外提供的資料
                    return new UnitExtendedRight(one.Properties);
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
        /// 啟用時間
        /// </summary>
        internal DateTime EnableTime;

        /// <summary>
        /// 實作額外權限結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        internal UnitExtendedRight(in ResultPropertyCollection properties)
        {
            Name = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_EXTENDEDRIGHT_PROPERTY, properties).ToLower();
            RightsGUID = LDAPConfiguration.ParseSingleValue<string>(ATTRIBUTE_EXTENDEDRIGHT_GUID, properties).ToLower();

            EnableTime = DateTime.UtcNow;
        }
    }
}