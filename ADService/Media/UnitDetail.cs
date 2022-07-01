using ADService.Environments;

namespace ADService.Media
{
    /// <summary>
    /// 存取權限細節
    /// </summary>
    internal struct ControlAccessDetail
    {
        /// <summary>
        /// 使用屬性避免空值
        /// </summary>
        internal string Name => AttbuteName ?? string.Empty;
        /// <summary>
        /// 存取規則名稱
        /// </summary>
        private readonly string AttbuteName;
        /// <summary>
        /// 存取規則類型
        /// </summary>
        internal ControlAccessType AccessType { get; private set; }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="attributeName">物件名稱</param>
        /// <param name="accessType">物件類型</param>
        internal ControlAccessDetail(in string attributeName, in ControlAccessType accessType)
        {
            AttbuteName = attributeName;
            AccessType = accessType;
        }
    }
}
