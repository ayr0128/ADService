using ADService.Details;
using ADService.Environments;
using ADService.Features;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// 紀錄存取控制權縣的類型
        /// </summary>
        private readonly Dictionary<string, ControlAccessType> dictionaryControlAccessGUIDWithType;
        /// <summary>
        /// 目標物件可使用的屬性值
        /// </summary>
        private readonly string[] destinationAllowedAttributes;
        /// <summary>
        /// 目標物件類型可包含的下層物件
        /// </summary>
        private readonly UnitSchemaClass[] destinatioChildrenUnitSchemaClass;
        /// <summary>
        /// 目標物件類型所支援的存取權限
        /// </summary>
        private readonly UnitControlAccess[] destinatioUnitControlAccesses;

        /// <summary>
        /// 建構子: 取得指定影響類型的存取權限
        /// </summary>
        /// <param name="dispatcher">設定分配氣</param>
        /// <param name="invoker">呼叫者</param>
        /// <param name="destination">目標物件</param>
        internal LDAPPermissions(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination)
        {
            // 取得物件持有類別
            string[] classNames = destination.GetPropertyMultiple<string>(Properties.C_OBJECTCLASS);
            // 透過物件持有類別取得所有可用屬性以及所有可用子類別
            UnitSchemaClass[] unitSchemaClasses = dispatcher.GetClasses(classNames);

            // 取得可飽含的子類別
            destinatioChildrenUnitSchemaClass = dispatcher.GetChildrenClasess(unitSchemaClasses);
            // 取得此類別支援的存取權限
            destinatioUnitControlAccesses = dispatcher.GeControlAccess(unitSchemaClasses);
            // 組合持有物件類型以及驅動物件類型
            destinationAllowedAttributes = UnitSchemaClass.UniqueAttributeNames(unitSchemaClasses);
            // 目標類別持有的存取控制
            dictionaryControlAccessGUIDWithType = new Dictionary<string, ControlAccessType>(destinatioUnitControlAccesses.Length);

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

            // 使用查詢 SID 陣列取得所有存取權限 (包含沒有生效的)
            AccessRuleConverted[] accessRuleConverteds = destination.GetAccessRuleConverteds(invokerSecuritySIDHashSet);
            // 取得能產生影響的 GUID
            HashSet<Guid> accessRuleGUIDs = AccessRuleConverted.GetGUIDs(accessRuleConverteds);
            // 將此類別可用的存取權限轉換成 GUID 對應的字典
            Dictionary<string, UnitControlAccess> dictionaryGUIDithUnitControlAccesses = destinatioUnitControlAccesses.ToDictionary(unitControlAccess => unitControlAccess.GUID.ToLower());
            // 轉換匹配用陣列
            foreach (AccessRuleConverted accessRuleConverted in accessRuleConverteds)
            {
                // 不產生影響的物件不須進行動作
                if (!accessRuleConverted.IsEffected)
                {
                    // 跳過
                    continue;
                }

                // 是空GUID時需進行特殊動作
                if (accessRuleConverted.AttributeGUID.Equals(Guid.Empty))
                {
                    Array.ForEach(destinationAllowedAttributes, destinationAllowedAttribute =>
                    {
                        controlAccess.Set(destinationAllowedAttribute, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights);
                    });
                    continue;
                }

                // 將資料轉換成小寫
                string attributeGUIDLower = accessRuleConverted.AttributeGUID.ToString("D").ToLower();
                UnitSchemaAttribute[] unitSchemaAttributes;
                // 先重存取控制的權限開始取得對應資料
                if (dictionaryGUIDithUnitControlAccesses.TryGetValue(attributeGUIDLower, out UnitControlAccess unitControlAccess))
                {
                    // 能從存取權限中取得時必定能從關聯取得對應狀態
                    ControlAccessType controlAccessType = dispatcher.GetControlAccessAttributes(unitControlAccess, out unitSchemaAttributes);
                    // 設置存取控制權縣關聯
                    dictionaryControlAccessGUIDWithType.Add(unitControlAccess.Name, controlAccessType);
                }
                else
                {
                    // 取得參數名稱
                    UnitSchemaAttribute unitSchemaAttribute = dispatcher.GetUnitSchemaAttribute(accessRuleConverted.AttributeGUID);
                    // 推入需設置項目
                    unitSchemaAttributes = new UnitSchemaAttribute[1] { unitSchemaAttribute };
                }

                // 設置所有取得的屬性
                Array.ForEach(unitSchemaAttributes, unitSchemaAttribute =>
                {
                    controlAccess.Set(unitSchemaAttribute.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights);
                });
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
    }
}
