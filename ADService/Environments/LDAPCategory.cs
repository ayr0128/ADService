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
        /// 檢查是否為容器類的物件
        /// </summary>
        /// <param name="className">物件類型</param>
        /// <returns>是否為容器</returns>
        public static bool IsContainer(in string className) => containerHashSet.Contains(className);

        /// <summary>
        /// 將容器類型集合成一組檢查表
        /// </summary>
        private static HashSet<string> containerHashSet = new HashSet<string>()
        {
            CLASS_DOMAINDNS,
            CLASS_CONTAINER,
            CLASS_ORGANIZATIONUNIT,
            CLASS_APTREE,
        };

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
        /// 應用程式或功能
        /// </summary>
        public const string CLASS_APTREE = "aptree";

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
        ///         <item> <term><see cref="CLASS_APTREE">物件類型名稱:人員</see></term> 對照 <see cref="CategoryTypes.APTREE">物件類型旗標:人員</see> </item>
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
            foreach (string className in classNames)
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
        /// 檢查提供的物件類型並過濾出可用的部分
        /// </summary>
        /// <param name="classNames">
        ///     物件類別, 參閱下述列表
        ///     <list type="table">
        ///         <item> <see cref="CLASS_CONTAINER">物件類型名稱:容器</see> </item>
        ///         <item> <see cref="CLASS_ORGANIZATIONUNIT">物件類型名稱:組織單位</see> </item>
        ///         <item> <see cref="CLASS_FOREIGNSECURITYPRINCIPALS">物件類型名稱:外部安全性主體</see> </item>
        ///         <item> <see cref="CLASS_GROUP">物件類型名稱:群組</see> </item>
        ///         <item> <see cref="CLASS_PERSON">物件類型名稱:人員</see> </item>
        ///         <item> <see cref="CLASS_APTREE">物件類型名稱:人員</see> </item>
        ///     </list>
        /// </param>
        /// <returns>有註冊且可用的物件類型</returns>
        public static HashSet<string> GetSupportedClassNames(in HashSet<string> classNames = null)
        {
            // 沒有指定任何項目, 全部找尋
            if (classNames == null || classNames.Count == 0)
            {
                // 返回支援的所有類型
                return new HashSet<string>(dictionaryClassNameWithCategoryType.Keys);
            }

            // 最多支援項目大小為目前支援的長度大小
            HashSet<string> supportedClassNames = new HashSet<string>(dictionaryClassNameWithCategoryType.Count);
            // 遍歷支援項目
            foreach (string className in dictionaryClassNameWithCategoryType.Keys)
            {
                // 檢查是否為支援的類別
                if (!classNames.Contains(className))
                {
                    // 不是則跳過
                    continue;
                }

                // 加入作為可用項目: HashSet 本身會避免重複
                supportedClassNames.Add(className);
            }
            // 對外提供資料
            return supportedClassNames;
        }

        /// <summary>
        /// 使用指定物件類型名稱取得物件類型列舉旗標
        /// </summary>
        /// <param name="categoryTypes">物件類別, </param>
        /// <returns></returns>
        public static HashSet<string> GetClassNames(in CategoryTypes categoryTypes)
        {
            // 沒有指定任何項目, 全部找尋
            if (categoryTypes == CategoryTypes.NONE)
            {
                // 返回支援的所有類型
                return new HashSet<string>(dictionaryClassNameWithCategoryType.Keys);
            }

            // 最多支援項目大小為目前支援的長度大小
            HashSet<string> supportedClassNames = new HashSet<string>(dictionaryClassNameWithCategoryType.Count);
            // 遍立指定的物件類型名稱
            foreach (KeyValuePair<string, CategoryTypes> pair in dictionaryClassNameWithCategoryType)
            {
                // 檢查是否支援
                if ((pair.Value & categoryTypes) == CategoryTypes.NONE)
                {
                    // 不支援則跳過
                    continue;
                }

                // 加入作為可用項目: HashSet 本身會避免重複
                supportedClassNames.Add(pair.Key);
            }
            // 最後對外提供的時候: 此動作保證了未提供任何可用類別時會自動取用所有可用類別
            return supportedClassNames;
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
            { CLASS_APTREE, CategoryTypes.APTREE },
        };
    }
}
