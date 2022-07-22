using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Certificate
{
    /// <summary>
    /// 施行細節: 經協議書認證後留下紀錄的應執行細節
    /// </summary>
    internal class ExecutionDetails : IDisposable
    {
        /// <summary>
        /// 應諄手的權限細則
        /// </summary>
        internal AccessRuleRelationPermission[] AccessRuleRelationPermissions;

        /// <summary>
        /// 通過保證書以及權利狀獲得應遵守的權限與目標使用者
        /// </summary>
        /// <param name="accessRuleRelationPermissions">限制執行細項的條件</param>
        internal ExecutionDetails(in List<AccessRuleRelationPermission> accessRuleRelationPermissions) => AccessRuleRelationPermissions = accessRuleRelationPermissions.ToArray();

        /// <summary>
        /// 提供名稱檢查是否具備相關權限
        /// </summary>
        /// <param name="guid">目標參數的 Guid </param>
        /// <param name="activeDirectoryRights">希望檢驗的存取規則</param>
        /// <returns>此名稱是否可以存取</returns>
        internal bool IsAllow(in Guid guid, in ActiveDirectoryRights activeDirectoryRights)
        {

            return false;
        }

        /// <summary>
        /// 紀錄發生影響的相關入口物件
        /// </summary>
        private readonly Dictionary<string, RequiredCommitSet> dictionaryDNWithCommitSet = new Dictionary<string, RequiredCommitSet>();

        /// <summary>
        /// 取得目前儲存的指定區分名稱入口物件
        /// </summary>
        /// <param name="distinguishedName">指定區分名稱</param>
        /// <returns>指定區分名稱的入口物件</returns>
        internal RequiredCommitSet GetEntry(in string distinguishedName)
        {
            // 嘗試從目前暫存的影響入口物件取得指定的目標
            if (!dictionaryDNWithCommitSet.TryGetValue(distinguishedName, out RequiredCommitSet set))
            {
                // 不存在提供空物件, 外部自行判斷是否需要丟出例外
                return null;
            }

            // 返回找到或創建的目前影響物件
            return set;
        }
        /// <summary>
        /// 將取得的入口物件設置至暫存區
        /// </summary>
        /// <param name="distinguishedName">指定區分名稱</param>
        /// <param name="entry">入口物件</param>
        internal RequiredCommitSet SetEntry(in string distinguishedName, in DirectoryEntry entry)
        {
            // 創建站存結構
            RequiredCommitSet requiredCommitSet = new RequiredCommitSet(entry);
            // 推入字典
            dictionaryDNWithCommitSet.Add(distinguishedName, requiredCommitSet);
            // 提供給外部
            return requiredCommitSet;
        }

        /// <summary>
        /// 推入相關影響後取得入口物件
        /// </summary>
        /// <returns>所有有影響的入口物件, 結構如右: Dictionary'區分名稱, 入口物件' </returns>
        internal Dictionary<string, DirectoryEntry> Commited()
        {
            // 用來儲存總共有多少項目需要提供給外部轉換
            Dictionary<string, DirectoryEntry> dictionarySetByDN = new Dictionary<string, DirectoryEntry>(dictionaryDNWithCommitSet.Count);
            // 遍歷目前註冊有產生影響的物件並取得相關的入口物件
            foreach (KeyValuePair<string, RequiredCommitSet> pair in dictionaryDNWithCommitSet)
            {
                // 取得內容
                RequiredCommitSet set = pair.Value;
                // 喚醒推入動作: 保持程式碼相同
                if (set.InvokedCommit() && !dictionarySetByDN.ContainsKey(pair.Key))
                {
                    // 推入字典黨提供給外部進行資料轉換
                    dictionarySetByDN.Add(pair.Key, set.Entry);
                }
            }

            // 全部異動都推入完成後進行刷新
            foreach (KeyValuePair<string, RequiredCommitSet> pair in dictionaryDNWithCommitSet)
            {
                // 取得內容
                RequiredCommitSet set = pair.Value;
                // 喚醒刷新動作, 之前尚未因為異動而堆入推外提供項目
                if (!dictionarySetByDN.ContainsKey(pair.Key))
                {
                    // 推入字典黨提供給外部進行資料轉換
                    dictionarySetByDN.Add(pair.Key, set.Entry);
                }
            }

            // 轉換成陣列提供給外部
            return dictionarySetByDN;
        }

        /// <summary>
        /// 釋放所有目前使用的資源
        /// </summary>
        void IDisposable.Dispose()
        {
            // 遍歷目前項目釋放所有資源
            foreach (RequiredCommitSet set in dictionaryDNWithCommitSet.Values)
            {
                set.Entry.Dispose(); // 釋放資源
            }
            // 清除所有資料
            dictionaryDNWithCommitSet.Clear();
        }
    }
}
