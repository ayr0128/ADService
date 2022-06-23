namespace ADService.Protocol
{
    /// <summary>
    /// 改鰾密碼用的協議結構
    /// </summary>
    public sealed class ChangePWD
    {
        /// <summary>
        /// 舊密碼
        /// </summary>
        public string From;
        /// <summary>
        /// 新密碼
        /// </summary>
        public string To;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="from">舊密碼</param>
        /// <param name="to">新密碼</param>
        public ChangePWD(in string from, in string to)
        {
            From = from;
            To = to;
        }
    }
}
