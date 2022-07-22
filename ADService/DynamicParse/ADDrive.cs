using ADService.Basis;
using System.Collections.Generic;

namespace ADService.DynamicParse
{
    /// <summary>
    /// 所有要使用動態家仔的物件都應繼承此抽象類別
    /// </summary>
    public abstract class ADDrive
    {
        #region 搜尋字串組合
        /// <summary>
        /// 組成找尋指定區分名稱的過濾字串, 要求至少提供一筆
        /// </summary>
        /// <param name="propertyName">目標欄位</param>
        /// <param name="value">限制的內容</param>
        /// <param name="values">限制的內容</param>
        /// <returns>過濾用字串, 沒有任何區分名稱需指定會提供空字串</returns>
        internal static string CombineORFiliter(in string propertyName, in string value, params string[] values)
        {
            // 先將不定長度的項目提供給 HashSet
            HashSet<string> unduplicateValues = new HashSet<string>(values) { value };
            // 移除空字串
            unduplicateValues.RemoveWhere(insideValue => string.IsNullOrWhiteSpace(insideValue));

            // 如果沒有任何區分名稱則不必進行過濾字串組合
            if (string.IsNullOrWhiteSpace(propertyName) || unduplicateValues.Count == 0)
            {
                // 返回空字串讓外部跳過搜尋動作
                return string.Empty;
            }
            // 透過內部轉換成過濾用字串
            return ComibieFiliter(propertyName, "|", unduplicateValues);
        }

        /// <summary>
        /// 組成找尋指定區分名稱的過濾字串
        /// </summary>
        /// <param name="propertyName">目標欄位</param>
        /// <param name="values">限制的內容</param>
        /// <returns>過濾用字串, 沒有任何區分名稱需指定會提供空字串</returns>
        internal static string CombineORFiliter(in string propertyName, in IEnumerable<string> values)
        {
            // 先將不定長度的項目提供給 HashSet
            HashSet<string> unduplicateValues = new HashSet<string>(values);
            // 移除空字串
            unduplicateValues.RemoveWhere(insideValue => string.IsNullOrWhiteSpace(insideValue));

            // 如果沒有任何區分名稱則不必進行過濾字串組合
            if (string.IsNullOrWhiteSpace(propertyName) || unduplicateValues.Count == 0)
            {
                // 返回空字串讓外部跳過搜尋動作
                return string.Empty;
            }
            // 透過內部轉換成過濾用字串
            return ComibieFiliter(propertyName, "|", unduplicateValues);
        }

        /// <summary>
        /// 組成找尋指定區分名稱的過濾字串, 要求至少提供一筆
        /// </summary>
        /// <param name="propertyName">目標欄位</param>
        /// <param name="value">限制的內容</param>
        /// <param name="values">限制的內容</param>
        /// <returns>過濾用字串, 沒有任何區分名稱需指定會提供空字串</returns>
        internal static string CombineANDFiliter(in string propertyName, in string value, params string[] values)
        {
            // 先將不定長度的項目提供給 HashSet
            HashSet<string> unduplicateValues = new HashSet<string>(values) { value };
            // 移除空字串
            unduplicateValues.RemoveWhere(insideValue => string.IsNullOrWhiteSpace(insideValue));

            // 如果沒有任何區分名稱則不必進行過濾字串組合
            if (string.IsNullOrWhiteSpace(propertyName) || unduplicateValues.Count == 0)
            {
                // 返回空字串讓外部跳過搜尋動作
                return string.Empty;
            }
            // 透過內部轉換成過濾用字串
            return ComibieFiliter(propertyName, "&", unduplicateValues);
        }

