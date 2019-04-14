using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AADB2C.CustomPolicy.Parser;

namespace AADB2C.CustomPolicy.Parser.Client
{
    public class B2CClientParser
    {
        IEFParser iEFParser;

        public void DoSomething()
        {
            iEFParser.AddRelyingParty("susi", null);
            //iEFParser.AddRelyingParty("passwordreset", null);
            iEFParser.AddIdentityProvider("localaccount", null);
            Dictionary<string, string> kvPairs = new Dictionary<string, string>
            {
                { "client_id", Guid.NewGuid().ToString() },
                {"client_secret", "B2C_1A_facebooksecret" },
                {"secret", Guid.NewGuid().ToString() }
            };
            iEFParser.AddIdentityProvider("facebook", kvPairs);

            kvPairs = new Dictionary<string, string>
            {
                { "client_id", Guid.NewGuid().ToString() },
                {"client_secret", "B2C_1A_googlesecret" },
                {"secret", Guid.NewGuid().ToString() }
            };
            iEFParser.AddIdentityProvider("google", kvPairs);


        }

        internal void TestAuth()
        {
            //existingtenant1p
            string clientId = "23982485-ee5c-4c44-8053-0a9834c84132";
            string tenantId = "b2cdelete3.onmicrosoft.com";
            var authHelper = new AuthenticationHelper(clientId, $"b2cadmin@{tenantId}");
            string token = authHelper.LoginAsAdmin();

            //existingtenant
            clientId = "dc852c37-8bad-4f6d-992c-efe3d7b15655";
            // Your tenant Name, for example "myb2ctenant.onmicrosoft.com"
            tenantId = "b2cconsumer.onmicrosoft.com";
            // Login as global admin of the Azure AD B2C tenant
            authHelper = new AuthenticationHelper(clientId, $"newadmin@{tenantId}");
            token = authHelper.LoginAsAdmin();

            //newtenant
            clientId = "ccf89a71-694d-46e2-aef2-297a8ee50c82";
            tenantId = "b2cnav.onmicrosoft.com";
            authHelper = new AuthenticationHelper(clientId, $"b2cadmin@{tenantId}");
            token = authHelper.LoginAsAdmin();

        }

        public void NewTenant()
        {
            string clientId = "ccf89a71-694d-46e2-aef2-297a8ee50c82";
            string tenantId = "b2cnav.onmicrosoft.com";
            var authHelper = new AuthenticationHelper(clientId, $"b2cadmin@{tenantId}");
            string token = authHelper.LoginAsAdmin();
            iEFParser = new IEFParser(tenantId, token);
            if (!iEFParser.SetUpParserForTenant())
            {
                throw new InvalidProgramException("setup parser failed");
            }
            DoSomething();

            iEFParser.ChangeTracker.FinalizeChanges();
            iEFParser.Helper.Download1POwnedPolicies();
            iEFParser.Helper.DownloadAllKeysets();


        }


        public void ExistingTenant()
        {
            // Client ID is the application guid used uniquely identify itself to the v2.0 authentication endpoint
            string clientId = "dc852c37-8bad-4f6d-992c-efe3d7b15655";
            // Your tenant Name, for example "myb2ctenant.onmicrosoft.com"
            string tenantId = "b2cconsumer.onmicrosoft.com";
            // Login as global admin of the Azure AD B2C tenant
            var authHelper = new AuthenticationHelper(clientId, $"newadmin@{tenantId}");
            string token = authHelper.LoginAsAdmin();
            iEFParser = new IEFParser(tenantId, token);
            if (!iEFParser.SetUpParserForTenant())
            {
                throw new InvalidProgramException("setup parser failed");
            }
            DoSomething();

            iEFParser.ChangeTracker.FinalizeChanges();
            iEFParser.Helper.Download1POwnedPolicies();
            iEFParser.Helper.DownloadAllKeysets();
        }

        public void ExistingTenantWith1P()
        {
            string clientId = "23982485-ee5c-4c44-8053-0a9834c84132";
            string tenantId = "b2cdelete3.onmicrosoft.com";
            var authHelper = new AuthenticationHelper(clientId, $"b2cadmin@{tenantId}");
            string token = authHelper.LoginAsAdmin();
            iEFParser = new IEFParser(tenantId, token);
            if (!iEFParser.SetUpParserForTenant())
            {
                throw new InvalidProgramException("setup parser failed");
            }
            DoSomething();

            iEFParser.ChangeTracker.FinalizeChanges();
            iEFParser.Helper.Download1POwnedPolicies();
            iEFParser.Helper.DownloadAllKeysets();
        }
        public static void PrintRequest(HttpRequestMessage request)
        {
            if (request != null)
            {
                Debug.Write(request.Method + " ");
                Debug.WriteLine(request.RequestUri);
                Debug.WriteLine(request.RequestUri);
                Debug.WriteLine("");
            }
            else
            {
                Debug.WriteLine("null request");
            }
        }
    }
}
