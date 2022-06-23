using ADService.Environments;
using ADService.Media;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;

namespace ADService.Permissions
{
    /// <summary>
    /// 此藍本物件相關資料
    /// </summary>
    internal sealed class SchemaUnit
    {
        /// <summary>
        /// 藍本的描述名稱
        /// </summary>
        internal string Name;
        /// <summary>
        /// 藍本的 GUID
        /// </summary>
        internal string GUID;
        /// <summary>
        /// 是否從額外權限取得
        /// </summary>
        internal bool IsExtendRight;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="name">藍本的描述名稱</param>
        /// <param name="guid">藍本的 GUID</param>
        /// <param name="isExtendRight">是否從額外權限取得</param>
        internal SchemaUnit(string name, string guid, bool isExtendRight)
        {
            Name = name;
            GUID = guid;
            IsExtendRight = isExtendRight;
        }
    }

    /// <summary>
    /// 內部使用的存取權限轉換器
    /// </summary>
    internal sealed class LDAPSchema : IDisposable
    {
        #region 固定與取值參數
        /// <summary>
        /// 設定入口物件位置
        /// </summary>
        private const string CONTEXT_CONFIGURATION = "configurationNamingContext";
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
        /// 取得指定 GUID 的群組設定對照
        /// </summary>
        /// <param name="entries">入口物件製作器</param>
        /// <param name="value">目標 GUID</param>
        /// <returns>名稱 HashSet</returns>
        internal HashSet<string> GetPropertiesSetSchemaName(in LDAPEntriesMedia entries, in Guid value)
        {
            // 藍本入口物件不存在
            if (entrySchema == null)
            {
                // 新建立藍本入口物件
                entrySchema = entries.ByDistinguisedName($"{CONTEXT_SCHEMA},{ConfigurationDistinguishedName}");
            }

            // 使用文字串流
            StringBuilder sb = new StringBuilder();
            // 遍歷位元組
            foreach (byte convertRequired in value.ToByteArray())
            {
                // 轉化各位元組至十六進位
                sb.Append($"\\{convertRequired:X2}");
            }

            // 需使用加密避免 LDAP 注入式攻擊
            string filiter = $"({ATTRIBUTE_SCHEMA_SECURITY_GUID}={sb})";
            // 從入口物件中找尋到指定物件
            using (DirectorySearcher searcher = new DirectorySearcher(entrySchema, filiter, new string[] { ATTRIBUTE_SCHEMA_PROPERTY }))
            {
                // 取得指定物件
                using (SearchResultCollection all = searcher.FindAll())
                {
                    // 指定的所有參數都應該能在藍本中被找到
                    HashSet<string> displayNameHashSet = new HashSet<string>(all.Count);
                    // 遍歷所有取得的資料
                    foreach (SearchResult one in all)
                    {
                        // 展示名稱, 應該被找到
                        displayNameHashSet.Add(LDAPAttributes.ParseSingleValue<string>(ATTRIBUTE_SCHEMA_PROPERTY, true, one.Properties));
                    }

                    // 提供給外部對應資料
                    return displayNameHashSet;
                }
            }
        }
        #endregion

        /// <summary>
        /// 設定區分名稱
        /// </summary>
        private readonly string ConfigurationDistinguishedName;

        /// <summary>
        /// 暫存 Schema 入口物件: 如果以取得則須於 IDisposable 中釋放
        /// </summary>
        private DirectoryEntry entrySchema = null;
        /// <summary>
        /// 暫存 Configuration 入口物件: 如果以取得則須於 IDisposable 中釋放
        /// </summary>
        private DirectoryEntry entrExtendedRight = null;

        /// <summary>
        /// 取得 DSE 中的設定區分名稱位置, 並建構連線用相關暫存
        /// </summary>
        /// <param name="entries">入口物件製作器</param>
        internal LDAPSchema(in LDAPEntriesMedia entries)
        {
            // 取得設定位置
            using (DirectoryEntry root = entries.DSERoot())
            {
                // 取得內部設定位置
                ConfigurationDistinguishedName = LDAPAttributes.ParseSingleValue<string>(CONTEXT_CONFIGURATION, true, root.Properties);
            }
        }

        /// <summary>
        /// 使用 GUID 作為鍵職儲存藍本元件, 格式如右: Dictionary 'GUID, 藍本元件'
        /// </summary>
        private readonly Dictionary<string, SchemaUnit> dictionaryGUIDWithSchemaUnit = new Dictionary<string, SchemaUnit>();

