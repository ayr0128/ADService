using ADService.Basis;
using ADService.DynamicParse;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;

namespace ADService.RootDSE
{
    /// <summary>
    /// 放置相關的所有設定
    /// </summary>
    internal sealed class Configurate
    {
        /// <summary>
        /// 取得 GUID 字串
        /// </summary>
        /// <param name="valueGUID">目標 GUID</param>
        /// <returns>轉為小寫後的特定格式 GUID</returns>
        internal static string GetGUID(in Guid valueGUID) => valueGUID.ToString("D").ToLower();
        /// <summary>
        /// 取得 GUID 用於搜尋用的二進位字串
        /// </summary>
        /// <param name="valueGUID">目標 GUID</param>
        /// <returns>搜尋字串</returns>
        internal static string GetFiliter(in Guid valueGUID)
        {
            // 使用文字串流來推入 GUID
            StringBuilder sb = new StringBuilder();
            // 遍歷位元組
            foreach (byte convertRequired in valueGUID.ToByteArray())
            {
                // 轉化各位元組至十六進位
                sb.Append($"\\{convertRequired:X2}");
            }
            // 對外提供組合完成的結果
            return sb.ToString();
        }
        /// <summary>
        /// 取得 SID 用於搜尋用的二進位字串
        /// </summary>
        /// <param name="valueSID">目標 SID</param>
        /// <returns>搜尋字串</returns>
        internal static string GetFiliter(in SecurityIdentifier valueSID)
        {
            // 宣告陣列用於複製
            byte[] valueSIDbytes = new byte[valueSID.BinaryLength];
            // 複製二進位資料至陣列中
            valueSID.GetBinaryForm(valueSIDbytes, 0);

            // 使用文字串流來推入 GUID
            StringBuilder sb = new StringBuilder();
            // 遍歷位元組
            foreach (byte convertRequired in valueSIDbytes)
            {
                // 轉化各位元組至十六進位
                sb.Append($"\\{convertRequired:X2}");
            }
            // 對外提供組合完成的結果
            return sb.ToString();
        }

        /// <summary>
        /// 資料過期時間, 可以考慮由外部設置
        /// </summary>
        private static TimeSpan EXPIRES_DURATION = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 搜尋時找尋的必須資料
        /// </summary>
        internal static readonly string[] BASE_PROPERTIES = new string[] {
            Properties.C_DISTINGUISHEDNAME,
            Properties.C_OBJECTCLASS,
            Properties.P_NAME,
        };

        #region 儲存資料解析
        /// <summary>
        /// 將 OID 字串轉換微陣列, 此程式碼由 <see href="https://www.codeproject.com/articles/16468/oid-conversion"> 此處取得 </see>
        /// </summary>
        /// <param name="oID">OID 字串</param>
        /// <returns>轉換後的</returns>
        internal static byte[] OidStringToBytes(string oID)
        {
            string[] split = oID.Trim(' ', '.').Split('.');
            List<int> retVal = new List<int>();
            for (int a = 0, i = 0; i < split.Length; i++)
            {
                if (i == 0)
                    a = int.Parse(split[0]);
                else if (i == 1)
                    retVal.Add(40 * a + int.Parse(split[1]));
                else
                {
                    int b = int.Parse(split[i]);
                    if (b < 128)
                        retVal.Add(b);
                    else
                    {
                        retVal.Add(128 + (b / 128));
                        retVal.Add(b % 128);
                    }
                }
            }

            byte[] temp = new byte[retVal.Count];

            for (int i = 0; i < retVal.Count; i++)
                temp[i] = (byte)retVal[i];

            return temp;

        }

        /// <summary>
        /// 將 OID 字串轉換微陣列, 此程式碼由 <see href="https://www.codeproject.com/articles/16468/oid-conversion"> 此處取得 </see>
        /// </summary>
        /// <param name="oID">OID 二進位</param>
        /// <returns>轉換後的</returns>
        internal static string OidBytesToString(byte[] oID)
        {
            StringBuilder retVal = new StringBuilder();
            for (int i = 0; i < oID.Length; i++)
            {
                if (i == 0)
                {
                    int b = oID[0] % 40;
                    int a = (oID[0] - b) / 40;
                    retVal.Append($"{a}.{b}");
                }
                else
                {
                    if (oID[i] < 128)
                        retVal.Append($".{oID[i]}");
                    else
                    {
                        retVal.Append($".{((oID[i] - 128) * 128) + oID[i + 1]}");
                        i++;
                    }
                }
            }

            return retVal.ToString();
        }

