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
        private readonly UnitSchemaClass[] destinatioChildrenUnitSchemaClasses;
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
            // 取得自身物件類型
            Dictionary<string, UnitSchemaClass> dictionaryNameWithUnitSchemaClass = unitSchemaClasses.ToDictionary(unitSchemaClass => unitSchemaClass.Name);
            // 透過物件實際類型取得類型藍本
            dictionaryNameWithUnitSchemaClass.TryGetValue(classNames[classNames.Length - 1], out UnitSchemaClass destinationUnitSchemaClass);

            // 取得可飽含的子類別
            destinatioChildrenUnitSchemaClasses = dispatcher.GetChildrenClasess(unitSchemaClasses);
            // 取得此類別支援的存取權限
            destinatioUnitControlAccesses = dispatcher.GeControlAccess(unitSchemaClasses);
            // 組合持有物件類型以及驅動物件類型
            destinationAllowedAttributes = UnitSchemaClass.UniqueAttributeNames(unitSchemaClasses);

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

            // 將此類別可用的存取權限轉換成 GUID 對應的字典
            Dictionary<string, UnitControlAccess> dictionaryGUIDithUnitControlAccesses = destinatioUnitControlAccesses.ToDictionary(unitControlAccess => unitControlAccess.GUID.ToLower());
            // 目標類別持有的存取控制
            dictionaryControlAccessGUIDWithType = new Dictionary<string, ControlAccessType>(destinatioUnitControlAccesses.Length);
            // 使用查詢 SID 陣列取得所有存取權限 (包含沒有生效的)
            foreach (AccessRuleConverted accessRuleConverted in destination.GetAccessRuleConverteds(invokerSecuritySIDHashSet))
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
                    // 所有屬性都需要設置一次全域的設定
                    Array.ForEach(destinationAllowedAttributes, destinationAllowedAttribute =>
                    {
                        controlAccess.Set(destinationAllowedAttribute, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights);
                    });

                    // 支援的類別物件也需要
                    Array.ForEach(destinatioChildrenUnitSchemaClasses, destinatioChildrenUnitSchemaClass => controlAccess.Set(destinatioChildrenUnitSchemaClass.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights) );

                    // 最後設置自身
                    controlAccess.Set(destinationUnitSchemaClass.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights);
                    continue;
                }

                // 將資料轉換成小寫
                string attributeGUIDLower = accessRuleConverted.AttributeGUID.ToString("D").ToLower();
                // 先重存取控制的權限開始取得對應資料
                if (dictionaryGUIDithUnitControlAccesses.TryGetValue(attributeGUIDLower, out UnitControlAccess unitControlAccess))
                {
                    // 能從存取權限中取得時必定能從關聯取得對應狀態
                    ControlAccessType controlAccessType = dispatcher.GetControlAccessAttributes(unitControlAccess, out UnitSchema[] unitSchemas);
                    // 設置存取控制權縣關聯
                    dictionaryControlAccessGUIDWithType.Add(unitControlAccess.Name, controlAccessType);

                    // 設置所有取得的屬性
                    Array.ForEach(unitSchemas, unitSchemaAttribute => controlAccess.Set(unitSchemaAttribute.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights) );

                    // 執行完畢
                    continue;
                }

                // 取得參數名稱
                UnitSchema unitSchema = dispatcher.GetUnitSchema(accessRuleConverted.AttributeGUID);
                // 無法取得對應屬性資料
                if (unitSchema == null)
                {
                    // 代表有不支援的存取璇縣
                    continue;
                }

                controlAccess.Set(unitSchema.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights);
            }
        }

        /// <summary>
        /// 取得指定屬性職是否存在指定權限
        /// </summary>
        /// <param name="name">目標群取權限</param>
        /// <param name="accessRuleRightFlagsLimited">任意一個權限存在就是允許</param>
        /// <returns>是否可用</returns>
        internal bool IsAllow(in string name, in AccessRuleRightFlags accessRuleRightFlagsLimited) => (controlAccess.Get(name) & accessRuleRightFlagsLimited) != AccessRuleRightFlags.None;
    }
}
