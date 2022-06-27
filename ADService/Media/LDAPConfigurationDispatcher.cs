using ADService.Environments;
using ADService.Protocol;
using System;
using System.DirectoryServices;

namespace ADService.Media
{
    /// <summary>
    /// 執行續安全的設定取得結構
    /// </summary>
    internal class LDAPConfigurationDispatcher
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
        /// 紀錄外部提供的入口物件創建器
        /// </summary>
        internal readonly LDAPConfiguration Configuration;

        /// <summary>
        /// 設定區分名稱
        /// </summary>
        internal readonly string ConfigurationDistinguishedName;

        /// <summary>
        /// 取得 DSE 中的設定區分名稱位置, 並建構連線用相關暫存
        /// </summary>
        /// <param name="userName">使用者名稱</param>
        /// <param name="password">密碼</param>
        /// <param name="configuration">資料分配與調度者</param>
        internal LDAPConfigurationDispatcher(in string userName, in string password, in LDAPConfiguration configuration)
        {
            UserName = userName;
            Password = password;
            Configuration = configuration;

            // 取得設定位置
            using (DirectoryEntry root = DSERoot())
            {
                // 取得內部設定位置
                ConfigurationDistinguishedName = LDAPConfiguration.ParseSingleValue<string>(LDAPConfiguration.CONTEXT_CONFIGURATION, root.Properties);
            }
        }

        #region 設定取得
        /// <summary>
        /// 取得藍本
        /// </summary>
        /// <param name="value">目標 GUID</param>
        /// <returns>藍本結構</returns>
        internal UnitSchema GetSchema(in Guid value) => Configuration.GetSchema(this, value);

        /// <summary>
        /// 使用展示名稱 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="value">展示名稱</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchema GetSchema(in string value) => Configuration.GetSchema(this, value);

        /// <summary>
        /// 取得額外權限
        /// </summary>
        /// <param name="value">目標 GUID</param>
        /// <returns>額外權限結構</returns>
        internal UnitExtendedRight GetExtendedRight(in Guid value) => Configuration.GetExtendedRight(this, value);

        /// <summary>
        /// 使用展示名稱 進行搜尋指定目標額外權限物件
        /// </summary>
        /// <param name="value">展示名稱</param>
        /// <returns>指定額外權限物件, 可能不存在</returns>
        internal UnitExtendedRight GetExtendedRight(in string value) => Configuration.GetExtendedRight(this, value);

        /// <summary>
        /// 使用 GUID 找到展示名稱
        /// </summary>
        /// <param name="value">目標 GUID</param>
        /// <returns></returns>
        internal string FindName(in Guid value)
        {
            // 提供的 GUID 為空
            if (LDAPConfiguration.IsGUIDEmpty(value))
            {
                // 提供空字串
                return string.Empty;
            }

            // 轉換成根據查詢結構: 若此處出現錯誤則必定是羅錯誤導致加入重複物件
            UnitExtendedRight unitExtendedRight = Configuration.GetExtendedRight(this, value);
            // 從額外權限中找到目標資料
            if (unitExtendedRight != null)
            {
                // 設置對外提供的名稱: 額外權權的展示名稱
                return unitExtendedRight.Name;
            }

            // 轉換成根據查詢結構: 若此處出現錯誤則必定是羅錯誤導致加入重複物件
            UnitSchema unitSchema = Configuration.GetSchema(this, value);
            // 從藍本中找到目標資料
            if (unitSchema != null)
            {
                // 設置對外提供的名稱: 額外權權的展示名稱
                return unitSchema.Name;
            }

            // 應找尋得到額外權限: 若取得此例外則必定是 AD 設置有漏洞
            throw new LDAPExceptions($"目標 GUID:{value} 無法於額外權限中找得展示名稱因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
        }
        #endregion

        #region 入口物件取得
        /// <summary>
        /// 透過使用者的權限取得網域入口物件, 注意如果無法創建會丟出各種例外, 取得的入口物件也需進行 <see cref="IDisposable">釋放</see> 行為
        /// </summary>
        /// <returns>入口物件, 使用完畢務必<see cref="IDisposable">釋放</see> </returns>
        /// <exception cref="COMException">使用者無法連線至目標網域伺服器時對外丟出</exception>
        /// <exception cref="DirectoryServicesCOMException">可以連線至目標網域伺服器, 但是帳號或密碼不正確...等相關錯誤</exception>
        internal DirectoryEntry DomainRoot()
        {
            // 使用提供的使用者帳號密碼連線至根網域物件: 此時有可能丟出的例外: 伺服器無法連線
            DirectoryEntry entryRoot = new DirectoryEntry($"LDAP://{Configuration.Domain}:{Configuration.Port}", UserName, Password);
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
            DirectoryEntry entryRoot = new DirectoryEntry($"LDAP://{Configuration.Domain}:{Configuration.Port}/rootDSE", UserName, Password);
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
            DirectoryEntry entryRoot = new DirectoryEntry($"LDAP://{Configuration.Domain}:{Configuration.Port}/{distinguisedName}", UserName, Password, AuthenticationTypes.Secure | AuthenticationTypes.ServerBind);
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
            DirectoryEntry entryRoot = new DirectoryEntry($"LDAP://{Configuration.Domain}:{Configuration.Port}/<GUID={GUID}>", UserName, Password);
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
            DirectoryEntry entryRoot = new DirectoryEntry($"LDAP://{Configuration.Domain}:{Configuration.Port}/<SID={SID}>", UserName, Password);
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
        #endregion
    }
}
