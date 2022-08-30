
# Piraeus - PowerShell v6 Management
## Introduction
Below lists the PowerShell cmdlets, group by feature set that can be used to manage Piraeus.  

### API Security Cmdlet
| **Cmdlet**     | **Definition**                                                                                                                      |
|-------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| [Get-PiraeusManagmentToken](Get-PiraeusManagementToken.md)        |  Returns a security token used for subsequent calls to the management api using other cmdlets.                                    
----------------
### Access Control Cmdlets
See [CAPL Intro](caplintro.md)
| **Cmdlet**     | **Definition**                                                                                                                      |
|-------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| [Add-CaplPolicy](Add-CaplPolicy.md) | Adds or updates a CAPL Authorization Policy to Piraeus.
| [Get-CaplPolicy](Get-CaplPolicy.md) | Gets a CAPL Authorization Policy from Piraeus.
| [Remove-CaplPolicy](Remove-CaplPolicy.md) | Deletes a CAPL Authorization Policy from Piraeus.
| [New-CaplPolicy](Get-PiraeusManagementToken.md)        |  Returns a CAPL authorization. |
| [New-CaplRule](New-CaplRule.md) | Returns a new CAPL Rule used by one of the following (Policy, LogicalAnd, or LogicalOr)
| [New-CaplOperation](New-CaplOperation.md) | Return a new CAPL Operation used by a Rule.
| [New-CaplMatch](New-CaplMatch.md)| Returns a new CAPL Match Expression used by a Rule.
| [New-CaplLogicalAnd](New-CaplLogicalAnd.md) | Returns a CAPL Logical AND connective used by one of the following (LogicalOr, LogicalAnd, or Policy).
| [New-CaplLogicalOr](New-CaplLogicalOr.md) | Returns a CAPL Logical OR connective used by one of the following (LogicalOr, LogicalAnd, or Policy).
| [New-CaplTransform](New-CaplTransform.md) | Returns a CAPL Transform used by a Policy.
| [New-CaplLiteralClaim](New-CaplLiteralClaim.md) | Returns a CAPL Literal Claim used by a Transform.
---------------------------
### π-system Cmdlets
π-systems are a fundamental primitive in Piraeus.  This is the point of agreement between senders and receivers for specific events or message types.
| **Cmdlet**     | **Definition**                                                                                                                      |
|-------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| [Add-PiraeusEventMetadata](pisystems.md) | Adds or updates a π-system in Piraeus.
|[Get-PiraeusEventMetadata](Add-CaplPolicy) | Gets a π-system from Piraeus.
|[Get-PiraeusSigmaAlgebra](Add-CaplPolicy) | Gets a list of  π-system from Piraeus.
|[Remove-PiraeusEvent](Add-CaplPolicy) | Removes a π-system from Piraeus and any subscriptions to the π-system.
---------------

