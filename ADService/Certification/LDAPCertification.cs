using ADService.Advanced;
using ADService.Analytical;
using ADService.Environments;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Runtime.InteropServices;

namespace ADService.Certification
{
    /// <summary>
    /// 存取權限證書
    /// </summary>
    public class LDAPCertification
    {
        #region 存取規則描述對照表
        /// <summary>
        /// 右建功能對應的權限解析方法
        /// </summary>
        internal static readonly Dictionary<string, Method> dictionaryMethodWithAnalytical = new List<Method>() {
            new MethodReName(),         // 重新命名
            new MethodMoveTo(),         // 移動
            new MethodChangePassword(), // 重置密碼
            new MethodResetPassword(),  // 強制重設密碼

            new MethodShowDetail(),   // 展示細節
            new MethodModifyDetail(), // 異動細節

            new MethodShowSecurity(),   // 展示安全性
            new MethodModifySecurity(), // 修改安全性: 未完成

            new MethodShowCreateable(),         // 創毽子物件
            new MethodCreateUser(),             // 創建成員
            new MethodCreateGroup(),            // 創建群組
            new MethodCreateOrganizationUnit(), // 創建組織單位
        }.ToDictionary(analytical => analytical.Name);
        #endregion

        /// <summary>
        /// 用來製作入口物件的介面ˇ
        /// </summary>
        internal LDAPConfigurationDispatcher Dispatcher;
        /// <summary>
        /// 喚起行為的物件: 通常為登入者
        /// </summary>
        internal readonly LDAPObject Invoker;
        /// <summary>
        /// 執行的目標
        /// </summary>
        internal readonly LDAPObject Destination;

        /// <summary>
        /// 集合可供外部使用的功能即和類別: 只有 DLL 內部能夠繼承予宣告
        /// </summary>
        /// <param name="dispatcher">入口物件創建器</param>
        /// <param name="invoker">針對目標物件進行行為的喚起物件: 一般為登入者</param>
        /// <param name="destination">目標物件: 可能為喚起物件本身</param>
        internal LDAPCertification(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination)
        {
            Dispatcher = dispatcher;
            Invoker = invoker;
            Destination = destination;
        }

