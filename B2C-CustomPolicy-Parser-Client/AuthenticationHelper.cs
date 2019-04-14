﻿using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace AADB2C.CustomPolicy.Parser.Client
{
    public class AuthenticationHelper
    {

        // This API requires "Policy.ReadWrite.TrustFramework" "TrustFrameworkKeySet.ReadWrite.All" permission as an admin-only scope,
        // so authorization will fail if you sign in with a non-admin account.
        // For now, this API is only accessible on tenants that have been allow listed
        public string[] Scopes = {

            "Directory.AccessAsUser.All","Directory.Read.All", "User.Read", "Policy.ReadWrite.TrustFramework", "TrustFrameworkKeySet.ReadWrite.All" };

        public IPublicClientApplication IdentityClientApp;
        //public PublicClientApplication IdentityClientApp;
        public string TokenForUser = null;
        public DateTimeOffset Expiration;

        private static GraphServiceClient graphClient = null;
        private string ClientId = "ENTER_YOUR_CLIENT_ID";

        public string LoginHint { get; private set; }
        public string TenantId { get; private set; }
        public Uri AADAuthority { get; private set; }

        public AuthenticationHelper(string clientId, string loginHint)
        {
            ClientId = clientId;
            LoginHint = loginHint;
            TenantId = loginHint.Split('@')[1];
            AADAuthority = new Uri($"https://login.microsoftonline.com/{TenantId}");
        }

        public string LoginAsAdmin()
        {

            
            IdentityClientApp = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority(AADAuthority)
                .WithRedirectUri($"msal{ClientId}://auth")
                .Build();
            TokenCacheHelper.EnableSerialization(IdentityClientApp.UserTokenCache);
            Debug.WriteLine("Login as a global admin of the tenant (example: admin@myb2c.onmicrosoft.com");
            Debug.WriteLine("=============================");

            if (CreateGraphClient())
            {
                User user = graphClient.Me.Request().GetAsync().Result;
                Debug.WriteLine("Current user:    Id: {0}  UPN: {1}", user.Id, user.UserPrincipalName);
            }

            return TokenForUser;
        }
        private bool CreateGraphClient()
        {
            try
            {
                //*********************************************************************
                // setup Microsoft Graph Client for delegated user.
                //*********************************************************************
                    graphClient = GetAuthenticatedClientForUser();
                    return true;
                
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Debug.WriteLine("Acquiring a token failed with the following error: {0}", ex.Message);
                if (ex.InnerException != null)
                {
                    //You should implement retry and back-off logic per the guidance given here:http://msdn.microsoft.com/en-us/library/dn168916.aspx
                    //InnerException Message will contain the HTTP error status codes mentioned in the link above
                    Debug.WriteLine("Error detail: {0}", ex.InnerException.Message);
                }
                Console.ResetColor();
                Console.ReadKey();
                return false;
            }
        }
        // Get an access token for the given context and resourceId. An attempt is first made to 
        // acquire the token silently. If that fails, then we try to acquire the token by prompting the user.
        public GraphServiceClient GetAuthenticatedClientForUser()
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
                throw ex;
            }

            return graphClient;
        }


        /// <summary>
        /// Get Token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public async Task<string> GetTokenForUserAsync()
        {
            AuthenticationResult authResult;
            var accounts = await IdentityClientApp.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            try
            {
                
                
                authResult = await IdentityClientApp.AcquireTokenSilent(Scopes, firstAccount)
                                            .ExecuteAsync();
                TokenForUser = authResult.AccessToken;
            }

            catch (MsalUiRequiredException ex)
            {
                
                Debug.WriteLine(ex.Message);
                if (TokenForUser == null || Expiration <= DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    try
                    {
                        var r = IdentityClientApp.AcquireTokenInteractive(Scopes)
                            
                            .WithLoginHint(LoginHint)
                            .WithPrompt(Prompt.SelectAccount);
                        authResult = await r.ExecuteAsync();
                        TokenForUser = authResult.AccessToken;
                        Expiration = authResult.ExpiresOn;
                    }
                    catch (MsalException ex2)
                    {
                        ex2.Data.Add("innerException", ex);
                        Debug.WriteLine("token interactive: " + ex2.Message);
                        throw ex2;
                    }
                    catch(Exception ex3)
                    {
                        Debug.WriteLine("other exception: " + ex3.Message);
                        throw ex3;

                    }



                }
            }

            return TokenForUser;
        }

        /// <summary>
        /// Signs the user out of the service.
        /// </summary>
        public void SignOut()
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
