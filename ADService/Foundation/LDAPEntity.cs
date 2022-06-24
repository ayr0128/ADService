using ADService.Environments;
using ADService.Features;
using ADService.Media;
using ADService.Protocol;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace ADService.Foundation
{
    /// <summary>
    /// 成員, 電腦, 群組等名稱唯一的物件
    /// </summary>
    public abstract class LDAPEntity :
        LDAPObject,
        IRevealerSID,
        IRevealerMemberOf
    {
        #region 介面:IRevealerSID
        string IRevealerSID.Value => StoredProperties.GetPropertySID(Properties.C_OBJECTSID);
        #endregion

        #region 介面:IRevealerMemberOf
        /// <summary>
        /// 隸屬群組
        /// </summary>
        internal Dictionary<string, LDAPRelationship> MemberOf;

        LDAPRelationship[] IRevealerMemberOf.Elements => MemberOf.Values.ToArray();
        #endregion

        /// <summary>
        /// 透過建構子解析內容資料
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="entriesMedia">入口物件創建器</param>
        /// <param name="propertiesResult">透過找尋取得字的屬性</param>
        internal LDAPEntity(in DirectoryEntry entry, in LDAPEntriesMedia entriesMedia, in ResultPropertyCollection propertiesResult) : base(entry, entriesMedia, propertiesResult)
        {
            // 取得 memberOf: 不存在應丟出例外
            string[] memberOf = StoredProperties.GetPropertyMultiple<string>(Properties.P_MEMBEROF);
            // 初始化隸屬群組
            MemberOf = ToRelationshipByDNs(entriesMedia, memberOf);
        }
    }
}
