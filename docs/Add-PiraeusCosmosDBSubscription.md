﻿


Add-PiraeusCosmosDBSubscription Cmdlet
=====
[Back](MgmtApi.md)

Adds a subscription for CosmosDB storage as a static route from a π-system.

| **Parameter**     | **Optional** | **Definition**                                                                                                                      |
|-------------------|--------------|-------------------------------------------------------------------------------------------------------------------------------------|
| ServiceUrl        | N            | Url of the Piraeus Management API service, e.g., https://\<dns\>.\<location\>.cloudapp.azure.com                                    |
| SecurityToken     | N            | Security token acquired from the Management API using a security code.                                                              |
| ResourceUriString | N            | The π-system URI identifier associated with a specific event.                                                                       |
| Account           | N            | The CosmosDB account name.                                                                                                |
| Key               | N            | The CosmosDB security key.                                                                                                         |
| Database         | N            | Name of Cosmos DB database.                                                                 |
| Collection          | N            | Name of Cosmos DB collection.                                                             |
| NumClients		| Y		| Number of clients.  Default is 1.
| Description       | Y            | An optional description of the subscription, which is useful if querying subscriptions for a π-system from the management API.      |


**Example**
```
$url = "http://piraeus.eastus.cloudapp.azure.com" 
$code = "12345678" 
$token = Get-PiraeusManagementToken -ServiceUrl $url -Key $code

$piSystemId= "http://skunklab.io/test/resource-a"
$account = ""
$database = ""
$collection = ""
$key = ""
$numClients = 1
$description = ""


Add-PiraeusCosmosDbSubscription `
				-ServiceUrl $url `
				-SecurityToken $token `
                                -ResourceUriString $piSystemId `
                                -Account $account `
                                -Database $database `
                                -Collection $collection `
                                -Key $key `
                                -NumClients $numClients `
                                -Description $description 
```

[Management API](MgmtApi.md)
