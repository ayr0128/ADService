using ADService.ControlAccessRule;
using ADService.Environments;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Certification
{
    /// <summary>
    /// 異動持有屬性
    /// </summary>
    internal sealed class AnalyticalCreateOrganizationUnit : Analytical
    {
        /// <summary>
        /// 可處理的類型
        /// [TODO] 改為成從 LDAP 組成定義
        /// </summary>
        private const CategoryTypes categoryType = CategoryTypes.ORGANIZATION_UNIT;
        /// <summary>
        /// 用來檢查的必要渠縣
        /// </summary>
        private const ActiveDirectoryRights activeDirectoryRights = ActiveDirectoryRights.CreateChild;

        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal AnalyticalCreateOrganizationUnit() : base(Methods.M_CREATEORGANIZATIONUNIT, false) { }

        internal override (InvokeCondition, string) Invokable(in LDAPConfigurationDispatcher dispatcher, LDAPPermissions permissions)
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
                return (null, $"因物件類型:{permissions.Destination.Type} 的目標物件:{permissions.Destination.DistinguishedName} 不具有:{activeDirectoryRights} 權限因而無法提供創建功能");
            }

            /* 一般需求參數限制如下所述:
                 - 回傳協定內資料不可為空 (包含預設類型)
                 - 應限制目標物件類型
                 - 應提供物件類型的參數:
                 - 方法類型只要能夠呼叫就能夠編輯
            */
            const ProtocolAttributeFlags commonFlags = ProtocolAttributeFlags.EDITABLE;
            // 需求內容: 採用封盒動作
            Dictionary<string, object> dictionaryProtocolWithDetail = new Dictionary<string, object> {
                { InvokeCondition.RECEIVEDTYPE, typeof(CreateOrganizationUnit).Name },
            };

            // 持有項目時就外部就能夠異動
            return (new InvokeCondition(commonFlags, dictionaryProtocolWithDetail), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in JToken protocol, LDAPPermissions permissions)
        {
            // 解析成創建成員所需參數
            CreateOrganizationUnit createGroup = protocol?.ToObject<CreateOrganizationUnit>();
            // 創建資料不存在
            if (createGroup == null)
            {
                // 對外提供失敗與空資料
                return false;
            }

            // 取得成員字串
            Dictionary<CategoryTypes, string> dictionaryCategoryTypeWithValue = LDAPCategory.GetAccessRulesByTypes(categoryType);
            // 必須要能取得 [使用者] 的定內容
            if (!dictionaryCategoryTypeWithValue.TryGetValue(categoryType, out string valueOrganizationUnit))
            {
                return false;
            }

            // 取得是否支援創建目標物件
            bool isAllow = permissions.IsAllow(valueOrganizationUnit, activeDirectoryRights);
            // 檢查是否具備權限
            if (!isAllow)
            {
                return false;
            }

            /* 下述參數皆不得為空
                 - 物件名稱
            */
            if (string.IsNullOrEmpty(createGroup.Name))
            {
                return false;
            }

            // 取得根目錄物件: 
            using (DirectoryEntry root = certification.Dispatcher.ByDistinguisedName(permissions.Destination.DistinguishedName))
            {
                // 任一符合則不正確
                string encoderFiliter = LDAPConfiguration.GetORFiliter(Properties.P_OU, createGroup.Name);
                // 找尋符合條件的物件
                using (DirectorySearcher searcher = new DirectorySearcher(root, encoderFiliter, LDAPObject.PropertiesToLoad, SearchScope.OneLevel))
                {
                    // 不得搜尋到任何物件
                    return searcher.FindOne() == null;
                }
            }
        }

        internal override void Invoke(ref CertificationProperties certification, in JToken protocol, LDAPPermissions permissions)
        {
            // 解析成創建成員所需參數
            CreateGroup createGroup = protocol?.ToObject<CreateGroup>();
            // 創建資料不存在
            if (createGroup == null)
            {
                // 對外提供失敗與空資料
                return;
            }


            // 取得成員字串
            Dictionary<CategoryTypes, string> dictionaryCategoryTypeWithValue = LDAPCategory.GetAccessRulesByTypes(categoryType);
            // 必須要能取得 [使用者] 的定內容
            if (!dictionaryCategoryTypeWithValue.TryGetValue(categoryType, out string valueOrganizationUnit))
            {
                return;
            }

            // 取得是否具有目標物件
            RequiredCommitSet setProcessed = certification.GetEntry(permissions.Destination.DistinguishedName);
            // 若入口物件不存在
            if (setProcessed == null)
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 創建新的子物件
            DirectoryEntry newGroup = setProcessed.Entry.Children.Add($"{Properties.P_OU}={createGroup.Name}", valueOrganizationUnit);
            // 直接推入
            newGroup.CommitChanges();
            // 更新
            newGroup.RefreshCache();

            // 取得區分名稱
            string distinguishedName = LDAPConfiguration.ParseSingleValue<string>(Properties.C_DISTINGUISHEDNAME, newGroup.Properties);
            // 設定區分名稱與物件
            RequiredCommitSet requiredCommitSet =  certification.SetEntry(newGroup, distinguishedName);
            // 舍弟需要寫入
            requiredCommitSet.ReflashRequired();
        }
    }
}
