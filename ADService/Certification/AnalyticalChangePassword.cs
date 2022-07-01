using ADService.ControlAccessRule;
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
    /// 重置密碼方法是可以呼叫, 可以參閱下述 <see href="https://docs.microsoft.com/en-us/windows/win32/api/iads/nf-iads-iadsuser-changepassword">文件</see>>
    /// </summary>
    internal sealed class AnalyticalChangePassword : Analytical
    {
        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal AnalyticalChangePassword() : base(Methods.M_CHANGEPWD) { }

        internal override (InvokeCondition, string) Invokable(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination, LDAPPermissions permissions)
        {
            // 根目錄不應重新命名
            if (destination.Type != CategoryTypes.PERSON)
            {
                // 對外提供失敗
                return (null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 是不能重新命名");
            }

            // 不存在 '重設密碼' 的額外權限
            if (!permissions.IsAllow(Properties.EX_CHANGEPASSWORD, null, AccessRuleRightFlags.RightExtended))
            {
                // 對外提供失敗
                return (null, $"類型:{destination.Type} 的目標物件:{destination.DistinguishedName} 需具有存取規則:{Properties.EX_CHANGEPASSWORD} 的額外權限");
            }

            // 具有修改名稱權限
            ProtocolAttributeFlags protocolAttributeFlags = ProtocolAttributeFlags.NULLDISABLE | ProtocolAttributeFlags.EDITABLE;
            // 預期項目: 必定是改變密碼用的格式
            Type typeChangePWD = typeof(ChangePWD);
            // 宣告持有內容: 修改時需求的類型是改變密碼
            Dictionary<string, object> dictionaryProtocolWithDetailInside = new Dictionary<string, object>()
            {
                { InvokeCondition.RECEIVEDTYPE, typeChangePWD.Name }, // 預期內容描述
            };

            // 只要有寫入權限就可以進行修改
            return (new InvokeCondition(protocolAttributeFlags, dictionaryProtocolWithDetailInside), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol, LDAPPermissions permissions)
        {
            // 將協議轉換成改變密碼用格式
            ChangePWD changePWDProtocol = protocol?.ToObject<ChangePWD>();
            /* 符合下述任意一個條件時, 驗證失敗
                 1. 協議為空
                 2. 舊密碼為空
                 3. 新密碼為空
            */
            if (changePWDProtocol == null
                || string.IsNullOrEmpty(changePWDProtocol.From)
                || string.IsNullOrEmpty(changePWDProtocol.To))
            {
                // 返回失敗
                return false;
            }

            // 由於密碼需要透過 LDAP 設置時才能知道規則是否允許, 所以設置密碼的驗證是相當簡易的
            return true;
        }

        internal override void Invoke(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol, LDAPPermissions permissions)
        {
            // 將協議轉換成改變密碼用格式
            ChangePWD changePWDProtocol = protocol?.ToObject<ChangePWD>();
            /* 符合下述任意一個條件時, 不進行密碼調整
                 1. 協議為空
                 2. 舊密碼為空
                 3. 新密碼為空
            */
            if (changePWDProtocol == null
                || string.IsNullOrEmpty(changePWDProtocol.From)
                || string.IsNullOrEmpty(changePWDProtocol.To))
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
            object invokeResult = set.Entry.Invoke("ChangePassword", changePWDProtocol.From, changePWDProtocol.To);
            // 此時會鳩收到的回覆格式必定為字串
            if (Convert.ToUInt64(invokeResult) != 0)
            {
                throw new LDAPExceptions($"類型:{destination.Type} 的物件:{destination.DistinguishedName} 於重置密碼時因錯誤代碼:{invokeResult} 而失敗", ErrorCodes.ACTION_FAILURE);
            }

            // 重設密碼會即刻產生影響, 因此只需要刷新即可
            set.InvokedReflash();
        }
    }
}