        /// <summary>
        /// 組成找尋指定區分名稱的過濾字串
        /// </summary>
        /// <param name="propertyName">目標欄位</param>
        /// <param name="values">限制的內容</param>
        /// <returns>過濾用字串, 沒有任何區分名稱需指定會提供空字串</returns>
        internal static string CombineANDFiliter(in string propertyName, in IEnumerable<string> values)
        {
            // 先將不定長度的項目提供給 HashSet
            HashSet<string> unduplicateValues = new HashSet<string>(values);
            // 移除空字串
            unduplicateValues.RemoveWhere(insideValue => string.IsNullOrWhiteSpace(insideValue));

            // 如果沒有任何區分名稱則不必進行過濾字串組合
            if (string.IsNullOrWhiteSpace(propertyName) || unduplicateValues.Count == 0)
            {
                // 返回空字串讓外部跳過搜尋動作
                return string.Empty;
            }
            // 透過內部轉換成過濾用字串
            return ComibieFiliter(propertyName, "&", unduplicateValues);
        }

        /// <summary>
        /// 組成找尋指定區分名稱的過濾字串, 要求至少提供一筆
        /// </summary>
        /// <param name="propertyName">目標欄位</param>
        /// <param name="value">限制的內容</param>
        /// <param name="values">限制的內容</param>
        /// <returns>過濾用字串, 沒有任何區分名稱需指定會提供空字串</returns>
        internal static string CombineFiliter(in string propertyName, in string value, params string[] values)
        {
            // 先將不定長度的項目提供給 HashSet
            HashSet<string> unduplicateValues = new HashSet<string>(values) { value };
            // 移除空字串
            unduplicateValues.RemoveWhere(insideValue => string.IsNullOrWhiteSpace(insideValue));

            // 如果沒有任何區分名稱則不必進行過濾字串組合
            if (string.IsNullOrWhiteSpace(propertyName) || unduplicateValues.Count == 0)
            {
                // 返回空字串讓外部跳過搜尋動作
                return string.Empty;
            }
            // 透過內部轉換成過濾用字串
            return ComibieFiliter(propertyName, string.Empty, unduplicateValues);
        }

        /// <summary>
        /// 組成找尋指定區分名稱的過濾字串
        /// </summary>
        /// <param name="propertyName">目標欄位</param>
        /// <param name="values">限制的內容</param>
        /// <returns>過濾用字串, 沒有任何區分名稱需指定會提供空字串</returns>
        internal static string CombineFiliter(in string propertyName, in IEnumerable<string> values)
        {
            // 先將不定長度的項目提供給 HashSet
            HashSet<string> unduplicateValues = new HashSet<string>(values);
            // 移除空字串
            unduplicateValues.RemoveWhere(insideValue => string.IsNullOrWhiteSpace(insideValue));

            // 如果沒有任何區分名稱則不必進行過濾字串組合
            if (string.IsNullOrWhiteSpace(propertyName) || unduplicateValues.Count == 0)
            {
                // 返回空字串讓外部跳過搜尋動作
                return string.Empty;
            }
            // 透過內部轉換成過濾用字串
            return ComibieFiliter(propertyName, string.Empty, unduplicateValues);
        }

        /// <summary>
        /// 內部使用: 組成找尋指定區分名稱的過濾字串
        /// </summary>
        /// <param name="propertyName">目標欄位</param>
        /// <param name="headFlag">標頭</param>
        /// <param name="values">限制的內容</param>
        /// <returns>過濾用字串, 沒有任何區分名稱需指定會提供空字串</returns>
        private static string ComibieFiliter(in string propertyName, in string headFlag, in IEnumerable<string> values)
        {
            // 區分名稱的過濾內容: 此時會缺失開頭與結尾的部分
            string subFiliter = string.Join($")({propertyName}=", values);
            // 基本將對外回傳的資料
            string baseFiliter = $"({propertyName}={subFiliter})";
            // 組成找尋任意一個與指定區分名稱相符的過濾字串
            if (string.IsNullOrWhiteSpace(headFlag))
            {
                // 不存在標頭時對外提供基礎過濾字串
                return baseFiliter;
            }

            // 存在標頭時需額外組成
            return $"({headFlag}{baseFiliter})";
        }
        #endregion

        /// <summary>
        /// 基底物件
        /// </summary>
        public ADCustomUnit CustomUnit { get; private set; }

        /// <summary>
        /// 系統喚醒時會自動加戴
        /// </summary>
        /// <param name="customUnit">物件蘭園</param>
        public ADDrive(in ADCustomUnit customUnit) => CustomUnit = customUnit;
    }
}
