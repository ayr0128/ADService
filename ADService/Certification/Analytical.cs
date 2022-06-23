using ADService.Features;
using ADService.Foundation;
using ADService.Media;
using ADService.Permissions;
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
        /// <param name="entriesMedia">入口物件製作器</param>
        /// <param name="invoker">喚起物件</param>
        /// <param name="destination">目標物件</param>
        /// <returns>是否可使用</returns>
        internal abstract (bool, InvokeCondition, string) Invokable(in LDAPEntriesMedia entriesMedia, in LDAPObject invoker, in LDAPObject destination);
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
        /// </summary>
        /// <returns>按屬性鍵值區分的存取權限, 格式如右: Dictionary '屬性鍵值, 存取權限' </returns>
        /// <summary>
        /// 取得換取者對於目標物件的權限
        /// </summary>
        /// <param name="invoker">呼叫者</param>
        /// <param name="destination">目標物件</param>
        /// <param name="limitedSIDs">限制拿取目標的權限, 沒有限制時會根據喚呼叫者與目標物件做判斷</param>
        /// <returns>按屬性鍵值區分的存取權限, 格式如右: Dictionary '屬性鍵值, 存取權限' </returns>
        internal static AccessRuleInformation[] GetAccessRuleInformations(in LDAPObject invoker, in LDAPObject destination, params string[] limitedSIDs)
        {
            // 處理的群組 SID: 一開始會持有的是經過判斷的 SID
            List<string> SIDs = new List<string>();
            // 先判斷外部是否有限制目標
            if (limitedSIDs == null || limitedSIDs.Length == 0)
            {
                // 處理的群組 SID: 一開始會持有的是經過判斷的 SID
                SIDs.Add(invoker.GUID == destination.GUID ? SID_SELF : SID_EVERYONE);
                // 加入所有描述持有的 SID: 每次都重新拿取
                Array.ForEach(invoker is IRevealerMemberOf memberOf ? memberOf.Elements : Array.Empty<LDAPRelationship>(), (relationship) => SIDs.Add(relationship.SID));

                // 取得操作者本身 SID 介面
                if (invoker is IRevealerSID SID)
                {
                    // 加入操作者 SID: 安全性也能指定使用者作為主體
                    SIDs.Add(SID.Value);
                }
            }
            else
            {
                // 加入外部限制項目
                SIDs.AddRange(limitedSIDs);
            }

            // 整合所有權縣
           List<AccessRuleInformation> accessRuleInformations = new List<AccessRuleInformation>();
            // 遍歷想要限制的 SID
            foreach (string SID in SIDs)
            {
                // 取得 SID 應支援的支援所有屬性名稱
                AccessRuleInformation[] permissioAccessRuleInformations = destination.StoredPermissions.GetAccessRuleInformations(SID);
                // 如果不存在或長度為 0, 則當作未持有
                if (permissioAccessRuleInformations == null || permissioAccessRuleInformations.Length == 0)
                {
                    // 跳過: 當作未持有
                    continue;
                }

                // 整合至陣列
                accessRuleInformations.AddRange(permissioAccessRuleInformations);
            }

            // 取得 SID 應支援的支援所有屬性名稱
            return accessRuleInformations.ToArray();
        }
    }
}
