using ADService.Certification;
using ADService.Environments;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Runtime.InteropServices;

namespace ADService
{
    /// <summary>
    /// 基礎架構, 須被繼承後才能使用
    /// </summary>
    public abstract class LDAPServe
    {
        /// <summary>
        /// 此網域伺服器目前被讀取做為暫存參數的所有設定: 有效時間為讀取後五分鐘
        /// </summary>
        private readonly LDAPConfiguration Configuration;

        /// <summary>
        /// 儲存預計連線的伺服器位置, 當有連線需求時會使用提供的帳號與密碼進行連線伺服器嘗試取得資料
        /// </summary>
        /// <param name="domain">伺服器 綁定DNS 或 固定IP</param>
        /// <param name="port">連線埠</param>
        /// <exception cref="ArgumentException">上述數值不符合規則時對外提供</exception>
        public LDAPServe(in string domain, in ushort port)
        {
            /* 外部提供的 網域 或 固定IP 不得為空
               [CHECK] 是否需要增加正規表達式檢查
            */
            if (string.IsNullOrEmpty(domain))
            {
                // 對外丟出 ArgumentException
                throw new ArgumentException($"'{nameof(domain)}' 不得為 Null 或空白字元。", nameof(domain));
            }

            // 初始化入口功能支援結構方法
            Configuration = new LDAPConfiguration(domain, port);

            // 備註: 上述會對外丟出例外的各種參數只是提前判斷基礎規則, 如果不能連線仍然會在嘗試與 AD 伺服器溝通時丟出例外錯誤
        }

        /// <summary>
        /// 使用登入者的帳號與密碼取得隸屬群組與樹系位置
        /// </summary>
        /// <param name="userName">登入者帳號</param>
        /// <param name="password">登入者密碼</param>
        /// <exception cref="ArgumentException">提供的使用者帳號或密碼不符合規則時對外丟出</exception>
        /// <exception cref="LDAPExceptions">帳號密碼不正確, 無法發現使用者等多種情況時對外丟出, 可參考例外儲存的 <see cref="ErrorCodes">ErrorCode</see> 參數判斷錯誤類型</exception>
        public LDAPLogonPerson AuthenticationUser(in string userName, in string password)
        {
            // 帳號不得為空或全是空白
            if (string.IsNullOrWhiteSpace(userName))
            {
                // 對外丟出 ArgumentException
                throw new ArgumentException($"'{nameof(userName)}' 不得為 Null 或空白字元。", nameof(userName));
            }

            // 密碼不得為空或全是空白 (不允許不用密碼進行登入)
            if (string.IsNullOrWhiteSpace(password))
            {
                // 對外丟出 ArgumentException
                throw new ArgumentException($"'{nameof(password)}' 不得為 Null 或空白字元。", nameof(password));
            }

            /* 此處有可能接收到下述例外
                 - COMException: 伺服器發生問題
                 - DirectoryServicesCOMException: 使用帳號連線出現問題
            */
            try
            {
                // 取得設定與入口物件創建器
                LDAPConfigurationDispatcher dispatcher = Configuration.Dispatch(userName, password);
                // 使用 using 讓連線在跳出方法後即刻釋放: 此處使用的權限是登入者的帳號權限
                using (DirectoryEntry root = dispatcher.DomainRoot())
                {
                    // 找到須限制的物件類型
                    Dictionary<CategoryTypes, string> dictionaryLimitedCategory = LDAPCategory.GetValuesByTypes(CategoryTypes.PERSON);
                    // 加密避免 LDAP 注入式攻擊
                    string encoderFiliter = $"(&{LDAPConfiguration.GetORFiliter(Properties.C_OBJECTCATEGORY, dictionaryLimitedCategory.Values)}(|(sAMAccountName={userName})(userPrincipalName={userName})))";
                    /* 備註: 為何要額外搜尋一次?
                         1. 連線時如果未在伺服器後提供區分名稱, 會使用物件類型 domainDNS 來回傳
                         2. 為避免部分資料缺失, 需額外指定
                    */
                    using (DirectorySearcher searcher = new DirectorySearcher(root, encoderFiliter, LDAPObject.PropertiesToLoad))
                    {
                        // 必定存在至少一個搜尋結果:
                        SearchResult one = searcher.FindOne();
                        // 不存在搜尋結果
                        if (one == null)
                        {
                            // 對外丟出例外: 邏輯錯誤, 這種錯誤除非多網域否則不應發生
                            throw new LDAPExceptions($"登入使用者:{userName} 時因無法使用者的實體物件而失敗丟出例外", ErrorCodes.LOGIC_ERROR);
                        }

                        using (DirectoryEntry entry = one.GetDirectoryEntry())
                        {
                            // 對外提供登入者結構: 建構時若無法找到必須存在的鍵值會丟出例外
                            return new LDAPLogonPerson(entry, dispatcher, one.Properties);
                        }
                    }
                }
            }
            // 發生時機: 使用者登入時發現例外錯誤
            catch (DirectoryServicesCOMException exception)
            {
                // 解析例外字串, 並提供例外
                throw LDAPExceptions.OnNormalException(exception.ExtendedErrorMessage);
            }
            // 發生時機: 與伺服器連線時發現錯誤
            catch (COMException exception)
            {
                // 取得錯誤描述, 並提供例外
                throw LDAPExceptions.OnServeException(exception.Message);
            }
            // 沒有 Finally 語句
        }

