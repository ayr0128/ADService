using System;
using System.DirectoryServices;

namespace ADService.Basis
{
    /// <summary>
    /// 內部存放的屬性格式
    /// </summary>
    internal class PropertyValue
    {
        /// <summary>
        /// 轉換後獲得的 OID 格式
        /// </summary>
        internal string OID;
        /// <summary>
        /// 被轉換成對應格式儲存的物件
        /// </summary>
        internal object Value;

        /// <summary>
        /// 外部轉換完成後提供的結果
        /// </summary>
        /// <param name="convertor">轉換器</param>
        /// <param name="collection">指定屬性</param>
        internal PropertyValue(in PropertyConvertor convertor, in PropertyValueCollection collection)
        {
            OID = convertor.OIDSyntax;

            // 根據是否多筆決定處理方式
            if (collection.Count == 1 && !convertor.IsArray)
            {
                // 一筆實應為獨立物件
                Value = convertor.ConvertorFunc(collection.Value);
            }
            else
            {
                // 多筆實應為陣列物件
                Array values = Array.CreateInstance(convertor.TypeSyntax, collection.Count);
                // 遍歷參數
                for (int index = 0; index < collection.Count; index++)
                {
                    // 進行轉換
                    object convertedValue = convertor.ConvertorFunc(collection[index]);
                    // 設置轉換完成的物件
                    values.SetValue(convertedValue, index);
                }
                // 賦予至實際儲存的物件上
                Value = values;
            }
        }
        /// <summary>
        /// 外部轉換完成後提供的結果
        /// </summary>
        /// <param name="convertor">轉換器</param>
        /// <param name="collection">指定屬性</param>
        internal PropertyValue(in PropertyConvertor convertor, in ResultPropertyValueCollection collection)
        {
            OID = convertor.OIDSyntax;

            // 根據是否多筆決定處理方式
            if (collection.Count == 1 && !convertor.IsArray)
            {
                // 一筆實應為獨立物件
                Value = convertor.ConvertorFunc(collection[0]);
            }
            else
            {
                // 多筆實應為陣列物件
                Array values = Array.CreateInstance(convertor.TypeSyntax, collection.Count);
                // 遍歷參數
                for (int index = 0; index < collection.Count; index++)
                {
                    // 進行轉換
                    object convertedValue = convertor.ConvertorFunc(collection[index]);
                    // 設置轉換完成的物件
                    values.SetValue(convertedValue, index);
                }
                // 賦予至實際儲存的物件上
                Value = values;
            }
        }
    }
}
