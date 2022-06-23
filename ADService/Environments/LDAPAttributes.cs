using ADService.Protocol;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;

namespace ADService.Environments
{
    /// <summary>
    /// 所有支援的特性鍵值
    /// </summary>
    public static class LDAPAttributes
    {
        #region 過濾字串集成
        /// <summary>
        /// 組成找尋指定區分名稱的過濾字串
        /// </summary>
        /// <param name="distinguishedNames">限制區分名稱</param>
        /// <returns>過濾用字串, 沒有任何區分名稱需指定會提供空字串</returns>
        internal static string GetOneOfDNFiliter(params string[] distinguishedNames)
        {
            // 如果沒有任何區分名稱則不必進行過濾字串組合
            if (distinguishedNames.Length == 0)
            {
                // 返回空字串讓外部跳過搜尋動作
                return string.Empty;
            }

            // 區分名稱的過濾內容: 此時會缺失開頭與結尾的部分
            string distinguishedNamesSubFiliter = string.Join($")({C_DISTINGGUISHEDNAME}=", distinguishedNames);
            // 組成找尋任意一個與指定區分名稱相符的過濾字串
            return $"(|({C_DISTINGGUISHEDNAME}={distinguishedNamesSubFiliter}))";
        }

        /// <summary>
        /// 組成找尋指定物件類型的過濾字串
        /// </summary>
        /// <param name="categories">限制區分名稱</param>
        /// <returns>過濾用字串, 沒有任何區分名稱需指定會提供空字串</returns>
        internal static string GetOneOfCategoryFiliter(in CategoryTypes categories)
        {
            // 找到須限制的物件類型
            Dictionary<CategoryTypes, string> dictionaryLimitedCategory = LDAPCategory.GetValuesByTypes(categories);
            // 如果沒有過濾類型則不必進行過濾字串組合
            if (dictionaryLimitedCategory.Count == 0)
            {
                // 返回空字串讓外部跳過搜尋動作
                return string.Empty;
            }

            // 物件類型的過濾內容: 外部須注意不得提供
            string categoriesSubFiliter = string.Join($")({C_OBJECTCATEGORY}=", dictionaryLimitedCategory.Values);
            // 組成找尋任意一個與指定區分名稱相符的過濾字串
            return $"(|({C_OBJECTCATEGORY}={categoriesSubFiliter}))";
        }
        #endregion

        #region 解析 LDAP 鍵值
        /// <summary>
        /// 解析目標鍵值
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="forceExist">資料是否必須存在</param>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static T ParseSingleValue<T>(in string propertyName, in bool forceExist, in PropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            PropertyValueCollection collection = properties[propertyName];
            // 不存在資料且強迫必須存在時
            if ((collection == null || collection.Count == 0) && forceExist)
            {
                // 此特性鍵值必須存在因而丟出例外
                throw new LDAPExceptions($"解析資訊:{propertyName} 儲存的內容時, 內容長度不如預期", ErrorCodes.LOGIC_ERROR);
            }

            // 轉換至指定型別
            T converted = collection == null ? default : (T)collection.Value;
            // 對外提供轉換後型別
            return converted;
        }
        /// <summary>
        /// 解析目標鍵值
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="forceExist">資料是否必須存在</param>
        /// <param name="properties">搜尋的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static T ParseSingleValue<T>(in string propertyName, in bool forceExist, in ResultPropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            ResultPropertyValueCollection collection = properties[propertyName];
            // 不存在資料且強迫必須存在時
            if ((collection == null || collection.Count == 0) && forceExist)
            {
                // 此特性鍵值必須存在因而丟出例外
                throw new LDAPExceptions($"解析資訊:{propertyName} 儲存的內容時, 內容長度不如預期", ErrorCodes.LOGIC_ERROR);
            }

            // 轉換至指定型別
            T converted = collection == null ? default : (T)collection[0];
            // 對外提供轉換後型別
            return converted;
        }

        /// <summary>
        /// 解析目標鍵值, 預期格式是 SID
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="forceExist">資料是否必須存在</param>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static string ParseSID(in string propertyName, in bool forceExist, in PropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            byte[] valueBytes = ParseSingleValue<byte[]>(propertyName, forceExist, properties);
            // 不存在資料且強迫必須存在時
            if ((valueBytes == null || valueBytes.Length == 0) && forceExist)
            {
                // 此特性鍵值必須存在因而丟出例外
                throw new LDAPExceptions($"解析資訊:{propertyName} 儲存的內容時, 內容長度不如預期", ErrorCodes.LOGIC_ERROR);
            }

            // 對外提供的 SID 轉換器
            SecurityIdentifier convertor;
            // 資料為空
            if (valueBytes == null || valueBytes.Length == 0)
            {
                // 退外提供空的 SID
                convertor = new SecurityIdentifier(WellKnownSidType.NullSid, null);
            }
            else
            {
                // 需要透過 SecurityIdentifier 轉換成對應字串
                convertor = new SecurityIdentifier(valueBytes, 0);
            }
            // 對外提供轉換後型別
            return convertor.ToString();
        }

