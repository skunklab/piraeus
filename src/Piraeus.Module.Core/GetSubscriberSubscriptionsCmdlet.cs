﻿using System.Collections.Generic;
using System.Management.Automation;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Get, "PiraeusSubscriberSubscriptions")]
    public class GetSubscriberSubscriptionsCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Identity of the subscriber", Mandatory = true)]
        public string Identity;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url = string.Format("{0}/api/subscription/GetSubscriberSubscriptions?identity={1}", ServiceUrl,
                Identity);
            RestRequestBuilder builder =
                new RestRequestBuilder("GET", url, RestConstants.ContentType.Json, true, SecurityToken);
            RestRequest request = new RestRequest(builder);

            IEnumerable<string> subscriptions = request.Get<IEnumerable<string>>();

            WriteObject(subscriptions);
        }
    }
}