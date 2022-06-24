using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Runtime.InteropServices;

namespace ADService.Foundation
{
    /// <summary>
    /// 登入的 ADService 使用者
    /// </summary>
    public sealed class LDAPLogonPerson : LDAPPerson
    {
        /// <summary>
        /// 提供登入者結構
        /// </summary>
        /// <param name="entriesMedia">入口物件製作器</param>
        /// <param name="userName">登入者名稱</param>
        /// <param name="password">登入者密碼</param>
        /// <returns></returns>
        /// <exception cref="DirectoryServicesCOMException">帳號密碼不正確等帳號相關情況時</exception>
        /// <exception cref="COMException">無法連線至伺服器時</exception>
        internal static LDAPLogonPerson Authentication(in LDAPEntriesMedia entriesMedia, in string userName, in string password)
        {
            // 找到須限制的物件類型
            Dictionary<CategoryTypes, string> dictionaryLimitedCategory = LDAPCategory.GetValuesByTypes(CategoryTypes.PERSON);
            // 使用 using 讓連線在跳出方法後即刻釋放: 此處使用的權限是登入者的帳號權限
            using ( DirectoryEntry root = entriesMedia.DomainRoot() )
            {
                // 直行至此已經確認使用者可以
                // 加密避免 LDAP 注入式攻擊
                string encoderFiliter = $"(&{LDAPEntries.GetORFiliter(Properties.C_OBJECTCATEGORY, dictionaryLimitedCategory.Values)}(|(sAMAccountName={userName})(userPrincipalName={userName})))";
                /* 備註: 為何要額外搜尋一次?
                     1. 連線時如果未在伺服器後提供區分名稱, 會使用物件類型 domainDNS 來回傳
                     2. 為避免部分資料缺失, 需額外指定
                */
                using ( DirectorySearcher searcher = new DirectorySearcher(root, encoderFiliter, PropertiesToLoad) )
                {
                    // 必定存在至少一個搜尋結果:
                    SearchResult one = searcher.FindOne();
                    // 不存在搜尋結果
                    if (one == null)
                    {
                        // 對外丟出例外: 邏輯錯誤, 這種錯誤除非多網域否則不應發生
                        throw new LDAPExceptions($"登入使用者:{userName} 時因無法使用者的實體物件而失敗丟出例外", ErrorCodes.LOGIC_ERROR);
                    }

                    using (DirectoryEntry entry = one.GetDirectoryEntry())
                    {
                        // 對外提供登入者結構: 建構時若無法找到必須存在的鍵值會丟出例外
                        return new LDAPLogonPerson(userName, password, entry, entriesMedia, one.Properties);
                    }
                }
            }
        }

        /// <summary>
        /// 帳號
        /// </summary>
        internal readonly string UserName;
        /// <summary>
        /// 密碼
        /// </summary>
        internal readonly string Password;

        /// <summary>
        /// 透過建構子解析內容資料
        /// </summary>
        /// <param name="userName">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="entry">入口物件</param>
        /// <param name="entriesMedia">入口物件創建器</param>
        /// <param name="propertiesResult">透過找尋取得字的屬性</param>
        /// <exception cref="LDAPExceptions">解析鍵值不符合規則時對外丟出</exception>
        internal LDAPLogonPerson(in string userName, in string password, in DirectoryEntry entry, in LDAPEntriesMedia entriesMedia, in ResultPropertyCollection propertiesResult) : base(entry, entriesMedia, propertiesResult)
        {
            // 記錄帳號
            UserName = userName;
            // 紀錄密碼
            Password = password;
        }
    }
}