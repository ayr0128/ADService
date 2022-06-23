using System;
using System.DirectoryServices;
using System.Runtime.InteropServices;

namespace ADService.Media
{
    /// <summary>
    /// 創建伺服器連線資訊時同步宣告, 儲存伺服器連線相關資訊並提供呼叫方法
    /// </summary>
    internal class LDAPEntries
    {
        /// <summary>
        /// 連線網域: 可用 IP 或 網址, 根據實作方式限制
        /// </summary>
        internal readonly string Domain;
        /// <summary>
        /// 連線埠
        /// </summary>
        internal readonly ushort Port;

        /// <summary>
        /// 初始化伺服器連線用方法
        /// </summary>
        /// <param name="domain">指定網域</param>
        /// <param name="port">指定埠</param>
        internal LDAPEntries(string domain, ushort port)
        {
            Domain = domain;
            Port = port;
        }

        /// <summary>
        /// 提供使用者名稱與密碼, 將透過此使用者的權限與伺服器聯繫並取得相關物件
        /// </summary>
        /// <param name="userName">使用者名稱</param>
        /// <param name="password">使用者密碼</param>
        /// <returns>提供透過使用者權限與伺服器聯繫並取得入口物件相關功能的介面</returns>
        internal LDAPEntriesMedia GetCreator(in string userName, in string password) => new LDAPEntriesMedia(userName, password, Domain, Port);
    }

    /// <summary>
    /// 繼承了取得入口物件方法的媒介類別
    /// </summary>
    internal sealed class LDAPEntriesMedia 
    {
        /// <summary>
        /// 使用者名稱
        /// </summary>
        internal readonly string UserName;
        /// <summary>
        /// 使用者密碼
        /// </summary>
        internal readonly string Password;
        /// <summary>
        /// 往玉
        /// </summary>
        internal readonly string Domain;
        /// <summary>
        /// 埠
        /// </summary>
        internal readonly ushort Port;

        /// <summary>
        /// 創建實體用來對外提供創建入口功能
        /// </summary>
        /// <param name="userName">使用者名稱</param>
        /// <param name="password">密碼</param>
        /// <param name="domain">目標網域</param>
        /// <param name="port">目標埠</param>
        internal LDAPEntriesMedia(in string userName, in string password, in string domain, in ushort port)
        {
            UserName = userName;
            Password = password;
            Domain = domain;
            Port = port;
        }

        /// <summary>
        /// 透過使用者的權限取得網域入口物件, 注意如果無法創建會丟出各種例外, 取得的入口物件也需進行 <see cref="IDisposable">釋放</see> 行為
        /// </summary>
        /// <returns>入口物件, 使用完畢務必<see cref="IDisposable">釋放</see> </returns>
        /// <exception cref="COMException">使用者無法連線至目標網域伺服器時對外丟出</exception>
        /// <exception cref="DirectoryServicesCOMException">可以連線至目標網域伺服器, 但是帳號或密碼不正確...等相關錯誤</exception>
        internal DirectoryEntry DomainRoot()
        {
            // 使用提供的使用者帳號密碼連線至根網域物件: 此時有可能丟出的例外: 伺服器無法連線
            DirectoryEntry entryRoot = new DirectoryEntry($"LDAP://{Domain}:{Port}", UserName, Password);
            /* 使用其中一個物件用以判斷是有正確取得資料, 此時有可能丟出的例外:
                 - 帳號密碼錯誤
                 - 帳號禁用
                 - 密碼過期
                 - 非可登入時間
            */
            _ = entryRoot.NativeGuid;
            // 運行至此應可正常取得相關入口物件
            return entryRoot;
        }

        /// <summary>
        /// 透過使用者的權限取得網域設定物件, 注意如果無法創建會丟出各種例外, 取得的入口物件也需進行 <see cref="IDisposable">釋放</see> 行為
        /// </summary>
        /// <returns>設定物件, 使用完畢務必<see cref="IDisposable">釋放</see> </returns>
        /// <exception cref="COMException">使用者無法連線至目標網域伺服器時對外丟出</exception>
        /// <exception cref="DirectoryServicesCOMException">可以連線至目標網域伺服器, 但是帳號或密碼不正確...等相關錯誤</exception>
        internal DirectoryEntry DSERoot()
        {
            // 使用提供的使用者帳號密碼連線至根網域設定物件: 此時有可能丟出的例外: 伺服器無法連線
            DirectoryEntry entryRoot = new DirectoryEntry($"LDAP://{Domain}:{Port}/rootDSE", UserName, Password);
            /* 使用其中一個物件用以判斷是有正確取得資料, 此時有可能丟出的例外:
                 - 帳號密碼錯誤
                 - 帳號禁用
                 - 密碼過期
                 - 非可登入時間
            */
            _ = entryRoot.NativeGuid;
            // 運行至此應可正常取得相關入口物件
            return entryRoot;
        }

