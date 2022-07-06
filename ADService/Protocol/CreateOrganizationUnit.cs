using System.Collections.Generic;

namespace ADService.Protocol
{
    /// <summary>
    /// 創建組織單位需求的參數
    /// </summary>
    public sealed class CreateOrganizationUnit
    {
        /// <summary>
        /// 目標物件的名稱, 局部唯一, 如果發現重複會無法進行動作
        /// </summary>
        public string Name = string.Empty;
    }
}
