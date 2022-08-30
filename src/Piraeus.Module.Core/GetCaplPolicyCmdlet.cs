﻿using System.Management.Automation;
using Capl.Authorization;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Get, "CaplPolicy")]
    public class GetCaplPolicyCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Access control policy URI string that identifies the policy.", Mandatory = true)]
        public string PolicyId;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url = string.Format("{0}/api/accesscontrol/getaccesscontrolpolicy?policyuristring={1}", ServiceUrl,
                PolicyId);
            RestRequestBuilder builder =
                new RestRequestBuilder("GET", url, RestConstants.ContentType.Xml, true, SecurityToken);
            RestRequest request = new RestRequest(builder);

            AuthorizationPolicy policy = request.Get<AuthorizationPolicy>();

            WriteObject(policy);
        }
    }
}