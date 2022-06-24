using ADService.Configuration;
using ADService.Environments;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Reflection;
using System.Security.Principal;

namespace ADService.Revealer
{
    /// <summary>
    /// 使用 JSON 做為基底的特性鍵值轉換器
    /// </summary>
    internal sealed class LDAPProperties
    {
        /// <summary>
        /// 目前儲存的特性鍵值資料
        /// </summary>
        private readonly Dictionary<string, PropertyDetail> dictionaryNameWithPorpertyDetail = new Dictionary<string, PropertyDetail>();

        /// <summary>
        /// 從入口物件取得的資料
        /// </summary>
        private class PropertyDetail
        {
            /// <summary>
            /// 是否為單一數值
            /// </summary>
            internal bool IsSingleValue;
            /// <summary>
            /// 從入口物件取得的資料數值
            /// </summary>
            internal object[] PropertyValue;

            /// <summary>
            /// 建構藍本物件如何解析
            /// </summary>
            /// <param name="schema">藍本物件</param>
            /// <param name="property">入口物件儲存資料</param>
            internal PropertyDetail(in UnitSchema schema, in PropertyValueCollection property)
            {
                PropertyValue = new object[property.Count];
                IsSingleValue = schema.IsSingleValued;

                // 根據是否為單一數值決定如何處理
                if (schema.IsSingleValued)
                {
                    // 單筆資料
                    PropertyValue[0] = property.Value;
                }
                else
                {
                    // 多筆資料
                    property.CopyTo(PropertyValue, 0);
                }
            }
        }

        /// <summary>
        /// 以 SID 記錄各條存取權限
        /// </summary>
        private readonly Dictionary<string, List<AccessRuleInformation>> dictionarySIDWithPermissions = new Dictionary<string, List<AccessRuleInformation>>();

