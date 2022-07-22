using ADService.Authority;
using ADService.Basis;
using ADService.Certificate;
using ADService.DynamicParse;
using ADService.Environments;
using ADService.Protocol;
using ADService.RootDSE;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;

namespace ADService.Foundation
{
    /// <summary>
    /// 登入的使用者
    /// </summary>
    public sealed class ADLoginUser : IUserAuthorization
    {
        #region 綁定
        /// <summary>
        /// 通過指定登入者的帳號密碼與設定執行簡易登入
        /// </summary>
        /// <param name="configuration">設定資料</param>
        /// <param name="account">使用者帳號</param>
        /// <param name="oassword">使用者密碼</param>
        /// <returns></returns>
        internal static ADLoginUser Bind(in Configurate configuration, in string account, in string oassword)
        {
            // 登入者提供的帳號有可能是 sMMaccountName
            string sAMAccountFiliter = ADDrive.CombineFiliter(Properties.C_SMMACCOUNTNAME, account);
            // 登入者提供的帳號有可能是 userPrincipalName
            string userPrincipalFiliter = ADDrive.CombineFiliter(Properties.C_USERPRINCIPALNAME, account);
            // 登入者必定是使用者物件
            string userFiliter = ADDrive.CombineFiliter(Properties.C_OBJECTCLASS, LDAPCategory.CLASS_PERSON);

            // 使用帳號登入指定使用者, 並提供具有權限的入口物件
            using (DirectoryEntry entry = configuration.GetEntryByDN(account, oassword))
            {
                // 預設查詢是下層所有的物件並取得目標物件
                SearchResult result = Configurate.ExecSearcherSingle(entry, $"(&{userFiliter}(|{sAMAccountFiliter}{userPrincipalFiliter}))");
                // 分析並取得對應物件
                ADCustomUnit customUnit = configuration.ConvertToCustom<ADCustomUnit>(account, oassword, result);
                // 對外提供查詢結果
                return new ADLoginUser(configuration, customUnit, account, oassword);
            }
        }
        #endregion

        #region 實作:IUserAuthorization
        DirectoryEntry IUserAuthorization.GetEntryByDN(in string distinguishedName)
        {
            // 檢查區分名稱
            if (string.IsNullOrWhiteSpace(distinguishedName))
            {
                // 區分名稱為空實對外提供空物件
                return null;
            }

            // 要找尋那些物件
            string distinguishedNameFiliter = ADDrive.CombineFiliter(Properties.C_DISTINGUISHEDNAME, distinguishedName);
            // 產生入口物件
            using (DirectoryEntry entry = Configurate.GetEntryByDN(Account, Password))
            {
                // 預設查詢是下層所有的物件並取得過濾條件中所有的物件
                SearchResult result = Configurate.ExecSearcherSingle(entry, distinguishedNameFiliter);
                // 找不到指定物件
                // 找不到指定物件
                return result?.GetDirectoryEntry();
            }
        }

        DirectoryEntry IUserAuthorization.GetEntryByGUID(in string valueGUID)
        {
            // 檢查區分名稱
            if (string.IsNullOrWhiteSpace(valueGUID))
            {
                // 區分名稱為空實對外提供空物件
                return null;
            }

            // 轉換成 GUID
            Guid convertedGUID = new Guid(valueGUID);

            // 要找尋那些物件
            string objectGUIDFiliter = ADDrive.CombineFiliter(Properties.C_OBJECTGUID, Configurate.GetFiliter(convertedGUID));
            // 產生入口物件
            using (DirectoryEntry entry = Configurate.GetEntryByDN(Account, Password))
            {
                // 預設查詢是下層所有的物件並取得過濾條件中所有的物件
                SearchResult result = Configurate.ExecSearcherSingle(entry, objectGUIDFiliter);
                // 找不到指定物件
                return result?.GetDirectoryEntry();
            }
        }

        DirectoryEntry IUserAuthorization.GetEntryBySID(in string valueSID)
        {
            // 檢查區分名稱
            if (string.IsNullOrWhiteSpace(valueSID))
            {
                // 區分名稱為空實對外提供空物件
                return null;
            }

            // 使用外部提供 SID 進行轉換
            SecurityIdentifier securityIdentifier = new SecurityIdentifier(valueSID);
            // 要找尋那些物件
            string objectGUIDFiliter = ADDrive.CombineFiliter(Properties.C_OBJECTSID, Configurate.GetFiliter(securityIdentifier));
            // 產生入口物件
            using (DirectoryEntry entry = Configurate.GetEntryByDN(Account, Password))
            {
                // 預設查詢是下層所有的物件並取得過濾條件中所有的物件
                SearchResult result = Configurate.ExecSearcherSingle(entry, objectGUIDFiliter);
                // 找不到指定物件
                return result?.GetDirectoryEntry();
            }
        }

