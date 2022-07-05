using System.DirectoryServices;

namespace ADService.Details
{
    /// <summary>
    /// 從入口物件取得的資料
    /// </summary>
    internal class PropertyDetail
    {
        /// <summary>
        /// 是否為單一數值
        /// </summary>
        internal bool IsSingleValue;
        /// <summary>
        /// 從入口物件取得的資料數值
        /// </summary>
        internal object PropertyValue;
        /// <summary>
        /// 數據大小
        /// </summary>
        internal int SizeOf;

        /// <summary>
        /// 建構藍本物件如何解析
        /// </summary>
        /// <param name="property">入口物件儲存資料</param>
        /// <param name="isSingleValued">是否為單一值</param>
        internal PropertyDetail(in PropertyValueCollection property, in bool isSingleValued)
        {
            PropertyValue = property.Value;
            SizeOf = property.Count;

            IsSingleValue = isSingleValued;
        }
    }
}
