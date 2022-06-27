using ADService.Environments;
using ADService.Foundation;
using ADService.Media;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ADService.Certification
{
    /// <summary>
    /// 異動持有屬性
    /// </summary>
    internal sealed class AnalyticalShowDetail : Analytical
    {
        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal AnalyticalShowDetail() : base(Methods.M_SHOWDETAIL, true) { }

        internal override (bool, InvokeCondition, string) Invokable(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination)
        {
            // 宣告異動細節分析氣
            AnalyticalModifyDetail analyticalModifyDetail = new AnalyticalModifyDetail();
            // 是否能展示須根據是否能異動決定
            (bool invokable, _, string message) = analyticalModifyDetail.Invokable(dispatcher, invoker, destination);
            // 若不可呼叫
            if (!invokable)
            {
                return (false, null, message);
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
                { InvokeCondition.METHODCONDITION, analyticalModifyDetail.Name }
            };

            // 持有項目時就外部就能夠異動
            return (true, new InvokeCondition(commonFlags, dictionaryProtocolWithDetail), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol) => false;

        internal override void Invoke(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol) { }
    }
}
