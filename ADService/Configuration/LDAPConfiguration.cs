using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace ADService.Configuration
{
    /// <summary>
    /// 內部使用的存取權限轉換器
    /// </summary>
    internal sealed class LDAPConfiguration
    {
        /// <summary>
        /// 設定入口物件位置
        /// </summary>
        private const string CONTEXT_CONFIGURATION = "configurationNamingContext";
        /// <summary>
        /// 設定入口物件位置
        /// </summary>
        private const string CONTEXT_SCHEMA = "schemaNamingContext";

        /// <summary>
        /// 紀錄外部提供的入口物件創建器
        /// </summary>
        internal readonly LDAPEntriesMedia EntriesMedia;

        /// <summary>
        /// 設定區分名稱
        /// </summary>
        private readonly string ConfigurationDistinguishedName;
        /// <summary>
        /// 藍本區分名稱
        /// </summary>
        private readonly string SchemaDistinguishedName;

        /// <summary>
        /// 取得 DSE 中的設定區分名稱位置, 並建構連線用相關暫存
        /// </summary>
        /// <param name="entriesMedia">入口物件製作器</param>
        internal LDAPConfiguration(in LDAPEntriesMedia entriesMedia)
        {
            EntriesMedia = entriesMedia;

            // 取得設定位置
            using (DirectoryEntry root = entriesMedia.DSERoot())
            {
                // 取得內部設定位置
                ConfigurationDistinguishedName = LDAPEntries.ParseSingleValue<string>(CONTEXT_CONFIGURATION, root.Properties);
                // 取得內部難本位置
                SchemaDistinguishedName = LDAPEntries.ParseSingleValue<string>(CONTEXT_SCHEMA, root.Properties);
            }
        }

        /// <summary>
        /// 是否為空 GUID
        /// </summary>
        /// <param name="value">檢查 GUID </param>
        /// <returns>是否為空</returns>
        internal static bool IsGUIDEmpty(in Guid value) => value.Equals(Guid.Empty);
        /// <summary>
        /// 是否為空 GUID
        /// </summary>
        /// <param name="value">檢查 GUID </param>
        /// <returns>是否為空</returns>
        internal static bool IsGUIDEmpty(in string value) => !Guid.TryParse(value, out Guid convertedValue) || convertedValue.Equals(Guid.Empty);

        #region 取得藍本物件
        /// <summary>
        /// 藍本物件陣列
        /// </summary>
        private readonly List<UnitSchema> UnitSchemas = new List<UnitSchema>();

        /// <summary>
        /// 使用 GUID 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="value">目標 GUID </param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchema GetSchema(in Guid value)
        {
            // 轉換成根據查詢結構: 若此處出現錯誤則必定是羅錯誤導致加入重複物件
            Dictionary<string, UnitSchema> dictionaryGUIDWithUnitSchema = UnitSchemas.ToDictionary(schema => schema.SchemaGUID);
            // 對外提供的結果
            UnitSchema result = null;
            // 由於儲存結構中醫慮採用小寫, 所以搜尋參數於搜尋前須改為小寫
            string valueGUID = value.ToString("D").ToLower();
            // 找尋目標藍本結構
            if (!Guid.Empty.Equals(value) && !dictionaryGUIDWithUnitSchema.TryGetValue(valueGUID, out result))
            {
                // 重新建立藍本結構
                result = UnitSchema.Get(EntriesMedia, value, ConfigurationDistinguishedName);
                // 只有在非 null 時
                if (result != null)
                {
                    // 加入查詢陣列
                    UnitSchemas.Add(result);
                }
            }
            // 對外提供取得的資料: 注意可能為空
            return result;
        }

        /// <summary>
        /// 使用展示名稱 進行搜尋指定目標藍本物件
        /// </summary>
        /// <param name="value">展示名稱</param>
        /// <returns>指定藍本物件, 可能不存在</returns>
        internal UnitSchema GetSchema(in string value)
        {
            // 轉換成根據查詢結構: 若此處出現錯誤則必定是羅錯誤導致加入重複物件
            Dictionary<string, UnitSchema> dictionaryDisplayNameWithUnitSchema = UnitSchemas.ToDictionary(schema => schema.Name);
            // 對外提供的結果
            UnitSchema result = null;
            // 由於儲存結構中醫慮採用小寫, 所以搜尋參數於搜尋前須改為小寫
            string valueLower= value.ToLower();
            // 找尋目標藍本結構
            if (!string.IsNullOrWhiteSpace(value) && !dictionaryDisplayNameWithUnitSchema.TryGetValue(value, out result))
            {
                // 重新建立藍本結構
                result = UnitSchema.Get(EntriesMedia, value, ConfigurationDistinguishedName);
                // 只有在非 null 時
                if (result != null)
                {
                    // 加入查詢陣列
                    UnitSchemas.Add(result);
                }
            }
            // 對外提供取得的資料: 注意可能為空
            return result;
        }
        #endregion

        #region 取得額外權限
        /// <summary>
        /// 額外權限陣列
        /// </summary>
        private readonly List<UnitExtendedRight> UnitExtendedRights = new List<UnitExtendedRight>();

        /// <summary>
        /// 使用 GUID 進行搜尋指定目標額外權限物件
        /// </summary>
        /// <param name="value">目標 GUID </param>
        /// <returns>指定額外權限物件, 可能不存在</returns>
        internal UnitExtendedRight GetExtendedRight(in Guid value)
        {
            // 轉換成根據查詢結構: 若此處出現錯誤則必定是羅錯誤導致加入重複物件
            Dictionary<string, UnitExtendedRight> dictionaryGUIDWithUnitExtendedRight = UnitExtendedRights.ToDictionary(extendedRight => extendedRight.RightsGUID);
            // 對外提供的結果
            UnitExtendedRight result = null;
            // 由於儲存結構中醫慮採用小寫, 所以搜尋參數於搜尋前須改為小寫
            string valueGUID = value.ToString("D").ToLower();
            // 找尋目標額外權限
            if (!Guid.Empty.Equals(value) && !dictionaryGUIDWithUnitExtendedRight.TryGetValue(valueGUID, out result))
            {
                // 重新建立藍本結構
                result = UnitExtendedRight.Get(EntriesMedia, value, ConfigurationDistinguishedName);
                // 只有在非 null 時
                if (result != null)
                {
                    // 加入查詢陣列
                    UnitExtendedRights.Add(result);
                }
            }
            // 對外提供取得的資料: 注意可能為空
            return result;
        }
        #endregion

        #region 通用
        /// <summary>
        /// 使用 GUID 找到展示名稱
        /// </summary>
        /// <param name="value">目標 GUID</param>
        /// <returns></returns>
        internal string FindName(in Guid value)
        {
            // 提供的 GUID 為空
            if (IsGUIDEmpty(value))
            {
                // 提供空字串
                return string.Empty;
            }

            // 轉換成根據查詢結構: 若此處出現錯誤則必定是羅錯誤導致加入重複物件
            UnitExtendedRight unitExtendedRight = GetExtendedRight(value);
            // 從額外權限中找到目標資料
            if (unitExtendedRight != null)
            {
                // 設置對外提供的名稱: 額外權權的展示名稱
                return unitExtendedRight.Name;
            }

            // 轉換成根據查詢結構: 若此處出現錯誤則必定是羅錯誤導致加入重複物件
            UnitSchema unitSchema = GetSchema(value);
            // 從藍本中找到目標資料
            if (unitSchema != null)
            {
                // 設置對外提供的名稱: 額外權權的展示名稱
                return unitSchema.Name;
            }

            // 應找尋得到額外權限: 若取得此例外則必定是 AD 設置有漏洞
            throw new LDAPExceptions($"目標 GUID:{value} 無法於額外權限中找得展示名稱因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
        }
        #endregion
    }
}
