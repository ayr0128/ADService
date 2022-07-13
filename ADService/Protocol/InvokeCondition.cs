using Newtonsoft.Json;
using System.Collections.Generic;

namespace ADService.Protocol
{
    /// <summary>
    /// 喚醒條件
    /// </summary>
    public class InvokeCondition
    {
        /// <summary>
        /// 限制目標物件類型
        /// </summary>
        public const string CATEGORYLIMITED = "CategoryLimited";
        /// <summary>
        /// 提供物件參數
        /// </summary>
        public const string PROPERTIES = "Properties";
        /// <summary>
        /// 元素
        /// </summary>
        public const string ELEMENTS = "Elements";
        /// <summary>
        /// 目前持有內容
        /// </summary>
        public const string VALUE = "Value";
        /// <summary>
        /// 支援填入的列舉表
        /// </summary>
        public const string ENUMLIST = "EnumList";
        /// <summary>
        /// 支援填入的旗標遮罩
        /// </summary>
        public const string FLAGMASK = "FlagMask";
        /// <summary>
        /// 與其他持有同樣標籤的物件視為同一組物件
        /// </summary>
        public const string COMBINETAG = "CombineWith";
        /// <summary>
        /// 儲存類型
        /// </summary>
        public const string STOREDTYPE = "StoredType";
        /// <summary>
        /// 接收類型
        /// </summary>
        public const string RECEIVEDTYPE = "ReceivedType";
        /// <summary>
        /// 支援方法列表
        /// </summary>
        public const string METHODS = "Methods";

        /// <summary>
        /// 協議旗標
        /// </summary>
        public ProtocolAttributeFlags Flags { get; private set; }
        /// <summary>
        /// 協議內容: 根據旗標可得知如何解析
        /// </summary>

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Details { get; private set; }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="flags">協議旗標</param>
        /// <param name="details">協議內容</param>
        public InvokeCondition(in ProtocolAttributeFlags flags, in Dictionary<string, object> details = null)
        {
            Flags = flags;
            Details = details;
        }
    }
}
