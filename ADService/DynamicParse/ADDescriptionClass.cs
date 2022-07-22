using System;
using System.Collections.Generic;

namespace ADService.DynamicParse
{
    /// <summary>
    /// 僅能賦予至類別上的特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ADDescriptionClass : Attribute
    {
        /// <summary>
        /// 取得並轉換的目標鍵值
        /// </summary>
        private HashSet<string> ClassNames;

        /// <summary>
        /// 物件的驅動類型是否允許解析
        /// </summary>
        /// <param name="className">物件類型</param>
        internal bool IsAllow(in string className) => ClassNames.Contains(className);

        /// <summary>
        /// 建構時務必提供
        /// </summary>
        /// <param name="classNames">限制類別</param>
        public ADDescriptionClass(params string[] classNames) => ClassNames = new HashSet<string>(classNames);
    }
}