### Durable Subscription Cmdlets
Durable subscriptions are static routes attached to a π-system.  Most of time these are used by passive agents, i.e., agents that do not initialize connections to a Piraeus gateway.  For example, a Storage account or Web service. However, they can be used by active agents, which are automatically subscribed to a durable subscription by matching the active agents identity to the subscription.  Neither an active or passive agent can self-unsubscribe from a durable subscription.  This must be perform by an administrator using the Management API.
| **Cmdlet**     | **Definition**                                                                                                                      |
|-------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| [Add-PiraeusSubscription](pisystems.md) | Adds or updates a subscription to a  π-system in Piraeus. Use for active receivers with durable subscriptions.
|[Get-PiraeusSubscription](Add-CaplPolicy) | Gets a π-system from Piraeus.
|[Remove-PiraeusSubscription](Add-CaplPolicy) | Deletes a subscription to a π-system from Piraeus.
|[Add-PiraeusBlobStorageSubscription](Add-CaplPolicy) | Adds or updates a subscription to a  π-system in Piraeus for an Azure blob storage receiver.
|[Add-PiraeusCosmosDbSubscription](Add-CaplPolicy) | Adds or updates a subscription to a  π-system in Piraeus for an Azure Cosmos DB receiver.
|[Add-PiraeusDataLakeSubscription](Add-CaplPolicy) | Adds or updates a subscription to a  π-system in Piraeus for an Azure Data Lake receiver.
|[Add-PiraeusEventGridSubscription](Add-CaplPolicy) |Adds or updates a subscription to a  π-system in Piraeus for an Azure Event Grid receiver.
|[Add-PiraeusEventHubSubscription](Add-CaplPolicy) | Adds or updates a subscription to a  π-system in Piraeus for an Azure Event Hub receiver.
|[Add-PiraeusIotHubCommandSubscription](Add-CaplPolicy) | Adds or updates a subscription to a  π-system in Piraeus for an Azure IoT Hub command receiver. This means the subscription will send a message to an Azure IoT Hub device.
|[Add-PiraeusIotHubDeviceSubscription](Add-CaplPolicy) | Adds or updates a subscription to a  π-system in Piraeus for an Azure IoT Hub device receiver. This means the subscription acts as an Azure IoT Hub device and sends the subscription message to Azure IoT Hub.
|[Add-PiraeusIotHubDirectMethodSubscription](Add-CaplPolicy) | Adds or updates a subscription to a  π-system in Piraeus for an Azure IoT Hub direct method receiver. This means the subscription will forward the message as a direct method to an Azure IoT device or Azure IoT Edge module.
|[Add-PiraeusQueueStorageSubscription](Add-CaplPolicy) |Adds or updates a subscription to a  π-system in Piraeus for an Azure Storage Queue receiver.
|[Add-PiraeusServiceBusSubscription](Add-CaplPolicy) | Adds or updates a subscription to a  π-system in Piraeus for an Azure Service Bus receiver. This requires use of a Servie Bus Topic, Service Bus queues are not allowed.
|[Add-PiraeusWebServiceSubscription](Add-CaplPolicy) | Adds or updates a subscription to a  π-system in Piraeus sends to a Web service as an HTTP-POST.  This also can be used to send to Azure Functions.
|[Add-PiraeusRedisCacheSubscription](Add-CaplPolicy) | Adds or updates a subscription to a π-system in Piraeus for a Redis cache receiver.
|[Get-PiraeusSubscriptionList](Add-CaplPolicy) | Gets a list of subscriptions from a  π-system in Piraeus.
|[Get-PiraeusSubscriberSubscriptions](Add-CaplPolicy) | Gets a list of durable subscriptions for an identity.
---------------
### Metrics Cmdlets
These cmdlets retrieve metrics from π-systems orsubscriptions, e.g. number of messages, total bytes sent/received, and errors.

| **Cmdlet**     | **Definition**                                                                                                                      |
|-------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| [Get-PiraeusEventMetrics](pisystems.md) | Gets metrics from a π-system.
|[Get-PiraeusSubscriptionMetrics](Add-CaplPolicy) | Gets metrics from a subscription.
----------------------------------
### Service Identity Cmdlets
These cmdlets can be used (if needed) to configure the Piraeus service identity.  The service identity is used only when sending to a durable subscription that requires an identity, e.g., most Web services.

| **Cmdlet**     | **Definition**                                                                                                                      |
|-------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| [Add-PiraeusServiceIdentityClaims](pisystems.md) | Add or update Piraeus service identity claims used in symmetric key based security tokens.  This is seldom used as it is also configure on deployment.  
|[Add-PiraeusServiceIdentityCertificate](Add-CaplPolicy) | Specifies the location of a X.509 certificate to be used as a security token if required. This is seldom used as it is also configure on deployment.  

### Pre-Shared Key (PSK) Cmdlets
These cmdlets are used to manage PSKs for gateways that require them.  PSKs allow an active agent to connect using TLSv2, an encrypted channel, without using X.509 certificates. They are not a substitute for identity.  A security token is required for identity.

| **Cmdlet**     | **Definition**                                                                                                                      |
|-------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| [Add-PiraeusPskSecret](pisystems.md) | Add or update a PSK secret.  
|[Remove-PiraeusPskSecret](Add-CaplPolicy) | Remove a PSK secret.  






