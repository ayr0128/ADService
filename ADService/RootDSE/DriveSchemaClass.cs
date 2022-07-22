using ADService.DynamicParse;
using ADService.Protocol;
using System;
using System.Collections.Generic;

namespace ADService.RootDSE
{
    /// <summary>
    /// 物件類型藍本
    /// </summary>
    internal class DriveSchemaClass : ADDrive, IExpired
    {
        /// <summary>
        /// 指定類別的查詢用大類, 避免指定類行導致查詢到其他非預估物件
        /// </summary>
        /// <param name="schemaClasses">意圖查詢的類型</param>
        /// <returns>應查詢的類型</returns>
        internal static string[] GetCategories(params DriveSchemaClass[] schemaClasses)
        {
            // 先宣告長度
            HashSet<string> categories = new HashSet<string>(schemaClasses.Length);
            // 遍歷類型彆擷取出獄社大類
            foreach (DriveSchemaClass driveSchemaClass in schemaClasses)
            {
                // 使用預設入口類型
                categories.Add(driveSchemaClass.DefaultCategory);
            }
            // 宣告新的容器
            string[] resultCategories = new string[categories.Count];
            // 複製至陣列
            categories.CopyTo(resultCategories, 0);
            // 對外提供
            return resultCategories;
        }

        /// <summary>
        /// 類別類型
        /// </summary>
        internal const string CATEGORY = "classSchema";

        /// <summary>
        /// 啟用時間
        /// </summary>
        private readonly DateTime EnableTime = DateTime.UtcNow;

        bool IExpired.Check(in TimeSpan duration) => (DateTime.UtcNow - EnableTime) >= duration;

        /// <summary>
        /// 物件名稱
        /// </summary>
        [ADDescriptionProperty(Properties.C_SCHEMA_SUBCLASSOF)]
        internal string SubClassOf { get; set; }

        /// <summary>
        /// 物件名稱
        /// </summary>
        [ADDescriptionProperty(Properties.C__SCHEMALDAPDISPLAYNAME)]
        internal string LDAPDisplayName { get; set; }

        /// <summary>
        /// 物件名稱: CN
        /// </summary>
        [ADDescriptionProperty(Properties.P_CN)]
        internal string CN { get; set; }

        /// <summary>
        /// 物件名稱: Name
        /// </summary>
        [ADDescriptionProperty(Properties.P_NAME)]
        internal string Name { get; set; }

        /// <summary>
        /// 物件名稱: Name
        /// </summary>
        [ADDescriptionProperty(Properties.C_SCHEMA_DEFAULTCATEGORY)]
        internal string DefaultCategory { get; set; }

        /// <summary>
        /// 物件藍本 GUID
        /// </summary>
        internal Guid SchemaGUID => new Guid(SchemaGUIDInBytes);

        /// <summary>
        /// 從資料取得的藍本 GUID
        /// </summary>
        [ADDescriptionProperty(Properties.C_SCHEMA_SCHEMAGUID)]
        private Byte[] SchemaGUIDInBytes { get; set; }

        /// <summary>
        /// 此建構子將透過反射被呼叫
        /// </summary>
        /// <param name="customUnit">跟物件</param>
        internal DriveSchemaClass(in ADCustomUnit customUnit) : base(customUnit) { }
    }
}
