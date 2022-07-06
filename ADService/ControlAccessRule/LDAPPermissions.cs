using ADService.Details;
using ADService.Environments;
using ADService.Features;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
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
        /// 目標物件的隸屬類別
        /// </summary>
        private readonly UnitSchemaClass[] destinatioUnitSchemaClasses;

        /// <summary>
        /// 建構子: 取得指定影響類型的存取權限
        /// </summary>
        /// <param name="dispatcher">設定分配氣</param>
        /// <param name="invoker">呼叫者</param>
        /// <param name="destination">目標物件</param>
        internal LDAPPermissions(ref LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination)
        {
            // 取得物件持有類別
            string[] classNames = destination.GetPropertyMultiple<string>(Properties.C_OBJECTCLASS);
            // 透過物件持有類別取得所有可用屬性以及所有可用子類別
            destinatioUnitSchemaClasses = dispatcher.GetClasses(classNames);

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

            // 取得所有可用的存取規則: 包含不會生效的部分
            AccessRuleConverted[] accessRuleConverteds = destination.GetAccessRuleConverteds(invokerSecuritySIDHashSet);
            // 設置非空 GUID 的存取權限
            SetControlAccessNoneEmpty(ref dispatcher, accessRuleConverteds);
            // 設置空 GUID 的存取權限
            SetControlAccessWasEmpty(ref dispatcher, accessRuleConverteds);
        }

        /// <summary>
        /// 設置非空 GUID 的存取權限
        /// </summary>
        /// <param name="dispatcher">共用的設定分配氣</param>
        /// <param name="accessRuleConverteds">目前的權限設置</param>
        private void SetControlAccessNoneEmpty(ref LDAPConfigurationDispatcher dispatcher, params AccessRuleConverted[] accessRuleConverteds)
        {
            // 宣告 HashSet
            HashSet<string> unitSchemaClassGUIDHashSet = new HashSet<string>(destinatioUnitSchemaClasses.Length);
            // 遍歷並提供 HashSet
            foreach (UnitSchemaClass unitSchemaClass in destinatioUnitSchemaClasses)
            {
                // 推入 
                unitSchemaClassGUIDHashSet.Add(unitSchemaClass.SchemaGUID.ToLower());
            }

            // 取得可飽含的子類別
            UnitControlAccess[] destinatioUnitControlAccesses = dispatcher.GeControlAccess(destinatioUnitSchemaClasses);
            // 將此類別可用的存取權限轉換成 GUID 對應的字典
            Dictionary<string, UnitControlAccess> dictionaryGUIDithUnitControlAccesses = destinatioUnitControlAccesses.ToDictionary(unitControlAccess => unitControlAccess.GUID.ToLower());
            // 使用查詢 SID 陣列取得所有存取權限 (包含沒有生效的)
            foreach (AccessRuleConverted accessRuleConverted in accessRuleConverteds)
            {
                // 取得對於此存取規則而言是否可用
                bool isEffected = accessRuleConverted.IsEffected(unitSchemaClassGUIDHashSet);
                // 不產生影響的物件不須進行動作
                if (!isEffected || accessRuleConverted.IsEmpty)
                {
                    // 跳過
                    continue;
                }

                // 將資料轉換成小寫
                string attributeGUIDLower = accessRuleConverted.AttributeGUID.ToString("D").ToLower();
                // 先重存取控制的權限開始取得對應資料
                if (dictionaryGUIDithUnitControlAccesses.TryGetValue(attributeGUIDLower, out UnitControlAccess unitControlAccess))
                {
                    // 能從存取權限中取得時必定能從關聯取得對應狀態
                    UnitSchemaAttribute[] unitSchemaAttributes = dispatcher.GetUnitSchemaAttribute(unitControlAccess);
                    // 遍歷得到的所有關聯屬性
                    foreach (UnitSchemaAttribute unitSchemaAttribute in unitSchemaAttributes)
                    {
                        // 設置關聯屬性
                        controlAccess.Set(unitSchemaAttribute.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights);
                    }

                    // 是否為拓展權限
                    bool isExtendedRights = (accessRuleConverted.AccessRuleRights & ActiveDirectoryRights.ExtendedRight) == ActiveDirectoryRights.ExtendedRight;
                    // 沒有任何關聯屬性而且存取權限本身為拓展權限
                    if (unitSchemaAttributes.Length == 0 && isExtendedRights)
                    {
                        // 設置自身的權限
                        controlAccess.Set(unitControlAccess.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights);
                    }
                }
                else
                {
                    // 取得參數名稱
                    UnitSchema unitSchema = dispatcher.GetUnitSchema(accessRuleConverted.AttributeGUID);
                    // 若此  GUID 無法取得屬性值, 這代表此 GUID 為非本物件能支援的存取權限
                    if (unitSchema == null)
                    {
                        // 非本物件支援的存取權限可以跳過
                        continue;
                    }   
                    
                    // 設置自身的權限
                    controlAccess.Set(unitSchema.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, accessRuleConverted.AccessRuleRights);
                }
            }
        }

        /// <summary>
        /// 設置空 GUID 的存取權限
        /// </summary>
        /// <param name="dispatcher">共用的設定分配氣</param>
        /// <param name="accessRuleConverteds">目前的權限設置</param>
        private void SetControlAccessWasEmpty(ref LDAPConfigurationDispatcher dispatcher, params AccessRuleConverted[] accessRuleConverteds)
        {
            // 宣告 HashSet
            HashSet<string> unitSchemaClassGUIDHashSet = new HashSet<string>(destinatioUnitSchemaClasses.Length);
            // 遍歷並提供 HashSet
            foreach (UnitSchemaClass unitSchemaClass in destinatioUnitSchemaClasses)
            {
                // 推入 
                unitSchemaClassGUIDHashSet.Add(unitSchemaClass.SchemaGUID.ToLower());
            }

            // 取得可飽含的子類別
            UnitControlAccess[] destinatioUnitControlAccesses = dispatcher.GeControlAccess(destinatioUnitSchemaClasses);
            // 取得所有允許的屬性
            string[] allowedAttributes = UnitSchemaClass.UniqueAttributeNames(destinatioUnitSchemaClasses);
            // 使用查詢 SID 陣列取得所有存取權限 (包含沒有生效的)
            foreach (AccessRuleConverted accessRuleConverted in accessRuleConverteds)
            {
                // 取得對於此存取規則而言是否可用
                bool isEffected = accessRuleConverted.IsEffected(unitSchemaClassGUIDHashSet);
                // 不產生影響的物件不須進行動作
                if (!isEffected || !accessRuleConverted.IsEmpty)
                {
                    // 跳過
                    continue;
                }

                // 遍歷所有可用的權限
                foreach (UnitControlAccess unitControlAccess in destinatioUnitControlAccesses)
                {
                    // 惡技能填入的瞿縣
                    ActiveDirectoryRights activeDirectoryRightsControlAccesses = accessRuleConverted.AccessRuleRights & unitControlAccess.ValidAccesses;
                    // 不包含任意一組時
                    if (activeDirectoryRightsControlAccesses == 0)
                    {
                        // 跳過
                        continue;
                    }

                    // 能從存取權限中取得時必定能從關聯取得對應狀態
                    UnitSchemaAttribute[] unitSchemaAttributes = dispatcher.GetUnitSchemaAttribute(unitControlAccess);
                    // 遍歷得到的所有關聯屬性
                    foreach (UnitSchemaAttribute unitSchemaAttribute in unitSchemaAttributes)
                    {
                        // 設置關聯屬性
                        controlAccess.Set(unitSchemaAttribute.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, activeDirectoryRightsControlAccesses);
                    }

                    // 是否為拓展權限
                    bool isExtendedRights = (activeDirectoryRightsControlAccesses & ActiveDirectoryRights.ExtendedRight) == ActiveDirectoryRights.ExtendedRight;
                    // 沒有任何關聯屬性而且存取權限本身為拓展權限
                    if (unitSchemaAttributes.Length == 0 && isExtendedRights)
                    {
                        // 設置自身的權限
                        controlAccess.Set(unitControlAccess.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, activeDirectoryRightsControlAccesses);
                    }
                }

                // 檢查是否含有屬性設置
                ActiveDirectoryRights activeDirectoryRightsAttirbutes = accessRuleConverted.AccessRuleRights & UnitSchema.VALIDACCESSES_ATTRIBUTE;
                // 含有屬性設置權限時
                if (activeDirectoryRightsAttirbutes != 0)
                {
                    // 所有可支援的屬性都會被設置成對應類別
                    foreach (UnitSchemaAttribute unitSchemaAttribute in dispatcher.GetUnitSchemaAttribute(allowedAttributes))
                    {
                        // 設置自身的權限
                        controlAccess.Set(unitSchemaAttribute.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, activeDirectoryRightsAttirbutes);
                    }
                }

                // 只對自身類別發生作用的權限
                const ActiveDirectoryRights activeDirectoryRightsSelf = ActiveDirectoryRights.Delete | ActiveDirectoryRights.ListChildren;
                // 檢查是否含有屬性設置
                ActiveDirectoryRights activeDirectoryRightsClass = accessRuleConverted.AccessRuleRights & UnitSchema.VALIDACCESSES_CLASS;
                // 對類別有用的數值去除僅作用於自身的: 即是能使用在子物件上的權限
                ActiveDirectoryRights activeDirectoryRightsClassChild = activeDirectoryRightsClass & ~activeDirectoryRightsSelf;
                // 子物件權限存在時含有屬性設置權限時
                if (activeDirectoryRightsClassChild != 0)
                {
                    // 所有可支援的屬性都會被設置成對應類別
                    foreach (UnitSchemaClass unitSchemaClass in dispatcher.GetChildrenClasess(destinatioUnitSchemaClasses))
                    {
                        // 設置自身的權限
                        controlAccess.Set(unitSchemaClass.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, activeDirectoryRightsClassChild);
                    }
                }

                // 對類別有用的數值去除僅作用於自身的: 即是能使用在自身的權限
                ActiveDirectoryRights activeDirectoryRightsClassSelf = activeDirectoryRightsClass & activeDirectoryRightsSelf;
                // 自身權限存在時含有屬性設置權限時
                if (activeDirectoryRightsClassSelf != 0)
                {
                    // 最後一個物件即為此類別的曲類類型
                    UnitSchemaClass unitSchemaClass = destinatioUnitSchemaClasses.Last();
                    // 設置自身的權限
                    controlAccess.Set(unitSchemaClass.Name, accessRuleConverted.WasAllow, accessRuleConverted.IsInherited, activeDirectoryRightsClassSelf);
                }
            }
        }

        /// <summary>
        /// 取得指定屬性職是否存在指定權限
        /// </summary>
        /// <param name="name">目標群取權限</param>
        /// <param name="activeDirectoryRights">任意一個權限存在就是允許</param>
        /// <returns>是否可用</returns>
        internal bool IsAllow(in string name, in ActiveDirectoryRights activeDirectoryRights) => (controlAccess.Get(name) & activeDirectoryRights) != 0;
    }
}