        T IUserAuthorization.ConvertToCustom<T>(in DirectoryEntry entry) => Configurate.ConvertToCustom<T>(Account, Password, entry);
        #endregion

        /// <summary>
        /// 設定資料
        /// </summary>
        private readonly Configurate Configurate;
        /// <summary>
        /// 最基礎的物件資訊
        /// </summary>
        private readonly ADCustomUnit CustomUnit;
        /// <summary>
        /// 使用者帳號
        /// </summary>
        private readonly string Account;
        /// <summary>
        /// 使用者密碼
        /// </summary>
        private readonly string Password;

        /// <summary>
        /// 紀錄設定文件以及使用者帳號密碼
        /// </summary>
        /// <param name="configurate">設定資料</param>
        /// <param name="customUnit">基礎資料</param>
        /// <param name="account">使用者帳號</param>
        /// <param name="password">使用者密碼</param>
        internal ADLoginUser(in Configurate configurate, in ADCustomUnit customUnit, in string account, in string password)
        {
            Configurate = configurate;
            CustomUnit = customUnit;
            Account = account;
            Password = password;
        }

        /// <summary>
        /// 使用物件類別找尋包含物件類型的物件, 可以指定一個物件做為跟目錄
        /// </summary>
        /// <param name="rootCustomUnit">指定根目錄, 可以為 null</param>
        /// <param name="classNames">想要找尋的物件類型</param>
        /// <returns>能找到的物件類型</returns>
        public ADCustomUnit[] GetUnitsByClassNames(in ADCustomUnit rootCustomUnit, params string[] classNames)
        {
            // 取得想查詢的物件類型
            DriveSchemaClass[] driveSchemaClasses = Configurate.GetSchemaClassByNames(Account, Password, classNames);
            // 轉化成?正確的入口名稱
            string[] categories = DriveSchemaClass.GetCategories(driveSchemaClasses);
            // 如果指定的物件類型經過組合後釋空字串
            if (categories.Length == 0)
            {
                // 對外提供空鎮葉
                return Array.Empty<ADCustomUnit>();
            }

            // 要找尋那些物件
            string classFiliter = ADDrive.CombineORFiliter(Properties.C_OBJECTCATEGORY, categories);
            // 取得指定根目錄
            string distinguishedName = rootCustomUnit == null ? string.Empty : rootCustomUnit.DistinguishedName;
            // 產生入口物件: 注意物件如果不存在會直接出錯
            using (DirectoryEntry entry = Configurate.GetEntryByDN(Account, Password, distinguishedName))
            {
                // 輸入項目轉換成檢查用的 HashSet
                HashSet<string> classNameSet = new HashSet<string>(classNames);
                /* 符合下述邏輯時必須將根目錄作為查詢主體
                     - 無提供查詢項目時
                     - 查詢目標包含根目錄
                */
                if (string.IsNullOrEmpty(distinguishedName) && classNameSet.Contains(LDAPCategory.CLASS_DOMAINDNS))
                {
                    // 預設查詢是下層所有的物件並取得過濾條件中所有的物件
                    SearchResult result = Configurate.ExecSearcherSingle(entry, classFiliter, SearchScope.Base);
                    // 轉換為單一參數
                    ADCustomUnit customUnit = Configurate.ConvertToCustom<ADCustomUnit>(Account, Password, result);
                    // 找不到指定物件
                    if (result == null)
                    {
                        // 對外回傳空物件
                        return null;
                    }

                    // 對外提供
                    return new ADCustomUnit[] { customUnit };
                }
                else
                {
                    // 預設查詢是下層所有的物件並取得過濾條件中所有的物件
                    SearchResult[] results = Configurate.ExecSearcherMutiple(entry, classFiliter, SearchScope.OneLevel);
                    // 找尋到指定類別的物件
                    return Configurate.ConvertToCustoms<ADCustomUnit>(Account, Password, results);
                }
            }
        }

