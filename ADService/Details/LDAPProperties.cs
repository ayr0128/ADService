using ADService.Configuration;
using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Reflection;
using System.Security.Principal;

namespace ADService.Details
{
    /// <summary>
    /// 從入口物件取得的資料
    /// </summary>
    internal class PropertyDetail
    {
        /// <summary>
        /// 是否為單一數值
        /// </summary>
        internal bool IsSingleValue;
        /// <summary>
        /// 從入口物件取得的資料數值
        /// </summary>
        internal object PropertyValue;
        /// <summary>
        /// 數據大小
        /// </summary>
        internal int SizeOf;

        /// <summary>
        /// 建構藍本物件如何解析
        /// </summary>
        /// <param name="schema">藍本物件</param>
        /// <param name="property">入口物件儲存資料</param>
        internal PropertyDetail(in UnitSchema schema, in PropertyValueCollection property)
        {
            PropertyValue = property.Value;
            SizeOf = property.Count;

            IsSingleValue = schema.IsSingleValued;
        }
    }

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
        /// 以 SID 記錄各條存取權限
        /// </summary>
        private readonly Dictionary<string, List<AccessRuleInformation>> dictionarySIDWithPermissions = new Dictionary<string, List<AccessRuleInformation>>();

        /// <summary>
        /// 允許使用的屬性
        /// </summary>
        private readonly HashSet<string> AllowedAttributes;

        /// <summary>
        /// 建構特性儲存與分析類別
        /// </summary>
        /// <param name="configuration">藍本物件</param>
        /// <param name="entry">入口物件</param>
        /// <param name="propertiesResult">透過找尋取得字的屬性</param>
        internal LDAPProperties(in LDAPConfiguration configuration, in DirectoryEntry entry, in ResultPropertyCollection propertiesResult)
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
                dictionaryNameWithPorpertyDetail.Add(schema.Name, propertyDetail);
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

