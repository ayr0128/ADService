using ADService.Environments;
using ADService.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using System.Text;

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

            // 區分名稱的過濾內容: 此時會缺失開頭與結尾的部分
            string subFiliter = string.Join($")({propertyName}=", values);
            // 組成找尋任意一個與指定區分名稱相符的過濾字串
            return string.IsNullOrEmpty(subFiliter) ? string.Empty : $"(|({propertyName}={subFiliter}))";
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

            // 區分名稱的過濾內容: 此時會缺失開頭與結尾的部分
            string subFiliter = string.Join($")({propertyName}=", values);
            // 組成找尋任意一個與指定區分名稱相符的過濾字串
            return string.IsNullOrEmpty(subFiliter) ? string.Empty : $"(|({propertyName}={subFiliter}))";
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
        /// 解析目標鍵值, 預期格式是 SID
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static string ParseSID(in string propertyName, in ResultPropertyCollection properties)
        {
            // 搜尋而來的參數應使用小寫進行分解
            string propertyNameLower = propertyName.ToLower();
            // 取得指定特性鍵值內容
            byte[] valueBytes = ParseSingleValue<byte[]>(propertyNameLower, properties);
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
            // 搜尋而來的參數應使用小寫進行分解
            string propertyNameLower = propertyName.ToLower();
            // 取得指定特性鍵值內容
            byte[] valueBytes = ParseSingleValue<byte[]>(propertyNameLower, properties);
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
        /// 藍本額外權限
        /// </summary>
        private sealed class PropertySet
        {
            /// <summary>
            /// 啟用時間
            /// </summary>
            private readonly DateTime EnableTime = DateTime.UtcNow;
            /// <summary>
            /// 關聯額外權限
            /// </summary>
            internal readonly HashSet<string> GUIDHashSet = new HashSet<string>();

            /// <summary>
            /// 是否已經超過保留時間
            /// </summary>
            /// <param name="duration"></param>
            /// <returns>是否過期</returns>
            internal bool IsExpired(in TimeSpan duration) => (DateTime.UtcNow - EnableTime) >= duration;
        }

        /// <summary>
        /// 設定入口物件位置
        /// </summary>
        internal const string CONTEXT_CONFIGURATION = "configurationNamingContext";
        /// <summary>
        /// 類別與屬性入口物件位置
        /// </summary>
        internal const string CONTEXT_SUBSCHENA = "subschemaSubentry";
        /// <summary>
        /// 資料過期時間, 可以考慮由外部設置
        /// </summary>
        private static TimeSpan EXPIRES_DURATION = TimeSpan.FromMinutes(5);

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
        /// <list type="table|bullet">
        ///     <item> <see href="https://docs.microsoft.com/en-us/windows/win32/adschema/property-sets">組合式權限</see> </item>
        ///     <item> <see href="https://docs.microsoft.com/en-us/windows/win32/adschema/attributes">屬性定義</see> </item>
        ///     <item> <see href="https://docs.microsoft.com/en-us/windows/win32/adschema/extended-rights">額外權限定義</see> </item>
        /// </list>
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
        /// <param name="enumerableGUID">目標 GUID 陣列</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchema[] GetSchema(in LDAPConfigurationDispatcher dispatcher, in IEnumerable<Guid> enumerableGUID)
        {
            // 使用查詢數量作為容器大小
            HashSet<string> researchConvertedGUIDs = new HashSet<string>();
            // 對襪提供的查詢資料
            List<UnitSchema> unitSchemas = new List<UnitSchema>();
            // 使用文字串流來推入 GUID
            StringBuilder sb = new StringBuilder();
            // 遍歷所有須查詢的 GUID
            foreach (Guid guid in enumerableGUID)
            {
                // 檢查是否微空GUID
                if (guid == Guid.Empty)
                {
                    // 跳過
                    continue;
                }

                // 轉成小寫
                string schemaGUIDLower = guid.ToString("D").ToLower();
                /* 符合下述規則時重新搜尋
                     1. 資料不存在
                     2. 存在超過預計時間
                */
                if (!dictionaryGUIDWithUnitSchema.TryGetValue(schemaGUIDLower, out UnitSchema result) || result.IsExpired(EXPIRES_DURATION))
                {
                    // 遍歷位元組
                    foreach (byte convertRequired in guid.ToByteArray())
                    {
                        // 轉化各位元組至十六進位
                        sb.Append($"\\{convertRequired:X2}");
                    }
                    // 轉換成查詢格式
                    researchConvertedGUIDs.Add(sb.ToString());
                }
                else
                {
                    // 未過期時直接提供給外部查詢
                    unitSchemas.Add(result);
                }

                // 每個 GUID 後就清空一次串流
                sb.Clear();
            }

            // 找尋目標藍本結構
            if (researchConvertedGUIDs.Count != 0)
            {
                // 重新建立藍本結構
                foreach (UnitSchema unitSchema in UnitSchema.GetWithGUID(dispatcher, researchConvertedGUIDs))
                {
                    // 更新的 GUID 字串必須是小寫
                    string schemaGUIDLower = unitSchema.SchemaGUID.ToLower();
                    // 避免託管記憶體洩漏
                    UnitSchema newUnitSchema = unitSchema;
                    // 先移除舊的資料
                    dictionaryGUIDWithUnitSchema.AddOrUpdate(
                        schemaGUIDLower,
                        newUnitSchema,
                        (GUID, oldUnitSchema) => oldUnitSchema.IsExpired(EXPIRES_DURATION) ? newUnitSchema : oldUnitSchema
                    );

                    // 設置成對外提供項目
                    unitSchemas.Add(unitSchema);
                }
            }
            // 對外提供取得的資料: 注意可能為空
            return unitSchemas.ToArray();
        }

        /// <summary>
        /// 使用展示名稱 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="attributeNames">屬性名稱</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchema[] GetSchema(in LDAPConfigurationDispatcher dispatcher, params string[] attributeNames)
        {
            // 組成名稱對應藍本的字典
            Dictionary<string, UnitSchema> dictionaryNameWithUnitSchema = new Dictionary<string, UnitSchema>();
            // 比對展示名稱作為鍵值的字典: 透過本身持有的功能轉換成陣列, 避免多執行續狀況下的錯誤
            foreach (KeyValuePair<string, UnitSchema> pair in dictionaryGUIDWithUnitSchema.ToArray())
            {
                // 轉成強型別方便閱讀
                UnitSchema schema = pair.Value;

                // 轉換記錄格是
                dictionaryNameWithUnitSchema.Add(schema.Name, schema);
            }

            // 不需重新搜尋的項目
            List<UnitSchema> unitSchemas = new List<UnitSchema>(attributeNames.Length);
            // 需重新搜尋的項目
            List<string> researchNames = new List<string>(attributeNames.Length);
            // 遍歷希望查詢的物件
            foreach (string attributeName in attributeNames)
            {
                // 存在且未過期
                if (dictionaryNameWithUnitSchema.TryGetValue(attributeName, out UnitSchema schema) && !schema.IsExpired(EXPIRES_DURATION))
                {
                    // 推入對外提供項目
                    unitSchemas.Add(schema);
                }
                else
                {
                    // 推入充新找尋提供項目
                    researchNames.Add(attributeName);
                }
            }

            // 存在需重新找尋項目時才執行
            if (researchNames.Count != 0)
            {
                // 重新建立藍本結構
                UnitSchema[] newUnitSchemas = UnitSchema.GetWithName(dispatcher, researchNames);
                // 籌溝死得時
                if (newUnitSchemas.Length != researchNames.Count)
                {
                    // 拋出例外
                    throw new LDAPExceptions($"藍本名稱:{string.Join(",", researchNames)} 無法在相關網域:{dispatcher.Configuration} 中被發現, 請聯絡程式維護人員", ErrorCodes.SERVER_ERROR);
                }

                foreach (UnitSchema unitSchema in newUnitSchemas)
                {
                    // 避免託管記憶體洩漏
                    string schemaGUIDLower = unitSchema.SchemaGUID.ToLower();
                    // 避免託管記憶體洩漏
                    UnitSchema newUnitSchema = unitSchema;
                    // 先移除舊的資料
                    dictionaryGUIDWithUnitSchema.AddOrUpdate(
                        schemaGUIDLower,
                        newUnitSchema,
                        (GUID, oldUnitSchema) => oldUnitSchema.IsExpired(EXPIRES_DURATION) ? newUnitSchema : oldUnitSchema
                    );

                    // 設置成對外提供項目
                    unitSchemas.Add(unitSchema);
                }
            }
            // 對外提供取得的資料: 注意可能為空
            return unitSchemas.ToArray();
        }

        /// <summary>
        /// 安全屬性 GUID 與相關屬性職
        /// </summary>
        private readonly ConcurrentDictionary<string, PropertySet> dictionaryExtendedRightGUIDWithPropertySet = new ConcurrentDictionary<string, PropertySet>();

        /// <summary>
        /// 使用 GUID 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="unitExtendedRight">額外權限</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchema[] GetPropertySet(in LDAPConfigurationDispatcher dispatcher, in UnitExtendedRight unitExtendedRight)
        {
            // 檢查是否微空GUID
            if (unitExtendedRight == null)
            {
                // 對外提供空陣列
                return Array.Empty<UnitSchema>();
            }

            // 轉成小寫
            string securityGUIDLower = unitExtendedRight.GUID.ToLower();
            /* 符合下述規則時重新搜尋
                 1. 資料不存在
                 2. 存在超過預計時間
            */
            if (!dictionaryExtendedRightGUIDWithPropertySet.TryGetValue(securityGUIDLower, out PropertySet propertySet) || propertySet.IsExpired(EXPIRES_DURATION))
            {
                // 提供給外部處理
                PropertySet newPropertySet = new PropertySet();

                // 轉換成 GUID
                Guid securityGUID = new Guid(unitExtendedRight.GUID);
                // 重新建立藍本結構
                foreach (UnitSchema unitSchema in UnitSchema.GetWithSecurityGUID(dispatcher, securityGUID))
                {
                    // 更新的 GUID 字串必須是小寫
                    string extendedRightGUIDLower = unitSchema.SchemaGUID.ToLower();
                    // 先移除舊的資料
                    dictionaryGUIDWithUnitSchema.AddOrUpdate(
                        extendedRightGUIDLower,
                        unitSchema,
                        (GUID, oldUnitSchema) => oldUnitSchema.IsExpired(EXPIRES_DURATION) ? unitSchema : oldUnitSchema
                    );

                    // 設置成對外提供項目
                    newPropertySet.GUIDHashSet.Add(extendedRightGUIDLower);
                }

                // 更新時使用的是目標鍵值
                dictionaryExtendedRightGUIDWithPropertySet.AddOrUpdate(
                    securityGUIDLower,
                    newPropertySet,
                    (unitSchemaGUIDLower, oldPropertySet) => oldPropertySet.IsExpired(EXPIRES_DURATION) ? newPropertySet : oldPropertySet
                );

                // 提供給外部使用
                propertySet = newPropertySet;
            }

            // 使用屬性 GUID 的長度作為容器大小
            List<UnitSchema> unitSchemas = new List<UnitSchema>(propertySet.GUIDHashSet.Count);
            // 遍歷集成並將資料對外提供
            foreach (string unitSchemaGUIDLower in propertySet.GUIDHashSet)
            {
                // 先移除舊的資料
                if (!dictionaryGUIDWithUnitSchema.TryGetValue(unitSchemaGUIDLower, out UnitSchema unitSchema))
                {
                    // 必定能取得: 此處為簡易防呆
                    continue;
                }

                // 設置成對外提供項目
                unitSchemas.Add(unitSchema);
            }
            // 對外提供取得的資料: 注意可能為空
            return unitSchemas.ToArray();
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
        /// <param name="enumerableGUID">目標 GUID 陣列</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitExtendedRight GetExtendedRight(in LDAPConfigurationDispatcher dispatcher, in Guid guid)
        {
            // 轉成小寫
            string unitExtendedRightGUIDLower = guid.ToString("D").ToLower();
            /* 符合下述規則時重新搜尋
                 1. 資料不存在
                 2. 存在超過預計時間
            */
            bool research = !dictionaryGUIDWithUnitExtendedRight.TryGetValue(unitExtendedRightGUIDLower, out UnitExtendedRight result) || result.IsExpired(EXPIRES_DURATION);
            // 找尋目標額萬權限結構
            if (research)
            {
                // 重新建立藍本結構
                UnitExtendedRight unitExtendedRight = UnitExtendedRight.GetWithGUID(dispatcher, unitExtendedRightGUIDLower);
                // 如果不存在此 GUID
                if (unitExtendedRight == null)
                {
                    // 拋出例外
                    throw new LDAPExceptions($"額外權限:{unitExtendedRightGUIDLower} 無法在相關網域:{dispatcher.Configuration} 中被發現, 請聯絡程式維護人員", ErrorCodes.SERVER_ERROR);
                }

                // 先移除舊的資料
                dictionaryGUIDWithUnitExtendedRight.AddOrUpdate(
                    unitExtendedRightGUIDLower,
                    unitExtendedRight,
                    (GUID, oldUnitExtendedRight) => oldUnitExtendedRight.IsExpired(EXPIRES_DURATION) ? unitExtendedRight : oldUnitExtendedRight
                );

                result = unitExtendedRight;
            }
            // 對外提供取得的資料: 注意可能為空
            return result;
        }

        /// <summary>
        /// 使用 GUID 進行搜尋指定目標額外權限物件
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="enumerableGUID">目標 GUID 陣列</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitExtendedRight[] GetExtendedRight(in LDAPConfigurationDispatcher dispatcher, in IEnumerable<Guid> enumerableGUID)
        {
            // 使用查詢數量作為容器大小
            HashSet<string> researchGUIDs = new HashSet<string>();
            // 對襪提供的查詢資料
            List<UnitExtendedRight> unitExtendedRights = new List<UnitExtendedRight>();
            // 遍歷所有須查詢的 GUID
            foreach (Guid guid in enumerableGUID)
            {
                // 檢查是否微空GUID
                if (guid == Guid.Empty)
                {
                    // 跳過
                    continue;
                }

                // 轉成小寫
                string schemaGUIDLower = guid.ToString("D").ToLower();
                /* 符合下述規則時重新搜尋
                     1. 資料不存在
                     2. 存在超過預計時間
                */
                if (!dictionaryGUIDWithUnitExtendedRight.TryGetValue(schemaGUIDLower, out UnitExtendedRight result) || result.IsExpired(EXPIRES_DURATION))
                {
                    // 轉換成查詢格式
                    researchGUIDs.Add(schemaGUIDLower);
                }
                else
                {
                    // 未過期時直接提供給外部查詢
                    unitExtendedRights.Add(result);
                }
            }

            // 找尋目標額萬權限結構
            if (researchGUIDs.Count != 0)
            {
                // 重新建立藍本結構
                foreach (UnitExtendedRight unitExtendedRight in UnitExtendedRight.GetWithGUID(dispatcher, researchGUIDs))
                {
                    // 更新的 GUID 字串必須是小寫
                    string extendedRightGUIDLower = unitExtendedRight.GUID.ToLower();
                    // 避免託管記憶體洩漏
                    UnitExtendedRight newUnitExtendedRight = unitExtendedRight;
                    // 先移除舊的資料
                    dictionaryGUIDWithUnitExtendedRight.AddOrUpdate(
                        extendedRightGUIDLower,
                        newUnitExtendedRight,
                        (GUID, oldUnitExtendedRight) => oldUnitExtendedRight.IsExpired(EXPIRES_DURATION) ? newUnitExtendedRight : oldUnitExtendedRight
                    );

                    // 設置成對外提供項目
                    unitExtendedRights.Add(unitExtendedRight);
                }
            }
            // 對外提供取得的資料: 注意可能為空
            return unitExtendedRights.ToArray();
        }

        /// <summary>
        /// 藍本 GUID 與 額外權限
        /// </summary>
        private readonly ConcurrentDictionary<string, PropertySet> dictionarySchemaGUIDWithPropertySet = new ConcurrentDictionary<string, PropertySet>();

        /// <summary>
        /// 透過展示名稱取額指定額外全縣
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="unitSchemas">查詢的藍本</param>
        /// <returns>指定額外權限物件, 可能不存在</returns>
        internal UnitExtendedRight[] GetExtendedRight(in LDAPConfigurationDispatcher dispatcher, params UnitSchema[] unitSchemas)
        {
            // 即將用來搜尋的字典
            Dictionary<string, HashSet<string>> dictionaryAttributeNameWithValues = new Dictionary<string, HashSet<string>>();
            // 轉換成物件類型 GUID 與是否過期的字典
            foreach (UnitSchema unitSchema in unitSchemas)
            {
                // 查詢時須使用小寫的 GUID
                string searcherSchenaGUID = unitSchema.SchemaGUID.ToLower();
                // 取得藍本額外權限關聯
                if (dictionarySchemaGUIDWithPropertySet.TryGetValue(searcherSchenaGUID, out PropertySet propertySet) && !propertySet.IsExpired(EXPIRES_DURATION))
                {
                    // 已存在且未過期
                    continue;
                }

                // 組成應搜尋出更新物件的字典黨
                unitSchema.CombineFiliter(ref dictionaryAttributeNameWithValues);
            }

            // 找尋目標藍本結構
            if (dictionaryAttributeNameWithValues.Count != 0)
            {
                // 即將用來搜尋的字典
                Dictionary<string, PropertySet> dictionarySchemaGUIDWithPropertySetCache = new Dictionary<string, PropertySet>();
                // 這些資料都常是進行刷新
                foreach (UnitExtendedRight newUnitExtendedRight in UnitExtendedRight.GetWithPropertySet(dispatcher, dictionaryAttributeNameWithValues))
                {
                    // 查詢時須使用小寫的 GUID
                    string unitExtendedRightGUIDLower = newUnitExtendedRight.GUID.ToLower();
                    // 更新相關資訊
                    dictionaryGUIDWithUnitExtendedRight.AddOrUpdate(
                        unitExtendedRightGUIDLower,
                        newUnitExtendedRight,
                        // 此時無論是否過期都強制設置
                        (GUID, oldUnitExtendedRight) => newUnitExtendedRight
                    );

                    // 遍歷藍本物件進行檢查
                    foreach (UnitSchema unitSchema in unitSchemas)
                    {
                        // 預設狀況時不必處理
                        if (unitSchema.GetPorpertyType(newUnitExtendedRight) == PropertytFlags.NONE)
                        {
                            // 不是則跳過
                            continue;
                        }

                        // 轉成查詢與推入用的小寫字串
                        string unitSchemaGUIDLower = unitSchema.SchemaGUID.ToLower();
                        // 取得目前集成
                        if (!dictionarySchemaGUIDWithPropertySetCache.TryGetValue(unitSchemaGUIDLower, out PropertySet propertySet))
                        {
                            // 不存在時沖新宣告
                            propertySet = new PropertySet();
                            // 並推入
                            dictionarySchemaGUIDWithPropertySetCache.Add(unitSchemaGUIDLower, propertySet);
                        }

                        // 推入作為查詢用物件
                        propertySet.GUIDHashSet.Add(newUnitExtendedRight.GUID);
                    }
                }

                // 更新資料
                foreach (KeyValuePair<string, PropertySet> pair in dictionarySchemaGUIDWithPropertySetCache)
                {
                    // 避免託管記憶體洩漏
                    PropertySet newPropertySet = pair.Value;
                    // 避免託管記憶體洩漏
                    string schemaGUIDLower = pair.Key;
                    // 更新時使用的是目標鍵值
                    dictionarySchemaGUIDWithPropertySet.AddOrUpdate(
                        schemaGUIDLower,
                        newPropertySet,
                        (unitSchemaGUIDLower, oldPropertySet) => oldPropertySet.IsExpired(EXPIRES_DURATION) ? newPropertySet : oldPropertySet
                    );
                }
            }

            // 最多為目前額外權限的大小
            Dictionary<string, UnitExtendedRight> dictionaryGUIDWithUnitExtendedRightResult = new Dictionary<string, UnitExtendedRight>();
            // 轉換成物件類型 GUID 與是否過期的字典
            foreach (UnitSchema unitSchema in unitSchemas)
            {
                // 將藍本物件 GUID 轉乘小寫
                string unitSchemaGUIDLower = unitSchema.SchemaGUID.ToLower();
                // 找到關聯物件列表 (不必檢查是否過期)
                if (!dictionarySchemaGUIDWithPropertySet.TryGetValue(unitSchemaGUIDLower, out PropertySet propertySet))
                {
                    // 不存在就跳過: 極少觸發, 不持有權限的欄位不必開放查看
                    continue;
                }

                // 遍歷此
                foreach (string unitExtendedRightGUID in propertySet.GUIDHashSet)
                {
                    // 將屬性組 GUID 轉乘小寫
                    string unitExtendedRightGUIDLower = unitExtendedRightGUID.ToLower();
                    // 檢查是否以推入過
                    if (dictionaryGUIDWithUnitExtendedRightResult.ContainsKey(unitExtendedRightGUIDLower))
                    {
                        // 已包含則跳過
                        continue;
                    }

                    // 找到目標屬性組 GUID 相關額外權限
                    if (!dictionaryGUIDWithUnitExtendedRight.TryGetValue(unitExtendedRightGUIDLower, out UnitExtendedRight unitExtendedRight))
                    {
                        // 無罰找到也跳過
                        continue;
                    }

                    // 找到時推入作為外部可用權限
                    dictionaryGUIDWithUnitExtendedRightResult.Add(unitExtendedRightGUIDLower, unitExtendedRight);
                }
            }
            // 對外提供取得的資料: 注意可能為空
            return dictionaryGUIDWithUnitExtendedRightResult.Values.ToArray();
        }
        #endregion

    }
}
