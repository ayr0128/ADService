using ADService.Environments;

namespace ADService.Media
{
    /// <summary>
    /// 存取權限細節
    /// </summary>
    internal struct ControlAccessDetail
    {
        /// <summary>
        /// 存取規則名稱
        /// </summary>
        internal string Name;
        /// <summary>
        /// 存取規則類型
        /// </summary>
        internal ControlAccessType UnitType;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="name">物件名稱</param>
        /// <param name="unitType">物件類型</param>
        internal ControlAccessDetail(in string name, in ControlAccessType unitType)
        {
            Name = name;
            UnitType = unitType;
        }
    }
}
