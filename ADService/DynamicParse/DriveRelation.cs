using ADService.Environments;
using ADService.Protocol;
using System;
using System.Security.Principal;

namespace ADService.DynamicParse
{
    /// <summary>
    /// 關係獲取
    /// </summary>
    [ADDescriptionClass(LDAPCategory.CLASS_PERSON, LDAPCategory.CLASS_GROUP, LDAPCategory.CLASS_COMPUTER)]
    internal class DriveRelation : ADDrive
    {
        /// <summary>
        /// 取得隸屬於, 
        /// </summary>
        [ADDescriptionProperty(Properties.P_MEMBEROF)]
        internal string[] MemberOf { get; private set; }

        /// <summary>
        /// 主要隸屬尋組 
        /// </summary>
        [ADDescriptionProperty(Properties.C_PRIMARYGROUPID), ADDescriptionClass(LDAPCategory.CLASS_PERSON, LDAPCategory.CLASS_COMPUTER)]
        internal int PrimaryGroupID { get; private set; }

        /// <summary>
        /// 取得成員, 只有群組持有
        /// </summary>
        [ADDescriptionProperty(Properties.P_MEMBER), ADDescriptionClass(LDAPCategory.CLASS_GROUP)]
        internal string[] Member { get; private set; }

        /// <summary>
        /// 物件藍本 SID 字串
        /// </summary>
        internal string SID => ObjectSID.ToSID(SecurityIdentifier);

        /// <summary>
        /// 物件藍本 SID
        /// </summary>
        internal SecurityIdentifier SecurityIdentifier => new SecurityIdentifier(SecurityIdentifierInBytes, 0);

        /// <summary>
        /// 從資料取得的藍本 SID
        /// </summary>
        [ADDescriptionProperty(Properties.C_OBJECTSID)]
        private Byte[] SecurityIdentifierInBytes { get; set; }

        /// <summary>
        /// 物件藍本 SID
        /// </summary>
        internal Guid GUID => new Guid(GUIDInBytes);

        /// <summary>
        /// 從資料取得的藍本 SID
        /// </summary>
        [ADDescriptionProperty(Properties.C_OBJECTGUID)]
        private Byte[] GUIDInBytes { get; set; }

        /// <summary>
        /// 取得物件與自身的關係
        /// </summary>
        /// <param name="customUnit">查詢的物件</param>
        internal DriveRelation(in ADCustomUnit customUnit) : base(customUnit) { }
    }
}