        /// <summary>
        /// 指定區分名稱直接取得物件, 若此區分名稱物件不存在會提供空物件
        /// </summary>
        /// <param name="distinguishedName">區分名稱</param>
        /// <returns>基礎物件</returns>
        public ADCustomUnit GetUnitByDN(in string distinguishedName)
        {
            // 檢查區分名稱
            if (string.IsNullOrWhiteSpace(distinguishedName))
            {
                // 區分名稱為空實對外提供空物件
                return null;
            }

            // 要找尋那些物件
            string distinguishedNameFiliter = ADDrive.CombineFiliter(Properties.C_DISTINGUISHEDNAME, distinguishedName);
            // 產生入口物件
            using (DirectoryEntry entry = Configurate.GetEntryByDN(Account, Password))
            {
                // 預設查詢是下層所有的物件並取得過濾條件中所有的物件
                SearchResult result = Configurate.ExecSearcherSingle(entry, distinguishedNameFiliter);
                // 找不到指定物件
                if (result == null)
                {
                    // 對外回傳空物件
                    return null;
                }

                // 找尋到指定類別的物件: 一次僅取得一層
                return Configurate.ConvertToCustom<ADCustomUnit>(Account, Password, result);
            }
        }

        /// <summary>
        /// 指定區分名稱直接取得物件, 若此 GUID 物件不存在會提供空物件
        /// </summary>
        /// <param name="valueGUID">指定GUID</param>
        /// <returns>基礎物件</returns>
        public ADCustomUnit GetUnitByGUID(in string valueGUID)
        {
            // 檢查區分名稱
            if (string.IsNullOrWhiteSpace(valueGUID))
            {
                // 區分名稱為空實對外提供空物件
                return null;
            }

            // 轉換成 GUID
            Guid convertedGUID = new Guid(valueGUID);

            // 要找尋那些物件
            string objectGUIDFiliter = ADDrive.CombineFiliter(Properties.C_OBJECTGUID, Configurate.GetFiliter(convertedGUID));
            // 產生入口物件
            using (DirectoryEntry entry = Configurate.GetEntryByDN(Account, Password))
            {
                // 預設查詢是下層所有的物件並取得過濾條件中所有的物件
                SearchResult result = Configurate.ExecSearcherSingle(entry, objectGUIDFiliter);
                // 找不到指定物件
                if (result == null)
                {
                    // 對外回傳空物件
                    return null;
                }

                // 找尋到指定類別的物件: 一次僅取得一層
                return Configurate.ConvertToCustom<ADCustomUnit>(Account, Password, result);
            }
        }

        /// <summary>
        /// 指定區分名稱直接取得物件, 若此 SID 物件不存在造成錯誤
        /// </summary>
        /// <param name="valueSID">指定 SUD</param>
        /// <returns>基礎物件</returns>
        public ADCustomUnit GetUnitBySID(in string valueSID)
        {
            // 檢查區分名稱
            if (string.IsNullOrWhiteSpace(valueSID))
            {
                // 區分名稱為空實對外提供空物件
                return null;
            }

            // 使用外部提供 SID 進行轉換
            SecurityIdentifier securityIdentifier = new SecurityIdentifier(valueSID);
            // 要找尋那些物件
            string objectGUIDFiliter = ADDrive.CombineFiliter(Properties.C_OBJECTSID, Configurate.GetFiliter(securityIdentifier));
            // 產生入口物件
            using (DirectoryEntry entry = Configurate.GetEntryByDN(Account, Password))
            {
                // 預設查詢是下層所有的物件並取得過濾條件中所有的物件
                SearchResult result = Configurate.ExecSearcherSingle(entry, objectGUIDFiliter);
                // 找不到指定物件
                if (result == null)
                {
                    // 對外回傳空物件
                    return null;
                }

                // 找尋到指定類別的物件: 一次僅取得一層
                return Configurate.ConvertToCustom<ADCustomUnit>(Account, Password, result);
            }
        }

        /// <summary>
        /// 取得指定單的隸屬關係, 不指定任何單位時將取得自身關係
        /// </summary>
        /// <param name="customUnit">指定物件</param>
        public ADRelationShip[] GetRelationShips(in ADCustomUnit customUnit = null)
        {
            // 若外部沒有指定任何物件則查詢自己
            ADCustomUnit processCustomUnit = customUnit ?? CustomUnit;
            // 將成員相關資料撈出
            DriveRelation driveRelation = Configurate.ConvertToDrive<DriveRelation>(Account, Password, processCustomUnit);
            // 處理隸屬於組織
            return GetRelations(driveRelation, InterpersonalRelationFlags.MEMBEROF | InterpersonalRelationFlags.MEMBER);
        }

        /// <summary>
        /// 提供繼承了指定型別的樣板, 此樣板會透過屬性權限動態撈取資料
        /// </summary>
        /// <typeparam name="T">繼承特性型別的樣板</typeparam>
        /// <param name="customUnits">欲轉換的所有類型</param>
        /// <returns>成功轉換的所有資料</returns>
        public T[] ConvertToDrive<T>(params ADCustomUnit[] customUnits) where T : ADDrive => Configurate.ConvertToDrives<T>(Account, Password, customUnits);

