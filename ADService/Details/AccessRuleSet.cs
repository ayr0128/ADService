using ADService.Media;
using System;
using System.DirectoryServices;
using System.Security.Principal;

namespace ADService.Details
{
    /// <summary>
    /// 包含持有存取規則的持有等詳細資料的組合
    /// </summary>
    internal class AccessRuleSet
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
        /// 持有此存取權限的物件區分名稱
        /// </summary>
        internal readonly string DistinguishedName;
        /// <summary>
        /// 持有此安全性存取權限的物件名稱
        /// </summary>
        internal readonly string UnitName;
        /// <summary>
        /// 持有此安全性存取權限的物件安全性序列號
        /// </summary>
        internal readonly string SecurityID;
        /// <summary>
        /// 是否透過繼承取得
        /// </summary>
        internal readonly bool IsInherited;
        /// <summary>
        /// 原始存取權限
        /// </summary>
        internal readonly ActiveDirectoryAccessRule Raw;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="distinguishedName">物件區分名稱</param>
        /// <param name="isInherited">是否透過繼承取得</param>
        /// <param name="raw">原始存取權限</param>
        internal AccessRuleSet(string distinguishedName, in bool isInherited, ActiveDirectoryAccessRule raw)
        {
            DistinguishedName = distinguishedName;
            IsInherited = isInherited;
            Raw = raw;

            // 注意需要透過 NTAccount 取得
            UnitName = raw.IdentityReference.ToString(); 
            SecurityID = raw.IdentityReference.Translate(typeof(SecurityIdentifier)).ToString();
        }

        /// <summary>
        /// 指定存取規則是否包含目標權限
        /// </summary>
        /// <param name="activeDirectoryRightsMask">目標權限遮罩</param>
        /// <returns>透過遮罩遮蔽後持有的旗標</returns>
        internal ActiveDirectoryRights RightMasks(in ActiveDirectoryRights activeDirectoryRightsMask) => Raw.ActiveDirectoryRights & activeDirectoryRightsMask;

        /// <summary>
        /// 檢查此存取權限在目標類型物件上是否能產生影響
        /// </summary>
        /// <param name="unitSchemaClass">目標類型</param>
        /// <returns>是否產生影響</returns>
        internal bool Activable(in UnitSchemaClass unitSchemaClass) => IsGUIDEmpty(Raw.InheritedObjectType) || ConvertedGUID(Raw.InheritedObjectType) == unitSchemaClass.SchemaGUID.ToLower();
    }
}
