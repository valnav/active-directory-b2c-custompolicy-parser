namespace aad_b2c_parse_trustframeworkpolicy
{
    internal class Constants
    {
        // TODO: update "ClientIdForUserAuthn" with your app guid and "Tenant" with your tenant name
        //       see README.md for instructions

        // Client ID is the application guid used uniquely identify itself to the v2.0 authentication endpoint
        public const string ClientIdForUserAuthn = "dc852c37-8bad-4f6d-992c-efe3d7b15655";
        // Your tenant Name, for example "myb2ctenant.onmicrosoft.com"
        public const string Tenant = "b2cconsumer.onmicrosoft.com";

        // leave these as-is - URIs used for auth
        public const string AuthorityUri = "https://login.microsoftonline.com/" + Tenant + "/oauth2/v2.0/token";
        public const string RedirectUriForAppAuthn = "https://login.microsoftonline.com";

        // leave these as-is - Private Preview Graph URIs for custom trust framework policy
        public const string TrustFrameworkPolicesUri = "https://graph.microsoft.com/testcpimtf/trustFrameworkPolicies";
        public const string TrustFrameworkPolicyByIDUri = "https://graph.microsoft.com/testcpimtf/trustFrameworkPolicies/{0}/$value";
    }
}
