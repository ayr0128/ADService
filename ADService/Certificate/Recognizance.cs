using ADService.Basis;
using ADService.DynamicParse;
using ADService.Protocol;
using System;

namespace ADService.Certificate
{
    /// <summary>
    /// 取自使用者列出自身條件讓第三方可以根據使用者關係對其他目標進行操作
    /// </summary>
    internal sealed class Recognizance
    {
        /// <summary>
        /// 設定資料
        /// </summary>
        internal readonly IUserAuthorization UserAuthorization;
        /// <summary>
        /// 製作此證書的持有者
        /// </summary>
        private DriveRelation DriveRelation;
        /// <summary>
        /// 證書持有者的隸屬關係, 用來保證可進行的操作
        /// </summary>
        private ADRelationShip[] RelationShipADs;

        /// <summary>
        /// 授權第三方可以根據喚起者的隸屬關係對乙方做出授權操作
        /// </summary>
        /// <param name="userAuthorization">授權操作</param>
        /// <param name="driveRelation">自身的隸屬關係, 用來取得 SID</param>
        /// <param name="relationShipADs">自身隸屬於那些群組或個人, 用來保證能實施的操作</param>
        internal Recognizance(
            in IUserAuthorization userAuthorization,
            in DriveRelation driveRelation,
            params ADRelationShip[] relationShipADs
        )
        {
            UserAuthorization = userAuthorization;
            DriveRelation = driveRelation;

            RelationShipADs = relationShipADs;
        }

        /// <summary>
        /// 取得持有這些權限的單元 SID
        /// </summary>
        internal string PrincipalSID => DriveRelation.SID;

        /// <summary>
        /// 遍歷關係網取得 SID
        /// </summary>
        internal string[] RelationPrincipalSIDs => Array.ConvertAll(RelationShipADs, relationShipAD => relationShipAD.RelationDriveAD.SID);
    }
}
