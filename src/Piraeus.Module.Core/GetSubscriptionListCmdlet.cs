﻿using System.Collections.Generic;
using System.Management.Automation;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Get, "PiraeusSubscriptionList")]
    public class GetSubscriptionListCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Unique URI identifier of resource.", Mandatory = true)]
        public string ResourceUriString;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url = string.Format("{0}/api/resource/getpisystemsubscriptionlist?resourceuristring={1}", ServiceUrl,
                ResourceUriString);
            RestRequestBuilder builder =
                new RestRequestBuilder("GET", url, RestConstants.ContentType.Json, true, SecurityToken);
            RestRequest request = new RestRequest(builder);

            IEnumerable<string> list = request.Get<IEnumerable<string>>();

            if (list != null)
            {
                WriteObject(list);
            }
            else
            {
                WriteObject("Empty");
            }
        }
    }
}