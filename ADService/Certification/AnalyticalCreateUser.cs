using ADService.ControlAccessRule;
using ADService.Environments;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;

namespace ADService.Certification
{
    /// <summary>
    /// 異動持有屬性
    /// </summary>
    internal sealed class AnalyticalCreateUser : Analytical
    {
        /// <summary>
        /// 可處理的類型
        /// [TODO] 改為成從 LDAP 組成定義
        /// </summary>
        private const CategoryTypes categoryType = CategoryTypes.PERSON;
        /// <summary>
        /// 用來檢查的必要渠縣
        /// </summary>
        private const ActiveDirectoryRights activeDirectoryRights = ActiveDirectoryRights.CreateChild;

        /// <summary>
        /// 可自行填入的資料
        /// </summary>
        private readonly static string[] ENABLE_ATTRIBUTES = new string[]
        {
            Properties.P_INITIALS,
            Properties.P_SN,
            Properties.P_GIVENNAME,
            Properties.P_DISPLAYNAME,
        };

        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal AnalyticalCreateUser() : base(Methods.M_CREATEUSER, false) { }

        internal override (InvokeCondition, string) Invokable(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination, LDAPPermissions permissions)
        {
            // 取得成員字串
            Dictionary<CategoryTypes, string> dictionaryCategoryTypeWithValue = LDAPCategory.GetAccessRulesByTypes(categoryType);
            // 必須要能取得 [使用者] 的定內容
            if (!dictionaryCategoryTypeWithValue.TryGetValue(categoryType, out string valuePerson))
            {
                return (null, $"類型:{categoryType} 無法取得自訂議內容");
            }

            // 取得是否支援創建目標物件
            bool isAllow = permissions.IsAllow(valuePerson, activeDirectoryRights);
            // 檢查是否具備權限
            if (!isAllow)
            {
                return (null, $"因物件類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 不具有:{activeDirectoryRights} 權限因而無法提供創建功能");
            }

            // 預期項目: 必定是字串
            Type typeString = typeof(string);
            // 需求內容: 採用封盒動作
            Dictionary<string, object> dictionaryProtocolWithValue = new Dictionary<string, object> { { InvokeCondition.RECEIVEDTYPE, typeString.Name } };

            /* 一般需求參數限制如下所述:
                 - 回傳協定內資料不可為空 (包含預設類型)
                 - 應限制目標物件類型
                 - 應提供物件類型的參數:
                 - 方法類型只要能夠呼叫就能夠編輯
            */
            const ProtocolAttributeFlags commonFlags = ProtocolAttributeFlags.EDITABLE | ProtocolAttributeFlags.PROPERTIES;
            // 需求內容: 採用封盒動作
            Dictionary<string, object> dictionaryProtocolWithDetail = new Dictionary<string, object> {
                { InvokeCondition.RECEIVEDTYPE, typeof(CreateUser).Name },
                { InvokeCondition.PROPERTIES, ENABLE_ATTRIBUTES },
                { Properties.P_INITIALS, new InvokeCondition(ProtocolAttributeFlags.EDITABLE, dictionaryProtocolWithValue) },
                { Properties.P_SN, new InvokeCondition(ProtocolAttributeFlags.EDITABLE, dictionaryProtocolWithValue) },
                { Properties.P_GIVENNAME, new InvokeCondition(ProtocolAttributeFlags.EDITABLE, dictionaryProtocolWithValue) },
                { Properties.P_DISPLAYNAME, new InvokeCondition(ProtocolAttributeFlags.EDITABLE, dictionaryProtocolWithValue) },
            };

            // 持有項目時就外部就能夠異動
            return (new InvokeCondition(commonFlags, dictionaryProtocolWithDetail), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol, LDAPPermissions permissions)
        {
            // 解析成創建成員所需參數
            CreateUser createUser = protocol?.ToObject<CreateUser>();
            // 創建資料不存在
            if (createUser == null)
            {
                // 對外提供失敗與空資料
                return false;
            }

            // 取得成員字串
            Dictionary<CategoryTypes, string> dictionaryCategoryTypeWithValue = LDAPCategory.GetAccessRulesByTypes(categoryType);
            // 必須要能取得 [使用者] 的定內容
            if (!dictionaryCategoryTypeWithValue.TryGetValue(categoryType, out string valuePerson))
            {
                return false;
            }

            // 取得是否支援創建目標物件
            bool isAllow = permissions.IsAllow(valuePerson, activeDirectoryRights);
            // 檢查是否具備權限
            if (!isAllow)
            {
                return false;
            }

            /* 下述參數皆不得為空
                 - 物件名稱
                 - 密碼
                 - 帳號
            */
            if (string.IsNullOrEmpty(createUser.Name) || string.IsNullOrEmpty(createUser.Password) || string.IsNullOrEmpty(createUser.Account))
            {
                return false;
            }

            // 取得根目錄物件: 
            using (DirectoryEntry root = certification.Dispatcher.DomainRoot())
            {
                // 任一符合則不正確
                string encoderFiliter = $"(|{LDAPConfiguration.GetORFiliter(Properties.P_CN, createUser.Name)}{LDAPConfiguration.GetORFiliter(Properties.C_SMMACCOUNTNAME, createUser.Account)})";
                // 找尋符合條件的物件
                using (DirectorySearcher searcher = new DirectorySearcher(root, encoderFiliter, LDAPObject.PropertiesToLoad))
                {
                    // 不得搜尋到任何物件
                    if (searcher.FindOne() != null)
                    {
                        // 對外無權限
                        return false;
                    }
                }
            }

            // 否則返回成功
            return true;
        }

        internal override void Invoke(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol, LDAPPermissions permissions)
        {
            // 解析成創建成員所需參數
            CreateUser createUser = protocol?.ToObject<CreateUser>();
            // 創建資料不存在
            if (createUser == null)
            {
                // 對外提供失敗與空資料
                return;
            }


            // 取得成員字串
            Dictionary<CategoryTypes, string> dictionaryCategoryTypeWithValue = LDAPCategory.GetAccessRulesByTypes(categoryType);
            // 必須要能取得 [使用者] 的定內容
            if (!dictionaryCategoryTypeWithValue.TryGetValue(categoryType, out string valuePerson))
            {
                return;
            }

            // 取得是否具有目標物件
            RequiredCommitSet setProcessed = certification.GetEntry(destination.DistinguishedName);
            // 若入口物件不存在
            if (setProcessed == null)
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 顯示名稱檢查
            if (!createUser.DictionaryAttributeNameWithValue.TryGetValue(Properties.P_DISPLAYNAME, out string displayName) || string.IsNullOrEmpty(displayName))
            {
                // 取得姓
                createUser.DictionaryAttributeNameWithValue.TryGetValue(Properties.P_SN, out string surName);
                // 取得名
                createUser.DictionaryAttributeNameWithValue.TryGetValue(Properties.P_GIVENNAME, out string giveName);
                // 將姓名組合作為展示名稱
                createUser.DictionaryAttributeNameWithValue[Properties.P_DISPLAYNAME] = $"{surName ?? string.Empty}{giveName ?? string.Empty}";
            }

            // 創建新的子物件
            DirectoryEntry newPerson = setProcessed.Entry.Children.Add($"{Properties.P_CN}={createUser.Name}", valuePerson);
            // 只將部分可設置的資料
            foreach (string attributeName in ENABLE_ATTRIBUTES)
            {
                // 檢查是否存在目標資料
                if (!createUser.DictionaryAttributeNameWithValue.TryGetValue(attributeName, out string value))
                {
                    // 不存在跳過
                    continue;
                }

                // 設置資料
                newPerson.Properties[attributeName].Value = value;
            }
            // 設定密碼
            newPerson.Properties[Properties.C_UNICODEPWD].Value = Encoding.Unicode.GetBytes($"\"{createUser.Password}\"");
            // 直接推入
            newPerson.CommitChanges();
            // 更新
            newPerson.RefreshCache();

            // 取得區分名稱
            string distinguishedName = LDAPConfiguration.ParseSingleValue<string>(Properties.C_DISTINGUISHEDNAME, newPerson.Properties);
            // 設定區分名稱與物件
            RequiredCommitSet requiredCommitSet =  certification.SetEntry(newPerson, distinguishedName);
            // 舍弟需要寫入
            requiredCommitSet.ReflashRequired();
        }
    }
}
