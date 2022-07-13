using ADService.Details;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace ADService.Advanced
{
    /// <summary>
    /// 彙整所有目標物建的持有規則
    /// </summary>
    internal class LDAPAccessRules
    {
        /// <summary>
        /// 紀錄目標物件
        /// </summary>
        internal readonly LDAPObject Destination;
        /// <summary>
        /// 所有可用的存取方法
        /// </summary>
        internal readonly UnitControlAccess[] UnitControlAccesses;
        /// <summary>
        /// 所有可用的子物件
        /// </summary>
        internal readonly UnitSchemaClass[] UnitSchemaClasses;
        /// <summary>
        /// 提供給前端查看的協定
        /// </summary>
        internal readonly AccessRuleProtocol[] AccessRuleProtocols;

        /// <summary>
        /// 建構子: 取得轉換並取得目標存取規則
        /// </summary>
        /// <param name="dispatcher">設定分配氣</param>
        /// <param name="destination">目標物件</param>
        internal LDAPAccessRules(ref LDAPConfigurationDispatcher dispatcher, in LDAPObject destination)
        {
            Destination = destination;

            UnitControlAccesses = dispatcher.GeControlAccess(Destination.driveUnitSchemaClasses);
            UnitSchemaClasses = dispatcher.GetChildrenClasess(Destination.driveUnitSchemaClasses);

            // 取得可飽含的子類別
            UnitControlAccess[] destinatioUnitControlAccesses = dispatcher.GeControlAccess(Destination.driveUnitSchemaClasses);
            // 將此類別可用的存取權限轉換成 GUID 對應的字典
            Dictionary<string, UnitControlAccess> dictionaryGUIDithUnitControlAccesses = destinatioUnitControlAccesses.ToDictionary(unitControlAccess => unitControlAccess.GUID.ToLower());
            // 玉器提供的大小事全不規則的大小
            List<AccessRuleProtocol> accessRuleProtocols = new List<AccessRuleProtocol>(Destination.accessRuleSets.Length);
            // 遍歷目標物件持有的存取規則
            foreach (AccessRuleSet accessRuleSet in Destination.accessRuleSets)
            {
                // 先使用從系統取出時的物件名稱
                string unitName = accessRuleSet.UnitName;
                // 安全性流水編號與物件名稱不同時為系統物件
                bool isSystem = unitName != accessRuleSet.SecurityID;
                // 當存取規則的名稱與安全流水編號相同同時代表此存取規則的主體是網域安全性物件或主體, 需要重新查詢一次
                if (!isSystem)
                {
                    // 使用安全性流水編號取得物件
                    using (DirectoryEntry securityEntry = dispatcher.BySID(accessRuleSet.SecurityID))
                    {
                        // 使用物件名稱
                        unitName = LDAPConfiguration.ParseSingleValue<string>(Properties.P_NAME, securityEntry.Properties);
                    }
                }

                // 取得目標控制權限或屬性名稱
                string objectName = string.Empty;
                // 將資料轉換成小寫
                string objectGUIDLower = AccessRuleProtocol.ConvertedGUID(accessRuleSet.Raw.ObjectType);
                // 檢查是否指定目標控制權限或屬性指定目標時
                if (!AccessRuleProtocol.IsGUIDEmpty(accessRuleSet.Raw.ObjectType))
                {
                    // 查看是否能從控制存取權限中取得, 並檢查設旗標是否包含在內容當中
                    if (dictionaryGUIDithUnitControlAccesses.TryGetValue(objectGUIDLower, out UnitControlAccess unitControlAccess) && accessRuleSet.RightMasks(unitControlAccess.AccessRuleControl) != 0)
                    {
                        // 必須能從控制存取權限中發現並且持有相關的權限
                        objectName = unitControlAccess.Name;
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

                        // 此時需要取用物件或屬性名稱
                        objectName = unitSchema.Name;
                    }
                }

                // 取得目標類型名稱
                string inheritedName = string.Empty;
                // 將資料轉換成小寫
                string inheritedGUIDLower = AccessRuleProtocol.ConvertedGUID(accessRuleSet.Raw.InheritedObjectType);
                // 檢查是否指定目標類型物件逕行動作
                if (!AccessRuleProtocol.IsGUIDEmpty(accessRuleSet.Raw.InheritedObjectType))
                {
                    // 取得參數名稱: 此時必定是類型, 而且不會找不到
                    UnitSchema unitSchema = dispatcher.GetUnitSchema(accessRuleSet.Raw.InheritedObjectType);
                    // 此時需要取用物件名稱
                    inheritedName = unitSchema.Name;
                }

                // 轉換成簽名檔
                string signature = AccessRuleProtocol.CreateSignature(unitName, isSystem, accessRuleSet.IsInherited, accessRuleSet.DistinguishedName, objectName, inheritedName, accessRuleSet.Raw);
                // 提供簽名至協定
                AccessRuleProtocol accessRuleProtocol = new AccessRuleProtocol(signature);
                // 推入協定準備提供給外部
                accessRuleProtocols.Add(accessRuleProtocol);
            }

            // 取得轉換的協定
            AccessRuleProtocols = accessRuleProtocols.ToArray();
        }
    }
}
