using ADService.Environments;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Media
{
    /// <summary>
    /// 此藍本物件相關資料
    /// </summary>
    internal sealed class UnitSchemaClass : UnitSchema
    {
        /// <summary>
        /// 用來作為固定搜尋字串的鍵值
        /// </summary>
        private const string FILITER_ATTRIBUTE = UnitExtendedRight.ATTRIBUTE_EXTENDEDRIGHT_APPLIESTO;

        /// <summary>
        /// 實作藍本結構
        /// </summary>
        /// <param name="properties">入口物件持有的屬性</param>
        internal UnitSchemaClass(in ResultPropertyCollection properties) : base(properties) { }

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
            if (combineValues.Contains(SchemaGUID))
            {
                // 跳出不處理
                return;
            }

            // 加入此物件類別的 GUID
            combineValues.Add(SchemaGUID);
        }

        internal override PropertytFlags GetPorpertyType(in UnitExtendedRight unitExtendedRight) => unitExtendedRight.WasAppliedWith(SchemaGUID) ? PropertytFlags.APPLIES : PropertytFlags.NONE;
    }
}
