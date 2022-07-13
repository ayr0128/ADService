using ADService.Advanced;
using ADService.Protocol;
using Newtonsoft.Json.Linq;

namespace ADService.Analytical
{
    /// <summary>
    /// 此方法或權限所需求的權限
    /// </summary>
    internal abstract class Method
    {
        /// <summary>
        /// 註冊給外部使用的方法或功能索引
        /// </summary>
        internal string Name;
        /// <summary>
        /// 是否展示在功能列表
        /// </summary>
        internal bool IsShowed;

        /// <summary>
        /// 基底建構子
        /// </summary>
        /// <param name="name">方法或屬性名稱</param>
        /// <param name="isShowed">是否展示在功能列表</param>
        internal Method(in string name, in bool isShowed = true)
        {
            Name = name;
            IsShowed = isShowed;
        }

        /// <summary>
        /// 檢查持有權限能否觸發此方法
        /// </summary>
        /// <param name="certification">用來儲存屬性異動的證書</param>
        /// <param name="protocol">外部傳遞的協定內容</param>
        /// <param name="permissions">喚起者與目標能使用的權限</param>
        /// <param name="accessRules">釐清繼承關係的所有權限</param>
        /// <returns>是否可使用</returns>
        internal abstract (InvokeCondition, string) Invokable(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules);
        /// <summary>
        /// 驗證提供的協議內容是否可用
        /// </summary>
        /// <param name="certification">用來儲存屬性異動的證書</param>
        /// <param name="protocol">外部傳遞的協定內容</param>
        /// <param name="permissions">喚起者與目標能使用的權限</param>
        /// <param name="accessRules">釐清繼承關係的所有權限</param>
        /// <returns>此協定是否可用</returns>
        internal abstract bool Authenicate(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules);
        /// <summary>
        /// 觸發此方法
        /// </summary>
        /// <param name="certification">用來儲存屬性異動的證書</param>
        /// <param name="protocol">外部傳遞的協定內容</param>
        /// <param name="permissions">喚起者與目標能使用的權限</param>
        /// <param name="accessRules">釐清繼承關係的所有權限</param>
        internal abstract void Invoke(ref CertificationProperties certification, in JToken protocol, in LDAPPermissions permissions, in LDAPAccessRules accessRules);
    }
}
