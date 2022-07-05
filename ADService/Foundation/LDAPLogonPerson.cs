using ADService.Environments;
using ADService.Media;
using System.DirectoryServices;

namespace ADService.Foundation
{
    /// <summary>
    /// 登入的 ADService 使用者
    /// </summary>
    public sealed class LDAPLogonPerson : LDAPPerson
    {
        /// <summary>
        /// 帳號
        /// </summary>
        internal readonly string UserName;
        /// <summary>
        /// 密碼
        /// </summary>
        internal readonly string Password;

        /// <summary>
        /// 透過建構子解析內容資料
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="dispatcher">入口物件創建器</param>
        /// <exception cref="LDAPExceptions">解析鍵值不符合規則時對外丟出</exception>
        internal LDAPLogonPerson(in DirectoryEntry entry, in LDAPConfigurationDispatcher dispatcher) : base(entry, dispatcher)
        {
            UserName  = dispatcher.UserName;
            Password  = dispatcher.Password;
        }
    }
}