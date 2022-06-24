using ADService.Environments;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ADService.Media
{
    /// <summary>
    /// 創建伺服器連線資訊時同步宣告, 儲存伺服器連線相關資訊並提供呼叫方法
    /// </summary>
    internal class LDAPEntries
    {
        #region 通用搜尋字串
        /// <summary>
        /// 組成找尋指定區分名稱的過濾字串
        /// </summary>
        /// <param name="propertyName">目標欄位</param>
        /// <param name="values">限制的內容</param>
        /// <returns>過濾用字串, 沒有任何區分名稱需指定會提供空字串</returns>
        internal static string GetORFiliter(in string propertyName, params string[] values)
        {
            // 如果沒有任何區分名稱則不必進行過濾字串組合
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                // 返回空字串讓外部跳過搜尋動作
                return string.Empty;
            }

            // 轉乘小寫
            string nameLower = propertyName.ToLower();
            // 區分名稱的過濾內容: 此時會缺失開頭與結尾的部分
            string subFiliter = string.Join($")({nameLower}=", values);
            // 組成找尋任意一個與指定區分名稱相符的過濾字串
            return string.IsNullOrEmpty(subFiliter) ? string.Empty : $"(|({nameLower}={subFiliter}))";
        }

        /// <summary>
        /// 組成找尋指定區分名稱的過濾字串
        /// </summary>
        /// <param name="propertyName">目標欄位</param>
        /// <param name="values">限制的內容</param>
        /// <returns>過濾用字串, 沒有任何區分名稱需指定會提供空字串</returns>
        internal static string GetORFiliter(in string propertyName, in IEnumerable<string> values)
        {
            // 如果沒有任何區分名稱則不必進行過濾字串組合
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                // 返回空字串讓外部跳過搜尋動作
                return string.Empty;
            }

            // 轉乘小寫
            string nameLower = propertyName.ToLower();
            // 區分名稱的過濾內容: 此時會缺失開頭與結尾的部分
            string subFiliter = string.Join($")({nameLower}=", values);
            // 組成找尋任意一個與指定區分名稱相符的過濾字串
            return string.IsNullOrEmpty(subFiliter) ? string.Empty : $"(|({nameLower}={subFiliter}))";
        }
        #endregion

        #region 解析 LDAP 鍵值
        /// <summary>
        /// 解析目標鍵值
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static T ParseSingleValue<T>(in string propertyName, in PropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            PropertyValueCollection collection = properties[propertyName];
            // 不存在資料且強迫必須存在時
            if ((collection == null || collection.Count == 0))
            {
                // 此特性鍵值必須存在因而丟出例外
                return default(T);
            }

            // 對外提供轉換後型別
            return (T)collection.Value;
        }
        /// <summary>
        /// 解析目標鍵值
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static T[] ParseMutipleValue<T>(in string propertyName, in PropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            PropertyValueCollection collection = properties[propertyName];
            // 不存在資料且強迫必須存在時
            if ((collection == null || collection.Count == 0))
            {
                // 此特性鍵值必須存在因而丟出例外
                return Array.Empty<T>();
            }

            // 對外提供轉換後型別
            return Array.ConvertAll((object[])collection.Value, convertedObject => (T)convertedObject);
        }

        /// <summary>
        /// 解析目標鍵值
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static T ParseSingleValue<T>(in string propertyName, in ResultPropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            ResultPropertyValueCollection collection = properties[propertyName];
            // 不存在資料且強迫必須存在時
            if ((collection == null || collection.Count == 0))
            {
                // 此特性鍵值必須存在因而丟出例外
                return default(T);
            }

            // 對外提供轉換後型別
            return (T)collection[0];
        }

        /// <summary>
        /// 解析目標鍵值
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static T[] ParseMutipleValue<T>(in string propertyName, in ResultPropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            ResultPropertyValueCollection collection = properties[propertyName];
            // 不存在資料且強迫必須存在時
            if ((collection == null || collection.Count == 0))
            {
                // 此特性鍵值必須存在因而丟出例外
                return Array.Empty<T>();
            }

            // 宣告長度
            object[] copyOut = new object[collection.Count];
            // 將資料拷貝出來
            collection.CopyTo(copyOut, 0);
            // 對外提供轉換後型別
            return Array.ConvertAll(copyOut, convertedObject => (T)convertedObject);
        }

        /// <summary>
        /// 解析目標鍵值, 預期格式是 SID
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static string ParseSID(in string propertyName, in PropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            byte[] valueBytes = ParseSingleValue<byte[]>(propertyName, properties);
            // 不存在資料且強迫必須存在時
            if (valueBytes == null || valueBytes.Length == 0)
            {
                // 對外提供空字串
                return string.Empty;
            }

            // 對外提供的 SID 轉換器
            SecurityIdentifier convertor;
            // 資料為空
            if (valueBytes == null || valueBytes.Length == 0)
            {
                // 退外提供空的 SID
                convertor = new SecurityIdentifier(WellKnownSidType.NullSid, null);
            }
            else
            {
                // 需要透過 SecurityIdentifier 轉換成對應字串
                convertor = new SecurityIdentifier(valueBytes, 0);
            }
            // 對外提供轉換後型別
            return convertor.ToString();
        }
        /// <summary>
        /// 解析目標鍵值, 預期格式是 GUID
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="properties">整包的搜尋鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static string ParseGUID(in string propertyName, in PropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            byte[] valueBytes = ParseSingleValue<byte[]>(propertyName, properties);
            // 對外提供的 SID 轉換器
            Guid convertor;
            // 不存在資料且強迫必須存在時
            if (valueBytes == null || valueBytes.Length == 0)
            {
                // 退外提供空的 SID
                convertor = Guid.Empty;
            }
            else
            {
                // 需要透過 SecurityIdentifier 轉換成對應字串
                convertor = new Guid(valueBytes);
            }
            // 對外提供轉換後型別
            return convertor.ToString("D");
        }
        /// <summary>
        /// 解析目標鍵值, 預期格式是 GUID
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="properties">整包的搜尋鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static string ParseGUID(in string propertyName, in ResultPropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            byte[] valueBytes = ParseSingleValue<byte[]>(propertyName, properties);
            // 對外提供的 SID 轉換器
            Guid convertor;
            // 不存在資料且強迫必須存在時
            if (valueBytes == null || valueBytes.Length == 0)
            {
                // 退外提供空的 SID
                convertor = Guid.Empty;
            }
            else
            {
                // 需要透過 SecurityIdentifier 轉換成對應字串
                convertor = new Guid(valueBytes);
            }
            // 對外提供轉換後型別
            return convertor.ToString("D");
        }

        /// <summary>
        /// 解析 <see cref="C_OBJECTCATEGORY">物件類型</see> 的鍵值容器
        /// </summary>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得的 <see cref="C_OBJECTCATEGORY">物件類型</see> 鍵值內容</returns>
        internal static CategoryTypes ParseCategory(in PropertyCollection properties)
        {
            // 取得 '物件類型' 特性鍵值內容
            string categoryDistinguishedName = ParseSingleValue<string>(Properties.C_OBJECTCATEGORY, properties);
            // 解析取得的區分名稱來得到物件類型
            return GetObjectType(categoryDistinguishedName);
        }
        /// <summary>
        /// 根據解析物件類型區分名稱來提供物件為何種類型
        /// </summary>
        /// <param name="categoryDistinguishedName">需解析區分名稱</param>
        /// <returns></returns>
        /// <exception cref="LDAPExceptions">區分名稱無法正常解析實對外丟出</exception>
        internal static CategoryTypes GetObjectType(in string categoryDistinguishedName)
        {
            // 用來切割物件類型的字串
            string[] splitElements = new string[] { $"{Properties.P_DC.ToUpper()}=", $"{Properties.P_OU.ToUpper()}=", $"{Properties.P_CN.ToUpper()}=" };
            // 切割物件類型
            string[] elements = categoryDistinguishedName.Split(splitElements, StringSplitOptions.RemoveEmptyEntries);

            // 第一個參數為物件類型的描述: 但是需要物件類型的長度決定如何處理
            string category = string.Empty;
            // 物件類型長度不可能比 1 少, 但是為了防呆還是增加此邏輯判斷
            if (elements.Length >= 1)
            {
                // 第一個元素必定是物件類型
                string elementFirst = elements[0];
                // 如果物件類型字串解析後長度比 1 大, 則第一個元素後面會多一個 ',' 會需要被移除
                category = elements.Length > 1 ? elementFirst.Substring(0, elementFirst.Length - 1) : elementFirst;
            }

            // 透過描述取得物件類型描述
            Dictionary<string, CategoryTypes> result = LDAPCategory.GetTypeByCategories(category);
            // 物件類型不存在
            if (!result.TryGetValue(category, out CategoryTypes type))
            {
                // 對外丟出例外: 未實做邏輯錯誤
                throw new LDAPExceptions($"未實作解析資訊:{Properties.C_OBJECTCATEGORY} 儲存的內容:{categoryDistinguishedName}", ErrorCodes.LOGIC_ERROR);
            }
            // 存在時對外提供物件類型
            return type;
        }
        #endregion

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
