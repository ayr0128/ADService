using ADService.Protocol;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.ControlAccessRule
{
    /// <summary>
    /// 紀錄的存取規則
    /// </summary>
    internal class ControlAccess
    {
        /// <summary>
        /// 內部存取用的對應規則: 允許
        /// </summary>
        private readonly Dictionary<string, InheritedAccessRule> dictionaryNameWithInheritedAccessRuleAllowed = new Dictionary<string, InheritedAccessRule>();
        /// <summary>
        /// 內部存取用的對應規則: 拒絕
        /// </summary>
        private readonly Dictionary<string, InheritedAccessRule> dictionaryNameWithInheritedAccessRuleDisllowed = new Dictionary<string, InheritedAccessRule>();

        /// <summary>
        /// 內部使用, 設置相關存取權限
        /// </summary>
        /// <param name="name">目標名稱</param>
        /// <param name="wasAllow">是否允許</param>
        /// <param name="isInherited">是否透過繼承取得</param>
        /// <param name="activeDirectoryRights">設置的旗標</param>
        internal void Set(in string name, in bool wasAllow, in bool isInherited, in ActiveDirectoryRights activeDirectoryRights)
        {
            // 根據允許或拒絕取得實際操作目標
            Dictionary<string, InheritedAccessRule> dictionaryNameWithInheritedAccessRule = wasAllow ? dictionaryNameWithInheritedAccessRuleAllowed : dictionaryNameWithInheritedAccessRuleDisllowed;

            // 取得目標存取規則的存取情況
            if (!dictionaryNameWithInheritedAccessRule.TryGetValue(name, out InheritedAccessRule inheritedAccessRule))
            {
                // 宣告儲存繼承旗標的結構
                inheritedAccessRule = new InheritedAccessRule();
                // 推入此物件
                dictionaryNameWithInheritedAccessRule.Add(name, inheritedAccessRule);
            }

            // 使用劑成狀態將旗標填入
            inheritedAccessRule.Set(isInherited, activeDirectoryRights);
        }

        /// <summary>
        /// 內部使用, 設置相關存取權限
        /// </summary>
        /// <param name="name">目標名稱</param>
        internal ActiveDirectoryRights Get(in string name)
        {
            // 需取得全域設置與指定的名稱屬性
            string[] attributesNames = new string[] { string.Empty, name };

            // 疊加的允許權限
            ActiveDirectoryRights activeDirectoryRightsAllow = 0;
            // 先取得允許
            foreach (string attributesName in attributesNames)
            {
                // 檢查是否有指定目標的純取規則
                if (!dictionaryNameWithInheritedAccessRuleAllowed.TryGetValue(attributesName, out InheritedAccessRule inheritedAccessRule))
                {
                    // 部欑在責跳過
                    continue;
                }

                // 疊加存取規則
                activeDirectoryRightsAllow |= inheritedAccessRule.Get();
            }

            // 疊加的拒絕權限
            ActiveDirectoryRights ActiveDirectoryRightsDeny = 0;
            // 先取得允許
            foreach (string attributesName in attributesNames)
            {
                // 檢查是否有指定目標的純取規則
                if (!dictionaryNameWithInheritedAccessRuleDisllowed.TryGetValue(attributesName, out InheritedAccessRule inheritedAccessRule))
                {
                    // 部欑在責跳過
                    continue;
                }

                // 疊加存取規則
                ActiveDirectoryRightsDeny |= inheritedAccessRule.Get();
            }

            // 允許權限必須被拒絕權限遮蔽
            return activeDirectoryRightsAllow & ~ActiveDirectoryRightsDeny;
        }
    }
}
