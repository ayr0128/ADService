using ADService.Foundation;
using ADService.Media;
using System.Collections.Generic;

namespace ADService.Advanced
{
    /// <summary>
    /// 彙整所有目標物建的持有規則
    /// </summary>
    internal class LDAPAccessRules
    {
        /// <summary>
        /// 紀錄目標物件
        /// </summary>
        internal readonly LDAPObject Destination;
        /// <summary>
        /// 所有可用的存取方法
        /// </summary>
        internal readonly UnitControlAccess[] UnitControlAccesses;
        /// <summary>
        /// 所有可用的子物件
        /// </summary>
        internal readonly UnitSchemaClass[] UnitSchemaClasses;

        /// <summary>
        /// 建構子: 取得轉換並取得目標存取規則
        /// </summary>
        /// <param name="dispatcher">設定分配氣</param>
        /// <param name="destination">目標物件</param>
        internal LDAPAccessRules(ref LDAPConfigurationDispatcher dispatcher, in LDAPObject destination)
        {
            Destination = destination;

            UnitControlAccesses = dispatcher.GeControlAccess(Destination.driveUnitSchemaClasses);
            UnitSchemaClasses = dispatcher.GetChildrenClasess(Destination.driveUnitSchemaClasses);
        }
    }
}
