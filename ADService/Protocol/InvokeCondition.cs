using Newtonsoft.Json;
using System;
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
        /// 使用遮罩方式取得是否去有指定旗標值
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public ProtocolAttributeFlags MaskFlags(in ProtocolAttributeFlags flags) => Flags & flags;

        /// <summary>
        /// 協議內容: 根據旗標可得知如何解析
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Details { get; private set; }
        /// <summary>
        /// 使用指定格式轉換目標資料
        /// </summary>
        /// <typeparam name="T">樣板, 如果經過 JSON 轉換時作應改為 JSON 解析</typeparam>
        /// <param name="key">鍵值</param>
        /// <returns>是否包含此數值</returns>
        public T CastSingleValue<T>(in string key)
        {
            // 先取得是否存在
            bool isExist = Details.TryGetValue(key, out object storedValue);
            // 檢查是否存在
            if (!isExist)
            {
                // 對外提供預設值
                return default;
            }

            // 存在時進行強制轉換並回傳
            return (T)storedValue;
        }
        /// <summary>
        /// 使用指定格式轉換目標資料
        /// </summary>
        /// <typeparam name="T">樣板, 如果經過 JSON 轉換時作應改為 JSON 解析</typeparam>
        /// <param name="key">鍵值</param>
        /// <returns>是否包含此數值</returns>
        public T[] CastMutipleValue<T>(in string key)
        {
            // 先取得是否存在
            bool isExist = Details.TryGetValue(key, out object storedValue);
            // 檢查是否存在
            if (!isExist)
            {
                // 對外提供預設值
                return default;
            }

            // 存在時進行強制轉換並回傳
            return Array.ConvertAll((object[])storedValue, converttedObject => (T)converttedObject); ;
        }

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
