using ADService.Details;
using ADService.Foundation;
using ADService.Media;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.AccessControl;

namespace ADService.Advanced
{
    /// <summary>
    /// 存放針對執行者對於目標物件的相關存取規則
    /// </summary>
    internal sealed class LDAPPermissions
    {
        /// <summary>
        /// 允許的存取權限
        /// </summary>
        private readonly CombinePermissions combinePermissions = new CombinePermissions();
        /// <summary>
        /// 紀錄目標物件
        /// </summary>
        internal readonly LDAPObject Destination;

        /// <summary>
        /// 建構子: 取得指定影響類型的存取權限
        /// </summary>
        /// <param name="dispatcher">設定分配氣</param>
        /// <param name="destination">目標物件</param>
        /// <param name="securitySIDs">關係群組</param>
        internal LDAPPermissions(ref LDAPConfigurationDispatcher dispatcher, in LDAPObject destination, in HashSet<string> securitySIDs)
        {
            Destination = destination;

            // 總長度尚未確定
            AccessRuleSet[] accessRuleSets = destination.GetAccessRuleSets(securitySIDs);
            // 設置非空 GUID 的存取權限
            SetControlAccessNoneEmpty(ref dispatcher, accessRuleSets);
            // 設置空 GUID 的存取權限
            SetControlAccessWasEmpty(ref dispatcher, accessRuleSets);
        }

        #region 解析持有參數與安全性
        /// <summary>
        /// 設置非空 GUID 的存取權限
        /// </summary>
        /// <param name="dispatcher">共用的設定分配氣</param>
        /// <param name="accessRuleSets">目前的權限設置</param>
        private void SetControlAccessNoneEmpty(ref LDAPConfigurationDispatcher dispatcher, params AccessRuleSet[] accessRuleSets)
        {
            // 由於經過排序動作: 自身持有類別的最後一項必定是驅動類別
            UnitSchemaClass destinationUnitSchemaClass = Destination.driveUnitSchemaClasses.Last();

            // 取得可飽含的子類別
            UnitControlAccess[] destinatioUnitControlAccesses = dispatcher.GeControlAccess(Destination.driveUnitSchemaClasses);
            // 將此類別可用的存取權限轉換成 GUID 對應的字典
            Dictionary<string, UnitControlAccess> dictionaryGUIDithUnitControlAccesses = destinatioUnitControlAccesses.ToDictionary(unitControlAccess => unitControlAccess.GUID.ToLower());
            // 使用查詢 SID 陣列取得所有存取權限 (包含沒有生效的)
            foreach (AccessRuleSet accessRuleSet in accessRuleSets)
            {
                // 取得對於此存取規則而言是否可用
                bool isActivable = accessRuleSet.Activable(destinationUnitSchemaClass);
                // 不產生影響的物件不須進行動作
                if (!isActivable || AccessRuleSet.IsGUIDEmpty(accessRuleSet.Raw.ObjectType))
                {
                    // 跳過
                    continue;
                }

                // 是否是允許
                bool isAllow = accessRuleSet.Raw.AccessControlType == AccessControlType.Allow;
                // 將資料轉換成小寫
                string obkectGUIDLower = AccessRuleSet.ConvertedGUID(accessRuleSet.Raw.ObjectType);
                // 先重存取控制的權限開始取得對應資料
                if (dictionaryGUIDithUnitControlAccesses.TryGetValue(obkectGUIDLower, out UnitControlAccess unitControlAccess))
                {
                    // 能從存取權限中取得時必定能從關聯取得對應狀態
                    UnitSchemaAttribute[] unitSchemaAttributes = dispatcher.GetUnitSchemaAttribute(unitControlAccess);
                    // 遍歷得到的所有關聯屬性
                    foreach (UnitSchemaAttribute unitSchemaAttribute in unitSchemaAttributes)
                    {
                        // 設置關聯屬性
                        combinePermissions.Set(unitSchemaAttribute.Name, isAllow, accessRuleSet.Raw.ActiveDirectoryRights);
                    }

                    // 是否為拓展權限
                    bool hasRights = accessRuleSet.RightMasks(ActiveDirectoryRights.ExtendedRight) == ActiveDirectoryRights.ExtendedRight;
                    // 沒有任何關聯屬性而且存取權限本身為拓展權限
                    if (unitSchemaAttributes.Length == 0 && hasRights)
                    {
                        // 設置自身的權限
                        combinePermissions.Set(unitControlAccess.Name, isAllow, accessRuleSet.Raw.ActiveDirectoryRights);
                    }
                }
                else
                {
                    // 取得參數名稱
                    UnitSchema unitSchema = dispatcher.GetUnitSchema(accessRuleSet.Raw.ObjectType);
                    // 若此  GUID 無法取得屬性值, 這代表此 GUID 為非本物件能支援的存取權限
                    if (unitSchema == null)
                    {
                        // 非本物件支援的存取權限可以跳過
                        continue;
                    }
                    
                    // 設置自身的權限
                    combinePermissions.Set(unitSchema.Name, isAllow, accessRuleSet.Raw.ActiveDirectoryRights);
                }
            }
        }

