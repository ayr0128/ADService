using System;

namespace ADService.DynamicParse
{
    /// <summary>
    /// 定義參數用來做動態解析的屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ADDescriptionProperty : Attribute
    {
        /// <summary>
        /// 取得並轉換的目標鍵值
        /// </summary>
        internal string PropertyName;

        /// <summary>
        /// 建構時務必提供轉換目標鍵值
        /// </summary>
        /// <param name="propertyName">目標鍵值</param>
        public ADDescriptionProperty(string propertyName) => PropertyName = propertyName;
    }
}
