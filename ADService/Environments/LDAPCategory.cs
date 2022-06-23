using ADService.Protocol;
using System.Collections.Generic;

namespace ADService.Environments
{
    /// <summary>
    /// 紀錄物件類型相關
    /// </summary>
    public static class LDAPCategory
    {
        /// <summary>
        /// 使用物件類型列舉快速取得描述
        /// </summary>
        /// <param name="type">物件類型</param>
        /// <returns>對應描述</returns>
        public static Dictionary<CategoryTypes, string> GetValuesByTypes(CategoryTypes type)
        {
            // 最後提供給外部的對應資料: 以要求的鍵值為主
            Dictionary<CategoryTypes, string> resultDictionary = new Dictionary<CategoryTypes, string>();
            // 遍歷支援的類型取得相關描述
            foreach (KeyValuePair<CategoryTypes, string> pairEnumWithValue in dictionaryEnumWithValue)
            {
                // 使用 & 運算換算出旗標
                CategoryTypes flag = type & pairEnumWithValue.Key;
                // 不是支援旗標
                if (flag == CategoryTypes.NONE)
                {
                    // 跳過
                    continue;
                }

                // 將支援旗標與相關描述對外提供
                resultDictionary.Add(pairEnumWithValue.Key, pairEnumWithValue.Value);
            }

            // 對外提供描述
            return resultDictionary;
        }

        /// <summary>
        /// 使用物件類型列舉快速取得描述
        /// </summary>
        /// <param name="type">物件類型</param>
        /// <returns>對應描述</returns>
        public static Dictionary<CategoryTypes, string> GetAccessRulesByTypes(CategoryTypes type)
        {
            // 最後提供給外部的對應資料: 以要求的鍵值為主
            Dictionary<CategoryTypes, string> resultDictionary = new Dictionary<CategoryTypes, string>();
            // 遍歷支援的類型取得相關描述
            foreach (KeyValuePair<CategoryTypes, string> pairEnumWithValue in dictionaryEnumWithAccessRule)
            {
                // 使用 & 運算換算出旗標
                CategoryTypes flag = type & pairEnumWithValue.Key;
                // 不是支援旗標
                if (flag == CategoryTypes.NONE)
                {
                    // 跳過
                    continue;
                }

                // 將支援旗標與相關描述對外提供
                resultDictionary.Add(pairEnumWithValue.Key, pairEnumWithValue.Value);
            }

            // 對外提供描述
            return resultDictionary;
        }

        /// <summary>
        /// 使用描述快速取得物件類型列舉
        /// </summary>
        /// <param name="categories">物件描述</param>
        /// <returns>對應列舉</returns>
        public static Dictionary<string, CategoryTypes> GetTypeByCategories(params string[] categories)
        {
            // 最後提供給外部的對應資料: 以要求的描述為鍵值
            Dictionary<string, CategoryTypes> resultDictionary = new Dictionary<string, CategoryTypes>();
            // 遍歷需求並將資料做不重複處理
            foreach (string category in categories)
            {
                // 相關類型的描述不存在對應的物件類型
                if (!dictionaryCategoryWithEnum.TryGetValue(category, out CategoryTypes type))
                {
                    // 跳過
                    continue;
                }

                // 若已經處理過此描述
                if (resultDictionary.ContainsKey(category))
                {
                    // 跳過
                    continue;
                }

                // 提供對應的型態
                resultDictionary.Add(category, type);
            }

            // 對外提供型態
            return resultDictionary;
        }

        /// <summary>
        /// 列舉與物件類型描述的字典檔
        /// </summary>
        private static readonly Dictionary<CategoryTypes, string> dictionaryEnumWithValue = new Dictionary<CategoryTypes, string>
        {
            { CategoryTypes.CONTAINER, "container" },
            { CategoryTypes.DOMAIN_DNS, "domainDns" },
            { CategoryTypes.ORGANIZATION_UNIT, "organizationalUnit" },
            { CategoryTypes.ForeignSecurityPrincipals, "foreignSecurityPrincipals" },
            { CategoryTypes.GROUP, "group" },
            { CategoryTypes.PERSON, "person" },
        };

        /// <summary>
        /// 列舉與物件類型描述的字典檔
        /// </summary>
        private static readonly Dictionary<CategoryTypes, string> dictionaryEnumWithAccessRule = new Dictionary<CategoryTypes, string>
        {
            { CategoryTypes.CONTAINER, "container" },
            { CategoryTypes.DOMAIN_DNS, "domainDns" },
            { CategoryTypes.ORGANIZATION_UNIT, "organizationalUnit" },
            { CategoryTypes.ForeignSecurityPrincipals, "foreignSecurityPrincipals" },
            { CategoryTypes.GROUP, "group" },
            { CategoryTypes.PERSON, "user" },
        };

        /// <summary>
        /// 物件類型描述與列舉的字典檔
        /// </summary>
        private static readonly Dictionary<string, CategoryTypes> dictionaryCategoryWithEnum = new Dictionary<string, CategoryTypes>
        {
            { "Container", CategoryTypes.CONTAINER },
            { "Domain-DNS", CategoryTypes.DOMAIN_DNS },
            { "Organizational-Unit", CategoryTypes.ORGANIZATION_UNIT },
            { "Foreign-Security-Principal", CategoryTypes.ForeignSecurityPrincipals },
            { "Group", CategoryTypes.GROUP },
            { "Person", CategoryTypes.PERSON },
        };
    }
}
