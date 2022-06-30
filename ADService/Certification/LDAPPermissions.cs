using ADService.Details;
using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using System;
using System.Collections.Generic;

namespace ADService.Certification
{
    /// <summary>
    /// 存放針對執行者對於目標物件的相關存取規則
    /// </summary>
    internal sealed class LDAPPermissions
    {
        /// <summary>
        /// 內部使用的儲存結構
        /// </summary>
        private sealed class InheritedAccessRuleRight
        {
            /// <summary>
            /// 內部存取用的對應規則
            /// </summary>
            private Dictionary<string, Dictionary<bool, AccessRuleRightFlags>> dictionaryNameWithAccessRuleRightFlags = new Dictionary<string, Dictionary<bool, AccessRuleRightFlags>>();

            /// <summary>
            /// 內部使用, 設置相關存取權限
            /// </summary>
            /// <param name="name">目標名稱</param>
            /// <param name="isInherited">是否透過繼承取得</param>
            /// <param name="accessRuleRightFlagsReceived">設置的旗標</param>
            internal void Set(in string name, in bool isInherited, in AccessRuleRightFlags accessRuleRightFlagsReceived)
            {
                // 取得目標存取規則的存取情況
                if (!dictionaryNameWithAccessRuleRightFlags.TryGetValue(name, out Dictionary<bool, AccessRuleRightFlags> dictionaryInheritedeWithAccessRuleRightFlags))
                {
                    // 無法取得時需重新宣告, 因為 bool 作為鍵值時必定只有 true 與 false 兩種值, 所以容器必定只需要兩個
                    dictionaryInheritedeWithAccessRuleRightFlags = new Dictionary<bool, AccessRuleRightFlags>(2);
                    // 推入此物件
                    dictionaryNameWithAccessRuleRightFlags.Add(name, dictionaryInheritedeWithAccessRuleRightFlags);
                }

                // 取得目前繼承狀況的旗標
                if (!dictionaryInheritedeWithAccessRuleRightFlags.TryGetValue(isInherited, out AccessRuleRightFlags accessRuleRightFlagsStored))
                {
                    // 使用劑成狀態將旗標填入
                    dictionaryInheritedeWithAccessRuleRightFlags.Add(isInherited, accessRuleRightFlagsReceived);
                }
                else
                {
                    // 兩者間實施 OR 運算
                    dictionaryInheritedeWithAccessRuleRightFlags[isInherited] = accessRuleRightFlagsStored | accessRuleRightFlagsReceived;
                }
            }


            /// <summary>
            /// 內部使用, 取得相關存取權限
            /// </summary>
            /// <param name="name">目標名稱</param>
            /// <param name="isInherited">是否透過繼承取得</param>
            internal AccessRuleRightFlags Get(in string name, in bool? isInherited)
            {
                // 者到指定目標的權限狀態
                if (!dictionaryNameWithAccessRuleRightFlags.TryGetValue(name, out Dictionary<bool, AccessRuleRightFlags> dictionaryInheritedeWithAccessRuleRightFlags))
                {
                    // 找不到就跳過
                    return AccessRuleRightFlags.None;
                }

                // 從限制的目標取得的存取權限
                AccessRuleRightFlags accessRuleRightFlags = AccessRuleRightFlags.None;
                // 有要求劑成狀態
                if (isInherited != null)
                {
                    // 從指定劑成狀態取得存取權限
                    dictionaryInheritedeWithAccessRuleRightFlags.TryGetValue(isInherited.Value, out accessRuleRightFlags);
                    // 處理完成跳過下方的處理
                    return accessRuleRightFlags;
                }

                // 遍歷儲存的資料
                foreach (AccessRuleRightFlags accessRuleRightFlagsStored in dictionaryInheritedeWithAccessRuleRightFlags.Values)
                {
                    // 蝶家權限
                    accessRuleRightFlags |= accessRuleRightFlagsStored;
                }
                // 處理完成跳過下方的處理
                return accessRuleRightFlags;
            }
        }

        /// <summary>
        /// 允許的存取權限
        /// </summary>
        private readonly InheritedAccessRuleRight InheritedAccessRuleRightAllowed = new InheritedAccessRuleRight();
        /// <summary>
        /// 拒絕的存取權限
        /// </summary>
        private readonly InheritedAccessRuleRight InheritedAccessRuleRightDisallowed = new InheritedAccessRuleRight();