        /// <summary>
        /// 透過指定的證書提供可用方法與其動作協議描述, 如何使用需解析 <see cref="InvokeCondition">協議描述</see>, 目前支援下述項目
        /// <list type="table|number">
        ///    <item> <see cref="Methods.M_RENAME">重新命名</see> </item>
        ///    <item> <see cref="Methods.M_MOVETO">移動至</see> </item>
        ///    <item> <see cref="Methods.M_SHOWDETAIL">物件細節參數</see> </item>
        ///    <item> <see cref="Methods.M_CHANGEPWD">重置密碼</see> </item>
        ///    <item> <see cref="Methods.M_RESETPWD">強制重設密碼</see> </item>
        ///    <item> <see cref="Methods.M_SHOWCRATEABLE">新增</see> </item>
        /// </list>
        /// </summary>
        /// <returns>可用方法與預期接收參數, 格式如右 Dictionary '功能, 協議描述' </returns>
        public Dictionary<string, InvokeCondition> ListAllMethods()
        {
            // 宣告資料異動證書: 使用 Finally 進行釋放
            IDisposable iRelease = null;
            // 過程若出現任何錯誤應被截取會並處理
            try
            {
                // 推入並設置入口物件
                CertificationProperties certification = new CertificationProperties(Dispatcher, Invoker);
                // 轉換成釋放用介面
                iRelease = certification;

                // 整合各 SID 權限狀態
                LDAPPermissions permissions = certification.CreatePermissions(Destination);
                // 整合所有可用全縣權限狀態
                LDAPAccessRules accessRules = certification.CreateAccessRules(Destination);
                // 最多回傳的長度是所有的項目都支援
                Dictionary<string, InvokeCondition> dictionaryAttributeNameWithProtocol = new Dictionary<string, InvokeCondition>(dictionaryMethodWithAnalytical.Count);
                // 遍歷權限並檢查是否可以喚醒
                foreach (Method analyticalRights in dictionaryMethodWithAnalytical.Values)
                {
                    // 不是功能列表展示的項目
                    if (!analyticalRights.IsShowed)
                    {
                        // 跳過
                        continue;
                    }

                    // 取得結果
                    (InvokeCondition condition, _) = analyticalRights.Invokable(ref certification, null, permissions, accessRules);
                    // 無法啟動代表無法呼叫
                    if (condition == null)
                    {
                        // 跳過
                        continue;
                    }

                    // 對外提供支援方法與名稱
                    dictionaryAttributeNameWithProtocol.Add(analyticalRights.Name, condition);
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
        /// 透過指定的證書與可用方法取得對應協定描述, 如何使用需解析 <see cref="InvokeCondition">協議描述</see>, 目前支援下述項目
        /// <list type="table|number">
        ///    <item> <see cref="Methods.M_MODIFYDETAIL">異動細節</see> </item>
        /// </list>
        /// </summary>
        /// <param name="attributeName">目標屬性</param>
        /// <param name="protocol">喚醒時額外提供的協定資料, 預設為 null</param>
        /// <returns>預期接收協議描述</returns>
        public InvokeCondition GetMethodCondition(in string attributeName, in JToken protocol = null)
        {
            // 取得目標屬性分析
            if (!dictionaryMethodWithAnalytical.TryGetValue(attributeName, out Method analytical))
            {
                // 丟出例外: 呼叫喚醒時必須是可以被找到的方法
                throw new LDAPExceptions($"物件:{Destination.DistinguishedName} 於檢驗功能:{attributeName} 時發現尚未實作, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 宣告資料異動證書: 使用 Finally 進行釋放
            IDisposable iRelease = null;
            // 過程若出現任何錯誤應被截取會並處理
            try
            {
                // 是功能列表展示的項目
                if (analytical.IsShowed)
                {
                    // 丟出例外: 取得單一方法條件時必須是可以被隱藏在列表外的方法
                    throw new LDAPExceptions($"物件:{Destination.DistinguishedName} 於檢驗功能:{attributeName} 時發現不能直接呼叫, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                }

                // 推入並設置入口物件
                CertificationProperties certification = new CertificationProperties(Dispatcher, Invoker);
                // 轉換成釋放用介面
                iRelease = certification;

                // 整合各 SID 權向狀態
                LDAPPermissions permissions = certification.CreatePermissions(Destination);
                // 整理繼承關係完成的所有全縣
                LDAPAccessRules accessRules = certification.CreateAccessRules(Destination);
                // 取得結果
                (InvokeCondition condition, _) = analytical.Invokable(ref certification, protocol, permissions, accessRules);
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
        /// <param name="attributeName">指定喚醒的參數</param>
        /// <param name="protocol">需求協議</param>
        /// <returns>此協議的組合內容是否可用</returns>
        public bool AuthenicateMethod(in string attributeName, in JToken protocol)
        {
            // 取得目標屬性分析
            if (!dictionaryMethodWithAnalytical.TryGetValue(attributeName, out Method analytical))
            {
                // 丟出例外: 呼叫喚醒時必須是可以被找到的方法
                throw new LDAPExceptions($"類型:{Destination.DriveClassName} 的物件:{Destination.DistinguishedName} 於檢驗功能:{attributeName} 時發現尚未實作, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 宣告資料異動證書: 使用 Finally 進行釋放
            IDisposable iRelease = null;
            // 過程若出現任何錯誤應被截取會並處理
            try
            {
                // 推入並設置入口物件
                CertificationProperties certification = new CertificationProperties(Dispatcher, Invoker);
                // 轉換成釋放用介面
                iRelease = certification;

                // 整合各 SID 權向狀態
                LDAPPermissions permissions = certification.CreatePermissions(Destination);
                // 整合所有可用全縣權限狀態
                LDAPAccessRules accessRules = certification.CreateAccessRules(Destination);
                // 遍歷權限驗證協議是否可用
                return analytical.Authenicate(ref certification, protocol, permissions, accessRules);
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
        /// <param name="attributeName">指定喚醒的參數</param>
        /// <param name="protocol">需求協議</param>
        /// <returns>此異動影響的物件, 格式如右 Dictionary '區分名稱, 新物件' </returns>
        public Dictionary<string, LDAPObject> InvokeMethod(in string attributeName, in JToken protocol)
        {
            // 取得目標屬性分析
            if (!dictionaryMethodWithAnalytical.TryGetValue(attributeName, out Method analytical))
            {
                // 丟出例外: 呼叫喚醒時必須是可以被找到的方法
                throw new LDAPExceptions($"類型:{Destination.DriveClassName} 的物件:{Destination.DistinguishedName} 於喚醒功能:{attributeName} 時發現尚未實作, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 宣告資料異動證書: 使用 Finally 進行釋放
            IDisposable iRelease = null;
            // 過程若出現任何錯誤應被截取會並處理
            try
            {
                // 推入並設置入口物件
                CertificationProperties certification = new CertificationProperties(Dispatcher, Invoker);
                // 轉換成釋放用介面
                iRelease = certification;

                // 整合各 SID 權向狀態
                LDAPPermissions permissions = certification.CreatePermissions(Destination);
                // 整合所有可用全縣權限狀態
                LDAPAccessRules accessRules = certification.CreateAccessRules(Destination);
                // 驗證是否可用
                bool authenicateSuccess = analytical.Authenicate(ref certification, protocol, permissions, accessRules);
                // 檢查驗證是否成功
                if (!authenicateSuccess)
                {
                    // 失敗或不須異動: 對外提供空物件
                    return new Dictionary<string, LDAPObject>(0);
                }

                // 執行異動或呼叫
                analytical.Invoke(ref certification, protocol, permissions, accessRules);

                // 對外提供所有影響的物件
                Dictionary<string, LDAPObject> dictionaryDistinguishedNameWithLDAPObject = new Dictionary<string, LDAPObject>();
                // 遍歷修改內容
                foreach (KeyValuePair<string, DirectoryEntry> pair in certification.Commited())
                {
                    // 重新製作目標物件
                    LDAPObject reflashObject = LDAPObject.ToObject(pair.Value, Dispatcher);
                    // 轉換物件為空
                    if (reflashObject == null)
                    {
                        // 推入更新完成的物件
                        dictionaryDistinguishedNameWithLDAPObject.Add(pair.Key, null);
                        // 跳過
                        continue;
                    }

                    // 是否為呼叫者
                    bool isInvoker = Invoker.GUID == reflashObject.GUID;
                    // 將資料更新至目標物件
                    LDAPObject storedObject = isInvoker ? Invoker.SwapFrom(reflashObject) : Destination.SwapFrom(reflashObject);
                    // 取得權限
                    LDAPPermissions storedPermissions = certification.CreatePermissions(storedObject);
                    // 是否可以陳列
                    bool isListObject = storedPermissions.IsAllow(storedObject.DriveClassName, ActiveDirectoryRights.ListObject);
                    // 異動自身或者可以陳列時
                    if (!isInvoker && !isListObject)
                    {
                        // 跳過
                        continue;
                    }

                    // 推入更新完成的物件
                    dictionaryDistinguishedNameWithLDAPObject.Add(pair.Key, storedObject);
                }
                // 對外提供修改並同步完成的物件字典
                return dictionaryDistinguishedNameWithLDAPObject;
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
