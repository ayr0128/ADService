using ADService.Advanced;
using ADService.Environments;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Analytical
{
    /// <summary>
    /// 異動持有屬性
    /// </summary>
    internal sealed class MethodCreateGroup : Method
    {
        /// <summary>
        /// 用來檢查的必要渠縣
        /// </summary>
        private const ActiveDirectoryRights activeDirectoryRights = ActiveDirectoryRights.CreateChild;

        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal MethodCreateGroup() : base(Methods.M_CREATEGROUP, false) { }

        internal override (InvokeCondition, string) Invokable(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules)
        {
            // 取得是否支援創建目標物件
            bool isAllow = permissions.IsAllow(LDAPCategory.CLASS_GROUP, activeDirectoryRights);
            // 檢查是否具備權限
            if (!isAllow)
            {
                return (null, $"物件:{permissions.Destination.DistinguishedName} 不具有:{activeDirectoryRights} 權限因而無法提供創建功能");
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
                { InvokeCondition.RECEIVEDTYPE, typeof(CreateGroup).Name },
            };

            // 持有項目時就外部就能夠異動
            return (new InvokeCondition(commonFlags, dictionaryProtocolWithDetail), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules)
        {
            // 解析成創建成員所需參數
            CreateGroup createGroup = protocol?.ToObject<CreateGroup>();
            // 創建資料不存在
            if (createGroup == null)
            {
                // 對外提供失敗與空資料
                return false;
            }

            // 取得是否支援創建目標物件
            bool isAllow = permissions.IsAllow(LDAPCategory.CLASS_GROUP, activeDirectoryRights);
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
            using (DirectoryEntry root = certification.Dispatcher.DomainRoot())
            {
                // 任一符合則不正確
                string encoderFiliter = $"{LDAPConfiguration.GetORFiliter(Properties.P_CN, createGroup.Name)}";
                // 找尋符合條件的物件
                using (DirectorySearcher searcher = new DirectorySearcher(root, encoderFiliter, LDAPObject.PropertiesToLoad))
                {
                    // 不得搜尋到任何物件
                    return searcher.FindOne() == null;
                }
            }
        }

        internal override void Invoke(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules)
        {
            // 解析成創建成員所需參數
            CreateGroup createGroup = protocol?.ToObject<CreateGroup>();
            // 創建資料不存在
            if (createGroup == null)
            {
                // 對外提供失敗與空資料
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
            DirectoryEntry newGroup = setProcessed.Entry.Children.Add($"{Properties.P_CN}={createGroup.Name}", LDAPCategory.CLASS_GROUP);

            // 直接推入
            newGroup.CommitChanges();
            // 更新
            newGroup.RefreshCache();

            // 取得區分名稱
            string distinguishedName = LDAPConfiguration.ParseSingleValue<string>(Properties.C_DISTINGUISHEDNAME, newGroup.Properties);
            // 設定區分名稱與物件
            RequiredCommitSet requiredCommitSet = certification.SetEntry(newGroup, distinguishedName);
            // 舍弟需要寫入
            requiredCommitSet.ReflashRequired();
        }
    }
}
