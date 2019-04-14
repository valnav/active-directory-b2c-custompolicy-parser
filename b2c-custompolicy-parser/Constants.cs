using System.Collections.Generic;

namespace AADB2C.CustomPolicy.Parser
{
    public enum Tracker
    {
        New,
        Changed,
        Remove
        
    }
    public class Constants
    {
        
        public static string PolicyPrefix = "B2C_1AC_";
        internal const string ClientID = "client_id";
        internal const string ClientSecretRef = "client_secret";
        internal const string Secret = "secret";
        internal const string Resource = "resource_id";
        internal const string IdTokenAudience = "IdTokenAudience";

        internal const string TemplatePolicyBaseId = "B2C_1A_B2CTrustFrameworkBaseV2";
        internal const string TemplatePolicyExtensionsId = "TemplatePolicyExtensions";
        internal const string TemplateTenantPolicyId = "TemplateTenantPolicy";

        // leave these as-is - URIs for custom trust framework policy and keysets
        internal const string TrustFrameworkPolicesUri = "https://graph.microsoft.com/beta/trustFramework/Policies";
        internal const string TrustFrameworkPolicyByIDUri = "https://graph.microsoft.com/beta/trustFramework/Policies/{0}/$value";
        internal const string TrustFrameworkKeysetsUri = "https://graph.microsoft.com/beta/trustframework/keysets";
        internal const string TrustFrameworkKeysetsByIDUri = "https://graph.microsoft.com/beta/trustframework/keysets/{0}";
        internal static string TrustFrameworkKeysetsUploadSecret = $"{TrustFrameworkKeysetsByIDUri}/uploadSecret";
        internal const string TenantIdFor1P = "appcentertrust.onmicrosoft.com";

        public static Dictionary<string, string> SupportedRPs = new Dictionary<string, string>
        {
            { "susi", "B2CSignUpOrSignIn_V2"},
            { "passwordreset", "B2CPasswordReset_V2"},
            { "profileedit", "B2CUserProfileUpdate_V2"},
            { "ropc", "B2CResourceOwner"}
        };
        public static Dictionary<string, KeyValuePair<string, string>> SupportedIDPs = new Dictionary<string, KeyValuePair<string, string>>
        {
            { "localaccount", new KeyValuePair<string, string>("SelfAsserted-LocalAccountSignin-Email", "LocalAccountSigninEmailExchange" ) },
            {"localaccount-required", new KeyValuePair<string, string>("AAD-Common", "")},
            
            { "facebook", new KeyValuePair<string, string>("Facebook-OAUTH", "FacebookExchange")},
            { "msa", new KeyValuePair<string, string>("MSA-OIDC","MicrosoftAccountExchange")},
            { "google", new KeyValuePair<string, string>("Google-OAUTH","GoogleExchange")},
            { "linkedin", new KeyValuePair<string, string>("LinkedIn-OAUTH","LinkedInExchange")},
            //{ "MFA", new KeyValuePair<string, string>("PhoneFactor-Common")},
            { "github", new KeyValuePair<string, string>("GitHub-OAUTH2", "GitHubExchange")}
        };

        public static string UploadSecretForSocialIdPs = @"{
                                                            ""id"": ""{0}"",
                                                             ""keys"": [
                                                                {
                                                                 ""k"": ""{0}"",
                                                                 ""use"": ""sig"",
                                                                 ""kty"": ""oct""
                                                                }
                                                             ]
                                                        }"; 
        //public static Dictionary<string, string> SupportedReplacements = new Dictionary<string, string>
        //{
        //    { "storagereferenceid", "StorageReferenceId"},
        //    { "clientid", "client_id"},
        //    {"clientsecret", "client_secret" },
        //    { "idtokenaudience", "IdTokenAudience"},
        //    { "ropc", "ROPC"}
        //};

        //public const string AddMFA = @"<DisplayName>PhoneFactor</DisplayName>
        //                                <TechnicalProfiles>
        //                                <TechnicalProfile Id=""PhoneFactor-Common"">
        //                                <EnabledForUserJourneys>OnClaimsExistence</EnabledForUserJourneys>
        //                                </TechnicalProfile>
        //                                </TechnicalProfiles>";

        //public const string TenantPolicyTemplate = @"<TrustFrameworkPolicy 
        //                              xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
        //                              xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
        //                              xmlns=""http://schemas.microsoft.com/online/cpim/schemas/2013/06"" 
        //                              PolicySchemaVersion=""0.3.0.0"" 
        //                              TenantId=""yourtenant.onmicrosoft.com"" 
        //                              PolicyId=""B2C_1A_TrustFrameworkExtensions"" 
        //                              PublicPolicyUri=""http://yourtenant.onmicrosoft.com/B2C_1A_TrustFrameworkExtensions"">
        //                              <BasePolicy>
        //                                <TenantId>appcentertrust.onmicrosoft.com</TenantId>
        //                                <PolicyId>B2C_1A_B2CTrustFrameworkBaseV2</PolicyId>
        //                              </BasePolicy>
        //                             <BuildingBlocks>

        //                              </BuildingBlocks>
        //                            <ClaimsProviders>
        //                            </ClaimsProviders>

        //                            </TrustFrameworkPolicy>";
    }
}
