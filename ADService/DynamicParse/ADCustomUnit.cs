using ADService.Environments;
using ADService.Protocol;
using System.Linq;

namespace ADService.DynamicParse
{
    /// <summary>
    /// 所有物件都是以此物件為基礎進行構成
    /// </summary>
    public class ADCustomUnit
    {
        /// <summary>
        /// 區分名稱
        /// </summary>
        [ADDescriptionProperty(Properties.C_DISTINGUISHEDNAME)]
        public string DistinguishedName { get; internal set; }
        /// <summary>
        /// 物件名稱
        /// </summary>
        [ADDescriptionProperty(Properties.P_NAME)]
        public string Name { get; internal set; }
        /// <summary>
        /// 物件類別
        /// </summary>
        [ADDescriptionProperty(Properties.C_OBJECTCLASS)]
        public string[] Classes { get; internal set; }

        /// <summary>
        /// 獲取自身隸屬呃組織, 若物件是根目錄時將提供空字串
        /// </summary>
        public string OrganizationBelong
        {
            get
            {
                // 物件名稱
                string className = Classes.Last();
                // 不可以視網域跟目錄
                if (className == LDAPCategory.CLASS_DOMAINDNS)
                {
                    // 返回空字串
                    return string.Empty;
                }

                // 找到名稱的位置: 必定能找到
                int index = DistinguishedName.IndexOf(Name);
                // 切割字串取得目標所在的組織單位
                return DistinguishedName.Substring(index + Name.Length + 1);
            }
        }
    }
}
