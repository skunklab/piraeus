﻿using System.Management.Automation;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Remove, "CaplPolicy")]
    public class RemoveCaplPolicyCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Access control policy URI string that identifies the policy.", Mandatory = true)]
        public string PolicyId;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url = string.Format("{0}/api/accesscontrol/deleteaccesscontrolpolicy?policyuristring={1}",
                ServiceUrl, PolicyId);
            RestRequestBuilder builder =
                new RestRequestBuilder("DELETE", url, RestConstants.ContentType.Json, true, SecurityToken);
            RestRequest request = new RestRequest(builder);

            request.Delete();
        }
    }
}