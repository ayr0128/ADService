namespace ADService
{
    /// <summary>
    /// 預設 LDAP 透過簽證驗證
    /// </summary>
    public class LDAPSecurity : LDAPServe
    {
        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="domain">組織伺服器的 固定IP 或者 綁定DNS </param>
        public LDAPSecurity(in string domain) : base(domain, SECURITY_PORT) { }
    }
}