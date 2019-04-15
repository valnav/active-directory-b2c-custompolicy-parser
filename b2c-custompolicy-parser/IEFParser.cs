using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

//using Microsoft.Cpim.SocialIdpPolicy;
//using Microsoft.Cpim.Common;
//using Microsoft.Cpim.Common.Diagnostics;
//using Microsoft.Cpim.Configuration;
//using Microsoft.Cpim.Data;
//using static Microsoft.Cpim.Configuration.CpimServiceConfiguration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Linq;

namespace AADB2C.CustomPolicy.Parser
{
    public class IEFParser
    {
        //property used for referencing the tenant id in policies
        internal string TenantId { get; set; }

        //property used for referencing base policy id for the tenant
        public string BasePolicyId { get; internal set; }

        //property for the extensions policy id for the tenant.
        public string TenantPolicyId { get; internal set; }

        //Dictionary for ALL policy in the tenant
        public Dictionary<string, TrustFrameworkPolicy> PolicyIdsInTenant { get; internal set; }
        //Dictionary for Keysets in the tenant
        public Dictionary<string, TrustFrameworkKeyset> KeySetsInTenant { get; internal set; }
        public bool NewTenant { get; private set; }

        public List<ClaimsProvider> AllClaimProviders { get; internal set; }
        public List<UserJourney> AllUserJourneys { get; internal set; }
        //internal List<TrustFrameworkKeyset> AllKeysets { get; set; }

        //reference to the extensions policy template
        public TrustFrameworkPolicy TemplateTenantPolicy { get; internal set; }

        //Dictionary for all the Template RPs 
        public Dictionary<string, TrustFrameworkPolicy> TemplateRPs { get; internal set; }
        //Reference to the base policy template
        public TrustFrameworkPolicy TemplateBasePolicy { get; internal set; }
        //reference to the extensions policy template
        public TrustFrameworkPolicy TemplatePolicyExtensions { get; internal set; }

        //Helps for REST, Resource management and Change Tracking
        public CustomPolicyRestHelper PolicyRestHelper { get; private set; }
        public ResourceHelper Helper { get; private set; }
        public PolicyTracker ChangeTracker { get; private set; }

        //todo: can remove and make it a local variable.
        internal Stream xsdStream;

        /*
         * TenantId is used to setup the parser
         * Token is used for invoking get/post/put on B2C resources
         */
        public IEFParser(string tenantId, string token)
        {
            PolicyIdsInTenant = new Dictionary<string, TrustFrameworkPolicy>();
            TemplateRPs = new Dictionary<string, TrustFrameworkPolicy>();
            KeySetsInTenant = new Dictionary<string, TrustFrameworkKeyset>();


            AllClaimProviders = new List<ClaimsProvider>();
            AllUserJourneys = new List<UserJourney>();

            PolicyRestHelper = new CustomPolicyRestHelper(this, token);
            Helper = new ResourceHelper(this);
            ChangeTracker = new PolicyTracker(this);
            TenantId = tenantId;
        }

        /*
         * The setup parser does the heavy lifting of initializing the parser 
         * responsible for loading all the templates
         * responsible for loading all the policys in the tenant
         * responsible for creating the tenant policy if one doesnt exist - this happens for new tenants
         */
        public bool SetUpParserForTenant()
        {
            bool ret = false;

            try
            {
                Helper.LoadEmbeddedResources();

                PolicyRestHelper.LoadPolicies();

                //todo: needs work to extend, if customer-owned base policy needs to be supported
                //Assuming a 1p model. This will not be set this and instead load this xml as an embedded resource.
                //so not looking for base in this tenant

                if (string.IsNullOrEmpty(TenantPolicyId))
                {
                    NewTenant = true;
                    if (!CreateTenantPolicy()) ret = false;
                }
                else
                {
                    Debug.WriteLine("Extension exists and is called {0}", TenantPolicyId);
                }
                PolicyRestHelper.LoadKeysets();

                AllClaimProviders = GetClaimsProviders();
                AllUserJourneys = GetUserJourneys();

                ret = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw ex;
            }
            return ret;

        }
        
