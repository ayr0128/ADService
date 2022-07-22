using ADService.Basis;
using ADService.DynamicParse;
using ADService.Protocol;
using System;
using System.Collections.Concurrent;
using System.DirectoryServices;

namespace ADService.RootDSE
{
    /// <summary>
    /// 記錄子藍本物件中將被使用的設置
    /// </summary>
    internal class SubSchema
    {
        #region 捨性類型
        /// <summary>
        /// 經過搜尋才能取得的特殊屬性直
        /// </summary>
        private const string ATTRIBUTE_TYPES = "attributeTypes";
        /// <summary>
        /// 已解析完成的屬性資料類型
        /// </summary>
        private readonly ConcurrentDictionary<string, AttributeType> dictionaryPropertyNameWithAttributeType = new ConcurrentDictionary<string, AttributeType>();

        /// <summary>
        /// 透過指定的帳號權限取得對應參數的 OID 資料描述, 以下兩種情況會觸發重新找尋
        /// <list type="table">
        ///    <item> 資料除純器自宣告後已經超過暫存時間 </item>
        ///    <item> 指定的屬性名稱找不到 </item>
        /// </list>
        /// </summary>
        /// <param name="configurate">資料儲存位置A</param>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="name">屬性名稱</param>
        /// <param name="expiresDuration">設定的過期時間</param>
        /// <returns>此屬性的 OID</returns>
        internal AttributeType GeByName(in Configurate configurate, in string account, in string password, in string name, in TimeSpan expiresDuration)
        {
            // 指定屬性是否存在
            bool isExist = dictionaryPropertyNameWithAttributeType.TryGetValue(name, out AttributeType attributeType);
            // 是否過期
            bool isExpired = !(attributeType is IExpired iExpired) || iExpired.Check(expiresDuration);
            /* 已過期或者目標屬性不存在
                 此處可能存在多執行緒問題: 
            */
            if (isExpired || !isExist)
            {
                // 使用建構時提供的區分位置至指定位置拿取資料: 注意物件如果不存在會直接出錯
                using (DirectoryEntry entry = configurate.GetEntryByDN(account, password, DistinguisedName))
                {
                    // 查詢自身的過濾條件
                    string filiter = ADDrive.CombineFiliter(Properties.C_OBJECTCLASS, "*");
                    // 僅找尋指定欄位: 屬性類型
                    using (DirectorySearcher searcher = new DirectorySearcher(entry, filiter, new string[] { ATTRIBUTE_TYPES }, SearchScope.Base))
                    {
                        // 一定可以得到此物件
                        SearchResult one = searcher.FindOne();
                        // 取得指定類型的資料集合: 一定存在此集合
                        ResultPropertyValueCollection collection = one.Properties[ATTRIBUTE_TYPES];
                        // 將獲得的所有資料推入
                        foreach (string attributeTypeDescription in collection)
                        {
                            // 宣告新的結構
                            AttributeType newAttributeType = new AttributeType(attributeTypeDescription);
                            // 取代舊結構
                            dictionaryPropertyNameWithAttributeType.AddOrUpdate(
                                newAttributeType.Name,
                                newAttributeType,
                                (PropertyName, OldAttributeType) => newAttributeType
                            );

                            // 名稱與指定目標相同時執行替換
                            attributeType = newAttributeType.Name == name ? newAttributeType : attributeType;
                        }
                    }
                }
            }
            // 對外提供查詢的結果
            return attributeType;
        }
        #endregion

        /// <summary>
        /// 入口物件區分名稱
        /// </summary>
        private readonly string DistinguisedName;

        /// <summary>
        /// 建構時應提供入口物件位置
        /// </summary>
        /// <param name="distinguisedName">區分名稱</param>
        internal SubSchema(in string distinguisedName) => DistinguisedName = distinguisedName;
    }
}
