using ADService.Environments;
using ADService.Protocol;
using System;
using System.DirectoryServices;
using System.Text;

namespace ADService.Media
{
    /// <summary>
    /// 此藍本物件相關資料
    /// </summary>
    internal abstract class UnitSchema
    {
        /// <summary>
        /// 針對屬性使用存取權限, 查看下述列表
        /// <list type="table">
        ///     <item> <see cref="ActiveDirectoryRights.WriteProperty">寫入屬性</see> </item>
        ///     <item> <see cref="ActiveDirectoryRights.ReadProperty">讀取屬性</see> </item>
        /// </list>
        /// </summary>
        internal const ActiveDirectoryRights VALIDACCESSES_ATTRIBUTE = ActiveDirectoryRights.WriteProperty | ActiveDirectoryRights.ReadProperty;
        /// <summary>
        /// 針對類別使用存取權限, 查看下述列表
        /// <list type="table">
        ///     <item> <see cref="ActiveDirectoryRights.CreateChild">創建子系物件</see> </item>
        ///     <item> <see cref="ActiveDirectoryRights.DeleteChild">刪除子系物件</see> </item>
        ///     <item> <see cref="ActiveDirectoryRights.ListChildren">陳列子系容器內的物件</see> </item>
        ///     <item> <see cref="ActiveDirectoryRights.DeleteTree">刪除術系</see> </item>
        ///     <item> <see cref="ActiveDirectoryRights.ListObject">陳列物件的子系物件</see> </item>
        ///     <item> <see cref="ActiveDirectoryRights.Delete">刪除本身</see> </item>
        /// </list>
        /// </summary>
        internal const ActiveDirectoryRights VALIDACCESSES_CLASS = ActiveDirectoryRights.CreateChild | ActiveDirectoryRights.DeleteChild | ActiveDirectoryRights.ListChildren | ActiveDirectoryRights.ListObject | ActiveDirectoryRights.DeleteTree | ActiveDirectoryRights.Delete;

        #region 查詢相關資料
        /// <summary>
        /// 藍本的搜尋目標
        /// </summary>
        internal const string SCHEMA_PROPERTY = "ldapDisplayName";
        /// <summary>
        /// 藍本的 DN 組合字尾
        /// </summary>
        protected const string CONTEXT_SCHEMA = "CN=Schema";
        /// <summary>
        /// 藍本的 GUID 欄位名稱
        /// </summary>
        protected const string SCHEMA_GUID = "schemaIDGUID";
        /// <summary>
        /// 搜尋時找尋的資料
        /// </summary>
        protected static readonly string[] BASE_PROPERTIES = new string[] {
            Properties.C_DISTINGUISHEDNAME,
        };

        /// <summary>
        /// 取得使用目標安全性 GUID 的藍本
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="unitSchemaAGUID">目標屬性的 GUID</param>
        /// <returns>藍本結構</returns>
        internal static SearchResult GetWithSchemaEntry(in LDAPConfigurationDispatcher dispatcher, in Guid unitSchemaAGUID)
        {
            // 藍本入口物件不存在
            using (DirectoryEntry entrySchema = dispatcher.ByDistinguisedName($"{CONTEXT_SCHEMA},{dispatcher.ConfigurationDistinguishedName}"))
            {
                // 使用文字串流來推入 GUID
                StringBuilder sb = new StringBuilder();
                // 遍歷位元組
                foreach (byte convertRequired in unitSchemaAGUID.ToByteArray())
                {
                    // 轉化各位元組至十六進位
                    sb.Append($"\\{convertRequired:X2}");
                }
                // 需使用加密避免 LDAP 注入式攻擊
                string filiter = $"({SCHEMA_GUID}={sb})";
                // 從入口物件中找尋到指定物件
                using (DirectorySearcher searcher = new DirectorySearcher(entrySchema, filiter, BASE_PROPERTIES))
                {
                    // 找到所有查詢
                    return searcher.FindOne();
                }
            }
        }
        #endregion

        /// <summary>
        /// 使用欄位 <see cref="CONTEXT_SCHEMA"> 展示名稱 </see> 取得的相關字串
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// 使用欄位 <see cref="SCHEMA_GUID"> GUID </see> 取得的相關字串
        /// </summary>
        internal readonly string SchemaGUID;

        /// <summary>
        /// 系統旗標
        /// </summary>
        private readonly SystemFlags systemFlags;

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
        /// 實作藍本結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        internal UnitSchema(in PropertyCollection properties)
        {
            Name = LDAPConfiguration.ParseSingleValue<string>(SCHEMA_PROPERTY, properties);
            SchemaGUID = LDAPConfiguration.ParseGUID(SCHEMA_GUID, properties);

            // 取得內部儲存的類型
            int storedSystemFlags = LDAPConfiguration.ParseSingleValue<int>(Properties.C_SYSTEMFLAGS, properties);
            // 強制轉型並取得系統旗標
            systemFlags = (SystemFlags)Enum.ToObject(typeof(SystemFlags), storedSystemFlags);
        }
    }
}