        //make changes to idps
        internal void FixupTenantPolicy()
        {
            var tenantPolicy = PolicyIdsInTenant[TenantPolicyId];
            if (tenantPolicy == null)
                throw new ArgumentNullException(string.Format("{0} not found", TenantPolicyId));
            //do nothing
        }

        /*
         * <ClaimsProviderSelections>
            <ClaimsProviderSelection TargetClaimsExchangeId="FacebookExchange"/>
            <ClaimsProviderSelection ValidationClaimsExchangeId="LocalAccountSigninEmailExchange"/>
            <ClaimsProviderSelection TargetClaimsExchangeId="MicrosoftAccountExchange"/>
          </ClaimsProviderSelections>
          //right now it only handles susi
         */
        internal void FixUpRPs()
        {

            var tfp = PolicyIdsInTenant[Constants.PolicyPrefix + "susi"];
            if (tfp == null)
                return;
            var oSteps = tfp.UserJourneys[0].OrchestrationSteps[0];
            var cpss = oSteps.ClaimsProviderSelections;
            ClaimsProviderSelection[] cps;
            if (cpss.IsNullOrEmpty())
            {
                cpss = new ClaimsProviderSelections[1];
                oSteps.ClaimsProviderSelections = cpss;
                cps = new ClaimsProviderSelection[1];
                cpss[0].ClaimsProviderSelection = cps;
            }
            else
            {
                cps = cpss[0].ClaimsProviderSelection;
                if (cps.IsNullOrEmpty())
                {
                    cps = new ClaimsProviderSelection[1];
                    cpss[0].ClaimsProviderSelection = cps;
                }
            }


            foreach (var item in ChangeTracker.IdPChanges.Keys)
            {
                var tracker = ChangeTracker.IdPChanges[item];
                if (!Constants.SupportedIDPs.ContainsKey(item))
                    throw new InvalidOperationException(string.Format("{0} is not valid idp", item));
                var kv = Constants.SupportedIDPs[item];
                List<ClaimsProviderSelection> list;
                switch (tracker)
                {
                    case Tracker.New:
                        ClaimsProviderSelection cp = new ClaimsProviderSelection();
                        if (item == "localaccount")
                        {
                            cp.ValidationClaimsExchangeId = kv.Key;
                        }
                        else
                        {
                            cp.TargetClaimsExchangeId = kv.Key;
                        }
                        list = new List<ClaimsProviderSelection>(cps);
                        list.Add(cp);
                        cps = list.ToArray();
                        //cpss[0].ClaimsProviderSelection = cps;
                        break;
                    case Tracker.Remove:
                        if (cps.Length <= 0)
                            throw new InvalidOperationException(string.Format("{0} cannot be removed because ClaimsProviderSelections is empty", item));
                        cp = GetClaimsProviderSelection(cps, kv.Key);
                        list = new List<ClaimsProviderSelection>(cps);
                        list.Remove(cp);
                        cps = list.ToArray();
                        //oSteps.ClaimsProviderSelections = cpss;
                        break;
                }
            }
        }

        private bool CreateTenantPolicy()
        {
            bool ret = true;
            var tfp = ObjectExtensions.Clone<TrustFrameworkPolicy>(TemplateTenantPolicy);
            //TrustFrameworkPolicy tfp = new TrustFrameworkPolicy();
            var basePolicy = new BasePolicy
            {
                PolicyId = TemplateTenantPolicy.BasePolicy.PolicyId,
                TenantId = TemplateTenantPolicy.BasePolicy.TenantId
            };

            tfp.BasePolicy = basePolicy;
            tfp.TenantId = TenantId;
            tfp.PolicyId = GetNameFromTemplate(TemplateTenantPolicy.PolicyId);
            tfp.PublicPolicyUri = "http://" + TenantId + "/" + tfp.PolicyId;

            tfp.PolicySchemaVersion = "0.3.0.0";
            TenantPolicyId = tfp.PolicyId;
            tfp.BuildingBlocks = new BuildingBlocks();
            var policyConstraints = new TrustFrameworkPolicyPolicyConstraints
            {
                Is1POwned = true,
                IsExtension = true
            };
            tfp.PolicyConstraints = policyConstraints;
            
            var request = PolicyRestHelper.HttpGet(@"https://graph.microsoft.com/beta/applications?filter=startswith(displayName, 'b2c-extensions-app')");
            string response;
            ret = PolicyRestHelper.ExtractResponse(request, out response);
            var json = JObject.Parse(response);
            string id = (string)json.SelectToken("$.value[0].appId");
            string objectId = (string)json.SelectToken("$.value[0].id");
            var claimProvider = Array.Find(tfp.ClaimsProviders, cp => cp.TechnicalProfiles[0].Id == Constants.SupportedIDPs["localaccount-required"].Key);
            var tp = claimProvider.TechnicalProfiles[0];
            var tPMetadatas = tp.Metadata;
            var tpClientId = tPMetadatas[0];
            tpClientId.Value = id;
            var tpObjectId = tPMetadatas[1];
            tpClientId.Value = objectId;

            PolicyIdsInTenant[tfp.PolicyId] = tfp;


            ChangeTracker.UpdatePolicyChange(TenantPolicyId, Tracker.New);
            return ret;
        }

