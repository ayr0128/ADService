using System;

namespace ADService.Environments
{

    /// <summary>
    /// 錯誤編碼
    /// </summary>
    public enum ErrorCodes : ushort
    {
        /// <summary>
        /// 保留 0 不做任何用途
        /// </summary>
        NONE_ERROR,
        /// <summary>
        /// 伺服器錯誤
        /// </summary>
        SERVER_ERROR,
        /// <summary>
        /// 邏輯錯誤
        /// </summary>
        LOGIC_ERROR,
        /// <summary>
        /// 預期資料內容錯誤
        /// </summary>
        DATA_ERROR,
        /// <summary>
        /// 帳號已遭禁用
        /// </summary>
        ACCOUNT_DISABLE,
        /// <summary>
        /// 帳號遭鎖定
        /// </summary>
        ACCOUNT_LOCKED,
        /// <summary>
        /// 帳號已過期
        /// </summary>
        ACCOUNT_EXPIRED,
        /// <summary>
        /// 帳號不正確 (無此使用者)
        /// </summary>
        ACCOUNT_INCORRECT,
        /// <summary>
        /// 密碼已過期
        /// </summary>
        PASSWORD_EXPIRED,
        /// <summary>
        /// 密碼不正確
        /// </summary>
        PASSWORD_INCORRECT,
        /// <summary>
        /// 密碼需重新設置
        /// </summary>
        PASSWORD_LOGON_RESET,
        /// <summary>
        /// 工作站禁止登入
        /// </summary>
        REJECT_LOGIN_AT_WORKSTATION,
        /// <summary>
        /// 時間段禁止登入
        /// </summary>
        REJECT_LOGIN_AT_TIME,
        /// <summary>
        /// 名稱重複
        /// </summary>
        NAME_DUPLICATE,
        /// <summary>
        /// 提供參數取得的資料有誤
        /// </summary>
        ARG_DATA_ERROR,
        /// <summary>
        /// 無法找到指定物件
        /// </summary>
        OBJECT_NOTFOUND,
        /// <summary>
        /// 執行動作時權限不足
        /// </summary>
        PERMISSION_DENIED,
        /// <summary>
        /// 執行動作不吻合 AD 規則而失敗
        /// </summary>
        ACTION_FAILURE,
    }

    /// <summary>
    /// 帳號控制協議旗標
    /// </summary>
    [Flags]
    internal enum AccountControlFlags : int
    {
        /// <summary>
        /// 預設: 空白旗標
        /// </summary>
        NONE = 0x00000000,
        /// <summary>
        /// 啟用登入者後執行腳本動作
        /// </summary>
        SCRIPT = 0x00000001,
        /// <summary>
        /// 帳號已停用
        /// </summary>
        ACCOUNTDISABLE = 0x00000002,
        /// <summary>
        /// 需求首頁
        /// </summary>
        HOMEDIRREQUIRED = 0x00000008,
        /// <summary>
        /// 帳號已鎖定
        /// </summary>
        LOCKOUT = 0x00000010,
        /// <summary>
        /// 不須帳號密碼即可登入
        /// </summary>
        PWD_NOTREQUIRED = 0x00000020,
        /// <summary>
        /// 使用者不可自行變更密碼
        /// </summary>
        PWD_NOT_CHANGEABLE = 0x00000040,
        /// <summary>
        /// 密碼使用已加密的方式存放
        /// </summary>
        PWD_ENCRYPTED_ALLOWED = 0x00000080,
        /// <summary>
        /// 帳號為其他網域成員
        /// </summary>
        ACCOUNT_DUPLICATE = 0x00000100,
        /// <summary>
        /// 帳號為本地網域成員
        /// </summary>
        ACCOUNT_NORMAL = 0x00000200,
        /// <summary>
        /// 電腦為其他網域信任的電腦
        /// </summary>
        ACCOUNT_DOMAIN_TRUST = 0x00000800,
        /// <summary>
        /// 電腦為其他工作站信任的電腦
        /// </summary>
        COMPUTER_STATION_TRUST = 0x00001000,
        /// <summary>
        /// 帳號為伺服器信任的成員
        /// </summary>
        COMPUTER_SERVER_TRUST = 0x00002000,
        /// <summary>
        /// 密碼永不過期
        /// </summary>
        PWD_DONT_EXPIRE = 0x00010000,
        /// <summary>
        /// 帳號為 NMS 登入
        /// </summary>
        LOGON_NMS = 0x00020000,
        /// <summary>
        /// 帳號為 智慧卡 登入
        /// </summary>
        LOGON_SMARTCARD = 0x00040000,
        /// <summary>
        /// 信任 Kerberos 委派
        /// </summary>
        DELEGATION_TRUSTED = 0x00080000,
        /// <summary>
        /// 不可委派 (即使信任 Kerberos 委派也不行)
        /// </summary>
        DELEGATION_NONE = 0x00100000,
        /// <summary>
        /// 僅在資料進行 DES 加密狀態下可用
        /// </summary>
        DES_KEY_ONLY = 0x00200000,
        /// <summary>
        /// 登入時不須使用 Kerberos 進行驗證
        /// </summary>
        RREAUTH_DONT_REQUIRE = 0x00400000,
        /// <summary>
        /// 密碼已過期
        /// </summary>
        PWD_EXPIRED = 0x00800000,
        /// <summary>
        /// 允許委派的帳號
        /// </summary>
        DELEGATION_AUTHENTICATE = 0x01000000,
    }

    /// <summary>
    /// 通訊加密方式
    /// </summary>
    [Flags]
    internal enum EncryptedType : int
    {
        /// <summary>
        /// 預設
        /// </summary>
        NONE = 0x00,
        /// <summary>
        /// DES CRC 加密
        /// </summary>
        DES_CRC = 0x01,
        /// <summary>
        /// DES MD5 加密
        /// </summary>
        DES_MD5 = 0x02,
        /// <summary>
        /// RC4 HMAC 加密
        /// </summary>
        RC4_HMAC = 0x04,
        /// <summary>
        /// AES128-CTS-HMAC-SHA1-96 加密
        /// </summary>
        AES128 = 0x08,
        /// <summary>
        /// AES256-CTS-HMAC-SHA1-96 加密
        /// </summary>
        AES256 = 0x10,
    }
}
