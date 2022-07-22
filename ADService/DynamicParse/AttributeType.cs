using System;

namespace ADService.DynamicParse
{
    /// <summary>
    /// 解析屬性類型字串
    /// </summary>
    internal class AttributeType : IExpired
    {
        #region 定位設定
        /// <summary>
        /// 用來分割字串的字串
        /// </summary>
        private readonly static string[] SPLIT_STRINGS = new string[] { "(", " ", "'", ")" };
        /// <summary>
        /// 名稱索引名稱
        /// </summary>
        private const string NAME_INDEX = "NAME";
        /// <summary>
        /// 語法索引名稱
        /// </summary>
        private const string SYNTAX_INDEX = "SYNTAX";
        /// <summary>
        /// 儲存目標是否應微陣列
        /// </summary>
        private const string SINGLEVALUE_INDEX = "SINGLE-VALUE";
        #endregion

        /// <summary>
        /// 紀錄原始屬性類型描述
        /// </summary>
        private readonly string AttributeTypeDescription;

        /// <summary>
        /// 屬性名稱
        /// </summary>
        internal string Name
        {
            get
            {
                // 取得描述
                string[] attributeDescriptions = AttributeTypeDescription.Split(SPLIT_STRINGS, StringSplitOptions.RemoveEmptyEntries);
                // 取得名稱索引位置
                int index = Array.IndexOf(attributeDescriptions, NAME_INDEX);
                // 必定能夠發現名稱的位置: 名稱索引位置 + 1 就是名稱的位置
                return attributeDescriptions[index + 1];
            }
        }

        /// <summary>
        /// 物件 OID
        /// </summary>
        internal string OIDObject => AttributeTypeDescription.Split(SPLIT_STRINGS, StringSplitOptions.RemoveEmptyEntries)[0];
        /// <summary>
        /// 類型 OID 
        /// </summary>
        internal string OIDSyntax
        {
            get
            {
                // 取得描述
                string[] attributeDescriptions = AttributeTypeDescription.Split(SPLIT_STRINGS, StringSplitOptions.RemoveEmptyEntries);
                // 取得語法索引位置
                int index = Array.IndexOf(attributeDescriptions, SYNTAX_INDEX);
                // 必定能夠發現名稱的位置: 名稱索引語法 + 1 就是類型 OID 的位置
                return attributeDescriptions[index + 1];
            }
        }

        /// <summary>
        /// 儲存目標是否應微陣列
        /// </summary>
        internal bool IsSingle
        {
            get
            {
                // 取得描述
                string[] attributeDescriptions = AttributeTypeDescription.Split(SPLIT_STRINGS, StringSplitOptions.RemoveEmptyEntries);
                // 取得語法索引位置
                int index = Array.IndexOf(attributeDescriptions, SINGLEVALUE_INDEX);
                // 找不到索引時代表預期儲存目標不是陣列
                return index != -1;
            }
        }

        /// <summary>
        /// 啟用時間
        /// </summary>
        private readonly DateTime EnableTime = DateTime.UtcNow;

        bool IExpired.Check(in TimeSpan duration) => (DateTime.UtcNow - EnableTime) >= duration;

        /// <summary>
        /// 轉換 IDIF 中紀錄的屬性類型描述
        /// </summary>
        /// <param name="attributeTypeDescription">屬性類型描述</param>
        internal AttributeType(in string attributeTypeDescription) => AttributeTypeDescription = attributeTypeDescription;
    }
}