        public bool AddRelyingParty(string policyid, Dictionary<string, string> keyValuePairs)
        {

            bool ret = false;
            try
            {
                var tfp = ObjectExtensions.Clone<TrustFrameworkPolicy>(TemplateRPs[policyid]);
                tfp.TenantId = TenantId;
                tfp.BasePolicy.TenantId = TenantId;
                tfp.BasePolicy.PolicyId = TenantPolicyId;
                tfp.PolicyId = GetNameFromTemplate(tfp.PolicyId);
                tfp.PublicPolicyUri = "http://" + TenantId + "/" + tfp.PolicyId;
                tfp.PolicyConstraints = new TrustFrameworkPolicyPolicyConstraints { Is1POwned = true };

                //add RP to PolicyIds in Tenant
                PolicyIdsInTenant[tfp.PolicyId] = tfp;
                ret = true;
            }
            catch { throw new InvalidOperationException("error adding relying party"); }

            return ret;
        }

        public bool RemoveRelyingParty(string policyid)
        {
            bool ret = false;
            try
            {
                //var tfp = PolicyIdsInTenant[policyid];
                if (PolicyIdsInTenant.ContainsKey(policyid))
                {
                    PolicyIdsInTenant.Remove(policyid);
                    ret = true;
                }
                else
                    throw new ArgumentNullException(string.Format("policy doesnt exist {0}", policyid));
            }
            catch (Exception)
            {

                throw new InvalidOperationException("error removing RP");
            }

            return ret;
        }

