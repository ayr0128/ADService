using ADService.Advanced;
using ADService.Environments;
using ADService.Media;
using ADService.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace ADService.Analytical
{
    /// <summary>
    /// 陳列持有安全性
    /// </summary>
    internal sealed class MethodModifySecurity : Method
    {
        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal MethodModifySecurity() : base(Methods.M_MODIFYSCEURITY, false) { }

        internal override (ADInvokeCondition, string) Invokable(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules)
        {
            // 檢查是否持有展示安全性的權限
            if (accessRules == null)
            {
                // 不存在時不可呼叫
                return (null, $"使用者:{certification.Invoker} 並不隸屬於管理者群組中, 因此無法喚起異動持有安全性");
            }

            // 外部可以傳遞對於特定類別的
            string classNameLimited = protocol?.ToObject<string>() ?? string.Empty;
            // 轉換可用下層物件類型
            Dictionary<string, UnitSchemaClass> dictionaryNameLowerWithUnitSchemaClass = accessRules.UnitSchemaClasses.ToDictionary(unitSchemaClass => unitSchemaClass.Name.ToLower());
            /* 一般需求參數限制如下所述:
                 - 回傳協定內資料不可為空 (包含預設類型)
                 - 應限制目標物件類型
                 - 應提供物件類型的參數:
                 - 方法類型只要能夠呼叫就能夠編輯
            */
            const ProtocolAttributeFlags commonFlags = ProtocolAttributeFlags.HASVALUE | ProtocolAttributeFlags.INVOKEMETHOD;
            // 資料描述
            ValueDescription description = new ValueDescription(typeof(AccessRuleProtocol).Name, accessRules.AccessRuleProtocols.Length, true);
            // 需求內容: 採用封盒動作
            Dictionary<string, object> dictionaryProtocolWithDetail = new Dictionary<string, object> {
                { ADInvokeCondition.STOREDTYPE, description },                // 持有內容描述
                { ADInvokeCondition.VALUE, accessRules.AccessRuleProtocols }, // 持有內容
                { ADInvokeCondition.METHODS, Methods.M_MODIFYSCEURITY }       // 持有
            };

            // 持有項目時就外部就能夠異動
            return (new ADInvokeCondition(commonFlags, dictionaryProtocolWithDetail), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules) => false;

        internal override void Invoke(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules) { }
    }
}
