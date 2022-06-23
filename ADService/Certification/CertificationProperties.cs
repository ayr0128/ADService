using ADService.Media;
using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Certification
{
    /// <summary>
    /// 傳遞修改內容證書
    /// </summary>
    internal sealed class CertificationProperties : IDisposable
    {
        #region 內部呼叫用格式
        /// <summary>
        /// 儲存的入口物件是否需要被簽入異動
        /// </summary>
        private sealed class RequiredCommitSet
        {
            /// <summary>
            /// 儲存的入口物件
            /// </summary>
            internal DirectoryEntry Entry { get; private set; }
            /// <summary>
            /// 儲存的入口物件是否需要被簽入異動
            /// </summary>
            internal bool RequiredCommit { get; private set; }
            /// <summary>
            /// 宣告一個暴露的事件註冊器, 用來在推入完成後更新自己
            /// </summary>
            internal event Action<DirectoryEntry> OnCommitedFinish = null;

            /// <summary>
            /// 推入入口物件並預設為不須簽入
            /// </summary>
            /// <param name="entry">入口物件</param>
            internal RequiredCommitSet(in DirectoryEntry entry)
            {
                RequiredCommit = false; // 預設: 沒有被異動不須簽入
                Entry = entry;
            }

            /// <summary>
            /// 有被異動因此需要簽入
            /// </summary>
            internal void Modified() => RequiredCommit = true;
            /// <summary>
            /// 事件需要由方法內部喚起
            /// </summary>
            /// <returns>是否存在推入後需修改事件</returns>
            internal bool InvokedCommit()
            {
                // 曾經異動過
                if (RequiredCommit)
                {
                    // 需要推入異動
                    Entry.CommitChanges();
                }

                // 喚起完成行為
                OnCommitedFinish?.Invoke(Entry);
                // 返回存在簽入後事件
                return OnCommitedFinish != null | RequiredCommit;
            }
        }
        #endregion

        /// <summary>
        /// 紀錄外部提供的入口物件創建器
        /// </summary>
        internal readonly LDAPEntriesMedia EntriesMedia;

        /// <summary>
        /// 初始化時須提供持有此簽證的持有者入口物件
        /// </summary>
        /// <param name="entriesMedia">物件分析氣</param>
        /// <param name="distinguishedName">持有者區分名稱</param>
        internal CertificationProperties(in LDAPEntriesMedia entriesMedia, in string distinguishedName)
        {
            EntriesMedia = entriesMedia;

            // 取得入口物件
            DirectoryEntry entry = EntriesMedia.ByDistinguisedName(distinguishedName);
            // 推入入口物件
            dictionaryDistinguishedNameWithEntry.Add(distinguishedName, new RequiredCommitSet(entry));
        }

        /// <summary>
        /// 紀錄發生影響的相關入口物件
        /// </summary>
        private readonly Dictionary<string, RequiredCommitSet> dictionaryDistinguishedNameWithEntry = new Dictionary<string, RequiredCommitSet>();

        /// <summary>
        /// 取得目前儲存的指定區分名稱入口物件
        /// </summary>
        /// <param name="distinguishedName">指定區分名稱</param>
        /// <returns>指定區分名稱的入口物件</returns>
        internal DirectoryEntry GetEntry(in string distinguishedName)
        {
            // 嘗試從目前暫存的影響入口物件取得指定的目標
            if (!dictionaryDistinguishedNameWithEntry.TryGetValue(distinguishedName, out RequiredCommitSet set))
            {
                // 不存在提供空物件, 外部自行判斷是否需要丟出例外
                return null;
            }

            // 返回找到或創建的目前影響物件
            return set.Entry;
        }
        /// <summary>
        /// 將取得的入口物件設置至暫存區
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="distinguishedName">指定區分名稱</param>
        internal void SetEntry(in DirectoryEntry entry, in string distinguishedName) => dictionaryDistinguishedNameWithEntry.Add(distinguishedName, new RequiredCommitSet(entry));

        /// <summary>
        /// 設定某個區分名稱有異動, 需要產生簽入行為
        /// </summary>
        /// <param name="distinguishedName">區分名稱</param>
        /// <returns>此區分名稱是否存在設置成功</returns>
        internal bool RequiredCommit(in string distinguishedName)
        {
            // 嘗試從目前暫存的影響入口物件取得指定的目標
            if (!dictionaryDistinguishedNameWithEntry.TryGetValue(distinguishedName, out RequiredCommitSet set))
            {
                // 不存在丟出未設置, 外部自行判斷是否出錯
                return false;
            }

            // 將此區分名稱設置為需要簽入
            set.Modified();
            // 設置成功
            return true;
        }

        /// <summary>
        /// 設定某個區分名稱被推入或者確認沒有推入後應被喚起的行為
        /// </summary>
        /// <param name="action">註冊地喚起行為</param>
        /// <param name="distinguishedName">應執行的目標區分名稱</param>
        /// <returns>此區分名稱是否存在設置成功</returns>
        internal bool RegisterCommitedInvoker(in Action<DirectoryEntry> action, in string distinguishedName)
        {
            // 嘗試從目前暫存的影響入口物件取得指定的目標
            if (!dictionaryDistinguishedNameWithEntry.TryGetValue(distinguishedName, out RequiredCommitSet set))
            {
                // 不存在丟出未設置, 外部自行判斷是否出錯
                return false;
            }

            // 推入事件
            set.OnCommitedFinish += action;
            // 返回資料設置完成
            return true;
        }

        /// <summary>
        /// 推入相關影響後取得入口物件
        /// </summary>
        /// <returns>所有有影響的入口物件, 結構如右: Dictionary'區分名稱, 入口物件' </returns>
        internal Dictionary<string, DirectoryEntry> Commited()
        {
            // 用來儲存總共有多少項目需要提供給外部轉換
            Dictionary<string, DirectoryEntry> dictionaryEntryByDN = new Dictionary<string, DirectoryEntry>(dictionaryDistinguishedNameWithEntry.Count);
            // 遍歷目前註冊有產生影響的物件並取得相關的入口物件
            foreach (KeyValuePair<string, RequiredCommitSet> pair in dictionaryDistinguishedNameWithEntry)
            {
                // 取得內容
                RequiredCommitSet set = pair.Value;
                // 喚醒推入動作
                if (set.InvokedCommit())
                {
                    // 推入字典黨提供給外部進行資料轉換
                    dictionaryEntryByDN.Add(pair.Key, set.Entry);
                }
            }
            // 轉換成陣列提供給外部
            return dictionaryEntryByDN;
        }

        /// <summary>
        /// 釋放所有目前使用的資源
        /// </summary>
        void IDisposable.Dispose()
        {
            // 遍歷目前項目釋放所有資源
            foreach (RequiredCommitSet set in dictionaryDistinguishedNameWithEntry.Values)
            {
                set.Entry.Dispose(); // 釋放資源
            }
            // 清除所有資料
            dictionaryDistinguishedNameWithEntry.Clear();
        }
    }
}
