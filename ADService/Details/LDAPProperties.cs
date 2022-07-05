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
    /// 使用 JSON 做為基底的特性鍵值轉換器
    /// </summary>
    public abstract class LDAPProperties
    {
        #region 取得所有設置參數
        /// <summary>
        /// 目前儲存的特性鍵值資料
        /// </summary>
        internal Dictionary<string, PropertyDetail> dictionaryNameWithPropertyDetail = new Dictionary<string, PropertyDetail>();

        /// <summary>
        /// 整合屬性值轉換功能
        /// </summary>
        /// <param name="dispatcher">藍本物件</param>
        /// <param name="properties">入口物件持有的屬性</param>
        /// <returns>支援的屬性</returns>
        private static Dictionary<string, PropertyDetail> ParseProperties(in LDAPConfigurationDispatcher dispatcher, in PropertyCollection properties)
        {
            // 預計對外提供的項目
            Dictionary<string, PropertyDetail> dictionaryNameWithPropertyDetail = new Dictionary<string, PropertyDetail>(properties.Count);
            // 遍歷屬性找到屬性的
            foreach (PropertyValueCollection value in properties)
            {
                // 取得目標藍本描述: 內部將戰存屬性格式
                UnitSchemaAttribute[] unitSchemaAttributes = dispatcher.GetUnitSchemaAttribute(value.PropertyName);
                // 若無法取得則跳過處理
                if (unitSchemaAttributes.Length == 0)
                {
                    // 拋出例外: 此部分數值是由 AD 設置產生, 因此退外提供伺服器錯誤
                    throw new LDAPExceptions($"物件:{value.PropertyName} 於解析過程中發現不是屬性卻被設置為屬性成員, 請聯絡程式維護人員", ErrorCodes.SERVER_ERROR);
                }

                // 宣告實體資料
                PropertyDetail propertyDetail = new PropertyDetail(value, unitSchemaAttributes[0].IsSingleValued);
                // 整理屬性
                dictionaryNameWithPropertyDetail.Add(unitSchemaAttributes[0].Name, propertyDetail);
            }
            // 轉換後對外提供項目
            return dictionaryNameWithPropertyDetail;
        }
        #endregion

        #region 轉換存取權限
        /// <summary>
        /// 以 SID 記錄各條存取權限
        /// </summary>
        internal Dictionary<string, List<AccessRuleConverted>> dictionarySIDWithAccessRuleConverteds = new Dictionary<string, List<AccessRuleConverted>>();

        /// <summary>
        /// 整合權限轉換功能
        /// </summary>
        /// <param name="securityAccessRule">入口物件持有的存取權限</param>
        /// <returns>案群組持有的權限</returns>
        private static Dictionary<string, List<AccessRuleConverted>> ParseSecurityAccessRule(in ActiveDirectorySecurity securityAccessRule)
        {
            // 預計對外提供的項目
            Dictionary<string, List<AccessRuleConverted>> dictionarySIDWithPermissions = new Dictionary<string, List<AccessRuleConverted>>();
            // 遍歷持有的存取權限
            foreach (ActiveDirectoryAccessRule accessRule in securityAccessRule.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                // 識別字串就是 SID, 因為使用 SecurityIdentifier 的類型去取得資料
                string SID = accessRule.IdentityReference.ToString();
                // 此 SID 尚未推入過字典
                if (!dictionarySIDWithPermissions.TryGetValue(SID, out List<AccessRuleConverted> storedList))
                {
                    // 重新宣告用以儲存的列表
                    storedList = new List<AccessRuleConverted>();
                    // 推入字典儲存
                    dictionarySIDWithPermissions.Add(SID, storedList);
                }

                AccessRuleConverted accessRuleInformation = new AccessRuleConverted(accessRule);
                // 推入此單位的存取權限
                storedList.Add(accessRuleInformation);
            }
            // 轉換後對外提供項目
            return dictionarySIDWithPermissions;
        }
        #endregion

        /// <summary>
        /// 建構特性儲存與分析類別
        /// </summary>
        /// <param name="dispatcher">藍本物件</param>
        /// <param name="entry">入口物件</param>
        /// <param name="propertiesResult">透過找尋取得字的屬性</param>
        internal LDAPProperties(in LDAPConfigurationDispatcher dispatcher, in DirectoryEntry entry)
        {
            dictionaryNameWithPropertyDetail = ParseProperties(dispatcher, entry.Properties);
            dictionarySIDWithAccessRuleConverteds = ParseSecurityAccessRule(entry.ObjectSecurity);
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
            // 取得資料: 此處僅能取得已存在資料的欄位
            dictionaryNameWithPropertyDetail.TryGetValue(propertyName, out PropertyDetail detail);
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
            // 取得資料: 此處僅能取得已存在資料的欄位
            dictionaryNameWithPropertyDetail.TryGetValue(propertyName, out PropertyDetail detail);
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
            int lowPart = (int)type.InvokeMember("LowPart", BindingFlags.GetProperty, null, detail.PropertyValue, null);
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
            // 取得資料: 此處僅能取得已存在資料的欄位
            dictionaryNameWithPropertyDetail.TryGetValue(propertyName, out PropertyDetail detail);
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
            // 取得資料: 此處僅能取得已存在資料的欄位
            dictionaryNameWithPropertyDetail.TryGetValue(propertyName, out PropertyDetail detail);
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
            // 取得資料: 此處僅能取得已存在資料的欄位
            dictionaryNameWithPropertyDetail.TryGetValue(propertyName, out PropertyDetail detail);
            // 提供查詢結果
            if (detail == null || detail.SizeOf == 0)
            {
                return Array.Empty<T>();
            }

            // 只有一個時需慘用特例處理
            return detail.SizeOf == 1 ? new T[] { (T)detail.PropertyValue } : Array.ConvertAll((object[])detail.PropertyValue, converted => (T)converted);
        }

        /// <summary>
        /// 使用指定群組的 SID 取得所有支援的屬性
        /// </summary>
        /// <param name="securitySIDs">可套用的安全性群組 SID</param>
        /// <returns>這些群組對應到的權限</returns>
        internal AccessRuleConverted[] GetAccessRuleConverteds(in IEnumerable<string> securitySIDs)
        {
            // 總長度尚未確定
            List<AccessRuleConverted> accessRuleConvertedsResult = new List<AccessRuleConverted>();
            // 遍歷所有可使用的安全性群組 SID
            foreach (string securitySID in securitySIDs)
            {
                // 取得安全性群組 SID 關聯的權限
                if (!dictionarySIDWithAccessRuleConverteds.TryGetValue(securitySID, out List<AccessRuleConverted> accessRuleConverteds))
                {
                    // 無法找到則跳過
                    continue;
                }

                // 加入特定 SID 所持有的存取權限 (包含沒有生效的)
                accessRuleConvertedsResult.AddRange(accessRuleConverteds);
            }
            // 將找尋到的所有資料對外提供 (包含沒有生效的)
            return accessRuleConvertedsResult.ToArray();
        }
    }
}
