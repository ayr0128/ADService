using ADService.Media;
using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Certification
{
    #region 記錄用類別
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
        /// 儲存的入口物件是否需要被簽入異動
        /// </summary>
        private bool RequiredReflash;
        /// <summary>
        /// 儲存的入口物件
        /// </summary>
        internal DirectoryEntry Entry { get; private set; }

        /// <summary>
        /// 推入入口物件並預設為不須簽入
        /// </summary>
        /// <param name="entry">入口物件</param>
        internal RequiredCommitSet(in DirectoryEntry entry)
        {
            RequiredCommit = false;  // 預設: 沒有被異動不須簽入
            RequiredReflash = false; // 預設: 沒有異動不須刷新

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
        internal void ReflashRequired() => RequiredReflash = true;
        /// <summary>
        /// 刷新參數取得影響
        /// </summary>
        /// <returns>是否受到影響</returns>
        internal bool InvokedReflash()
        {
            // 需要刷新 (有異動也需要刷新)
            if (RequiredCommit | RequiredReflash)
            {
                // 刷新
                Entry.RefreshCache();
            }

            // 返回是否刷新 (有異動也需要刷新)
            return RequiredCommit | RequiredReflash;
        }
    }
    #endregion

    /// <summary>
    /// 傳遞修改內容證書
    /// </summary>
    internal sealed class CertificationProperties : IDisposable
    {
        /// <summary>
        /// 紀錄外部提供的入口物件創建器
        /// </summary>
        internal readonly LDAPConfigurationDispatcher Dispatcher;

        /// <summary>
        /// 初始化時須提供持有此簽證的持有者入口物件
        /// </summary>
        /// <param name="dispatcher">物件分析氣</param>
        /// <param name="distinguishedName">持有者區分名稱</param>
        internal CertificationProperties(in LDAPConfigurationDispatcher dispatcher, in string distinguishedName)
        {
            Dispatcher = dispatcher;

            // 取得入口物件
            DirectoryEntry entry = Dispatcher.ByDistinguisedName(distinguishedName);
            // 推入入口物件
            dictionaryDistinguishedNameWitSet.Add(distinguishedName, new RequiredCommitSet(entry));
        }

        /// <summary>
        /// 紀錄發生影響的相關入口物件
        /// </summary>
        private readonly Dictionary<string, RequiredCommitSet> dictionaryDistinguishedNameWitSet = new Dictionary<string, RequiredCommitSet>();

        /// <summary>
        /// 取得目前儲存的指定區分名稱入口物件
        /// </summary>
        /// <param name="distinguishedName">指定區分名稱</param>
        /// <returns>指定區分名稱的入口物件</returns>
        internal RequiredCommitSet GetEntry(in string distinguishedName)
        {
            // 嘗試從目前暫存的影響入口物件取得指定的目標
            if (!dictionaryDistinguishedNameWitSet.TryGetValue(distinguishedName, out RequiredCommitSet set))
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
        /// <param name="entry">入口物件</param>
        /// <param name="distinguishedName">指定區分名稱</param>
        internal RequiredCommitSet SetEntry(in DirectoryEntry entry, in string distinguishedName)
        {
            // 創建站存結構
            RequiredCommitSet requiredCommitSet = new RequiredCommitSet(entry);
            // 推入字典
            dictionaryDistinguishedNameWitSet.Add(distinguishedName, requiredCommitSet);
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
            Dictionary<string, DirectoryEntry> dictionarySetByDN = new Dictionary<string, DirectoryEntry>(dictionaryDistinguishedNameWitSet.Count);
            // 遍歷目前註冊有產生影響的物件並取得相關的入口物件
            foreach (KeyValuePair<string, RequiredCommitSet> pair in dictionaryDistinguishedNameWitSet)
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
            foreach (KeyValuePair<string, RequiredCommitSet> pair in dictionaryDistinguishedNameWitSet)
            {
                // 取得內容
                RequiredCommitSet set = pair.Value;
                // 喚醒刷新動作, 之前尚未因為異動而堆入推外提供項目
                if (set.InvokedReflash() && !dictionarySetByDN.ContainsKey(pair.Key))
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
            foreach (RequiredCommitSet set in dictionaryDistinguishedNameWitSet.Values)
            {
                set.Entry.Dispose(); // 釋放資源
            }
            // 清除所有資料
            dictionaryDistinguishedNameWitSet.Clear();
        }
    }
}
