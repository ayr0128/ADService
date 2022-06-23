using ADService.Environments;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace ADService.Revealer
{
    /// <summary>
    /// 使用 JSON 做為基底的特性鍵值轉換器
    /// </summary>
    internal sealed class LDAPPropertiesRevealer
    {
        #region 目前註冊支援解析查看的屬性鍵值
        /// <summary>
        /// 特性鍵值解析器
        /// </summary>
        private static readonly List<RevealerProperties> Revealers = new List<RevealerProperties>()
        {
            new RevealerGUIDProperties(LDAPAttributes.C_OBJECTGUID, true),                              // GUID: 一旦宣布持有, 則必須存在
            new RevealerSIDProperties(LDAPAttributes.C_OBJECTSID, true),                                // SID: 一旦宣布持有, 則必須存在
            new RevealerEnumProperties<AccountControlFlags>(LDAPAttributes.P_USERACCOUNTCONTROL, true), // 使用者帳號旗標: 一旦宣布持有, 則必須存在

            new RevealerSingleProperties<string>(LDAPAttributes.C_OBJECTCATEGORY, true),     // 物件類型: 一旦宣布持有, 則必須存在
            new RevealerSingleProperties<string>(LDAPAttributes.C_DISTINGGUISHEDNAME, true), // 區分名稱: 一旦宣布持有, 則必須存在
            new RevealerSingleProperties<string>(LDAPAttributes.P_NAME, true),               // 名稱: 一旦宣布持有, 則必須存在
            new RevealerSingleProperties<int>(LDAPAttributes.C_PRIMARYGROUPID, true),        // 主要隸屬群組: 一旦宣布持有, 則必須存在

            new RevealerSingleProperties<string>(LDAPAttributes.P_DISPLAYNAME, false), // 展示名稱: 即使宣布持有, 還是可以設置為空
            new RevealerSingleProperties<string>(LDAPAttributes.P_DESCRIPTION, false), // 描述: 即使宣布持有, 還是可以設置為空
            new RevealerSingleProperties<string>(LDAPAttributes.P_SN, false),          // 姓: 即使宣布持有, 還是可以設置為空
            new RevealerSingleProperties<string>(LDAPAttributes.P_GIVENNAME, false),   // 名: 即使宣布持有, 還是可以設置為空
            new RevealerSingleProperties<string>(LDAPAttributes.P_INITIALS, false),    // 縮寫: 即使宣布持有, 還是可以設置為空
            new RevealerMultipleProperties<string>(LDAPAttributes.P_MEMBER, false),    // 成員: 即使宣布持有, 還是可以設置為空
            new RevealerMultipleProperties<string>(LDAPAttributes.P_MEMBEROF, false),  // 隸屬群組: 即使宣布持有, 還是可以設置為空
            
            new RevealerSingleLongProperties(LDAPAttributes.P_PWDLASTSET, false),                        // 密碼最後設置: 即使宣布持有, 還是可以設置為 0
            new RevealerSingleLongProperties(LDAPAttributes.P_LOCKOUTTIME, false),                       // 帳號鎖定時間: 即使宣布持有, 還是可以設置為 0
            new RevealerSingleLongProperties(LDAPAttributes.P_ACCOUNTEXPIRES, false),                    // 密碼過期時間: 即使宣布持有, 還是可以設置為 0
            new RevealerEnumProperties<EncryptedType>(LDAPAttributes.P_SUPPORTEDENCRYPTIONTYPES, false), // 加密方式旗標: 即使宣布持有, 還是可以設置為 0
        };

        /// <summary>
        /// 快捷找尋全域設定中支援的特性鍵值轉換器
        /// </summary>
        /// <param name="propertyName">特性鍵值名稱</param>
        /// <returns>轉換器</returns>
        private static RevealerProperties FindRevealer(in string propertyName)
        {
            // 轉換成以屬性鍵值為主的字典
            Dictionary<string, RevealerProperties> dictionaryPropertyWuthRevealer = Revealers.ToDictionary(pair => pair.PropertyName);
            // 取得查看內容
            return dictionaryPropertyWuthRevealer.TryGetValue(propertyName, out RevealerProperties revealer) ? revealer : null;
        }
        #endregion

        /// <summary>
        /// 目前儲存的特性鍵值資料
        /// </summary>
        internal readonly Dictionary<string, object> DictionaryPropertyWithObject = new Dictionary<string, object>();

        /// <summary>
        /// 建構特性儲存與分析類別
        /// </summary>
        internal LDAPPropertiesRevealer() {}

        /// <summary>
        /// 建構特性儲存與分析類別
        /// </summary>
        /// <param name="properties">入口物件取得的特性參數</param>
        /// <param name="propertyNames">應支援的特性鍵值</param>
        /// <exception cref="LDAPExceptions">無法轉換時對外提供</exception>
        internal void SetPropertiesSupported(in PropertyCollection properties, params string[] propertyNames)
        {
            // 遍歷應支援項目
            foreach (string propertyName in propertyNames)
            {
                // 使用特性鍵值找尋轉換器
                RevealerProperties revealer = FindRevealer(propertyName);
                // 轉換器不得為空, 前端提供的數值應被約束
                if (revealer == null)
                {
                    // 對外目標特性鍵值不存在的錯誤: 此為邏輯錯誤, 因為前端提供的特性鍵值必定且應該在預期當中
                    throw new LDAPExceptions($"找尋特性鍵值:{propertyName} 的轉換器以設定支援, 但此特性鍵值不存在的例外邏輯錯誤", ErrorCodes.LOGIC_ERROR);
                }

                // 獲得的結果
                object value = revealer.Parse(properties);
                // 檢查鍵值是否重複
                if (DictionaryPropertyWithObject.ContainsKey(propertyName))
                {
                    // 重複時直接覆蓋舊有鍵值持有資料: 這樣多次設置時只會已最後設置者為主
                    DictionaryPropertyWithObject[propertyName] = value;
                }
                else
                {
                    // 不重複時直接增加鍵值對應資料
                    DictionaryPropertyWithObject.Add(propertyName, value);
                }
            }
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <param name="propertyName">特性參數</param>
        /// <param name="convertedValue">實際資料</param>
        /// <returns> 資料是否存在 </returns>
        internal bool GetPropertyValue<T>(in string propertyName, out T convertedValue)
        {
            // 嘗試取得內容
            bool isExist = DictionaryPropertyWithObject.TryGetValue(propertyName, out object value);
            // 提供對應資料
            convertedValue = value == null ? default(T) : (T)value;
            // 提供查詢結果
            return isExist;
        }
    }
}
