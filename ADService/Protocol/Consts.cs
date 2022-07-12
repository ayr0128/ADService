using System;
using System.Collections.Generic;

namespace ADService.Protocol
{
    /// <summary>
    /// 指定物件的類錫
    /// </summary>
    public enum UnitType : byte
    {
        /// <summary>
        /// 預設, 不可能出現
        /// </summary>
        NONE,
        /// <summary>
        /// 指定系統物件
        /// </summary>
        SYSTEM,
        /// <summary>
        /// 指定網域物件
        /// </summary>
        DOMAIN,
    }

    /// <summary>
    /// 存取規則類型
    /// </summary>
    public enum ObjectType : byte
    {
        /// <summary>
        /// 預設, 出現時代表所有權限皆被啟用
        /// </summary>
        NONE,
        /// <summary>
        /// 屬性值
        /// </summary>
        ATTRIBUTE,
        /// <summary>
        /// 控制存取權限
        /// </summary>
        CONROLACCESS,
    }

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
    /// 提供給客戶端進行設置的帳號控制旗標
    /// </summary>
    [Flags]
    public enum AccountControlProtocols : uint
    {
        /// <summary>
        /// 預設
        /// </summary>
        NONE = 0x0000,
        /// <summary>
        /// 密碼須在下次登入時重新設置
        /// </summary>
        PWD_CHANGE_NEXTLOGON = 0x0001,
        /// <summary>
        /// 密碼不可異動, 此旗標與 <see cref="PWD_CHANGE_NEXTLOGON">密碼須在下次登入時重新設置</see> 彼此衝突, 同時存在時以 <see cref="PWD_CHANGE_NEXTLOGON">密碼須在下次登入時重新設置</see> 優先
        /// </summary>
        PWD_DISABLE_CHANGE = 0x0002,
        /// <summary>
        /// 密碼永久有效, 此旗標與 <see cref="PWD_CHANGE_NEXTLOGON">密碼須在下次登入時重新設置</see> 彼此衝突, 同時存在時以 <see cref="PWD_ENABLE_FOREVER">密碼永久有效</see> 優先
        /// </summary>
        PWD_ENABLE_FOREVER = 0x0004,
        /// <summary>
        /// 使用可還原的加密存放密碼
        /// </summary>
        PWD_ENCRYPTED = 0x0008,
        /// <summary>
        /// 帳號已停用
        /// </summary>
        ACCOUNT_DISABLE = 0x0010,
        /// <summary>
        /// 帳號已鎖定
        /// </summary>
        ACCOUNT_LOCKOUT = 0x0020,
        /// <summary>
        /// 帳號使用智能卡登入
        /// </summary>
        ACCOUNT_SMARTCARD = 0x0040,
        /// <summary>
        /// 帳號為禁止委派的機密帳號
        /// </summary>
        ACCOUNT_CONFIDENTIAL = 0x0080,
        /// <summary>
        /// 帳號支援使用 Kerberos DES 加密類型
        /// </summary>
        ACCOUNT_KERBEROS_DES = 0x0100,
        /// <summary>
        /// 帳號支援使用 Kerberos AES128 加密類型
        /// </summary>
        ACCOUNT_KERBEROS_AES128 = 0x0200,
        /// <summary>
        /// 帳號支援使用 Kerberos AES128 加密類型
        /// </summary>
        ACCOUNT_KERBEROS_AES256 = 0x0400,
        /// <summary>
        /// 帳號不須使用 Kerberos 預先驗證
        /// </summary>
        ACCOUNT_KERBEROS_PREAUTH = 0x0800,
    }

    /// <summary>
    /// 物件類型, 可以旗標方式拓寬
    /// </summary>
    [Flags]
    public enum CategoryTypes : ushort
    {
        /// <summary>
        /// 保留 0 不做任何用途
        /// </summary>
        NONE = 0x0000,
        /// <summary>
        /// 容器
        /// </summary>
        CONTAINER = 0x0001,
        /// <summary>
        /// 網域節點
        /// </summary>
        DOMAIN_DNS = 0x0002,
        /// <summary>
        /// 組織架構
        /// </summary>
        ORGANIZATION_UNIT = 0x0004,
        /// <summary>
        /// 安全權限群組: 內部預留
        /// </summary>
        ForeignSecurityPrincipals = 0x0008,
        /// <summary>
        /// 權限群組
        /// </summary>
        GROUP = 0x0010,
        /// <summary>
        /// 使用者
        /// </summary>
        PERSON = 0x0020,
        /// <summary>
        /// 所有容器類型的物件
        /// </summary>
        ALL_CONTAINERS = CONTAINER | DOMAIN_DNS | ORGANIZATION_UNIT,
        /// <summary>
        /// 所有可用單位
        /// </summary>
        ALL_UNITS = GROUP | PERSON,
        /// <summary>
        /// 可以觸發重新命名功能的單位
        /// </summary>
        ALL_RENAME = ORGANIZATION_UNIT | ALL_UNITS,
        /// <summary>
        /// 所有類型
        /// </summary>
        ALL = ALL_CONTAINERS | ALL_UNITS,
    }