        #region 使用 GUID 取得藍本物件
        /// <summary>
        /// 使用 GUID 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="entries">入口物件製作器</param>
        /// <param name="value">目標 GUID </param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal SchemaUnit Get(in LDAPEntriesMedia entries, in Guid value)
        {
            // 取得指定物件 GUID
            string attributeGUID = value == null ? string.Empty : value.ToString("D");
            // 嘗試找尋指定物件
            if (!dictionaryGUIDWithSchemaUnit.TryGetValue(attributeGUID, out SchemaUnit unit) && !Guid.Empty.Equals(value) && !string.IsNullOrEmpty(attributeGUID))
            {
                // 優先於擴展權限中檢查
                string attributeName = GetPropertyExtendedRightName(entries, value);
                /* 目前指定影響參數為空, 有下述可能性導致發生
                     - 存取規則中不含有擴展權限旗標且未於拓展權限中發現
                */
                if (!string.IsNullOrEmpty(attributeName))
                {
                    // 從額外權限取得
                    unit = new SchemaUnit(attributeName, attributeGUID, true);
                }
                else
                {
                    // 於藍本中找尋指定目標
                    attributeName = GetPropertySchemaName(entries, value);
                    // 從藍本中取得
                    unit = new SchemaUnit(attributeName, attributeGUID, false);
                }

                // 加入 GUID 字典
                dictionaryGUIDWithSchemaUnit.Add(attributeGUID, unit);
                // 加入 展示名稱 字典
                dictionaryDisplayNameWithSchemaUnit.Add(attributeName, unit);
            }
            // 對外提供取得的資料: 注意可能為空
            return unit;
        }

        /// <summary>
        /// 取得藍本的指定欄位名稱
        /// </summary>
        /// <param name="entries">入口物件製作器</param>
        /// <param name="value">目標 GUID</param>
        /// <returns>此 GUID 指定的藍本指定欄位名稱</returns>
        internal string GetPropertySchemaName(in LDAPEntriesMedia entries, in Guid value)
        {
            // 藍本入口物件不存在
            if (entrySchema == null)
            {
                // 新建立藍本入口物件
                entrySchema = entries.ByDistinguisedName($"{CONTEXT_SCHEMA},{ConfigurationDistinguishedName}");
            }

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
            using (DirectorySearcher searcher = new DirectorySearcher(entrySchema, filiter, new string[] { ATTRIBUTE_SCHEMA_PROPERTY }))
            {
                // 取得指定物件
                SearchResult one = searcher.FindOne();
                // 簡易防呆
                if (one == null)
                {
                    // 拋出例外: 如果程式正確不應技進入此處
                    throw new LDAPExceptions($"於藍本中嘗試取得指定物件:{value} 時但物件不存在", ErrorCodes.LOGIC_ERROR);
                }

                // 取得影響的指定參數: 這邊指定參數取得的內容可以與英文版的 Windows Server AD 服務上互相對照
                return LDAPAttributes.ParseSingleValue<string>(ATTRIBUTE_SCHEMA_PROPERTY, true, one.Properties);
            }
        }

        /// <summary>
        /// 取得擴展權限的指定欄位名稱
        /// </summary>
        /// <param name="entries">入口物件製作器</param>
        /// <param name="value">目標 GUID</param>
        /// <returns>此 GUID 指定的拓展權限指定欄位名稱</returns>
        private string GetPropertyExtendedRightName(in LDAPEntriesMedia entries, in Guid value)
        {
            // 是空的 GUID
            if (value.Equals(Guid.Empty))
            {
                // 返回空字串
                return string.Empty;
            }

            // 藍本入口物件不存在
            if (entrExtendedRight == null)
            {
                // 新建立藍本入口物件
                entrExtendedRight = entries.ByDistinguisedName($"{CONTEXT_EXTENDEDRIGHT},{ConfigurationDistinguishedName}");
            }

            // 需使用加密避免 LDAP 注入式攻擊
            string filiter = $"({ATTRIBUTE_EXTENDEDRIGHT_GUID}={value:D})";
            // 從入口物件中找尋到指定物件
            using (DirectorySearcher searcher = new DirectorySearcher(entrExtendedRight, filiter, new string[] { ATTRIBUTE_EXTENDEDRIGHT_PROPERTY }))
            {
                // 取得指定物件
                SearchResult one = searcher.FindOne();
                // 簡易防呆
                if (one == null)
                {
                    // 額外權限內不一定存在目標
                    return string.Empty;
                }

                // 取得影響的指定參數: 這邊指定參數取得的內容可以與英文版的 Windows Server AD 服務上互相對照
                return LDAPAttributes.ParseSingleValue<string>(ATTRIBUTE_EXTENDEDRIGHT_PROPERTY, true, one.Properties);
            }
        }
        #endregion

        /// <summary>
        /// 使用 展示名稱 作為鍵職儲存藍本元件, 格式如右: Dictionary '展示名稱, 藍本元件'
        /// </summary>
        private readonly Dictionary<string, SchemaUnit> dictionaryDisplayNameWithSchemaUnit = new Dictionary<string, SchemaUnit>();

