using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Cpim.SocialIdpPolicy;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Cpim.Common;
using Microsoft.Cpim.Common.Diagnostics;
using Microsoft.Cpim.Configuration;
using Microsoft.Cpim.Data;
using static Microsoft.Cpim.Configuration.CpimServiceConfiguration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace aad_b2c_parse_trustframeworkpolicy
{
    public class IEFParser
    {
        
        public string TenantId { get; set; }

        public string BasePolicyId { get; set; }

        public string TenantPolicyId { get; set; }

        public Dictionary<string, TrustFrameworkPolicy> PolicyIdsInTenant { get; private set; }
        public List<ClaimsProvider> AllClaimProviders = new List<ClaimsProvider>();
        public List<UserJourney> AllUserJourneys = new List<UserJourney>();
        public string PolicyContent { get; set; }
        public IEFParser(string tenantId)
        {
            PolicyIdsInTenant = new Dictionary<string, TrustFrameworkPolicy>();
            
            TenantId = tenantId;
        }

        
        
        public HttpRequestMessage ParsePolicyIds()
        {
            HttpRequestMessage request = UserMode.HttpGet(Constants.TrustFrameworkPolicesUri);
            Program.PrintRequest(request);

            string response = UserMode.ExtractResponse(request);


            JObject jo = JObject.Parse(response);
            JArray policyIds = (JArray)jo["value"];
            foreach (var item in policyIds.Children<JObject>())
            {

                string propertyValue = (string)item.Property("id").Value;
                PolicyIdsInTenant[propertyValue] = GetPolicy(propertyValue);
                
            }

            
            if (IsBaseThenSet())
            {
                Debug.WriteLine("found base:{0}", BasePolicyId);
            }
            if (IsExtensionThenSet())
            {
                Debug.WriteLine("found Extension:{0}", TenantPolicyId);
            }

            AllClaimProviders = GetClaimsProviders();
            AllUserJourneys = GetUserJourneys();
            return request;

        }

        private List<UserJourney> GetUserJourneys()
        {
            List < UserJourney > userJourneys = new List<UserJourney>();

            foreach (var item in PolicyIdsInTenant.Values)
            {
                var tfp = item;
                if (tfp.UserJourneys != null && tfp.UserJourneys.Length > 0)
                {
                    userJourneys.AddRange(tfp.UserJourneys);
                    Debug.WriteLine("for policy ID {0} found following user journeys - ", tfp.PolicyId);
                    foreach (var item2 in tfp.UserJourneys)
                    {
                        Debug.Write("{0}  ", item2.Id);
                    }
                    Debug.WriteLine("");
                }
            }

            return userJourneys;
        }

        private List<ClaimsProvider> GetClaimsProviders()
        {
            List<ClaimsProvider> claimsProviders = new List<ClaimsProvider>();

            foreach (var item in PolicyIdsInTenant.Values)
            {
                var tfp = item;
                if (tfp.ClaimsProviders != null && tfp.ClaimsProviders.Length > 0) { 
                    claimsProviders.AddRange(tfp.ClaimsProviders);
                Debug.WriteLine("for policy ID {0} found following claim providers - ", tfp.PolicyId);
                foreach (var item2 in tfp.ClaimsProviders)
                {
                    Debug.Write("{0}  ", item2.DisplayName);
                }
                Debug.WriteLine("");
                }
            }

            return claimsProviders;
        }

        //https://graph.microsoft.com/beta/trustFramework/policies/B2C_1A_Test/$value
        public TrustFrameworkPolicy GetPolicy(string policyId)
        {
            string content = string.Empty;
            TrustFrameworkPolicy tfPolicy = null;
            HttpRequestMessage request = UserMode.HttpGetID(Constants.TrustFrameworkPolicyByIDUri, policyId);
            content = UserMode.ExtractResponse(request);
            //XmlDocument xDoc = new XmlDocument();
            //xDoc.LoadXml(content);
            XmlSerializer serializer = new XmlSerializer(typeof(TrustFrameworkPolicy));
            using (TextReader reader = new StringReader(content))
            {
                tfPolicy = (TrustFrameworkPolicy)serializer.Deserialize(reader);
            }

            Debug.WriteLine(tfPolicy.PolicyId);

            return tfPolicy;
        }

        public bool IsBaseThenSet()
        {
            bool ret = false;
            foreach (var item in PolicyIdsInTenant.Keys)
            {
                var tfp = PolicyIdsInTenant[item];
                if (tfp.BasePolicy == null) { 
                    BasePolicyId = tfp.PolicyId;

                    ret = true;
                }
            }
            return ret;

        }
        public bool IsExtensionThenSet()
        {
            bool ret = false;
            foreach (var item in PolicyIdsInTenant.Keys)
            {
                var tfp = PolicyIdsInTenant[item];
                if (tfp.BasePolicy != null && tfp.BasePolicy.PolicyId == BasePolicyId)
                {
                    TenantPolicyId = tfp.PolicyId;
                    ret = true;
                }
            }
            return ret;
        }

    }
}
