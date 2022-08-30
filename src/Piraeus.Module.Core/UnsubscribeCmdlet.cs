﻿using System.Management.Automation;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Remove, "PiraeusSubscription")]
    public class UnsubscribeCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        [Parameter(HelpMessage = "Unique URI identifier of subscription.", Mandatory = true)]
        public string SubscriptionUriString;

        protected override void ProcessRecord()
        {
            string url = string.Format("{0}/api/resource/unsubscribe?subscriptionuristring={1}", ServiceUrl,
                SubscriptionUriString);
            RestRequestBuilder builder =
                new RestRequestBuilder("DELETE", url, RestConstants.ContentType.Json, true, SecurityToken);
            RestRequest request = new RestRequest(builder);

            request.Delete();
        }
    }
}