using System;
using System.DirectoryServices;
using System.Security.AccessControl;

namespace ADService.Protocol
{
    /// <summary>
    /// 提供給
    /// </summary>
    public struct AccessRuleProtocol
    {
        /// <summary>
        /// 提供 GUID 格式, 轉換成對應的小寫格式
        /// </summary>
        /// <param name="valueGUID">指定 GUID</param>
        /// <returns>轉換成對照格事後的小寫 GUID</returns>
        internal static string ConvertedGUID(in Guid valueGUID) => valueGUID.ToString("D").ToLower();
        /// <summary>
        /// 提供 GUID 格式確認是否為空的 GUID
        /// </summary>
        /// <param name="valueGUID">指定 GUID</param>
        /// <returns> GUID 是否為空</returns>
        internal static bool IsGUIDEmpty(in Guid valueGUID) => valueGUID.Equals(Guid.Empty);

        /// <summary>
        /// 製作簽名
        /// </summary>
        /// <param name="unitName">隸屬群組或物件</param>
        /// <param name="isSystemSecurityID">是否是系統群組或物件</param>
        /// <param name="isInherited">是否從繼承而來</param>
        /// <param name="distinguishedName">持有此權限的區分名稱</param>
        /// <param name="objectName">針對屬性名稱</param>
        /// <param name="inheritedName">針對類型</param>
        /// <param name="activeDirectoryAccessRule">存取規則</param>
        /// <returns>簽名字串</returns>
        internal static string CreateSignature(in string unitName, in bool isSystemSecurityID, in bool isInherited, in string distinguishedName, in string objectName, in string inheritedName, in ActiveDirectoryAccessRule activeDirectoryAccessRule)
        {
            // 將權限轉換成數字
            uint valueRights = Convert.ToUInt32(activeDirectoryAccessRule.ActiveDirectoryRights);
            // 轉換是否允許
            bool isAllow = activeDirectoryAccessRule.AccessControlType == AccessControlType.Allow;
            // 繼承動作
            byte valueInherited = Convert.ToByte(activeDirectoryAccessRule.InheritanceType);
            // 存取權限持有者
            string ownerDistinguishedName = isInherited ? distinguishedName : string.Empty;
            // 此內容必定獨一無二
            return CreateSignature(unitName, isSystemSecurityID, ownerDistinguishedName, objectName, inheritedName, valueRights, isAllow, valueInherited);
        }

        /// <summary>
        /// 內部呼叫的簽名製作功能
        /// </summary>
        /// <param name="unitName">隸屬群組或物件</param>
        /// <param name="isSystemSecurityID">是否是系統群組或物件</param>
        /// <param name="distinguishedName">持有此權限的物件區分名稱: 空白時代表為自身持有的權限</param>
        /// <param name="objectName">針對屬性名稱</param>
        /// <param name="inheritedName">針對類型</param>
        /// <param name="rightFlags">可用權限</param>
        /// <param name="isAllow">是否允許</param>
        /// <param name="inheritanceType">繼承方式</param>
        /// <returns>簽名字串</returns>
        private static string CreateSignature(in string unitName, in bool isSystemSecurityID, in string distinguishedName, in string objectName, in string inheritedName, in uint rightFlags, in bool isAllow, in byte inheritanceType)
        {
            // 此內容必定獨一無二
            return $"{distinguishedName}:{unitName}:{isSystemSecurityID}:{objectName}:{inheritedName}:{rightFlags}:{isAllow}:{inheritanceType}";
        }