        /// <summary>
        /// 取得 LDAP 設定相關的所有參數
        /// </summary>
        /// <param name="domain">網域</param>
        /// <param name="port">埠</param>
        /// <returns> RootDSE 入口物件</returns>
        private static DirectoryEntry GetRootDSE(in string domain, in ushort port)
        {
            // 取得指定入口物件
            DirectoryEntry entryRootDSE = new DirectoryEntry($"LDAP://{domain}:{port}/rootDSE");
            // 透過檢查原生 GUID 確認是否能成功取得: 此處有可能提供例外
            _ = entryRootDSE.NativeGuid;
            // 對外提供檢查完成入口物件
            return entryRootDSE;
        }

        /// <summary>
        /// 透過使用者的登入名稱與密碼取得具有權限的連線執行續
        /// </summary>
        /// <param name="domain">網域</param>
        /// <param name="port">埠</param>
        /// <param name="userAccount">使用者名稱</param>
        /// <param name="userPassword">使用者密碼</param>
        /// <param name="distinguisedName">指定區分名稱</param>
        /// <returns> RootDSE 入口物件</returns>
        private static DirectoryEntry GetEntry(in string domain, in ushort port, in string userAccount, in string userPassword, in string distinguisedName)
        {
            // 入口物件
            DirectoryEntry entry = new DirectoryEntry($"LDAP://{domain}:{port}/{distinguisedName}", userAccount, userPassword);
            // 透過檢查原生 GUID 確認是否能成功取得: 此處有可能提供例外
            _ = entry.NativeGuid;
            // 對外提供檢查完成入口物件
            return entry;
        }

        /// <summary>
        /// 使用指定入口物件與過濾條件查詢並取得特定物件
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="filiter">過濾條件</param>
        /// <param name="scope">範圍</param>
        /// <param name="customProperties">自定義查詢的項目</param>
        /// <returns>最基礎的物件</returns>
        internal static SearchResult ExecSearcherSingle(
            in DirectoryEntry entry,
            in string filiter,
            in SearchScope scope = SearchScope.Subtree,
            params string[] customProperties
        )
        {
            // 查看網路文件有發現可能有攔截問題, 是否需要使用加密動作? [TODO]
            string encodeFiliter = filiter;
            // 固定指搜尋特定屬性
            using (DirectorySearcher searcher = new DirectorySearcher(entry, encodeFiliter, customProperties.Length != 0 ? customProperties : BASE_PROPERTIES, scope))
            {
                // 無法發現時對外提供空物件
                return searcher.FindOne();
            }
        }

        /// <summary>
        /// 使用指定入口物件與過濾條件查詢並取得多個物件
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="filiter">過濾條件</param>
        /// <param name="scope">範圍</param>
        /// <param name="customProperties">自定義查詢的項目</param>
        /// <returns>最基礎的物件</returns>
        internal static SearchResult[] ExecSearcherMutiple(
            in DirectoryEntry entry,
            in string filiter,
            in SearchScope scope = SearchScope.Subtree,
            params string[] customProperties
        )
        {
            // 查看網路文件有發現可能有攔截問題, 是否需要使用加密動作? [TODO]
            string encodeFiliter = filiter;
            // 固定指搜尋特定屬性
            using (DirectorySearcher searcher = new DirectorySearcher(entry, encodeFiliter, customProperties.Length != 0 ? customProperties : BASE_PROPERTIES, scope))
            {
                // 取得找尋結果
                using (SearchResultCollection all = searcher.FindAll())
                {
                    // 使用長度作為容器大小
                    SearchResult[] results = new SearchResult[all.Count];
                    // 固定指找尋一個目標
                    for (int index = 0; index < all.Count; index++)
                    {
                        // 設置至指定位置
                        results[index] = all[index];
                    }
                    // 對外提供參數
                    return results;
                }
            }
        }
        #endregion

