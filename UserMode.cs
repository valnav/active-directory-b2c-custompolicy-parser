using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace aad_b2c_parse_trustframeworkpolicy
{
    internal class UserMode
    {
        public static GraphServiceClient client;

        public static IEFParser Parser = new IEFParser(Constants.Tenant);
        public static bool CreateGraphClient()
        {
            try
            {
                //*********************************************************************
                // setup Microsoft Graph Client for delegated user.
                //*********************************************************************
                if (Constants.ClientIdForUserAuthn != "ENTER_YOUR_CLIENT_ID")
                {
                    client = AuthenticationHelper.GetAuthenticatedClientForUser();
                    return true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Debug.WriteLine("You haven't configured a value for ClientIdForUserAuthn in Constants.cs. Please follow the Readme instructions for configuring this application.");
                    Console.ResetColor();
                    Console.ReadKey();
                    return false;
                }
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

        public static HttpRequestMessage HttpGet(string uri)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            AuthenticationHelper.AddHeaders(request);
            return request;
        }

        public static HttpRequestMessage HttpGetID(string uri, string id)
        {
            string uriWithID = String.Format(uri, id);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uriWithID);
            AuthenticationHelper.AddHeaders(request);
            return request;
        }

        public static HttpRequestMessage HttpPutID(string uri, string id, string xml)
        {
            string uriWithID = String.Format(uri, id);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uriWithID);
            AuthenticationHelper.AddHeaders(request);
            request.Content = new StringContent(xml, Encoding.UTF8, "application/xml");
            return request;
        }

        public static HttpRequestMessage HttpPost(string uri, string xml)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            AuthenticationHelper.AddHeaders(request);
            request.Content = new StringContent(xml, Encoding.UTF8, "application/xml");
            return request;
        }

        public static HttpRequestMessage HttpDeleteID(string uri, string id)
        {
            string uriWithID = String.Format(uri, id);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uriWithID);
            AuthenticationHelper.AddHeaders(request);
            return request;
        }

        
        public static HttpRequestMessage Parse()
        {
            return Parser.ParsePolicyIds();
        }

        public static string ExtractResponse(HttpRequestMessage request)
        {
            HttpClient httpClient = new HttpClient();
            Task<HttpResponseMessage> responseTask = httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            responseTask.Wait();
            HttpResponseMessage response = responseTask.Result;
            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine("Error Calling the Graph API HTTP Status={0}", response.StatusCode);
            }

            Debug.WriteLine(response.Headers);
            Task<string> taskContentString = response.Content.ReadAsStringAsync();
            taskContentString.Wait();
            Debug.WriteLine(taskContentString.Result);

            return taskContentString.Result;
        }
        public static void LoginAsAdmin()
        {
            Debug.WriteLine("Login as a global admin of the tenant (example: admin@myb2c.onmicrosoft.com");
            Debug.WriteLine("=============================");

            if (CreateGraphClient())
            {
                User user = client.Me.Request().GetAsync().Result;
                Debug.WriteLine("Current user:    Id: {0}  UPN: {1}", user.Id, user.UserPrincipalName);
            }
        }
    }
}
