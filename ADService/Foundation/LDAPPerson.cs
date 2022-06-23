using ADService.Environments;
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
                if (!StoredProperties.GetPropertyValue(LDAPAttributes.C_OBJECTSID, out string primarySID) || string.IsNullOrEmpty(primarySID))
                {
                    throw new LDAPExceptions($"嘗試取得物件:{DistinguishedName} 的:{LDAPAttributes.C_OBJECTSID} 但資料不存在, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                }

                // 已知成員的 SID 去除最後一個 '-' 後的資料則是網域 SID
                int index = primarySID.LastIndexOf('-');

                // 取得 GROUPID: 不存在應丟出例外
                if (!StoredProperties.GetPropertyValue(LDAPAttributes.C_PRIMARYGROUPID, out int primaryGROUPID))
                {
                    throw new LDAPExceptions($"嘗試取得物件:{DistinguishedName} 的:{LDAPAttributes.C_PRIMARYGROUPID} 但資料不存在, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                }

                // 組成主要隸屬群組 SID
                return $"{primarySID.Substring(0, index)}-{primaryGROUPID}";
            }
        }

        /// <summary>
        /// 透過建構子解析內容資料
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <param name="entriesMedia">入口物件創建器</param>
        internal LDAPPerson(in DirectoryEntry entry, in LDAPEntriesMedia entriesMedia) : base(entry, entriesMedia)
        {
            // 不是允許類型: 成員
            if ((Type & CategoryTypes.PERSON) == CategoryTypes.NONE)
            {
                // 對外丟出類型不正確例外
                throw new LDAPExceptions($"基礎物件類型:{Type} 不是期望的成員類型:{CategoryTypes.PERSON}", ErrorCodes.LOGIC_ERROR);
            }

            // 設定支援鍵值
            StoredProperties.SetPropertiesSupported(
                entry.Properties, // 搜尋得到的結果

                LDAPAttributes.P_DISPLAYNAME,              // 支援: 顯示名稱
                LDAPAttributes.P_SN,                       // 支援: 姓
                LDAPAttributes.P_GIVENNAME,                // 支援: 名
                LDAPAttributes.P_INITIALS,                 // 支援: 縮寫
                LDAPAttributes.P_USERACCOUNTCONTROL,       // 支援: 使用者帳號控制旗標
                LDAPAttributes.P_PWDLASTSET,               // 支援: 密碼最後設置時間
                LDAPAttributes.P_LOCKOUTTIME,              // 支援: 帳號鎖定時間
                LDAPAttributes.P_ACCOUNTEXPIRES,           // 支援: 密碼過期時間
                LDAPAttributes.P_SUPPORTEDENCRYPTIONTYPES, // 支援: 加密方式支援

                LDAPAttributes.C_PRIMARYGROUPID // 支援: 主要隸屬群組
            );

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
