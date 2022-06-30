using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Media
{
    /// <summary>
    /// 此藍本物件相關資料
    /// </summary>
    internal sealed class UnitSchemaAttribute : UnitSchema
    {
        /// <summary>
        /// 用來作為固定搜尋字串的鍵值
        /// </summary>
        private const string FILITER_ATTRIBUTE = UnitExtendedRight.ATTRIBUTE_EXTENDEDRIGHT_GUID;


        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_SCHEMA_SECURITY_GUID"> GUID </see> 取得的相關字串
        /// </summary>
        internal readonly string SecurityGUID;

        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_SCHEMA_ATTRIBUTEID"> 是否為屬性 </see> 取得的相關字串
        /// </summary>
        internal readonly string AttributeID;

        /// <summary>
        /// 使用欄位 <see cref="ATTRIBUTE_SCHEMA_IS_SINGLEVALUED"> 是否一筆 </see> 取得的相關字串
        /// </summary>
        internal readonly bool IsSingleValued;

        /// <summary>
        /// 實作藍本結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        /// <param name="attributeID">入口物件持有的屬性</param>
        internal UnitSchemaAttribute(in ResultPropertyCollection properties, in string attributeID) : base(properties)
        {
            AttributeID = attributeID;

            SecurityGUID = LDAPConfiguration.ParseGUID(ATTRIBUTE_SCHEMA_SECURITY_GUID, properties);
            IsSingleValued = LDAPConfiguration.ParseSingleValue<bool>(ATTRIBUTE_SCHEMA_IS_SINGLEVALUED, properties);
        }

        internal override void CombineFiliter(ref Dictionary<string, HashSet<string>> dictionaryAttributeNameWithValues)
        {
            // 取得依賴餘鍵值得可用組合參數
            if (!dictionaryAttributeNameWithValues.TryGetValue(FILITER_ATTRIBUTE, out HashSet<string> combineValues))
            {
                // 不存在時重新宣告
                combineValues = new HashSet<string>();
                // 並推入
                dictionaryAttributeNameWithValues.Add(FILITER_ATTRIBUTE, combineValues);
            }

            // 之前已經包含過此物件類別的 GUID
            if (combineValues.Contains(SecurityGUID))
            {
                // 跳出不處理
                return;
            }

            // 加入此物件類別的 GUID
            combineValues.Add(SecurityGUID);
        }

        internal override bool IsPropertySet(in UnitExtendedRight unitExtendedRight) => unitExtendedRight.RightsGUID.ToLower() == SecurityGUID.ToLower();
    }
}