        /// <summary>
        /// 指定一個目標單元取得能對其進行動作的協議, 當未指定任何目標時會提供登入者自身的可行駛權限
        /// </summary>
        /// <param name="customUnit"></param>
        /// <returns></returns>
        public ADAgreement GetAgreement(in ADCustomUnit customUnit = null)
        {
            // 取得自身成員關係表
            DriveRelation driveRelation = Configurate.ConvertToDrive<DriveRelation>(Account, Password, CustomUnit);
            // 僅需取得隸屬關係
            ADRelationShip[] relationShipADs = GetRelations(driveRelation, InterpersonalRelationFlags.MEMBEROF);
            // 取得登入者奔深的授權書
            Recognizance recognizance = new Recognizance(this, driveRelation, relationShipADs);
            // 取得協議書
            return ADAgreement.CreatePermission(recognizance, customUnit ?? CustomUnit);
        }

        /// <summary>
        /// 根據關係與指定旗標決定找尋那些關係
        /// </summary>
        /// <param name="driveRelation">關係表</param>
        /// <param name="relationFlags">找尋那些關係, 根據 <see cref="InterpersonalRelationFlags"> 旗標 </see> 運作</param>
        /// <returns>根據旗標決定取得資料</returns>
        private ADRelationShip[] GetRelations(in DriveRelation driveRelation, in InterpersonalRelationFlags relationFlags)
        {
            // 處理隸屬於組織
            if (driveRelation == null)
            {
                // 不應無法取得自身關係
                return null;
            }

            // 成員與隸屬於必定不會重複
            Dictionary<string, InterpersonalRelationFlags> dictionaryDistinguishedNameWithRelationFlags = new Dictionary<string, InterpersonalRelationFlags>();
            // 檢查是否包還旗標: 隸屬
            bool isContainMemberOf = (relationFlags & InterpersonalRelationFlags.MEMBEROF) != InterpersonalRelationFlags.NONE;
            // 當指定旗標包含隸屬時才進行動作
            if (isContainMemberOf)
            {
                // 遍歷填入關係表: 隸屬於
                Array.ForEach(driveRelation.MemberOf ?? Array.Empty<string>(), distinguishedName => dictionaryDistinguishedNameWithRelationFlags.Add(distinguishedName, InterpersonalRelationFlags.MEMBEROF));
            }

            // 檢查是否包還旗標: 成員
            bool isContainMember = (relationFlags & InterpersonalRelationFlags.MEMBER) != InterpersonalRelationFlags.NONE;
            // 當指定旗標包含成員時才進行動作
            if (isContainMember)
            {
                // 遍歷填入關係表L 成員
                Array.ForEach(driveRelation.Member ?? Array.Empty<string>(), distinguishedName => dictionaryDistinguishedNameWithRelationFlags.Add(distinguishedName, InterpersonalRelationFlags.MEMBER));
            }

            // 找尋隸屬於
            string distinguishedNamesFiliter = ADDrive.CombineORFiliter(Properties.C_DISTINGUISHEDNAME, dictionaryDistinguishedNameWithRelationFlags.Keys);

            // 查詢關係時應使用下方個查詢屬性
            string[] QueryProperties = new string[2] { Properties.C_DISTINGUISHEDNAME , Properties.C_OBJECTSID };

            // 宣告用來放置資料的陣列
            List<ADRelationShip> relationShipADs = new List<ADRelationShip>();
            // 產生入口物件: 沒有指定時必定是根目錄
            using (DirectoryEntry entry = Configurate.GetEntryByDN(Account, Password))
            {
                // 指定找尋成員與群組; 只有這兩種可以做為成員或隸屬的原件
                DriveSchemaClass[] driveSchemaClasses = Configurate.GetSchemaClassByNames(Account, Password, LDAPCategory.CLASS_GROUP, LDAPCategory.CLASS_PERSON);
                // 轉化成?正確的入口名稱
                string[] categories = DriveSchemaClass.GetCategories(driveSchemaClasses);
                // 這些類型就是要找尋的類型限制
                string categoriesFiliter = ADDrive.CombineORFiliter(Properties.C_OBJECTCATEGORY, categories);

                // 檢查是否存在須查詢區分名稱
                if (!string.IsNullOrEmpty(distinguishedNamesFiliter))
                {
                    // 預設查詢是下層所有的物件並取得過濾條件中所有的物件
                    SearchResult[] results = Configurate.ExecSearcherMutiple(entry, $"(&{categoriesFiliter}{distinguishedNamesFiliter})", SearchScope.Subtree, QueryProperties);
                    // 找尋到指定類別的物件
                    foreach (ADCustomRelation unitRelationDrive in Configurate.ConvertToCustoms<ADCustomRelation>(Account, Password, results))
                    {
                        // 取得對照關係: 必定能夠發現: 無法發現時關線會是 NONE
                        dictionaryDistinguishedNameWithRelationFlags.TryGetValue(unitRelationDrive.DistinguishedName, out InterpersonalRelationFlags storedRelationFlags);
                        // 建置關係表
                        ADRelationShip relationShipAD = new ADRelationShip(unitRelationDrive, storedRelationFlags);
                        // 推入對外提供的項目
                        relationShipADs.Add(relationShipAD);
                    }
                }

                // 處理主要隸屬群組與主要隸屬成員: 只有在存在主要隸屬群組時才會處理
                if (driveRelation.PrimaryGroupID != 0 && isContainMemberOf)
                {
                    // 查詢潤義務建
                    string classAllFiliter = ADDrive.CombineFiliter(Properties.C_OBJECTCLASS, "*");
                    // 取得根網域的查詢結果
                    SearchResult resultSID = Configurate.ExecSearcherSingle(entry, classAllFiliter, SearchScope.Base, Properties.C_OBJECTSID);
                    // 應提供所引致目標項目的類別: 預計僅拿取 SID
                    ObjectSID objectSID = Configurate.ConvertToCustom<ObjectSID>(Account, Password, resultSID);

                    // 將主要群組 ID 與 網域SID組合後就會是主要鼠群組的 SID
                    SecurityIdentifier securityIdentifier = new SecurityIdentifier($"{objectSID.TranslateSID}-{driveRelation.PrimaryGroupID}");
                    // 要找尋那些物件
                    string objectSIDFiliter = ADDrive.CombineFiliter(Properties.C_OBJECTSID, Configurate.GetFiliter(securityIdentifier));
                    // 預設查詢是下層所有的物件並取得過濾條件中所有的物件
                    SearchResult resultPrimaryGroup = Configurate.ExecSearcherSingle(entry, $"(&{objectSIDFiliter}{categoriesFiliter})", SearchScope.Subtree, QueryProperties);
                    // 找尋到指定類別的物件: 一次僅取得一層
                    ADCustomRelation relationDriveAD = Configurate.ConvertToCustom<ADCustomRelation>(Account, Password, resultPrimaryGroup);
                    // 建置關係表
                    ADRelationShip relationShipAD = new ADRelationShip(relationDriveAD, InterpersonalRelationFlags.MEMBEROF | InterpersonalRelationFlags.PRIMARY);
                    // 推入對外提供的項目
                    relationShipADs.Add(relationShipAD);
                }

                // 是否需要找尋主要隸屬成員: 需檢查提供目標是否為群組
                if (driveRelation.CustomUnit.Classes.Last() == LDAPCategory.CLASS_GROUP && isContainMember)
                {
                    // 取得查詢目標的 SID 字串: 已知最後一串數字
                    string objectSID = driveRelation.SID;
                    // 已知群組 SID 最後一個 '-' 後的資料就是 PrimaryGroupToken
                    int index = objectSID.LastIndexOf('-');
                    // 取得 PrimaryGroupToken: [TODO] 是否有錯誤的可能性呢
                    string primaryGroupToken = objectSID.Substring(index + 1);

                    // 要找尋那些物件
                    string primaryGroupIDFiliter = ADDrive.CombineFiliter(Properties.C_PRIMARYGROUPID, primaryGroupToken);
                    // 預設查詢是下層所有的物件並取得過濾條件中所有的物件
                    SearchResult[] resultPrimaryMembers = Configurate.ExecSearcherMutiple(entry, $"(&{primaryGroupIDFiliter}{categoriesFiliter})", SearchScope.Subtree, QueryProperties);
                    // 找尋到指定類別的物件: 一次僅取得一層
                    foreach (ADCustomRelation relationDriveAD in Configurate.ConvertToCustoms<ADCustomRelation>(Account, Password, resultPrimaryMembers))
                    {
                        // 建置關係表
                        ADRelationShip relationShipAD = new ADRelationShip(relationDriveAD, InterpersonalRelationFlags.MEMBER | InterpersonalRelationFlags.PRIMARY);
                        // 推入對外提供的項目
                        relationShipADs.Add(relationShipAD);
                    }
                }
            }

            // 對外提供相關項目
            return relationShipADs.ToArray();
        }
    }
}
