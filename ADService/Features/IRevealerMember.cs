using ADService.Media;

namespace ADService.Features
{
    /// <summary>
    /// 支援查看 成員 的介面
    /// </summary>
    public interface IRevealerMember
    {
        /// <summary>
        /// 取得所有元素陣列
        /// </summary>
        LDAPRelationship[] Elements { get; }
    }
}
