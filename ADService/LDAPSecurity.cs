namespace ADService
{
    /// <summary>
    /// 預設 LDAP 透過簽證驗證
    /// </summary>
    public class LDAPSecurity : LDAPServe
    {
        /// <summary>
        /// 建構子, 使用 <see href="https://docs.microsoft.com/en-us/troubleshoot/windows-server/networking/service-overview-and-network-port-requirements#active-directory-local-security-authority">Port:636 or 3269</see>
        /// </summary>
        /// <param name="domain">組織伺服器的 固定IP 或者 綁定DNS </param>
        public LDAPSecurity(in string domain) : base(domain, 636) { }
    }
}