using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Foundation
{
    /// <summary>
    /// 封裝類型
    /// </summary>
    public abstract class LDAPAssembly : LDAPObject
    {
        /// <summary>
        /// 找尋目標入口下所有的組織單位與成員和群組並組成組織單位對外回傳
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="dispatcher">入口物件創建器</param>
        /// <param name="classNames">意圖查詢的資料妹別</param>
        /// <returns>組織單位</returns>
        internal static List<LDAPObject> WithChild(in DirectoryEntry entry, in LDAPConfigurationDispatcher dispatcher, in IEnumerable<string> classNames)
        {
            // [TODO] 應使用加密字串避免注入式攻擊
            string encoderFiliter = LDAPConfiguration.GetORFiliter(Properties.C_OBJECTCLASS, classNames);
            //  使用 using 讓連線在跳出方法後即刻釋放: 找尋限定的組織單位
            using (DirectorySearcher searcherMixed = new DirectorySearcher(entry, encoderFiliter, PropertiesToLoad, SearchScope.OneLevel))
            {
                // 使用 using 讓連線在跳出方法後即刻釋放: 取得隸屬於此組織單位的組織單位與成員
                using (SearchResultCollection all = searcherMixed.FindAll())
                {
                    // 利用結果數量宣告儲存陣列的容器大小
                    List<LDAPObject> objectMixedLists = new List<LDAPObject>(all.Count);
                    // 必定存在至少一個搜尋結果:
                    foreach (SearchResult one in all)
                    {
                        using (DirectoryEntry entryOne = one.GetDirectoryEntry())
                        {
                            // 轉換為基礎物件: 不可能轉換失敗
                            LDAPObject objectSearched = ToObject(entryOne, dispatcher);
                            // 推入轉換完成的物件
                            objectMixedLists.Add(objectSearched);
                        }
                    }
                    // 對外提供此組織單位的結構
                    return objectMixedLists;
                }
            }
        }

        /// <summary>
        /// 系統中僅存在一個根網域時統一使用的鍵值位置
        /// </summary>
        public const string ROOT = "Domains";

        /// <summary>
        /// 隸屬於此組織單位的基礎物件, 可能為空 (null)
        /// </summary>
        public LDAPObject[] Children => storedMixedList?.ToArray();
        /// <summary>
        /// 隸屬於此組織單位的各類物件, 不直接對外提供避免被外部移除或調整
        /// </summary>
        private List<LDAPObject> storedMixedList = null;

        /// <summary>
        /// 建構組織單位物件
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="dispatcher">入口物件創建器</param>
        /// <exception cref="LDAPExceptions">移除外部整理過屬於此組織單位的成員或組織單位後還有其他剩餘資料時丟出</exception>
        internal LDAPAssembly(in DirectoryEntry entry, in LDAPConfigurationDispatcher dispatcher) : base(entry, dispatcher) { }

        /// <summary>
        /// 以傳入的陣列刷新此組織單位持有的組織單位或成員或群組
        /// </summary>
        /// <param name="objectMixedList">新的組織單位或成員或群組的混和陣列</param>
        internal void Reflash(in List<LDAPObject> objectMixedList)
        {
            // 區分類型儲存物件
            storedMixedList = objectMixedList;
        }
    }
}
