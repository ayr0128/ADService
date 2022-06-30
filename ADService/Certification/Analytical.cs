using ADService.Details;
using ADService.Features;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace ADService.Certification
{
    /// <summary>
    /// 此方法或權限所需求的權限
    /// </summary>
    internal abstract class Analytical
    {
        /// <summary>
        /// 系統自訂群組 SELF 的安全性 SID
        /// </summary>
        internal static string SID_SELF
        {
            get
            {
                SecurityIdentifier self = new SecurityIdentifier(WellKnownSidType.SelfSid, null);
                return self.Translate(typeof(SecurityIdentifier)).ToString();
            }
        }
        /// <summary>
        /// 系統自訂群組 EVERYONE 的安全性 SID
        /// </summary>
        internal static string SID_EVERYONE
        {
            get
            {
                SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                return everyone.Translate(typeof(SecurityIdentifier)).ToString();
            }
        }

        /// <summary>
        /// 註冊給外部使用的方法或功能索引
        /// </summary>
        internal string Name;
        /// <summary>
        /// 是否展示在功能列表
        /// </summary>
        internal bool IsShowed;

        /// <summary>
        /// 基底建構子
        /// </summary>
        /// <param name="name">方法或屬性名稱</param>
        /// <param name="isShowed">是否展示在功能列表</param>
        internal Analytical(in string name, in bool isShowed = true)
        {
            Name = name;
            IsShowed = isShowed;
        }

        /// <summary>
        /// 檢查持有權限能否觸發此方法
        /// </summary>
        /// <param name="dispatcher">入口物件製作器</param>
        /// <param name="invoker">喚起物件</param>
        /// <param name="destination">目標物件</param>
        /// <returns>是否可使用</returns>
        internal abstract (bool, InvokeCondition, string) Invokable(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination);
        /// <summary>
        /// 驗證提供的協議內容是否可用
        /// </summary>
        /// <param name="certification">用來儲存屬性異動的證書</param>
        /// <param name="invoker">喚起物件</param>
        /// <param name="destination">目標物件</param>
        /// <param name="protocol">外部傳遞的協定內容</param>
        /// <returns>此協定是否可用</returns>
        internal abstract bool Authenicate(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol);
        /// <summary>
        /// 觸發此方法
        /// </summary>
        /// <param name="certification">用來儲存屬性異動的證書</param>
        /// <param name="invoker">喚起物件</param>
        /// <param name="destination">目標物件</param>
        /// <param name="protocol">外部傳遞的協定內容</param>
        internal abstract void Invoke(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol);

        /// <summary>
        /// 取得換取者對於目標物件的權限
        /// </summary>
        /// <param name="dispatcher">設定分配氣</param>
        /// <param name="invoker">呼叫者</param>
        /// <param name="destination">目標物件</param>
        /// <returns>按屬性鍵值區分的存取權限, 格式如右: Dictionary '屬性鍵值, 存取權限' </returns>
        internal static LDAPPermissions GetPermissions(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination)
        {
            // 支援的所有安全性群組 SID
            string[] invokerSecuritySIDs = invoker is IRevealerSecuritySIDs revealerSecuritySIDs ? revealerSecuritySIDs.Values : Array.Empty<string>();
            // 轉成 HashSet 判斷喚起者是否為自身
            HashSet<string> invokerSecuritySIDHashSet = new HashSet<string>(invokerSecuritySIDs);
            /* 根據情況決定添加何種額外 SID
                 1. 目標不持有 SID 介面: 視為所有人
                 2. 喚起者與目標非相同物件: 視為所有人
                 3. 其他情況: 是為自己
            */
            string extendedSID = destination is IRevealerSID revealerSID && invokerSecuritySIDHashSet.Contains(revealerSID.Value) ? SID_SELF : SID_EVERYONE;
            // 推入此參數
            invokerSecuritySIDHashSet.Add(extendedSID);

            // 宣告查詢用陣列: 長度是安全性群組的大小
            string[] securitySIDs = new string[invokerSecuritySIDHashSet.Count];
            // 將 安全性群組SID 複製到查詢用的陣列內
            invokerSecuritySIDHashSet.CopyTo(securitySIDs, 0);
            // 使用查詢 SID 陣列取得所有存取權限 (包含沒有生效的)
            AccessRuleConverted[] accessRuleConverteds = destination.StoredProperties.GetAccessRuleConverteds(securitySIDs);
            // 取得 SID 應支援的支援所有屬性名稱
            return new LDAPPermissions(dispatcher, true, accessRuleConverteds);
        }
    }
}
