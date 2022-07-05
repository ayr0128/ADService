using ADService.Environments;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

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
        /// 取得指定展示名稱的物件類別藍本
        /// </summary>
        /// <param name="ldapDisplayNames">類別物件的展示名稱</param>
        /// <returns>類別物件藍本</returns>
        internal UnitSchemaClass[] GetClasses(params string[] ldapDisplayNames)
        {
            // 透過物件持有類別取得所有可用屬性以及所有可用子類別
            UnitSchemaClass[] originUnitSchemaClasses = Configuration.GetOriginClasses(this, ldapDisplayNames);
            // 取得所有指定類別的可用輔助類別
            UnitSchemaClass[] drivedUnitSchemaClasses = Configuration.GetDrivedClasses(this, originUnitSchemaClasses);
            // 整合上述兩者
            List<UnitSchemaClass> unitSchemaClasses = new List<UnitSchemaClass>(originUnitSchemaClasses.Length + drivedUnitSchemaClasses.Length);
            // 推入原始類別藍本
            unitSchemaClasses.AddRange(originUnitSchemaClasses);
            // 推入驅動類別藍本
            unitSchemaClasses.AddRange(drivedUnitSchemaClasses);
            // 對外提供資料
            return unitSchemaClasses.ToArray();
        }

        /// <summary>
        /// 取得以類別藍本物件為父層的類別
        /// </summary>
        /// <param name="unitSchemaClasses">查詢的物件藍本</param>
        /// <returns>類別物件藍本</returns>
        internal UnitSchemaClass[] GetChildrenClasess(params UnitSchemaClass[] unitSchemaClasses) => Configuration.GetChildrenClasses(this, unitSchemaClasses);

        /// <summary>
        /// 使用物件藍本取得所有存取控制權限
        /// </summary>
        /// <param name="unitSchemaClasses">查詢的物件藍本</param>
        /// <returns>指定額外權限物件, 可能不存在</returns>
        internal UnitControlAccess[] GeControlAccess(params UnitSchemaClass[] unitSchemaClasses) => Configuration.GeControlAccess(this, unitSchemaClasses);

        /// <summary>
        /// 使用指定存取權限找到相關聯的屬性值, 並回傳存取類型
        /// </summary>
        /// <param name="unitControlAccess">目標存取璇縣</param>
        /// <param name="unitSchemaAttributes">此存取權限關聯的屬性</param>
        /// <returns>此存取權限為何種類型</returns>
        internal ControlAccessType GetControlAccessAttributes(in UnitControlAccess unitControlAccess, out UnitSchemaAttribute[] unitSchemaAttributes) => Configuration.GeControlAccessAttributes(this, unitControlAccess, out unitSchemaAttributes);

        /// <summary>
        /// 使用展示名稱 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="attributeGUID">屬性 GUID </param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchemaAttribute GetUnitSchemaAttribute(in Guid attributeGUID) => Configuration.GetUnitSchemaAttribute(this, attributeGUID);

        /// <summary>
        /// 使用展示名稱 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="lDAPDisplayNames">屬性名稱 </param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchemaAttribute[] GetUnitSchemaAttribute(params string[] lDAPDisplayNames) => Configuration.GetUnitSchemaAttribute(this, lDAPDisplayNames);
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
