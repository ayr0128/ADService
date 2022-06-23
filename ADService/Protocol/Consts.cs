using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADService.Protocol
{
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

    /// <summary>
    /// 存取權限, 可對照至 <see cref="ActiveDirectoryRights"> Wiindows 權限 </see> 表
    /// </summary>
    [Flags]
    public enum AccessRuleRightFlags : ulong
    {
        /// <summary>
        /// 預設: 無作用
        /// </summary>
        None = 0x00000000,
        /// <summary>
        /// 創造子物件
        /// </summary>
        ChildrenCreate = ActiveDirectoryRights.CreateChild,
        /// <summary>
        /// 刪除子物件
        /// </summary>
        ChildrenDelete = ActiveDirectoryRights.DeleteChild,
        /// <summary>
        /// 陳列子物件
        /// </summary>
        ChildrenListed = ActiveDirectoryRights.ListChildren,
        /// <summary>
        /// 當物件權限變更時會複寫自身 SID, <see href="https://docs.microsoft.com/en-us/dotnet/api/system.directoryservices.activedirectoryrights?view=dotnet-plat-ext-6.0"> 參閱文件 </see>
        /// </summary>
        Self = ActiveDirectoryRights.Self,
        /// <summary>
        /// 讀取屬性
        /// </summary>
        PropertyRead = ActiveDirectoryRights.ReadProperty,
        /// <summary>
        /// 寫入屬性
        /// </summary>
        PropertyWrite = ActiveDirectoryRights.WriteProperty,
        /// <summary>
        /// 刪除所有子物件權限, 包含節點
        /// </summary>
        TreeDelete = ActiveDirectoryRights.DeleteTree,
        /// <summary>
        /// 陳列物件
        /// </summary>
        ListObject = ActiveDirectoryRights.ListObject,
        /// <summary>
        /// 擴增全縣
        /// </summary>
        RightExtended = ActiveDirectoryRights.ExtendedRight,
        /// <summary>
        /// 刪除特定物件
        /// </summary>
        Delete = ActiveDirectoryRights.Delete,
        /// <summary>
        /// 讀取角色控制參數
        /// </summary>
        ControlRead = ActiveDirectoryRights.ReadControl,
        /// <summary>
        /// Dacl 寫入權限
        /// </summary>
        DaclWrite = ActiveDirectoryRights.WriteDacl,
        /// <summary>
        /// 修改擁有者
        /// </summary>
        OwnerWrite = ActiveDirectoryRights.WriteOwner,
        /// <summary>
        /// 同步執行
        /// </summary>
        Synchronize = ActiveDirectoryRights.Synchronize,
        /// <summary>
        /// 存取系統安全性
        /// </summary>
        AccessSecurity = ActiveDirectoryRights.AccessSystemSecurity,
        /// <summary>
        /// 一般規則
        /// </summary>
        GenericAll = ActiveDirectoryRights.GenericAll,
    }
}
