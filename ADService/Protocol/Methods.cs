namespace ADService.Environments
{
    /// <summary>
    /// 所有支援外部呼叫修改的方法與功能
    /// </summary>
    public static class Methods
    {
        /// <summary>
        /// 重新命名物件: 右鍵方法
        /// </summary>
        public const string M_RENAME = "ReName";
        /// <summary>
        /// 移動物件: 右鍵方法
        /// </summary>
        public const string M_MOVETO = "MoveTo";
        /// <summary>
        /// 修改詳細內容: 額外呼叫
        /// </summary>
        public const string M_MODIFYDETAIL = "ModifyDetail";
        /// <summary>
        /// 展示詳細內容: 右鍵方法
        /// </summary>
        public const string M_SHOWDETAIL = "ShowDetail";
        /// <summary>
        /// 展示安全性: 右鍵方法
        /// </summary>
        public const string M_SHOWSCEURITY = "ShowSecurity";
        /// <summary>
        /// 修改安全性
        /// </summary>
        public const string M_MODIFYSCEURITY = "ModifySecurity";
        /// <summary>
        /// 顯示可創建的子物件: 右鍵方法
        /// </summary>
        public const string M_SHOWCRATEABLE = "ShowCreateable";
        /// <summary>
        /// 創建成員: 額外呼叫
        /// </summary>
        public const string M_CREATEUSER = "CreateUser";
        /// <summary>
        /// 創建成員: 額外呼叫
        /// </summary>
        public const string M_CREATEGROUP = "CreateGroup";
        /// <summary>
        /// 創建成員: 額外呼叫
        /// </summary>
        public const string M_CREATEORGANIZATIONUNIT = "CreateOrganizationUnit";
        /// <summary>
        /// 重置密碼: 額外權限, 直接作為右鍵方法使用
        /// </summary>
        public const string M_CHANGEPWD = "ChangePWD";
        /// <summary>
        /// 設置密碼: 額外權限, 直接作為右鍵方法使用
        /// </summary>
        public const string M_RESETPWD = "ResetPWD";
        /// <summary>
        /// 帳號控制參數的整合旗標
        /// </summary>
        public const string CT_USERACCOUNTCONTROL = "AccountControl";
    }
}
