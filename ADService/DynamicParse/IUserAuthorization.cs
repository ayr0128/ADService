using System.DirectoryServices;

namespace ADService.DynamicParse
{
    /// <summary>
    /// 使用者受玄介面
    /// </summary>
    internal interface IUserAuthorization
    {
        /// <summary>
        /// 指定區分名稱直接取得物件, 若此區分名稱物件不存在會提供空物件
        /// </summary>
        /// <param name="distinguishedName">區分名稱</param>
        /// <returns>入口物件</returns>
        DirectoryEntry GetEntryByDN(in string distinguishedName);
        /// <summary>
        /// 指定區分名稱直接取得物件, 若此 GUID 物件不存在會提供空物件
        /// </summary>
        /// <param name="valueGUID">指定GUID</param>
        /// <returns>入口物件</returns>
        DirectoryEntry GetEntryByGUID(in string valueGUID);
        /// <summary>
        /// 指定區分名稱直接取得物件, 若此 SID 物件不存在造成錯誤
        /// </summary>
        /// <param name="valueSID">指定 SUD</param>
        /// <returns>入口物件</returns>
        DirectoryEntry GetEntryBySID(in string valueSID);

        /// <summary>
        /// 指定區分名稱直接取得物件, 若此 SID 物件不存在造成錯誤
        /// </summary>
        /// <param name="entry">指定 入口物件</param>
        /// <returns>目標轉換型別</returns>
        T ConvertToCustom<T>(in DirectoryEntry entry) where T : new();
    }
}
