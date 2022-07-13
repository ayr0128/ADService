using System;

namespace ADService.Protocol
{
    /// <summary>
    /// 儲存資料描述
    /// </summary>
    public struct ValueDescription
    {
        /// <summary>
        /// 儲存的資料是否為陣列
        /// </summary>
        public bool IsArray;
        /// <summary>
        /// 儲存的資料長度, 儲存資料是陣列時才有意義
        /// </summary>
        public int Count;
        /// <summary>
        /// 儲存資料的類型
        /// </summary>
        public string ValueType;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="valueType">內容型別描述</param>
        /// <param name="count">陣列長度, 儲存資料是陣列時才有意義</param>
        /// <param name="isArray">是否為陣列, 預設為否</param>
        /// <returns></returns>
        public ValueDescription(in string valueType,in int count = 1, in bool isArray = false)
        {
            ValueType = valueType;
            Count = count;
            IsArray = isArray;
        }
    }
}
