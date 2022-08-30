﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Xml.XPath;

namespace Capl.Authorization.Matching
{
    /// <summary>
    ///     Matches the string literal of a claim type and optional XSLT expression to match the claim value.
    /// </summary>
    /// <remarks>
    ///     Assumes the claim value is encoded as Xml.
    /// </remarks>
    public class ComplexTypeMatchExpression : MatchExpression
    {
        public static Uri MatchUri => new Uri(AuthorizationConstants.MatchUris.ComplexType);

        public override Uri Uri => new Uri(AuthorizationConstants.MatchUris.ComplexType);

        public override IList<Claim> MatchClaims(IEnumerable<Claim> claims, string claimType, string xpath)
        {
            _ = claims ?? throw new ArgumentNullException(nameof(claims));

            ClaimsIdentity ci = new ClaimsIdentity(claims);
            IEnumerable<Claim> claimSet = ci.FindAll(delegate (Claim claim) {
                if (claim.Type == claimType)
                {
                    string claimValue = HttpUtility.HtmlDecode(claim.Value);
                    using (Stream stream = new MemoryStream(Encoding.UTF32.GetBytes(claimValue)))
                    {
                        try
                        {
                            XPathDocument doc = new XPathDocument(stream);
                            XPathExpression expression = XPathExpression.Compile(xpath);

                            XPathNavigator nav = doc.CreateNavigator();
                            XPathNodeIterator iterator = nav.Select(expression);

                            while (iterator.MoveNext())
                            {
                                if (!string.IsNullOrEmpty(iterator.Current.Value))
                                {
                                    return true;
                                }
                            }
                        }
                        catch //if the claim type is not valid xml catch the exception so true cannot be returned and method will not fail
                        {
                        }
                    }

                    return false;
                }

                return false;
            });

            return new List<Claim>(claimSet);
        }
    }
}