        /// <summary>
        /// 透過使用者的權限取得指定區分名稱物件作為入口物件, 注意如果無法創建會丟出各種例外, 取得的入口物件也需進行 <see cref="IDisposable">釋放</see> 行為
        /// </summary>
        /// <param name="distinguisedName">指定物件的區分名稱</param>
        /// <returns>入口物件, 使用完畢務必<see cref="IDisposable">釋放</see> </returns>
        /// <exception cref="COMException">使用者無法連線至目標網域伺服器時對外丟出</exception>
        /// <exception cref="DirectoryServicesCOMException">可以連線至目標網域伺服器, 但是帳號或密碼不正確, 指定物件不存在...等相關錯誤</exception>
        internal DirectoryEntry ByDistinguisedName(in string distinguisedName)
        {
            // 區分名稱為空或不存在: 簡易防呆
            if (string.IsNullOrWhiteSpace(distinguisedName))
            {
                // 對外丟出 ArgumentException
                throw new ArgumentException($"'{nameof(distinguisedName)}' 不得為 Null 或空白字元。", nameof(distinguisedName));
            }

            // 使用提供的使用者帳號密碼連線至根網域物件: 此時有可能丟出的例外: 伺服器無法連線
            DirectoryEntry entryRoot = new DirectoryEntry($"LDAP://{Domain}:{Port}/{distinguisedName}", UserName, Password, AuthenticationTypes.Secure | AuthenticationTypes.ServerBind);
            /* 使用其中一個物件用以判斷是有正確取得資料, 此時有可能丟出的例外:
                 - 帳號密碼錯誤
                 - 帳號禁用
                 - 密碼過期
                 - 非可登入時間
                 - 指定物件不存在
            */
            _ = entryRoot.NativeGuid;
            // 運行至此應可正常取得相關入口物件
            return entryRoot;
        }

        /// <summary>
        /// 透過使用者的權限取得指定 GUID 物件作為入口物件, 注意如果無法創建會丟出各種例外, 取得的入口物件也需進行 <see cref="IDisposable">釋放</see> 行為
        /// </summary>
        /// <param name="GUID">指定物件的 GUID</param>
        /// <returns>入口物件, 使用完畢務必<see cref="IDisposable">釋放</see> </returns>
        /// <exception cref="COMException">使用者無法連線至目標網域伺服器時對外丟出</exception>
        /// <exception cref="DirectoryServicesCOMException">可以連線至目標網域伺服器, 但是帳號或密碼不正確, 指定物件不存在...等相關錯誤</exception>
        internal DirectoryEntry ByGUID(in string GUID)
        {
            // 區分名稱為空或不存在: 簡易防呆
            if (string.IsNullOrWhiteSpace(GUID))
            {
                // 對外丟出 ArgumentException
                throw new ArgumentException($"'{nameof(GUID)}' 不得為 Null 或空白字元。", nameof(GUID));
            }

            // 使用提供的使用者帳號密碼連線至根網域物件: 此時有可能丟出的例外: 伺服器無法連線
            DirectoryEntry entryRoot = new DirectoryEntry($"LDAP://{Domain}:{Port}/<GUID={GUID}>", UserName, Password);
            /* 使用其中一個物件用以判斷是有正確取得資料, 此時有可能丟出的例外:
                 - 帳號密碼錯誤
                 - 帳號禁用
                 - 密碼過期
                 - 非可登入時間
                 - 指定物件不存在
            */
            _ = entryRoot.NativeGuid;
            // 運行至此應可正常取得相關入口物件
            return entryRoot;
        }

        /// <summary>
        /// 透過使用者的權限取得指定 SID 物件作為入口物件, 注意如果無法創建會丟出各種例外, 取得的入口物件也需進行 <see cref="IDisposable">釋放</see> 行為
        /// </summary>
        /// <param name="SID">指定物件的 SID</param>
        /// <returns>入口物件, 使用完畢務必<see cref="IDisposable">釋放</see> </returns>
        /// <exception cref="COMException">使用者無法連線至目標網域伺服器時對外丟出</exception>
        /// <exception cref="DirectoryServicesCOMException">可以連線至目標網域伺服器, 但是帳號或密碼不正確, 指定物件不存在...等相關錯誤</exception>
        internal DirectoryEntry BySID(in string SID)
        {
            // 區分名稱為空或不存在: 簡易防呆
            if (string.IsNullOrWhiteSpace(SID))
            {
                // 對外丟出 ArgumentException
                throw new ArgumentException($"'{nameof(SID)}' 不得為 Null 或空白字元。", nameof(SID));
            }

            // 使用提供的使用者帳號密碼連線至根網域物件: 此時有可能丟出的例外: 伺服器無法連線
            DirectoryEntry entryRoot = new DirectoryEntry($"LDAP://{Domain}:{Port}/<SID={SID}>", UserName, Password);
            /* 使用其中一個物件用以判斷是有正確取得資料, 此時有可能丟出的例外:
                 - 帳號密碼錯誤
                 - 帳號禁用
                 - 密碼過期
                 - 非可登入時間
                 - 指定物件不存在
            */
            _ = entryRoot.NativeGuid;
            // 運行至此應可正常取得相關入口物件
            return entryRoot;
        }
    }
}
