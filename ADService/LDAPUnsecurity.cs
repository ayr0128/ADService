namespace ADService
{
    /// <summary>
    /// 預設 LDAP 不透過簽證驗證
    /// </summary>
    public class LDAPUnsecurity : LDAPServe
    {
        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="domain">組織伺服器的 固定IP 或者 綁定DNS </param>
        public LDAPUnsecurity(in string domain) : base(domain, UNSECURITY_PORT) { }
    }
}