        /// <summary>
        /// 設置空 GUID 的存取權限
        /// </summary>
        /// <param name="dispatcher">共用的設定分配氣</param>
        /// <param name="accessRuleSets">目前的權限設置</param>
        private void SetControlAccessWasEmpty(ref LDAPConfigurationDispatcher dispatcher, params AccessRuleSet[] accessRuleSets)
        {
            // 由於經過排序動作: 自身持有類別的最後一項必定是驅動類別
            UnitSchemaClass destinationUnitSchemaClass = Destination.driveUnitSchemaClasses.Last();
            // 使用查詢 SID 陣列取得所有存取權限 (包含沒有生效的)
            foreach (AccessRuleSet accessRuleSet in accessRuleSets)
            {
                // 取得對於此存取規則而言是否可用
                bool isActivable = accessRuleSet.Activable(destinationUnitSchemaClass);
                // 不產生影響的物件不須進行動作
                if (!isActivable || !AccessRuleSet.IsGUIDEmpty(accessRuleSet.Raw.ObjectType))
                {
                    // 跳過
                    continue;
                }

                // 是否是允許
                bool isAllow = accessRuleSet.Raw.AccessControlType == AccessControlType.Allow;
                // 遍歷所有可用的權限
                foreach (UnitControlAccess unitControlAccess in dispatcher.GeControlAccess(Destination.driveUnitSchemaClasses))
                {
                    // 惡技能填入的瞿縣
                    ActiveDirectoryRights activeDirectoryRightsControlAccesses = accessRuleSet.RightMasks(unitControlAccess.AccessRuleControl);
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
                        combinePermissions.Set(unitSchemaAttribute.Name, isAllow, activeDirectoryRightsControlAccesses);
                    }

                    // 是否為拓展權限
                    bool isExtendedRights = (activeDirectoryRightsControlAccesses & ActiveDirectoryRights.ExtendedRight) == ActiveDirectoryRights.ExtendedRight;
                    // 沒有任何關聯屬性而且存取權限本身為拓展權限
                    if (unitSchemaAttributes.Length == 0 && isExtendedRights)
                    {
                        // 設置自身的權限
                        combinePermissions.Set(unitControlAccess.Name, isAllow, activeDirectoryRightsControlAccesses);
                    }
                }

                // 檢查是否含有屬性設置
                ActiveDirectoryRights activeDirectoryRightsAttirbutes = accessRuleSet.RightMasks(UnitSchema.VALIDACCESSES_ATTRIBUTE);
                // 含有屬性設置權限時
                if (activeDirectoryRightsAttirbutes != 0)
                {
                    // 所有可支援的屬性都會被設置成對應類別
                    foreach (UnitSchemaAttribute unitSchemaAttribute in dispatcher.GetUnitSchemaAttribute(Destination.AllowedAttributeNames))
                    {
                        // 取得是否為可用參數
                        if(!unitSchemaAttribute.isEffecteive)
                        {
                            // 不可用參數需跳過
                            continue;
                        }

                        // 設置自身的權限
                        combinePermissions.Set(unitSchemaAttribute.Name, isAllow, activeDirectoryRightsAttirbutes);
                    }
                }

                // 只對自身類別發生作用的權限
                const ActiveDirectoryRights activeDirectoryRightsSelf = ActiveDirectoryRights.Delete | ActiveDirectoryRights.ListObject;
                // 檢查是否含有屬性設置
                ActiveDirectoryRights activeDirectoryRightsClass = accessRuleSet.RightMasks(UnitSchema.VALIDACCESSES_CLASS);
                // 對類別有用的數值去除僅作用於自身的: 即是能使用在子物件上的權限
                ActiveDirectoryRights activeDirectoryRightsClassChild = activeDirectoryRightsClass & ~activeDirectoryRightsSelf;
                // 子物件權限存在時含有屬性設置權限時
                if (activeDirectoryRightsClassChild != 0)
                {
                    // 所有可支援的屬性都會被設置成對應類別
                    foreach (UnitSchemaClass unitSchemaClass in dispatcher.GetChildrenClasess(Destination.driveUnitSchemaClasses))
                    {
                        // 設置自身的權限
                        combinePermissions.Set(unitSchemaClass.Name, isAllow, activeDirectoryRightsClassChild);
                    }
                }

                // 對類別有用的數值去除僅作用於自身的: 即是能使用在自身的權限
                ActiveDirectoryRights activeDirectoryRightsClassSelf = activeDirectoryRightsClass & activeDirectoryRightsSelf;
                // 過濾部分會因繼承關係產生轉換的屬性: 刪除子物件
                bool isContainDeleteChild = (activeDirectoryRightsClass & ActiveDirectoryRights.DeleteChild) == ActiveDirectoryRights.DeleteChild;
                // 在繼承的情況下: 刪除子物件應被轉匯為刪除
                activeDirectoryRightsClassSelf |= isContainDeleteChild && accessRuleSet.IsInherited ? ActiveDirectoryRights.Delete : 0;
                // 過濾部分會因繼承關係產生轉換的屬性: 陳列子物件
                bool isContainListChildren = (activeDirectoryRightsClass & ActiveDirectoryRights.ListChildren) == ActiveDirectoryRights.ListChildren;
                // 在繼承的情況下: 陳列子物件應被轉匯為陳列
                activeDirectoryRightsClassSelf |= isContainListChildren && accessRuleSet.IsInherited ? ActiveDirectoryRights.ListObject : 0;
                // 自身權限存在時含有屬性設置權限時
                if (activeDirectoryRightsClassSelf != 0)
                {
                    // 使用驅動類別的名稱註冊作為權限持有目標
                    combinePermissions.Set(destinationUnitSchemaClass.Name, isAllow, activeDirectoryRightsClassSelf);
                }
            }
        }
        #endregion

        /// <summary>
        /// 取得指定屬性職是否存在指定權限
        /// </summary>
        /// <param name="name">目標群取權限</param>
        /// <param name="activeDirectoryRights">任意一個權限存在就是允許</param>
        /// <returns>是否可用</returns>
        internal bool IsAllow(in string name, in ActiveDirectoryRights activeDirectoryRights) => (combinePermissions.Get(name) & activeDirectoryRights) != 0;
    }
}
