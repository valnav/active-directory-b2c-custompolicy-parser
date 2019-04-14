using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AADB2C.CustomPolicy.Parser
{
    public class PolicyTracker
    {
        
        IEFParser Parser;
        internal Dictionary<string, Tracker> PolicyChanges { get; private set; }
        internal Dictionary<string, Tracker> IdPChanges { get; private set; }
        internal Dictionary<string, Tracker> KeysetChanges { get; private set; }

        internal PolicyTracker(IEFParser parser)
        {
            PolicyChanges = new Dictionary<string, Tracker>();
            IdPChanges = new Dictionary<string, Tracker>();
            KeysetChanges = new Dictionary<string, Tracker>();
            Parser = parser;
        }
        public bool CommitAllChanges()
        {
            bool ret = false;
            //always update tenantpolicy first;
            if (Parser.PolicyIdsInTenant.ContainsKey(Parser.TenantPolicyId))
            {
                CommitPolicyChange(Parser.TenantPolicyId);
                Parser.PolicyIdsInTenant.Remove(Parser.TenantPolicyId);
            }

            foreach (var item in Parser.PolicyIdsInTenant.Keys)
            {
                var tfp = Parser.PolicyIdsInTenant[item];
                if (PolicyChanges.ContainsKey(item))
                {
                    CommitPolicyChange(item);
                }
            }
            foreach (var item in KeysetChanges.Keys)
            {
                if (KeysetChanges.ContainsKey(item)) CommitKeysetChange(item);
            }
            //reset tracker;
            PolicyChanges = new Dictionary<string, Tracker>();
            ret = true;
            return ret;
        }

        private void CommitKeysetChange(string item)
        {
            Enum switch_on = KeysetChanges[item];
            var ks = Parser.KeySetsInTenant.ContainsKey(item)? Parser.KeySetsInTenant[item] : null;

            if (ks == null)
                throw new ArgumentNullException(string.Format("{0} not found keyset ", item));

            switch (switch_on)
            {
                case Tracker.Changed:
                    Parser.PolicyRestHelper.UploadKeyset(ks);
                    break;
                case Tracker.New:
                    Parser.PolicyRestHelper.CreateKeyset(ks);
                    Parser.PolicyRestHelper.UploadKeyset(ks);
                    break;

                case Tracker.Remove:
                    Parser.PolicyRestHelper.DeleteKeyset(ks);
                    break;
            }


        }
        private void CommitPolicyChange(string item)
        {
            Enum switch_on = PolicyChanges[item];
            var tfp = Parser.PolicyIdsInTenant[item];

            if (tfp == null)
                throw new ArgumentNullException(string.Format("{0} not found policy dictionary", item));
            if (!tfp.PolicyConstraints.Is1POwned)
                throw new InvalidOperationException(string.Format("{0} found policy that is not 1p owned", item));
            switch (switch_on)
            {
                case Tracker.Changed:
                    Parser.PolicyRestHelper.UpdatePolicy(item);
                    break;
                case Tracker.New:
                    Parser.PolicyRestHelper.CreatePolicy(tfp);
                    break;

                case Tracker.Remove:
                    Parser.PolicyRestHelper.DeletePolicy(tfp);
                    break;
            }


        }
        private void CommitIdPChange(string item)
        {
            //right now do nothing.
        }

        internal void ValidateIdPChanges()
        {
            if (IdPChanges != null && IdPChanges.Count <= 0)
                return;
            bool foundIssue = false;
            foreach (var idPName in IdPChanges.Keys)
            {

                var tracker = IdPChanges[idPName];
                KeyValuePair<string, string> keyValuePair = Constants.SupportedIDPs[idPName];
                var cPName = keyValuePair.Key;
                var cpExchange = keyValuePair.Value;
                bool exists = Parser.ClaimsProviderExistsInTenant(cPName);
                switch (tracker)
                {
                    case Tracker.Remove:
                        foundIssue = true;
                        Debug.WriteLine(string.Format("idp {0} shouldnt be there", idPName));
                        break;
                    case Tracker.New:
                    case Tracker.Changed:
                    default:
                        break;
                }

               
            }
            if (foundIssue)
            {
                throw new InvalidOperationException("some idps werent removed from tenant policy");
            }
        }
        public void FinalizeChanges()
        {
            ValidateIdPChanges();
            Parser.FixupTenantPolicy();
            Parser.FixUpRPs();
            
        }

        //if key doesnt exist then add to tracker
        //basically if key exist check conditions below
        //if new is there and then delete is sent, then just delete basically an undo action
        //if new and then update is sent, then do nothing, stays new
        //if update and then delete, then change to delete
        //if update and then new - do nothing.... but shouldnt happen
        //if delete and then new - throw invalidoperation exception
        //if delete and then update - throw invalidoperation exception
        internal void UpdatePolicyChange(string name, Tracker tracker)
        {
            if (PolicyChanges.ContainsKey(name))
            {
                if (tracker == PolicyChanges[name])
                    //do nothing;
                    return;
                if (PolicyChanges[name] == Tracker.New && tracker == Tracker.Remove)
                {
                    PolicyChanges.Remove(name);
                    return;
                }

                if (PolicyChanges[name] == Tracker.New && tracker == Tracker.Changed)
                    return;

                if (PolicyChanges[name] == Tracker.Changed && tracker == Tracker.Remove)
                {
                    PolicyChanges[name] = tracker;
                    return;
                }
                if (PolicyChanges[name] == Tracker.Changed && tracker == Tracker.New)
                {
                    return;
                }
                if (PolicyChanges[name] == Tracker.Remove && tracker == Tracker.New)
                {
                    throw new InvalidOperationException(string.Format("The policy {0}, currently has {1} and are trying to change it to {2}", name, PolicyChanges[name].ToString(), tracker.ToString()));
                }
                if (PolicyChanges[name] == Tracker.Remove && tracker == Tracker.Changed)
                {
                    throw new InvalidOperationException(string.Format("The policy {0}, currently has {1} and are trying to change it to {2}", name, PolicyChanges[name].ToString(), tracker.ToString()));
                }

            }
            else
            {
                PolicyChanges[name] = tracker;
                return;
            }
        }
        //not checking for now
        //TODO: do some change tracking checks like in policy changes
        internal void UpdateIdPChange(string name, Tracker tracker)
        {
            IdPChanges[name] = tracker;
            return;
        }
    }
}
