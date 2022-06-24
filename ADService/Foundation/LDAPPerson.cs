﻿using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using System.DirectoryServices;

namespace ADService.Foundation
{
    /// <summary>
    /// 成員
    /// </summary>
    public class LDAPPerson : LDAPEntity
    {
        /// <summary>
        /// 主要隸屬群組的 SID
        /// </summary>
        internal string PrimaryGroupSID
        {
            get
            {
                // 取得 SID: 不存在應丟出例外
                string primarySID = StoredProperties.GetPropertySID(Properties.C_OBJECTSID);
                // 取得 GROUPID: 不存在應丟出例外
                int primaryGROUPID = StoredProperties.GetPropertySingle<int>(Properties.C_PRIMARYGROUPID);

                // 已知成員的 SID 去除最後一個 '-' 後的資料則是網域 SID
                int index = primarySID.LastIndexOf('-');
                // 組成主要隸屬群組 SID
                return $"{primarySID.Substring(0, index)}-{primaryGROUPID}";
            }
        }

        /// <summary>
        /// 透過建構子解析內容資料
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="entriesMedia">入口物件創建器</param>
        /// <param name="propertiesResult">透過找尋取得字的屬性</param>
        internal LDAPPerson(in DirectoryEntry entry, in LDAPEntriesMedia entriesMedia, in ResultPropertyCollection propertiesResult) : base(entry, entriesMedia, propertiesResult)
        {
            // 不是允許類型: 成員
            if ((Type & CategoryTypes.PERSON) == CategoryTypes.NONE)
            {
                // 對外丟出類型不正確例外
                throw new LDAPExceptions($"基礎物件類型:{Type} 不是期望的成員類型:{CategoryTypes.PERSON}", ErrorCodes.LOGIC_ERROR);
            }

            // 初始化主要隸屬群組
            LDAPRelationship primaryGroup = ToRelationshipBySID(entriesMedia, PrimaryGroupSID);
            // 將主要隸屬群組加入隸屬群組
            MemberOf.Add(primaryGroup.DistinguishedName, primaryGroup);
        }

        internal override LDAPObject SwapFrom(in LDAPObject newObject)
        {
            // 先執行舊版動作
            LDAPObject resultObject = base.SwapFrom(newObject);
            // 成功執行時
            if (resultObject == this)
            {
                // 則交換物件必定是可以轉換為自己這個類型
                LDAPPerson uintFrom = (LDAPPerson)newObject;
                // 額外交換隸屬群組: 常常會被異動
                MemberOf = uintFrom.MemberOf;
            }
            return resultObject;
        }
    }
}