        /// <summary>
        /// 內部簽名格式: 簽名格式可以轉換成指定目標
        /// </summary>
        public readonly string Signature;
        /// <summary>
        /// 持有此權限的區分名稱: 如果為空就代表此權限是自身持有的, 可以移除或修改
        /// </summary>
        public readonly string DistinguishedName;
        /// <summary>
        /// 持有此權限的群組或物件: 注意有可能是系統物件, 這類型的物件無法透過搜尋取得, 需使用特殊方式搜尋
        /// </summary>
        public readonly string UnitName;
        /// <summary>
        /// 持有此權限的群組或物件是否為系統物件
        /// </summary>
        public readonly bool IsSystem;
        /// <summary>
        /// 權限針對的物件或屬性或控制存取群組
        /// </summary>
        public readonly string ObjectName;
        /// <summary>
        /// 當繼承發生時此權限能影響到的類型
        /// </summary>
        public readonly string InheritedName;
        /// <summary>
        /// 可用的權限
        /// </summary>
        public readonly ActiveDirectoryRights RightFlags;
        /// <summary>
        /// 允許或是拒絕設置的權限
        /// </summary>
        public readonly AccessControlType ControlType;
        /// <summary>
        /// 繼承方式
        /// </summary>
        public readonly ActiveDirectorySecurityInheritance InheritanceType;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="signature">按照規定格式製作的簽名</param>
        public AccessRuleProtocol(in string signature)
        {
            Signature = signature;

            // 切割簽名取得內部格式
            string[] elements = Signature.Split(':');
            // 區分名稱
            DistinguishedName = elements[0];
            // 持有此權限的群組或物件
            UnitName = elements[1];
            // 持有此權限的群組或物件是否為系統持有
            IsSystem = Convert.ToBoolean(elements[2]);
            // 權限針對的物件或屬性或控制存取群組
            ObjectName = elements[3];
            // 當繼承發生時此權限能影響到的類型
            InheritedName = elements[4];

            // 轉換權限
            uint valueRights = Convert.ToUInt32(elements[5]);
            // 轉換權限
            RightFlags = (ActiveDirectoryRights)Enum.ToObject(typeof(ActiveDirectoryRights), valueRights);
            // 允許或是拒絕設置的權限
            ControlType = Convert.ToBoolean(elements[6]) ? AccessControlType.Allow : AccessControlType.Deny;

            // 轉換繼承方式
            uint valueInherited = Convert.ToUInt32(elements[7]);
            // 轉換繼承方式
            InheritanceType = (ActiveDirectorySecurityInheritance)Enum.ToObject(typeof(ActiveDirectorySecurityInheritance), valueInherited);
        }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitName">隸屬群組或物件</param>
        /// <param name="isSystem">是否是系統群組或物件</param>
        /// <param name="objectName">針對屬性名稱</param>
        /// <param name="inheritedName">針對類型</param>
        /// <param name="rightFlags">可用權限</param>
        /// <param name="isAllow">是否允許</param>
        /// <param name="inheritanceType">繼承方式</param>
        public AccessRuleProtocol(in string unitName, in bool isSystem, in string objectName, in string inheritedName, in ActiveDirectoryRights rightFlags, in bool isAllow, in ActiveDirectorySecurityInheritance inheritanceType)
        {
            // 區分名稱只能只掉目標自己
            DistinguishedName = string.Empty;
            // 持有此權限的群組或物件
            UnitName = unitName;
            // 持有此權限的群組或物件是否為系統持有
            IsSystem = isSystem;
            // 權限針對的物件或屬性或控制存取群組
            ObjectName = objectName;
            // 當繼承發生時此權限能影響到的類型
            InheritedName = inheritedName;

            // 轉換權限
            RightFlags = rightFlags;
            // 允許或是拒絕設置的權限
            ControlType = isAllow ? AccessControlType.Allow : AccessControlType.Deny;

            // 轉換繼承方式
            InheritanceType = inheritanceType;

            // 將權限轉換成數字
            uint valueRights = Convert.ToUInt32(RightFlags);
            // 繼承動作
            byte valueInherited = Convert.ToByte(InheritanceType);
            // 製作簽名
            Signature = CreateSignature(UnitName, IsSystem, DistinguishedName, ObjectName, InheritedName, valueRights, isAllow, valueInherited);
        }
    }
}
