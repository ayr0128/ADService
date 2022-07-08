using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Advanced
{
    /// <summary>
    /// 紀錄的存取規則
    /// </summary>
    internal class CombinePermissions
    {
        /// <summary>
        /// 內部存取用的對應規則: 允許
        /// </summary>
        private readonly Dictionary<string, ActiveDirectoryRights> dictionaryNameWithActiveDirectoryRightsAllow = new Dictionary<string, ActiveDirectoryRights>();
        /// <summary>
        /// 內部存取用的對應規則: 拒絕
        /// </summary>
        private readonly Dictionary<string, ActiveDirectoryRights> dictionaryNameWithActiveDirectoryRightsDeny = new Dictionary<string, ActiveDirectoryRights>();

        /// <summary>
        /// 內部使用, 設置相關存取權限
        /// </summary>
        /// <param name="name">目標名稱</param>
        /// <param name="wasAllow">是否允許</param>
        /// <param name="activeDirectoryRights">設置的旗標</param>
        internal void Set(in string name, in bool wasAllow, in ActiveDirectoryRights activeDirectoryRights)
        {
            // 根據允許或拒絕取得實際操作目標
            Dictionary<string, ActiveDirectoryRights> dictionaryNameWithActiveDirectoryRights = wasAllow ? dictionaryNameWithActiveDirectoryRightsAllow : dictionaryNameWithActiveDirectoryRightsDeny;

            // 取得目標存取規則的存取情況
            if (!dictionaryNameWithActiveDirectoryRights.TryGetValue(name, out ActiveDirectoryRights activeDirectoryRightsStored))
            {
                // 推入此物件
                dictionaryNameWithActiveDirectoryRights.Add(name, activeDirectoryRights);
            }
            else
            {
                // 使用劑成狀態將旗標填入
                dictionaryNameWithActiveDirectoryRights[name] = activeDirectoryRightsStored | activeDirectoryRights;
            }
        }

        /// <summary>
        /// 內部使用, 設置相關存取權限
        /// </summary>
        /// <param name="name">目標名稱</param>
        internal ActiveDirectoryRights Get(in string name)
        {
            // 疊加的允許權限: 由於儲存的是實體數值所以資料不存在時會提供 0, 因此不會出錯
            dictionaryNameWithActiveDirectoryRightsAllow.TryGetValue(name, out ActiveDirectoryRights activeDirectoryRightsAllow);
            // 疊加的拒絕權限: 由於儲存的是實體數值所以資料不存在時會提供 0, 因此不會出錯
            dictionaryNameWithActiveDirectoryRightsDeny.TryGetValue(name, out ActiveDirectoryRights ActiveDirectoryRightsDeny);
            // 允許權限必須被拒絕權限遮蔽
            return activeDirectoryRightsAllow & ~ActiveDirectoryRightsDeny;
        }
    }
}
