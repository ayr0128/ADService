using ADService.Environments;
using ADService.Protocol;
using System.DirectoryServices;

namespace ADService.Media
{
    /// <summary>
    /// 專門用於描述隸屬成員的物件
    /// </summary>
    public sealed class LDAPRelationship
    {
        /// <summary>
        /// 此物件的名稱
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 此物件的區分名稱
        /// </summary>
        public string DistinguishedName { get; private set; }
        /// <summary>
        /// 此物件的全域唯一標識符
        /// </summary>
        public string GUID { get; private set; }
        /// <summary>
        /// 容器類型
        /// </summary>
        public CategoryTypes Type { get; private set; }
        /// <summary>
        /// 此物件的 SID 資料
        /// </summary>
        public string SID { get; private set; }
        /// <summary>
        /// 是否是主要隸屬關連而來
        /// </summary>
        public bool IsPrimary { get; private set; }

        /// <summary>
        /// 使用鍵值參數初始化
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="isPrimary">是否為主要關聯物件</param>
        /// <exception cref="LDAPExceptions">解析鍵值不符合規則時對外丟出</exception>
        public LDAPRelationship(in DirectoryEntry entry, in bool isPrimary)
        {
            DistinguishedName = LDAPEntries.ParseSingleValue<string>(Properties.C_DISTINGGUISHEDNAME, entry.Properties);
            Name              = LDAPEntries.ParseSingleValue<string>(Properties.P_NAME, entry.Properties);

            Type = LDAPEntries.ParseCategory(entry.Properties);
            SID  = LDAPEntries.ParseSID(Properties.C_OBJECTSID, entry.Properties);
            GUID = LDAPEntries.ParseGUID(Properties.C_OBJECTGUID, entry.Properties);

            // 紀錄是否從主要關聯物件而來
            IsPrimary = isPrimary;
        }
    }
}
