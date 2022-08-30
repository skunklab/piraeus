﻿using System.Management.Automation;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Remove, "PiraeusPskSecret")]
    public class RemovePskSecretCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Psk Identity", Mandatory = true)]
        public string Identity;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url = string.Format("{0}/api/psk/RemovePskSecret?key={1}", ServiceUrl, Identity);
            RestRequestBuilder builder =
                new RestRequestBuilder("DELETE", url, RestConstants.ContentType.Json, true, SecurityToken);
            RestRequest request = new RestRequest(builder);
            request.Delete();
        }
    }
}