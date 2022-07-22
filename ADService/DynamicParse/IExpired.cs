using System;

namespace ADService.DynamicParse
{
    /// <summary>
    /// 是否過期
    /// </summary>
    internal interface IExpired
    {
        /// <summary>
        /// 檢查紀錄的時間是否已經存債超過指定時間
        /// </summary>
        /// <param name="duration">經過多久後過期</param>
        /// <returns>是否過期</returns>
        bool Check(in TimeSpan duration);
    }
}
