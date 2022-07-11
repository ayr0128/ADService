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
        /// 紀錄原始資料
        /// </summary>
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
        /// 是否為空 GUID
        /// </summary>
        internal bool IsEmpty => rawActiveDirectoryAccessRule.ObjectType.Equals(Guid.Empty);
        /// <summary>
        /// 是否產生影響
        /// </summary>
        internal bool IsEffected(in HashSet<string> classGUIDs)
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
                        // 取得是否為限制子系物件
                        bool isClassInherited = rawActiveDirectoryAccessRule.InheritanceFlags != InheritanceFlags.None;
                        // 限制的繼承類型是否為空
                        bool isInherbitedEmpty = rawActiveDirectoryAccessRule.InheritedObjectType.Equals(Guid.Empty);
                        // 限制鍵類型的 GUID
                        string inheritedObjectGUIDLower = rawActiveDirectoryAccessRule.InheritedObjectType.ToString("D").ToLower();
                        // 繼承物件是否可用
                        bool isInherbitedUsed = isClassInherited && (isInherbitedEmpty || classGUIDs.Contains(inheritedObjectGUIDLower));
                        // 若 AD 系統正確運作, 發生繼承時此狀趟應會影響各自應影響的範圍
                        return !IsInherited ? true : isInherbitedUsed;
                    }
                case ActiveDirectorySecurityInheritance.Children:    // 僅包含直接子系物件
                case ActiveDirectorySecurityInheritance.Descendents: // 包含所有子系物件
                    {
                        // 取得是否為限制子系物件
                        bool isClassInherited = rawActiveDirectoryAccessRule.InheritanceFlags != InheritanceFlags.None;
                        // 限制的繼承類型是否為空
                        bool isInherbitedEmpty = rawActiveDirectoryAccessRule.InheritedObjectType.Equals(Guid.Empty);
                        // 限制鍵類型的 GUID
                        string inheritedObjectGUIDLower = rawActiveDirectoryAccessRule.InheritedObjectType.ToString("D").ToLower();
                        // 繼承物件是否可用
                        bool isInherbitedUsed = isClassInherited && (isInherbitedEmpty || classGUIDs.Contains(inheritedObjectGUIDLower));
                        /* 若 AD 系統正確運作, 發生繼承時此狀趟應只影響持有繼承權限的物件
                             - 若此權限從繼承而來, 則對外轉換
                        */
                        return IsInherited && isInherbitedUsed;
                    }
                // 其他的預設狀態
                default:
                    {
                        // 丟出例外: 因為此狀態沒有實作
                        throw new LDAPExceptions($"存取規則:{rawActiveDirectoryAccessRule.IdentityReference} 設定物件時發現未實作的繼承狀態:{rawActiveDirectoryAccessRule.InheritanceType} 因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                    }
            }
        }

        /// <summary>
        /// 存取規則
        /// </summary>
        internal ActiveDirectoryRights AccessRuleRights => rawActiveDirectoryAccessRule.ActiveDirectoryRights;

        /// <summary>
        /// 設定物件類型限定與鍵值設定
        /// </summary>
        /// <param name="activeDirectoryAccessRule">存取規則, 整包船入取得目標需求資料</param>
        internal AccessRuleConverted(in ActiveDirectoryAccessRule activeDirectoryAccessRule) => rawActiveDirectoryAccessRule = activeDirectoryAccessRule;
    }
}