            // 將允許團入屬性改為陣列
            string[] allowedAttributes = LDAPEntries.ParseMutipleValue<string>(Properties.C_ALLOWEDATTRIBUTES, propertiesResult);
            // 轉換成可比對的屬性參數
            AllowedAttributes = new HashSet<string>(allowedAttributes);

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
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        internal T GetPropertySingle<T>(in string propertyName)
        {
            // 查詢的資料鍵值須為小姐
            string nameLower = propertyName.ToLower();
            // 嘗試取得內容
            bool isAllowed = GetProperty(nameLower, out PropertyDetail detail);
            /* 下述任意情況出現時對外丟出例外
                 1. 不支援且資料不存在
                 2. 資料是單一述值十
            */
            if ((!isAllowed && detail == null) ||
                (detail != null && !detail.IsSingleValue))
            {
                // 丟出例外: 因為此狀態沒有實作
                throw new LDAPExceptions($"屬性:{propertyName} 應使用陣列方式進行轉換, 因使用了錯誤方法因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 提供查詢結果
            return detail == null ? default(T) : (T)detail.PropertyValue;
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <param name="propertyName">特性參數</param>
        /// <param name="convertedValue">實際資料</param>
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        internal long GetPropertyInterval(in string propertyName)
        {
            // 查詢的資料鍵值須為小姐
            string nameLower = propertyName.ToLower();
            // 嘗試取得內容
            bool isAllowed = GetProperty(nameLower, out PropertyDetail detail);
            /* 下述任意情況出現時對外丟出例外
                 1. 不支援且資料不存在
                 2. 資料是單一述值十
            */
            if ((!isAllowed && detail == null) ||
                (detail != null && !detail.IsSingleValue))
            {
                // 丟出例外: 因為此狀態沒有實作
                throw new LDAPExceptions($"屬性:{propertyName} 應使用陣列方式進行轉換, 因使用了錯誤方法因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 資料不存在
            if (detail == null)
            {
                // 提供 0
                return 0;
            }

            // 取得類型
            Type type = detail.PropertyValue.GetType();
            // 取得高位元
            int highPart = (int)type.InvokeMember("HighPart", BindingFlags.GetProperty, null, detail.PropertyValue, null);
            // 取得低位元
            int lowPart  = (int)type.InvokeMember("LowPart", BindingFlags.GetProperty, null, detail.PropertyValue, null);
            // 提供查詢結果
            return (long)highPart << 32 | (uint)lowPart;
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <param name="propertyName">特性參數</param>
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        internal string GetPropertySID(in string propertyName)
        {
            // 查詢的資料鍵值須為小姐
            string nameLower = propertyName.ToLower();
            // 嘗試取得內容
            bool isAllowed = GetProperty(nameLower, out PropertyDetail detail);
            /* 下述任意情況出現時對外丟出例外
                 1. 不支援且資料不存在
                 2. 資料是單一述值十
            */
            if ((!isAllowed && detail == null) ||
                (detail != null && !detail.IsSingleValue))
            {
                // 丟出例外: 因為此狀態沒有實作
                throw new LDAPExceptions($"屬性:{propertyName} 應使用陣列方式進行轉換, 因使用了錯誤方法因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 轉換
            SecurityIdentifier securityIdentifier = detail == null ? new SecurityIdentifier(WellKnownSidType.NullSid, null) : new SecurityIdentifier((byte[])detail.PropertyValue, 0);
            // 提供查詢結果
            return securityIdentifier.ToString();
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <param name="propertyName">特性參數</param>
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        internal string GetPropertyGUID(in string propertyName)
        {
            // 查詢的資料鍵值須為小姐
            string nameLower = propertyName.ToLower();
            // 嘗試取得內容
            bool isAllowed = GetProperty(nameLower, out PropertyDetail detail);
            /* 下述任意情況出現時對外丟出例外
                 1. 不支援且資料不存在
                 2. 資料是單一述值十
            */
            if ((!isAllowed && detail == null) ||
                (detail != null && !detail.IsSingleValue))
            {
                // 丟出例外: 因為此狀態沒有實作
                throw new LDAPExceptions($"屬性:{propertyName} 應使用陣列方式進行轉換, 因使用了錯誤方法因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 轉換
            Guid guid = detail == null ? Guid.Empty : new Guid((byte[])detail.PropertyValue);
            // 提供查詢結果
            return guid.ToString("D");
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <typeparam name="T">樣板</typeparam>
        /// <param name="propertyName">特性參數</param>
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        internal T[] GetPropertyMultiple<T>(in string propertyName)
        {
            // 查詢的資料鍵值須為小姐
            string nameLower = propertyName.ToLower();
            // 嘗試取得內容
            bool isAllowed = GetProperty(nameLower, out PropertyDetail detail);
            /* 下述任意情況出現時對外丟出例外
                 1. 不支援且資料不存在
                 2. 資料是單一述值十
            */
            if ((!isAllowed && detail == null) ||
                (detail != null && detail.IsSingleValue))
            {
                // 丟出例外: 因為此狀態沒有實作
                throw new LDAPExceptions($"屬性:{propertyName} 應使用陣列方式進行轉換, 因使用了錯誤方法因而丟出例外, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 提供查詢結果
            return detail == null ? Array.Empty<T>() : Array.ConvertAll((object[])detail.PropertyValue, converted => (T)converted);
        }

        /// <summary>
        /// 提供指定屬性鍵值取得支援情況與實際儲存資料
        /// </summary>
        /// <param name="propertyName">指定屬性鍵值</param>
        /// <param name="detail">屬性鍵值目前儲存資料</param>
        /// <returns>是否支援</returns>
        internal bool GetProperty(in string propertyName, out PropertyDetail detail)
        {
            // 查詢的資料鍵值須為小姐
            string nameLower = propertyName.ToLower();
            // 取得資料
            dictionaryNameWithPorpertyDetail.TryGetValue(nameLower, out detail);
            // 是否支援: 支援與否和資料是否存在沒有直接關係
            return AllowedAttributes.Contains(nameLower);
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
