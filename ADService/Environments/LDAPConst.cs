using System;

namespace ADService.Environments
{
    /// <summary>
    /// 物件的系統旗標直
    /// </summary>
    [Flags]
    internal enum SystemFlags : uint
    {
        /// <summary>
        /// 預設, 通常不使用
        /// </summary>
        NONE = 0x00000000,
        /// <summary>
        /// 根據下述規則進行判斷
        /// <list type="table">
        ///     <item> <term>當 Attribute 性持有此旗標時</term> 不可複製 </item>
        ///     <item> <term>當 Cross-Ref 持有此旗標時</term> 物件位於 NTDS </item>
        /// </list>
        /// </summary>
        REPLICATED_DISABLE_REFOBJECT_IN_NTDS = 0x00000001,
        /// <summary>
        /// 根據下述規則進行判斷
        /// <list type="table">
        ///     <item> <term>當 Attribute 性持有此旗標時</term> 可複製 </item>
        ///     <item> <term>當 Cross-Ref 持有此旗標時</term> 物件位於 Domains </item>
        /// </list>
        /// </summary>
        REPLICATED_ENABLE_REFOBJECT_IN_DOMAIN = 0x00000002,
        /// <summary>
        /// 當 Attribute 性持有此旗標時, 此為建構完成的屬性
        /// </summary>
        ATTRIBUTE_CONSTRUCTED = 0x00000004,
        /// <summary>
        /// 設置時, 視為類型 1 的物件, 這些物件將被設置於系統基礎藍本中
        /// </summary>
        SYSTEM_BASE_SCHEMA = 0x00000010,
        /// <summary>
        /// 執行刪除時, 不將物件移至戰存區進而直接移除
        /// </summary>
        DELETED_IMMEDIATELY = 0x02000000,
        /// <summary>
        /// 不可移動
        /// </summary>
        MOVE_DISABLE = 0x04000000,
        /// <summary>
        /// 不可重新命名
        /// </summary>
        RENAME_DISABLE = 0x08000000,
        /// <summary>
        /// 設定中, 必須持有此旗標物件才可以在受限制的情況下進行移動, 否則不能移動
        /// </summary>
        CONFIGURATION_MOVE_ENABLE_ON_RESTRICTIONS = 0x10000000,
        /// <summary>
        /// 設定中, 必須持有此旗標物件可以在不受受限制的情況下進行移動, 否則不能移動
        /// </summary>
        CONFIGURATION_MOVE_ENABLE_NOT_RESTRICTIONS = 0x20000000,
        /// <summary>
        /// 設定中, 必須持有此旗標物件可以進行重新命名, 否則不能
        /// </summary>
        CONFIGURATION_RENAME_ENABLE = 0x40000000,
        /// <summary>
        /// 不可刪除
        /// </summary>
        DELETE_DISABLE = 0x80000000,
    }

    /// <summary>
    /// 關聯類型
    /// </summary>
    [Flags]
    internal enum PropertytFlags : byte
    {
        /// <summary>
        /// 預設, 無使用
        /// </summary>
        NONE,
        /// <summary>
        /// 是關聯屬性
        /// </summary>
        SET = 0x01,
        /// <summary>
        /// 是寫入有效
        /// </summary>
        WRITE = 0x02,
        /// <summary>
        /// 是可套用
        /// </summary>
        APPLIES = 0x04,
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
