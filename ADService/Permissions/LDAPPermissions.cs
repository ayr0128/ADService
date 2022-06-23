using ADService.Environments;
using ADService.Media;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ADService.Permissions
{
    /// <summary>
    /// 入口物件解析後的存取規則
    /// </summary>
    internal class LDAPPermissions
    {
        /// <summary>
        /// 以 SID 記錄各條存取權限
        /// </summary>
        private readonly Dictionary<string, List<AccessRuleInformation>> dictionarySIDWithPermissions = new Dictionary<string, List<AccessRuleInformation>>();

        /// <summary>
        /// 從入口物件取得的存取規則
        /// </summary>
        /// <param name="entriesMedia">入口物件創建器</param>
        /// <param name="schema">藍本找尋物件</param>
        /// <param name="activeDirectorySecurity"></param>
        internal LDAPPermissions(in LDAPEntriesMedia entriesMedia, in LDAPSchema schema, in ActiveDirectorySecurity activeDirectorySecurity)
        {
            // 取得存取權限集合
            AuthorizationRuleCollection accessRuleCollection = activeDirectorySecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
            // 權限集合不存在不做任何解析動作
            if (accessRuleCollection == null)
            {
                // 
                return;
            }

            // 遍歷持有的存取權限
            foreach (ActiveDirectoryAccessRule accessRule in accessRuleCollection)
            {
                // 識別字串就是 SID, 因為使用 SecurityIdentifier 的類型去取得資料
                string SID = accessRule.IdentityReference.ToString();
                // 此 SID 尚未推入過字典
                if (!dictionarySIDWithPermissions.TryGetValue(SID, out List<AccessRuleInformation> storedList))
                {
                    // 重新宣告用以儲存的列表
                    storedList = new List<AccessRuleInformation>();
                    // 推入字典儲存
                    dictionarySIDWithPermissions.Add(SID, storedList);
                }

                // 是否對外提供
                bool isOfferable;
                // 查看繼承方式決定是否對外提供
                switch (accessRule.InheritanceType)
                {
                    // 僅包含自己
                    case ActiveDirectorySecurityInheritance.None:
                        {
                            /* 若 AD 系統正確運作, 發生繼承時此狀趟應只影響持有最原始權限的物件
                                 - 若此權限從繼承而來, 則不對外轉換
                            */
                            isOfferable = !accessRule.IsInherited;
                        }
                        break;
                    case ActiveDirectorySecurityInheritance.SelfAndChildren: // 包含自己與直接子系物件
                    case ActiveDirectorySecurityInheritance.All:             // 包含自己與所有子系物件
                        {
                            // 若 AD 系統正確運作, 發生繼承時此狀趟應會影響各自應影響的範圍
                            isOfferable = true;
                        }
                        break;
                    case ActiveDirectorySecurityInheritance.Children:    // 僅包含直接子系物件
                    case ActiveDirectorySecurityInheritance.Descendents: // 包含所有子系物件
                        {
                            /* 若 AD 系統正確運作, 發生繼承時此狀趟應只影響持有繼承權限的物件
                                 - 若此權限從繼承而來, 則對外轉換
                            */
                            isOfferable = accessRule.IsInherited;
                        }
                        break;
                    // 其他的預設狀態
                    default:
                        {
                            // 丟出例外: 因為此狀態沒有實作
                            throw new LDAPExceptions($"存取規則:{accessRule.IdentityReference} 設定物件時發現未實作的繼承狀態:{accessRule.InheritanceType} 因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                        }
                }

                // 不可對外提供
                if (!isOfferable)
                {
                    // 跳過所有處理提供空物件
                    continue;
                }

                // 取得目標藍本描述
                SchemaUnit unit = schema.Get(entriesMedia, accessRule.ObjectType);
                // 儲存相關聯的屬性表
                HashSet<string> attributeNameHashSet = null;
                // 從額外權限額來
                if (unit != null && unit.IsExtendRight)
                {
                    // 遍歷物件的關聯設定
                    attributeNameHashSet = schema.GetPropertiesSetSchemaName(entriesMedia, accessRule.ObjectType);
                }

                // 推入此單位的存取權限
                storedList.Add(new AccessRuleInformation(unit, attributeNameHashSet, accessRule));
            }
        }

        /// <summary>
        /// 使用指定群組的 SID 取得所有支援的屬性
        /// </summary>
        /// <param name="limitedSID">群組 SID</param>
        /// <returns>這些群組對應到的權限</returns>
        internal AccessRuleInformation[] GetAccessRuleInformations(in string limitedSID)
        {
            // 取得 SID 關聯存取規則
            dictionarySIDWithPermissions.TryGetValue(limitedSID, out List<AccessRuleInformation> accessRuleInformations);
            // 對外提供資歷
            return accessRuleInformations?.ToArray();
        }
    }
}
