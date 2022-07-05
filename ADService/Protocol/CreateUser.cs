using System.Collections.Generic;

namespace ADService.Protocol
{
    /// <summary>
    /// 創建使用者需求的參數
    /// </summary>
    public sealed class CreateUser
    {
        /// <summary>
        /// 目標物件的名稱, 全域唯一, 如果發現重複會無法進行動作
        /// </summary>
        public string Name = string.Empty;
        /// <summary>
        /// 帳號: 也應是全域唯一
        /// </summary>
        public string Account = string.Empty;
        /// <summary>
        /// 密碼: 須符合網域安全規則
        /// </summary>
        public string Password = string.Empty;
        /// <summary>
        /// 只掉須提供的參數
        /// </summary>
        public Dictionary<string, string> DictionaryAttributeNameWithValue = new Dictionary<string, string>();
    }
}