        public bool AddIdentityProvider(string idpName, Dictionary<string, string> keyValuePairs)
        {
            bool ret = true;
            KeyValuePair<string, string> keyValuePair = Constants.SupportedIDPs[idpName];
            var tPName = keyValuePair.Key;
            var cpExchange = keyValuePair.Value;
            //create new from template
            if (string.IsNullOrEmpty(TenantPolicyId))
                return ret;
            var tenantPolicy = PolicyIdsInTenant[TenantPolicyId];
            if (ClaimsProviderExistsInTenant(tPName))
            {
                Debug.WriteLine(string.Format("claims provider exists already", tPName));
                return false;
            }
            ClaimsProvider claimsProvider;
            if (idpName == "localaccount")
            {
                //this is because we dont have this in the extensions and is only required for claimsproviderselection
                claimsProvider = new ClaimsProvider();
                claimsProvider.TechnicalProfiles = new TechnicalProfile[] { new TechnicalProfile() };
                claimsProvider.TechnicalProfiles[0].Id = tPName;
            }
            else
            {
                claimsProvider = GetClaimsProviderFromTemplate(tPName);

                //TODO: we are assuming only 1 TP exists in the claims provider.
                TechnicalProfile tp;
                int index = GetTechnicalProfile(claimsProvider, tPName, out tp);
                if (tp == null)
                {
                    Debug.WriteLine(string.Format("Technical Profile not found ", tPName));
                    throw new ArgumentNullException(string.Format("Technical Profile not found ", tPName));
                }

                if (keyValuePair.Equals(default(KeyValuePair<string, string>)))
                    throw new InvalidOperationException(string.Format("invalid idp {0}", idpName));


                List<ClaimsProvider> list = new List<ClaimsProvider>();
                //create an empty array
                if (!tenantPolicy.ClaimsProviders.IsNullOrEmpty())
                {
                    list = new List<ClaimsProvider>(tenantPolicy.ClaimsProviders);
                }

                claimsProvider = ObjectExtensions.Clone<ClaimsProvider>(claimsProvider);
                list.Add(claimsProvider);
                tenantPolicy.ClaimsProviders = list.ToArray();


                //we are assuming we are only adding 1 TP in the claims provider.
                //this is not using the tp found earlier because that's the one from the template
                //which is getting cloned.
                tp = claimsProvider.TechnicalProfiles[0];

                CryptographicKeysKey[] cryptographicKeys = tp.CryptographicKeys;
                var tPMetadata = tp.Metadata;
                List<metadataItemTYPE> mitList = !tPMetadata.IsNullOrEmpty() ? new List<metadataItemTYPE>(tPMetadata) : new List<metadataItemTYPE>();
                List<CryptographicKeysKey> cryptoKeys = !cryptographicKeys.IsNullOrEmpty() ? new List<CryptographicKeysKey>(cryptographicKeys) : new List<CryptographicKeysKey>();

                foreach (var item in keyValuePairs.Keys)
                {
                    if (item == Constants.ClientID)
                    {
                        metadataItemTYPE metadataItem = mitList.Find(mit => mit.Key == item);
                        if (metadataItem == null)
                        {
                            metadataItem = new metadataItemTYPE();
                            metadataItem.Key = item;
                            mitList.Add(metadataItem);
                        }
                        metadataItem.Value = keyValuePairs[item];

                    }
                    else if (item == Constants.ClientSecretRef)
                    {
                        CryptographicKeysKey cryptoKey = cryptoKeys.Find(cry => cry.Id == item);
                        if (cryptoKey == null)
                        {
                            cryptoKey = new CryptographicKeysKey();
                            cryptoKey.Id = item;
                            cryptoKeys.Add(cryptoKey);
                        }
                        cryptoKey.StorageReferenceId = keyValuePairs[item];

                    }
                    else if (item == Constants.Secret)
                    {
                        //clientsecretref must exist.
                        var id = keyValuePairs[Constants.ClientSecretRef];
                        var keyset = TrustFrameworkKeyset.FromJson(Constants.UploadSecretForSocialIdPs);
                        keyset.Id = id;
                        keyset.Keys[0].K = keyValuePairs[Constants.Secret];
                        var t = KeySetsInTenant.ContainsKey(id) ? Tracker.Changed : Tracker.New;
                        KeySetsInTenant[id] = keyset;
                        ChangeTracker.KeysetChanges[id] = t;

                    }
                }
                tp.Metadata = mitList.ToArray();
                tp.CryptographicKeys = cryptoKeys.ToArray();
            }
            //this is because the idp affects the claimsSelection
            ChangeTracker.UpdateIdPChange(idpName, Tracker.New);
            //for local and social idps add it to our list;
            if (ClaimsProviderExistsInTenant(tPName))
            {
                Debug.WriteLine(string.Format("removing existing claims provider, because adding one {0} with TP ID {1}", claimsProvider.DisplayName, tPName));
                var cp = AllClaimProviders.Find(cp2 => cp2.TechnicalProfiles[0].Id == tPName);
                AllClaimProviders.Remove(cp);
                //then remove 

            }
            //update TFP in policyidsintenant
            PolicyIdsInTenant[TenantPolicyId] = tenantPolicy;
            AllClaimProviders.Add(claimsProvider);
            return ret;
        }

