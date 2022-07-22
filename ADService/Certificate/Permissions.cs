using ADService.DynamicParse;
using System;
using System.Collections.Generic;

namespace ADService.Certificate
{
    /// <summary>
    /// 透過保證書與指定物件可獲取獲取相關權限書
    /// </summary>
    internal sealed class Permissions
    {
        /// <summary>
        /// 指定物件
        /// </summary>
        private readonly CustomSIDUnit CustomSIDUnit;
        /// <summary>
        /// 按持有權限的單元 SID 儲存的存取規則權限關係
        /// </summary>
        private readonly Dictionary<string, List<AccessRuleRelationPermission>> dictionarySIDWithAccessRuleRelationPermissions = new Dictionary<string, List<AccessRuleRelationPermission>>();

        /// <summary>
        /// 提供指定物件以及透過保證書獲取的持有全縣
        /// </summary>
        /// <param name="customSIDUnit">詞有或繼承這些權限的單位, 額外添加 SID 資訊</param>
        /// <param name="accessRuleRelationPermissions">存取規則權限</param>
        internal Permissions(in CustomSIDUnit customSIDUnit, in List<AccessRuleRelationPermission> accessRuleRelationPermissions)
        {
            CustomSIDUnit = customSIDUnit;

            // 轉換成按群組 SID 儲存的格式
            foreach (AccessRuleRelationPermission accessRuleRelationPermission in accessRuleRelationPermissions)
            {
                // 此權限的持有群組 SID
                string SID = accessRuleRelationPermission.SID;
                // 取得此群組 SID 持有的儲存存取權限表
                if(!dictionarySIDWithAccessRuleRelationPermissions.TryGetValue(SID, out List<AccessRuleRelationPermission> storedAccessRuleRelationPermissions))
                {
                    // 尚未新增則新增儲存陣列
                    storedAccessRuleRelationPermissions = new List<AccessRuleRelationPermission>();
                    // 儲存至指定群組中
                    dictionarySIDWithAccessRuleRelationPermissions.Add(SID, storedAccessRuleRelationPermissions);
                }

                // 由於採用址位置方式儲存, 所有修改陣列表內容就會同步異動到字典
                storedAccessRuleRelationPermissions.Add(accessRuleRelationPermission);
            }
        }

        /// <summary>
        /// 主體區分名稱
        /// </summary>
        internal string PrincipalDN => CustomSIDUnit.DistinguishedName;

        /// <summary>
        /// 取得持有這些權限的單元 SID
        /// </summary>
        internal string PrincipalSID => CustomSIDUnit.SID;

        /// <summary>
        /// 提供主體安全性序列識別號取得其應被套用的權限
        /// </summary>
        /// <param name="principalSID">主體安全性序列識別碼</param>
        /// <returns>主體安全性序列識別號應受到的存取規則權限</returns>
        internal AccessRuleRelationPermission[] ListWithSID(in string principalSID) => dictionarySIDWithAccessRuleRelationPermissions.TryGetValue(principalSID, out List<AccessRuleRelationPermission> accessRuleRelationPermissions) ? accessRuleRelationPermissions.ToArray() : Array.Empty<AccessRuleRelationPermission>();
    }
}
