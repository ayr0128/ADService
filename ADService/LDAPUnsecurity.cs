namespace ADService
{
    /// <summary>
    /// 預設 LDAP 不透過簽證驗證
    /// </summary>
    public class LDAPUnsecurity : LDAPServe
    {
        /// <summary>
        /// 建構子, 使用 <see href="https://docs.microsoft.com/en-us/troubleshoot/windows-server/networking/service-overview-and-network-port-requirements#active-directory-local-security-authority">Port:389 or 3268</see>
        /// </summary>
        /// <param name="domain">組織伺服器的 固定IP 或者 綁定DNS </param>
        public LDAPUnsecurity(in string domain) : base(domain, 389) { }
    }
}
