using ADService.DynamicParse;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ADService.Certificate
{
    /// <summary>
    /// 此存取權限的關係與原始資料
    /// </summary>
    internal class AccessRuleRelationPermission
    {
        /// <summary>
        /// 持有此存取權限的物件區分名稱
        /// </summary>
        internal readonly CustomGUIDUnit CustomGUIDUnit;
        /// <summary>
        /// 是否透過繼承取得
        /// </summary>
        internal readonly bool IsInherited;
        /// <summary>
        /// 持有此安全性存取權限的物件名稱
        /// </summary>
        internal string Name => Raw.IdentityReference.ToString();
        /// <summary>
        /// 持有此安全性存取權限的物件安全性序列號
        /// </summary>
        internal string SID => ObjectSID.ToSID(Raw.IdentityReference);
        /// <summary>
        /// 是否是系統群組或系統人員
        /// </summary>
        internal bool IsSystem => Name != SID;

        /// <summary>
        /// 原始存取權限
        /// </summary>
        private readonly ActiveDirectoryAccessRule Raw;

        /// <summary>
        /// 建構子, 使用者只能異動不是透過繼承取得的部分
        /// </summary>
        /// <param name="customGUIDUnit">持有此權限的物件</param>
        /// <param name="isInherited">是否透過繼承取得</param>
        /// <param name="raw">原始存取權限</param>
        internal AccessRuleRelationPermission(in CustomGUIDUnit customGUIDUnit, in bool isInherited, ActiveDirectoryAccessRule raw)
        {
            CustomGUIDUnit = customGUIDUnit;
            IsInherited = isInherited;
            Raw = raw;
        }
    }
}
