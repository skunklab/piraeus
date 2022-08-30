﻿using System;
using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "PiraeusWebServiceSubscription")]
    public class AddAzureWebServiceSubscriptionCmdlet : Cmdlet
    {
        [Parameter(
            HelpMessage =
                "(Optional) Audience to include in security token sent to Web service for symmetric key tokens.",
            Mandatory = false)]
        public string Audience;

        [Parameter(HelpMessage = "Description of the subscription.", Mandatory = false)]
        public string Description;

        [Parameter(
            HelpMessage =
                "(Optional) Issuer to include in security token sent to Web service for symmetric key tokens.",
            Mandatory = false)]
        public string Issuer;

        [Parameter(
            HelpMessage =
                "(Optional) Symmetric key used to build security token for authentication with Web service when TokenType is JWT or SWT.",
            Mandatory = false)]
        public string Key;

        [Parameter(HelpMessage = "Unique URI identifier of resource to subscribe.", Mandatory = true)]
        public string ResourceUriString;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        [Parameter(HelpMessage = "Type of security token to be used when sending to Web service.", Mandatory = true)]
        public SecurityTokenType TokenType;

        [Parameter(HelpMessage = "URL of Web service to send messages which can include a query string.",
            Mandatory = true)]
        public string WebServiceUrl;

        protected override void ProcessRecord()
        {
            string uriString = WebServiceUrl;

            Uri uri = new Uri(uriString);

            string query = !string.IsNullOrEmpty(Issuer) && !string.IsNullOrEmpty(Audience)
                ? string.Format("issuer={0}&audience={1}", Issuer, Audience)
                : !string.IsNullOrEmpty(Issuer)
                    ? string.Format("issuer={0}", Issuer)
                    : !string.IsNullOrEmpty(Audience)
                        ? string.Format("audience={0}", Audience)
                        : null;

            uriString = !string.IsNullOrEmpty(uri.Query) && !string.IsNullOrEmpty(query)
                ? string.Format("&{0}&{1}", uriString, query)
                : string.IsNullOrEmpty(uri.Query) && !string.IsNullOrEmpty(query)
                    ? string.Format("?{0}&{1}", uriString, query)
                    : uriString;

            SubscriptionMetadata metadata = new SubscriptionMetadata
            {
                IsEphemeral = false,
                NotifyAddress = uriString,
                SymmetricKey = Key,
                TokenType = TokenType,
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