        /// <summary>
        /// 連線網域: 可用 IP 或 網址, 根據實作方式限制
        /// </summary>
        private readonly string Domain;
        /// <summary>
        /// 連線埠
        /// </summary>
        private readonly ushort Port;

        /// <summary>
        /// 屬性藍本子藍本
        /// </summary>
        private const string DEFAULT_NAMINGCONTEXT = "defaultNamingContext";
        /// <summary>
        /// 預設網域
        /// </summary>
        [ADDescriptionProperty(DEFAULT_NAMINGCONTEXT)]
        private string DefaultNamingContext { get; set; }

        /// <summary>
        /// 此區分名稱是否為根目錄
        /// </summary>
        /// <param name="distinguisedName">指定區分名稱</param>
        /// <returns>是否根目錄</returns>
        internal bool IsDefault(in string distinguisedName) => DefaultNamingContext == distinguisedName;

        /// <summary>
        /// 儲存子藍本物件位置的欄位
        /// </summary>
        private const string SCHEMA_SUBENTRY = "subschemaSubentry";
        /// <summary>
        /// 子藍本物件入口
        /// </summary>
        [ADDescriptionProperty(SCHEMA_SUBENTRY)]
        private string SubSchemaSubEntry { get; set; }
        /// <summary>
        /// 紀錄並存取相關設置
        /// </summary>
        private readonly SubSchema StoredSubSchema;

        /// <summary>
        /// 儲存藍本物件位置的欄位
        /// </summary>
        private const string SCHEMA_CONTEXT = "schemaNamingContext";
        /// <summary>
        /// 藍本物件入口
        /// </summary>
        [ADDescriptionProperty(SCHEMA_CONTEXT)]
        private string SchemaContext { get; set; }
        /// <summary>
        /// 紀錄並存取相關設置
        /// </summary>
        private readonly SchemaClasses StoredSchemaClass;

        /// <summary>
        /// 透過指定網域與埠取得設定, [注意] 尚未開始解析
        /// </summary>
        /// <param name="domain">支援 IPv4 或 網域的伺服器位置</param>
        /// <param name="port">可用埠</param>
        internal Configurate(in string domain, in ushort port)
        {
            Domain = domain;
            Port = port;

            // 透過取得網域設定檢查輸入的網域與埠是否正確
            using (DirectoryEntry entryRootDSE = GetRootDSE(Domain, Port))
            {

                // 取得部標類型
                Type instanceType = typeof(Configurate);
                // 遍歷所有可用的成員
                foreach (PropertyInfo propertyInfo in instanceType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    // 取得屬性描述
                    ADDescriptionProperty descriptionProperty = propertyInfo.GetCustomAttribute<ADDescriptionProperty>();
                    // 屬性描述不存在就跳過
                    if (descriptionProperty == null)
                    {
                        // 不存在指定屬性描述
                        continue;
                    }

                    // 檢查是否能取得希望解析的資料
                    if (!entryRootDSE.Properties.Contains(descriptionProperty.PropertyName))
                    {
                        // 不能則跳過
                        continue;
                    }

                    // 已知設定內儲存的資料必定是 String(Teletex), 是否微陣列由儲存目標決定
                    PropertyConvertor convertor = PropertyConvertor.Create(PropertyConvertor.STRING_TELEX, propertyInfo.PropertyType.IsArray);
                    // 轉換數據內容
                    PropertyValue propertyValue = new PropertyValue(convertor, entryRootDSE.Properties[descriptionProperty.PropertyName]);
                    // 設置資料
                    propertyInfo.SetValue(this, propertyValue.Value);
                }
            }

            StoredSubSchema = new SubSchema(SubSchemaSubEntry);
            StoredSchemaClass = new SchemaClasses(SchemaContext);
        }

        /// <summary>
        /// 使用指定的帳號密碼取得具有指定帳號權限的入口物件, 可以指定區分名稱取得指定物件
        /// </summary>
        /// <param name="account">帳號</param>
        /// <param name="aassword">密碼</param>
        /// <param name="distinguisedName">指定區分名稱, 預設為空</param>
        /// <returns>取得具有指定帳號權限的入口物件</returns>
        internal DirectoryEntry GetEntryByDN(in string account, in string aassword, in string distinguisedName = null) => GetEntry(Domain, Port, account, aassword, string.IsNullOrEmpty(distinguisedName) ? DefaultNamingContext : distinguisedName);

