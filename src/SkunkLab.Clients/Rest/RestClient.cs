﻿using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using SkunkLab.Channels;
using SkunkLab.Channels.Http;

namespace Piraeus.Clients.Rest
{
    public class RestClient
    {
        private readonly IChannel receiveChannel;

        private readonly HttpClientChannel sendChannel;

        public RestClient(string endpoint, string securityToken, IEnumerable<Observer> observers = null,
            CancellationToken token = default)
        {
            sendChannel = ChannelFactory.Create(endpoint, securityToken) as HttpClientChannel;

            if (observers != null)
            {
                foreach (var ob in observers)
                {
                    if (endpoint.Contains("?"))
                    {
                        endpoint += $"&sub={ob.ResourceUri.ToString().ToLowerInvariant()}";
                    }
                }

                receiveChannel = ChannelFactory.Create(endpoint, securityToken, observers, token);
                Task openTask = receiveChannel.OpenAsync();
                Task.WaitAll(openTask);

                Task receiveTask = receiveChannel.ReceiveAsync();
                Task.WhenAll(receiveTask);
            }
        }

        public RestClient(string endpoint, X509Certificate2 certificate, IEnumerable<Observer> observers = null,
            CancellationToken token = default)
        {
            sendChannel = ChannelFactory.Create(endpoint, certificate) as HttpClientChannel;

            if (observers != null)
            {
                receiveChannel = ChannelFactory.Create(endpoint, certificate, observers, token);

                Task receiveTask = receiveChannel.ReceiveAsync();
                Task.WhenAll(receiveTask);
            }
        }

        public async Task ReceiveAsync()
        {
            if (!receiveChannel.IsConnected)
            {
                await receiveChannel.OpenAsync();
            }

            await receiveChannel.ReceiveAsync();
        }

        public async Task SendAsync(string resourceUriString, string contentType, byte[] message,
            string cacheKey = null, List<KeyValuePair<string, string>> indexes = null)
        {
            await sendChannel.SendAsync(resourceUriString, contentType, message, cacheKey, indexes);
        }
    }
}