    /// <summary>
    /// 對於協議參數的描述
    /// </summary>
    [Flags]
    public enum ProtocolAttributeFlags : ulong
    {
        /// <summary>
        /// 預設, 無作用
        /// </summary>
        NONE = 0x00000000,
        /// <summary>
        /// 數據為列舉: 出現時必定包含 下述欄位
        /// <list type="table|bullet">
        ///     <item><term> <see cref="InvokeCondition.ENUMLIST">可用列舉表</see> </term> 可用 <see cref="HashSet{Enum}"> 列舉列表 </see> </item>
        /// </list>
        /// </summary>
        ISENUM = 0x00000100,
        /// <summary>
        /// 數據為旗標: 出現時必定包含 下述欄位
        /// <list type="table|bullet">
        ///     <item><term> <see cref="InvokeCondition.FLAGMASK">可用旗標遮罩</see> </term> 可用 <see cref="FlagsAttribute"> 旗標遮罩 </see> </item>
        /// </list>
        /// </summary>
        ISFLAGS = 0x00000200,
        /// <summary>
        /// 持有數值: 出現時必定包含下述欄位
        /// <list type="table|bullet">
        ///     <item><term> <see cref="InvokeCondition.VALUE">資料內容</see> </term> 須根據儲存格式變換 </item>
        ///     <item><term> <see cref="InvokeCondition.STOREDTYPE">資料類型</see> </term> 儲存的資料類型 </item>
        /// </list>
        /// </summary>
        HASVALUE = 0x00001000,
        /// <summary>
        /// 是否需要組合
        /// <list type="table|bullet">
        ///     <item><term> <see cref="InvokeCondition.COMBINETAG">組合旗標</see> </term> <see cref="string">字串</see> </item>
        /// </list>
        /// </summary>
        COMBINE = 0x00002000,
        /// <summary>
        /// 是否為陣列: 協議描述中會包含下述欄位, 額外元素中必定持有相關參數的資料描述
        /// <list type="table|bullet">
        ///     <item><term> <see cref="InvokeCondition.COUNT">陣列長度</see> </term> <see cref="int">整數</see> </item>
        /// </list>
        /// </summary>
        ISARRAY = 0x00004000,
        /// <summary>
        /// 需求屬性: 協議描述中會包含下述欄位, 額外元素中必定持有相關參數的資料描述
        /// <list type="table|bullet">
        ///     <item><term> <see cref="InvokeCondition.PROPERTIES">需求參數</see> </term> <see cref="HashSet{String}">字串陣列</see> </item>
        /// </list>
        /// </summary>
        PROPERTIES = 0x00010000,
        /// <summary>
        /// 持有元素: 協議描述中會包含下述欄位, 額外元素中必定持有相關參數的資料描述
        /// <list type="table|bullet">
        ///     <item><term> <see cref="InvokeCondition.ELEMENTS">元素</see> </term> <see cref="Dictionary{String, InvokeCondition}">協議字典</see> </item>
        /// </list>
        /// </summary>
        ELEMENTS = 0x00020000,
        /// <summary>
        /// 特殊旗標, 可以包含自己
        /// </summary>
        SELF = 0x010000000,
        /// <summary>
        /// 不可為空: 出現時代表回傳的資料不可為空
        /// </summary>
        NULLDISABLE = 0x02000000,
        /// <summary>
        /// 限制目標類型: 協議描述中會包含下述欄位
        /// <list type="table|bullet">
        ///     <item><term> <see cref="InvokeCondition.CATEGORYLIMITED">限制目標物件</see> </term> <see cref="CategoryTypes">類型</see> </item>
        /// </list>
        /// </summary>
        CATEGORYLIMITED = 0x04000000,
        /// <summary>
        /// 可否編輯
        /// </summary>
        EDITABLE = 0x40000000,
        /// <summary>
        /// 可喚起其他方法: 出現時必定包含下述欄位
        /// <list type="table|bullet">
        ///     <item><term> <see cref="InvokeCondition.METHODCONDITION">限制目標物件</see> </term> <see cref="CategoryTypes">類型</see> </item>
        /// </list>
        /// </summary>
        INVOKEMETHOD = 0x80000000,
    }
}
