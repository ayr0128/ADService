using System;

namespace ADService.Protocol
{
    /// <summary>
    /// 接受屬性描述
    /// </summary>
    public struct PropertyDescription
    {
        /// <summary>
        /// 物件名稱
        /// </summary>
        public string Name;
        /// <summary>
        /// 儲存資料的類型
        /// </summary>
        public string ValueType;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="name">內容型別描述</param>
        /// <param name="valueType">陣列長度, 儲存資料是陣列時才有意義</param>
        /// <returns></returns>
        public PropertyDescription(in string name, in string valueType)
        {
            Name = name;
            ValueType = valueType;
        }
    }
}
