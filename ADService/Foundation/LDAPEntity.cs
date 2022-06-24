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
        string IRevealerSID.Value
        {
            get
            {
                // 嘗試取得 SID 的儲存資料
                if (!StoredProperties.GetPropertySID(LDAPAttributes.C_OBJECTSID, out string valueSID) || string.IsNullOrEmpty(valueSID))
                {
                    throw new LDAPExceptions($"嘗試取得物件:{DistinguishedName} 的:{LDAPAttributes.C_OBJECTSID} 但資料不存在, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                }

                // 對外提供 SID
                return valueSID;
            }
        }
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
        internal LDAPEntity(in DirectoryEntry entry, in LDAPEntriesMedia entriesMedia) : base(entry, entriesMedia)
        {
            // 取得 memberOf: 不存在應丟出例外
            StoredProperties.GetPropertyMultiple(LDAPAttributes.P_MEMBEROF, out string[] memberOf);
            // 初始化隸屬群組
            MemberOf = ToRelationshipByDNs(entriesMedia, memberOf);
        }
    }
}
