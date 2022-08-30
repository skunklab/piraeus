﻿
Azure Queue Storage Subscription
===============================

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

$resource = ""
$account = ""
$database = ""
$collection = ""
$key = ""
$numClients = 1
$description = ""


Add-PiraeusCosmosDbSubscription -ServiceUrl $url -SecurityToken $token `
                                -ResourceUriString $resource `
                                -Account $account `
                                -Database $database `
                                -Collection $collection `
                                -Key $key `
                                -NumClients $numClients `
                                -Description $description 
