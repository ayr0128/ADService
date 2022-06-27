using ADService.Environments;
using ADService.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace ADService.Media
{
    /// <summary>
    /// 內部使用的存取權限轉換器
    /// </summary>
    internal sealed class LDAPConfiguration
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
        /// 設定入口物件位置
        /// </summary>
        internal const string CONTEXT_CONFIGURATION = "configurationNamingContext";
        /// <summary>
        /// 資料過期時間, 可以考慮由外部設置
        /// </summary>
        internal const long EXPIRES_TIME = TimeSpan.TicksPerMinute * 5;

        /// <summary>
        /// 是否為空 GUID
        /// </summary>
        /// <param name="value">檢查 GUID </param>
        /// <returns>是否為空</returns>
        internal static bool IsGUIDEmpty(in Guid value) => value.Equals(Guid.Empty);
        /// <summary>
        /// 是否為空 GUID
        /// </summary>
        /// <param name="value">檢查 GUID </param>
        /// <returns>是否為空</returns>
        internal static bool IsGUIDEmpty(in string value) => !Guid.TryParse(value, out Guid convertedValue) || IsGUIDEmpty(convertedValue);

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
        internal LDAPConfiguration(string domain, ushort port)
        {
            Domain = domain;
            Port = port;
        }

        /// <summary>
        /// 創建一個獨立的執行續安全分配器
        /// </summary>
        /// <param name="userName">使用者名稱</param>
        /// <param name="password">使用者密碼</param>
        /// <returns>執行續安全的設定取得結構</returns>
        internal LDAPConfigurationDispatcher Dispatch(in string userName, in string password) => new LDAPConfigurationDispatcher(userName, password, this);

        #region 取得藍本物件
        /// <summary>
        /// 藍本物件陣列
        /// </summary>
        private readonly ConcurrentDictionary<string, UnitSchema> dictionaryGUIDWithUnitSchema = new ConcurrentDictionary<string, UnitSchema>();

        /// <summary>
        /// 使用 GUID 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="value">目標 GUID</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchema GetSchema(in LDAPConfigurationDispatcher dispatcher, in Guid value)
        {
            // 由於儲存結構中醫慮採用小寫, 所以搜尋參數於搜尋前須改為小寫
            string valueGUID = value.ToString("D").ToLower();
            /* 符合下述規則時重新搜尋
                 1. 資料不存在
                 2. 存在超過預計時間
            */
            bool needResearch = !dictionaryGUIDWithUnitSchema.TryGetValue(valueGUID, out UnitSchema result) || (DateTime.UtcNow - result.EnableTime).Ticks > EXPIRES_TIME;
            // 找尋目標藍本結構
            if (!IsGUIDEmpty(value) && needResearch)
            {
                // 重新建立藍本結構
                UnitSchema newValue = UnitSchema.Get(dispatcher, value);
                // 籌溝死得時
                if (newValue != null)
                {
                    // 先移除舊的資料
                    dictionaryGUIDWithUnitSchema.TryRemove(valueGUID, out _);
                    // 使用新增過更新動作
                    dictionaryGUIDWithUnitSchema.TryAdd(valueGUID, newValue);
                }
                // 將舊有資料換成新的
                result = newValue;
            }
            // 對外提供取得的資料: 注意可能為空
            return result;
        }

        /// <summary>
        /// 使用展示名稱 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="value">展示名稱</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchema GetSchema(in LDAPConfigurationDispatcher dispatcher, in string value)
        {
            // 由於儲存結構中醫慮採用小寫, 所以搜尋參數於搜尋前須改為小寫
            string valueLower = value.ToLower();
            // 透過本身持有的功能轉換成陣列, 避免多執行續狀況下的錯誤
            KeyValuePair<string, UnitSchema>[] pairs = dictionaryGUIDWithUnitSchema.ToArray();
            // 轉換成根據查詢結構: 若此處出現錯誤則必定是羅錯誤導致加入重複物件
            Dictionary<string, UnitSchema> dictionaryNameWithUnitSchema = new Dictionary<string, UnitSchema>(pairs.Length);
            // 初始化使用展示名稱作為鍵值的字典
            foreach (KeyValuePair<string, UnitSchema> pair in pairs)
            {
                // 設置舊資料至本次茶燻需使用的字典
                dictionaryNameWithUnitSchema.Add(pair.Key, pair.Value);
            }

            /* 符合下述規則時重新搜尋
                 1. 資料不存在
                 2. 存在超過預計時間
            */
            bool needResearch = !dictionaryNameWithUnitSchema.TryGetValue(valueLower, out UnitSchema result) || (DateTime.UtcNow - result.EnableTime).Ticks > EXPIRES_TIME;
            // 找尋目標藍本結構
            if (!string.IsNullOrWhiteSpace(valueLower) && needResearch)
            {
                // 重新建立藍本結構
                UnitSchema newValue = UnitSchema.Get(dispatcher, valueLower);
                // 籌溝死得時
                if (newValue != null)
                {
                    // 先移除舊的資料
                    dictionaryGUIDWithUnitSchema.TryRemove(newValue.SchemaGUID, out _);
                    // 使用新增過更新動作
                    dictionaryGUIDWithUnitSchema.TryAdd(newValue.SchemaGUID, newValue);
                }
                // 將舊有資料換成新的
                result = newValue;
            }
            // 對外提供取得的資料: 注意可能為空
            return result;
        }
        #endregion

        #region 取得額外權限
        /// <summary>
        /// 額外權限陣列
        /// </summary>
        private readonly ConcurrentDictionary<string, UnitExtendedRight> dictionaryGUIDWithUnitExtendedRight = new ConcurrentDictionary<string, UnitExtendedRight>();

        /// <summary>
        /// 使用 GUID 進行搜尋指定目標額外權限物件
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="value">目標 GUID</param>
        /// <returns>指定額外權限物件, 可能不存在</returns>
        internal UnitExtendedRight GetExtendedRight(in LDAPConfigurationDispatcher dispatcher, in Guid value)
        {
            // 由於儲存結構中醫慮採用小寫, 所以搜尋參數於搜尋前須改為小寫
            string valueGUID = value.ToString("D").ToLower();
            /* 符合下述規則時重新搜尋
                 1. 資料不存在
                 2. 存在超過預計時間
            */
            bool needResearch = !dictionaryGUIDWithUnitExtendedRight.TryGetValue(valueGUID, out UnitExtendedRight result) || (DateTime.UtcNow - result.EnableTime).Ticks > EXPIRES_TIME;
            // 找尋目標額外權限
            if (!IsGUIDEmpty(value) && needResearch)
            {
                // 重新建立藍本結構
                UnitExtendedRight newValue = UnitExtendedRight.Get(dispatcher, value);
                // 籌溝死得時
                if (newValue != null)
                {
                    // 先移除舊的資料
                    dictionaryGUIDWithUnitExtendedRight.TryRemove(valueGUID, out _);
                    // 使用新增過更新動作
                    dictionaryGUIDWithUnitExtendedRight.TryAdd(valueGUID, newValue);
                }
                // 將舊有資料換成新的
                result = newValue;
            }
            // 對外提供取得的資料: 注意可能為空
            return result;
        }

        /// <summary>
        /// 透過展示名稱取額指定額外全縣
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="value">展示名稱</param>
        /// <returns>指定額外權限物件, 可能不存在</returns>
        internal UnitExtendedRight GetExtendedRight(in LDAPConfigurationDispatcher dispatcher, in string value)
        {
            // 由於儲存結構中醫慮採用小寫, 所以搜尋參數於搜尋前須改為小寫
            string valueLower = value.ToLower();
            // 透過本身持有的功能轉換成陣列, 避免多執行續狀況下的錯誤
            KeyValuePair<string, UnitExtendedRight>[] pairs = dictionaryGUIDWithUnitExtendedRight.ToArray();
            // 轉換成根據查詢結構: 若此處出現錯誤則必定是羅錯誤導致加入重複物件
            Dictionary<string, UnitExtendedRight> dictionaryNameWithUnitExtendedRight = new Dictionary<string, UnitExtendedRight>(pairs.Length);
            // 初始化使用展示名稱作為鍵值的字典
            foreach (KeyValuePair<string, UnitExtendedRight> pair in pairs)
            {
                // 設置舊資料至本次茶燻需使用的字典
                dictionaryNameWithUnitExtendedRight.Add(pair.Key, pair.Value);
            }

            /* 符合下述規則時重新搜尋
                 1. 資料不存在
                 2. 存在超過預計時間
            */
            bool needResearch = !dictionaryNameWithUnitExtendedRight.TryGetValue(valueLower, out UnitExtendedRight result) || (DateTime.UtcNow - result.EnableTime).Ticks > EXPIRES_TIME;
            // 找尋目標藍本結構
            if (!string.IsNullOrWhiteSpace(valueLower) && needResearch)
            {
                // 重新建立藍本結構
                UnitExtendedRight newValue = UnitExtendedRight.Get(dispatcher, valueLower);
                // 籌溝死得時
                if (newValue != null)
                {
                    // 先移除舊的資料
                    dictionaryGUIDWithUnitExtendedRight.TryRemove(newValue.RightsGUID, out _);
                    // 使用新增過更新動作
                    dictionaryGUIDWithUnitExtendedRight.TryAdd(newValue.RightsGUID, newValue);
                }
                // 將舊有資料換成新的
                result = newValue;
            }
            // 對外提供取得的資料: 注意可能為空
            return result;
        }
        #endregion
    }
}
