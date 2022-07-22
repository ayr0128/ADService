using ADService.Protocol;
using System;

namespace ADService.DynamicParse
{
    /// <summary>
    /// 所有物件都是以此物件為基礎進行構成
    /// </summary>
    internal class CustomGUIDUnit : ADCustomUnit
    {
        /// <summary>
        /// 物件藍本 SID
        /// </summary>
        internal Guid GUID => new Guid(GUIDInBytes);

        /// <summary>
        /// 從資料取得的藍本 SID
        /// </summary>
        [ADDescriptionProperty(Properties.C_OBJECTGUID)]
        private Byte[] GUIDInBytes { get; set; }
    }
}
