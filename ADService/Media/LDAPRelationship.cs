using ADService.Environments;
using ADService.Protocol;
using System;
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
        [Obsolete("即將棄用, 請改用 DriveClassName 判斷物件類型")]
        public CategoryTypes Type => LDAPCategory.GetCategoryTypes(DriveClassName);
        /// <summary>
        /// 容器類型
        /// </summary>
        public string DriveClassName { get; private set; }
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
        /// <param name="driveClassName">此物件的類型</param>
        public LDAPRelationship(in DirectoryEntry entry, in bool isPrimary, in string driveClassName)
        {
            DistinguishedName = LDAPConfiguration.ParseSingleValue<string>(Properties.C_DISTINGUISHEDNAME, entry.Properties);
            Name = LDAPConfiguration.ParseSingleValue<string>(Properties.P_NAME, entry.Properties);

            SID = LDAPConfiguration.ParseSID(Properties.C_OBJECTSID, entry.Properties);
            GUID = LDAPConfiguration.ParseGUID(Properties.C_OBJECTGUID, entry.Properties);

            IsPrimary = isPrimary;
            DriveClassName = driveClassName;
        }
    }
}
