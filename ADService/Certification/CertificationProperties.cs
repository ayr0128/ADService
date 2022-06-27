using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Certification
{
    /// <summary>
    /// 儲存的入口物件是否需要被簽入異動
    /// </summary>
    internal sealed class RequiredCommitSet
    {
        /// <summary>
        /// 儲存的入口物件
        /// </summary>
        internal DirectoryEntry Entry { get; private set; }
        /// <summary>
        /// 儲存的入口物件
        /// </summary>
        internal ResultPropertyCollection Properties { get; private set; }

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
        /// <param name="one">額外查詢的屬性</param>
        internal RequiredCommitSet(in DirectoryEntry entry, in SearchResult one)
        {
            RequiredCommit = false; // 預設: 沒有被異動不須簽入\

            Entry = entry;
            Properties = one.Properties;
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
                // 刷新快取: 獲取之前推入的部分
                Entry.RefreshCache();
            }

            // 喚起完成行為
            OnCommitedFinish?.Invoke(Entry);
            // 返回存在簽入後事件
            return OnCommitedFinish != null | RequiredCommit;
        }
    }

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
            // [TODO] 應使用加密字串避免注入式攻擊
            string encoderFiliter = LDAPConfiguration.GetORFiliter(Properties.C_DISTINGGUISHEDNAME, distinguishedName);
            // 找尋某些額外參數
            using (DirectorySearcher searcher = new DirectorySearcher(entry, encoderFiliter, LDAPObject.PropertiesToLoad, SearchScope.Base))
            {
                // 推入入口物件
                dictionaryDistinguishedNameWitSet.Add(distinguishedName, new RequiredCommitSet(entry, searcher.FindOne()));
            }
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
        /// <param name="one">找尋獲得的物件</param>
        /// <param name="distinguishedName">指定區分名稱</param>
        internal void SetEntry(in SearchResult one, in string distinguishedName) => dictionaryDistinguishedNameWitSet.Add(distinguishedName, new RequiredCommitSet(one.GetDirectoryEntry(), one));

        /// <summary>
        /// 設定某個區分名稱有異動, 需要產生簽入行為
        /// </summary>
        /// <param name="distinguishedName">區分名稱</param>
        /// <returns>此區分名稱是否存在設置成功</returns>
        internal bool RequiredCommit(in string distinguishedName)
        {
            // 嘗試從目前暫存的影響入口物件取得指定的目標
            if (!dictionaryDistinguishedNameWitSet.TryGetValue(distinguishedName, out RequiredCommitSet set))
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
            if (!dictionaryDistinguishedNameWitSet.TryGetValue(distinguishedName, out RequiredCommitSet set))
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
        internal Dictionary<string, RequiredCommitSet> Commited()
        {
            // 用來儲存總共有多少項目需要提供給外部轉換
            Dictionary<string, RequiredCommitSet> dictionarySetByDN = new Dictionary<string, RequiredCommitSet>(dictionaryDistinguishedNameWitSet.Count);
            // 遍歷目前註冊有產生影響的物件並取得相關的入口物件
            foreach (KeyValuePair<string, RequiredCommitSet> pair in dictionaryDistinguishedNameWitSet)
            {
                // 取得內容
                RequiredCommitSet set = pair.Value;
                // 喚醒推入動作
                if (set.InvokedCommit())
                {
                    // 推入字典黨提供給外部進行資料轉換
                    dictionarySetByDN.Add(pair.Key, set);
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
