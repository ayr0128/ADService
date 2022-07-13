using ADService.Features;
using ADService.Foundation;
using ADService.Media;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace ADService.Advanced
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
        #region 安全性主體
        /// <summary>
        /// 系統自訂群組 SELF 的安全性 SID
        /// </summary>
        internal static string SID_SELF => GetSID(WellKnownSidType.SelfSid);
        /// <summary>
        /// 系統自訂群組 EVERYONE 的安全性 SID
        /// </summary>
        internal static string SID_EVERYONE => GetSID(WellKnownSidType.WorldSid);
        /// <summary>
        /// 根據提供的 SID 類型取得相關的開頭
        /// </summary>
        /// <param name="sidType">指定 SID 類型</param>
        /// <returns>相關字串</returns>
        internal static string GetSID(in WellKnownSidType sidType)
        {
            // 宣告 SecurityIdentifier 實體
            SecurityIdentifier everyone = new SecurityIdentifier(sidType, null);
            // 翻譯成對應文字
            return everyone.Translate(typeof(SecurityIdentifier)).ToString();
        }
        /// <summary>
        /// 視為安全性撙的 SID
        /// </summary>
        internal static WellKnownSidType[] SecuritySIDAdmins = new WellKnownSidType[]
        {
            WellKnownSidType.AccountAdministratorSid,    // 帳號管理群組
            WellKnownSidType.AccountDomainAdminsSid,     // 網域管理群組
            WellKnownSidType.AccountEnterpriseAdminsSid, // 企業系統管理群組
            WellKnownSidType.BuiltinAccountOperatorsSid, // 帳戶操作員
            WellKnownSidType.BuiltinAdministratorsSid,   // 管理員
        };

        /// <summary>
        /// 提供的隸屬群組中是否有安全性群組
        /// </summary>
        /// <param name="securitySIDs">隸屬群組</param>
        /// <returns>是否包含安全性主體</returns>
        internal static bool IsSecurityPrincipals(in IEnumerable<string> securitySIDs)
        {
            // 是否為安全性主體
            bool isSecurityPrincipals = false;
            // 遍歷群組 SID
            foreach (string securitySID in securitySIDs)
            {
                // 上一次檢查後確認為安全性主體
                if (isSecurityPrincipals)
                {
                    // 跳過
                    break;
                }

                // 解析成安瘸性識別字串
                SecurityIdentifier securityIdentifier = new SecurityIdentifier(securitySID);
                // 疊加彆疊加是否為安全性主體
                Array.ForEach(SecuritySIDAdmins, SecuritySIDAdmin => isSecurityPrincipals |= securityIdentifier.IsWellKnown(SecuritySIDAdmin));
            }
            // 回傳結果
            return isSecurityPrincipals;
        }

        /// <summary>
        /// 取得喚起者對於目標物見的可用群組 SID
        /// </summary>
        /// <param name="invoker">喚起者</param>
        /// <param name="destination">目標物建</param>
        /// <returns>相關群組 SID</returns>
        internal static HashSet<string> GetSceuritySIDs(in LDAPObject invoker, in LDAPObject destination)
        {
            // 支援的所有安全性群組 SID
            string[] invokerSecuritySIDs = invoker is IRevealerSecuritySIDs revealerSecuritySIDs ? revealerSecuritySIDs.Values : Array.Empty<string>();
            // 轉成 HashSet 判斷喚起者是否為自身
            HashSet<string> invokerSecuritySIDHashSet = new HashSet<string>(invokerSecuritySIDs);
            /* 根據情況決定添加何種額外 SID
                 1. 目標不持有 SID 介面: 視為所有人
                 2. 喚起者與目標非相同物件: 視為所有人
                 3. 其他情況: 是為自己
            */
            string extendedSID = destination is IRevealerSID revealerSID && invokerSecuritySIDHashSet.Contains(revealerSID.Value) ? SID_SELF : SID_EVERYONE;
            // 推入此參數
            invokerSecuritySIDHashSet.Add(extendedSID);
            // 對外提供組合結果
            return invokerSecuritySIDHashSet;
        }
        #endregion

        /// <summary>
        /// 紀錄喚起此動作的喚醒物件
        /// </summary>
        internal readonly LDAPObject Invoker;
        /// <summary>
        /// 紀錄外部提供的入口物件創建器
        /// </summary>
        internal LDAPConfigurationDispatcher Dispatcher;

        /// <summary>
        /// 初始化時須提供持有此簽證的持有者入口物件
        /// </summary>
        /// <param name="dispatcher">物件分析氣</param>
        /// <param name="invoker">呼叫者</param>
        internal CertificationProperties(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker)
        {
            Dispatcher = dispatcher;
            Invoker = invoker;
        }

        /// <summary>
        /// 創建呼叫者對於目標物建的可用存取規則
        /// </summary>
        /// <param name="destination">目標物建</param>
        /// <returns>可用權限集合</returns>
        internal LDAPPermissions CreatePermissions(in LDAPObject destination)
        {
            // 取得目標資訊
            if (!dictionaryDistinguishedNameWitSet.TryGetValue(destination.DistinguishedName, out _))
            {
                // 取得入口物件
                DirectoryEntry entry = Dispatcher.ByDistinguisedName(destination.DistinguishedName);
                // 推入入口物件
                dictionaryDistinguishedNameWitSet.Add(destination.DistinguishedName, new RequiredCommitSet(entry));
            }

            // 取得於此物件的可用 SID
            HashSet<string> sceuritySID = GetSceuritySIDs(Invoker, destination);
            // 提供指定目標的權限情況
            return new LDAPPermissions(ref Dispatcher, destination, sceuritySID);
        }


        /// <summary>
        /// 創建查看目標物件的持有存取規則
        /// </summary>
        /// <param name="destination">目標物建</param>
        /// <returns>可用權限集合</returns>
        internal LDAPAccessRules CreateAccessRules(in LDAPObject destination)
        {
            // 取得目標資訊
            if (!dictionaryDistinguishedNameWitSet.TryGetValue(destination.DistinguishedName, out _))
            {
                // 取得入口物件
                DirectoryEntry entry = Dispatcher.ByDistinguisedName(destination.DistinguishedName);
                // 存取可用入口物件的結構
                RequiredCommitSet requiredCommitSet = new RequiredCommitSet(entry);
                // 推入入口物件
                dictionaryDistinguishedNameWitSet.Add(destination.DistinguishedName, requiredCommitSet);
            }

            // 取得於此物件的可用 SID
            HashSet<string> sceuritySID = GetSceuritySIDs(Invoker, destination);
            // 只有隸屬於安全性群組時才可以取得存取規則
            return IsSecurityPrincipals(sceuritySID) ? new LDAPAccessRules(ref Dispatcher, destination) : null;
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
