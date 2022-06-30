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
        /// 透過透過額外權限取得所有關聯屬性組
        /// </summary>
        /// <param name="securityGUID">額外權限的 GUID, 用來匹配藍本的安全屬性 GUID</param>
        /// <returns>此額外權限需求的藍本群組</returns>
        internal UnitSchema[] GetPropertySet(in Guid securityGUID) => Configuration.GetPropertySet(this, securityGUID);

        /// <summary>
        /// 取得藍本
        /// </summary>
        /// <param name="guids">目標 GUID 陣列</param>
        /// <returns>藍本結構</returns>
        internal UnitSchema[] GetSchema(params Guid[] guids) => Configuration.GetSchema(this, guids);

        /// <summary>
        /// 使用展示名稱 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="attributeNames">屬性名稱</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchema[] GetSchema(params string[] attributeNames) => Configuration.GetSchema(this, attributeNames);

        /// <summary>
        /// 取得相關 GUID 的
        /// </summary>
        /// <param name="guids">目標 GUID 陣列</param>
        /// <returns>藍本結構</returns>
        internal UnitExtendedRight[] GetExtendedRight(params Guid[] guids) => Configuration.GetExtendedRight(this, guids);

        /// <summary>
        /// 使用依賴類別 GUID 找尋相關的額外權限
        /// </summary>
        /// <param name="unitSchemas">查詢的藍本</param>
        /// <returns>指定額外權限物件, 可能不存在</returns>
        internal UnitExtendedRight[] GetExtendedRight(params UnitSchema[] unitSchemas) => Configuration.GetExtendedRight(this, unitSchemas);

        /// <summary>
        /// 提供存取權限中目標物件的 GUID 查詢對應的資料
        /// </summary>
        /// <param name="accessRuleGUIDs">目標 GUID 陣列</param>
        /// <returns>返回各 GUID 的詳細資料, 格式如右: Dictiobary 'Guid 字串(小寫), 存取規則描述' </returns>
        internal Dictionary<string, UnitDetail> GetUnitDetail(in IEnumerable<Guid> accessRuleGUIDs)
        {
            // 轉換成袋查詢的資料
            Dictionary<string, Guid> dictionaryGUIDLowerWithGUID = accessRuleGUIDs.ToDictionary(accessRuleGUID => accessRuleGUID.ToString("D").ToLower());
            // 長度最多為外部宣告的 GUID 大小
            Dictionary<string, UnitDetail> dictionaryGuidWithAUnitDetail = new Dictionary<string, UnitDetail>(dictionaryGUIDLowerWithGUID.Count);
            // 遍歷所有取得的藍本
            foreach (UnitSchema unitSchema in Configuration.GetSchema(this, dictionaryGUIDLowerWithGUID.Values))
            {
                // 交查詢到的 GUID 轉為小寫
                string unitSchemaGUIDLower = unitSchema.SchemaGUID.ToLower();
                // 取得類型
                UnitType accessRuleObjectType = unitSchema is UnitSchemaAttribute _ ? UnitType.ATTRIBUTE : UnitType.CLASS;
                // 強型別宣告方便閱讀
                UnitDetail accessRuleObjectDetail = new UnitDetail(unitSchema.Name, accessRuleObjectType);
                // 推入查詢物件
                dictionaryGuidWithAUnitDetail.Add(unitSchemaGUIDLower, accessRuleObjectDetail);

                // 將查詢到的藍本移除
                dictionaryGUIDLowerWithGUID.Remove(unitSchemaGUIDLower);
            }
            
            // 遍歷所有取得的額外權限
            foreach (UnitExtendedRight unitExtendedRight in Configuration.GetExtendedRight(this, dictionaryGUIDLowerWithGUID.Values))
            {
                // 交查詢到的 GUID 轉為小寫
                string unitExtendedRightGUIDLower = unitExtendedRight.RightsGUID.ToLower();
                // 強型別宣告方便閱讀
                UnitDetail accessRuleObjectDetail = new UnitDetail(unitExtendedRight.Name, UnitType.EXTENDEDRIGHT);
                // 推入查詢物件
                dictionaryGuidWithAUnitDetail.Add(unitExtendedRightGUIDLower, accessRuleObjectDetail);

                // 將查詢到的藍本移除
                dictionaryGUIDLowerWithGUID.Remove(unitExtendedRightGUIDLower);
            }

            // 安全防呆: 所有指定的 GUID 都應該要能被找到
            if (dictionaryGUIDLowerWithGUID.Count != 0)
            {
                // 此時拋出例外: 因為 GUID 應被晚整取得, 此處對外提供邏輯錯誤
                throw new LDAPExceptions($"預期應找尋的 GUID 中還有剩餘的部分:{string.Join(",", dictionaryGUIDLowerWithGUID.Keys)} 因而拋出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 無錯誤情況下對外提供資料
            return dictionaryGuidWithAUnitDetail;
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
