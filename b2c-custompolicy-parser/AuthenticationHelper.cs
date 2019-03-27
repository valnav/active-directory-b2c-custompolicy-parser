using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace aad_b2c_parse_trustframeworkpolicy
{
    class AuthenticationHelper
    {
        // The test endpoint currently does not require a specific scope.
        // By public preview, this API will require Policy.ReadWrite.All permission as an admin-only scope,
        // so authorization will fail if you sign in with a non-admin account.
        // For now, this API is only accessible on tenants that have been allow listed
        public static string[] Scopes = { "User.Read" };//, "Policy.ReadWrite.TrustFramework" };

        public static PublicClientApplication IdentityClientApp = new PublicClientApplication(Constants.ClientIdForUserAuthn);
        public static string TokenForUser = null;
        public static DateTimeOffset Expiration;

        private static GraphServiceClient graphClient = null;

        // Get an access token for the given context and resourceId. An attempt is first made to 
        // acquire the token silently. If that fails, then we try to acquire the token by prompting the user.
        public static GraphServiceClient GetAuthenticatedClientForUser()
        {
            // Create Microsoft Graph client.
            try
            {
                //IdentityClientApp.RedirectUri = @"msal{Constants.ClientIdForUserAuthn}://auth";
                graphClient = new GraphServiceClient(
                    "https://graph.microsoft.com/v1.0",
                    new DelegateAuthenticationProvider(
                        async (requestMessage) =>
                        {
                            var token = await GetTokenForUserAsync();
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                            // This header has been added to identify usage of this sample in the Microsoft Graph service.  You are free to remove it without impacting functionlity.
                            requestMessage.Headers.Add("SampleID", "console-csharp-iefparser");
                        }));
                return graphClient;
            }

            catch (Exception ex)
            {
                Debug.WriteLine("Could not create a graph client: " + ex.Message);
            }

            return graphClient;
        }

        public static void AddHeaders(HttpRequestMessage requestMessage)
        {
            if(TokenForUser == null)
            {
                Debug.WriteLine("Call GetAuthenticatedClientForUser first");
            }

            try
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", TokenForUser);
                requestMessage.Headers.Add("SampleID", "console-csharp-trustframeworkpolicy");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not add headers to HttpRequestMessage: " + ex.Message);
            }
        }

        /// <summary>
        /// Get Token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public static async Task<string> GetTokenForUserAsync()
        {
            AuthenticationResult authResult;
            try
            {
                var accounts = await IdentityClientApp.GetAccountsAsync();
                authResult = await IdentityClientApp.AcquireTokenSilentAsync(Scopes, accounts.FirstOrDefault());
                TokenForUser = authResult.AccessToken;
            }

            catch (Exception)
            {
                if (TokenForUser == null || Expiration <= DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    try
                    {
                        var r = IdentityClientApp.AcquireTokenInteractive(Scopes, null);
                        authResult = await r.ExecuteAsync();
                        TokenForUser = authResult.AccessToken;
                        Expiration = authResult.ExpiresOn;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("token interactive: " + ex.Message);

                    }
                    

                    
                }
            }

            return TokenForUser;
        }

        /// <summary>
        /// Signs the user out of the service.
        /// </summary>
        public static void SignOut()
        {
            foreach (var user in IdentityClientApp.GetAccountsAsync().Result)
            {
                IdentityClientApp.RemoveAsync(user);
            }
            graphClient = null;
            TokenForUser = null;
        }

    }
}
