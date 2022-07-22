using ADService.DynamicParse;

namespace ADService.Protocol
{
    /// <summary>
    /// 描述元件關係: 注意此類別不會拿來作為動態元件
    /// </summary>
    public sealed class ADRelationShip
    {
        /// <summary>
        /// 此物件與查詢目標的關係
        /// </summary>
        public InterpersonalRelationFlags RelationFlags { get; private set; }

        /// <summary>
        /// 與指定目標的隸屬關係
        /// </summary>
        public ADCustomRelation RelationDriveAD { get; private set; }

        /// <summary>
        /// 取得物件與自身的關係
        /// </summary>
        /// <param name="relationDriveAD">查詢的物件</param>
        /// <param name="relationFlags">關係描述</param>
        internal ADRelationShip(in ADCustomRelation relationDriveAD, in InterpersonalRelationFlags relationFlags)
        {
            RelationDriveAD = relationDriveAD;
            RelationFlags = relationFlags;
        }
    }
}
