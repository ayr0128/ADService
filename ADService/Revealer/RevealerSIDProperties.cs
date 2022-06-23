using ADService.Environments;
using System.DirectoryServices;
using System.Security.Principal;

namespace ADService.Revealer
{
    /// <summary>
    /// 屬性數值儲存與樂度器
    /// </summary>
    internal sealed class RevealerSIDProperties : RevealerProperties
    {
        /// <summary>
        /// 建構解析特性鍵值所需資料的集合
        /// </summary>
        /// <param name="propertyName">解析的目標鍵值名稱</param>
        /// <param name="isForceExist">是否強制設定需存在資料</param>
        internal RevealerSIDProperties(in string propertyName, in bool isForceExist) : base(propertyName, isForceExist) { }

        internal override object Parse(in PropertyCollection properties) => LDAPAttributes.ParseSID(PropertyName, IsForceExist, properties);
    }
}
