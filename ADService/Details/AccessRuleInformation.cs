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
    internal sealed class AccessRuleInformation
    {
        /// <summary>
        /// 轉換提供的 Windows 權限至支援 Json 轉換的對照列舉
        /// </summary>
        /// <param name="activeDirectoryRights">轉換的目標權限</param>
        /// <returns>分割完成的權限對照 HashSet </returns>
        private static HashSet<AccessRuleRightFlags> ToAccessRuleRightFlags(in ActiveDirectoryRights activeDirectoryRights)
        {
            // 目前持有的數值
            ulong valueActiveDirectoryRights = Convert.ToUInt64(activeDirectoryRights);
            // 初始化單一權限 HashSet 表
            HashSet<AccessRuleRightFlags> _Rights = new HashSet<AccessRuleRightFlags>();
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
                _Rights.Add(flag);
            }
            // 權限
            return _Rights;
        }

        /// <summary>
        /// 目標鍵值
        /// </summary>
        internal readonly string NameAttribute;
        /// <summary>
        /// 目標鍵值
        /// </summary>
        internal readonly HashSet<string> PropertySet;
        /// <summary>
        /// 是否允許
        /// </summary>
        internal readonly bool WasAllow;
        /// <summary>
        /// 是否從繼承取得
        /// </summary>
        internal readonly bool IsInherited;
        /// <summary>
        /// 存取規則
        /// </summary>
        internal readonly HashSet<AccessRuleRightFlags> HashSetAccessRights;

        /// <summary>
        /// 設定物件類型限定與鍵值設定
        /// </summary>
        /// <param name="unit">此 GUID 的相關資料</param>
        /// <param name="protertySet">目標鍵值</param>
        /// <param name="accessRule">存取規則, 整包船入取得目標需求資料</param>
        internal AccessRuleInformation(in string name, in HashSet<string> propertySet, in ActiveDirectoryAccessRule accessRule)
        {
            NameAttribute = name;
            PropertySet   = propertySet ?? new HashSet<string>(0);

            WasAllow = accessRule.AccessControlType == AccessControlType.Allow;
            IsInherited = accessRule.IsInherited;
            HashSetAccessRights = ToAccessRuleRightFlags(accessRule.ActiveDirectoryRights);

            // 權限必須存在長度
            if (HashSetAccessRights.Count == 0)
            {
                // 應能取得對照的方法解析描述
                throw new LDAPExceptions($"屬性:{NameAttribute} 解析權限:{accessRule.ActiveDirectoryRights} 後發現無法轉換成功, 請聯繫程式維護人員", ErrorCodes.LOGIC_ERROR);
            }
        }

        /// <summary>
        /// 整理持有的所有存取規則
        /// </summary>
        /// <param name="attributeName">目標存取鍵值</param>
        /// <param name="wasInherited">是否從繼承而來</param>
        /// <param name="accessRuleInformations">所有可用的存取全縣</param>
        /// <returns>符合規則的所有全縣</returns>
        internal static AccessRuleRightFlags CombineAccessRuleRightFlags(in string attributeName, in bool wasInherited, params AccessRuleInformation[] accessRuleInformations)
        {
            // 紀錄允許的權限
            AccessRuleRightFlags accessRuleRightFlagsIsAllow    = AccessRuleRightFlags.None;
            // 紀錄不允許的權限
            AccessRuleRightFlags accessRuleRightFlagsIsDisallow = AccessRuleRightFlags.None;
            // 遍歷所有存取權限
            foreach (AccessRuleInformation accessRuleInformation in accessRuleInformations)
            {
                // 是否為指定的存取鍵值
                bool isAttributeName = attributeName == accessRuleInformation.NameAttribute;
                // 是否於關聯群組內
                bool isInPropertySet = accessRuleInformation.PropertySet.Contains(attributeName);
                /* 符合下述規則時不對外提供
                     - 不是指定的存取鍵值
                     - 不在關聯群組內
                     - 不是全域
                */
                if (!isAttributeName && !isInPropertySet && !string.IsNullOrEmpty(accessRuleInformation.NameAttribute))
                {
                    // 跳過
                    continue;
                }

                // 反之則繼續檢查指定繼承狀態是否相同
                if (wasInherited != accessRuleInformation.IsInherited)
                {
                    // 不同跳過
                    continue;
                }

                // 遍歷可用權限
                foreach (AccessRuleRightFlags accessRuleRightFlag in accessRuleInformation.HashSetAccessRights)
                {
                    // 根據權限是否允許決定如何做疊加
                    if (accessRuleInformation.WasAllow)
                    {
                        // 對允許權限做疊加
                        accessRuleRightFlagsIsAllow |= accessRuleRightFlag;
                    }
                    else
                    {
                        // 對拒絕權限做疊加
                        accessRuleRightFlagsIsDisallow |= accessRuleRightFlag;
                    }
                }
            }

            // 使用拒絕權限作為遮罩過濾允許權限
            return accessRuleRightFlagsIsAllow & ~accessRuleRightFlagsIsDisallow;
        }

        /// <summary>
        /// 忽略是否從繼承而來, 整理持有的所有存取規則
        /// </summary>
        /// <param name="attributeName">目標存取鍵值</param>
        /// <param name="accessRuleInformations">所有可用的存取全縣</param>
        /// <returns>符合規則的所有全縣</returns>
        internal static AccessRuleRightFlags CombineAccessRuleRightFlags(in string attributeName, params AccessRuleInformation[] accessRuleInformations)
        {
            // 紀錄允許的權限
            AccessRuleRightFlags accessRuleRightFlagsIsAllow = AccessRuleRightFlags.None;
            // 紀錄不允許的權限
            AccessRuleRightFlags accessRuleRightFlagsIsDisallow = AccessRuleRightFlags.None;
            // 遍歷所有存取權限
            foreach (AccessRuleInformation accessRuleInformation in accessRuleInformations)
            {
                // 是否為指定的存取鍵值
                bool isAttributeName = attributeName == accessRuleInformation.NameAttribute;
                // 是否於關聯群組內
                bool isInPropertySet = accessRuleInformation.PropertySet.Contains(attributeName);
                /* 符合下述規則時不對外提供
                     - 不是指定的存取鍵值
                     - 不在關聯群組內
                     - 不是全域
                */
                if (!isAttributeName && !isInPropertySet && !string.IsNullOrEmpty(accessRuleInformation.NameAttribute))
                {
                    // 跳過
                    continue;
                }

                // 遍歷可用權限
                foreach (AccessRuleRightFlags accessRuleRightFlag in accessRuleInformation.HashSetAccessRights)
                {
                    // 根據權限是否允許決定如何做疊加
                    if (accessRuleInformation.WasAllow)
                    {
                        // 對允許權限做疊加
                        accessRuleRightFlagsIsAllow |= accessRuleRightFlag;
                    }
                    else
                    {
                        // 對拒絕權限做疊加
                        accessRuleRightFlagsIsDisallow |= accessRuleRightFlag;
                    }
                }
            }

            // 使用拒絕權限作為遮罩過濾允許權限
            return accessRuleRightFlagsIsAllow & ~accessRuleRightFlagsIsDisallow;
        }
    }
}
