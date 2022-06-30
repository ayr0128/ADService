using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using System.DirectoryServices;

namespace ADService.Foundation
{
    /// <summary>
    /// 組織樹狀圖的節點
    /// </summary>
    public class LDAPDomainDNS : LDAPAssembly
    {
        /// <summary>
        /// 建構網域物件
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="dispatcher">入口物件創建器</param>
        /// <param name="propertiesResult">透過找尋取得字的屬性</param>
        /// <exception cref="LDAPExceptions">移除外部整理過屬於此組織單位的成員或組織單位後還有其他剩餘資料時丟出</exception>
        internal LDAPDomainDNS(in DirectoryEntry entry, in LDAPConfigurationDispatcher dispatcher, in ResultPropertyCollection propertiesResult) : base(entry, CategoryTypes.DOMAIN_DNS, dispatcher, propertiesResult) { }
    }
}