        /// <summary>
        /// 透過登入者權限找尋指定 GUID
        /// </summary>
        /// <param name="logon">登入者</param>
        /// <param name="GUID"></param>
        /// <returns></returns>
        public LDAPObject GetObjectByGUID(in LDAPLogonPerson logon, in string GUID)
        {
            // 使用指定使用者帳號密碼製作一個入口物件製作器
            LDAPConfigurationDispatcher dispatcher = Configuration.Dispatch(logon.UserName, logon.Password);
            /* 此處處理出現問題會接收到例外:
                 1. 若訪問伺服器發生問題: 會對外提供 LDAPExceptions
            */
            try
            {
                // 透過 GUID 找到指定物件, 注意如果找不到會有奇怪的錯誤
                using (DirectoryEntry entry = dispatcher.ByGUID(GUID))
                {
                    // 取得區分名稱
                    string distinguishedName = LDAPConfiguration.ParseSingleValue<string>(Properties.C_DISTINGGUISHEDNAME, entry.Properties);
                    // [TODO] 應使用加密字串避免注入式攻擊
                    string encoderFiliter = LDAPConfiguration.GetORFiliter(Properties.C_DISTINGGUISHEDNAME, distinguishedName);
                    // 找尋某些額外參數
                    using (DirectorySearcher searcher = new DirectorySearcher(entry, encoderFiliter, LDAPObject.PropertiesToLoad, SearchScope.Base))
                    {
                        // 找到其他屬性
                        SearchResult one = searcher.FindOne();
                        // 轉換成可用物件
                        return LDAPObject.ToObject(entry, dispatcher, one.Properties);
                    }
                }
            }
            // 發生時機: 使用者登入時發現例外錯誤
            catch (DirectoryServicesCOMException exception)
            {
                // 解析例外字串, 並提供例外
                throw LDAPExceptions.OnNormalException(exception.ExtendedErrorMessage);
            }
            // 發生時機: 與伺服器連線時發現錯誤
            catch (COMException exception)
            {
                // 取得錯誤描述, 並提供例外
                throw LDAPExceptions.OnServeException(exception.Message);
            }
            // 沒有 Finally 語句
        }

