using System.DirectoryServices;

namespace ADService.Protocol
{
    /// <summary>
    /// 提供給
    /// </summary>
    public class AccessRuleProtocol
    {
        /// <summary>
        /// 代表此權限將拒絕或允許那些權限
        /// <list type="table">
        ///     <item> 數值為真時, 代表允許 </item>
        ///     <item> 數值為否時, 代表拒絕 </item>
        /// </list>
        /// </summary>
        public bool AccessControl;

        /// <summary>
        /// 隸屬於哪個群組或者是哪個使用者時, 將會產生作用
        /// </summary>
        public string UnitName;
        /// <summary>
        /// 用來判斷指定單位的類型
        /// </summary>
        public UnitType TypeUnit;

        /// <summary>
        /// 針對屬性或控制存取權發生作用, 需使用 <see cref="TypeObject">控制權限類型</see> 決定如何動作
        /// </summary>
        public string ObjectName;
        /// <summary>
        /// 控制存取類型, 根據下述描述對 <see cref="ObjectName">物件名稱</see> 慘生作用
        /// <list type="table">
        ///     <item> <term><see cref="ObjectType.NONE">預設</see></term> 此時<see cref="ObjectName">物件名稱</see>不重要, 因為會對權限影響到的項目進行動作  </item>
        ///     <item> <term><see cref="ObjectType.CONROLACCESS">預設</see></term> 此時<see cref="ObjectName">物件名稱</see>將只影響到控制存取權限 </item>
        ///     <item> <term><see cref="ObjectType.ATTRIBUTE">預設</see></term> 此時<see cref="ObjectName">物件名稱</see>將只影響到屬性 </item>
        /// </list>
        /// </summary>
        public ObjectType TypeObject;

        /// <summary>
        /// 子物件為何種類型時, 此控制權限將發生繼承動作
        /// </summary>
        public string InheritedName;
        /// <summary>
        /// 繼承以及運作方式
        /// </summary>
        public ActiveDirectorySecurityInheritance SecurityInheritance;

        /// <summary>
        /// 此規則的權限
        /// </summary>
        public ActiveDirectoryRights ActiveDirectoryRight;
        /// <summary>
        /// 繼承來源
        /// </summary>
        public string ParentDistinguishedName;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="accessControl">允許或拒絕</param>
        /// <param name="unitName">對應此權限的群組或成員名稱</param>
        /// <param name="typeUnit">群組或成員類型</param>
        /// <param name="objectName">影響物件名稱</param>
        /// <param name="typeAccessRule">影響物件是屬性還是控制存取權縣</param>
        /// <param name="inheritedName">繼承的物件</param>
        /// <param name="securityInheritance">如何繼承</param>
        /// <param name="activeDirectoryRight">持有權限</param>
        /// <param name="parentDistinguishedName">繼承來源</param>
        public AccessRuleProtocol(in bool accessControl,
            in string unitName, in UnitType typeUnit,
            in string objectName, in ObjectType typeAccessRule,
            in string inheritedName, in ActiveDirectorySecurityInheritance securityInheritance,
            in ActiveDirectoryRights activeDirectoryRight, in string parentDistinguishedName)
        {
            AccessControl = accessControl;

            UnitName = unitName;
            TypeUnit = typeUnit;

            ObjectName = objectName;
            TypeObject = typeAccessRule;

            InheritedName = inheritedName;
            SecurityInheritance = securityInheritance;

            ActiveDirectoryRight = activeDirectoryRight;
            ParentDistinguishedName = parentDistinguishedName;
        }
    }
}
