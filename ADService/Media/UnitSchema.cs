using ADService.Environments;
using ADService.Protocol;
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
        /// <summary>
        /// 藍本的 DN 組合字尾
        /// </summary>
        protected const string CONTEXT_SCHEMA = "CN=Schema";
        /// <summary>
        /// 藍本的搜尋目標
        /// </summary>
        protected const string SCHEMA_PROPERTY = "ldapDisplayName";
        /// <summary>
        /// 藍本的 GUID 欄位名稱
        /// </summary>
        protected const string SCHEMA_GUID = "schemaIDGUID";
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
        /// 實作藍本結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        internal UnitSchema(in ResultPropertyCollection properties)
        {
            // 將名稱轉換成小寫
            Name = LDAPConfiguration.ParseSingleValue<string>(SCHEMA_PROPERTY, properties);
            SchemaGUID = LDAPConfiguration.ParseGUID(SCHEMA_GUID, properties);
        }
    }
}
