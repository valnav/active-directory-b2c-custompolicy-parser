using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace AADB2C.CustomPolicy.Parser
{
    public class CustomPolicyRestHelper
    {
        IEFParser Parser;
        public CustomPolicyRestHelper(IEFParser parser, string token)
        {
            TokenForUser = token;
            Parser = parser;
        }
        public string TokenForUser { get; set; }


        internal void LoadPolicies()
        {
            var request = HttpGet(Constants.TrustFrameworkPolicesUri);
            string response;
            if (!ExtractResponse(request, out response))
                throw new InvalidOperationException(string.Format(" getting TFP from tenant failed with {0}", response));

            var jo = JObject.Parse(response);
            JArray policyIds = (JArray)jo["value"];
            foreach (var item in policyIds.Children<JObject>())
            {

                LoadPolicy(item);
            }
        }
        private void LoadPolicy(JObject item)
        {
            string propertyValue = (string)item.Property("id").Value;
            var tfp = GetPolicy(propertyValue);
            tfp.PolicyConstraints = new TrustFrameworkPolicyPolicyConstraints();
            //skip adding the extension because it gets added in the if below
            if (Parser.PolicyIdsInTenant.ContainsKey(propertyValue))
            {
                Debug.WriteLine("Should be the extensions that got added by one of the RPs {0}", propertyValue);

            }
            else if (tfp.BasePolicy != null && tfp.RelyingParty != null)
            {
                tfp.PolicyConstraints.IsExtension = false;
                var parent = GetPolicy(tfp.BasePolicy?.PolicyId);
                if (parent != null && parent.BasePolicy != null && !string.IsNullOrWhiteSpace(parent.BasePolicy.TenantId))
                {
                    if (parent.BasePolicy.TenantId.ToLower().Equals(Constants.TenantIdFor1P))
                    {
                        parent.PolicyConstraints.Is1POwned = true;
                        parent.PolicyConstraints.IsExtension = true;
                        tfp.PolicyConstraints.Is1POwned = true;
                        Parser.TenantPolicyId = tfp.PolicyId;
                        Parser.PolicyIdsInTenant[propertyValue] = parent;
                        Debug.WriteLine(string.Format("1P Policy {0}'s base policy found {1}", tfp.PolicyId, tfp.BasePolicy?.PolicyId));
                    }
                    else
                        Debug.WriteLine(string.Format("Customer owned Policy {0}'s base policy found {1}", tfp.PolicyId, tfp.BasePolicy?.PolicyId));

                }
                else
                {
                    Debug.WriteLine(string.Format("{0} base policy not found {1}", tfp.PolicyId, tfp.BasePolicy?.PolicyId));
                }

            }
            else
            {
                Debug.WriteLine("existing tenant - this policy can be base or extensions not managed by app center - {0}", tfp.PolicyId);
            }

            Parser.PolicyIdsInTenant[propertyValue] = tfp;
        }
        internal void LoadKeysets()
        {
            HttpRequestMessage request = HttpGet(Constants.TrustFrameworkKeysetsUri);
            string response;
            if (!ExtractResponse(request, out response))
            {
                throw new InvalidOperationException(response);
            }
            var jo = JObject.Parse(response);
            JArray jArray = (JArray)jo["value"];

            foreach (var item in jArray.Children<JObject>())
            {

                LoadKeySetFromJson(item);

            }
        }

        private void LoadKeySetFromJson(JObject item)
        {
            var id = item.Value<string>("id");
            var request = HttpGetID(Constants.TrustFrameworkKeysetsUri, id);
            string response;
            if (!ExtractResponse(request, out response))
            {
                throw new InvalidOperationException(response);
            }

            var jo = JObject.Parse(response);
            var jt = jo["value"];
            var ks = TrustFrameworkKeyset.FromJson(jt.ToString());

            Parser.KeySetsInTenant[id] = ks;
        }



        private void AddHeaders(HttpRequestMessage requestMessage)
        {
            if (TokenForUser == null)
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

        public HttpRequestMessage HttpGet(string uri)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            AddHeaders(request);
            return request;
        }

        public HttpRequestMessage HttpGetID(string uri, string id)
        {
            string uriWithID = String.Format(uri, id);
            return HttpGet(uriWithID);
        }

        public HttpRequestMessage HttpPutID(string uri, string id, string xml, params string[] addl)
        {
            var contentType = addl.IsNullOrEmpty() ? "application/xml" : $"application/{addl[0]}";
            string uriWithID = String.Format(uri, id);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uriWithID);
            AddHeaders(request);
            request.Content = new StringContent(xml, Encoding.UTF8, contentType);
            return request;
        }

        public HttpRequestMessage HttpPost(string uri, string xml, params string[] addl)
        {
            var contentType = addl.IsNullOrEmpty() ? "application/xml" : $"application/{addl[0]}";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            AddHeaders(request);
            request.Content = new StringContent(xml, Encoding.UTF8, contentType);
            return request;
        }

        public HttpRequestMessage HttpDeleteID(string uri, string id)
        {
            string uriWithID = String.Format(uri, id);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uriWithID);
            AddHeaders(request);
            return request;
        }
        //https://graph.microsoft.com/beta/trustFramework/policies/B2C_1A_Test/$value
        public TrustFrameworkPolicy GetPolicy(string policyId)
        {
            //check dictionary before running the rest
            if (Parser.PolicyIdsInTenant.ContainsKey(policyId) && Parser.PolicyIdsInTenant[policyId] != null)
                return Parser.PolicyIdsInTenant[policyId];

            //else download from cpim
            TrustFrameworkPolicy tfPolicy = null;
            HttpRequestMessage request = HttpGetID(Constants.TrustFrameworkPolicyByIDUri, policyId);
            if (!ExtractResponse(request, out string content))
                throw new InvalidOperationException(string.Format(" Get TFP={1} on tenant failed with {0}", content, policyId));

            tfPolicy = ResourceHelper.ConvertXmlToTFP(content);

            return tfPolicy;
        }

       
       
        public bool CreatePolicy(TrustFrameworkPolicy tfp)
        {
            bool ret = false;
            string xml = ResourceHelper.SerializeToXml(tfp);
            HttpRequestMessage request = HttpPutID(Constants.TrustFrameworkPolicyByIDUri, tfp.PolicyId, xml);
            try
            {
                if (!ExtractResponse(request, out string response))
                    throw new InvalidOperationException(string.Format(" Create TFP on tenant failed with {0}", response));

                ret = true;
            }
            catch (Exception)
            {

                throw;
            }


            return ret;
        }

        //https://graph.microsoft.com/beta/trustFramework/policies/B2C_1A_Test/$value
        public bool UpdatePolicy(string policyId)
        {
            bool ret = false;
            TrustFrameworkPolicy tfp = null;
            try
            {
                tfp = GetPolicy(policyId);
                ret = UpdatePolicy(tfp);
            }
            catch (Exception)
            {

                throw;
            }


            return ret;
        }

        public bool UpdatePolicy(TrustFrameworkPolicy tfp)
        {
            bool ret = false;
            string xml = ResourceHelper.SerializeToXml(tfp);
            HttpRequestMessage request = HttpPutID(Constants.TrustFrameworkPolicyByIDUri, tfp.PolicyId, xml);
            try
            {
                if (!ExtractResponse(request, out string content))
                    throw new InvalidOperationException(string.Format(" Update TFP={1} on tenant failed with {0}", content, tfp.PolicyId));

                ret = true;
            }
            catch (Exception)
            {

                throw;
            }


            return ret;
        }

        public bool DeletePolicy(TrustFrameworkPolicy tfp)
        {
            bool ret = false;

            HttpRequestMessage request = HttpDeleteID(Constants.TrustFrameworkPolicyByIDUri, tfp.PolicyId);
            try
            {
                if (!ExtractResponse(request, out string content))
                    throw new InvalidOperationException(string.Format(" Delete TFP={1} on tenant failed with {0}", content, tfp.PolicyId));

                ret = true;
            }
            catch (Exception)
            {

                throw;
            }
            return ret;
        }

        public bool CreateKeyset(TrustFrameworkKeyset keyset)
        {
            bool ret = false;
            string json = keyset.ToJson();
            HttpRequestMessage request = HttpPost(Constants.TrustFrameworkKeysetsUri, json, "json");
            try
            {
                if (!ExtractResponse(request, out string response))
                    throw new InvalidOperationException(string.Format(" Create Keyset on tenant failed with {0}", response));

                ret = true;
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex);
                throw;
            }


            return ret;
        }
        public bool UploadKeyset(TrustFrameworkKeyset keyset)
        {
            bool ret = false;
            //todo: bug in sending id for upload secret in the json payload and hence need to remove.
            string id = keyset.Id;
            string json = keyset.ToJson();
            var keyset2 = TrustFrameworkKeyset.FromJson(json);
            keyset2.Id = null;
            json = keyset.ToJson();
            HttpRequestMessage request = HttpPost(string.Format(Constants.TrustFrameworkKeysetsUploadSecret, id), json, "json");
            try
            {
                if (!ExtractResponse(request, out string response))
                    throw new InvalidOperationException(string.Format(" Create Keyset on tenant failed with {0}", response));

                ret = true;
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex);
                throw;
            }


            return ret;
        }
        public bool DeleteKeyset(TrustFrameworkKeyset keyset)
        {
            bool ret = false;
            string json = keyset.ToJson();
            HttpRequestMessage request = HttpDeleteID(Constants.TrustFrameworkKeysetsByIDUri, keyset.Id);
            try
            {
                if (!ExtractResponse(request, out string response))
                    throw new InvalidOperationException(string.Format(" Create Keyset on tenant failed with {0}", response));

                ret = true;
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex);
                throw;
            }


            return ret;
        }
        public bool ExtractResponse(HttpRequestMessage request, out string responseString)
        {
            bool ret = true;
            HttpClient httpClient = new HttpClient();
            Task<HttpResponseMessage> responseTask = httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            responseTask.Wait();
            HttpResponseMessage response = responseTask.Result;
            if (!response.IsSuccessStatusCode)
            {
                ret = false;
                Debug.WriteLine("Error Calling the Graph API HTTP Status={0}", response.StatusCode);
            }

            Debug.WriteLine(response.Headers);
            Task<string> taskContentString = response.Content.ReadAsStringAsync();
            taskContentString.Wait();
            Debug.WriteLine(taskContentString.Result);

            responseString =  taskContentString.Result;
            return ret;
        }
        
    }
}
