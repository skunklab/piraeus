﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;

namespace Piraeus.GrainInterfaces
{
    public interface ISubscription : IGrainWithStringKey
    {
        [AlwaysInterleave]
        Task<string> AddObserverAsync(TimeSpan lifetime, IMessageObserver observer);

        [AlwaysInterleave]
        Task<string> AddObserverAsync(TimeSpan lifetime, IMetricObserver observer);

        [AlwaysInterleave]
        Task<string> AddObserverAsync(TimeSpan lifetime, IErrorObserver observer);

        Task ClearAsync();

        [AlwaysInterleave]
        Task<string> GetIdAsync();

        [AlwaysInterleave]
        Task<SubscriptionMetadata> GetMetadataAsync();

        [AlwaysInterleave]
        Task<CommunicationMetrics> GetMetricsAsync();

        [AlwaysInterleave]
        Task NotifyAsync(EventMessage message);

        [AlwaysInterleave]
        Task NotifyAsync(EventMessage message, List<KeyValuePair<string, string>> indexes);

        [AlwaysInterleave]
        Task RemoveObserverAsync(string leaseKey);

        Task<bool> RenewObserverLeaseAsync(string leaseKey, TimeSpan lifetime);

        [AlwaysInterleave]
        Task UpsertMetadataAsync(SubscriptionMetadata metadata);
    }
}