using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Certificate
{
    /// <summary>
    /// 儲存的入口物件是否需要被簽入異動
    /// </summary>
    internal sealed class RequiredCommitSet
    {
        /// <summary>
        /// 儲存的入口物件是否需要被簽入異動
        /// </summary>
        private bool RequiredCommit;

        /// <summary>
        /// 儲存的入口物件
        /// </summary>
        internal DirectoryEntry Entry { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        internal HashSet<string> ReflashNames = new HashSet<string>();

        /// <summary>
        /// 推入入口物件並預設為不須簽入
        /// </summary>
        /// <param name="entry">入口物件</param>
        internal RequiredCommitSet(in DirectoryEntry entry)
        {
            RequiredCommit = false;  // 預設: 沒有被異動不須簽入
            Entry = entry;
        }

        /// <summary>
        /// 設定有發生異動, 須執行推入行為
        /// </summary>
        internal void CommitRequired() => RequiredCommit = true;
        /// <summary>
        /// 事件需要由方法內部喚起
        /// </summary>
        /// <returns>室友有產生異動</returns>
        internal bool InvokedCommit()
        {
            // 曾經異動
            if (RequiredCommit)
            {
                // 推入異動
                Entry.CommitChanges();
            }

            // 返回存在異動
            return RequiredCommit;
        }

        /// <summary>
        /// 設定有發生異動或被影響, 須執行刷新行為
        /// </summary>
        /// <param name="name">需要被調整的項目</param>
        internal void SetReflashName(in string name) => ReflashNames.Add(name);
        /// <summary>
        /// 刷新參數取得影響
        /// </summary>
        /// <returns>是否受到影響</returns>
        internal void InvokedReflash()
        {
            // 需要刷新 (有異動也需要刷新)
            if (ReflashNames.Count != 0)
            {
                // 宣告陣列
                string[] redlashNames = new string[ReflashNames.Count];
                // 複製至儲存陣列
                ReflashNames.CopyTo(redlashNames, 0);
                // 刷新
                Entry.RefreshCache(redlashNames);
            }
        }
    }
}
