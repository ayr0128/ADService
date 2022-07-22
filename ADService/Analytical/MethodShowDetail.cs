using ADService.Advanced;
using ADService.Environments;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ADService.Analytical
{
    /// <summary>
    /// 異動持有屬性
    /// </summary>
    internal sealed class MethodShowDetail : Method
    {
        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal MethodShowDetail() : base(Methods.M_SHOWDETAIL, true) { }

        internal override (ADInvokeCondition, string) Invokable(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules)
        {
            // 宣告異動細節分析氣
            MethodModifyDetail analyticalModifyDetail = new MethodModifyDetail();
            // 是否能展示須根據是否能異動決定
            (ADInvokeCondition condition, string message) = analyticalModifyDetail.Invokable(ref certification, protocol, permissions, accessRules);
            // 若不可呼叫
            if (condition == null)
            {
                return (null, message);
            }

            /* 一般需求參數限制如下所述:
                 - 回傳協定內資料不可為空 (包含預設類型)
                 - 應限制目標物件類型
                 - 應提供物件類型的參數:
                 - 方法類型只要能夠呼叫就能夠編輯
            */
            const ProtocolAttributeFlags commonFlags = ProtocolAttributeFlags.INVOKEMETHOD;
            // 需求內容: 採用封盒動作
            Dictionary<string, object> dictionaryProtocolWithDetail = new Dictionary<string, object> {
                { ADInvokeCondition.METHODS, new string[1]{ analyticalModifyDetail.Name } }
            };

            // 持有項目時就外部就能夠異動
            return (new ADInvokeCondition(commonFlags, dictionaryProtocolWithDetail), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules) => false;

        internal override void Invoke(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules) { }
    }
}
