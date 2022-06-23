using ADService.Environments;
using System.DirectoryServices;

namespace ADService.Revealer
{
    /// <summary>
    /// 屬性數值儲存與樂度器
    /// </summary>
    internal abstract class RevealerProperties
    {
        /// <summary>
        /// 特性鍵值名稱: 建構時設定
        /// </summary>
        internal string PropertyName { get; private set; }
        /// <summary>
        /// 若啟用查看此欄位: 資料是否強制存在
        /// </summary>
        internal bool IsForceExist { get; private set; }

        /// <summary>
        /// 建構解析特性鍵值所需資料的集合
        /// </summary>
        /// <param name="propertyName">解析的目標鍵值名稱</param>
        /// <param name="isForceExist">是否強制設定需存在資料</param>
        internal RevealerProperties(in string propertyName, in bool isForceExist)
        {
            PropertyName = propertyName;
            IsForceExist = isForceExist;
        }

        /// <summary>
        /// 解析從入口物件取得的特性鍵值目標
        /// </summary>
        /// <param name="properties">入口物件特性鍵值集合</param>
        /// <returns>對外提供封盒後的物件</returns>
        /// <exception cref="LDAPExceptions">解析鍵值不符合預期時對外丟出: 如型態不如預期或轉換後資料長度不符等</exception>
        internal abstract object Parse(in PropertyCollection properties);
    }
}
