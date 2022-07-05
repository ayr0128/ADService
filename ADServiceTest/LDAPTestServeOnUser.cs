using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADServiceFrameworkTest
{
    /// <summary>
    /// 測試非登入者的成員
    /// </summary>
    [TestClass]
    public class LDAPTestServeOnUser
    {
        /// <summary>
        /// 驗證點擊自身時的可用方法
        /// </summary>
        [TestMethod]
        public void Test_LDAP_Supported_Features() => Defines.Test_LDAP_Supported_Features(Defines.User, Defines.OriginPERSON1);

        /// <summary>
        /// 測試功能中需額外取得資訊的方法
        /// </summary>
        [TestMethod]
        public void Test_LDAP_Feature_InvokeMethod() => Defines.Test_LDAP_Feature_InvokeMethod(Defines.User, Defines.OriginPERSON1);

        /// <summary>
        /// 測試重新命名功能
        /// </summary>
        [TestMethod]
        public void Test_LDAP_Feature_ReName() => Defines.Test_LDAP_Feature_ReName(Defines.User, Defines.OriginPERSON1);

        /// <summary>
        /// 測試移動至功能
        /// </summary>
        [TestMethod]
        public void Test_LDAP_Feature_MoveTo() => Defines.Test_LDAP_Feature_MoveTo(Defines.User, Defines.OriginPERSON1, Defines.OriginOU3);
    }
}
