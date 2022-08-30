﻿using System;

namespace Capl.Authorization.Operations
{
    /// <summary>
    ///     Compares two DateTime values to determine if the left argument is greater than or equal the right argument.
    /// </summary>
    public class GreaterThanOrEqualDateTimeOperation : Operation
    {
        public static Uri OperationUri => new Uri(AuthorizationConstants.OperationUris.GreaterThanOrEqualDateTime);

        /// <summary>
        ///     Gets the URI that identifies the operation.
        /// </summary>
        public override Uri Uri => new Uri(AuthorizationConstants.OperationUris.GreaterThanOrEqualDateTime);

        /// <summary>
        ///     Executes the comparsion.
        /// </summary>
        /// <param name="left">LHS of the expression argument.</param>
        /// <param name="right">RHS of the expression argument.</param>
        /// <returns>True, if the LHS argument is greater than or equal the RHS argument DateTime value; otherwise false.</returns>
        public override bool Execute(string left, string right)
        {
            DateTimeComparer comparer = new DateTimeComparer();
            int result = comparer.Compare(left, right);
            return result == 0 || result == 1;
        }
    }
}