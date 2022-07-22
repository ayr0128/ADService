using ADService.Basis;
using ADService.DynamicParse;
using ADService.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.RootDSE
{
    /// <summary>
    /// 存放已取得的物件類型藍本
    /// </summary>
    internal class SchemaClasses
    {
        #region 捨性類型
        /// <summary>
        /// 使用 GUID (小寫) 儲存的藍本類型
        /// </summary>
        private readonly ConcurrentDictionary<string, DriveSchemaClass> dictionaryGUIDWithDriveSchemaClass = new ConcurrentDictionary<string, DriveSchemaClass>();

        /// <summary>
        /// 取得指定類型名稱
        /// </summary>
        /// <param name="configurate">資料儲存位置A</param>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="expiresDuration">設定的過期時間</param>
        /// <param name="classLDAPDisplayNames"></param>
        /// <returns>指定查詢的類型物件類型</returns>
        internal DriveSchemaClass[] GetByNames(in Configurate configurate, in string account, in string password, in TimeSpan expiresDuration, in string[] classLDAPDisplayNames)
        {
            // 最大長度必定為執行續安全字典的長度
            Dictionary<string, DriveSchemaClass> dictionaryDisplayNameWithDriveSchemaClass = new Dictionary<string, DriveSchemaClass>(dictionaryGUIDWithDriveSchemaClass.Count);
            // 將執行續安全的字典轉成陣列
            foreach (KeyValuePair<string, DriveSchemaClass> pair in dictionaryGUIDWithDriveSchemaClass.ToArray())
            {
                // 使用對應型別儲存: 方便閱讀
                DriveSchemaClass schemaClass = pair.Value;
                // 轉換記錄格是
                dictionaryDisplayNameWithDriveSchemaClass.Add(schemaClass.LDAPDisplayName, schemaClass);
            }

            // 避免重複用
            HashSet<Guid> researchedGUIDs = new HashSet<Guid>(classLDAPDisplayNames.Length);
            // 避免重複用與找尋用
            HashSet<string> researchedLDAPDisplayNames = new HashSet<string>(classLDAPDisplayNames.Length);
            // 查詢之前是否已持有並停時過濾檢查
            foreach (string ldapDisplayName in classLDAPDisplayNames)
            {
                // 能找到指定名稱且尚未過期
                bool isExist = dictionaryDisplayNameWithDriveSchemaClass.TryGetValue(ldapDisplayName, out DriveSchemaClass driveSchemaClass);
                // 是否過期
                bool isExpire = !(driveSchemaClass is IExpired iExpired) || iExpired.Check(expiresDuration);
                // 存在且未過期的情況下
                if (isExist && !isExpire)
                {
                    // 則加入 GUID 最後取得
                    researchedGUIDs.Add(driveSchemaClass.SchemaGUID);
                }
                else
                {
                    // 表示此名稱需要被搜尋
                    researchedLDAPDisplayNames.Add(ldapDisplayName);
                }
            }

            // 使用屬性 GUID 的長度作為容器大小
            List<DriveSchemaClass> driveSchemaClasses = new List<DriveSchemaClass>(researchedGUIDs.Count);
            // 取得名稱過濾字串
            string nameFiliter = ADDrive.CombineORFiliter(Properties.C__SCHEMALDAPDISPLAYNAME, researchedLDAPDisplayNames);
            // 名稱過濾字串存在長度石材動作
            if (nameFiliter.Length != 0)
            {
                // 取得類型實須限制查詢特定類型
                string classFiliter = ADDrive.CombineFiliter(Properties.C_OBJECTCATEGORY, DriveSchemaClass.CATEGORY);
                // 使用上方條件組成搜尋字串
                string encodeFiliter = $"(&{nameFiliter}{classFiliter})";
                // 使用建構時提供的區分位置至指定位置拿取資料: 注意物件如果不存在會直接出錯
                using (DirectoryEntry entry = configurate.GetEntryByDN(account, password, DistinguisedName))
                {
                    // 找尋定義在此物件下的所有類別
                    SearchResult[] searchResults = Configurate.ExecSearcherMutiple(entry, encodeFiliter, SearchScope.OneLevel);
                    // 使用單元物件再次轉化
                    ADCustomUnit[] customUnits = configurate.ConvertToCustoms<ADCustomUnit>(account, password, searchResults);
                    // 逐一取代並檢查
                    foreach (DriveSchemaClass newDriveSchemaClass in configurate.ConvertToDrives<DriveSchemaClass>(account, password, customUnits))
                    {
                        // 取代舊結構
                        dictionaryGUIDWithDriveSchemaClass.AddOrUpdate(
                            Configurate.GetGUID(newDriveSchemaClass.SchemaGUID),
                            newDriveSchemaClass,
                            (PropertyName, OldDriveSchemaClass) => newDriveSchemaClass
                        );

                        // 加入對外提供項目
                        driveSchemaClasses.Add(newDriveSchemaClass);
                    }
                }
            }

            // 遍歷集成並將資料對外提供
            foreach (Guid unitSchemaGUID in researchedGUIDs)
            {
                // 轉成對應儲存字串
                string schemaClassGUIDLower = Configurate.GetGUID(unitSchemaGUID);
                // 先移除舊的資料
                if (!dictionaryGUIDWithDriveSchemaClass.TryGetValue(schemaClassGUIDLower, out DriveSchemaClass schemaClass))
                {
                    // 必定能取得: 此處為簡易防呆
                    continue;
                }

                // 設置成對外提供項目
                driveSchemaClasses.Add(schemaClass);
            }
            // 轉換成陣列對外提供
            return driveSchemaClasses.ToArray();
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
        internal SchemaClasses(in string distinguisedName) => DistinguisedName = distinguisedName;
    }
}
