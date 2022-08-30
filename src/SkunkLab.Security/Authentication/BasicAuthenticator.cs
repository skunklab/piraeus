﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using SkunkLab.Security.Tokens;

namespace SkunkLab.Security.Authentication
{
    public class BasicAuthenticator : IAuthenticator
    {
        private readonly Dictionary<string, Tuple<string, string, string>> container;

        private HttpContext context;

        public BasicAuthenticator()
        {
            container = new Dictionary<string, Tuple<string, string, string>>();
        }

        public void Add(SecurityTokenType type, string signingKey, string issuer = null, string audience = null,
            HttpContext context = null)
        {
            this.context = context;
            if (!container.ContainsKey(type.ToString()))
            {
                Tuple<string, string, string> tuple = new Tuple<string, string, string>(signingKey, issuer, audience);
                container.Add(type.ToString(), tuple);
            }
        }

        public bool Authenticate(SecurityTokenType type, byte[] token)
        {
            if (token != null)
            {
                return Authenticate(type,
                    type == SecurityTokenType.X509 ? Convert.ToBase64String(token) : Encoding.UTF8.GetString(token));
            }

            return Authenticate(type, token);
        }

        public bool Authenticate(SecurityTokenType type, string token)
        {
            if (container.ContainsKey(SecurityTokenType.NONE.ToString()) && type == SecurityTokenType.NONE)
            {
                return true;
            }

            if (token != null && container.ContainsKey(type.ToString()))
            {
                Tuple<string, string, string> tuple = container[type.ToString()];
                return SecurityTokenValidator.Validate(token, type, tuple.Item1, tuple.Item2, tuple.Item3, context);
            }

            return false;
        }

        public void Clear()
        {
            container.Clear();
        }

        public bool Remove(SecurityTokenType type)
        {
            return container.Remove(type.ToString());
        }
    }
}