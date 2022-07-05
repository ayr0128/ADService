using System.Collections.Generic;

namespace ADService.Protocol
{
    /// <summary>
    /// 創建使用者需求的參數
    /// </summary>
    public sealed class CreateGroup
    {
        /// <summary>
        /// 目標物件的名稱, 全域唯一, 如果發現重複會無法進行動作
        /// </summary>
        public string Name = string.Empty;
    }
}