        /// <summary>
        /// 建構子: 取得指定影響類型的存取權限
        /// </summary>
        /// <param name="dispatcher">設定分配器</param>
        /// <param name="isEffected">指定是否慘生影響以過濾非此類型之外的存取規則: NULL 時全部都取得</param>
        /// <param name="accessRuleConverteds">目前的存取規則</param>
        internal LDAPPermissions(in LDAPConfigurationDispatcher dispatcher, in bool? isEffected, params AccessRuleConverted[] accessRuleConverteds)
        {
            // 取得能產生影響的 GUID
            HashSet<Guid> accessRuleGUIDs = AccessRuleConverted.GetGUIDs(isEffected, accessRuleConverteds);
            // 取得匹配的所有資: 注意此處的 GUID 是小寫
            Dictionary<string, UnitDetail> dictionaryGUIDWithAccessRuleObjectDetail = dispatcher.GetUnitDetail(accessRuleGUIDs);
            // 轉換匹配用陣列
            foreach (AccessRuleConverted accessRuleConverted in accessRuleConverteds)
            {
                // 根據允許或拒絕取得實際操作目標
                InheritedAccessRuleRight inheritedAccessRuleRight = accessRuleConverted.WasAllow ? InheritedAccessRuleRightAllowed : InheritedAccessRuleRightDisallowed;

                // 是否為繼承
                bool isInherited = accessRuleConverted.IsInherited;
                // 允許的旗標職
                AccessRuleRightFlags accessRuleRightFlags = accessRuleConverted.AccessRuleRights;

                // 將資料轉換成小寫
                string attributeGUIDLower = accessRuleConverted.AttributeGUID.ToString("D").ToLower();
                // 除了全域權限之外都應取得對應的權限資料
                bool isExist = dictionaryGUIDWithAccessRuleObjectDetail.TryGetValue(attributeGUIDLower, out UnitDetail unitDetail);
                // 譨取得時須使用找尋到的目標轉換名稱
                switch (isExist ? unitDetail.UnitType : UnitType.NONE)
                {
                    // 屬性
                    case UnitType.ATTRIBUTE:
                    // 類型
                    case UnitType.CLASS:
                        {
                            // 設置參數
                            inheritedAccessRuleRight.Set(unitDetail.Name, isInherited, accessRuleRightFlags);
                        }
                        break;
                    // 額萬權限
                    case UnitType.EXTENDEDRIGHT:
                        {
                            // 取得指定目標額外權限
                            UnitExtendedRight unitExtendedRight = dispatcher.GetExtendedRight(accessRuleConverted.AttributeGUID);
                            // 取得此額外權限依賴的藍本
                            UnitSchema[] unitSchemas = dispatcher.GetPropertySet(unitExtendedRight);
                            // 取得此安全屬性的關聯物件並設置相關權限
                            foreach (UnitSchema unitSchema in unitSchemas)
                            {
                                // 設置參數
                                inheritedAccessRuleRight.Set(unitSchema.Name, isInherited, accessRuleRightFlags);
                            }

                            // 不依賴任何藍本的權限是需要觸發額外判斷的權限
                            if (unitSchemas.Length == 0)
                            {
                                // 設置參數
                                inheritedAccessRuleRight.Set(unitDetail.Name, isInherited, accessRuleRightFlags);
                            }
                        }
                        break;
                    default:
                        {
                            // 設置為全域
                            inheritedAccessRuleRight.Set(string.Empty, isInherited, accessRuleRightFlags);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 取得指定屬性職是否存在指定權限
        /// </summary>
        /// <param name="name">目標群取權限</param>
        /// <param name="isInherited">是否重繼承取得, NULL 時會忽略劑成狀態</param>
        /// <param name="accessRuleRightFlagsLimited">任意一個權限存在就是允許</param>
        /// <returns>是否可用</returns>
        internal bool IsAllow(in string name, in bool? isInherited, in AccessRuleRightFlags accessRuleRightFlagsLimited)
        {
            // 從限制的目標取得的存取權限
            AccessRuleRightFlags accessRuleRightFlagsCombineAllow = AccessRuleRightFlags.None;
            // 從限制的目標取得的存取權限
            AccessRuleRightFlags accessRuleRightFlagsCombineDisallow = AccessRuleRightFlags.None;
            // 固定找尋全域與指定的參數
            foreach (string attributeName in new string[] { string.Empty, name })
            {
                // 取得允許的旗標
                accessRuleRightFlagsCombineAllow |= InheritedAccessRuleRightAllowed.Get(attributeName, isInherited);
                // 取得拒絕的旗標
                accessRuleRightFlagsCombineDisallow |= InheritedAccessRuleRightDisallowed.Get(attributeName, isInherited);
            }
            // 允許的旗標使用拒絕旗標的反向遮罩過濾後, 即為可用的旗標
            AccessRuleRightFlags accessRuleRightFlagsCombine = accessRuleRightFlagsCombineAllow & ~accessRuleRightFlagsCombineDisallow;
            // 兩者間作 AND 運算, 任意一個權限存在即可
            return (accessRuleRightFlagsCombine & accessRuleRightFlagsLimited) != AccessRuleRightFlags.None;
        }
    }
}
