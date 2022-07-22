using ADService.Certificate;
using ADService.Protocol;
using Newtonsoft.Json.Linq;

namespace ADService.Authority
{
    /// <summary>
    /// 此方法或權限所需求的權限
    /// </summary>
    internal abstract class Article
    {
        /// <summary>
        /// 是否展示在功能列表
        /// </summary>
        internal bool IsShowed;

        /// <summary>
        /// 基底建構子
        /// </summary>
        /// <param name="isShowed">是否展示在功能列表</param>
        internal Article(in bool isShowed = true) => IsShowed = isShowed;

        /// <summary>
        /// 此方法是否可以被觸發, 可以額外接收觸發協定
        /// </summary>
        /// <param name="executionDetails">施行細則: 所有的異動項目都會記錄於此, 並於最後執行</param>
        /// <param name="recognizance">用來儲存屬性異動的證書</param>
        /// <param name="permissions">權限書: 目標持有的存取權限細則</param>
        /// <param name="protocol">觸發協定</param>
        /// <returns>使用條件與錯誤時字串</returns>
        internal abstract (ADInvokeCondition, string) Able(
            ref ExecutionDetails executionDetails,
            in Recognizance recognizance, in Permissions permissions,
            in JToken protocol
        );
        /// <summary>
        /// 驗證提供的協議內容是否可用
        /// </summary>
        /// <param name="executionDetails">施行細則: 所有的異動項目都會記錄於此, 並於最後執行</param>
        /// <param name="recognizance">保證書: 用來提供必須的需求</param>
        /// <param name="permissions">權限書: 目標持有的存取權限細則</param>
        /// <param name="protocol">驗證協定</param>
        /// <returns>此協定是否可用</returns>
        internal abstract bool Authenicate(
            ref ExecutionDetails executionDetails,
            in Recognizance recognizance, in Permissions permissions,
            in JToken protocol
        );
        /// <summary>
        /// 觸發此方法
        /// </summary>
        /// <param name="executionDetails">施行細則: 所有的異動項目都會記錄於此, 並於最後執行</param>
        /// <param name="recognizance">保證書: 用來提供必須的需求</param>
        /// <param name="permissions">權限書: 目標持有的存取權限細則</param>
        /// <param name="protocol">執行協定</param>
        internal abstract void Invoke(
            ref ExecutionDetails executionDetails,
            in Recognizance recognizance, in Permissions permissions,
            in JToken protocol
        );
    }
}
