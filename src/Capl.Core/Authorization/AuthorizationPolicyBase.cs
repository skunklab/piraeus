﻿using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Capl.Properties;

namespace Capl.Authorization
{
    /// <summary>
    ///     The base class for an authorization policy.
    /// </summary>
    [Serializable]
    [XmlSchemaProvider("GetSchema", IsAny = false)]
    [KnownType(typeof(AuthorizationPolicy))]
    public abstract class AuthorizationPolicyBase : IXmlSerializable
    {
        /// <summary>
        ///     Gets or sets an evaluation expression.
        /// </summary>
        public abstract Term Expression
        {
            get; set;
        }

        /// <summary>
        ///     Gets or sets a transform collection.
        /// </summary>
        public abstract TransformCollection Transforms
        {
            get; internal set;
        }

        /// <summary>
        ///     Provides a schema for an authorization policy.
        /// </summary>
        /// <param name="schemaSet">A schema set to populate.</param>
        /// <returns>Qualified name of an authorization policy type for a schema.</returns>
        public static XmlQualifiedName GetSchema(XmlSchemaSet schemaSet)
        {
            _ = schemaSet ?? throw new ArgumentNullException(nameof(schemaSet));

            using (StringReader reader = new StringReader(Resources.AuthorizationPolicySchema))
            {
                XmlSchema schema = XmlSchema.Read(reader, null);
                schemaSet.Add(schema);
            }

            return new XmlQualifiedName("AuthorizationPolicyType", AuthorizationConstants.Namespaces.Xmlns);
        }

        #region IXmlSerializable Members

        /// <summary>
        ///     Provides a schema for an authorization policy.
        /// </summary>
        /// <returns>Schema for an authorization policy.</returns>
        /// <remarks>The methods always return null; the schema is provided by an XmlSchemaProvider.</remarks>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        ///     Reads the Xml of an authorization policy.
        /// </summary>
        /// <param name="reader">An XmlReader for the authorization policy.</param>
        public abstract void ReadXml(XmlReader reader);

        /// <summary>
        ///     Writes the Xml of an authorization policy.
        /// </summary>
        /// <param name="writer">An XmlWriter for the authorization policy.</param>
        public abstract void WriteXml(XmlWriter writer);

        #endregion IXmlSerializable Members
    }
}