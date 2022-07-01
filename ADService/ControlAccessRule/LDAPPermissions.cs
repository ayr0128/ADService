using ADService.Details;
using ADService.Environments;
using ADService.Features;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace ADService.ControlAccessRule
{
    /// <summary>
    /// 存放針對執行者對於目標物件的相關存取規則
    /// </summary>
    internal sealed class LDAPPermissions
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
        /// 允許的存取權限
        /// </summary>
        private readonly ControlAccess controlAccess = new ControlAccess();

        /// <summary>
        /// 建構子: 取得指定影響類型的存取權限
        /// </summary>
        /// <param name="dispatcher">設定分配器</param>
        /// <param name="isEffected">指定是否慘生影響以過濾非此類型之外的存取規則: NULL 時全部都取得</param>
        /// <param name="accessRuleConverteds">目前的存取規則</param>
        internal LDAPPermissions(in LDAPConfigurationDispatcher dispatcher, params AccessRuleConverted[] accessRuleConverteds)
        {
            // 取得能產生影響的 GUID
            HashSet<Guid> accessRuleGUIDs = AccessRuleConverted.GetGUIDs(accessRuleConverteds);
            // 取得匹配的所有資: 注意此處的 GUID 是小寫
            Dictionary<string, ControlAccessDetail> dictionaryGUIDWithAccessRuleObjectDetail = dispatcher.GetControlAccessDetail(accessRuleGUIDs);
            // 轉換匹配用陣列
            foreach (AccessRuleConverted accessRuleConverted in accessRuleConverteds)
            {
                // 不產生影響的物件不須進行動作
                if (!accessRuleConverted.IsEffected)
                {
                    // 跳過
                    continue;
                }

                // 將資料轉換成小寫
                string attributeGUIDLower = accessRuleConverted.AttributeGUID.ToString("D").ToLower();
                // 除了全域權限之外都應取得對應的權限資料
                bool isExist = dictionaryGUIDWithAccessRuleObjectDetail.TryGetValue(attributeGUIDLower, out ControlAccessDetail contolAccessDetail);
                // 設置參數
                controlAccess.Set(isExist ? contolAccessDetail.Name : string.Empty, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights);
                // 譨取得時須使用找尋到的目標轉換名稱
                if (contolAccessDetail.UnitType == ControlAccessType.EXTENDEDRIGHT)
                {
                    // 取得指定目標額外權限
                    UnitExtendedRight unitExtendedRight = dispatcher.GetExtendedRight(accessRuleConverted.AttributeGUID);
                    // 取得此額外權限依賴的藍本
                    UnitSchema[] unitSchemas = dispatcher.GetPropertySet(unitExtendedRight);
                    // 取得此安全屬性的關聯物件並設置相關權限
                    foreach (UnitSchema unitSchema in unitSchemas)
                    {
                        // 設置參數
                        controlAccess.Set(unitSchema.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights);
                    }
                }
            }
        }

        /// <summary>
        /// 取得指定屬性職是否存在指定權限
        /// </summary>
        /// <param name="name">目標群取權限</param>
        /// <param name="isInherited">是否重繼承取得, NULL 時會忽略劑成狀態</param>
        /// <param name="accessRuleRightFlagsLimited">任意一個權限存在就是允許</param>
        /// <returns>是否可用</returns>
        internal bool IsAllow(in string name, in bool? isInherited, in AccessRuleRightFlags accessRuleRightFlagsLimited) 
        {
            // 從限制的目標取得的存取權限
            AccessRuleRightFlags accessRuleRightFlags = controlAccess.Get(name, isInherited);
            // 兩者間作 AND 運算, 任意一個權限存在即可
            return (accessRuleRightFlags & accessRuleRightFlagsLimited) != AccessRuleRightFlags.None;
        }

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
            return new LDAPPermissions(dispatcher, accessRuleConverteds);
        }
    }
}
