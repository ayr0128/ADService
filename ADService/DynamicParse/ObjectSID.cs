using ADService.Protocol;
using System;
using System.Security.Principal;

namespace ADService.DynamicParse
{
    /// <summary>
    /// 預計僅撈取 SID 的結構
    /// </summary>
    internal class ObjectSID
    {
        /// <summary>
        /// 將提供的 SID 轉換為序廖浩
        /// </summary>
        /// <param name="identityReference">目標安全辨識序列馬</param>
        /// <returns>安全性序列碼編號</returns>
        internal static string ToSID(in IdentityReference identityReference) => identityReference.Translate(typeof(SecurityIdentifier)).ToString();

        /// <summary>
        /// 取得儲存的 SID 資續
        /// </summary>
        internal string TranslateSID => ToSID(SecurityIdentifier);

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
