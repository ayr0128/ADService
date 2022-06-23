using ADService.Protocol;

namespace ADService.Features
{
    /// <summary>
    /// 支援查看 隸屬組織 的介面
    /// </summary>
    public interface IRevealerMemberOf
    {
        /// <summary>
        /// 取得所有元素陣列
        /// </summary>
        LDAPRelationship[] Elements { get; }
    }
}
