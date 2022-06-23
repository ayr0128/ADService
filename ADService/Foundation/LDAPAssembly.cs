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
        /// <param name="entriesMedia">入口物件創建器</param>
        /// <param name="extendFlags">找尋 <see cref="CategoryTypes">旗標</see>, 必須指定至少一種物件類型</param>
        /// <returns>組織單位</returns>
        /// <exception cref="LDAPExceptions">非支援物件類型或特性鍵值解析發生錯誤時對外丟出</exception>
        internal static List<LDAPObject> WithChild(in DirectoryEntry entry, in LDAPEntriesMedia entriesMedia, in CategoryTypes extendFlags)
        {
            // 限制必須是任意一種類型才行
            string filiter = LDAPAttributes.GetOneOfCategoryFiliter(extendFlags);
            // 沒有指定找尋組織單位底下物件類型
            if (string.IsNullOrEmpty(filiter))
            {
                // 對外提供例外: 不可能進行搜尋動作但不找尋任何物件
                throw new LDAPExceptions("沒有指定找尋任何物件類型, 此為邏輯錯誤", ErrorCodes.LOGIC_ERROR);
            }

            // [TODO] 應使用加密字串避免注入式攻擊
            string encoderMoveToFiliter = filiter;
            //  使用 using 讓連線在跳出方法後即刻釋放: 找尋限定的組織單位
            using (DirectorySearcher searcherMixed = new DirectorySearcher(entry, encoderMoveToFiliter, LDAPAttributes.PropertiesToLoad, SearchScope.OneLevel))
            {
                // 使用 using 讓連線在跳出方法後即刻釋放: 取得隸屬於此組織單位的組織單位與成員
                using (SearchResultCollection resultMixeds = searcherMixed.FindAll())
                {
                    // 利用結果數量宣告儲存陣列的容器大小
                    List<LDAPObject> objectMixedLists = new List<LDAPObject>(resultMixeds.Count);
                    // 必定存在至少一個搜尋結果:
                    foreach (SearchResult resultMixed in resultMixeds)
                    {
                        using (DirectoryEntry searchedEntry = resultMixed.GetDirectoryEntry())
                        {
                            // 轉換為基礎物件: 不可能轉換失敗
                            LDAPObject objectSearched = ToObject(searchedEntry, entriesMedia);
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
        /// <param name="limitedType">限制類型</param>
        /// <param name="entriesMedia">入口物件創建器</param>
        /// <exception cref="LDAPExceptions">移除外部整理過屬於此組織單位的成員或組織單位後還有其他剩餘資料時丟出</exception>
        internal LDAPAssembly(in DirectoryEntry entry, in CategoryTypes limitedType, in LDAPEntriesMedia entriesMedia) : base(entry, entriesMedia)
        {
            // 不是允許類型的其中一種
            if ((limitedType & Type & CategoryTypes.ALL_CONTAINERS) == CategoryTypes.NONE)
            {
                // 對外丟出類型不正確例外
                throw new LDAPExceptions($"基礎物件類型:{Type} 不是期望的物件類型:{limitedType} 或支援的類型:{CategoryTypes.ALL_CONTAINERS}", ErrorCodes.LOGIC_ERROR);
            }
        }

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