        /// <summary>
        /// 取得指定類型名稱
        /// </summary>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="classLDAPDisplayNames"></param>
        /// <returns>指定查詢的類型物件類型</returns>
        internal DriveSchemaClass[] GetSchemaClassByNames(in string account, in string password, params string[] classLDAPDisplayNames) => StoredSchemaClass.GetByNames(this, account, password, EXPIRES_DURATION, classLDAPDisplayNames);

        #region 轉換至通用基礎物件
        /// <summary>
        /// 轉換入口物件至目標樣板, 指支援參數屬性:<see cref="ADDescriptionProperty"/> 解析
        /// </summary>
        /// <typeparam name="T">解析目標樣板</typeparam>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="entry">入口物件</param>
        /// <returns>基礎物件</returns>
        internal T ConvertToCustom<T>(in string account, in string password, in DirectoryEntry entry) where T : new()
        {
            // 宣告轉換目標
            T custom = Activator.CreateInstance<T>();

            // 取得部標類型
            Type instanceType = typeof(T);
            // 需告需要處理的字串
            Dictionary<string, Tuple<PropertyConvertor, PropertyInfo>> dictionaryPropertyNameWithTuple = new Dictionary<string, Tuple<PropertyConvertor, PropertyInfo>>();
            // 遍歷所有可用的成員
            foreach (PropertyInfo propertyInfo in instanceType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // 取得屬性描述
                ADDescriptionProperty descriptionProperty = propertyInfo.GetCustomAttribute<ADDescriptionProperty>();
                // 屬性描述不存在就跳過
                if (descriptionProperty == null)
                {
                    // 不存在指定屬性描述
                    continue;
                }

                // 取得指定物件的類型
                AttributeType attributeType = StoredSubSchema.GeByName(this, account, password, descriptionProperty.PropertyName, EXPIRES_DURATION);
                // 如果無法取得此特性描述
                if (attributeType == null)
                {
                    // 跳過
                    continue;
                }

                // 已知設定內儲存的資料必定是 String(Teletex)
                PropertyConvertor convertor = PropertyConvertor.Create(attributeType.OIDSyntax, !attributeType.IsSingle);
                // 檢查轉化器是否正確取得
                if (convertor == null)
                {
                    // 無法正確取得跳過
                    continue;
                }

                // 宣告儲存用的 Tuple
                Tuple<PropertyConvertor, PropertyInfo> tuple = new Tuple<PropertyConvertor, PropertyInfo>(convertor, propertyInfo);
                // 推入儲存
                dictionaryPropertyNameWithTuple.Add(descriptionProperty.PropertyName, tuple);

            }

            // 嘗試刷新目標欄位
            string[] propertyNames = dictionaryPropertyNameWithTuple.Keys.ToArray();
            // 刷新指定項目
            entry.RefreshCache(propertyNames);

            // 逐個檢查是否持有目標特性並設置參數
            foreach(KeyValuePair<string, Tuple<PropertyConvertor, PropertyInfo>> pair in dictionaryPropertyNameWithTuple)
            {
                // 強型別宣告: 屬性名稱
                string propertyName = pair.Key;
                // 檢查是否能取得希望解析的資料
                if (!entry.Properties.Contains(propertyName))
                {
                    // 不能則跳過
                    continue;
                }

                // 強型別宣告: 轉換器
                PropertyConvertor convertor = pair.Value.Item1;
                // 轉換數據內容
                PropertyValue propertyValue = new PropertyValue(convertor, entry.Properties[propertyName]);

                // 強型別宣告: 處理欄位
                PropertyInfo propertyInfo = pair.Value.Item2;
                // 設置資料, 屬性如果不同會直接出錯
                propertyInfo.SetValue(custom, propertyValue.Value);
            }
            // 對外提供
            return custom;
        }

