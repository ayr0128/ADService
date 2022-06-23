using ADService.Environments;
using System;
using System.DirectoryServices;

namespace ADService.Revealer
{
    /// <summary>
    /// 屬性數值儲存與樂度器
    /// </summary>
    internal class RevealerEnumProperties<T> : RevealerProperties where T : Enum
    {
        /// <summary>
        /// 建構解析特性鍵值所需資料的集合
        /// </summary>
        /// <param name="propertyName">解析的目標鍵值名稱</param>
        /// <param name="isForceExist">是否強制設定需存在資料</param>
        internal RevealerEnumProperties(in string propertyName, in bool isForceExist) : base(propertyName, isForceExist) { }

        internal override object Parse(in PropertyCollection properties)
        {
            // 儲存的整數
            int storedValue = LDAPAttributes.ParseSingleValue<int>(PropertyName, IsForceExist, properties);
            // 轉換後的資料
            T convertedValue = (T)Enum.ToObject(typeof(T), storedValue);
            // 提供轉換後的資料
            return convertedValue;
        }
    }
}
