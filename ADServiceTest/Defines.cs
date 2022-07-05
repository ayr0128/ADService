using ADService;
using ADService.Certification;
using ADService.Environments;
using ADService.Foundation;
using ADService.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace ADServiceFrameworkTest
{
#pragma warning disable CS0649
    /// <summary>
    /// 模擬收到的可用協定
    /// </summary>
    internal class ClientCondition
    {
        /// <summary>
        /// 協議旗標
        /// </summary>
        public ulong Flags;
        /// <summary>
        /// 協議內容: 根據旗標可得知如何解析
        /// </summary>
        public Dictionary<string, JToken> Details;

        /// <summary>
        /// 是否存在指定旗標
        /// </summary>
        public bool IsContains(in ProtocolAttributeFlags flags) => (Flags & Convert.ToUInt64(flags)) != Convert.ToUInt64(ProtocolAttributeFlags.NONE);

        /// <summary>
        /// 將資料轉換至目標類型
        /// </summary>
        /// <typeparam name="T">目標類型樣板</typeparam>
        /// <param name="name">目標資料</param>
        /// <param name="value">對外提供的數值</param>
        /// <returns>是否成功</returns>
        public bool TryGetValue<T>(in string name, out T value)
        {
            // 先設置成預設
            value = default;
            // 如果未持有任何可用資料
            if (Details == null)
            {
                // 返回不存在
                return false;
            }

            // 取得目標鍵值內容
            bool isExist = Details.TryGetValue(name, out JToken storedValue);
            // 不存在時
            if (!isExist)
            {
                // 返回不存在
                return false;
            }

            // 一般來說存在資料, JToken 就必定存在數值: 此處做個簡易防呆
            if (storedValue == null)
            {
                // 返回成功
                return true;
            }

            // 進行轉轉換
            T convertedValue = storedValue.ToObject<T>();
            // 如果為空
            if (convertedValue == null)
            {
                // 轉換失敗
                return false;
            }

            // 設置對外提供項目
            value = convertedValue;
            // 返回成功
            return true;
        }
    }

    /// <summary>
    /// 專門用於描述隸屬成員的物件
    /// </summary>
    internal struct ClientRelationship
    {
        /// <summary>
        /// 此物件的名稱
        /// </summary>
        public string Name;
        /// <summary>
        /// 此物件的區分名稱
        /// </summary>
        public string DistinguishedName;
        /// <summary>
        /// 此物件的全域唯一標識符
        /// </summary>
        public string GUID;
        /// <summary>
        /// 容器類型
        /// </summary>
        public CategoryTypes Type;
        /// <summary>
        /// 此物件的 SID 資料
        /// </summary>
        public string SID;
        /// <summary>
        /// 是否是主要隸屬關連而來
        /// </summary>
        public bool IsPrimary;
    }

    /// <summary>
    /// 客戶端的重置密碼格式
    /// </summary>
    internal struct ChangePassword
    {
        /// <summary>
        /// 舊密碼
        /// </summary>
        public string From;
        /// <summary>
        /// 新密碼
        /// </summary>
        public string To;
    }
#pragma warning restore CS0649

    /// <summary>
    /// 定義測試會用到的粽多設定
    /// </summary>
    internal class Defines
    {
        // 登入使用者的資訊
        internal const string AccountUser = "黃彥程";
        internal const string Password = "@7847SRX";

        // 根網域的 DNS 資訊
        internal const string RootDCSimple = "ayr.test.idv";
        internal const string RootDCComplex = "DC=ayr,DC=test,DC=idv";

        // 用來測試的部門區分名稱與相關資料
        internal const string OriginOU1 = "OU=新北分部,DC=ayr,DC=test,DC=idv";
        internal const string OriginOU2 = "OU=台中分部,DC=ayr,DC=test,DC=idv";
        internal const string OriginOU3 = "OU=遷移,DC=ayr,DC=test,DC=idv";

        internal const string OriginPERSON1 = "CN=HuangYanChangYC,DC=ayr,DC=test,DC=idv";

        // 用來測試的群組
        internal const string OriginGROUP1 = "CN=w1,OU=新北分部,DC=ayr,DC=test,DC=idv";
        internal const string ModifyGROUP1Name = "m1";
        internal const string OriginGROUP2 = "CN=w2,OU=台中分部,DC=ayr,DC=test,DC=idv";
        internal const string OriginGROUP2Name = "w2";

        // 登入伺服器位置
        internal static readonly LDAPServe Serve = new LDAPUnsecurity("192.168.175.3");
        // 透過登入伺服器取得登入者
        internal static readonly LDAPLogonPerson User = Serve.AuthenticationUser(AccountUser, Password);

        /// <summary>
        /// 驗證點擊自身時的可用方法
        /// </summary>
        /// <param name="user">執行此操作的登入者</param>
        /// <param name="distinguishedName">指定目標區分名稱</param>
        internal static void Test_LDAP_Supported_Features(in LDAPLogonPerson user, in string distinguishedName)
        {
            #region 伺服器端取得對於目標物件的可用方法
            // 取得目標物件
            Dictionary<string, LDAPObject> dictionaryDNWithObject = Serve.GetObjects(user, CategoryTypes.NONE, distinguishedName);
            // 防呆測試: 指定的使用者 (自己) 應該能被發現
            dictionaryDNWithObject.TryGetValue(distinguishedName, out LDAPObject objectTarget);
            // 此時應存在物件
            Assert.IsNotNull(objectTarget, $"指定找尋使用者物件:{distinguishedName} 但無法找到指定物件");
            // 使用登入者與目標物件取得功能證書
            LDAPCertification certificate = Serve.GetCertificate(user, objectTarget);
            // 功能證書在喚起者與目標物件存在的情況下絕對不會為空
            Assert.IsNotNull(certificate, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時應能取得證書");
            // 取得支援的方法
            Dictionary<string, InvokeCondition> dictionaryArrtibuteNameWithCondition = certificate.ListAllMethods();
            // 將資料轉換成 JSON
            JObject protocol = JObject.FromObject(dictionaryArrtibuteNameWithCondition);
            #endregion
            #region 轉換成 JSON 物件提供給客戶端 (模擬封包行為)
            // 轉成 JSON 字串
            Assert.AreNotEqual(string.Empty, protocol.ToString(Newtonsoft.Json.Formatting.Indented));
            #endregion
        }

        /// <summary>
        /// 測試功能中需額外取得資訊的方法
        /// </summary>
        /// <param name="user">執行此操作的登入者</param>
        /// <param name="distinguishedName">指定目標區分名稱</param>
        internal static void Test_LDAP_Feature_InvokeMethod(in LDAPLogonPerson user, in string distinguishedName)
        {
            #region 模擬客戶端指定自身作為操作物件並取得右鍵方法
            // 取得目標物件
            Dictionary<string, LDAPObject> dictionaryDNWithObject = Serve.GetObjects(user, CategoryTypes.NONE, distinguishedName);
            // 防呆測試: 指定的使用者 (自己) 應該能被發現
            dictionaryDNWithObject.TryGetValue(distinguishedName, out LDAPObject objectTarget);
            // 此時應存在物件
            Assert.IsNotNull(objectTarget, $"指定找尋使用者物件:{distinguishedName} 但無法找到指定物件");
            // 使用登入者與目標物件取得功能證書
            LDAPCertification certificate = Serve.GetCertificate(user, objectTarget);
            // 功能證書在喚起者與目標物件存在的情況下絕對不會為空
            Assert.IsNotNull(certificate, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時應能取得證書");
            // 取得支援的方法
            Dictionary<string, InvokeCondition> dictionaryArrtibuteNameWithCondition = certificate.ListAllMethods();
            // 將資料轉換成 JSON
            JObject protocol = JObject.FromObject(dictionaryArrtibuteNameWithCondition);
            #endregion
            #region 模擬客戶端或網頁端呼叫的是展示物件內容方法
            // 假設支援展示物件細節方法: 支援方法中存在可用方法
            const string methodSupported = Methods.M_SHOWDETAIL;
            // 存在方法時: 若特定旗標存在, 則細節中必定存在的功能描述
            const string conditionName = InvokeCondition.METHODCONDITION;
            // 功能描述是展示細節方法時, 內部設定支援方法是異動細節方法
            const string conditionValue = Methods.M_MODIFYDETAIL;

            // 模擬收到封包後嘗試呼叫透定方法: 可呼叫的方法會陳列在封包的 KEY 內
            JToken conditionJSON = protocol.GetValue(methodSupported);
            // 此時應存在物件
            Assert.IsNotNull(conditionJSON, $"協議應支援:{methodSupported} 方法");

            // 展示物件細節的內容解析
            ClientCondition condition = conditionJSON.ToObject<ClientCondition>();
            // 指定方法應支援
            Assert.IsNotNull(condition, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時功能:{methodSupported} 應存在細節條件");
            // 展示物件細節需要依靠呼叫另外的方法取得參數內容: 可以透過協議條件判斷是何種運行方式
            bool isInvokeMethod = condition.IsContains(ProtocolAttributeFlags.INVOKEMETHOD);
            // 簡易防呆
            Assert.IsTrue(isInvokeMethod, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 應為呼叫方法:{ProtocolAttributeFlags.INVOKEMETHOD}");
            // 簡易防呆: 細節用來描述此功能如何使用或有哪些使用參數
            Assert.IsNotNull(condition.Details, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時功能:{methodSupported} 應存在詳細描述");
            // 還有此旗標時: 細節中必定含有呼叫目標方法
            bool isMethodExost = condition.TryGetValue(conditionName, out string method);
            // 簡易防呆
            Assert.IsTrue(isMethodExost, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時 的支援功能:{methodSupported} 應能取得呼叫方法參數:{conditionName}");
            // 簡易防呆
            Assert.IsNotNull(method, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時, 支援功能:{methodSupported} 呼叫方法參數:{conditionName} 內應具有目標方法");
            // 內部測試數值是否正確
            Assert.AreEqual(method, conditionValue, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 呼叫方法參數:{conditionName} 內應為方法:{conditionValue} ");
            #endregion
            #region 至伺服器端取得可用功能及其描述
            // 取得支援方法的展示細節
            InvokeCondition invokeCondition = certificate.GetMethodCondition(method);
            // 簡易防呆1: 應具有可異動與展示細節
            Assert.IsNotNull(invokeCondition, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 呼叫方法參數:{conditionName} 的方法:{conditionValue} 時應提供可用的集合");
            // 將資料轉換成 JSON: 用來傳遞至客戶端的資料
            JObject protocolJSON = JObject.FromObject(invokeCondition);
            // 此時應存在物件
            Assert.IsNotNull(protocolJSON, $"呼叫目標:{method} 應能取得呼叫細節");
            #endregion
            #region 模擬客戶端如何解析可用功能
            // 假設傳遞至客戶端, 透過不同方法進行解析
            ClientCondition conditionProtocol = protocolJSON.ToObject<ClientCondition>();
            // 此時應存在物件
            Assert.IsNotNull(conditionProtocol, $"呼叫目標:{method} 應能將資料轉換成客戶端的呼叫細節");

            // 持有元素旗標時, 代表將持有可用參數
            bool isExist = conditionProtocol.IsContains(ProtocolAttributeFlags.ELEMENTS);
            // 驗證
            Assert.IsTrue(isExist, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 呼叫方法參數:{conditionName} 的方法:{conditionValue} 時應包含:{ProtocolAttributeFlags.ELEMENTS} ");

            // 用來還原用的
            Dictionary<string, object> dictionaryOrigin = new Dictionary<string, object>();
            // 用來異動用的
            Dictionary<string, object> dictionaryModify = new Dictionary<string, object>();
            // 轉換所有可用元素: 這邊有防呆轉換, 實際上可以直接使用的
            bool isElementExist = conditionProtocol.TryGetValue(InvokeCondition.ELEMENTS, out Dictionary<string, ClientCondition> dictionaryNameWithCondition);
            // 驗證
            Assert.IsTrue(isElementExist, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 呼叫方法參數:{conditionName} 的方法:{conditionValue} 時應包含細節:{InvokeCondition.ELEMENTS} ");
            // 驗證
            Assert.IsNotNull(dictionaryNameWithCondition, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 呼叫方法參數:{conditionName} 的方法:{conditionValue} 時細節:{InvokeCondition.ELEMENTS} 應能正確轉換");

            // 遍歷所有參數: 
            foreach (KeyValuePair<string, ClientCondition> pair in dictionaryNameWithCondition)
            {
                // 是否存在數據類型
                bool hasValue = pair.Value.IsContains(ProtocolAttributeFlags.HASVALUE);
                // 不存在數據類型
                if (!hasValue)
                {
                    // 跳過
                    continue;
                }

                // 取得預期的物件類型
                bool isStoredExist = pair.Value.TryGetValue(InvokeCondition.STOREDTYPE, out string storedType);
                // 驗證
                Assert.IsTrue(isStoredExist, $"屬性:{pair.Key} 的欄位:{InvokeCondition.STOREDTYPE} 應可取得");
                // 驗證
                Assert.IsNotNull(storedType, $"屬性:{pair.Key} 的欄位:{InvokeCondition.STOREDTYPE} 應可正確轉換成字串");

                // 是否為陣列
                bool isArray = pair.Value.IsContains(ProtocolAttributeFlags.ISARRAY);
                // 可否編輯
                bool isEditable = pair.Value.IsContains(ProtocolAttributeFlags.EDITABLE);

                // 開始類型轉換
                switch (storedType)
                {
                    // 字串類型
                    case "String":
                        {
                            // 目前所有的字串都是單筆
                            Assert.IsFalse(isArray, $"屬性:{pair.Key} 的欄位:{InvokeCondition.VALUE} 應不可為字串陣列");

                            // 在這方法中存在細節則必定存在資料
                            bool isStringExist = pair.Value.TryGetValue(InvokeCondition.VALUE, out string convertedValue);
                            // 驗證
                            Assert.IsTrue(isStringExist, $"屬性:{pair.Key} 的欄位:{InvokeCondition.VALUE} 應可取得");
                            // 驗證
                            Assert.IsNotNull(convertedValue, $"屬性:{pair.Key} 的欄位:{InvokeCondition.VALUE} 應可正確轉換成字串");

                            // 可編譯的情況: 製作還原用與測試用的相關異動參數
                            if (isEditable)
                            {
                                // 取得預期的物件類型
                                bool isReceivedExist = pair.Value.TryGetValue(InvokeCondition.RECEIVEDTYPE, out string receivedType);
                                // 驗證
                                Assert.IsTrue(isReceivedExist, $"屬性:{pair.Key} 的欄位:{InvokeCondition.RECEIVEDTYPE} 應可取得");
                                // 驗證
                                Assert.IsNotNull(receivedType, $"屬性:{pair.Key} 的欄位:{InvokeCondition.RECEIVEDTYPE} 應可正確轉換成字串");
                                // 驗證
                                Assert.AreEqual(storedType, receivedType, $"屬性:{pair.Key} 的欄位:{InvokeCondition.RECEIVEDTYPE} 應與儲存類型:{InvokeCondition.STOREDTYPE} 相同");

                                // 一動用的固定字串
                                const string MODIFYSUBSTRING = "_";

                                // 先設置還原用的資料
                                dictionaryOrigin.Add(pair.Key, string.IsNullOrEmpty(convertedValue) ? null : convertedValue);
                                // 再設置調整後的資料:原字串加上 '_'
                                dictionaryModify.Add(pair.Key, string.IsNullOrEmpty(convertedValue) ? MODIFYSUBSTRING : convertedValue + MODIFYSUBSTRING);
                            }
                        }
                        break;
                    // 自訂類型物件: 目前這個類別只有 member 跟 memberOf 在使用
                    case "LDAPRelationship":
                        {
                            // 目前 member 跟 memberOf 必定持有陣列
                            Assert.IsTrue(isArray, $"屬性:{pair.Key} 的欄位:{InvokeCondition.VALUE} 應為陣列");

                            // 在這方法中存在細節則必定存在資料
                            bool isRelationShipExist = pair.Value.TryGetValue(InvokeCondition.VALUE, out ClientRelationship[] convertedRelationships);
                            // 驗證
                            Assert.IsTrue(isRelationShipExist, $"屬性:{pair.Key} 的欄位:{InvokeCondition.VALUE} 應可取得");
                            // 驗證
                            Assert.IsNotNull(convertedRelationships, $"屬性:{pair.Key} 的欄位:{InvokeCondition.VALUE} 應可正確轉換成字串");

                            // 在這方法中存在細節則必定存在資料
                            bool isCountExist = pair.Value.TryGetValue(InvokeCondition.COUNT, out int length);
                            // 驗證// 驗證
                            Assert.IsTrue(isCountExist, $"屬性:{pair.Key} 的欄位:{InvokeCondition.COUNT} 應可取得");
                            // 驗證
                            Assert.AreNotEqual(0, length, $"屬性:{pair.Key} 的欄位:{InvokeCondition.COUNT} 應可正確轉換成整數");

                            // 驗證
                            Assert.AreEqual(length, convertedRelationships.Length, $"屬性:{pair.Key} 的欄位:{InvokeCondition.VALUE} 正確轉換至字串類型陣列時長度應相同");

                            // 可編譯的情況: 製作還原用與測試用的相關異動參數
                            if (isEditable)
                            {
                                // 取得預期的物件類型
                                bool isReceivedExist = pair.Value.TryGetValue(InvokeCondition.RECEIVEDTYPE, out string receivedType);
                                // 驗證
                                Assert.IsTrue(isReceivedExist, $"屬性:{pair.Key} 的欄位:{InvokeCondition.RECEIVEDTYPE} 應可取得");
                                // 驗證
                                Assert.IsNotNull(receivedType, $"屬性:{pair.Key} 的欄位:{InvokeCondition.RECEIVEDTYPE} 應可正確轉換成字串");
                                // 驗證
                                Assert.AreEqual(typeof(string).Name, receivedType, $"屬性:{pair.Key} 的欄位:{InvokeCondition.RECEIVEDTYPE} 應與接收類型:{typeof(string).Name} 相同");

                                // 還原用的陣列
                                List<string> recoverList = new List<string>(convertedRelationships.Length);
                                // 填入目前持有的成員或隸屬群組
                                Array.ForEach(convertedRelationships, relationship => recoverList.Add(relationship.DistinguishedName));
                                // 先設置還原用的資料
                                dictionaryOrigin.Add(pair.Key, recoverList);

                                // 調整用的陣列
                                List<string> modifyList = new List<string>(recoverList);
                                // 須根據是 Member 還是 MemberOf 決定異動的動作
                                switch (pair.Key)
                                {
                                    // 添加至成員欄位
                                    case Properties.P_MEMBER:
                                        {
                                            // 使此成員成為群組成員
                                            modifyList.Add(OriginPERSON1);
                                            // 使此群組成為群組成員
                                            modifyList.Add(OriginGROUP1);
                                        }
                                        break;
                                    // 添加至隸屬群組欄位
                                    case Properties.P_MEMBEROF:
                                        {
                                            // 使此群組成為指定群組的成員
                                            modifyList.Add(OriginGROUP1);
                                            // 使此群組成為指定群組的成員
                                            modifyList.Add(OriginGROUP2);
                                        }
                                        break;
                                    default:
                                        {
                                            Assert.Fail($"屬性:{pair.Key} 尚未實作關於異動與還原的處理");
                                        }
                                        break;
                                }
                                // 再設置調整後的資料
                                dictionaryModify.Add(pair.Key, modifyList);
                            }
                        }
                        break;
                }
            }
            #endregion
            #region 模擬客戶端可發送協議至伺服器端進行的異動行為
            // 不論是否經過驗證都可以呼叫執行方法, 但是如果驗證不通過將不產生任何影響
            Dictionary<string, LDAPObject> dictionaryDNWithModified = new Dictionary<string, LDAPObject>();
            // 有異動項目時產生異動
            if (dictionaryModify.Count != 0)
            {
                // 模擬客戶端發送的異動封包格式
                JToken modifiedProtocol = JToken.FromObject(dictionaryModify);
                // 驗證目標協議是否可透過方法進行異動
                bool isAuthenicatableModified = certificate.AuthenicateMethod(method, modifiedProtocol);
                // 驗證
                Assert.IsTrue(isAuthenicatableModified, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時執行異動的驗證方法時:{method} 應通過 ");
                // 不論是否經過驗證都可以呼叫執行方法, 但是如果驗證不通過將不產生任何影響
                dictionaryDNWithModified = certificate.InvokeMethod(method, modifiedProtocol);
            }
            #endregion
            #region 還原剛剛的異動行為
            // 不論是否經過驗證都可以呼叫執行方法, 但是如果驗證不通過將不產生任何影響
            Dictionary<string, LDAPObject> dictionaryDNWithRecover = new Dictionary<string, LDAPObject>();
            // 有異動項目時產生異動
            if (dictionaryModify.Count != 0)
            {
                // 模擬客戶端發送的異動封包格式
                JToken recoverProtocol = JToken.FromObject(dictionaryOrigin);
                // 驗證目標協議是否可透過方法進行異動
                bool isAuthenicatableRecover = certificate.AuthenicateMethod(method, recoverProtocol);
                // 驗證
                Assert.IsTrue(isAuthenicatableRecover, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時執行還原的驗證方法時:{method} 應通過 ");
                // 不論是否經過驗證都可以呼叫執行方法, 但是如果驗證不通過將不產生任何影響
                dictionaryDNWithRecover = certificate.InvokeMethod(method, recoverProtocol);
            }
            #endregion
            #region 驗證異動與還原行為
            // 驗證
            Assert.AreEqual(dictionaryDNWithModified.Count, dictionaryDNWithRecover.Count, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時執行異動方法:{method} 影響項目應相同");
            #endregion
        }

        /// <summary>
        /// 測試重新命名功能
        /// </summary>
        /// <param name="user">執行此操作的登入者</param>
        /// <param name="distinguishedName">指定目標區分名稱</param>
        internal static void Test_LDAP_Feature_ReName(in LDAPLogonPerson user, in string distinguishedName)
        {
            #region 模擬客戶端指定自身作為操作物件並取得右鍵方法
            // 取得目標物件
            Dictionary<string, LDAPObject> dictionaryDNWithObject = Serve.GetObjects(user, CategoryTypes.NONE, distinguishedName);
            // 防呆測試: 指定的使用者 (自己) 應該能被發現
            dictionaryDNWithObject.TryGetValue(distinguishedName, out LDAPObject objectTarget);
            // 此時應存在物件
            Assert.IsNotNull(objectTarget, $"指定找尋使用者物件:{distinguishedName} 但無法找到指定物件");
            // 使用登入者與目標物件取得功能證書
            LDAPCertification certificate = Serve.GetCertificate(user, objectTarget);
            // 功能證書在喚起者與目標物件存在的情況下絕對不會為空
            Assert.IsNotNull(certificate, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時應能取得證書");
            // 取得支援的方法
            Dictionary<string, InvokeCondition> dictionaryArrtibuteNameWithCondition = certificate.ListAllMethods();
            // 將資料轉換成 JSON
            JObject protocol = JObject.FromObject(dictionaryArrtibuteNameWithCondition);
            #endregion
            #region 模擬客戶端或網頁端呼叫的是重新命名
            // 假設支援展示物件細節方法: 支援方法中存在可用方法
            const string methodSupported = Methods.M_RENAME;
            // 模擬收到封包後嘗試呼叫透定方法: 可呼叫的方法會陳列在封包的 KEY 內
            JToken conditionJSON = protocol.GetValue(methodSupported);
            // 此時應存在物件
            Assert.IsNotNull(conditionJSON, $"協議應支援:{methodSupported} 方法");

            // 重新命名的方法解析
            ClientCondition condition = conditionJSON.ToObject<ClientCondition>();
            // 指定方法應支援
            Assert.IsNotNull(condition, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時功能:{methodSupported} 應存在細節條件");
            #endregion
            #region 模擬客戶端發起驗證名稱是否可用: 失敗案例
            // 取得傳入空物件的驗證結果
            bool isAuthenicateNull = certificate.AuthenicateMethod(methodSupported, null);
            // 指定方法應支援
            Assert.IsFalse(isAuthenicateNull, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時應支援功能:{methodSupported} 應不能通過空設置");

            // 取得傳入空物件的重新命名結果
            Dictionary<string, LDAPObject> dictionaryDNWithReNameNull = certificate.InvokeMethod(methodSupported, null);
            // 驗證
            Assert.AreEqual(0, dictionaryDNWithReNameNull.Count, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時執行空物件重新命名時應無影響");

            // 空字串物件轉換
            JToken empty = JToken.FromObject(string.Empty);

            // 取得傳入空字串物件的驗證結果
            bool isAuthenicateEmpty = certificate.AuthenicateMethod(methodSupported, empty);
            // 指定方法應支援
            Assert.IsFalse(isAuthenicateEmpty, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時應支援功能:{methodSupported} 應不能通過空字串物件設置");

            // 取得傳入空物件的重新命名結果
            Dictionary<string, LDAPObject> dictionaryDNWithReNameEmpty = certificate.InvokeMethod(methodSupported, empty);
            // 驗證
            Assert.AreEqual(0, dictionaryDNWithReNameEmpty.Count, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時執行空字串物件重新命名時應無影響");
            #endregion
            #region 模擬客戶端發起驗證名稱是否可用: 成功案例與還原
            // 異動用的固定字串
            const string MODIFYSUBSTRING = "_";

            // 記錄崇興命名前的區分名稱
            string oldDistinguishedName = objectTarget.DistinguishedName;
            // 宣告還原用的名稱
            JToken recoverJToken = JToken.FromObject(objectTarget.Name);
            // 宣告修改用的名稱
            JToken modifiedJToken = JToken.FromObject(objectTarget.Name + MODIFYSUBSTRING);
            // 異動名稱
            Dictionary<string, LDAPObject> dictionaryDNWithModified = certificate.InvokeMethod(methodSupported, modifiedJToken);
            // 須包含目標物件
            Assert.IsTrue(dictionaryDNWithModified.ContainsKey(oldDistinguishedName), $"異動的影響結果應包含:{oldDistinguishedName} ");

            // 記錄崇興命名後的區分名稱: 物件是連棟的
            string newDistinguishedName = objectTarget.DistinguishedName;
            // 還原名稱
            Dictionary<string, LDAPObject> dictionaryDNWithRecover = certificate.InvokeMethod(methodSupported, recoverJToken);
            // 須包含目標物件
            Assert.IsTrue(dictionaryDNWithRecover.ContainsKey(newDistinguishedName), $"異動的影響結果應包含:{newDistinguishedName} ");
            #endregion
        }

        /// <summary>
        /// 測試移動至指定區分名稱下
        /// </summary>
        /// <param name="user">執行此操作的登入者</param>
        /// <param name="distinguishedNameMove">指定目標區分名稱</param>
        /// <param name="distinguishedNameMoveTo">指定移動到區分名稱下</param>
        internal static void Test_LDAP_Feature_MoveTo(in LDAPLogonPerson user, in string distinguishedNameMove, in string distinguishedNameMoveTo)
        {
            #region 模擬客戶端指定自身作為操作物件並取得右鍵方法
            // 取得目標物件
            Dictionary<string, LDAPObject> dictionaryDNWithObject = Serve.GetObjects(user, CategoryTypes.NONE, distinguishedNameMove);
            // 防呆測試: 指定的使用者 (自己) 應該能被發現
            dictionaryDNWithObject.TryGetValue(distinguishedNameMove, out LDAPObject objectTarget);
            // 此時應存在物件
            Assert.IsNotNull(objectTarget, $"指定找尋使用者物件:{distinguishedNameMove} 但無法找到指定物件");
            // 使用登入者與目標物件取得功能證書
            LDAPCertification certificate = Serve.GetCertificate(user, objectTarget);
            // 功能證書在喚起者與目標物件存在的情況下絕對不會為空
            Assert.IsNotNull(certificate, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedNameMove} 時應能取得證書");
            // 取得支援的方法
            Dictionary<string, InvokeCondition> dictionaryArrtibuteNameWithCondition = certificate.ListAllMethods();
            // 將資料轉換成 JSON
            JObject protocol = JObject.FromObject(dictionaryArrtibuteNameWithCondition);
            #endregion
            #region 模擬客戶端或網頁端呼叫的是移動到
            // 假設支援展示物件細節方法: 支援方法中存在可用方法
            const string methodSupported = Methods.M_MOVETO;
            // 模擬收到封包後嘗試呼叫透定方法: 可呼叫的方法會陳列在封包的 KEY 內
            JToken conditionJSON = protocol.GetValue(methodSupported);
            // 此時應存在物件
            Assert.IsNotNull(conditionJSON, $"協議應支援:{methodSupported} 方法");

            // 重新命名的方法解析
            ClientCondition condition = conditionJSON.ToObject<ClientCondition>();
            // 指定方法應支援
            Assert.IsNotNull(condition, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedNameMove} 時功能:{methodSupported} 應存在細節條件");
            #endregion
            #region 模擬客戶端至伺服器端驗證需求
            // 取得目標物件的父層
            bool isParent = objectTarget.GetOrganizationUnit(out string distinguishedNameParent);
            // 指定方法應支援
            Assert.IsTrue(isParent, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedNameMove} 時目標物件應能取得父層組織");

            // 記錄移動前的區分名稱
            string oldDistinguishedName = objectTarget.DistinguishedName;
            // 將移動目標位置轉換成 JToken
            JToken modifiedMoveTo = JToken.FromObject(distinguishedNameMoveTo);
            // 驗證目標位置是否持有權限
            bool isAuthenicateModifiedMoveTo = certificate.AuthenicateMethod(methodSupported, modifiedMoveTo);
            // certificate
            Assert.IsTrue(isAuthenicateModifiedMoveTo, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedNameMove} 時目標物件應能允許遷移");
            // 移動目標物件
            Dictionary<string, LDAPObject> dictionaryDNWithModified = certificate.InvokeMethod(methodSupported, modifiedMoveTo);
            // 須包含目標物件
            Assert.IsTrue(dictionaryDNWithModified.ContainsKey(oldDistinguishedName), $"異動的影響結果應包含:{oldDistinguishedName} ");

            // 記錄崇興命名後的區分名稱: 物件是連棟的
            string newDistinguishedName = objectTarget.DistinguishedName;
            // 將還原目標位置轉換成 JToken
            JToken recoverMoveTo = JToken.FromObject(distinguishedNameParent);
            // 驗證還原位置是否持有權限
            bool isAuthenicateRecoverMoveTo = certificate.AuthenicateMethod(methodSupported, recoverMoveTo);
            // 指定方法應支援
            Assert.IsTrue(isAuthenicateRecoverMoveTo, $"使用者:{user.DistinguishedName} 應能還原指定目標物件:{distinguishedNameMove} 至原始位置:{distinguishedNameParent} ");
            // 還原目標物件
            Dictionary<string, LDAPObject> dictionaryDNWithRecover = certificate.InvokeMethod(methodSupported, recoverMoveTo);
            // 須包含目標物件
            Assert.IsTrue(dictionaryDNWithRecover.ContainsKey(newDistinguishedName), $"異動的影響結果應包含:{newDistinguishedName} ");
            #endregion
        }

        /// <summary>
        /// 測試重置目標密碼
        /// </summary>
        /// <param name="user">執行此操作的登入者</param>
        /// <param name="distinguishedNameMove">指定目標區分名稱</param>
        /// <param name="oldPWD">舊密碼</param>
        /// <param name="newPWD">新密碼</param>
        internal static void Test_LDAP_Feature_ChangePassword(in LDAPLogonPerson user, in string distinguishedNameMove, in string oldPWD, in string newPWD)
        {
            #region 模擬客戶端指定自身作為操作物件並取得右鍵方法
            // 取得目標物件
            Dictionary<string, LDAPObject> dictionaryDNWithObject = Serve.GetObjects(user, CategoryTypes.NONE, distinguishedNameMove);
            // 防呆測試: 指定的使用者 (自己) 應該能被發現
            dictionaryDNWithObject.TryGetValue(distinguishedNameMove, out LDAPObject objectTarget);
            // 此時應存在物件
            Assert.IsNotNull(objectTarget, $"指定找尋使用者物件:{distinguishedNameMove} 但無法找到指定物件");
            // 使用登入者與目標物件取得功能證書
            LDAPCertification certificate = Serve.GetCertificate(user, objectTarget);
            // 功能證書在喚起者與目標物件存在的情況下絕對不會為空
            Assert.IsNotNull(certificate, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedNameMove} 時應能取得證書");
            // 取得支援的方法
            Dictionary<string, InvokeCondition> dictionaryArrtibuteNameWithCondition = certificate.ListAllMethods();
            // 將資料轉換成 JSON
            JObject protocol = JObject.FromObject(dictionaryArrtibuteNameWithCondition);
            #endregion
            #region 模擬客戶端或網頁端呼叫的是移動到
            // 假設支援展示物件細節方法: 支援方法中存在可用方法
            const string methodSupported = Methods.M_CHANGEPWD;
            // 模擬收到封包後嘗試呼叫透定方法: 可呼叫的方法會陳列在封包的 KEY 內
            JToken conditionJSON = protocol.GetValue(methodSupported);
            // 此時應存在物件
            Assert.IsNotNull(conditionJSON, $"協議應支援:{methodSupported} 方法");

            // 重新命名的方法解析
            ClientCondition condition = conditionJSON.ToObject<ClientCondition>();
            // 指定方法應支援
            Assert.IsNotNull(condition, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedNameMove} 時功能:{methodSupported} 應存在重置密碼方法");
            #endregion
            #region 模擬客戶端至伺服器端驗證需求
            // 取得目標物件的父層
            bool isParent = objectTarget.GetOrganizationUnit(out string distinguishedNameParent);
            // 指定方法應支援
            Assert.IsTrue(isParent, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedNameMove} 時目標物件應能取得父層組織");

            // 將重置密碼的協議轉換成 JToken
            JToken modifiedPWD = JToken.FromObject(new ChangePassword { From = oldPWD, To = newPWD });
            // 驗證目標位置是否持有權限
            bool isAuthenicateModifiedpwd = certificate.AuthenicateMethod(methodSupported, modifiedPWD);
            // 指定方法應支援
            Assert.IsTrue(isAuthenicateModifiedpwd, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedNameMove} 時目標物件應能允許重置密碼");
            // 移動目標物件
            Dictionary<string, LDAPObject> dictionaryDNWithModified = certificate.InvokeMethod(methodSupported, modifiedPWD);
            // 須包含目標物件
            Assert.IsTrue(dictionaryDNWithModified.ContainsKey(distinguishedNameMove), $"異動的影響結果應包含:{distinguishedNameMove} ");

            // 將還原密碼的協議轉換成 JToken
            JToken recoverPWD = JToken.FromObject(new ChangePassword { From = newPWD, To = oldPWD });
            // 驗證還原密碼是否持有權限
            bool isAuthenicateRecoverPWD = certificate.AuthenicateMethod(methodSupported, recoverPWD);
            // 指定方法應支援
            Assert.IsTrue(isAuthenicateRecoverPWD, $"使用者:{user.DistinguishedName} 應能還原指定目標物件:{distinguishedNameMove} 至原始位置:{distinguishedNameParent} ");
            // 還原目標物件
            Dictionary<string, LDAPObject> dictionaryDNWithRecover = certificate.InvokeMethod(methodSupported, recoverPWD);
            // 須包含目標物件
            Assert.IsTrue(dictionaryDNWithRecover.ContainsKey(distinguishedNameMove), $"還原的影響結果應包含:{distinguishedNameMove} ");
            #endregion
        }

        /// <summary>
        /// 驗證點擊自身時的可用方法
        /// </summary>
        /// <param name="user">執行此操作的登入者</param>
        /// <param name="distinguishedName">指定目標區分名稱</param>
        internal static void Test_LDAP_Feature_Create(in LDAPLogonPerson user, in string distinguishedName)
        {
            #region 模擬客戶端指定自身作為操作物件並取得右鍵方法
            // 取得目標物件
            Dictionary<string, LDAPObject> dictionaryDNWithObject = Serve.GetObjects(user, CategoryTypes.NONE, distinguishedName);
            // 防呆測試: 指定的使用者 (自己) 應該能被發現
            dictionaryDNWithObject.TryGetValue(distinguishedName, out LDAPObject objectTarget);
            // 此時應存在物件
            Assert.IsNotNull(objectTarget, $"指定找尋使用者物件:{distinguishedName} 但無法找到指定物件");
            // 使用登入者與目標物件取得功能證書
            LDAPCertification certificate = Serve.GetCertificate(user, objectTarget);
            // 功能證書在喚起者與目標物件存在的情況下絕對不會為空
            Assert.IsNotNull(certificate, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時應能取得證書");
            // 取得支援的方法
            Dictionary<string, InvokeCondition> dictionaryArrtibuteNameWithCondition = certificate.ListAllMethods();
            // 將資料轉換成 JSON
            JObject protocol = JObject.FromObject(dictionaryArrtibuteNameWithCondition);
            #endregion
            #region 模擬客戶端或網頁端呼叫的是展示物件內容方法
            // 假設支援展示物件細節方法: 支援方法中存在可用方法
            const string methodSupported = Methods.M_SHOWCRATEABLE;
            // 存在方法時: 若特定旗標存在, 則細節中必定存在的功能描述
            const string conditionName = InvokeCondition.METHODCONDITION;

            // 模擬收到封包後嘗試呼叫透定方法: 可呼叫的方法會陳列在封包的 KEY 內
            JToken conditionJSON = protocol.GetValue(methodSupported);
            // 此時應存在物件
            Assert.IsNotNull(conditionJSON, $"協議應支援:{methodSupported} 方法");

            // 展示物件細節的內容解析
            ClientCondition condition = conditionJSON.ToObject<ClientCondition>();
            // 指定方法應支援
            Assert.IsNotNull(condition, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時功能:{methodSupported} 應存在細節條件");
            // 展示物件細節需要依靠呼叫另外的方法取得參數內容: 可以透過協議條件判斷是何種運行方式
            bool isInvokeMethod = condition.IsContains(ProtocolAttributeFlags.INVOKEMETHOD);
            // 簡易防呆
            Assert.IsTrue(isInvokeMethod, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 應為呼叫方法:{ProtocolAttributeFlags.INVOKEMETHOD}");
            // 簡易防呆: 細節用來描述此功能如何使用或有哪些使用參數
            Assert.IsNotNull(condition.Details, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時功能:{methodSupported} 應存在詳細描述");
            // 內部儲存的資料是陣列需使用陣列解析
            bool isArray = condition.IsContains(ProtocolAttributeFlags.ISARRAY);
            // 簡易防呆
            Assert.IsTrue(isArray, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 應為陣列");
            // 還有此旗標時: 細節中必定含有呼叫目標方法
            bool isMethodExost = condition.TryGetValue(conditionName, out string[] methods);
            // 簡易防呆
            Assert.IsTrue(isMethodExost, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時 的支援功能:{methodSupported} 應能取得呼叫方法參數:{conditionName}");
            // 簡易防呆
            Assert.IsNotNull(methods, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時, 支援功能:{methodSupported} 呼叫方法參數:{conditionName} 內應具有目標方法");
            #endregion
            // 創建完成後應刪除的目標
            HashSet<string> deleteDN = new HashSet<string>();
            // 能執行的指令
            foreach (string method in methods)
            {
                #region 至伺服器端取得可用功能及其描述
                // 取得支援方法的展示細節
                InvokeCondition invokeCondition = certificate.GetMethodCondition(method);
                // 簡易防呆1: 應具有可異動與展示細節
                Assert.IsNotNull(invokeCondition, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 呼叫方法參數:{conditionName} 的方法:{method} 時應提供可用的集合");
                // 將資料轉換成 JSON: 用來傳遞至客戶端的資料
                JObject protocolJSON = JObject.FromObject(invokeCondition);
                // 此時應存在物件
                Assert.IsNotNull(protocolJSON, $"呼叫目標:{method} 應能取得呼叫細節");
                #endregion
                #region 客戶端根據方法提供的資料開始準備需求資料
                // 模擬客戶端解析呼叫後的資料資訊
                ClientCondition clientConditionMethod = protocolJSON.ToObject<ClientCondition>();
                // 展示物件細節需要依靠呼叫另外的方法取得參數內容: 可以透過協議條件判斷是何種運行方式
                bool isEditable = clientConditionMethod.IsContains(ProtocolAttributeFlags.EDITABLE);
                // 簡易防呆
                Assert.IsTrue(isEditable, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 的方法:{method} 應為可被異動:{ProtocolAttributeFlags.EDITABLE}");
                // 應只有兩中方法可以呼叫
                switch (method)
                {
                    case Methods.M_CREATEGROUP:
                        {
                            // 模擬發送應使用的封包
                            CreateGroup createGroup = new CreateGroup();
                            // 假設創建的名稱是
                            createGroup.Name = "32767";

                            // 模擬客戶端發送的異動封包格式
                            JToken modifiedProtocol = JToken.FromObject(createGroup);
                            // 驗證目標協議是否可透過方法進行異動
                            bool isAuthenicatableModified = certificate.AuthenicateMethod(method, modifiedProtocol);
                            // 驗證
                            Assert.IsTrue(isAuthenicatableModified, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時執行異動的驗證方法時:{method} 應通過 ");
                            // 不論是否經過驗證都可以呼叫執行方法, 但是如果驗證不通過將不產生任何影響
                            Dictionary<string, LDAPObject> dictionaryDNWithModified = certificate.InvokeMethod(method, modifiedProtocol);
                            // 推入作為刪除的目標
                            foreach (string dn in dictionaryDNWithModified.Keys)
                            {
                                // 推入作為刪除項目
                                deleteDN.Add(dn);
                            }
                        }
                        break;
                    case Methods.M_CREATEUSER:
                        {
                            // 模擬發送應使用的封包
                            CreateUser createUser = new CreateUser();
                            // 創建使用者有限制需要提供參數
                            bool isPorperties = clientConditionMethod.IsContains(ProtocolAttributeFlags.PROPERTIES);
                            // 簡易防呆
                            Assert.IsTrue(isPorperties, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 的方法:{method} 應具有需求屬性:{ProtocolAttributeFlags.PROPERTIES}");
                            // 取得可填入參數
                            clientConditionMethod.TryGetValue(InvokeCondition.PROPERTIES, out string[] properties);
                            // 簡易防呆
                            Assert.IsNotNull(properties, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時, 支援功能:{methodSupported} 的方法:{method} 應具有屬性:{InvokeCondition.PROPERTIES}");
                            // 故意填入任意文字測試用
                            foreach (string property in properties)
                            {
                                // 設置內容
                                createUser.DictionaryAttributeNameWithValue.Add(property, "_");
                            }
                            // 提供物件名稱
                            createUser.Name = "NewPerson";
                            // 提供使用者帳號
                            createUser.Account = "WahIna";
                            // 提供使用者密碼
                            createUser.Password = "@7847SrX";

                            // 模擬客戶端發送的異動封包格式
                            JToken modifiedProtocol = JToken.FromObject(createUser);
                            // 驗證目標協議是否可透過方法進行異動
                            bool isAuthenicatableModified = certificate.AuthenicateMethod(method, modifiedProtocol);
                            // 驗證
                            Assert.IsTrue(isAuthenicatableModified, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時執行異動的驗證方法時:{method} 應通過 ");
                            // 不論是否經過驗證都可以呼叫執行方法, 但是如果驗證不通過將不產生任何影響
                            Dictionary<string, LDAPObject> dictionaryDNWithModified = certificate.InvokeMethod(method, modifiedProtocol);
                            // 推入作為刪除的目標
                            foreach (string dn in dictionaryDNWithModified.Keys)
                            {
                                // 推入作為刪除項目
                                deleteDN.Add(dn);
                            }
                        }
                        break;
                    default:
                        {
                            // 簡易防呆1: 應具有可異動與展示細節
                            Assert.IsNotNull(invokeCondition, $"使用者:{user.DistinguishedName} 指定目標物件:{distinguishedName} 時的支援功能:{methodSupported} 呼叫方法參數:{conditionName} 的方法:{method} 不應被觸發");
                        }
                        break;
                }
                #endregion
            }
        }
    }
}