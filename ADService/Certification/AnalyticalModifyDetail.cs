using ADService.Environments;
using ADService.Features;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace ADService.Certification
{
    /// <summary>
    /// 異動持有屬性
    /// </summary>
    internal sealed class AnalyticalModifyDetail : Analytical
    {
        /// <summary>
        /// 存取權限與屬性鍵值對應表, 如果沒有屬性鍵值對照表, 鑿代表存取權限本身就是屬性鍵值
        /// </summary>
        internal static HashSet<string> PropertiesNames = new HashSet<string>
        {
            Properties.P_MEMBER,             // 成員
            Properties.P_MEMBEROF,           // 隸屬群組
            Properties.P_DESCRIPTION,        // 描述
            Properties.P_DISPLAYNAME,        // 顯示名稱
            Properties.P_SN,                 // 姓
            Properties.P_GIVENNAME,          // 名
            Properties.P_INITIALS,           // 英文縮寫

            Properties.P_PWDLASTSET,               // 密碼最後設置時間
            Properties.P_USERACCOUNTCONTROL,       // 帳號控制
            Properties.P_SUPPORTEDENCRYPTIONTYPES, // 支援的連線加密格式
        };

        /// <summary>
        /// 除去需特殊判斷的下述列舉外的所有列舉旗標集合
        /// <list type="table|number">
        ///    <item> <term> <see cref="AccountControlProtocols.PWD_CHANGE_NEXTLOGON">密碼須在下次登入時重新設置</see> </term> 根據 <see cref="Properties.P_PWDLASTSET">權限</see>> 決定是否可查看或調整 </item>
        ///    <item> <term> <see cref="AccountControlProtocols.PWD_DISABLE_CHANGE">密碼不可變更</see> </term> 與 <see cref="AccountControlProtocols.PWD_CHANGE_NEXTLOGON">密碼須在下次登入時重新設置</see>> 衝突, 衝突時以 <see cref="AccountControlProtocols.PWD_CHANGE_NEXTLOGON">密碼須在下次登入時重新設置</see>> 優先 </item>
        ///    <item> <term> <see cref="AccountControlProtocols.ACCOUNT_KERBEROS_AES128">支援 AES128 加密傳輸</see> </term> 根據 <see cref="Properties.P_SUPPORTEDENCRYPTIONTYPES">權限</see>> 決定是否可查看或調整 </item>
        ///    <item> <term> <see cref="AccountControlProtocols.ACCOUNT_KERBEROS_AES256">支援 AES256 加密傳輸</see> </term> 根據 <see cref="Properties.P_SUPPORTEDENCRYPTIONTYPES">權限</see>> 決定是否可查看或調整 </item>
        /// </list>
        /// </summary>
        private const AccountControlProtocols ACOUNTCONTROL_MASK = AccountControlProtocols.PWD_ENABLE_FOREVER
                                                                   | AccountControlProtocols.PWD_ENCRYPTED
                                                                   | AccountControlProtocols.ACCOUNT_DISABLE
                                                                   | AccountControlProtocols.ACCOUNT_SMARTCARD
                                                                   | AccountControlProtocols.ACCOUNT_CONFIDENTIAL
                                                                   | AccountControlProtocols.ACCOUNT_KERBEROS_DES
                                                                   | AccountControlProtocols.ACCOUNT_KERBEROS_PREAUTH;

        /// <summary>
        /// 連線加密的支援格式: 協議用
        /// </summary>
        const AccountControlProtocols ENCRYPT_PROTOCOLMASK = AccountControlProtocols.ACCOUNT_KERBEROS_AES128 | AccountControlProtocols.ACCOUNT_KERBEROS_AES256;

        /// <summary>
        /// 連線加密的支援格式: 內部紀錄用
        /// </summary>
        const EncryptedType ENCRYPT_MASK = EncryptedType.AES128 | EncryptedType.AES256;

        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal AnalyticalModifyDetail() : base(Methods.M_MODIFYDETAIL, false) { }

        /// <summary>
        /// 提供給繼承使用的呼叫建構子
        /// </summary>
        /// <param name="name">方法或屬性名稱</param>
        /// <param name="isShowed">是否展示在功能列表</param>
        internal AnalyticalModifyDetail(in string name, in bool isShowed) : base(name, isShowed) { }

        internal override (bool, InvokeCondition, string) Invokable(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination)
        {
            // 整合各 SID 權向狀態
            LDAPPermissions permissions = GetPermissions(dispatcher, invoker, destination);
            // 要對外回傳的所有項目: 預設容器大小等於所有需要轉換的項目
            Dictionary<string, InvokeCondition> dictionaryAttributesNameWithCondition = new Dictionary<string, InvokeCondition>(PropertiesNames.Count);
            #region 整理可對外提供的項目
            // 遍歷支援項目
            foreach (string propertyName in PropertiesNames)
            {
                // 檢查參數是否支援
                if (!destination.StoredProperties.GetProperty(propertyName, out _))
                {
                    // 不知原則必定不需處理
                    continue;
                }

                // 檢驗支援項目: 具有查看旗標
                if (!permissions.IsAllow(propertyName, null, AccessRuleRightFlags.PropertyRead))
                {
                    // 不具有查看旗標時: 就可以忽略修改旗標, 因為看不到等於不能修改
                    continue;
                }

                // 將對外提供的處理項目
                InvokeCondition invokeCondition;
                // 是否可異動
                bool isEditable = permissions.IsAllow(propertyName, null, AccessRuleRightFlags.PropertyWrite);
                // 對外提供項目有個需要特例處理
                switch (propertyName)
                {
                    #region 成員: 協議
                    case Properties.P_MEMBER:
                        {
                            // 取得介面
                            IRevealerMember revealerMember = destination as IRevealerMember;
                            // 介面不存在時代表此權限不必提供
                            if (revealerMember == null)
                            {
                                // 跳過此處理
                                continue;
                            }

                            // 嘗試轉換成目標介面並取得內容
                            LDAPRelationship[] values = revealerMember.Elements;
                            // 成員的預期項目: 必定持有自定項目
                            ProtocolAttributeFlags protocolAttributeFlags = ProtocolAttributeFlags.HASVALUE | ProtocolAttributeFlags.ISARRAY;
                            // 宣告持有內容: 運行至此必定包含可讀取項目
                            Dictionary<string, object> dictionaryProtocolWithDetailInside = new Dictionary<string, object>
                            {
                                { InvokeCondition.STOREDTYPE, typeof(LDAPRelationship).Name }, // 持有內容描述
                                { InvokeCondition.VALUE, values },                             // 持有內容
                                { InvokeCondition.COUNT, values.Length },                      // 陣列長度
                            };

                            // 異動能否包含自幾
                            bool isContainSelf = permissions.IsAllow(propertyName, null, AccessRuleRightFlags.Self);
                            // 是否可以進行異動: 只有在能異動的情況下進行動作
                            if (isEditable || isContainSelf)
                            {
                                // 特殊旗標: 添加是否能夠新增或移除自己
                                protocolAttributeFlags |= isContainSelf ? ProtocolAttributeFlags.SELF : ProtocolAttributeFlags.NONE;

                                // 添加是否能夠新增或移除自己以外的項目
                                protocolAttributeFlags |= isEditable ? ProtocolAttributeFlags.EDITABLE : ProtocolAttributeFlags.NONE;
                                // 添加異動項目處理類型
                                dictionaryProtocolWithDetailInside.Add(InvokeCondition.RECEIVEDTYPE, typeof(string).Name);

                                // 增加目標類型限制
                                protocolAttributeFlags |= ProtocolAttributeFlags.CATEGORYLIMITED;
                                // 限制應提供的類型: 群組或成員
                                dictionaryProtocolWithDetailInside.Add(InvokeCondition.CATEGORYLIMITED, CategoryTypes.GROUP | CategoryTypes.PERSON);
                            }

                            // 新增屬性描述
                            invokeCondition = new InvokeCondition(protocolAttributeFlags, dictionaryProtocolWithDetailInside);
                        }
                        break;
                    #endregion
                    #region 隸屬群組: 協議
                    case Properties.P_MEMBEROF:
                        {
                            // 取得介面
                            IRevealerMemberOf revealerMemberOf = destination as IRevealerMemberOf;
                            // 介面不存在時代表此權限不必提供
                            if (revealerMemberOf == null)
                            {
                                // 跳過此處理
                                continue;
                            }

                            // 嘗試轉換成目標介面並取得內容
                            LDAPRelationship[] values = revealerMemberOf.Elements;
                            // 隸屬群組的預期項目: 必定持有自定項目, 由於隸屬群組僅會持有可讀權限, 所以可讀的狀況下就可寫
                            ProtocolAttributeFlags protocolAttributeFlags = ProtocolAttributeFlags.HASVALUE | ProtocolAttributeFlags.ISARRAY;
                            // 宣告持有內容
                            Dictionary<string, object> dictionaryProtocolWithDetailInside = new Dictionary<string, object>
                            {
                                { InvokeCondition.STOREDTYPE, typeof(LDAPRelationship).Name }, // 持有內容描述
                                { InvokeCondition.VALUE, values },                             // 持有內容
                                { InvokeCondition.COUNT, values.Length },                      // 陣列長度
                            };

                            // 增加可以編譯
                            protocolAttributeFlags |= ProtocolAttributeFlags.EDITABLE;
                            // 異動時應提供的資料類型
                            dictionaryProtocolWithDetailInside.Add(InvokeCondition.RECEIVEDTYPE, typeof(string).Name);

                            // 增加目標類型限制
                            protocolAttributeFlags |= ProtocolAttributeFlags.CATEGORYLIMITED;
                            // 限制應提供的類型: 群組
                            dictionaryProtocolWithDetailInside.Add(InvokeCondition.CATEGORYLIMITED, CategoryTypes.GROUP);

                            // 新增屬性描述: 
                            invokeCondition = new InvokeCondition(protocolAttributeFlags, dictionaryProtocolWithDetailInside);
                        }
                        break;
                    #endregion
                    #region 使用者帳號控制: 使用整合旗標<LDAPMethods.CW_USERACCOUNTCONTROL> 作為協議
                    case Properties.P_USERACCOUNTCONTROL:
                        {
                            // 目前持有的資訊內容
                            AccountControlFlags storedValue = destination.StoredProperties.GetPropertySingle<AccountControlFlags>(propertyName);

                            // 將儲存的資料轉換成對外協議
                            AccountControlProtocols accountControlProtocols = AccountControlFromFlagsToProtocols(storedValue);

                            // 預期項目: 必定是列舉
                            Type typeEnum = typeof(AccountControlProtocols);
                            // 至此必定可察看數值列舉, 需要額外描述
                            ProtocolAttributeFlags protocolAttributeFlags = ProtocolAttributeFlags.HASVALUE | ProtocolAttributeFlags.ISFLAGS | ProtocolAttributeFlags.COMBINE;
                            // 宣告持有內容: 由於宣告為字串類型, 所以儲存與修改時需求的都會是字串
                            Dictionary<string, object> dictionaryProtocolWithDetail = new Dictionary<string, object>()
                            {
                                { InvokeCondition.STOREDTYPE, typeEnum.Name },                     // 持有內容描述
                                { InvokeCondition.VALUE, accountControlProtocols },                // 持有內容
                                { InvokeCondition.COMBINETAG, Methods.CT_USERACCOUNTCONTROL }, // 與其他持有此整合旗標的物件視為同一區塊作處理
                                { InvokeCondition.FLAGMASK, ACOUNTCONTROL_MASK },                  // 提供陳列用的遮罩
                            };

                            // 若可以編譯
                            if (isEditable)
                            {
                                // 增加可以編譯
                                protocolAttributeFlags |= ProtocolAttributeFlags.EDITABLE;
                                // 異動時應提供的資料類型
                                dictionaryProtocolWithDetail.Add(InvokeCondition.RECEIVEDTYPE, typeEnum.Name);
                            }

                            // 新增屬性描述: 
                            invokeCondition = new InvokeCondition(protocolAttributeFlags, dictionaryProtocolWithDetail);
                        }
                        break;
                    #endregion
                    #region 密碼最後設置時間: 使用整合旗標<LDAPMethods.CW_USERACCOUNTCONTROL> 作為協議
                    case Properties.P_PWDLASTSET:
                        {
                            // 目前持有的資訊內容
                            long storedValue = destination.StoredProperties.GetPropertyInterval(propertyName);

                            /* 由於處理邏輯所以數值將以下列方式轉換成旗標
                                 - 數值為 0: 啟用下次登入時需修改密碼
                                 - 數值非 0: 關閉下次登入時需修改密碼
                            */
                            AccountControlProtocols accountControlProtocols = storedValue == 0 ? AccountControlProtocols.PWD_CHANGE_NEXTLOGON : AccountControlProtocols.NONE;

                            // 預期項目: 必定是列舉
                            Type typeEnum = typeof(AccountControlProtocols);
                            // 至此必定可察看數值列舉, 需要額外描述
                            ProtocolAttributeFlags protocolAttributeFlags = ProtocolAttributeFlags.HASVALUE | ProtocolAttributeFlags.ISFLAGS | ProtocolAttributeFlags.COMBINE;
                            // 宣告持有內容: 由於宣告為字串類型, 所以儲存與修改時需求的都會是字串
                            Dictionary<string, object> dictionaryProtocolWithDetail = new Dictionary<string, object>()
                            {
                                { InvokeCondition.STOREDTYPE, typeEnum.Name },                              // 持有內容描述
                                { InvokeCondition.VALUE, accountControlProtocols },                         // 持有內容
                                { InvokeCondition.COMBINETAG, Methods.CT_USERACCOUNTCONTROL },          // 與其他持有此整合旗標的物件視為同一區塊作處理
                                { InvokeCondition.FLAGMASK, AccountControlProtocols.PWD_CHANGE_NEXTLOGON }, // 提供陳列用的遮罩
                            };

                            // 若可以編譯
                            if (isEditable)
                            {
                                // 增加可以編譯
                                protocolAttributeFlags |= ProtocolAttributeFlags.EDITABLE;
                                // 異動時應提供的資料類型
                                dictionaryProtocolWithDetail.Add(InvokeCondition.RECEIVEDTYPE, typeEnum.Name);
                            }

                            // 新增屬性描述: 
                            invokeCondition = new InvokeCondition(protocolAttributeFlags, dictionaryProtocolWithDetail);
                        }
                        break;
                    #endregion
                    #region 支援的連線加密格式: 使用整合旗標<LDAPMethods.CW_USERACCOUNTCONTROL> 作為協議
                    case Properties.P_SUPPORTEDENCRYPTIONTYPES:
                        {
                            // 目前持有的資訊內容
                            EncryptedType storedValue = destination.StoredProperties.GetPropertySingle<EncryptedType>(propertyName);

                            // 初始化
                            AccountControlProtocols accountControlProtocols = AccountControlProtocols.NONE;
                            // 轉換 AES128 支援狀態
                            accountControlProtocols |= (storedValue & EncryptedType.AES128) == EncryptedType.AES128 ? AccountControlProtocols.ACCOUNT_KERBEROS_AES128 : AccountControlProtocols.NONE;
                            // 轉換 AES256 支援狀態
                            accountControlProtocols |= (storedValue & EncryptedType.AES256) == EncryptedType.AES256 ? AccountControlProtocols.ACCOUNT_KERBEROS_AES256 : AccountControlProtocols.NONE;

                            // 預期項目: 必定是列舉
                            Type typeEnum = typeof(AccountControlProtocols);
                            // 至此必定可察看數值列舉, 需要額外描述
                            ProtocolAttributeFlags protocolAttributeFlags = ProtocolAttributeFlags.HASVALUE | ProtocolAttributeFlags.ISFLAGS | ProtocolAttributeFlags.COMBINE;
                            // 宣告持有內容: 由於宣告為字串類型, 所以儲存與修改時需求的都會是字串
                            Dictionary<string, object> dictionaryProtocolWithDetail = new Dictionary<string, object>()
                            {
                                { InvokeCondition.STOREDTYPE, typeEnum.Name },                     // 持有內容描述
                                { InvokeCondition.VALUE, accountControlProtocols },                // 持有內容
                                { InvokeCondition.COMBINETAG, Methods.CT_USERACCOUNTCONTROL }, // 與其他持有此整合旗標的物件視為同一區塊作處理
                                { InvokeCondition.FLAGMASK, ENCRYPT_PROTOCOLMASK },                // 提供陳列用的遮罩
                            };

                            // 若可以編譯
                            if (isEditable)
                            {
                                // 增加可以編譯
                                protocolAttributeFlags |= ProtocolAttributeFlags.EDITABLE;
                                // 異動時應提供的資料類型
                                dictionaryProtocolWithDetail.Add(InvokeCondition.RECEIVEDTYPE, typeEnum.Name);
                            }

                            // 新增屬性描述: 
                            invokeCondition = new InvokeCondition(protocolAttributeFlags, dictionaryProtocolWithDetail);
                        }
                        break;
                    #endregion
                    #region 採用字串處理的項目(預設): 協議
                    // 其他項目: 目前僅有處理字串的項目
                    default:
                        {
                            // 目前持有的資訊內容
                            string storedValue = destination.StoredProperties.GetPropertySingle<string>(propertyName);

                            // 預期項目: 必定是字串
                            Type typeString = typeof(string);
                            // 至此必定可察看數值
                            ProtocolAttributeFlags protocolAttributeFlags = ProtocolAttributeFlags.HASVALUE;
                            // 宣告持有內容: 由於宣告為字串類型, 所以儲存與修改時需求的都會是字串
                            Dictionary<string, object> dictionaryProtocolWithDetailInside = new Dictionary<string, object>()
                            {
                                { InvokeCondition.STOREDTYPE, typeString.Name },        // 持有內容描述
                                { InvokeCondition.VALUE, storedValue ?? string.Empty }, // 持有內容
                            };

                            // 必須根據能否寫入決定是否添加異動旗標
                            if (isEditable)
                            {
                                // 增加可以編譯
                                protocolAttributeFlags |= ProtocolAttributeFlags.EDITABLE;
                                // 異動時應提供的資料類型
                                dictionaryProtocolWithDetailInside.Add(InvokeCondition.RECEIVEDTYPE, typeString.Name);
                            }

                            // 新增屬性描述: 
                            invokeCondition = new InvokeCondition(protocolAttributeFlags, dictionaryProtocolWithDetailInside);
                        }
                        break;
                        #endregion
                }

                // 簡易防呆: 屬性描述不得為空
                if (invokeCondition == null)
                {
                    // 跳過
                    continue;
                }

                // 添加至額外元素
                dictionaryAttributesNameWithCondition.Add(propertyName, invokeCondition);
            }
            #endregion

            // 沒有任何可以對外回傳的項目
            if (dictionaryAttributesNameWithCondition.Count == 0)
            {
                return (false, null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 不具有任何可以異動的屬性權限");
            }

            /* 一般需求參數限制如下所述:
                 - 設置持有元素資料
            */
            const ProtocolAttributeFlags commonFlags = ProtocolAttributeFlags.ELEMENTS;
            // 需求內容: 採用封盒動作
            Dictionary<string, object> dictionaryProtocolWithDetailOutside = new Dictionary<string, object> {
                { InvokeCondition.ELEMENTS, dictionaryAttributesNameWithCondition }
            };

            // 持有項目時就外部就能夠異動
            return (true, new InvokeCondition(commonFlags, dictionaryProtocolWithDetailOutside), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol)
        {
            // 紀錄處理目標的區分名稱
            string distinguishedNameDestination = destination.DistinguishedName;
            // 紀錄喚起者的區分名稱
            string distinguishedNameInvoker = invoker.DistinguishedName;

            // 應存在修改目標
            if (certification.GetEntry(distinguishedNameDestination) == null)
            {
                // 若觸發此處例外必定為程式漏洞
                throw new LDAPExceptions($"類型:{destination.Type} 的物件:{distinguishedNameDestination} 於異動細節內容時發現不存在目標入口物件, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 轉換異動項目
            Dictionary<string, JToken> dictionaryAttributeNameWithDetail = protocol?.ToObject<Dictionary<string, JToken>>() ?? new Dictionary<string, JToken>();
            // 應存在修改目標
            if (dictionaryAttributeNameWithDetail == null || dictionaryAttributeNameWithDetail.Count == 0)
            {
                // 若觸發此處例外: 則有可能遭受網路攻擊
                throw new LDAPExceptions($"類型:{destination.Type} 的物件:{distinguishedNameDestination} 於異動細節內容時發現傳輸的協議:{protocol} 不符合規則, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
            }

            // 整合各 SID 權向狀態
            LDAPPermissions permissions = GetPermissions(certification.Dispatcher, invoker, destination);
            // 紀錄未處理的項目
            Dictionary<string, string> dictionaryFailureAttributeNameWithMessage = new Dictionary<string, string>(dictionaryAttributeNameWithDetail.Count);
            // 遍歷所有需求的項目是否有在支援列表內
            foreach (string propertyName in PropertiesNames)
            {
                // 不在支援項目
                if (!dictionaryAttributeNameWithDetail.TryGetValue(propertyName, out JToken receivedValue))
                {
                    // 跳過
                    continue;
                }

                // 是否可異動
                bool isEditable = permissions.IsAllow(propertyName, null, AccessRuleRightFlags.PropertyWrite);
                // 使用存取鍵值去處理
                switch (propertyName)
                {
                    #region 成員: 驗證
                    case Properties.P_MEMBER:
                        {
                            // 檢查目標物件能否取得隸屬群組介面
                            IRevealerMember revealerMember = destination as IRevealerMember;
                            // 無法取得
                            if (revealerMember == null)
                            {
                                // 若觸發此處例外: 則有可能遭受網路攻擊
                                throw new LDAPExceptions($"類型:{destination.Type} 的物件:{distinguishedNameDestination} 於異動細節內容時發現傳輸的異動協議:{propertyName} 不能套用至目標物件, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                            }

                            // 透過目前持有項目與接收到的協議
                            string[] unprocessedDNs = CombineStoredAndReceivedDNs(
                                revealerMember.Elements,                                      // 目前持有的成員
                                receivedValue?.ToObject<string[]>() ?? Array.Empty<string>(), // 接收到的協議
                                out Dictionary<string, bool> dictionaryDNWithIsRemove         // 本次應進行的異動
                            );

                            // 含有無法處理的項目
                            if (unprocessedDNs.Length != 0)
                            {
                                // 推入無法處理描述中
                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"因項目:{string.Join(",", unprocessedDNs)} 不符合存取規則而無法進行類型:{destination.Type} 物件:{distinguishedNameDestination} 的參數:{propertyName} 異動細節");
                                // 跳過下方的實際異動
                                continue;
                            }

                            // 紀錄需重新找尋的項目
                            List<string> researchDNHashSet = new List<string>(dictionaryDNWithIsRemove.Count);
                            // 檢查協定中有那些項目是新增的
                            foreach (string distinguishedName in dictionaryDNWithIsRemove.Keys)
                            {
                                // 如果之前已推入過異動
                                if (certification.GetEntry(distinguishedName) != null)
                                {
                                    // 不必推入重新找尋項目
                                    continue;
                                }

                                // 不包含時推入需重新找尋項目
                                researchDNHashSet.Add(distinguishedName);
                            }

                            // 紀錄不能處理的項目
                            List<string> unprocessedDNHashSet = new List<string>(dictionaryDNWithIsRemove.Count);
                            // 重新找尋項目存在長度
                            if (researchDNHashSet.Count != 0)
                            {
                                // 取得根目錄
                                using (DirectoryEntry entryRoot = certification.Dispatcher.DomainRoot())
                                {
                                    // 找到須限制的物件類型
                                    Dictionary<CategoryTypes, string> dictionaryLimitedCategory = LDAPCategory.GetValuesByTypes(CategoryTypes.GROUP | CategoryTypes.PERSON);
                                    /*轉換成實際過濾字串: 取得符合下述所有條件的物件
                                        - 限制只找尋群組或成員
                                        - 限制只找尋特定區分名稱
                                        [TODO] 應使用加密字串避免注入式攻擊
                                    */
                                    string encoderFiliter = $"(&{LDAPConfiguration.GetORFiliter(Properties.C_OBJECTCATEGORY, dictionaryLimitedCategory.Values)}{LDAPConfiguration.GetORFiliter(Properties.C_DISTINGGUISHEDNAME, researchDNHashSet)})";
                                    // 應從根目錄進行搜尋
                                    using (DirectorySearcher seacher = new DirectorySearcher(entryRoot, encoderFiliter, LDAPObject.PropertiesToLoad))
                                    {
                                        // 取得所有項目
                                        using (SearchResultCollection all = seacher.FindAll())
                                        {
                                            // 應找尋項目長度不吻合
                                            if (all.Count != researchDNHashSet.Count)
                                            {
                                                // 推入無法處理描述中
                                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"因項目:{string.Join(",", researchDNHashSet)} 有部分非需求類型無法進行類型:{destination.Type} 物件:{distinguishedNameDestination} 的參數:{propertyName} 異動細節");
                                                // 跳過下方的實際異動
                                                continue;
                                            }

                                            // 異動能否包含自身
                                            bool isContainSelf = permissions.IsAllow(propertyName, null, AccessRuleRightFlags.Self);
                                            // 遍歷所有項目轉換成入口物件
                                            foreach (SearchResult one in all)
                                            {
                                                // 取得區分名稱
                                                string distinguishedName = LDAPConfiguration.ParseSingleValue<string>(Properties.C_DISTINGGUISHEDNAME, one.Properties);
                                                /* 根據異動目標判斷異動權限是否不可用
                                                     1. 異動資料是自己, 不包含異動自己的權限
                                                     2. 異動資料不是自己, 不包含異動的權限
                                                */
                                                if (distinguishedName == distinguishedNameInvoker ? !isContainSelf : !isEditable)
                                                {
                                                    // 推入至無法處理項目
                                                    unprocessedDNHashSet.Add(distinguishedName);
                                                }
                                                else
                                                {
                                                    // 設定並轉換成入口物件
                                                    certification.SetEntry(one, distinguishedName);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // 含有無法處理的項目
                            if (unprocessedDNHashSet.Count != 0)
                            {
                                // 推入無法處理描述中
                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"因項目:{string.Join(",", unprocessedDNHashSet)} 不符合存取權限而無法進行類型:{destination.Type} 物件:{distinguishedNameDestination} 的參數:{propertyName} 異動細節");
                                // 跳過下方的實際異動
                                continue;
                            }
                        }
                        break;
                    #endregion
                    #region 隸屬群組: 驗證
                    case Properties.P_MEMBEROF:
                        {
                            // 檢查目標物件能否取得隸屬群組介面
                            IRevealerMemberOf revealerMemberOf = destination as IRevealerMemberOf;
                            // 無法取得
                            if (revealerMemberOf == null)
                            {
                                // 若觸發此處例外: 則有可能遭受網路攻擊
                                throw new LDAPExceptions($"類型:{destination.Type} 的物件:{distinguishedNameDestination} 於異動細節內容時發現傳輸的異動協議:{propertyName} 不能套用至目標物件, 請聯絡程式維護人員", ErrorCodes.LOGIC_ERROR);
                            }

                            // 透過目前持有項目與接收到的協議
                            string[] unprocessedDNs = CombineStoredAndReceivedDNs(
                                revealerMemberOf.Elements,                                    // 目前持有的隸屬組織
                                receivedValue?.ToObject<string[]>() ?? Array.Empty<string>(), // 接收到的協議
                                out Dictionary<string, bool> dictionaryDNWithIsRemove         // 本次應進行的異動
                            );

                            // 含有無法處理的項目
                            if (unprocessedDNs.Length != 0)
                            {
                                // 推入無法處理描述中
                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"因項目:{string.Join(",", unprocessedDNs)} 不符合項目而無法進行類型:{destination.Type} 物件:{distinguishedNameDestination} 的參數:{propertyName} 異動細節");
                                // 跳過下方的實際異動
                                continue;
                            }

                            // 紀錄需重新找尋的項目
                            List<string> researchDNHashSet = new List<string>(dictionaryDNWithIsRemove.Count);
                            // 檢查協定中有那些項目是新增的
                            foreach (string distinguishedName in dictionaryDNWithIsRemove.Keys)
                            {
                                // 如果之前已推入過異動
                                if (certification.GetEntry(distinguishedName) != null)
                                {
                                    // 不必推入重新找尋項目
                                    continue;
                                }

                                // 不包含時推入需重新找尋項目
                                researchDNHashSet.Add(distinguishedName);
                            }

                            // 重新找尋項目存在長度
                            if (researchDNHashSet.Count != 0)
                            {
                                // 取得根目錄
                                using (DirectoryEntry entryRoot = certification.Dispatcher.DomainRoot())
                                {
                                    // 找到須限制的物件類型
                                    Dictionary<CategoryTypes, string> dictionaryLimitedCategory = LDAPCategory.GetValuesByTypes(CategoryTypes.GROUP);
                                    /*轉換成實際過濾字串: 取得符合下述所有條件的物件
                                        - 限制只找尋群組
                                        - 限制只找尋特定區分名稱
                                        [TODO] 應使用加密字串避免注入式攻擊
                                    */
                                    string encoderFiliter = $"(&{LDAPConfiguration.GetORFiliter(Properties.C_OBJECTCATEGORY, dictionaryLimitedCategory.Values)}{LDAPConfiguration.GetORFiliter(Properties.C_DISTINGGUISHEDNAME, researchDNHashSet)})";
                                    // 應從根目錄進行搜尋
                                    using (DirectorySearcher seacher = new DirectorySearcher(entryRoot, encoderFiliter, LDAPObject.PropertiesToLoad))
                                    {
                                        // 取得所有項目
                                        using (SearchResultCollection all = seacher.FindAll())
                                        {
                                            // 應找尋項目長度不吻合
                                            if (all.Count != researchDNHashSet.Count)
                                            {
                                                // 推入無法處理描述中
                                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"因項目:{string.Join(",", researchDNHashSet)} 有部分非需求類型無法進行類型:{destination.Type} 物件:{distinguishedNameDestination} 的參數:{propertyName} 異動細節");
                                                // 跳過下方的實際異動
                                                continue;
                                            }

                                            // 遍歷所有項目轉換成入口物件
                                            foreach (SearchResult one in all)
                                            {
                                                // 取得區分名稱
                                                string distinguishedName = LDAPConfiguration.ParseSingleValue<string>(Properties.C_DISTINGGUISHEDNAME, one.Properties);
                                                // 設定並轉換成入口物件
                                                certification.SetEntry(one, distinguishedName);
                                            }
                                        }
                                    }
                                }
                            }

                            // 紀錄不能處理的項目
                            List<string> unprocessedDNHashSet = new List<string>(dictionaryDNWithIsRemove.Count);
                            // 遍歷所有項目: 檢查權限是否足夠
                            foreach (KeyValuePair<string, bool> pairMemberOfProcessed in dictionaryDNWithIsRemove)
                            {
                                // 取得將影響 Member 欄位的群組
                                RequiredCommitSet set = certification.GetEntry(pairMemberOfProcessed.Key);
                                // 簡易防呆
                                if (set == null)
                                {
                                    // 跳過
                                    continue;
                                }

                                // 轉換成基礎物件
                                LDAPObject entryObject = LDAPObject.ToObject(set.Entry, certification.Dispatcher, set.Properties);

                                // 整合各 SID 權向狀態
                                LDAPPermissions permissionsProtocol = GetPermissions(certification.Dispatcher, invoker, entryObject);
                                // 是否可異動
                                bool isProcessedEditable = permissionsProtocol.IsAllow(Properties.P_MEMBER, null, AccessRuleRightFlags.PropertyWrite);
                                // 異動能否包含自身
                                bool isProcessedContainSelf = permissionsProtocol.IsAllow(Properties.P_MEMBER, null, AccessRuleRightFlags.Self); 
                                /* 根據異動目標判斷異動權限是否不可用
                                     1. 異動資料是自己, 不包含異動自己的權限
                                     2. 異動資料不是自己, 不包含異動的權限
                                */
                                if (pairMemberOfProcessed.Key == distinguishedNameInvoker ? !isProcessedContainSelf : !isProcessedEditable)
                                {
                                    // 推入至無法處理項目
                                    unprocessedDNHashSet.Add(pairMemberOfProcessed.Key);
                                }
                            }

                            // 含有無法處理的項目
                            if (unprocessedDNHashSet.Count != 0)
                            {
                                // 推入無法處理描述中
                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"因項目:{string.Join(",", unprocessedDNHashSet)} 不符合存取權限而無法進行類型:{destination.Type} 物件:{distinguishedNameDestination} 的參數:{propertyName} 異動細節");
                                // 跳過下方的實際異動
                                continue;
                            }
                        }
                        break;
                    #endregion
                    #region 使用者帳號控制: 驗證
                    case Properties.P_USERACCOUNTCONTROL:
                        {
                            // 若無法編輯
                            if (!isEditable)
                            {
                                // 推入無法處理描述中
                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"參數:{propertyName} 因不具有異動權限而無法對類型:{destination.Type} 物件:{distinguishedNameDestination} 進行異動細節");
                                // 跳過下方的實際異動
                                continue;
                            }

                            // 轉換成控制旗標
                            AccountControlProtocols convertedProtocol = receivedValue?.ToObject<AccountControlProtocols>() ?? AccountControlProtocols.NONE;
                            // 此參數不可包含可用的參數之外的調整
                            bool isExsitControl = (convertedProtocol & ~ACOUNTCONTROL_MASK) != AccountControlProtocols.NONE;
                            // 目前持有的資訊內容
                            if (isExsitControl)
                            {
                                // 推入無法處理描述中
                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"參數:{propertyName} 因異動內容含有非法異動而無法對類型:{destination.Type} 物件:{distinguishedNameDestination} 進行異動細節");
                                // 跳過此處理
                                continue;
                            }

                            /* 注意: 設置同時存在幾種組合時必須使用特例處理
                                 組合:
                                      1. PWD_CHANGE_NEXTLOGON, 
                                      2. PWD_DISABLE_CHANGE
                                   僅保留: PWD_CHANGE_NEXTLOGON
                                 組合:
                                      1. PWD_CHANGE_NEXTLOGON, 
                                      2. PWD_ENABLE_FOREVER
                                   僅保留: PWD_ENABLE_FOREVER
                                 上述動作是 AD 於此種組合時的動作
                                 系統處理:
                                   驗證時不做過濾, 直接按照此邏輯進行設置
                            */
                        }
                        break;
                    #endregion
                    #region 使用者帳號控制: 驗證
                    case Properties.P_PWDLASTSET:
                        {
                            // 若無法編輯
                            if (!isEditable)
                            {
                                // 推入無法處理描述中
                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"參數:{propertyName} 因不具有異動權限而無法對類型:{destination.Type} 物件:{distinguishedNameDestination} 進行異動細節");
                                // 跳過下方的實際異動
                                continue;
                            }

                            // 轉換成控制旗標
                            AccountControlProtocols convertedProtocol = receivedValue?.ToObject<AccountControlProtocols>() ?? AccountControlProtocols.NONE;
                            // 此參數不可包含可用的參數之外的調整
                            bool isExsitControl = (convertedProtocol & ~AccountControlProtocols.PWD_CHANGE_NEXTLOGON) != AccountControlProtocols.NONE;
                            // 目前持有的資訊內容
                            if (isExsitControl)
                            {
                                // 推入無法處理描述中
                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"參數:{propertyName} 因異動內容含有非法異動而無法對類型:{destination.Type} 物件:{distinguishedNameDestination} 進行異動細節");
                                // 跳過此處理
                                continue;
                            }
                        }
                        break;
                    #endregion
                    #region 支援的連線加密格式: 驗證
                    case Properties.P_SUPPORTEDENCRYPTIONTYPES:
                        {
                            // 若無法編輯
                            if (!isEditable)
                            {
                                // 推入無法處理描述中
                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"參數:{propertyName} 因不具有異動權限而無法對類型:{destination.Type} 物件:{distinguishedNameDestination} 進行異動細節");
                                // 跳過下方的實際異動
                                continue;
                            }

                            // 轉換成控制旗標
                            AccountControlProtocols convertedProtocol = receivedValue?.ToObject<AccountControlProtocols>() ?? AccountControlProtocols.NONE;
                            // 此參數不可包含可用的參數之外的調整
                            bool isExsitControl = (convertedProtocol & ~ENCRYPT_PROTOCOLMASK) != AccountControlProtocols.NONE;
                            // 目前持有的資訊內容
                            if (isExsitControl)
                            {
                                // 推入無法處理描述中
                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"參數:{propertyName} 因異動內容含有非法異動而無法對類型:{destination.Type} 物件:{distinguishedNameDestination} 進行異動細節");
                                // 跳過此處理
                                continue;
                            }
                        }
                        break;
                    #endregion
                    #region 採用字串處理的項目(預設): 協議
                    default:
                        {
                            // 若無法編輯
                            if (!isEditable)
                            {
                                // 推入無法處理描述中
                                dictionaryFailureAttributeNameWithMessage.Add(propertyName, $"參數:{propertyName} 因不具有異動權限而無法對類型:{destination.Type} 物件:{distinguishedNameDestination} 進行異動細節");
                                // 跳過下方的實際異動
                                continue;
                            }
                        }
                        break;
                        #endregion
                }
            }

            // 不具長度時才是呼叫成功
            return dictionaryFailureAttributeNameWithMessage.Count == 0;
        }

        internal override void Invoke(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol)
        {
            // 紀錄目標的區分名稱
            string distinguishedNameDestination = destination.DistinguishedName;
            // 取得修改目標的入口物件
            RequiredCommitSet setDestination = certification.GetEntry(destination.DistinguishedName);
            // 簡易防呆: 應於驗證處完成檢驗
            if (setDestination == null)
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 轉換異動項目
            Dictionary<string, JToken> dictionaryAttributeNameWithDetail = protocol?.ToObject<Dictionary<string, JToken>>() ?? new Dictionary<string, JToken>();
            // 簡易防呆: 應於驗證處完成檢驗
            if (dictionaryAttributeNameWithDetail == null || dictionaryAttributeNameWithDetail.Count == 0)
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 遍歷所有需求的項目是否有在支援列表內
            foreach (string propertyName in PropertiesNames)
            {
                // 不在支援項目
                if (!dictionaryAttributeNameWithDetail.TryGetValue(propertyName, out JToken receivedValue))
                {
                    // 跳過: 觸發此處例外必定為程式漏洞
                    continue;
                }

                // 使用存取鍵值去處理
                switch (propertyName)
                {
                    #region 成員: 實際修改
                    case Properties.P_MEMBER:
                        {
                            // 檢查目標物件能否取得隸屬群組介面
                            IRevealerMember revealerMember = destination as IRevealerMember;
                            // 無法取得
                            if (revealerMember == null)
                            {
                                // 應於檢驗處理完成, 此處不應進入
                                continue;
                            }

                            // 透過目前持有項目與接收到的協議
                            CombineStoredAndReceivedDNs(
                                revealerMember.Elements,                                      // 目前持有的成員
                                receivedValue?.ToObject<string[]>() ?? Array.Empty<string>(), // 接收到的協議
                                out Dictionary<string, bool> dictionaryDNWithIsRemove         // 本次應進行的異動
                            );

                            // 存在異動時
                            if (dictionaryDNWithIsRemove.Count != 0)
                            {
                                // 遍歷所有項目: 進行異動
                                foreach (KeyValuePair<string, bool> pairMemberProcessed in dictionaryDNWithIsRemove)
                                {
                                    // 取得將影響 Member 欄位的群組
                                    RequiredCommitSet setProcessed = certification.GetEntry(pairMemberProcessed.Key);
                                    // 簡易防呆: 物件不存在
                                    if (setProcessed == null)
                                    {
                                        // 不可能發生, 但做個邏輯上的簡易防呆
                                        continue;
                                    }

                                    // 是否為移除
                                    if (pairMemberProcessed.Value)
                                    {
                                        // 喚起移除動作
                                        setDestination.Entry.Properties[Properties.P_MEMBER].Remove(pairMemberProcessed.Key);
                                    }
                                    else
                                    {
                                        // 喚起新增動作
                                        setDestination.Entry.Properties[Properties.P_MEMBER].Add(pairMemberProcessed.Key);
                                    }

                                    // 推入此物件作為異動後影響項目
                                    setProcessed.ReflashRequired();
                                }

                                // 設定需求推入實作
                                setDestination.CommitRequired();
                            }
                        }
                        break;
                    #endregion
                    #region 隸屬群組: 實際修改
                    case Properties.P_MEMBEROF:
                        {
                            // 檢查目標物件能否取得隸屬群組介面
                            IRevealerMemberOf revealerMemberOf = destination as IRevealerMemberOf;
                            // 無法取得
                            if (revealerMemberOf == null)
                            {
                                // 應於檢驗處理完成, 此處不應進入
                                continue;
                            }

                            // 透過目前持有項目與接收到的協議
                            CombineStoredAndReceivedDNs(
                                revealerMemberOf.Elements,                                    // 目前持有的隸屬組織
                                receivedValue?.ToObject<string[]>() ?? Array.Empty<string>(), // 接收到的協議
                                out Dictionary<string, bool> dictionaryDNWithIsRemove         // 本次應進行的異動
                            );

                            // 存在異動時
                            if (dictionaryDNWithIsRemove.Count != 0)
                            {
                                // 遍歷所有項目: 開始產生實際的異動
                                foreach (KeyValuePair<string, bool> pairMemberOfProcessed in dictionaryDNWithIsRemove)
                                {
                                    // 取得將影響 Member 欄位的群組
                                    RequiredCommitSet setProcessed = certification.GetEntry(pairMemberOfProcessed.Key);
                                    // 簡易防呆: 物件不存在
                                    if (setProcessed == null)
                                    {
                                        // 不可能發生, 但做個邏輯上的簡易防呆
                                        continue;
                                    }

                                    // 是否為移除
                                    if (pairMemberOfProcessed.Value)
                                    {
                                        // 喚起移除動作
                                        setProcessed.Entry.Properties[Properties.P_MEMBER].Remove(distinguishedNameDestination);
                                    }
                                    else
                                    {
                                        // 喚起新增動作
                                        setProcessed.Entry.Properties[Properties.P_MEMBER].Add(distinguishedNameDestination);
                                    }

                                    // 此時目標物件必定有造成異動
                                    setProcessed.CommitRequired();
                                }

                                // 設定需求刷新
                                setDestination.ReflashRequired();
                            }
                        }
                        break;
                    #endregion
                    #region 使用者帳號控制: 實際修改
                    case Properties.P_USERACCOUNTCONTROL:
                        {
                            // 轉換成控制旗標
                            AccountControlProtocols convertedProtocol = receivedValue?.ToObject<AccountControlProtocols>() ?? AccountControlProtocols.NONE;
                            // 將對外協議換成內部設置的旗標
                            AccountControlFlags accountControlFlags = AccountControlFromProtocolsToFlags(convertedProtocol);

                            // 取得這個參數可調整的參數
                            AccountControlFlags accountControlFlagsMask = AccountControlFromProtocolsToFlags(ACOUNTCONTROL_MASK);
                            // 取得目前持有的屬性
                            AccountControlFlags storedValue = destination.StoredProperties.GetPropertySingle<AccountControlFlags>(propertyName);
                            // 檢查是否有異動
                            if ((storedValue & accountControlFlagsMask) == accountControlFlags)
                            {
                                // 無異動, 跳過
                                continue;
                            }

                            // 將不支援調整個參數保留
                            storedValue &= ~accountControlFlagsMask;
                            // 調整本次須異動的資料
                            storedValue |= accountControlFlags;

                            // 喚起設置動作
                            setDestination.Entry.Properties[propertyName].Value = storedValue;
                            // 此時自身必定有造成異動
                            setDestination.CommitRequired();
                        }
                        break;
                    #endregion
                    #region 密碼最後設置時間: 實際修改
                    case Properties.P_PWDLASTSET:
                        {
                            // 轉換成控制旗標
                            AccountControlProtocols convertedProtocol = receivedValue?.ToObject<AccountControlProtocols>() ?? AccountControlProtocols.NONE;
                            // 取得目前持有的屬性
                            long storedValue = destination.StoredProperties.GetPropertyInterval(propertyName);
                            // 取得本次是否包含使用者照號一棟
                            if (dictionaryAttributeNameWithDetail.TryGetValue(Properties.P_USERACCOUNTCONTROL, out JToken userAccountControlJToken))
                            {
                                // 混和與疊加控制全縣
                                convertedProtocol |= userAccountControlJToken?.ToObject<AccountControlProtocols>() ?? AccountControlProtocols.NONE;
                            }
                            else
                            {
                                // 取得目前持有的使用者帳號控制屬性
                                AccountControlFlags userAccountControlValue = destination.StoredProperties.GetPropertySingle<AccountControlFlags>(Properties.P_USERACCOUNTCONTROL);
                                // 轉換成外部協議可是別的格式
                                convertedProtocol |= AccountControlFromFlagsToProtocols(userAccountControlValue);
                            }

                            // 製作中和旗標拿來做判斷
                            const AccountControlProtocols PWD_CHANGE_AND_FOREVER_MASK = AccountControlProtocols.PWD_CHANGE_NEXTLOGON | AccountControlProtocols.PWD_ENABLE_FOREVER;
                            /* 同時包含下述組合時:
                                      1. PWD_CHANGE_NEXTLOGON
                                      2. PWD_ENABLE_FOREVER
                                   僅保留: PWD_ENABLE_FOREVER
                               也就是說需亙具情況決定如何異動:
                            */
                            bool isMaskOn = (convertedProtocol & PWD_CHANGE_AND_FOREVER_MASK) == PWD_CHANGE_AND_FOREVER_MASK;
                            // 發現旗標包含了 PWD_CHANGE_NEXTLOGON 與 PWD_ENABLE_FOREVER, 若目前的 pwdLastSet 非 0, 則代表不必調整
                            if (isMaskOn && storedValue != 0)
                            {
                                // 跳過 pwdLastSet 的異動
                                continue;
                            }

                            /* 根據下述規則決定設置的數值
                                 1. 設置為 -1 (命令伺服器將此欄位調整呈現在時間)
                                    a. 下述情況皆吻和
                                      - 同時包含了下述權限
                                        1. PWD_CHANGE_NEXTLOGON
                                        2. PWD_ENABLE_FOREVER
                                      - 目前儲存數值是 0
                                    b. 本次異動設置了:  PWD_CHANGE_NEXTLOGON
                                 2. 設置成 0  (啟用 下次登入時必須重新設置密碼 )
                                    - 目前儲存資料非 0
                                    - 本次異動無設置:  PWD_CHANGE_NEXTLOGON
                            */
                            long modifiedPWDLastSet = ((isMaskOn && storedValue == 0) || ((convertedProtocol & AccountControlProtocols.PWD_CHANGE_NEXTLOGON) == AccountControlProtocols.PWD_CHANGE_NEXTLOGON)) ? -1 : 0;
                            // 與現在資料做比對
                            if (storedValue == 0 ? modifiedPWDLastSet == storedValue : modifiedPWDLastSet == -1)
                            {
                                // 相同跳過
                                continue;
                            }

                            // 喚起設置動作: 備註, 設置為 -1 會調整成伺服器的現在時間
                            setDestination.Entry.Properties[propertyName].Value = modifiedPWDLastSet;
                            // 此時自身必定有造成異動
                            setDestination.CommitRequired();
                        }
                        break;
                    #endregion
                    #region 支援的連線加密格式: 實際修改
                    case Properties.P_SUPPORTEDENCRYPTIONTYPES:
                        {
                            // 轉換成控制旗標
                            AccountControlProtocols convertedProtocol = receivedValue?.ToObject<AccountControlProtocols>() ?? AccountControlProtocols.NONE;

                            // 初始化
                            EncryptedType encryptedType = EncryptedType.NONE;
                            // 轉換 AES128 支援狀態
                            encryptedType |= (convertedProtocol & AccountControlProtocols.ACCOUNT_KERBEROS_AES128) == AccountControlProtocols.ACCOUNT_KERBEROS_AES128 ? EncryptedType.AES128 : EncryptedType.NONE;
                            // 轉換 AES256 支援狀態
                            encryptedType |= (convertedProtocol & AccountControlProtocols.ACCOUNT_KERBEROS_AES256) == AccountControlProtocols.ACCOUNT_KERBEROS_AES256 ? EncryptedType.AES256 : EncryptedType.NONE;

                            // 取得目前持有的屬性
                            EncryptedType storedValue = destination.StoredProperties.GetPropertySingle<EncryptedType>(propertyName);
                            // 檢查修改後設是否與現在鄉相同
                            if ((storedValue & ENCRYPT_MASK) == encryptedType)
                            {
                                // 跳過
                                continue;
                            }

                            // 將不支援調整個參數保留
                            storedValue &= ~ENCRYPT_MASK;
                            // 調整本次須異動的資料
                            storedValue |= encryptedType;

                            // 喚起設置動作
                            setDestination.Entry.Properties[propertyName].Value = storedValue;
                            // 此時自身必定有造成異動
                            setDestination.CommitRequired();
                        }
                        break;
                    #endregion
                    #region 採用字串處理的項目(預設): 實際修改
                    default:
                        {
                            // 取得轉換完成的數值
                            string convertedValue = receivedValue?.ToObject<string>() ?? string.Empty;
                            // 喚起設置動作
                            setDestination.Entry.Properties[propertyName].Value = string.IsNullOrEmpty(convertedValue) ? null : convertedValue;
                            // 此時自身必定有造成異動
                            setDestination.CommitRequired();
                        }
                        break;
                        #endregion
                }
            }
        }

        /// <summary>
        /// 專用於下述參數的目前儲存資料與接收資料的轉換器, 用以取得目前儲存資料應如何處理
        /// </summary>
        /// <param name="relationships">目前儲存的關係資訊</param>
        /// <param name="modifiedTo">調整完成後最後的樣式</param>
        /// <param name="dictionaryDNWithIsRemove">目前儲存資料的處理方式, 結構如右 Dictionary '區分名稱, 是否為刪除' </param>
        /// <returns>無法處理的項目</returns>
        private static string[] CombineStoredAndReceivedDNs(in LDAPRelationship[] relationships, in string[] modifiedTo, out Dictionary<string, bool> dictionaryDNWithIsRemove)
        {
            // 轉成 HashSet 加速找尋
            HashSet<string> distinguishedNameHashSet = new HashSet<string>(modifiedTo);

            // 最後將對外提供的資料, 結構如右 Dictionary '區分名稱, 是否為刪除': 預設長度用目前儲存的關係表長度
            dictionaryDNWithIsRemove = new Dictionary<string, bool>(relationships.Length);
            // 紀錄不能處理的項目: 預設長度用目前儲存的關係表長度
            List<string> unprocessedDNHashSet = new List<string>(relationships.Length);

            // 轉換成以區分名稱作為鍵值得字典
            Dictionary<string, LDAPRelationship> dictionaryDNWithRelationship = relationships.ToDictionary(storedValue => storedValue.DistinguishedName);
            // 遍歷所有項目: 進行檢查
            foreach (KeyValuePair<string, LDAPRelationship> pairDNWithRelationship in dictionaryDNWithRelationship)
            {
                // 包含此項目
                if (distinguishedNameHashSet.Contains(pairDNWithRelationship.Key))
                {
                    // 跳過: 代表保持原樣
                    continue;
                }

                // 移除時, 若發現是主要隸屬群組時不可進行動作
                if (pairDNWithRelationship.Value.IsPrimary)
                {
                    // 推入至無法處理項目
                    unprocessedDNHashSet.Add(pairDNWithRelationship.Key);
                    // 跳過接下來的檢查
                    continue;
                }

                // 簡易防呆: 避免重複加入
                if (dictionaryDNWithIsRemove.ContainsKey(pairDNWithRelationship.Key))
                {
                    // 包含時跳過
                    continue;
                }

                // 推入作為刪除項目
                dictionaryDNWithIsRemove.Add(pairDNWithRelationship.Key, true);
            }

            // 檢查協定中有那些項目是新增的
            foreach (string distinguishedName in distinguishedNameHashSet)
            {
                // 是否包含此項目
                if (dictionaryDNWithRelationship.ContainsKey(distinguishedName))
                {
                    // 包含時跳過
                    continue;
                }

                // [TODO] 可以考慮使用正則表達式檢查最後保留項目是否符合區分名稱命命規則

                // 簡易防呆: 避免重複加入
                if (dictionaryDNWithIsRemove.ContainsKey(distinguishedName))
                {
                    // 包含時跳過
                    continue;
                }

                // 推入作為新增項目
                dictionaryDNWithIsRemove.Add(distinguishedName, false);
            }

            // 對外提供無法處理的資料
            return unprocessedDNHashSet.ToArray();
        }

        /// <summary>
        /// 將帳戶控制資訊轉換成對外可識別的帳戶控制資訊協議
        /// </summary>
        /// <param name="accountControlFlags">轉換的旗標</param>
        /// <returns>協議可識別的內容</returns>
        private static AccountControlProtocols AccountControlFromFlagsToProtocols(in AccountControlFlags accountControlFlags)
        {
            // 宣告在方法內, 雖然每次都需要重新使用記憶體並宣告, 但是也相對變成只有在使用時才占用記憶體
            Dictionary<AccountControlFlags, AccountControlProtocols> dictionaryFlagsToProtocols = new Dictionary<AccountControlFlags, AccountControlProtocols>
            {
                { AccountControlFlags.PWD_DONT_EXPIRE, AccountControlProtocols.PWD_ENABLE_FOREVER },            // 密碼永久有效的對照
                { AccountControlFlags.PWD_ENCRYPTED_ALLOWED, AccountControlProtocols.PWD_ENCRYPTED },           // 密碼使用可還原加密的對照
                { AccountControlFlags.ACCOUNTDISABLE, AccountControlProtocols.ACCOUNT_DISABLE },                // 帳號已停用的對照
                { AccountControlFlags.LOGON_SMARTCARD, AccountControlProtocols.ACCOUNT_SMARTCARD },             // 帳號使用智慧卡登入的對照
                { AccountControlFlags.DELEGATION_NONE, AccountControlProtocols.ACCOUNT_CONFIDENTIAL },          // 帳號禁止委派的對照
                { AccountControlFlags.DES_KEY_ONLY, AccountControlProtocols.ACCOUNT_KERBEROS_DES },             // 帳號使用 DES 加密的對照
                { AccountControlFlags.RREAUTH_DONT_REQUIRE, AccountControlProtocols.ACCOUNT_KERBEROS_PREAUTH }, // 不使用 Kerberos 預先驗證的對照
            };

            // 先宣告成預設
            AccountControlProtocols accountControlProtocols = AccountControlProtocols.NONE;
            // 遍歷目前宣告的所有帳號控制旗標
            foreach (AccountControlFlags accountControlFlag in Enum.GetValues(typeof(AccountControlFlags)).Cast<AccountControlFlags>())
            {
                // 若不是目前儲存的資料
                if ((accountControlFlags & accountControlFlag) != accountControlFlag)
                {
                    // 跳過
                    continue;
                }

                // 不存在對照表內
                if (!dictionaryFlagsToProtocols.TryGetValue(accountControlFlag, out AccountControlProtocols accountControlProtocol))
                {
                    // 跳過
                    continue;
                }

                // 存在則疊加至對外提供旗標
                accountControlProtocols |= accountControlProtocol;
            }
            // 對外提供可用的資料
            return accountControlProtocols;
        }

        /// <summary>
        /// 將外部可識別的帳戶控制資訊協議轉換成內部可用的旗標
        /// </summary>
        /// <param name="accountControlProtocols">轉換的旗標</param>
        /// <returns>內部可用的內容</returns>
        private static AccountControlFlags AccountControlFromProtocolsToFlags(in AccountControlProtocols accountControlProtocols)
        {
            // 宣告在方法內, 雖然每次都需要重新使用記憶體並宣告, 但是也相對變成只有在使用時才占用記憶體
            Dictionary<AccountControlProtocols, AccountControlFlags> dictionaryProtocolsToFlags = new Dictionary<AccountControlProtocols, AccountControlFlags>
            {
                { AccountControlProtocols.PWD_ENABLE_FOREVER, AccountControlFlags.PWD_DONT_EXPIRE },            // 密碼永久有效的對照
                { AccountControlProtocols.PWD_ENCRYPTED, AccountControlFlags.PWD_ENCRYPTED_ALLOWED },           // 密碼使用可還原加密的對照
                { AccountControlProtocols.ACCOUNT_DISABLE, AccountControlFlags.ACCOUNTDISABLE },                // 帳號已停用的對照
                { AccountControlProtocols.ACCOUNT_SMARTCARD, AccountControlFlags.LOGON_SMARTCARD },             // 帳號使用智慧卡登入的對照
                { AccountControlProtocols.ACCOUNT_CONFIDENTIAL, AccountControlFlags.DELEGATION_NONE },          // 帳號禁止委派的對照
                { AccountControlProtocols.ACCOUNT_KERBEROS_DES, AccountControlFlags.DES_KEY_ONLY },             // 帳號使用 DES 加密的對照
                { AccountControlProtocols.ACCOUNT_KERBEROS_PREAUTH, AccountControlFlags.RREAUTH_DONT_REQUIRE }, // 不使用 Kerberos 預先驗證的對照
            };

            // 先宣告成預設
            AccountControlFlags accountControlFlags = AccountControlFlags.NONE;
            // 遍歷目前宣告的所有帳號控制旗標
            foreach (AccountControlProtocols accountControlProtocol in Enum.GetValues(typeof(AccountControlProtocols)).Cast<AccountControlProtocols>())
            {
                // 若不是目前儲存的資料
                if ((accountControlProtocols & accountControlProtocol) != accountControlProtocol)
                {
                    // 跳過
                    continue;
                }

                // 不存在對照表內
                if (!dictionaryProtocolsToFlags.TryGetValue(accountControlProtocol, out AccountControlFlags accountControlFlag))
                {
                    // 跳過
                    continue;
                }

                // 存在則疊加至對外提供旗標
                accountControlFlags |= accountControlFlag;
            }
            // 對外提供可用的資料
            return accountControlFlags;
        }
    }
}
