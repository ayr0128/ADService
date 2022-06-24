namespace ADService.Protocol
{
    /// <summary>
    /// 所有支援的特性鍵值
    /// </summary>
    public static class Properties
    {
        /// <summary>
        /// 樹系路徑: 隨時可能被異動
        /// </summary>
        public const string C_DISTINGGUISHEDNAME = "distinguishedName";
        /// <summary>
        /// 物件持有屬性
        /// </summary>
        public const string C_ALLOWEDATTRIBUTES = "allowedAttributes";
        /// <summary>
        /// 物件可持有的子類別
        /// </summary>
        public const string C_ALLOWEDCHILDCLASSES = "allowedChildClasses";

        /// <summary>
        /// 物件類型
        /// </summary>
        public const string C_OBJECTCATEGORY = "objectCategory";
        /// <summary>
        /// 物件GUID: 可參考 <see href="https://en.wikipedia.org/wiki/Universally_unique_identifier">微基百科</see> 說明文件
        /// </summary>
        public const string C_OBJECTGUID = "objectGUID";
        /// <summary>
        /// 物件 SID
        /// </summary>
        public const string C_OBJECTSID = "objectSID";
        /// <summary>
        /// 霧季類別
        /// </summary>
        public const string C_OBJECTCLASS = "objectClass";
        /// <summary>
        /// 主要隸屬群組: 只有成員與電腦持有
        /// </summary>
        public const string C_PRIMARYGROUPID = "primaryGroupID";
        /// <summary>
        /// 名稱
        /// </summary>
        public const string P_NAME = "name";
        /// <summary>
        /// 角色或容器名稱
        /// </summary>
        public const string P_CN = "cn";
        /// <summary>
        /// 組織名稱
        /// </summary>
        public const string P_OU = "ou";
        /// <summary>
        /// 根系名稱
        /// </summary>
        public const string P_DC = "dc";
        /// <summary>
        /// 對外顯示名稱
        /// </summary>
        public const string P_DISPLAYNAME = "displayName";
        /// <summary>
        /// 描述
        /// </summary>
        public const string P_DESCRIPTION = "description";
        /// <summary>
        /// 姓
        /// </summary>
        public const string P_SN = "sn";
        /// <summary>
        /// 名
        /// </summary>
        public const string P_GIVENNAME = "givenName";
        /// <summary>
        /// 英文縮寫
        /// </summary>
        public const string P_INITIALS = "initials";
        /// <summary>
        /// 持有成員: 只有組織才持有
        /// </summary>
        public const string P_MEMBER = "member";
        /// <summary>
        /// 隸屬組織: 組織與成員都持有
        /// </summary>
        public const string P_MEMBEROF = "memberOf";
        /// <summary>
        /// 成員控制旗標: 僅有成員持有
        /// </summary>
        public const string P_USERACCOUNTCONTROL = "userAccountControl";
        /// <summary>
        /// 密碼最後設置時間: 僅有成員持有
        /// </summary>
        public const string P_PWDLASTSET = "pwdLastSet";
        /// <summary>
        /// 密碼何時過期: 僅有成員持有
        /// </summary>
        public const string P_ACCOUNTEXPIRES = "accountExpires";
        /// <summary>
        /// 帳號何時鎖定: 僅有成員持有
        /// </summary>
        public const string P_LOCKOUTTIME = "lockoutTime";
        /// <summary>
        /// 通訊加密方式: 僅有成員持有
        /// </summary>
        public const string P_SUPPORTEDENCRYPTIONTYPES = "msDS-SupportedEncryptionTypes";

        /// <summary>
        /// 新增或移除成員內的方法: 額外權限
        /// </summary>
        public const string EX_A10EMEMBER = "Add/Remove self as member";
        /// <summary>
        /// 重置密碼: 額外權限, 直接作為右鍵方法使用
        /// </summary>
        public const string EX_CHANGEPASSWORD = "Change Password";
        /// <summary>
        /// 設置密碼: 額外權限, 直接作為右鍵方法使用
        /// </summary>
        public const string EX_RESETPASSWORD = "Reset Password";
    }
}