        #region 使用展示名稱取得藍本物件
        /// <summary>
        /// 使用 GUID 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="entries">入口物件製作器</param>
        /// <param name="value">展示名稱</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal SchemaUnit Get(in LDAPEntriesMedia entries, in string value)
        {
            // 嘗試找尋指定物件
            if (!dictionaryDisplayNameWithSchemaUnit.TryGetValue(value, out SchemaUnit unit) && !string.IsNullOrEmpty(value))
            {
                // 優先於擴展權限中檢查
                string attributeGUID = GetPropertyExtendedRightName(entries, value);
                /* 目前指定影響參數為空, 有下述可能性導致發生
                     - 存取規則中不含有擴展權限旗標且未於拓展權限中發現
                */
                if (!string.IsNullOrEmpty(attributeGUID))
                {
                    // 從額外權限取得
                    unit = new SchemaUnit(value, attributeGUID, true);
                }
                else
                {
                    // 於藍本中找尋指定目標
                    attributeGUID = GetPropertySchemaName(entries, value);
                    // 從藍本中取得
                    unit = new SchemaUnit(value, attributeGUID, false);
                }

                // 加入 GUID 字典
                dictionaryGUIDWithSchemaUnit.Add(attributeGUID, unit);
                // 加入 展示名稱 字典
                dictionaryDisplayNameWithSchemaUnit.Add(value, unit);
            }
            // 對外提供取得的資料: 注意可能為空
            return unit;
        }

        /// <summary>
        /// 取得藍本的 GUID
        /// </summary>
        /// <param name="entries">入口物件製作器</param>
        /// <param name="value">展示名稱</param>
        /// <returns>此 GUID 指定的藍本指定欄位名稱</returns>
        internal string GetPropertySchemaName(in LDAPEntriesMedia entries, in string value)
        {
            // 藍本入口物件不存在
            if (entrySchema == null)
            {
                // 新建立藍本入口物件
                entrySchema = entries.ByDistinguisedName($"{CONTEXT_SCHEMA},{ConfigurationDistinguishedName}");
            }

            // 需使用加密避免 LDAP 注入式攻擊
            string filiter = $"({ATTRIBUTE_SCHEMA_PROPERTY}={value})";
            // 從入口物件中找尋到指定物件
            using (DirectorySearcher searcher = new DirectorySearcher(entrySchema, filiter, new string[] { ATTRIBUTE_SCHEMA_GUID }))
            {
                // 取得指定物件
                SearchResult one = searcher.FindOne();
                // 簡易防呆
                if (one == null)
                {
                    // 拋出例外: 如果程式正確不應技進入此處
                    throw new LDAPExceptions($"於藍本中嘗試取得指定物件:{value} 時但物件不存在", ErrorCodes.LOGIC_ERROR);
                }

                // 取得影響的指定參數: 這邊指定參數取得的內容可以與英文版的 Windows Server AD 服務上互相對照
                return LDAPAttributes.ParseGUID(ATTRIBUTE_SCHEMA_GUID, true, one.Properties);
            }
        }

        /// <summary>
        /// 取得藍本的 GUID
        /// </summary>
        /// <param name="entries">入口物件製作器</param>
        /// <param name="value">展示名稱</param>
        /// <returns>此 GUID 指定的拓展權限指定欄位名稱</returns>
        private string GetPropertyExtendedRightName(in LDAPEntriesMedia entries, in string value)
        {
            // 是空的 GUID
            if (value.Equals(Guid.Empty))
            {
                // 返回空字串
                return string.Empty;
            }

            // 藍本入口物件不存在
            if (entrExtendedRight == null)
            {
                // 新建立藍本入口物件
                entrExtendedRight = entries.ByDistinguisedName($"{CONTEXT_EXTENDEDRIGHT},{ConfigurationDistinguishedName}");
            }

            // 需使用加密避免 LDAP 注入式攻擊
            string filiter = $"({ATTRIBUTE_EXTENDEDRIGHT_PROPERTY}={value})";
            // 從入口物件中找尋到指定物件
            using (DirectorySearcher searcher = new DirectorySearcher(entrExtendedRight, filiter, new string[] { ATTRIBUTE_EXTENDEDRIGHT_GUID }))
            {
                // 取得指定物件
                SearchResult one = searcher.FindOne();
                // 簡易防呆
                if (one == null)
                {
                    // 額外權限內不一定存在目標
                    return string.Empty;
                }

                // 取得影響的指定參數: 這邊指定參數取得的內容可以與英文版的 Windows Server AD 服務上互相對照
                return LDAPAttributes.ParseSingleValue<string>(ATTRIBUTE_EXTENDEDRIGHT_GUID, true, one.Properties);
            }
        }
        #endregion

        void IDisposable.Dispose()
        {
            // 釋放 Schema 入口物件
            entrySchema?.Dispose();
            // 釋放 Configuration 入口物件
            entrExtendedRight?.Dispose();
        }
    }
}
