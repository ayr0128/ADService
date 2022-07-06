using ADService.Protocol;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.ControlAccessRule
{
    /// <summary>
    /// 紀錄的存取規則
    /// </summary>
    internal class InheritedAccessRule
    {
        /// <summary>
        /// 紀錄的存取規則: 由於 Key 值為 BOOL, 所以容器大小必定等於 2
        /// </summary>
        private Dictionary<bool, ActiveDirectoryRights> directionaryInheritedWithFlags = new Dictionary<bool, ActiveDirectoryRights>(2);

        /// <summary>
        /// 內部使用, 設置相關存取權限
        /// </summary>
        /// <param name="isInherited">是否透過繼承取得</param>
        /// <param name="activeDirectoryRights">設置的旗標</param>
        internal void Set(in bool isInherited, in ActiveDirectoryRights activeDirectoryRights)
        {
            // 取得目前繼承狀況的旗標
            if (!directionaryInheritedWithFlags.TryGetValue(isInherited, out ActiveDirectoryRights activeDirectoryRightsWithInherited))
            {
                // 使用劑成狀態將旗標填入
                directionaryInheritedWithFlags.Add(isInherited, activeDirectoryRights);
            }
            else
            {
                // 兩者間實施 OR 運算
                directionaryInheritedWithFlags[isInherited] = activeDirectoryRightsWithInherited | activeDirectoryRights;
            }
        }

        /// <summary>
        /// 使用指定繼承狀態取得存取旗標
        /// </summary>
        /// <returns>指定繼承狀態的</returns>
        internal ActiveDirectoryRights Get()
        {
            // 預計對外回傳項目
            ActiveDirectoryRights activeDirectoryRights = 0;
            // 取得從繼承而來的起標語對外回傳項目進行疊加
            activeDirectoryRights |= directionaryInheritedWithFlags.TryGetValue(true, out ActiveDirectoryRights activeDirectoryRightsInherited) ? activeDirectoryRightsInherited : 0;
            // 取得不是繼承而來的起標語對外回傳項目進行疊加
            activeDirectoryRights |= directionaryInheritedWithFlags.TryGetValue(false, out ActiveDirectoryRights activeDirectoryRightsIsNotInherited) ? activeDirectoryRightsIsNotInherited : 0;
            // 對外提供疊加完成的項目
            return activeDirectoryRights;
        }
    }
}