        /// <summary>
        /// 建構特性儲存與分析類別
        /// </summary>
        /// <param name="configuration">藍本物件</param>
        /// <param name="entry">入口物件</param>
        internal LDAPProperties(in LDAPConfiguration configuration, in DirectoryEntry entry)
        {
            // 額外權限使用的屬性對照表
            Dictionary<string, HashSet<string>> dictionaryNameWithPropertySet = new Dictionary<string, HashSet<string>>();
            // 遍歷屬性找到屬性的
            foreach (PropertyValueCollection value in entry.Properties.Values)
            {
                // 取得目標藍本描述: 內部將戰存屬性格式
                UnitSchema schema = configuration.GetSchema(value.PropertyName);
                // 若無法取得則跳過處理
                if (schema == null)
                {
                    // 跳過
                    continue;
                }

                // 宣告實體資料
                PropertyDetail propertyDetail = new PropertyDetail(schema, value);
                // 整理屬性
                dictionaryNameWithPorpertyDetail.Add(value.PropertyName.ToLower(), propertyDetail);
                // 檢查是否需要加入作為群組物件
                if (LDAPConfiguration.IsGUIDEmpty(schema.SecurityGUID))
                {
                    // 不需要, 跳握
                    continue;
                }

                // 轉換成對應 GUID
                Guid SecurityGUID = new Guid(schema.SecurityGUID);
                // 取得額外資訊
                UnitExtendedRight extendedRight = configuration.GetExtendedRight(SecurityGUID);
                // 檢查之前是否已經設置
                if (!dictionaryNameWithPropertySet.TryGetValue(extendedRight.Name, out HashSet<string> valueHashSet))
                {
                    // 尚未設置需重新宣告
                    valueHashSet = new HashSet<string>();
                    // 推入字典
                    dictionaryNameWithPropertySet.Add(extendedRight.Name, valueHashSet);
                }

                // 展示名稱絕對不會重複, 如果重複必定是 AD 設計出錯
                valueHashSet.Add(schema.Name);
            }

            // 遍歷持有的存取權限
            foreach (ActiveDirectoryAccessRule accessRule in entry.ObjectSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                // 識別字串就是 SID, 因為使用 SecurityIdentifier 的類型去取得資料
                string SID = accessRule.IdentityReference.ToString();
                // 此 SID 尚未推入過字典
                if (!dictionarySIDWithPermissions.TryGetValue(SID, out List<AccessRuleInformation> storedList))
                {
                    // 重新宣告用以儲存的列表
                    storedList = new List<AccessRuleInformation>();
                    // 推入字典儲存
                    dictionarySIDWithPermissions.Add(SID, storedList);
                }

                // 是否對外提供
                bool isOfferable;
                // 查看繼承方式決定是否對外提供
                switch (accessRule.InheritanceType)
                {
                    // 僅包含自己
                    case ActiveDirectorySecurityInheritance.None:
                        {
                            /* 若 AD 系統正確運作, 發生繼承時此狀趟應只影響持有最原始權限的物件
                                 - 若此權限從繼承而來, 則不對外轉換
                            */
                            isOfferable = !accessRule.IsInherited;
                        }
                        break;
                    case ActiveDirectorySecurityInheritance.SelfAndChildren: // 包含自己與直接子系物件
                    case ActiveDirectorySecurityInheritance.All:             // 包含自己與所有子系物件
                        {
                            // 若 AD 系統正確運作, 發生繼承時此狀趟應會影響各自應影響的範圍
                            isOfferable = true;
                        }
                        break;
                    case ActiveDirectorySecurityInheritance.Children:    // 僅包含直接子系物件
                    case ActiveDirectorySecurityInheritance.Descendents: // 包含所有子系物件
                        {
                            /* 若 AD 系統正確運作, 發生繼承時此狀趟應只影響持有繼承權限的物件
                                 - 若此權限從繼承而來, 則對外轉換
                            */
                            isOfferable = accessRule.IsInherited;
                        }
                        break;
                    // 其他的預設狀態
                    default:
                        {
                            // 丟出例外: 因為此狀態沒有實作
                            throw new LDAPExceptions($"存取規則:{accessRule.IdentityReference} 設定物件時發現未實作的繼承狀態:{accessRule.InheritanceType} 因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                        }
                }

                // 不可對外提供
                if (!isOfferable)
                {
                    // 跳過所有處理提供空物件
                    continue;
                }

                // 取得目標藍本描述
                string name = configuration.FindName(accessRule.ObjectType);
                // 取得此名稱對應的屬性關聯
                dictionaryNameWithPropertySet.TryGetValue(name, out HashSet<string> propertySet);
                // 推入此單位的存取權限
                storedList.Add(new AccessRuleInformation(name, propertySet, accessRule));
            }
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <typeparam name="T">樣板</typeparam>
        /// <param name="propertyName">特性參數</param>
        /// <param name="convertedValue">實際資料</param>
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        internal bool GetPropertySingle<T>(in string propertyName, out T convertedValue)
        {
            // 預設
            convertedValue = default(T);
            // 嘗試取得內容
            if(!dictionaryNameWithPorpertyDetail.TryGetValue(propertyName.ToLower(), out PropertyDetail detail))
            {
                // 不存在時不處理
                return false;
            }

            // 要求必須是單一數值, 此時對愛丟出例外
            if (!detail.IsSingleValue)
            {
                // 丟出例外: 因為此狀態沒有實作
                throw new LDAPExceptions($"屬性:{propertyName} 應使用陣列方式進行轉換, 因使用了錯誤方法因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 轉換
            convertedValue = (T)detail.PropertyValue[0];
            // 提供查詢結果
            return true;
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <param name="propertyName">特性參數</param>
        /// <param name="convertedValue">實際資料</param>
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        internal bool GetPropertyInterval(in string propertyName, out long convertedValue)
        {
            // 預設
            convertedValue = 0;
            // 嘗試取得內容
            if (!dictionaryNameWithPorpertyDetail.TryGetValue(propertyName.ToLower(), out PropertyDetail detail))
            {
                // 不存在時不處理
                return false;
            }

            // 要求必須是單一數值, 此時對愛丟出例外
            if (!detail.IsSingleValue)
            {
                // 丟出例外: 因為此狀態沒有實作
                throw new LDAPExceptions($"屬性:{propertyName} 應使用陣列方式進行轉換, 因使用了錯誤方法因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }
            // 本次需處理資料
            object value = detail.PropertyValue[0];
            // 取得類型
            Type type = value.GetType();
            // 取得高位元
            int highPart = (int)type.InvokeMember("HighPart", BindingFlags.GetProperty, null, value, null);
            // 取得低位元
            int lowPart  = (int)type.InvokeMember("LowPart", BindingFlags.GetProperty, null, value, null);
            // 疊加作為結果
            convertedValue = (long)highPart << 32 | (uint)lowPart;
            // 提供查詢結果
            return true;
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <param name="propertyName">特性參數</param>
        /// <param name="convertedValue">實際資料</param>
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        internal bool GetPropertySID(in string propertyName, out string convertedValue)
        {
            // 預設
            convertedValue = string.Empty;
            // 嘗試取得內容
            if (!dictionaryNameWithPorpertyDetail.TryGetValue(propertyName.ToLower(), out PropertyDetail detail))
            {
                // 不存在時不處理
                return false;
            }

            // 要求必須是單一數值, 此時對愛丟出例外
            if (!detail.IsSingleValue)
            {
                // 丟出例外: 因為此狀態沒有實作
                throw new LDAPExceptions($"屬性:{propertyName} 應使用陣列方式進行轉換, 因使用了錯誤方法因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            SecurityIdentifier securityIdentifier = new SecurityIdentifier((byte[])detail.PropertyValue[0], 0);
            // 轉換
            convertedValue = securityIdentifier.ToString();
            // 提供查詢結果
            return true;
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <param name="propertyName">特性參數</param>
        /// <param name="convertedValue">實際資料</param>
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        internal bool GetPropertyGUID(in string propertyName, out string convertedValue)
        {
            // 預設
            convertedValue = string.Empty;
            // 嘗試取得內容
            if (!dictionaryNameWithPorpertyDetail.TryGetValue(propertyName.ToLower(), out PropertyDetail detail))
            {
                // 不存在時不處理
                return false;
            }

            // 要求必須是單一數值, 此時對愛丟出例外
            if (!detail.IsSingleValue)
            {
                // 丟出例外: 因為此狀態沒有實作
                throw new LDAPExceptions($"屬性:{propertyName} 應使用陣列方式進行轉換, 因使用了錯誤方法因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            Guid guid = new Guid((byte[])detail.PropertyValue[0]);
            // 轉換
            convertedValue = guid.ToString("D");
            // 提供查詢結果
            return true;
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <typeparam name="T">樣板</typeparam>
        /// <param name="propertyName">特性參數</param>
        /// <param name="convertedValue">實際資料</param>
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        internal bool GetPropertyMultiple<T>(in string propertyName, out T[] convertedValue)
        {
            // 預設
            convertedValue = Array.Empty<T>();
            // 嘗試取得內容
            if (!dictionaryNameWithPorpertyDetail.TryGetValue(propertyName.ToLower(), out PropertyDetail detail))
            {
                // 不存在時不處理
                return false;
            }

            // 要求必須是單一數值, 此時對愛丟出例外
            if (detail.IsSingleValue)
            {
                // 丟出例外: 因為此狀態沒有實作
                throw new LDAPExceptions($"屬性:{propertyName} 應使用陣列方式進行轉換, 因使用了錯誤方法因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 轉換
            convertedValue = Array.ConvertAll(detail.PropertyValue, converted => (T)converted);
            // 提供查詢結果
            return true;
        }

        /// <summary>
        /// 使用指定群組的 SID 取得所有支援的屬性
        /// </summary>
        /// <param name="limitedSID">群組 SID</param>
        /// <returns>這些群組對應到的權限</returns>
        internal AccessRuleInformation[] GetAccessRuleInformations(in string limitedSID)
        {
            // 取得 SID 關聯存取規則
            dictionarySIDWithPermissions.TryGetValue(limitedSID, out List<AccessRuleInformation> accessRuleInformations);
            // 對外提供資歷
            return accessRuleInformations?.ToArray();
        }
    }
}
