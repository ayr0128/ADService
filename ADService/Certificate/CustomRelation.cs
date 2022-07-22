using ADService.Protocol;
using System;
using System.Security.Principal;

namespace ADService.DynamicParse
{
    /// <summary>
    /// 所有物件都是以此物件為基礎進行構成
    /// </summary>
    internal class CustomSIDUnit : ADCustomUnit
    {
        /// <summary>
        /// 取得儲存的 SID 資續
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
    }
}
