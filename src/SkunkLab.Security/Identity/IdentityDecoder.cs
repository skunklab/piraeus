﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SkunkLab.Security.Identity
{
    public class IdentityDecoder
    {
        public IdentityDecoder(string identityClaimType, HttpContext context = null,
            List<KeyValuePair<string, string>> indexes = null)
        {
            Id = DecodeClaimType(context, identityClaimType);

            if (indexes != null)
            {
                Indexes = new List<KeyValuePair<string, string>>();

                foreach (var item in indexes)
                {
                    string value = DecodeClaimType(context, item.Key);
                    if (!string.IsNullOrEmpty(value))
                    {
                        Indexes.Add(new KeyValuePair<string, string>(item.Value, value));
                    }
                }
            }
        }

        public string Id
        {
            get; internal set;
        }

        public List<KeyValuePair<string, string>> Indexes
        {
            get; internal set;
        }

        private string DecodeClaimType(HttpContext context, string claimType)
        {
            if (claimType == null)
            {
                return null;
            }

            if (context == null)
            {
                return DecodeClaimType(claimType);
            }

            IEnumerable<Claim> claims =
                context.User.Claims.Where(c => c.Type.ToLowerInvariant() == claimType.ToLowerInvariant());
            if (claims != null && claims.Count() == 1)
            {
                return claims.First().Value;
            }

            return null;
        }

        private string DecodeClaimType(string claimType)
        {
            Task<string> task = Task.Factory.StartNew(() =>
            {
                if (claimType == null)
                {
                    return null;
                }

                if (!(Thread.CurrentPrincipal is ClaimsPrincipal principal))
                {
                    return null;
                }

                var identity = new ClaimsIdentity(principal.Claims);
                Claim claim =
                    identity.FindFirst(
                        c =>
                            c.Type.ToLower(CultureInfo.InvariantCulture) ==
                            claimType.ToLower(CultureInfo.InvariantCulture));

                return claim?.Value;
            });

            return task.Result;
        }
    }
}