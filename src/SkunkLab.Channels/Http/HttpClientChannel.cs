﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SkunkLab.Channels.Http
{
    public class HttpClientChannel : HttpChannel
    {
        private readonly CancellationToken internalToken;

        private readonly Uri requestUri;

        private readonly CancellationToken token;

        private readonly CancellationTokenSource tokenSource;

        private string cacheKey;

        private X509Certificate2 certificate;

        private string contentType;

        private bool disposed;

        private IEnumerable<KeyValuePair<string, string>> indexes;

        private IEnumerable<Observer> observers;

        private string resourceUriString;

        private string securityToken;

        private ChannelState state;

        public HttpClientChannel(string endpoint, string securityToken)
        {
            Id = "http-" + Guid.NewGuid();
            requestUri = new Uri(endpoint);
            this.securityToken = securityToken;
        }

        public HttpClientChannel(string endpoint, X509Certificate2 certificate)
        {
            Id = "http-" + Guid.NewGuid();
            requestUri = new Uri(endpoint);
            this.certificate = certificate;
        }

        public HttpClientChannel(string endpoint, string resourceUriString, string contentType, string securityToken,
            string cacheKey = null, List<KeyValuePair<string, string>> indexes = null)
        {
            Id = "http-" + Guid.NewGuid();
            requestUri = new Uri(endpoint);
            this.contentType = contentType;
            this.securityToken = securityToken;
            this.resourceUriString = resourceUriString;
            this.cacheKey = cacheKey;
            this.indexes = indexes;
        }

        public HttpClientChannel(string endpoint, string resourceUriString, string contentType,
            X509Certificate2 certificate, string cacheKey = null, List<KeyValuePair<string, string>> indexes = null)
        {
            Id = "http-" + Guid.NewGuid();
            requestUri = new Uri(endpoint);
            this.contentType = contentType;
            this.certificate = certificate;
            this.resourceUriString = resourceUriString;
            this.cacheKey = cacheKey;
            this.indexes = indexes;
        }

        public HttpClientChannel(string endpoint, string securityToken, IEnumerable<Observer> observers,
            CancellationToken token = default)
        {
            Id = "http-" + Guid.NewGuid();
            requestUri = new Uri(endpoint);
            this.securityToken = securityToken;
            this.observers = observers;
            this.token = token;
            tokenSource = new CancellationTokenSource();
            internalToken = tokenSource.Token;
            this.token.Register(() => tokenSource.Cancel());
        }

        public HttpClientChannel(string endpoint, X509Certificate2 certificate, IEnumerable<Observer> observers,
            CancellationToken token = default)
        {
            Id = "http-" + Guid.NewGuid();
            requestUri = new Uri(endpoint);
            this.certificate = certificate;
            this.observers = observers;
            this.token = token;
            tokenSource = new CancellationTokenSource();
            internalToken = tokenSource.Token;
            this.token.Register(() => tokenSource.Cancel());
        }

        public override event EventHandler<ChannelCloseEventArgs> OnClose;

        public override event EventHandler<ChannelErrorEventArgs> OnError;

        public override event EventHandler<ChannelOpenEventArgs> OnOpen;

        public override event EventHandler<ChannelReceivedEventArgs> OnReceive;

        public override event EventHandler<ChannelStateEventArgs> OnStateChange;

        public override string Id
        {
            get; internal set;
        }

        public override bool IsAuthenticated
        {
            get; internal set;
        }

        public override bool IsConnected => State == ChannelState.Open;

        public override bool IsEncrypted
        {
            get; internal set;
        }

        public override int Port
        {
            get; internal set;
        }

        public override bool RequireBlocking => false;

        public override ChannelState State
        {
            get => state;
            internal set
            {
                if (value != state)
                {
                    OnStateChange?.Invoke(this, new ChannelStateEventArgs(Id, value));
                }

                state = value;
            }
        }

        public override string TypeId => "HTTP";

        public override async Task AddMessageAsync(byte[] message)
        {
            await Task.CompletedTask;
        }

        public override async Task CloseAsync()
        {
            State = ChannelState.ClosedReceived;

            if (!internalToken.IsCancellationRequested)
            {
                tokenSource.Cancel();
            }

            OnClose?.Invoke(this, new ChannelCloseEventArgs(Id));

            State = ChannelState.Closed;
            await Task.CompletedTask;
        }

        public override void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }

        public override async Task OpenAsync()
        {
            OnOpen?.Invoke(this, new ChannelOpenEventArgs(Id, null));
            State = ChannelState.None;

            await Task.CompletedTask;
        }

        public override async Task ReceiveAsync()
        {
            while (!internalToken.IsCancellationRequested)
            {
                State = ChannelState.Connecting;
                HttpWebRequest request = GetRequest(HttpMethod.Get);
                Port = request.RequestUri.Port;
                IsEncrypted = request.RequestUri.Scheme == "https";
                foreach (var item in observers)
                {
                    request.Headers.Add(HttpChannelConstants.SUBSCRIBE_HEADER,
                        item.ResourceUri.ToString().ToLowerInvariant());
                }

                try
                {
                    State = ChannelState.Open;
                    using HttpWebResponse response =
                        await request.GetResponseAsync().WithCancellation(internalToken) as HttpWebResponse;
                    if (response.StatusCode == HttpStatusCode.OK ||
                        response.StatusCode == HttpStatusCode.Accepted)
                    {
                        using Stream stream = response.GetResponseStream();
                        byte[] buffer = new byte[response.ContentLength];
                        await stream.ReadAsync(buffer, 0, buffer.Length);

                        string resourceHeader = response.Headers.Get(HttpChannelConstants.RESOURCE_HEADER);

                        if (resourceHeader == null)
                        {
                            continue;
                        }

                        string resourceUriString = new Uri(resourceHeader).ToString().ToLowerInvariant();

                        foreach (Observer observer in observers)
                        {
                            if (observer.ResourceUri.ToString().ToLowerInvariant() == resourceUriString)
                            {
                                observer.Update(observer.ResourceUri, response.ContentType, buffer);
                            }
                        }

                        List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>> {
                            new KeyValuePair<string, string>("Resource", resourceUriString),
                            new KeyValuePair<string, string>("Content-Type", response.ContentType)
                        };
                        OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, buffer, list));
                    }
                    else
                    {
                        OnError?.Invoke(this,
                            new ChannelErrorEventArgs(Id,
                                new WebException(string.Format("Unexpected status code {0}", response.StatusCode))));
                    }

                    State = ChannelState.Closed;
                }
                catch (OperationCanceledException)
                {
                    State = ChannelState.Aborted;
                }
                catch (AggregateException ae)
                {
                    Trace.TraceError("Http client channel '{0}' receive error '{1}'", Id,
                        ae.Flatten().InnerException.Message);
                    State = ChannelState.Closed;
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ae.Flatten()));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Http client channel '{0}' receive error '{1}'", Id, ex.Message);
                    State = ChannelState.Closed;
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                }
            }
        }

        public async Task SendAsync(string resourceUriString, string contentType, byte[] message,
            string cacheKey = null, List<KeyValuePair<string, string>> indexes = null)
        {
            this.resourceUriString = resourceUriString;
            this.contentType = contentType;
            this.cacheKey = cacheKey;
            this.indexes = indexes;
            await SendAsync(message);
        }

        public override async Task SendAsync(byte[] message)
        {
            try
            {
                HttpWebRequest request = GetRequest(HttpMethod.Post);
                request.ContentLength = message.Length;
                Port = request.RequestUri.Port;
                IsEncrypted = request.RequestUri.Scheme == "https";

                using (Stream stream = await request.GetRequestStreamAsync().WithCancellation(internalToken))
                    await stream.WriteAsync(message, 0, message.Length);

                using HttpWebResponse response =
                    await request.GetResponseAsync().WithCancellation(internalToken) as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Accepted ||
                    response.StatusCode == HttpStatusCode.NoContent)
                {
                    IsAuthenticated = true;
                    State = ChannelState.CloseSent;
                }
                else
                {
                    State = ChannelState.Aborted;
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, new WebException(
                        string.Format("Invalid HTTP response status code {0}", response.StatusCode))));
                }
            }
            catch (OperationCanceledException oce)
            {
                Trace.TraceWarning("Http channel cancelled.");
                State = ChannelState.Aborted;
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, oce));
            }
            catch (AggregateException ae)
            {
                State = ChannelState.Aborted;
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ae.Flatten()));
            }
            catch (WebException we)
            {
                Trace.TraceError("Channel '{0}' error with '{1}'", Id, we.Message);
                State = ChannelState.Aborted;
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, we.InnerException));
            }
            catch (Exception ex)
            {
                Trace.TraceError("Channel '{0}' error with '{1}'", Id, ex.Message);
                State = ChannelState.Aborted;
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
            }
        }

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                if (internalToken.CanBeCanceled)
                {
                    tokenSource.Cancel();
                }

                IsAuthenticated = false;
                IsEncrypted = false;
                State = ChannelState.Closed;
                Port = 0;

                certificate = null;
                securityToken = null;
                observers = null;
                disposed = true;
            }
        }

        private HttpWebRequest GetRequest(HttpMethod method)
        {
            HttpWebRequest request = WebRequest.Create(requestUri) as HttpWebRequest;
            if (method == HttpMethod.Get)
            {
                request.ContentLength = 0;
                request.Method = "GET";
                request.KeepAlive = true;
                request.Timeout = int.MaxValue;
            }
            else if (method == HttpMethod.Post)
            {
                request.Method = "POST";
                request.ContentType = contentType;

                Uri resourceUri = new Uri(resourceUriString.ToLower(CultureInfo.InvariantCulture));

                request.Headers.Add(HttpChannelConstants.RESOURCE_HEADER, resourceUri.ToString());

                if (!string.IsNullOrEmpty(cacheKey))
                {
                    request.Headers.Add(HttpChannelConstants.CACHE_KEY, cacheKey);
                }

                if (indexes != null)
                {
                    foreach (KeyValuePair<string, string> index in indexes)
                        request.Headers.Add(HttpChannelConstants.INDEX_HEADER, index.Key + ";" + index.Value);
                }
            }
            else
            {
                throw new HttpRequestException(string.Format("Invalid request verb {0}", method));
            }

            if (!string.IsNullOrEmpty(securityToken))
            {
                request.Headers.Add("Authorization", string.Format("Bearer {0}", securityToken));
            }

            if (certificate != null)
            {
                request.ClientCertificates.Add(certificate);
            }

            return request;
        }
    }
}