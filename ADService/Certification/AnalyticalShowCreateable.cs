using ADService.ControlAccessRule;
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
    internal sealed class AnalyticalShowCreateable : Analytical
    {
        /// <summary>
        /// 呼叫基底建構子
        /// </summary>
        internal AnalyticalShowCreateable() : base(Methods.M_SHOWCRATEABLE, true) { }

        internal override (InvokeCondition, string) Invokable(in LDAPConfigurationDispatcher dispatcher, in LDAPObject invoker, in LDAPObject destination, LDAPPermissions permissions)
        {
            // 所有可用的方法
            List<string> invokedAble = new List<string>(2);

            // 宣告異動細節分析氣
            AnalyticalCreateUser analyticalCreateUser = new AnalyticalCreateUser();
            // 是否能展示須根據是否能異動決定
            (InvokeCondition conditionUser, _) = analyticalCreateUser.Invokable(dispatcher, invoker, destination, permissions);
            // 能夠取得條件時
            if (conditionUser != null)
            {
                // 推入作為可使用方法
                invokedAble.Add(analyticalCreateUser.Name);
            }

            // 宣告異動細節分析氣
            AnalyticalCreateGroup analyticalCreateGroup = new AnalyticalCreateGroup();
            // 是否能展示須根據是否能異動決定
            (InvokeCondition conditionGroup, _) = analyticalCreateGroup.Invokable(dispatcher, invoker, destination, permissions);
            // 能夠取得條件時
            if (conditionGroup != null)
            {
                // 推入作為可使用方法
                invokedAble.Add(analyticalCreateGroup.Name);
            }

            // 宣告異動細節分析氣
            AnalyticalCreateOrganizationUnit analyticalCreateOrganizationUnit = new AnalyticalCreateOrganizationUnit();
            // 是否能展示須根據是否能異動決定
            (InvokeCondition conditionOrganizationUnit, _) = analyticalCreateOrganizationUnit.Invokable(dispatcher, invoker, destination, permissions);
            // 能夠取得條件時
            if (conditionOrganizationUnit != null)
            {
                // 推入作為可使用方法
                invokedAble.Add(analyticalCreateOrganizationUnit.Name);
            }

            // 若不可呼叫
            if (invokedAble.Count == 0)
            {
                return (null, $"{analyticalCreateUser.Name} 與 {analyticalCreateGroup.Name} 皆無法使用");
            }

            /* 一般需求參數限制如下所述:
                 - 回傳協定內資料不可為空 (包含預設類型)
                 - 應限制目標物件類型
                 - 應提供物件類型的參數:
                 - 方法類型只要能夠呼叫就能夠編輯
            */
            const ProtocolAttributeFlags commonFlags = ProtocolAttributeFlags.INVOKEMETHOD | ProtocolAttributeFlags.ISARRAY;
            // 需求內容: 採用封盒動作
            Dictionary<string, object> dictionaryProtocolWithDetail = new Dictionary<string, object> {
                { InvokeCondition.METHODCONDITION, invokedAble.ToArray() }
            };

            // 持有項目時就外部就能夠異動
            return (new InvokeCondition(commonFlags, dictionaryProtocolWithDetail), string.Empty);
        }

        internal override bool Authenicate(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol, LDAPPermissions permissions) => false;

        internal override void Invoke(ref CertificationProperties certification, in LDAPObject invoker, in LDAPObject destination, in JToken protocol, LDAPPermissions permissions) { }
    }
}