        /// <summary>
        /// 找尋指定區分名稱的物件, 沒有指定時不會提供任何物件
        /// </summary>
        /// <param name="logon">登入者</param>
        /// <param name="categories">限制僅查詢某些類型, 預設為不限制</param>
        /// <param name="distinguishedNames">指定的區分名稱必須提供資料</param>
        /// <returns>使用提供條件找尋指定的區分名稱物件, 需自行檢查是否有發現目標, 格式如右: Dictionary '區分名稱, 基層物件類型'</returns>
        public Dictionary<string, LDAPObject> GetObjects(in LDAPLogonPerson logon, in CategoryTypes categories = CategoryTypes.NONE, params string[] distinguishedNames)
        {
            // 空字串: 沒有指定區分名稱
            if (distinguishedNames.Length == 0)
            {
                // 對外提供容器大小為 0 的字典, 因為沒有指定區分名稱
                return new Dictionary<string, LDAPObject>(0);
            }

            // 找到須限制的物件類型
            Dictionary<CategoryTypes, string> dictionaryLimitedCategory = LDAPCategory.GetValuesByTypes(categories);
            /* 此處處理出現問題會接收到例外:
                 1. 若訪問伺服器發生問題: 會對外提供 LDAPExceptions
            */
            try
            {
                // 使用指定使用者帳號密碼製作一個入口物件製作器
                LDAPConfigurationDispatcher dispatcher = Configuration.Dispatch(logon.UserName, logon.Password);
                // 找尋指定區分名稱時需要從根目錄開始找尋
                using (DirectoryEntry root = dispatcher.DomainRoot())
                {
                    // [TODO] 應使用加密字串避免注入式攻擊
                    string encoderFiliter = $"(&{LDAPConfiguration.GetORFiliter(Properties.C_OBJECTCATEGORY, dictionaryLimitedCategory.Values)}{LDAPConfiguration.GetORFiliter(Properties.C_DISTINGGUISHEDNAME, distinguishedNames)})";
                    // 找尋指定目標
                    using (DirectorySearcher searcher = new DirectorySearcher(root, encoderFiliter, LDAPObject.PropertiesToLoad, SearchScope.Subtree))
                    {
                        // 將指定目標過濾出來
                        using (SearchResultCollection all = searcher.FindAll())
                        {
                            // 使用取得的資料長度作為容器大小宣告字典, 格式如右: Dictionary '區分名稱, 基層物件類型'
                            Dictionary<string, LDAPObject> dictionaryDNWithObject = new Dictionary<string, LDAPObject>(all.Count);
                            // 遍歷取得的結果
                            foreach (SearchResult one in all)
                            {
                                // 將取得物件轉換為入口物件
                                using (DirectoryEntry resultEntry = one.GetDirectoryEntry())
                                {
                                    // 轉換成系統使用的物件類型
                                    LDAPObject resultObject = LDAPObject.ToObject(resultEntry, dispatcher, one.Properties);
                                    // 物件為登入者時, 使用新物件的特性鍵值更新登入者並更換儲存物件:
                                    LDAPObject storedObject = logon.SwapFrom(resultObject);
                                    // 絕對不應該重複
                                    dictionaryDNWithObject.Add(resultObject.DistinguishedName, storedObject);
                                }
                            }
                            // 將發現的字典黨對外提供
                            return dictionaryDNWithObject;
                        }
                    }
                }
            }
            // 發生時機: 使用者登入時發現例外錯誤
            catch (DirectoryServicesCOMException exception)
            {
                // 解析例外字串, 並提供例外
                throw LDAPExceptions.OnNormalException(exception.ExtendedErrorMessage);
            }
            // 發生時機: 與伺服器連線時發現錯誤
            catch (COMException exception)
            {
                // 取得錯誤描述, 並提供例外
                throw LDAPExceptions.OnServeException(exception.Message);
            }
            // 沒有 Finally 語句
        }

