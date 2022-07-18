using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Reflection;
using System.Security.AccessControl;
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
        internal AccessRuleSet[] accessRuleSets;

        /// <summary>
        /// 整合權限轉換功能
        /// </summary>
        /// <param name="entry">入口物件</param>
        /// <returns>案群組持有的權限</returns>
        private static AccessRuleSet[] ParseSecurityAccessRule(in DirectoryEntry entry)
        {
            // 取得喚起物件區分名稱
            string distinguishedName = LDAPConfiguration.ParseSingleValue<string>(Properties.C_DISTINGUISHEDNAME, entry.Properties);
            // 取得喚起物件名稱
            string name = LDAPConfiguration.ParseSingleValue<string>(Properties.P_NAME, entry.Properties);

            // 找到名稱的位置: 必定能找到
            int index = distinguishedName.IndexOf(name);
            // 切割字串取得目標所在的組織單位
            string parentDistinguishedName = distinguishedName.Substring(index + name.Length + 1);

            // 宣告用來儲存組合完成的存取規則表
            List<AccessRuleSet> accessRuleSets = new List<AccessRuleSet>();
            // 當入口物件的安全性持有繼承安全性時: 注意沒有終止條件需要靠內部判斷做跳出
            for (DirectoryEntry rootEntry = entry; ; rootEntry = rootEntry.Parent)
            {
                // 取得目前處理項目的安全性描述
                ActiveDirectorySecurity activeDirectorySecurity = rootEntry.ObjectSecurity;
                // 取得物件區分名稱
                string processDistinguishedName = LDAPConfiguration.ParseSingleValue<string>(Properties.C_DISTINGUISHEDNAME, rootEntry.Properties);

                // 由於根目錄 (DomainsDNS) 時不會跳轉道其父層所有必定是處理自己

                // 可接受的處理項目
                HashSet<ActiveDirectorySecurityInheritance> acceptedActiveDirectorySecurityInheritance;
                // 處理的項目是否為自己
                bool isSelf = processDistinguishedName == distinguishedName;
                // 處理的目標是否是自己
                if (isSelf)
                {
                    // 是自己時所有項目都可以列出
                    acceptedActiveDirectorySecurityInheritance = new HashSet<ActiveDirectorySecurityInheritance>()
                    {
                        ActiveDirectorySecurityInheritance.None,            // 自己
                        ActiveDirectorySecurityInheritance.All,             // 包含自己與所有子物件
                        ActiveDirectorySecurityInheritance.Descendents,     // 包含所有子系物件
                        ActiveDirectorySecurityInheritance.SelfAndChildren, // 包含自己與直接子系物件
                        ActiveDirectorySecurityInheritance.Children,        // 包含直接子系物件
                    };
                }
                // 處理的項目不是自己, 但是直接父層物件
                else if (processDistinguishedName == parentDistinguishedName)
                {
                    // 從直系父層繼承來來的項目部會包含自己
                    acceptedActiveDirectorySecurityInheritance = new HashSet<ActiveDirectorySecurityInheritance>()
                    {
                        ActiveDirectorySecurityInheritance.All,             // 包含自己與所有子物件
                        ActiveDirectorySecurityInheritance.Descendents,     // 包含所有子系物件
                        ActiveDirectorySecurityInheritance.SelfAndChildren, // 包含自己與直接子系物件
                        ActiveDirectorySecurityInheritance.Children,        // 包含直接子系物件
                    };
                }
                // 處理的項目不是自己亦不是直系父層物件, 那麼非直系父層物件
                else
                {
                    // 從非直系父層繼承來來的項目不包含直接子系物件
                    acceptedActiveDirectorySecurityInheritance = new HashSet<ActiveDirectorySecurityInheritance>()
                    {
                        ActiveDirectorySecurityInheritance.All,             // 包含自己與所有子物件
                        ActiveDirectorySecurityInheritance.Descendents,     // 包含所有子系物件
                    };
                }

                // 取得不包含繼承的集合
                AuthorizationRuleCollection authorizationRuleCollectionExceptInherited = activeDirectorySecurity.GetAccessRules(true, false, typeof(NTAccount));
                // 取得不繼承的集合
                foreach (ActiveDirectoryAccessRule activeDirectoryAccessRule in authorizationRuleCollectionExceptInherited)
                {
                    // 是否是可以處理的項目
                    if (!acceptedActiveDirectorySecurityInheritance.Contains(activeDirectoryAccessRule.InheritanceType))
                    {
                        // 不是則跳過處理
                        continue;
                    }

                    // 轉換成紀錄群組: 不是自己就是透過繼承取得
                    AccessRuleSet accessRuleSet = new AccessRuleSet(processDistinguishedName, !isSelf, activeDirectoryAccessRule);
                    // 推入陣列
                    accessRuleSets.Add(accessRuleSet);
                }

                // 取得包含繼承的集合
                AuthorizationRuleCollection authorizationRuleCollectionContainInherited = activeDirectorySecurity.GetAccessRules(true, true, typeof(NTAccount));
                /* 由於根目錄 (DomainsDNS) 時必定不持有繼承項目, 因此必定會在此處跳處
                   [TODO] 找到可以直接判斷是否啟用繼承的旗標
                */
                if (authorizationRuleCollectionContainInherited.Count == authorizationRuleCollectionExceptInherited.Count)
                {
                    // 由於不包含繼承與包含繼承的項目長度相同, 所以可以得知此入口務盡並沒有從父層而來的繼承項目
                    break;
                }

                // 若是進行至此則必定有透過父層項目繼承而來的存取權限, 所以可以透過 For 的結束動作進行父層替換
            }

            // 轉換後對外提供項目
            return accessRuleSets.ToArray();
        }
        #endregion

        /// <summary>
        /// 建構特性儲存與分析類別
        /// </summary>
        /// <param name="dispatcher">藍本物件</param>
        /// <param name="entry">入口物件</param>
        internal LDAPProperties(in LDAPConfigurationDispatcher dispatcher, in DirectoryEntry entry)
        {
            dictionaryNameWithPropertyDetail = ParseProperties(dispatcher, entry.Properties);
            accessRuleSets = ParseSecurityAccessRule(entry);
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <typeparam name="T">樣板</typeparam>
        /// <param name="propertyName">特性參數</param>
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        [Obsolete("先使用此版本取得對應資料, 會再提供更合適的版本")]
        public T GetPropertySingle<T>(in string propertyName)
        {
            // 取得資料: 此處僅能取得已存在資料的欄位
            dictionaryNameWithPropertyDetail.TryGetValue(propertyName, out PropertyDetail detail);
            // 提供查詢結果
            return detail == null ? default : (T)detail.PropertyValue;
        }

        /// <summary>
        /// 根據鍵值取得儲存內容
        /// </summary>
        /// <param name="propertyName">特性參數</param>
        /// <returns> 資料是否存在 </returns>
        /// <exception cref="LDAPExceptions">當內部儲存資料為多筆時將拋出例外</exception>
        [Obsolete("先使用此版本取得對應資料, 會再提供更合適的版本")]
        public long GetPropertyInterval(in string propertyName)
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
        [Obsolete("先使用此版本取得對應資料, 會再提供更合適的版本")]
        public string GetPropertySID(in string propertyName)
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
        [Obsolete("先使用此版本取得對應資料, 會再提供更合適的版本")]
        public string GetPropertyGUID(in string propertyName)
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
        [Obsolete("先使用此版本取得對應資料, 會再提供更合適的版本")]
        public T[] GetPropertyMultiple<T>(in string propertyName)
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
    }
}