        /// <summary>
        /// 解析目標鍵值, 預期格式是 GUID
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="forceExist">資料是否必須存在</param>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static string ParseGUID(in string propertyName, in bool forceExist, in PropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            byte[] valueBytes = ParseSingleValue<byte[]>(propertyName, forceExist, properties);
            // 不存在資料且強迫必須存在時
            if ((valueBytes == null || valueBytes.Length == 0) && forceExist)
            {
                // 此特性鍵值必須存在因而丟出例外
                throw new LDAPExceptions($"解析資訊:{propertyName} 儲存的內容時, 內容長度不如預期", ErrorCodes.LOGIC_ERROR);
            }

            // 對外提供的 SID 轉換器
            Guid convertor;
            // 資料為空
            if (valueBytes == null || valueBytes.Length == 0)
            {
                // 退外提供空的 SID
                convertor = Guid.Empty;
            }
            else
            {
                // 需要透過 SecurityIdentifier 轉換成對應字串
                convertor = new Guid(valueBytes);
            }
            // 對外提供轉換後型別
            return convertor.ToString("D");
        }
        /// <summary>
        /// 解析目標鍵值, 預期格式是 GUID
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="forceExist">資料是否必須存在</param>
        /// <param name="properties">整包的搜尋鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static string ParseGUID(in string propertyName, in bool forceExist, in ResultPropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            byte[] valueBytes = ParseSingleValue<byte[]>(propertyName, forceExist, properties);
            // 不存在資料且強迫必須存在時
            if ((valueBytes == null || valueBytes.Length == 0) && forceExist)
            {
                // 此特性鍵值必須存在因而丟出例外
                throw new LDAPExceptions($"解析資訊:{propertyName} 儲存的內容時, 內容長度不如預期", ErrorCodes.LOGIC_ERROR);
            }