        /// <summary>
        /// 轉換入口物件至目標樣板, 指支援參數屬性:<see cref="ADDescriptionProperty"/> 解析
        /// </summary>
        /// <typeparam name="T">解析目標樣板</typeparam>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="result">搜尋結果</param>
        /// <returns>基礎物件</returns>
        internal T ConvertToCustom<T>(in string account, in string password, in SearchResult result) where T : new()
        {
            // 宣告轉換目標
            T custom = Activator.CreateInstance<T>();

            // 取得部標類型
            Type instanceType = typeof(T);
            // 遍歷所有可用的成員
            foreach (PropertyInfo propertyInfo in instanceType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // 取得屬性描述
                ADDescriptionProperty descriptionProperty = propertyInfo.GetCustomAttribute<ADDescriptionProperty>();
                // 屬性描述不存在就跳過
                if (descriptionProperty == null)
                {
                    // 不存在指定屬性描述
                    continue;
                }

                // 檢查是否能取得希望解析的資料
                if (!result.Properties.Contains(descriptionProperty.PropertyName))
                {
                    // 不能則跳過
                    continue;
                }

                // 取得指定物件的類型
                AttributeType attributeType = StoredSubSchema.GeByName(this, account, password, descriptionProperty.PropertyName, EXPIRES_DURATION);
                // 已知設定內儲存的資料必定是 String(Teletex)
                PropertyConvertor convertor = PropertyConvertor.Create(attributeType.OIDSyntax, !attributeType.IsSingle);
                // 檢查轉化器是否正確取得
                if (convertor == null)
                {
                    // 無法正確取得跳過
                    continue;
                }

                // 轉換數據內容
                PropertyValue propertyValue = new PropertyValue(convertor, result.Properties[descriptionProperty.PropertyName]);
                // 設置資料, 屬性如果不同會直接出錯
                propertyInfo.SetValue(custom, propertyValue.Value);
            }
            // 對外提供
            return custom;
        }

        /// <summary>
        /// 轉換多筆搜尋結果, 指支援參數屬性:<see cref="ADDescriptionProperty"/> 解析
        /// </summary>
        /// <typeparam name="T">解析目標樣板</typeparam>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="results">所有搜尋結果</param>
        /// <returns>所有基礎物件</returns>
        internal T[] ConvertToCustoms<T>(in string account, in string password, params SearchResult[] results) where T : new()
        {
            // 宣告解析用語轉換用的結構
            Array array = Array.CreateInstance(typeof(T), results.Length);
            // 遍歷搜尋物件必定持有的屬性
            for (int index = 0; index < results.Length; index++)
            {
                // 取得轉換目標
                SearchResult searchResult = results[index];
                // 將參數解析結果提供給轉換用的結構紀錄
                T result = ConvertToCustom<T>(account, password, searchResult);
                // 設置
                array.SetValue(result, index);
            }
            // 對外提供
            return array as T[];
        }