        public bool RemoveIdentityProvider(string idpName)
        {
            bool ret = false;
            if (string.IsNullOrEmpty(TenantPolicyId))
                return ret;

            KeyValuePair<string, string> keyValuePair = Constants.SupportedIDPs[idpName];
            var cPName = keyValuePair.Key;
            var cpExchange = keyValuePair.Value;
            var cP = AllClaimProviders.Find(el => el.TechnicalProfiles[0].Id == cPName);
            if (cP == null)
                throw new ArgumentNullException(string.Format("{0} claims Provider not found", cPName));
            //because localaccount doesnt exist in the tenant policy extensions.
            if (idpName != "localaccount")
            {
                var tfp = PolicyIdsInTenant[TenantPolicyId];
                var list = new List<ClaimsProvider>(tfp.ClaimsProviders);
                list.Remove(cP);
                tfp.ClaimsProviders = list.ToArray();

            }
            //this is because the idp affects the claimsSelection especially for susi
            ChangeTracker.UpdateIdPChange(idpName, Tracker.Remove);

            return ret;
        }

        private ClaimsProviderSelection GetClaimsProviderSelection(ClaimsProviderSelection[] cPS, string name)
        {
            foreach (var item in cPS)
            {
                if (item.TargetClaimsExchangeId == name || item.ValidationClaimsExchangeId == name)
                    return item;
            }

            return null;
        }

        private ClaimsProvider GetClaimsProviderFromTemplate(string name)
        {
            return Array.Find<ClaimsProvider>(TemplatePolicyExtensions.ClaimsProviders, el => el.TechnicalProfiles[0].Id == name);
        }

        public bool ClaimsProviderExistsInTenant(string tPId)
        {
            //we are assuming only 1 TP exists in the claims provider.
            var exists = AllClaimProviders.Exists(cp => cp.TechnicalProfiles[0].Id == tPId);
            //exists &= Array.Exists<ClaimsProvider>(PolicyIdsInTenant[TenantPolicyId].ClaimsProviders, el => el.TechnicalProfiles[0].Id == tPId);
            return exists;
        }

        //callers get -1 if not found else returns index in CP
        internal int GetTechnicalProfile(ClaimsProvider cp, string tpName, out TechnicalProfile tp)
        {
            int index = -1;
            tp = null;
            if (cp.TechnicalProfiles != null && !cp.TechnicalProfiles.IsNullOrEmpty())
            {
                index = 0;
                for (; index < cp.TechnicalProfiles.Length; index++)
                {
                    if (tpName == cp.TechnicalProfiles[index].Id)
                    {
                        tp = cp.TechnicalProfiles[index];
                        return index;
                    }
                    index++;
                }
            }
            return index;
        }

        public List<UserJourney> GetUserJourneys()
        {
            List<UserJourney> userJourneys = new List<UserJourney>();

            foreach (var item in PolicyIdsInTenant.Values)
            {
                //policyconstraints should never be null;
                if (item.PolicyConstraints.Is1POwned)
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
            }

            return userJourneys;
        }

        public List<ClaimsProvider> GetClaimsProviders()
        {
            List<ClaimsProvider> claimsProviders = new List<ClaimsProvider>();

            foreach (var item in PolicyIdsInTenant.Values)
            {
                //policyconstraints should never be null;
                if (item.PolicyConstraints.Is1POwned)
                {
                    var tfp = item;

                    if (tfp.ClaimsProviders != null && tfp.ClaimsProviders.Length > 0)
                    {
                        claimsProviders.AddRange(tfp.ClaimsProviders);
                        Debug.WriteLine("for policy ID {0} found following claim providers - ", tfp.PolicyId);
                        foreach (var item2 in tfp.ClaimsProviders)
                        {
                            Debug.Write("{0}  ", item2.DisplayName);
                        }
                        Debug.WriteLine("");
                    }
                }
            }

            return claimsProviders;
        }

        private string GetNameFromTemplate(string templateId)
        {
            bool keepTrying = true;
            int i = 1;
            var pid = Constants.PolicyPrefix + templateId;
            if (PolicyIdsInTenant.ContainsKey(pid))
            {
                //this should do it
                while (keepTrying)
                {
                    pid = Constants.PolicyPrefix + string.Format("{0}_", i) + templateId;

                    if (!PolicyIdsInTenant.ContainsKey(pid)) keepTrying = false;

                    if (i > 10)
                        throw new InvalidDataException("tried 10 times generating  unique policyid, but failed hence giving up");

                }
            }
            return pid;
        }

    }


}
