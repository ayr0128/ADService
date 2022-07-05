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
                return default;
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

            // 宣告長度
            object[] copyOut = new object[collection.Count];
            // 將資料拷貝出來
            collection.CopyTo(copyOut, 0);
            // 對外提供轉換後型別
            return Array.ConvertAll(copyOut, convertedObject => (T)convertedObject);
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
                return default;
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
        /// 解析 <see cref="Properties.C_OBJECTCATEGORY">物件類型</see> 的鍵值容器
        /// </summary>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得的 <see cref="Properties.C_OBJECTCATEGORY">物件類型</see> 鍵值內容</returns>
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
            internal readonly HashSet<string> GUIDHashSet;

            /// <summary>
            /// 是否已經超過保留時間
            /// </summary>
            /// <param name="duration"></param>
            /// <returns>是否過期</returns>
            internal bool IsExpired(in TimeSpan duration) => (DateTime.UtcNow - EnableTime) >= duration;

            /// <summary>
            /// 金退入外部設硬的關聯設定
            /// </summary>
            /// <param name="guids">關聯的 GUID</param>
            internal PropertySet(IEnumerable<string> guids) => GUIDHashSet = new HashSet<string>(guids);
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
        /// 藍本物件的 GUID 與可作為期子層的類型藍本物件集合
        /// </summary>
        private readonly ConcurrentDictionary<string, PropertySet> dictionarySchemaClassGUIDWithChildrenUnitSchemaClassGUIDs = new ConcurrentDictionary<string, PropertySet>();

        /// <summary>
        /// 使用展示名稱 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="lDAPDisplayNames">屬性名稱</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchemaAttribute[] GetUnitSchemaAttribute(in LDAPConfigurationDispatcher dispatcher, params string[] lDAPDisplayNames)
        {
            // 最大長度必定為執行續安全字典的長度
            Dictionary<string, UnitSchemaAttribute> dictionaryLDAPDisplayNameWithUnitSchemaAttribute = new Dictionary<string, UnitSchemaAttribute>(dictionaryGUIDWithUnitSchema.Count);
            // 將執行續安全的字典轉成陣列
            foreach (KeyValuePair<string, UnitSchema> pair in dictionaryGUIDWithUnitSchema.ToArray())
            {
                // 本次檢查的目標是物件藍本: 將基底藍本轉換成物件藍本
                // 是否能正確轉換成物件藍本
                if (!(pair.Value is UnitSchemaAttribute unitSchemaAttribute))
                {
                    // 只要無法轉換成物件藍本則都可以跳過
                    continue;
                }

                // 轉換記錄格是
                dictionaryLDAPDisplayNameWithUnitSchemaAttribute.Add(unitSchemaAttribute.Name, unitSchemaAttribute);
            }

            // 避免重複用
            HashSet<string> researchedGUIDs = new HashSet<string>(lDAPDisplayNames.Length);
            // 避免重複用與找尋用
            HashSet<string> researchedLDAPDisplayNames = new HashSet<string>(lDAPDisplayNames.Length);
            // 查詢之前是否已持有並停時過濾檢查
            foreach (string ldapDisplayName in lDAPDisplayNames)
            {
                // 能找到指定名稱且尚未過期
                if (dictionaryLDAPDisplayNameWithUnitSchemaAttribute.TryGetValue(ldapDisplayName, out UnitSchemaAttribute unitSchemaAttribute) && !unitSchemaAttribute.IsExpired(EXPIRES_DURATION))
                {
                    // 找尋用的 GUID 必須微小寫
                    string schemaGUIDLower = unitSchemaAttribute.SchemaGUID.ToLower();
                    // 則加入最後的 GUID 搜尋
                    researchedGUIDs.Add(schemaGUIDLower);
                    // 跳過
                    continue;
                }

                /* 直行至此則必定下列條件須滿足
                     - 下述條件滿足其一:
                       1. 無法找到指定展示名稱的物件藍本
                       2. 能找到但是物件藍本已過期
                     - 物件尚未被推入不重複重新搜尋陣列 
                */
                researchedLDAPDisplayNames.Add(ldapDisplayName);
            }

            // 存在需重新搜尋的不重複物件展示名稱
            if (researchedLDAPDisplayNames.Count != 0)
            {
                // 重新建立藍本結構
                foreach (UnitSchemaAttribute unitSchemaAttribute in UnitSchemaAttribute.GetWithLDAPDisplayNames(dispatcher, researchedLDAPDisplayNames))
                {
                    // 更新的 GUID 字串必須是小寫
                    string schemaGUIDLower = unitSchemaAttribute.SchemaGUID.ToLower();
                    // 避免託管記憶體洩漏
                    UnitSchemaAttribute newUnitSchemaAttribute = unitSchemaAttribute;
                    // 先移除舊的資料
                    dictionaryGUIDWithUnitSchema.AddOrUpdate(
                        schemaGUIDLower,
                        newUnitSchemaAttribute,
                        (GUID, oldUnitSchema) => oldUnitSchema.IsExpired(EXPIRES_DURATION) ? newUnitSchemaAttribute : oldUnitSchema
                    );

                    // 則加入最後的 GUID 搜尋
                    researchedGUIDs.Add(schemaGUIDLower);
                }
            }

            // 使用屬性 GUID 的長度作為容器大小
            List<UnitSchemaAttribute> unitSchemaAttributes = new List<UnitSchemaAttribute>(researchedGUIDs.Count);
            // 遍歷集成並將資料對外提供
            foreach (string unitSchemaGUID in researchedGUIDs)
            {
                // 先移除舊的資料
                if (!dictionaryGUIDWithUnitSchema.TryGetValue(unitSchemaGUID, out UnitSchema unitSchema))
                {
                    // 必定能取得: 此處為簡易防呆
                    continue;
                }

                // 本次檢查的目標是物件藍本: 將基底藍本轉換成物件藍本
                // 是否能正確轉換成物件藍本
                if (!(unitSchema is UnitSchemaAttribute unitSchemaAttribute))
                {
                    // 只要無法轉換成物件藍本則都可以跳過
                    continue;
                }

                // 設置成對外提供項目
                unitSchemaAttributes.Add(unitSchemaAttribute);
            }

            // 轉換成陣列對外提供
            return unitSchemaAttributes.ToArray();
        }

        /// <summary>
        /// 使用展示名稱 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="attributeGUID">屬性 GUID</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchema GetUnitSchema(in LDAPConfigurationDispatcher dispatcher, in Guid attributeGUID)
        {
            string unitSchemaAttributeGUIDLower = attributeGUID.ToString("D").ToLower();
            // 存在且未過期
            if (!dictionaryGUIDWithUnitSchema.TryGetValue(unitSchemaAttributeGUIDLower, out UnitSchema unitSchema) || unitSchema.IsExpired(EXPIRES_DURATION))
            {
                // 找到指定物件
                SearchResult one = UnitSchema.GetWithSchemaEntry(dispatcher, attributeGUID);
                // 簡易防呆: 不可能出現
                if (one == null)
                {
                    // 無法找到資料交由外部判斷是否錯誤
                    return null;
                }

                // 取得藍本物件入口
                using (DirectoryEntry entry = one.GetDirectoryEntry())
                {
                    // 對外提供的資料
                    UnitSchema newUnitSchema = null;
                    // 取得物件類型
                    string objectCategory = ParseSingleValue<string>(Properties.C_OBJECTCATEGORY, entry.Properties);
                    // 取得物件類型並取得展示名稱
                    using (DirectoryEntry category = dispatcher.ByDistinguisedName(objectCategory))
                    {
                        // 取得類型展示名稱
                        string lDAPDisplayName = ParseSingleValue<string>(UnitSchema.SCHEMA_PROPERTY, category.Properties);
                        // 根據類型進行轉換
                        switch (lDAPDisplayName)
                        {
                            case UnitSchemaClass.SCHEMA_CLASS:
                                {
                                    // 製作成類別對外提供
                                    newUnitSchema = new UnitSchemaClass(entry.Properties);
                                }
                                break;
                            case UnitSchemaAttribute.SCHEMA_ATTRIBUTE:
                                {
                                    newUnitSchema = new UnitSchemaAttribute(entry.Properties);
                                }
                                break;
                            default:
                                {
                                    // 拋出例外
                                    throw new LDAPExceptions($"藍本 GUID:{unitSchemaAttributeGUIDLower} 無法在相關網域:{dispatcher.Configuration} 中被發現, 請聯絡程式維護人員", ErrorCodes.SERVER_ERROR);
                                }
                        }

                        // 先移除舊的資料
                        dictionaryGUIDWithUnitSchema.AddOrUpdate(
                            unitSchemaAttributeGUIDLower,
                            newUnitSchema,
                            (GUID, oldUnitSchema) => oldUnitSchema.IsExpired(EXPIRES_DURATION) ? newUnitSchema : oldUnitSchema
                        );
                    }
                    // 提供給外部使用
                    unitSchema = newUnitSchema;
                }
            }

            // 對外提供取得的資料
            return unitSchema;
        }

        /// <summary>
        /// 取得指定展示名稱的物件類別藍本
        /// </summary>
        /// <param name="dispatcher">提供 連線權限的分配氣</param>
        /// <param name="ldapDisplayNames">需要查詢的物件展示名稱</param>
        /// <returns>轉換成基底藍本的物件類型</returns>
        internal UnitSchemaClass[] GetOriginClasses(in LDAPConfigurationDispatcher dispatcher, params string[] ldapDisplayNames)
        {
            // 最大長度必定為執行續安全字典的長度
            Dictionary<string, UnitSchemaClass> dictionaryLDAPDisplayNameWithUnitSchemaClass = new Dictionary<string, UnitSchemaClass>(dictionaryGUIDWithUnitSchema.Count);
            // 將執行續安全的字典轉成陣列
            foreach (KeyValuePair<string, UnitSchema> pair in dictionaryGUIDWithUnitSchema.ToArray())
            {
                // 本次檢查的目標是物件藍本: 將基底藍本轉換成物件藍本
                // 是否能正確轉換成物件藍本
                if (!(pair.Value is UnitSchemaClass unitSchemaClass))
                {
                    // 只要無法轉換成物件藍本則都可以跳過
                    continue;
                }

                // 轉換記錄格是
                dictionaryLDAPDisplayNameWithUnitSchemaClass.Add(unitSchemaClass.Name, unitSchemaClass);
            }

            // 避免重複用
            HashSet<string> researchedGUIDs = new HashSet<string>();
            // 避免重複用與找尋用
            HashSet<string> researchedLDAPDisplayNames = new HashSet<string>();
            // 查詢之前是否已持有並停時過濾檢查
            foreach (string ldapDisplayName in ldapDisplayNames)
            {
                // 能找到指定名稱且尚未過期
                if (dictionaryLDAPDisplayNameWithUnitSchemaClass.TryGetValue(ldapDisplayName, out UnitSchemaClass unitSchemaClass) && !unitSchemaClass.IsExpired(EXPIRES_DURATION))
                {
                    // 找尋用的 GUID 必須微小寫
                    string schemaGUIDLower = unitSchemaClass.SchemaGUID.ToLower();
                    // 則加入最後的 GUID 搜尋
                    researchedGUIDs.Add(schemaGUIDLower);
                    // 跳過
                    continue;
                }

                /* 直行至此則必定下列條件須滿足
                     - 下述條件滿足其一:
                       1. 無法找到指定展示名稱的物件藍本
                       2. 能找到但是物件藍本已過期
                     - 物件尚未被推入不重複重新搜尋陣列 
                */
                researchedLDAPDisplayNames.Add(ldapDisplayName);
            }

            // 存在需重新搜尋的不重複物件展示名稱
            if (researchedLDAPDisplayNames.Count != 0)
            {
                // 重新建立藍本結構
                foreach (UnitSchemaClass unitSchemaClass in UnitSchemaClass.GetWithLDAPDisplayNames(dispatcher, researchedLDAPDisplayNames))
                {
                    // 更新的 GUID 字串必須是小寫
                    string schemaGUIDLower = unitSchemaClass.SchemaGUID.ToLower();
                    // 避免託管記憶體洩漏
                    UnitSchemaClass newUnitSchemaClass = unitSchemaClass;
                    // 先移除舊的資料
                    dictionaryGUIDWithUnitSchema.AddOrUpdate(
                        schemaGUIDLower,
                        newUnitSchemaClass,
                        (GUID, oldUnitSchema) => oldUnitSchema.IsExpired(EXPIRES_DURATION) ? newUnitSchemaClass : oldUnitSchema
                    );

                    // 則加入最後的 GUID 搜尋
                    researchedGUIDs.Add(schemaGUIDLower);
                }
            }

            // 使用屬性 GUID 的長度作為容器大小
            List<UnitSchemaClass> unitSchemaClasses = new List<UnitSchemaClass>(researchedGUIDs.Count);
            // 遍歷集成並將資料對外提供
            foreach (string unitSchemaGUID in researchedGUIDs)
            {
                // 先移除舊的資料
                if (!dictionaryGUIDWithUnitSchema.TryGetValue(unitSchemaGUID, out UnitSchema unitSchema))
                {
                    // 必定能取得: 此處為簡易防呆
                    continue;
                }

                // 本次檢查的目標是物件藍本: 將基底藍本轉換成物件藍本
                // 是否能正確轉換成物件藍本
                if (!(unitSchema is UnitSchemaClass unitSchemaClass))
                {
                    // 只要無法轉換成物件藍本則都可以跳過
                    continue;
                }

                // 設置成對外提供項目
                unitSchemaClasses.Add(unitSchemaClass);
            }

            // 轉換成陣列對外提供
            return unitSchemaClasses.ToArray();
        }

        /// <summary>
        /// 取得指定物件類別藍本的驅動類型藍本
        /// </summary>
        /// <param name="dispatcher">提供 連線權限的分配氣</param>
        /// <param name="unitSchemaClasses">指定物件類型</param>
        /// <returns>轉換成基底藍本的物件類型</returns>
        internal UnitSchemaClass[] GetDrivedClasses(in LDAPConfigurationDispatcher dispatcher, params UnitSchemaClass[] unitSchemaClasses)
        {
            // 最大長度必定為執行續安全字典的長度
            Dictionary<string, UnitSchemaClass> dictionaryLDAPDisplayNameWithUnitSchemaClass = new Dictionary<string, UnitSchemaClass>(dictionaryGUIDWithUnitSchema.Count);
            // 將執行續安全的字典轉成陣列
            foreach (KeyValuePair<string, UnitSchema> pair in dictionaryGUIDWithUnitSchema.ToArray())
            {
                // 本次檢查的目標是物件藍本: 將基底藍本轉換成物件藍本
                // 是否能正確轉換成物件藍本
                if (!(pair.Value is UnitSchemaClass unitSchemaClass))
                {
                    // 只要無法轉換成物件藍本則都可以跳過
                    continue;
                }

                // 轉換記錄格是
                dictionaryLDAPDisplayNameWithUnitSchemaClass.Add(unitSchemaClass.Name, unitSchemaClass);
            }

            // 找到所有應檢查的驅動類型
            HashSet<string> drivedClassNames = UnitSchemaClass.DrivedClassNames(unitSchemaClasses);
            // 避免重複用
            HashSet<string> researchedGUIDs = new HashSet<string>();
            // 避免重複用與找尋用
            HashSet<string> researchedLDAPDisplayNames = new HashSet<string>();
            // 查詢之前是否已持有並停時過濾檢查
            foreach (string drivedClassName in drivedClassNames)
            {
                // 能找到指定名稱且尚未過期
                if (dictionaryLDAPDisplayNameWithUnitSchemaClass.TryGetValue(drivedClassName, out UnitSchemaClass unitSchemaClass) && !unitSchemaClass.IsExpired(EXPIRES_DURATION))
                {
                    // 找尋用的 GUID 必須微小寫
                    string schemaGUIDLower = unitSchemaClass.SchemaGUID.ToLower();
                    // 則加入最後的 GUID 搜尋
                    researchedGUIDs.Add(schemaGUIDLower);
                    // 跳過
                    continue;
                }

                /* 直行至此則必定下列條件須滿足
                     - 下述條件滿足其一:
                       1. 無法找到指定展示名稱的物件藍本
                       2. 能找到但是物件藍本已過期
                     - 物件尚未被推入不重複重新搜尋陣列 
                */
                researchedLDAPDisplayNames.Add(drivedClassName);
            }

            // 存在需重新搜尋的不重複物件展示名稱
            if (researchedLDAPDisplayNames.Count != 0)
            {
                // 重新建立藍本結構
                foreach (UnitSchemaClass unitSchemaClass in UnitSchemaClass.GetWithLDAPDisplayNames(dispatcher, researchedLDAPDisplayNames))
                {
                    // 是否為史提類型
                    if (!unitSchemaClass.IsClassCategory(ClassCategory.STRUCTURAL_CLASS))
                    {
                        // 不是則跳過
                        continue;
                    }

                    // 是否為系統使用
                    if (unitSchemaClass.SystemOnly)
                    {
                        // 是系統使用跳過
                        continue;
                    }

                    // 更新的 GUID 字串必須是小寫
                    string schemaGUIDLower = unitSchemaClass.SchemaGUID.ToLower();
                    // 避免託管記憶體洩漏
                    UnitSchemaClass newUnitSchemaClass = unitSchemaClass;
                    // 先移除舊的資料
                    dictionaryGUIDWithUnitSchema.AddOrUpdate(
                        schemaGUIDLower,
                        newUnitSchemaClass,
                        (GUID, oldUnitSchema) => oldUnitSchema.IsExpired(EXPIRES_DURATION) ? newUnitSchemaClass : oldUnitSchema
                    );

                    // 尚未加入至最後的 GUID 搜尋
                    if (!researchedGUIDs.Contains(schemaGUIDLower))
                    {
                        // 則加入最後的 GUID 搜尋
                        researchedGUIDs.Add(schemaGUIDLower);
                    }
                }
            }

            // 使用屬性 GUID 的長度作為容器大小
            List<UnitSchemaClass> drivedUnitSchemaClasses = new List<UnitSchemaClass>(researchedGUIDs.Count);
            // 遍歷集成並將資料對外提供
            foreach (string unitSchemaGUID in researchedGUIDs)
            {
                // 先移除舊的資料
                if (!dictionaryGUIDWithUnitSchema.TryGetValue(unitSchemaGUID, out UnitSchema unitSchema))
                {
                    // 必定能取得: 此處為簡易防呆
                    continue;
                }

                // 是否能正確轉換成物件藍本
                if (!(unitSchema is UnitSchemaClass unitSchemaClass))
                {
                    // 只要無法轉換成物件藍本則都可以跳過
                    continue;
                }

                // 設置成對外提供項目
                drivedUnitSchemaClasses.Add(unitSchemaClass);
            }

            // 轉換成陣列對外提供
            return drivedUnitSchemaClasses.ToArray();
        }

        /// <summary>
        /// 透過展示名稱取額指定額外全縣
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="unitSchemaClasses">可作為父層藍本物件的類型</param>
        /// <returns>指定的物件藍本支援可包含的藍本物件</returns>
        internal UnitSchemaClass[] GetChildrenClasses(in LDAPConfigurationDispatcher dispatcher, params UnitSchemaClass[] unitSchemaClasses)
        {
            // 將資料換成小寫的 GUID 作為鍵值的字典
            Dictionary<string, UnitSchemaClass> dictionaryNameWithUnitSchemaClass = unitSchemaClasses.ToDictionary(unitSchemaClass => unitSchemaClass.Name.ToLower());
            // 即將用來搜尋的 GUID
            HashSet<string> researchedNames = new HashSet<string>(dictionaryNameWithUnitSchemaClass.Count);
            // 轉換成物件類型 GUID 與是否過期的字典
            foreach (KeyValuePair<string, UnitSchemaClass> pair in dictionaryNameWithUnitSchemaClass)
            {
                // 取得父層的 GUID 小寫
                string schemaClassGUIDLower = pair.Value.SchemaGUID.ToLower();
                // 取得藍本額外權限關聯
                if (dictionarySchemaClassGUIDWithChildrenUnitSchemaClassGUIDs.TryGetValue(schemaClassGUIDLower, out PropertySet propertySet) && !propertySet.IsExpired(EXPIRES_DURATION))
                {
                    // 已存在且未過期
                    continue;
                }

                // 組成應搜尋出更新物件的字典黨
                researchedNames.Add(pair.Key);
            }

            // 找尋目標藍本結構
            if (researchedNames.Count != 0)
            {
                // 取得可隸屬於指定藍本類型物件的下層類型藍本
                UnitSchemaClass[] unitSchemaClassChirden = UnitSchemaClass.GetWithSuperiorLDAPDisplayNames(dispatcher, researchedNames);
                // 遍歷存取權限並更新
                foreach (UnitSchemaClass unitSchemaClass in unitSchemaClassChirden)
                {
                    // 查詢時須使用小寫的 GUID
                    string unitSchemaGUIDLower = unitSchemaClass.SchemaGUID.ToLower();
                    // 避免託管記憶體洩漏
                    UnitSchemaClass newUnitSchemaClass = unitSchemaClass;
                    // 更新相關資訊
                    dictionaryGUIDWithUnitSchema.AddOrUpdate(
                        unitSchemaGUIDLower,
                        newUnitSchemaClass,
                        (GUID, oldUnitSchemaClass) => newUnitSchemaClass
                    );
                }

                // 更新資料
                foreach (string unitSchemaClassName in researchedNames)
                {
                    // 避免託管記憶體洩漏
                    bool isExist = dictionaryNameWithUnitSchemaClass.TryGetValue(unitSchemaClassName, out UnitSchemaClass unitSchemaClass);
                    // 簡易防呆: 肯定是存在的
                    if (!isExist)
                    {
                        // 但是不存在跳過處理
                        continue;
                    }

                    // 檢查得到的存取權限何者隸屬於查詢的目標
                    string[] unitSchemaClassGUIDs = UnitSchemaClass.WhichChildrenWith(unitSchemaClass, unitSchemaClassChirden);

                    // 建立新的關聯設定
                    PropertySet newPropertySet = new PropertySet(unitSchemaClassGUIDs);
                    // 避免託管記憶體洩漏
                    string unitSchemaClassGUIDLower = unitSchemaClass.SchemaGUID.ToLower();
                    // 更新時使用的是目標鍵值
                    dictionarySchemaClassGUIDWithChildrenUnitSchemaClassGUIDs.AddOrUpdate(
                        unitSchemaClassGUIDLower,
                        newPropertySet,
                        (unitSchemaGUIDLower, oldPropertySet) => oldPropertySet.IsExpired(EXPIRES_DURATION) ? newPropertySet : oldPropertySet
                    );
                }
            }

            // 最多為目前額外權限的大小: 此用字典是會了避免重複加入
            Dictionary<string, UnitSchemaClass> dictionaryUnitSchemaClassGUIDWithUnitSchemaClass = new Dictionary<string, UnitSchemaClass>();
            // 轉換成物件類型 GUID 與是否過期的字典
            foreach (UnitSchemaClass unitSchemaClass in unitSchemaClasses)
            {
                // 將藍本物件 GUID 轉乘小寫
                string unitSchemaClassGUIDLower = unitSchemaClass.SchemaGUID.ToLower();
                // 找到關聯物件列表 (不必檢查是否過期)
                if (!dictionarySchemaClassGUIDWithChildrenUnitSchemaClassGUIDs.TryGetValue(unitSchemaClassGUIDLower, out PropertySet propertySet))
                {
                    // 不存在就跳過: 極少觸發, 不持有權限的欄位不必開放查看
                    continue;
                }

                // 遍歷此
                foreach (string unitSchemaClassGUID in propertySet.GUIDHashSet)
                {
                    // 檢查是否以推入過
                    if (dictionaryUnitSchemaClassGUIDWithUnitSchemaClass.ContainsKey(unitSchemaClassGUID))
                    {
                        // 已包含則跳過
                        continue;
                    }

                    // 找到目標屬性組 GUID 相關額外權限
                    if (!dictionaryGUIDWithUnitSchema.TryGetValue(unitSchemaClassGUID, out UnitSchema unitSchema))
                    {
                        // 無罰找到也跳過
                        continue;
                    }

                    // 簡易防呆
                    if (!(unitSchema is UnitSchemaClass childrenUnitSchemaClass))
                    {
                        // 跳過
                        continue;
                    }

                    // 找到時推入作為外部可用權限
                    dictionaryUnitSchemaClassGUIDWithUnitSchemaClass.Add(unitSchemaClassGUID, childrenUnitSchemaClass);
                }
            }
            // 對外提供取得的資料: 注意可能為空
            return dictionaryUnitSchemaClassGUIDWithUnitSchemaClass.Values.ToArray();
        }
        #endregion

        #region 取得額外權限
        /// <summary>
        /// 存取控制的 GUID 與存取控制
        /// </summary>
        private readonly ConcurrentDictionary<string, UnitControlAccess> dictionaryGUIDWithUnitControlAccess = new ConcurrentDictionary<string, UnitControlAccess>();
        /// <summary>
        /// 藍本物件的 GUID 與 關聯存取渠線的集合
        /// </summary>
        private readonly ConcurrentDictionary<string, PropertySet> dictionarySchemaClassGUIDWithUnitControlAccesGUIDs = new ConcurrentDictionary<string, PropertySet>();

        /// <summary>
        /// 透過展示名稱取額指定額外全縣
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="unitSchemaClasses">查詢的物件藍本</param>
        /// <returns>指定的物件藍本支援的所有控制渠縣</returns>
        internal UnitControlAccess[] GeControlAccess(in LDAPConfigurationDispatcher dispatcher, params UnitSchemaClass[] unitSchemaClasses)
        {
            // 將資料換成小寫的 GUID 作為鍵值的字典
            Dictionary<string, UnitSchemaClass> dictionarySchemaClassGUIDWithUnitSchemaClass = unitSchemaClasses.ToDictionary(unitSchemaClass => unitSchemaClass.SchemaGUID.ToLower());
            // 即將用來搜尋的 GUID
            HashSet<string> researchedSchemaClassGUIDs = new HashSet<string>(dictionarySchemaClassGUIDWithUnitSchemaClass.Count);
            // 轉換成物件類型 GUID 與是否過期的字典
            foreach (string schemaClassGUIDLower in dictionarySchemaClassGUIDWithUnitSchemaClass.Keys)
            {
                // 取得藍本額外權限關聯
                if (dictionarySchemaClassGUIDWithUnitControlAccesGUIDs.TryGetValue(schemaClassGUIDLower, out PropertySet propertySet) && !propertySet.IsExpired(EXPIRES_DURATION))
                {
                    // 已存在且未過期
                    continue;
                }

                // 組成應搜尋出更新物件的字典黨
                researchedSchemaClassGUIDs.Add(schemaClassGUIDLower);
            }

            // 找尋目標藍本結構
            if (researchedSchemaClassGUIDs.Count != 0)
            {
                // 取得所有關聯於目標類型的存取權限
                UnitControlAccess[] unitControlAccesses = UnitControlAccess.GetAppliedTo(dispatcher, researchedSchemaClassGUIDs);
                // 遍歷存取權限並更新
                foreach (UnitControlAccess unitControlAccess in unitControlAccesses)
                {
                    // 查詢時須使用小寫的 GUID
                    string unitControlAccessGUIDLower = unitControlAccess.GUID.ToLower();
                    // 避免託管記憶體洩漏
                    UnitControlAccess newUnitControlAccess = unitControlAccess;
                    // 更新相關資訊
                    dictionaryGUIDWithUnitControlAccess.AddOrUpdate(
                        unitControlAccessGUIDLower,
                        newUnitControlAccess,
                        (GUID, oldUnitControlAccess) => newUnitControlAccess
                    );
                }

                // 更新資料
                foreach (string unitSchemaClassGUID in researchedSchemaClassGUIDs)
                {
                    // 避免託管記憶體洩漏
                    bool isExist = dictionarySchemaClassGUIDWithUnitSchemaClass.TryGetValue(unitSchemaClassGUID, out UnitSchemaClass unitSchemaClass);
                    // 簡易防呆: 肯定是存在的
                    if (!isExist)
                    {
                        // 但是不存在跳過處理
                        continue;
                    }

                    // 檢查得到的存取權限何者隸屬於查詢的目標
                    string[] unitControlAccessGUIDs = UnitSchemaClass.WhichAppliedWith(unitSchemaClass, unitControlAccesses);

                    // 建立新的關聯設定
                    PropertySet newPropertySet = new PropertySet(unitControlAccessGUIDs);
                    // 避免託管記憶體洩漏
                    string unitSchemaClassGUIDLower = unitSchemaClassGUID;
                    // 更新時使用的是目標鍵值
                    dictionarySchemaClassGUIDWithUnitControlAccesGUIDs.AddOrUpdate(
                        unitSchemaClassGUIDLower,
                        newPropertySet,
                        (unitSchemaGUIDLower, oldPropertySet) => oldPropertySet.IsExpired(EXPIRES_DURATION) ? newPropertySet : oldPropertySet
                    );
                }
            }

            // 最多為目前額外權限的大小: 此用字典是會了避免重複加入
            Dictionary<string, UnitControlAccess> dictionaryUnitControlAccessGUIDWithUnitControlAccess = new Dictionary<string, UnitControlAccess>();
            // 轉換成物件類型 GUID 與是否過期的字典
            foreach (UnitSchemaClass unitSchemaClass in unitSchemaClasses)
            {
                // 將藍本物件 GUID 轉乘小寫
                string unitSchemaClassGUIDLower = unitSchemaClass.SchemaGUID.ToLower();
                // 找到關聯物件列表 (不必檢查是否過期)
                if (!dictionarySchemaClassGUIDWithUnitControlAccesGUIDs.TryGetValue(unitSchemaClassGUIDLower, out PropertySet propertySet))
                {
                    // 不存在就跳過: 極少觸發, 不持有權限的欄位不必開放查看
                    continue;
                }

                // 遍歷此
                foreach (string unitExtendedRightGUID in propertySet.GUIDHashSet)
                {
                    // 檢查是否以推入過
                    if (dictionaryUnitControlAccessGUIDWithUnitControlAccess.ContainsKey(unitExtendedRightGUID))
                    {
                        // 已包含則跳過
                        continue;
                    }

                    // 找到目標屬性組 GUID 相關額外權限
                    if (!dictionaryGUIDWithUnitControlAccess.TryGetValue(unitExtendedRightGUID, out UnitControlAccess unitExtendedRight))
                    {
                        // 無罰找到也跳過
                        continue;
                    }

                    // 找到時推入作為外部可用權限
                    dictionaryUnitControlAccessGUIDWithUnitControlAccess.Add(unitExtendedRightGUID, unitExtendedRight);
                }
            }
            // 對外提供取得的資料: 注意可能為空
            return dictionaryUnitControlAccessGUIDWithUnitControlAccess.Values.ToArray();
        }

        /// <summary>
        /// 存取權限物件的 GUID 與關聯的相關參數
        /// </summary>
        private readonly ConcurrentDictionary<string, PropertySet> dictionaryUnitControlAccesGUIDWithUnitSchemaAttributeGUIDs = new ConcurrentDictionary<string, PropertySet>();

        /// <summary>
        /// 使用指定存取權限找到相關聯的屬性值, 並回傳存取類型
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="unitControlAccess">目標存取璇縣</param>
        /// <param name="unitSchemas">此存取權限關聯的屬性</param>
        /// <returns>此存取權限為何種類型</returns>
        internal ControlAccessType GeControlAccessAttributes(in LDAPConfigurationDispatcher dispatcher, in UnitControlAccess unitControlAccess, out UnitSchema[] unitSchemas)
        {
            // 轉成小寫
            string unitControlAccessGUIDLower = unitControlAccess.GUID.ToLower();
            // 轉成查詢用的 GUID
            Guid unitControlAccessGUID = new Guid(unitControlAccessGUIDLower);
            // 尚不存在或者過期時
            if (!dictionaryUnitControlAccesGUIDWithUnitSchemaAttributeGUIDs.TryGetValue(unitControlAccessGUIDLower, out PropertySet propertySet) || propertySet.IsExpired(EXPIRES_DURATION))
            {
                // 此存取權限的關聯屬性
                UnitSchemaAttribute[] unitSchemaAttributeCaches = UnitSchemaAttribute.GetWithControlAccessGUID(dispatcher, unitControlAccessGUID);
                // 紀錄所有可用的屬性
                HashSet<string> unitSchemaAttributeGUIDs = new HashSet<string>(unitSchemaAttributeCaches.Length);
                // 遍歷存取權限並更新
                foreach (UnitSchemaAttribute unitSchemaAttribute in unitSchemaAttributeCaches)
                {
                    // 查詢時須使用小寫的 GUID
                    string unitSchemaAttributeGUIDLower = unitSchemaAttribute.SchemaGUID.ToLower();
                    // 避免託管記憶體洩漏
                    UnitSchemaAttribute newUnitSchemaAttribute = unitSchemaAttribute;
                    // 更新相關資訊
                    dictionaryGUIDWithUnitSchema.AddOrUpdate(
                        unitSchemaAttributeGUIDLower,
                        newUnitSchemaAttribute,
                        (GUID, oldUnitSchemaAttribute) => newUnitSchemaAttribute
                    );

                    // 推入關聯
                    unitSchemaAttributeGUIDs.Add(unitSchemaAttributeGUIDLower);
                }

                // 建立新的關聯設定
                PropertySet newPropertySet = new PropertySet(unitSchemaAttributeGUIDs);
                // 更新相關資訊
                dictionaryUnitControlAccesGUIDWithUnitSchemaAttributeGUIDs.AddOrUpdate(
                    unitControlAccessGUIDLower,
                    newPropertySet,
                    (GUID, oldUnitSchemaPropertySet) => newPropertySet
                );

                // 取得新的群組
                propertySet = newPropertySet;
            }

            // 宣告總總長度
            List<UnitSchema> unitSchemasCache = new List<UnitSchema>(propertySet.GUIDHashSet.Count);
            // 遍歷權限
            foreach (string unitSchemaAttributeGUIDLower in propertySet.GUIDHashSet)
            {
                // 嘗試找到指定屬性
                if (!dictionaryGUIDWithUnitSchema.TryGetValue(unitSchemaAttributeGUIDLower, out UnitSchema unitSchema))
                {
                    // 不存在時跳過: 可以丟例外
                    continue;
                }

                // 轉換失敗時
                if (!(unitSchema is UnitSchemaAttribute unitSchemaAttribute))
                {
                    // 不存在時跳過: 可以丟例外
                    continue;
                }

                // 加入對外提供細目
                unitSchemasCache.Add(unitSchemaAttribute);
            }

            // 轉換成為陣列
            unitSchemas = unitSchemasCache.ToArray();
            // 根據長度進行判斷
            switch (unitSchemas.Length)
            {
                case 0:
                    {
                        // 不存在任何項目時必定是拓展權限
                        return ControlAccessType.EXTENDED_RIGHT;
                    }
                case 1:
                    {
                        // 取得第 0 筆
                        UnitSchemaAttribute unitSchemaAttribute = unitSchemasCache[0] as UnitSchemaAttribute;
                        // 查看是否為群組項目
                        return unitSchemaAttribute.IsPropertySet(unitControlAccessGUIDLower) ? ControlAccessType.PROPERTY_SET : ControlAccessType.VALIDATED_WRITE;
                    }
                default:
                    {
                        // 多筆時必定微群組設定
                        return ControlAccessType.PROPERTY_SET;
                    }
            }
        }
        #endregion

    }
}
