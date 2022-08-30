﻿using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "PiraeusIotHubDirectMethodSubscription")]
    public class AddAzureIotHubDirectMethodSubscription : Cmdlet
    {
        [Parameter(HelpMessage = "Account name of IoT Hub, e.g, <account>.azure-devices.net", Mandatory = true)]
        public string Account;

        [Parameter(HelpMessage = "Description of the subscription.", Mandatory = false)]
        public string Description;

        [Parameter(HelpMessage = "Device ID that you will send messages.", Mandatory = true)]
        public string DeviceId;

        [Parameter(HelpMessage = "SAS token used for authentication.", Mandatory = true)]
        public string Key;

        [Parameter(HelpMessage = "Name key used for authentication.", Mandatory = true)]
        public string KeyName;

        [Parameter(HelpMessage = "Name of method to be called on device.", Mandatory = true)]
        public string Method;

        [Parameter(HelpMessage = "Unique URI identifier of resource to subscribe.", Mandatory = true)]
        public string ResourceUriString;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string uriString = string.Format("iothub://{0}.azure-devices.net?deviceid={1}&keyname={2}&method={3}",
                Account, DeviceId, KeyName, Method);

            SubscriptionMetadata metadata = new SubscriptionMetadata
            {
                IsEphemeral = false,
                NotifyAddress = uriString,
                SymmetricKey = Key,
                Description = Description
            };

            string url = string.Format("{0}/api/resource/subscribe?resourceuristring={1}", ServiceUrl,
                ResourceUriString);
            RestRequestBuilder builder =
                new RestRequestBuilder("POST", url, RestConstants.ContentType.Json, false, SecurityToken);
            RestRequest request = new RestRequest(builder);

            string subscriptionUriString = request.Post<SubscriptionMetadata, string>(metadata);

            WriteObject(subscriptionUriString);
        }
    }
}