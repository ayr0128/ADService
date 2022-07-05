using ADService.Environments;
using ADService.Features;
using ADService.Media;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace ADService.Foundation
{
    /// <summary>
    /// 群組
    /// </summary>
    public class LDAPGroup :
        LDAPEntity,
        IRevealerMember
    {
        #region 介面:IRevealerMember
        /// <summary>
        /// 直接關聯的群組成員與群組
        /// </summary>
        internal Dictionary<string, LDAPRelationship> Member;

        LDAPRelationship[] IRevealerMember.Elements => Member.Values.ToArray();
        #endregion

        /// <summary>
        /// 主要隸屬群組的關聯 Token
        /// </summary>
        internal int PrimaryGroupyToken
        {
            get
            {
                // 取得 SID: 不存在應丟出例外
                string primarySID = GetPropertySID(Properties.C_OBJECTSID);

                // 已知群組 SID 最後一個 '-' 後的資料就是 PrimaryGroupToken
                int index = primarySID.LastIndexOf('-');
                // 組成主要隸屬群組的關聯 Token
                return int.Parse(primarySID.Substring(index + 1));
            }
        }

        /// <summary>
        /// 建構新的群組
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="dispatcher">入口物件創建器</param>
        internal LDAPGroup(in DirectoryEntry entry, in LDAPConfigurationDispatcher dispatcher) : base(entry, dispatcher)
        {
            // 限制應為: 成員, 內部安全性群組
            const CategoryTypes TypeLimited = CategoryTypes.GROUP | CategoryTypes.ForeignSecurityPrincipals;
            // 不是允許類型
            if ((Type & TypeLimited) == CategoryTypes.NONE)
            {
                // 對外丟出類型不正確例外
                throw new LDAPExceptions($"基礎物件類型:{Type} 不是期望的群組類型:{TypeLimited}", ErrorCodes.LOGIC_ERROR);
            }

            // 取得 member 不存在應丟出例外
            string[] member = GetPropertyMultiple<string>(Properties.P_MEMBER);
            // 初始化成員
            Member = ToRelationshipByDNs(dispatcher, member);

            // 初始化主要隸屬群組成員
            Dictionary<string, LDAPRelationship> primaryRelationship = ToRelationshipByToken(dispatcher, PrimaryGroupyToken);
            // 將主要隸屬物件加入成員
            Array.ForEach(primaryRelationship.Values.ToArray(), (relationship) => Member.Add(relationship.DistinguishedName, relationship));
        }

        internal override LDAPObject SwapFrom(in LDAPObject newObject)
        {
            // 先執行舊版動作
            LDAPObject resultObject = base.SwapFrom(newObject);
            // 成功執行時
            if (resultObject == this)
            {
                // 則交換物件必定是可以轉換為自己這個類型
                LDAPGroup uintFrom = (LDAPGroup)newObject;
                // 額外交換成員
                Member = uintFrom.Member;
                // 額外交換隸屬群組
                MemberOf = uintFrom.MemberOf;
            }
            return resultObject;
        }
    }
}