            // 對外提供的 SID 轉換器
            Guid convertor;
            // 資料為空
            if (valueBytes == null || valueBytes.Length == 0)
            {
                // 退外提供空的 SID
                convertor = Guid.Empty;
            }
            else
            {
                // 需要透過 SecurityIdentifier 轉換成對應字串
                convertor = new Guid(valueBytes);
            }
            // 對外提供轉換後型別
            return convertor.ToString("D");
        }

        /// <summary>
        /// 解析目標鍵值
        /// </summary>
        /// <param name="propertyName">解析目標鍵值</param>
        /// <param name="forceExist">資料是否必須存在</param>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得目標資料內容</returns>
        internal static T[] ParseMultipleValue<T>(in string propertyName, in bool forceExist, in PropertyCollection properties)
        {
            // 取得指定特性鍵值內容
            PropertyValueCollection collection = properties[propertyName];
            // 不存在資料且強迫必須存在時
            if ((collection == null || collection.Count == 0) && forceExist)
            {
                // 此特性鍵值必須存在因而丟出例外
                throw new LDAPExceptions($"解析資訊:{propertyName} 儲存的內容時, 內容長度不如預期", ErrorCodes.LOGIC_ERROR);
            }

            // 轉換至指定型別
            T[] converted = collection == null ? Array.Empty<T>() : collection.Cast<T>().ToArray();
            // 對外提供轉換後型別
            return converted;
        }

        /// <summary>
        /// 解析 <see cref="C_OBJECTCATEGORY">物件類型</see> 的鍵值容器
        /// </summary>
        /// <param name="properties">整包的鍵值儲存容器, 直接傳入不於外部解析是為了避免漏修改</param>
        /// <returns>從容器中取得的 <see cref="C_OBJECTCATEGORY">物件類型</see> 鍵值內容</returns>
        internal static CategoryTypes ParseCategory(in PropertyCollection properties)
        {
            // 取得 '物件類型' 特性鍵值內容
            string categoryDistinguishedName = ParseSingleValue<string>(C_OBJECTCATEGORY, true, properties);
            // 解析取得的區分名稱來得到物件類型
            return GetObjectType(categoryDistinguishedName);
        }
        /// <summary>
        /// 根據解析物件類型區分名稱來提供物件為何種類型
        /// </summary>
        /// <param name="categoryDistinguishedName">需解析區分名稱</param>
        /// <returns></returns>
        /// <exception cref="LDAPExceptions">區分名稱無法正常解析實對外丟出</exception>
        internal static CategoryTypes GetObjectType(in string categoryDistinguishedName)
        {
            // 用來切割物件類型的字串
            string[] splitElements = new string[] { $"{P_DC.ToUpper()}=", $"{P_OU.ToUpper()}=", $"{P_CN.ToUpper()}=" };
            // 切割物件類型
            string[] elements = categoryDistinguishedName.Split(splitElements, StringSplitOptions.RemoveEmptyEntries);

            // 第一個參數為物件類型的描述: 但是需要物件類型的長度決定如何處理
            string category = string.Empty;
            // 物件類型長度不可能比 1 少, 但是為了防呆還是增加此邏輯判斷
            if (elements.Length >= 1)
            {
                // 第一個元素必定是物件類型
                string elementFirst = elements[0];
                // 如果物件類型字串解析後長度比 1 大, 則第一個元素後面會多一個 ',' 會需要被移除
                category = elements.Length > 1 ? elementFirst.Substring(0, elementFirst.Length - 1) : elementFirst;
            }

            // 透過描述取得物件類型描述
            Dictionary<string, CategoryTypes> result = LDAPCategory.GetTypeByCategories(category);
            // 物件類型不存在
            if (!result.TryGetValue(category, out CategoryTypes type))
            {
                // 對外丟出例外: 未實做邏輯錯誤
                throw new LDAPExceptions($"未實作解析資訊:{C_OBJECTCATEGORY} 儲存的內容:{categoryDistinguishedName}", ErrorCodes.LOGIC_ERROR);
            }
            // 存在時對外提供物件類型
            return type;
        }
        #endregion

        /// <summary>
        /// 搜尋物件時使用的特性鍵值
        /// </summary>
        internal static string[] PropertiesToLoad => new string[] { C_DISTINGGUISHEDNAME };

        /// <summary>
        /// 樹系路徑: 隨時可能被異動
        /// </summary>
        public const string C_DISTINGGUISHEDNAME = "distinguishedName";
        /// <summary>
        /// 物件類型
        /// </summary>
        public const string C_OBJECTCATEGORY = "objectCategory";
        /// <summary>
        /// 物件GUID: 可參考 <see href="https://en.wikipedia.org/wiki/Universally_unique_identifier">微基百科</see> 說明文件
        /// </summary>
        public const string C_OBJECTGUID = "objectGUID";
        /// <summary>
        /// 物件 SID
        /// </summary>
        public const string C_OBJECTSID = "objectSID";
        /// <summary>
        /// 主要隸屬群組: 只有成員與電腦持有
        /// </summary>
        public const string C_PRIMARYGROUPID = "primaryGroupID";
        /// <summary>
        /// 名稱
        /// </summary>
        public const string P_NAME = "name";
        /// <summary>
        /// 角色或容器名稱
        /// </summary>
        public const string P_CN = "cn";
        /// <summary>
        /// 組織名稱
        /// </summary>
        public const string P_OU = "ou";
        /// <summary>
        /// 根系名稱
        /// </summary>
        public const string P_DC = "dc";
        /// <summary>
        /// 對外顯示名稱
        /// </summary>
        public const string P_DISPLAYNAME = "displayName";
        /// <summary>
        /// 描述
        /// </summary>
        public const string P_DESCRIPTION = "description";
        /// <summary>
        /// 姓
        /// </summary>
        public const string P_SN = "sn";
        /// <summary>
        /// 名
        /// </summary>
        public const string P_GIVENNAME = "givenName";
        /// <summary>
        /// 英文縮寫
        /// </summary>
        public const string P_INITIALS = "initials";
        /// <summary>
        /// 持有成員: 只有組織才持有
        /// </summary>
        public const string P_MEMBER = "member";
        /// <summary>
        /// 隸屬組織: 組織與成員都持有
        /// </summary>
        public const string P_MEMBEROF = "memberOf";
        /// <summary>
        /// 成員控制旗標: 僅有成員持有
        /// </summary>
        public const string P_USERACCOUNTCONTROL = "userAccountControl";
        /// <summary>
        /// 密碼最後設置時間: 僅有成員持有
        /// </summary>
        public const string P_PWDLASTSET = "pwdLastSet";
        /// <summary>
        /// 密碼何時過期: 僅有成員持有
        /// </summary>
        public const string P_ACCOUNTEXPIRES = "accountExpires";
        /// <summary>
        /// 帳號何時鎖定: 僅有成員持有
        /// </summary>
        public const string P_LOCKOUTTIME = "lockoutTime";
        /// <summary>
        /// 通訊加密方式: 僅有成員持有
        /// </summary>
        public const string P_SUPPORTEDENCRYPTIONTYPES = "msDS-SupportedEncryptionTypes";

        /// <summary>
        /// 新增或移除成員內的方法: 額外權限
        /// </summary>
        public const string EX_A10EMEMBER = "Add/Remove self as member";
        /// <summary>
        /// 重置密碼: 額外權限, 直接作為右鍵方法使用
        /// </summary>
        public const string EX_CHANGEPASSWORD = "Change Password";
        /// <summary>
        /// 設置密碼: 額外權限, 直接作為右鍵方法使用
        /// </summary>
        public const string EX_RESETPASSWORD = "Reset Password";
    }
}
