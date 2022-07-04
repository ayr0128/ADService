using ADService.Environments;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.AccessControl;

namespace ADService.Details
{
    /// <summary>
    /// 內部存取結構
    /// </summary>
    internal sealed class AccessRuleConverted
    {
        /// <summary>
        /// 轉換提供的 Windows 權限至支援 Json 轉換的對照列舉
        /// </summary>
        /// <param name="activeDirectoryRights">轉換的目標權限</param>
        /// <returns>分割完成的權限對照 HashSet </returns>
        private static AccessRuleRightFlags ToAccessRuleRightFlags(in ActiveDirectoryRights activeDirectoryRights)
        {
            // 目前持有的數值
            ulong valueActiveDirectoryRights = Convert.ToUInt64(activeDirectoryRights);
            // 初始化單一權限 HashSet 表
            AccessRuleRightFlags _Rights = AccessRuleRightFlags.None;
            // 採用最低位元逐漸偏移的方式來取得目標參數
            for (ulong flagsBase = 0x1; flagsBase < Convert.ToUInt64(AccessRuleRightFlags.GenericAll); flagsBase <<= 0x1)
            {
                // 轉換位元值至旗標
                AccessRuleRightFlags flag = (AccessRuleRightFlags)Enum.ToObject(typeof(AccessRuleRightFlags), flagsBase);
                // 不在一般所有的旗標中
                if ((flag & AccessRuleRightFlags.GenericAll) == 0)
                {
                    // 跳過不處理: 無用處
                    continue;
                }

                // 不是其中之一的資料
                if ((flagsBase & valueActiveDirectoryRights) == 0)
                {
                    // 跳過
                    continue;
                }

                // 其他情況則添加為支援項目
                _Rights |= flag;
            }
            // 權限
            return _Rights;
        }

        /// <summary>
        /// 取得提供的存取歸德中的 GUID HashSet
        /// </summary>
        /// <param name="accessRuleConverteds">取得存取規則</param>
        /// <returns></returns>
        internal static HashSet<Guid> GetGUIDs(in IEnumerable<AccessRuleConverted> accessRuleConverteds)
        {
            // 整理權限 GUID
            HashSet<Guid> accessRuleGUIDs = new HashSet<Guid>();
            // 只需處理非空 GUID 的部分 (包含沒有生效的)
            foreach (AccessRuleConverted accessRuleConverted in accessRuleConverteds)
            {
                // 使用強型別暫存方便閱讀
                Guid attributeGUID = accessRuleConverted.AttributeGUID;
                // 空的 GUID
                if (attributeGUID.Equals(Guid.Empty))
                {
                    // 不必處理
                    continue;
                }

                // 是否已經推入: 因為不同的安全性群組可能會持有相同的存取權限
                if (accessRuleGUIDs.Contains(attributeGUID))
                {
                    // 跳過
                    continue;
                }

                // 推入查詢
                accessRuleGUIDs.Add(attributeGUID);
            }
            // 對外提供此 HashSet
            return accessRuleGUIDs;
        }

        private readonly ActiveDirectoryAccessRule rawActiveDirectoryAccessRule;

        /// <summary>
        /// 目標鍵值
        /// </summary>
        internal Guid AttributeGUID => rawActiveDirectoryAccessRule.ObjectType;
        /// <summary>
        /// 是否允許
        /// </summary>
        internal bool WasAllow => rawActiveDirectoryAccessRule.AccessControlType == AccessControlType.Allow;
        /// <summary>
        /// 是否從繼承取得
        /// </summary>
        internal bool IsInherited => rawActiveDirectoryAccessRule.IsInherited;
        /// <summary>
        /// 是否產生影響
        /// </summary>
        internal bool IsEffected
        {
            get
            {
                // 查看繼承方式決定是否對外提供
                switch (rawActiveDirectoryAccessRule.InheritanceType)
                {
                    // 僅包含自己
                    case ActiveDirectorySecurityInheritance.None:
                        {
                            /* 若 AD 系統正確運作, 發生繼承時此狀趟應只影響持有最原始權限的物件
                                 - 若此權限從繼承而來, 則不對外轉換
                            */
                            return !IsInherited;
                        }
                    case ActiveDirectorySecurityInheritance.SelfAndChildren: // 包含自己與直接子系物件
                    case ActiveDirectorySecurityInheritance.All:             // 包含自己與所有子系物件
                        {
                            // 若 AD 系統正確運作, 發生繼承時此狀趟應會影響各自應影響的範圍
                            return true;
                        }
                    case ActiveDirectorySecurityInheritance.Children:    // 僅包含直接子系物件
                    case ActiveDirectorySecurityInheritance.Descendents: // 包含所有子系物件
                        {
                            /* 若 AD 系統正確運作, 發生繼承時此狀趟應只影響持有繼承權限的物件
                                 - 若此權限從繼承而來, 則對外轉換
                            */
                            return IsInherited;
                        }
                    // 其他的預設狀態
                    default:
                        {
                            // 丟出例外: 因為此狀態沒有實作
                            throw new LDAPExceptions($"存取規則:{rawActiveDirectoryAccessRule.IdentityReference} 設定物件時發現未實作的繼承狀態:{rawActiveDirectoryAccessRule.InheritanceType} 因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                        }
                }
            }
        }
        /// <summary>
        /// 存取規則
        /// </summary>
        internal AccessRuleRightFlags AccessRuleRights => ToAccessRuleRightFlags(rawActiveDirectoryAccessRule.ActiveDirectoryRights);

        /// <summary>
        /// 設定物件類型限定與鍵值設定
        /// </summary>
        /// <param name="activeDirectoryAccessRule">存取規則, 整包船入取得目標需求資料</param>
        internal AccessRuleConverted(in ActiveDirectoryAccessRule activeDirectoryAccessRule) => rawActiveDirectoryAccessRule = activeDirectoryAccessRule;
    }
}