        /// <summary>
        /// 根據提供的組織單位區分名稱取得歸屬於它的成員與組織單位, 組織單位區分名稱未提供時則提供網域
        /// </summary>
        /// <param name="logon">登入者</param>
        /// <param name="extendFlags">延展找尋 <see cref="CategoryTypes">旗標</see>, 基本會提供網域或組織單位, 根據延展需求額外提供內內容</param>
        /// <param name="objectLDAPs">任意的基礎物件, 但是僅處理 <see cref="CategoryTypes.ALL_CONTAINERS">這些類型</see></param>
        /// <returns>目標組織單位|網域入口的歸屬狀態, 沒有指定任何目標時必定能以 <see cref="LDAPAssembly.ROOT">根目錄</see>取得資料 </returns>
        /// <exception cref="LDAPExceptions">管理者帳號密碼不正確, 伺服器異常使用者等多種情況時對外丟出, 可參考例外儲存的 <see cref="ErrorCodes">ErrorCode</see> 參數判斷錯誤類型</exception>
        public Dictionary<string, LDAPAssembly> GetOrganizationUnits(in LDAPLogonPerson logon, in CategoryTypes extendFlags = CategoryTypes.NONE, params LDAPObject[] objectLDAPs)
        {
            // 此方法至少對外提供這幾個項目
            const CategoryTypes categories = CategoryTypes.DOMAIN_DNS | CategoryTypes.ORGANIZATION_UNIT;
            /* 此處處理出現問題會接收到例外:
                 1. 若訪問伺服器發生問題: 會對外提供 LDAPExceptions
            */
            try
            {
                // 使用指定使用者帳號密碼製作一個入口物件製作器
                LDAPConfigurationDispatcher dispatcher = Configuration.Dispatch(logon.UserName, logon.Password);
                // 有指定區分名稱
                if (objectLDAPs.Length != 0)
                {
                    // 無指定組織單位或網域時, 容器大小必為 1
                    Dictionary<string, LDAPAssembly> dictionaryDNWithAssembly = new Dictionary<string, LDAPAssembly>(objectLDAPs.Length);
                    // 遍歷所有希望確認的物件
                    foreach (LDAPObject mixedObject in objectLDAPs)
                    {
                        // 非容器類型時
                        if ((mixedObject.Type & CategoryTypes.ALL_CONTAINERS) == CategoryTypes.NONE)
                        {
                            // 跳過處理不推入物件
                            continue;
                        }

                        // 使用 using 讓連線在跳出方法後即刻釋放: 此處使用的權限是登入者的帳號權限
                        using (DirectoryEntry entryObject = dispatcher.ByDistinguisedName(mixedObject.DistinguishedName))
                        {
                            // 是容器類型是必定能被轉換成容器
                            LDAPAssembly assembly = (LDAPAssembly)mixedObject;
                            // 取得此入口物件類型下的目標類型物件
                            List<LDAPObject> children = LDAPAssembly.WithChild(entryObject, dispatcher, categories | extendFlags);
                            // 將找尋的下層子物件提供給集成類型物件並刷新
                            assembly.Reflash(children);
                            // 對外提供轉換完成的結果
                            dictionaryDNWithAssembly.Add(assembly.DistinguishedName, assembly);
                        }
                    }
                    // 將處理完成的組織單位提供給外部
                    return dictionaryDNWithAssembly;
                }
                // 不具有長度時當作找尋網域
                else
                {
                    // 無指定組織單位或網域時, 容器大小必為 1
                    Dictionary<string, LDAPAssembly> organizationuUnitDictionary = new Dictionary<string, LDAPAssembly>(1);
                    // 使用 using 讓連線在跳出方法後即刻釋放: 此處使用的權限是登入者的帳號權限
                    using (DirectoryEntry root = dispatcher.DomainRoot())
                    {
                        // 取得區分名稱
                        string distinguishedName = LDAPConfiguration.ParseSingleValue<string>(Properties.C_DISTINGGUISHEDNAME, root.Properties);
                        // [TODO] 應使用加密字串避免注入式攻擊
                        string encoderFiliter = LDAPConfiguration.GetORFiliter(Properties.C_DISTINGGUISHEDNAME, distinguishedName);
                        // 找尋某些額外參數
                        using (DirectorySearcher searcher = new DirectorySearcher(root, encoderFiliter, LDAPObject.PropertiesToLoad, SearchScope.Base))
                        {
                            // 找到需額外搜尋的資料
                            SearchResult one = searcher.FindOne();
                            // 製作根目錄物件: 此時絕對是集成類型的物件
                            LDAPAssembly assembly = (LDAPAssembly)LDAPObject.ToObject(root, dispatcher, one.Properties);
                            // 取得此入口物件類型下的目標類型物件
                            List<LDAPObject> children = LDAPAssembly.WithChild(root, dispatcher, categories | extendFlags);
                            // 將找尋的下層子物件提供給集成類型物件並刷新
                            assembly.Reflash(children);
                            // 由於外部沒有指定查詢的區分名稱, 因此使用固定字串讓外部可以取得找尋到的網域
                            organizationuUnitDictionary.Add(LDAPAssembly.ROOT, assembly);
                        }
                    }
                    // 將處理完成的組織單位提供給外部
                    return organizationuUnitDictionary;
                }
            }
            // 發生時機: 使用者登入時發現例外錯誤
            catch (DirectoryServicesCOMException exception)
            {
                // 解析例外字串, 並提供例外
                throw LDAPExceptions.OnNormalException(exception.ExtendedErrorMessage);
            }
            // 發生時機: 與伺服器連線時發現錯誤
            catch (COMException exception)
            {
                // 取得錯誤描述, 並提供例外
                throw LDAPExceptions.OnServeException(exception.Message);
            }
            // 沒有 Finally 語句
        }

        /// <summary>
        /// 提供功能集合的物件, 有若物件不支援有可能為空
        /// </summary>
        /// <param name="logon">登入者</param>
        /// <param name="destination">目標物件</param>
        /// <returns>提供異動功能證書</returns>
        /// <exception cref="ArgumentException">登入者或指定物件不存在時</exception>
        public LDAPCertification GetCertificate(in LDAPLogonPerson logon, in LDAPObject destination)
        {
            // 登入者不存在
            if (logon == null)
            {
                // 對外丟出例外: 因為重新命名需要檢查登入者的權限
                throw new ArgumentException("登入者不存在, 無法使用新對外顯示名稱重新命名指定的目標");
            }

            // 物件為空
            if (destination == null)
            {
                // 對外丟出例外: 因為提供的物件不存在, 此錯誤一般於外部操作過程中資料遺失
                throw new ArgumentException("指定物件不存在, 無法進行操作");
            }

            // 使用指定使用者帳號密碼製作一個入口物件製作器
            LDAPConfigurationDispatcher dispatcher = Configuration.Dispatch(logon.UserName, logon.Password);
            // 製作功能集合, 可能為空
            return new LDAPCertification(dispatcher, logon, destination);
        }
    }
}