        /// <summary>
        /// 務必提供初始化的類別, 支援參數屬性:<see cref="ADDescriptionProperty"/> 與類別:<see cref="ADDescriptionClass"/>解析
        /// </summary>
        /// <typeparam name="T">實做樣板, 建構子必須能接受單元物件</typeparam>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="customUnit">單元元件</param>
        /// <returns>轉換後的目標</returns>
        internal T ConvertToDrive<T>(in string account, in string password, in ADCustomUnit customUnit) where T : ADDrive
        {
            // 取得部標類型
            Type instanceType = typeof(T);
            // 取得最後一個物件類型
            string driveClassName = customUnit.Classes.Last();
            // 取得類別上的屬性設置
            ADDescriptionClass descriptionClassOnDefine = instanceType.GetCustomAttribute<ADDescriptionClass>();
            // 存在且不允許的情況下
            if (descriptionClassOnDefine != null && !descriptionClassOnDefine.IsAllow(driveClassName))
            {
                // 不吻合則會跳過
                return default;
            }

            // 轉換成入口物件: 注意物件如果不存在會直接出錯
            using (DirectoryEntry entry = GetEntryByDN(account, password, customUnit.DistinguishedName))
            {
                // 動作拆分成兩的部分
                Dictionary<string, Tuple<PropertyConvertor, PropertyInfo>> dictionaryNameWithTuple = new Dictionary<string, Tuple<PropertyConvertor, PropertyInfo>>();
                // 遍歷所有可用的成員
                foreach (PropertyInfo propertyInfo in instanceType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    // 取得類別限制
                    ADDescriptionClass descriptionClass = propertyInfo.GetCustomAttribute<ADDescriptionClass>();
                    // 類別限制存在時才進行動作
                    if (descriptionClass != null && !descriptionClass.IsAllow(driveClassName))
                    {
                        // 不吻合則會跳過
                        continue;
                    }

                    // 取得屬性描述
                    ADDescriptionProperty descriptionProperty = propertyInfo.GetCustomAttribute<ADDescriptionProperty>();
                    // 屬性描述不存在就跳過
                    if (descriptionProperty == null)
                    {
                        // 不存在指定屬性描述
                        continue;
                    }

                    // 檢查是否能取得希望解析的資料
                    if (!entry.Properties.Contains(descriptionProperty.PropertyName))
                    {
                        // 不能則跳過
                        continue;
                    }

                    // 取得指定物件的類型
                    AttributeType attributeType = StoredSubSchema.GeByName(this, account, password, descriptionProperty.PropertyName, EXPIRES_DURATION);
                    // 已知設定內儲存的資料必定是 String(Teletex)
                    PropertyConvertor convertor = PropertyConvertor.Create(attributeType.OIDSyntax, !attributeType.IsSingle);
                    // 檢查轉化器是否正確取得
                    if (convertor == null)
                    {
                        // 無法正確取得跳過
                        continue;
                    }

                    // 建立稍後用來處理的資料集合
                    Tuple<PropertyConvertor, PropertyInfo> set = new Tuple<PropertyConvertor, PropertyInfo>(convertor, propertyInfo);
                    // 推入對應解析器
                    dictionaryNameWithTuple.Add(descriptionProperty.PropertyName, set);
                }

                // 不存在任何解析器
                if (dictionaryNameWithTuple.Count == 0)
                {
                    // 對外提供預設值
                    return default;
                }

                // 使用類型實作物件, 提供元件作為初始化條件
                T result = Activator.CreateInstance(
                    instanceType,
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new object[] { customUnit },
                    CultureInfo.InvariantCulture
                ) as T;
                // 遍歷解析器推入資料
                foreach (KeyValuePair<string, Tuple<PropertyConvertor, PropertyInfo>> pair in dictionaryNameWithTuple)
                {
                    // 取得指定屬性嘗試轉換資料
                    PropertyValueCollection collection = entry.Properties[pair.Key];
                    // 使用強協竟錄方便閱讀: 轉換器
                    PropertyConvertor convertor = pair.Value.Item1;

                    // 使用強協竟錄方便閱讀: 設置資訊
                    PropertyInfo propertyInfo = pair.Value.Item2;
                    // 轉換數據內容
                    PropertyValue propertyValue = new PropertyValue(convertor, collection);
                    // 設置資料
                    propertyInfo.SetValue(result, propertyValue.Value);
                }
                // 對外提供
                return result;
            }
        }

        /// <summary>
        /// 務必提供初始化的類別, 支援參數屬性:<see cref="ADDescriptionProperty"/> 與類別:<see cref="ADDescriptionClass"/>解析
        /// </summary>
        /// <typeparam name="T">實做樣板, 建構子必須能接受單元物件</typeparam>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="customUnits">單元元件</param>
        /// <returns>所有轉換的目標</returns>
        internal T[] ConvertToDrives<T>(in string account, in string password, params ADCustomUnit[] customUnits) where T : ADDrive
        {
            // 取得部標類型
            Type type = typeof(T);
            // 宣告用來儲存的陣列
            Array results = Array.CreateInstance(type, customUnits.Length);
            // 逐一轉換
            for (int index = 0; index < customUnits.Length; index++)
            {
                // 取得轉換結果
                T result = ConvertToDrive<T>(account, password, customUnits[index]);
                // 設定至陣列
                results.SetValue(result, index);
            }
            // 必定是目標類型所以可以直接解幫
            return results as T[];
        }
        #endregion
    }
}
