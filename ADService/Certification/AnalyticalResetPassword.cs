using ADService.Details;
using ADService.Environments;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace ADService.Certification
{
    /// <summary>
    /// 強制設定密碼方法是可以呼叫, 可以參閱下述 <see href="https://docs.microsoft.com/en-us/windows/win32/api/iads/nf-iads-iadsuser-setpassword">文件</see>>
    /// </summary>
    internal sealed class AnalyticalResetPassword : Analytical
    {
        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal AnalyticalResetPassword() : base(Methods.M_RESETPWD) { }

        internal override (bool, InvokeCondition, string) Invokable(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination)
        {
            // 根目錄不應重新命名
            if (destination.Type != CategoryTypes.PERSON)
            {
                // 對外提供失敗
                return (false, null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 是不能重新命名");
            }

            // 整合各 SID 權向狀態
            AccessRuleInformation[] accessRuleInformations = GetAccessRuleInformations(invoker, destination);
            // 權限混和
            AccessRuleRightFlags mixedProcessedRightsProperty = AccessRuleInformation.CombineAccessRuleRightFlags(Properties.EX_RESETPASSWORD, accessRuleInformations);

            // 不存在 '名稱' 的寫入權限
            if ((mixedProcessedRightsProperty & AccessRuleRightFlags.RightExtended) == AccessRuleRightFlags.None)
            {
                // 對外提供失敗
                return (false, null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 需具有存取規則:{Properties.EX_RESETPASSWORD} 的額外權限");
            }

            // 具有修改名稱權限
            ProtocolAttributeFlags protocolAttributeFlags = ProtocolAttributeFlags.NULLDISABLE | ProtocolAttributeFlags.EDITABLE;
            // 預期項目: 必定是改變密碼用的格式
            Type typeSetPWD = typeof(string);
            // 宣告持有內容: 修改時需求的類型是改變密碼
            Dictionary<string, object> dictionaryProtocolWithDetailInside = new Dictionary<string, object>()
            {
                { InvokeCondition.RECEIVEDTYPE, typeSetPWD.Name }, // 預期內容描述
            };

            // 只要有寫入權限就可以進行修改
            return (true, new InvokeCondition(protocolAttributeFlags, dictionaryProtocolWithDetailInside), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol)
        {
            // 將協議轉換成改變密碼用格式
            string setPWDProtocol = protocol?.ToObject<string>();
            // 簡易檢查: 新密碼是否為空
            return !string.IsNullOrEmpty(setPWDProtocol);
        }

        internal override void Invoke(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol)
        {
            // 將協議轉換成改變密碼用格式
            string setPWDProtocol = protocol?.ToObject<string>();
            // 簡易檢查: 新密碼是否為空
            if (string.IsNullOrEmpty(setPWDProtocol))
            {
                // 返回失敗
                return;
            }

            // 取得修改目標的入口物件
            RequiredCommitSet set = certification.GetEntry(destination.DistinguishedName);
            // 應存在修改目標
            if (set == null)
            {
                // 若觸發此處例外必定為程式漏洞
                return;
            }

            // 呼叫改變密碼的動作
            object invokeResult = set.Entry.Invoke("SetPassword", setPWDProtocol);
            // 此時會鳩收到的回覆格式必定為字串
            if (Convert.ToUInt64(invokeResult) != 0)
            {
                throw new LDAPExceptions($"類型:{destination.Type} 的物件:{destination.DistinguishedName} 於重置密碼時因錯誤代碼:{invokeResult} 而失敗", ErrorCodes.ACTION_FAILURE);
            }

            // 重製密碼會即刻產生影響, 因此只需要刷新即可
            set.InvokedReflash();
        }
    }
}

