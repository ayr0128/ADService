using ADService.Environments;
using System;
using System.DirectoryServices;
using System.Reflection;

namespace ADService.Revealer
{
    /// <summary>
    /// 屬性數值儲存與樂度器
    /// </summary>
    internal class RevealerSingleLongProperties : RevealerProperties
    {
        /// <summary>
        /// 建構解析特性鍵值所需資料的集合
        /// </summary>
        /// <param name="propertyName">解析的目標鍵值名稱</param>
        /// <param name="isForceExist">是否強制設定需存在資料</param>
        internal RevealerSingleLongProperties(in string propertyName, in bool isForceExist) : base(propertyName, isForceExist) { }

        internal override object Parse(in PropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            PropertyValueCollection collection = properties[PropertyName];
            // 不存在資料且強迫必須存在時
            if ((collection == null || collection.Count == 0) && IsForceExist)
            {
                // 此特性鍵值必須存在因而丟出例外
                throw new LDAPExceptions($"解析資訊:{PropertyName} 儲存的內容時, 內容長度不如預期", ErrorCodes.LOGIC_ERROR);
            }

            // 取得物件類型
            Type typeCOM = collection.Value?.GetType();
            // 物件不存在類型 (儲存的資料室 null)
            if (typeCOM == null)
            {
                // 對外提供 long 的預設值
                return default(long);
            }

            //  取得高位元
            int high = (int)typeCOM.InvokeMember("HighPart", BindingFlags.GetProperty, null, collection.Value, null);
            //  取得低位元
            int low  = (int)typeCOM.InvokeMember("LowPart", BindingFlags.GetProperty, null, collection.Value, null);
            // 高低位元互換
            return (long)high << 32 | (uint)low;
        }
    }
}
