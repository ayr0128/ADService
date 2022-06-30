namespace ADService.Features
{
    /// <summary>
    /// 支援查看 隸屬組織 的介面
    /// </summary>
    public interface IRevealerSecuritySIDs
    {
        /// <summary>
        /// 取得 SID
        /// </summary>
        string[] Values { get; }
    }
}
