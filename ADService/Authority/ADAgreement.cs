using ADService.Basis;
using ADService.Certificate;
using ADService.DynamicParse;
using ADService.Environments;
using ADService.Protocol;
using ADService.RootDSE;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ADService.Authority
{
    /// <summary>
    /// 通過保證書與
    /// </summary>
    public class ADAgreement
    {
        #region 創建權利書
        /// <summary>
        /// 根據提供的保證書與指定物件, 產生協議其中會包含權利書
        /// </summary>
        /// <param name="recognizance">保證書</param>
        /// <param name="customUnit">指定單位</param>
        /// <returns>可行駛的操作協議書</returns>
        internal static ADAgreement CreatePermission(in Recognizance recognizance, in ADCustomUnit customUnit)
        {
            // 取得強行別介面
            IUserAuthorization userAuthorization = recognizance.UserAuthorization;
            // 宣告用來儲存組合完成的存取規則表
            List<AccessRuleRelationPermission> accessRuleRelationPermissions = new List<AccessRuleRelationPermission>();
            // 透過保證書的權限取得目標物件的入口物件
            using (DirectoryEntry entry = userAuthorization.GetEntryByDN(customUnit.DistinguishedName))
            {
                // 目標的客製關係結構
                CustomGUIDUnit customGUIDUnit = userAuthorization.ConvertToCustom<CustomGUIDUnit>(entry);

                // 先從指定入口物件處理
                DirectoryEntry rootEntry = entry;
                // 設定目標物件
                CustomGUIDUnit rootCustomGUIDUnit = customGUIDUnit;
                // 處理目標的父層區分名稱
                string parentDistinguishedName = customUnit.OrganizationBelong;

                // 宣告是否具續處理: 第一次必定是能夠處理的
                bool continueProcess = true;
                // 先處指定單位的入口物件再處理所有的父層入口物件
                do
                {
                    // 取得目前處理項目的安全性描述
                    ActiveDirectorySecurity activeDirectorySecurity = rootEntry.ObjectSecurity;
                    // 根據處理目標與處理單元本身的關係決定那些權限的旗標能被處理
                    HashSet<ActiveDirectorySecurityInheritance> processInheritedHashSet;
                    // 處理的項目是否為自己
                    bool isSelf = customUnit.DistinguishedName == rootCustomGUIDUnit.DistinguishedName;
                    // 檢查現在處理的目標單元是否是處理目標自身
                    if (isSelf)
                    {
                        // 是自己時所有項目都可以列出
                        processInheritedHashSet = new HashSet<ActiveDirectorySecurityInheritance>()
                        {
                            ActiveDirectorySecurityInheritance.None,            // 自己
                            ActiveDirectorySecurityInheritance.All,             // 包含自己與所有子物件
                            ActiveDirectorySecurityInheritance.Descendents,     // 包含所有子系物件
                            ActiveDirectorySecurityInheritance.SelfAndChildren, // 包含自己與直接子系物件
                            ActiveDirectorySecurityInheritance.Children,        // 包含直接子系物件
                        };
                    }
                    // 處理的項目不是處理目標自身, 但是直接父層物件
                    else if (customUnit.DistinguishedName == parentDistinguishedName)
                    {
                        // 從直系父層繼承來來的項目部會包含自己
                        processInheritedHashSet = new HashSet<ActiveDirectorySecurityInheritance>()
                        {
                            ActiveDirectorySecurityInheritance.All,             // 包含自己與所有子物件
                            ActiveDirectorySecurityInheritance.Descendents,     // 包含所有子系物件
                            ActiveDirectorySecurityInheritance.SelfAndChildren, // 包含自己與直接子系物件
                            ActiveDirectorySecurityInheritance.Children,        // 包含直接子系物件
                        };
                    }
                    // 處理的項目不是自己亦不是直系父層物件, 那麼非直系父層物件
                    else
                    {
                        // 從非直系父層繼承來來的項目不包含直接子系物件
                        processInheritedHashSet = new HashSet<ActiveDirectorySecurityInheritance>()
                        {
                            ActiveDirectorySecurityInheritance.All,             // 包含自己與所有子物件
                            ActiveDirectorySecurityInheritance.Descendents,     // 包含所有子系物件
                        };
                    }

                    // 先處理物件本身持有的所有權利規則集合
                    AuthorizationRuleCollection authorizationRuleCollectionNoneInherited = activeDirectorySecurity.GetAccessRules(true, false, typeof(NTAccount));
                    // 處理所有非繼承的項目
                    foreach (ActiveDirectoryAccessRule activeDirectoryAccessRule in authorizationRuleCollectionNoneInherited)
                    {
                        // 檢查是否是用來處理的項目
                        if (!processInheritedHashSet.Contains(activeDirectoryAccessRule.InheritanceType))
                        {
                            // 不繼續處理
                            continue;
                        }

                        // 宣告並轉換成儲存結構
                        AccessRuleRelationPermission accessRuleRelationPermission = new AccessRuleRelationPermission(rootCustomGUIDUnit, !isSelf, activeDirectoryAccessRule);
                        // 推入陣列
                        accessRuleRelationPermissions.Add(accessRuleRelationPermission);
                    }

                    // 再取得緩和繼承與非繼承的的權利規則集合
                    AuthorizationRuleCollection authorizationRuleCollection = activeDirectorySecurity.GetAccessRules(true, true, typeof(NTAccount));
                    // 根據兩者數目是否相同決定否繼續處理
                    continueProcess = authorizationRuleCollectionNoneInherited.Count != authorizationRuleCollection.Count;
                    /* 為何需比較不含繼承的存取權限與所有存取權限呢?
                         - 因為目前找不到 DirectoryEntry 中表示安全性主體是否開啟繼承的旗標
                         - 所以只能比較此兩者來半段是否開啟繼承
                       因為若是入口物件含有繼承的項目, 則目標則必定還有與上層相關的繼承權限
                       [TODO] 找到快捷判斷是否開啟繼承的旗標
                    */
                    if (continueProcess)
                    {
                        // 下一次處理的目標資料應透過父層轉換並取得
                        rootCustomGUIDUnit = userAuthorization.ConvertToCustom<CustomGUIDUnit>(rootEntry.Parent);
                        // 下一次處理的應為本次處理的父層
                        rootEntry = rootEntry.Parent;
                    }
                }
                while (continueProcess);

                // 製作權利書
                Permissions permissions = new Permissions(customGUIDUnit, accessRuleRelationPermissions);
                // 對外提供權利書
                return new ADAgreement(recognizance, permissions);
            }
        }
        #endregion

        #region 所有可供執行的條款
        private static Dictionary<string, Article> DictionaryArticleNameWithInstance = new Dictionary<string, Article>()
        {

        };

        /// <summary>
        /// 使用保證書陳列的關係透過權限狀獲得英被諄手的權限
        /// </summary>
        /// <param name="recognizance">保證書</param>
        /// <param name="permissions">權限狀</param>
        private static ExecutionDetails CreateExecutionDetails(in Recognizance recognizance, in Permissions permissions)
        {
            // 獲取授權介面
            IUserAuthorization iUserAuthorization = recognizance.UserAuthorization;
            // 轉成可查詢的 GUID
            string GUID = Configurate.GetGUID(permissions.PrincipalGUID);
            // 找尋操作目標的入口物件
            DirectoryEntry entryPermissions = iUserAuthorization.GetEntryByGUID(GUID);
            // 必須能發現指定目標
            if (entryPermissions == null)
            {
                // 對外扔出例外
                throw new LDAPExceptions($"目標單元:{permissions.PrincipalDN} 於陳列可用條文時無法透過安全序列識別號:{GUID} 取得, 物件已被移除", ErrorCodes.OBJECT_NOTFOUND);
            }

            // 取得可用權限
            List<AccessRuleRelationPermission> accessRuleRelationPermissions = PrincipalAccessRuleRelationPermissions(recognizance, permissions);
            // 推入並設置入口物件
            ExecutionDetails executionDetails = new ExecutionDetails(accessRuleRelationPermissions);
            // 目標項目必定是預期做的項目之一
            executionDetails.SetEntry(permissions.PrincipalDN, entryPermissions);
            // 對外提供可用項的執行細則
            return executionDetails;
        }
        #endregion

        #region 安全性主體解析
        /// <summary>
        /// 系統自訂群組 SELF 的安全性 SID
        /// </summary>
        private static string SID_SELF => ObjectSID.ToSID(new SecurityIdentifier(WellKnownSidType.SelfSid, null));
        /// <summary>
        /// 系統自訂群組 EVERYONE 的安全性 SID
        /// </summary>
        private static string SID_EVERYONE => ObjectSID.ToSID(new SecurityIdentifier(WellKnownSidType.WorldSid, null));

        /// <summary>
        /// 使用保證書陳列的關係透過權限狀獲得英被諄手的權限
        /// </summary>
        /// <param name="recognizance">保證書</param>
        /// <param name="permissions">權限狀</param>
        private static List<AccessRuleRelationPermission> PrincipalAccessRuleRelationPermissions(in Recognizance recognizance, in Permissions permissions)
        {
            // 此協議書的保護條文
            List<AccessRuleRelationPermission> storedAccessRuleRelationPermissions = new List<AccessRuleRelationPermission>();
            // 根據操作目標關係 SID 決定需添加的額外操作權限
            string principalExtraSID = recognizance.PrincipalGUID == permissions.PrincipalGUID ? SID_SELF : SID_EVERYONE;
            // 轉成可查詢的 GUID
            string GUID = Configurate.GetGUID(permissions.PrincipalGUID);
            // 取得喚起物件的關係 SID
            HashSet<string> principalSIDs = new HashSet<string>(recognizance.RelationPrincipalSIDs) { principalExtraSID, recognizance.PrincipalSID };
            // 使用上述關係查詢並過濾出可用的權限
            foreach (string principalSID in principalSIDs)
            {
                // 提供主體以取得這些安全性主體套用的安全存取權限
                AccessRuleRelationPermission[] accessRuleRelationPermissions = permissions.ListWithSID(principalSID);
                // 將上述的安全性規則填入條文
                storedAccessRuleRelationPermissions.AddRange(accessRuleRelationPermissions);
            }
            // 對外提供權限
            return storedAccessRuleRelationPermissions;
        }
        #endregion

        /// <summary>
        /// 可以行使權限的保證書
        /// </summary>
        private readonly Recognizance Recognizance;
        /// <summary>
        /// 目標持有的權利書
        /// </summary>
        private readonly Permissions Permissions;

        /// <summary>
        /// 協議書, 提供可用協議與實施可用操作
        /// </summary>
        /// <param name="recognizance">保證書</param>
        /// <param name="permissions">權限狀</param>
        internal ADAgreement(in Recognizance recognizance, in Permissions permissions)
        {
            Recognizance = recognizance;
            Permissions = permissions;
        }

        /// <summary>
        /// 透過本協議書在目前的保證書與權限狀下取得所有能夠執行的條文
        /// </summary>
        /// <param name="protocolJToken">喚醒協定, 預設為 null</param>
        /// <returns>可用方法與預期接收參數, 格式如右 Dictionary '功能, 條文細則' </returns>
        public Dictionary<string, ADInvokeCondition> ListArticles(in JToken protocolJToken = null)
        {
            // 宣告資料異動證書: 使用 Finally 進行釋放
            IDisposable iRelease = null;
            // 過程若出現任何錯誤應被截取會並處理
            try
            {
                // 製作執行細則
                ExecutionDetails executionDetails = CreateExecutionDetails(Recognizance, Permissions);
                // 轉換成釋放用介面
                iRelease = executionDetails;

                // 最多回傳的長度是所有的項目都支援
                Dictionary<string, ADInvokeCondition> dictionaryAttributeNameWithProtocol = new Dictionary<string, ADInvokeCondition>(DictionaryArticleNameWithInstance.Count);
                // 遍歷權限並檢查是否可以喚醒
                foreach (KeyValuePair<string, Article> pair in DictionaryArticleNameWithInstance)
                {
                    // 強型別宣告: 方便閱讀
                    Article article = pair.Value;
                    // 不是功能列表展示的項目: 非展示條文不會柴列在列表中
                    if (!article.IsShowed)
                    {
                        // 跳過
                        continue;
                    }

                    // 取得結果
                    (ADInvokeCondition condition, _) = article.Able(ref executionDetails, Recognizance, Permissions, protocolJToken);
                    // 無法啟動代表無法呼叫
                    if (condition == null)
                    {
                        // 跳過
                        continue;
                    }

                    // 強型別宣告: 協定名稱
                    string name = pair.Key;
                    // 對外提供支援方法與名稱
                    dictionaryAttributeNameWithProtocol.Add(name, condition);
                }
                // 回傳
                return dictionaryAttributeNameWithProtocol;
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
            finally
            {
                // 釋放異動證書
                iRelease?.Dispose();
            }
        }

        /// <summary>
        /// 透過指定的證書與可用方法取得對應協定描述, 如何使用需解析 <see cref="ADInvokeCondition">協議描述</see>, 目前支援下述項目
        /// </summary>
        /// <param name="articleName">目標屬性</param>
        /// <param name="protocolJToken">喚醒時額外提供的協定資料, 預設為 null</param>
        /// <returns>預期接收協議描述</returns>
        public ADInvokeCondition GeArticleCondition(in string articleName, in JToken protocolJToken = null)
        {
            // 取得目標屬性分析
            if (!DictionaryArticleNameWithInstance.TryGetValue(articleName, out Article article) || article.IsShowed)
            {
                // 丟出例外: 呼叫喚醒時必須是可以被找到的方法
                throw new LDAPExceptions($"物件:{Permissions.PrincipalDN} 於檢驗功能:{articleName} 時不允許呼叫, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 宣告資料異動證書: 使用 Finally 進行釋放
            IDisposable iRelease = null;
            // 過程若出現任何錯誤應被截取會並處理
            try
            {
                // 製作執行細則
                ExecutionDetails executionDetails = CreateExecutionDetails(Recognizance, Permissions);
                // 轉換成釋放用介面
                iRelease = executionDetails;

                // 取得結果
                (ADInvokeCondition condition, _) = article.Able(ref executionDetails, Recognizance, Permissions, protocolJToken);
                // 無法啟動代表無法呼叫
                if (condition == null)
                {
                    // 此時提供空的條件
                    return null;
                }

                // 回傳: 此時條件臂部微空
                return condition;
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
            finally
            {
                // 釋放異動證書
                iRelease?.Dispose();
            }
        }

        /// <summary>
        /// 透過指定的證書提與屬性驗證協定是否可用
        /// </summary>
        /// <param name="articleName">指定喚醒的參數</param>
        /// <param name="protocolJToken">需求協議</param>
        /// <returns>此協議的組合內容是否可用</returns>
        public bool AuthenicateArticle(in string articleName, in JToken protocolJToken)
        {
            // 取得目標屬性分析
            if (!DictionaryArticleNameWithInstance.TryGetValue(articleName, out Article article))
            {
                // 丟出例外: 呼叫喚醒時必須是可以被找到的方法
                throw new LDAPExceptions($"物件:{Permissions.PrincipalDN} 於檢驗功能:{articleName} 時發現尚未實作, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 宣告資料異動證書: 使用 Finally 進行釋放
            IDisposable iRelease = null;
            // 過程若出現任何錯誤應被截取會並處理
            try
            {
                // 製作執行細則
                ExecutionDetails executionDetails = CreateExecutionDetails(Recognizance, Permissions);
                // 轉換成釋放用介面
                iRelease = executionDetails;
                // 遍歷權限驗證協議是否可用
                return article.Authenicate(ref executionDetails, Recognizance, Permissions, protocolJToken);
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
            finally
            {
                // 釋放異動證書
                iRelease?.Dispose();
            }
        }

        /// <summary>
        /// 透過指定的證書提與屬性嘗試喚起方法執行動作
        /// </summary>
        /// <param name="articleName">指定喚醒的參數</param>
        /// <param name="protocolJToken">需求協議</param>
        /// <returns>此異動影響的物件, 格式如右 Dictionary '區分名稱, 新物件' </returns>
        public Dictionary<string, ADCustomUnit> InvokeArticle(in string articleName, in JToken protocolJToken)
        {
            // 取得目標屬性分析
            if (!DictionaryArticleNameWithInstance.TryGetValue(articleName, out Article article))
            {
                // 丟出例外: 呼叫喚醒時必須是可以被找到的方法
                throw new LDAPExceptions($"物件:{Permissions.PrincipalDN} 於檢驗功能:{articleName} 時發現尚未實作, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 宣告資料異動證書: 使用 Finally 進行釋放
            IDisposable iRelease = null;
            // 過程若出現任何錯誤應被截取會並處理
            try
            {
                // 製作執行細則
                ExecutionDetails executionDetails = CreateExecutionDetails(Recognizance, Permissions);
                // 轉換成釋放用介面
                iRelease = executionDetails;

                // 驗證是否可用
                bool authenicateSuccess = article.Authenicate(ref executionDetails, Recognizance, Permissions, protocolJToken);
                // 檢查驗證是否成功
                if (!authenicateSuccess)
                {
                    // 失敗或不須異動: 對外提供空物件
                    return new Dictionary<string, ADCustomUnit>(0);
                }

                // 執行異動或呼叫
                article.Invoke(ref executionDetails, Recognizance, Permissions, protocolJToken);

                // 強型別宣告方便閱讀: 授權介面
                IUserAuthorization iUserAuthorization = Recognizance.UserAuthorization;
                // 對外提供所有影響的物件
                Dictionary<string, ADCustomUnit> dictionaryDNWithCustomUnit = new Dictionary<string, ADCustomUnit>();
                // 遍歷修改內容
                foreach (KeyValuePair<string, DirectoryEntry> pair in executionDetails.Commited())
                {
                    // 強型別宣告方便閱讀
                    DirectoryEntry entry = pair.Value;
                    // 重新製作目標物件
                    ADCustomUnit customUnit = iUserAuthorization.ConvertToCustom<ADCustomUnit>(entry);
                    // 推入更新完成的物件
                    dictionaryDNWithCustomUnit.Add(pair.Key, customUnit);
                }
                // 對外提供修改並同步完成的物件字典
                return dictionaryDNWithCustomUnit;
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
            finally
            {
                // 釋放異動證書
                iRelease?.Dispose();
            }
        }
    }
}
