using ADService.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace ADService.Environments
{
    /// <summary>
    /// 紀錄物件類型相關
    /// </summary>
    public static class LDAPCategory
    {
        /// <summary>
        /// 網域跟目錄
        /// </summary>
        public const string CLASS_DOMAINDNS = "domainDNS";
        /// <summary>
        /// 容器
        /// </summary>
        public const string CLASS_CONTAINER = "container";
        /// <summary>
        /// 組織單位
        /// </summary>
        public const string CLASS_ORGANIZATIONUNIT = "organizationalUnit";
        /// <summary>
        /// 外部安全性主體
        /// </summary>
        public const string CLASS_FOREIGNSECURITYPRINCIPALS = "foreignSecurityPrincipals";
        /// <summary>
        /// 群組
        /// </summary>
        public const string CLASS_GROUP = "group";
        /// <summary>
        /// 使用者
        /// </summary>
        public const string CLASS_PERSON = "user";

        /// <summary>
        /// 使用指定物件類型名稱取得物件類型列舉旗標
        /// </summary>
        /// <param name="classNames">
        ///     物件類別, 參閱下述列表
        ///     <list type="table">
        ///         <item> <term><see cref="CLASS_CONTAINER">物件類型名稱:容器</see></term> 對照 <see cref="CategoryTypes.CONTAINER">物件類型旗標:容器</see> </item>
        ///         <item> <term><see cref="CLASS_ORGANIZATIONUNIT">物件類型名稱:組織單位</see></term> 對照 <see cref="CategoryTypes.ORGANIZATION_UNIT">物件類型旗標:組織單位</see> </item>
        ///         <item> <term><see cref="CLASS_FOREIGNSECURITYPRINCIPALS">物件類型名稱:外部安全性主體</see></term> 對照 <see cref="CategoryTypes.FOREIGN_SECURITYPRINCIPALS">物件類型旗標:外部安全性主體</see> </item>
        ///         <item> <term><see cref="CLASS_GROUP">物件類型名稱:群組</see></term> 對照 <see cref="CategoryTypes.GROUP">物件類型旗標:群組</see> </item>
        ///         <item> <term><see cref="CLASS_PERSON">物件類型名稱:人員</see></term> 對照 <see cref="CategoryTypes.PERSON">物件類型旗標:人員</see> </item>
        ///     </list>
        /// </param>
        /// <returns></returns>
        public static CategoryTypes GetCategoryTypes(params string[] classNames)
        {
            // 沒有指定任何項目, 全部找尋
            if (classNames.Length == 0)
            {
                // 返回支援的所有類型
                return CategoryTypes.ALL_TYPES;
            }

            // 需求的類型
            CategoryTypes wantCategoryTypes = CategoryTypes.NONE;
            // 遍立指定的物件類型名稱
            foreach(string className in classNames)
            {
                // 從做好的連接表內取得類型
                bool isExist = dictionaryClassNameWithCategoryType.TryGetValue(className, out CategoryTypes categoryTypes);
                // 檢查是否支援
                if (!isExist)
                {
                    // 不支援則跳過
                    continue;
                }

                // 蝶家齊標誌希望項目中: 此動作保證了指定項目依定能提供
                wantCategoryTypes |= categoryTypes;
            }
            // 最後對外提供的時候
            return wantCategoryTypes;
        }

        /// <summary>
        /// 使用指定物件類型名稱取得物件類型列舉旗標
        /// </summary>
        /// <param name="categoryTypes">物件類別, </param>
        /// <returns></returns>
        public static string[] GetClassNames(in CategoryTypes categoryTypes)
        {
            // 沒有指定任何項目, 全部找尋
            if (categoryTypes == CategoryTypes.NONE)
            {
                // 返回支援的所有類型
                return dictionaryClassNameWithCategoryType.Keys.ToArray();
            }

            // 串蒐集陣列
            List<string> classNames = new List<string>();
            // 遍立指定的物件類型名稱
            foreach (KeyValuePair<string, CategoryTypes> pair in dictionaryClassNameWithCategoryType)
            {
                // 檢查是否支援
                if ((pair.Value & categoryTypes) == CategoryTypes.NONE)
                {
                    // 不支援則跳過
                    continue;
                }

                // 蝶家齊標誌希望項目中: 此動作保證了指定項目依定能提供
                classNames.Add(pair.Key);
            }
            // 最後對外提供的時候: 此動作保證了未提供任何可用類別時會自動取用所有可用類別
            return classNames.ToArray();
        }

        /// <summary>
        /// 列舉與物件類型描述的字典檔
        /// </summary>
        private static readonly Dictionary<string, CategoryTypes> dictionaryClassNameWithCategoryType = new Dictionary<string, CategoryTypes>
        {
            { CLASS_DOMAINDNS, CategoryTypes.DOMAIN_DNS },
            { CLASS_CONTAINER, CategoryTypes.CONTAINER },
            { CLASS_ORGANIZATIONUNIT, CategoryTypes.ORGANIZATION_UNIT },
            { CLASS_FOREIGNSECURITYPRINCIPALS, CategoryTypes.FOREIGN_SECURITYPRINCIPALS },
            { CLASS_GROUP, CategoryTypes.GROUP },
            { CLASS_PERSON, CategoryTypes.PERSON },
        };
    }
}
