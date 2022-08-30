﻿using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;

namespace Capl.Authorization
{
    /// <summary>
    ///     Extensions for deserialization.
    /// </summary>
    public static class XmlReaderExtensions
    {
        public static string DefaultNamespace => AuthorizationConstants.Namespaces.Xmlns;

        public static string GetElementValue(this XmlReader reader, string localName)
        {
            return GetElementValue(reader, localName, DefaultNamespace);
        }

        /// <summary>
        ///     Gets the value of an Xml element from the reader.
        /// </summary>
        /// <param name="reader">XmlReader to extend.</param>
        /// <param name="localName">Local name of the element to be inspected.</param>
        /// <param name="namespaceUri">Namespace of the element to be inspected.</param>
        /// <returns>The string of the element.</returns>
        public static string GetElementValue(this XmlReader reader, string localName, string namespaceUri)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));
            _ = localName ?? throw new ArgumentNullException(nameof(localName));
            _ = namespaceUri ?? throw new ArgumentNullException(nameof(namespaceUri));

            if (reader.LocalName != localName)
            {
                throw new SerializationException(localName);
            }

            if (reader.NamespaceURI != namespaceUri)
            {
                throw new SerializationException(localName);
            }

            if (reader.IsEmptyElement)
            {
                return null;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                throw new SerializationException("Xml reader not positioned on an element to reader the value.");
            }

            return reader.ReadString();
        }

        /// <summary>
        ///     Gets an attribute value that is optional.
        /// </summary>
        /// <param name="reader">XmlReader to extend.</param>
        /// <param name="name">Name of attribute to inspect.</param>
        /// <returns>Attribute value as string if present; otherwise null.</returns>
        public static string GetOptionalAttribute(this XmlReader reader, string name)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            string val = reader.GetAttribute(name);

            return val;
        }

        /// <summary>
        ///     Gets a required attribute value.
        /// </summary>
        /// <param name="reader">XmlReader to extend.</param>
        /// <param name="name">Name of attribute to inspect.</param>
        /// <returns>Attribute value as string; otherwise throws and exception.</returns>
        public static string GetRequiredAttribute(this XmlReader reader, string name)
        {
            string val = reader.GetOptionalAttribute(name);

            if (string.IsNullOrEmpty(val))
            {
                throw new SerializationException(string.Format(CultureInfo.InvariantCulture,
                    "Required attribute {0} not found", name));
            }

            return val;
        }

        public static bool IsRequiredEndElement(this XmlReader reader, string localName)
        {
            return IsRequiredEndElement(reader, localName, DefaultNamespace);
        }

        /// <summary>
        ///     Determines whether an element is a required end element.
        /// </summary>
        /// <param name="reader">XmlReader to extend.</param>
        /// <param name="localName">Local name of element to inspect.</param>
        /// <param name="namespaceUri">Namespace of element to inspect.</param>
        /// <returns>True, if the element is a required end element; otherwise false.</returns>
        public static bool IsRequiredEndElement(this XmlReader reader, string localName, string namespaceUri)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            return reader.LocalName == localName && reader.NamespaceURI == namespaceUri &&
                   (reader.IsEmptyElement || reader.NodeType == XmlNodeType.EndElement);
        }

        public static bool IsRequiredStartElement(this XmlReader reader, string localName)
        {
            return IsRequiredStartElement(reader, localName, DefaultNamespace);
        }

        public static bool IsRequiredStartElement(this XmlReader reader, string localName, string namespaceUri)
        {
            return reader.LocalName == localName && reader.NamespaceURI == namespaceUri &&
                   reader.NodeType == XmlNodeType.Element;
        }

        public static void MoveToRequiredStartElement(this XmlReader reader, string localName)
        {
            MoveToRequiredStartElement(reader, localName, DefaultNamespace);
        }

        /// <summary>
        ///     Moves the XmlReader to a specific starting element.
        /// </summary>
        /// <param name="reader">XmlReader to extend.</param>
        /// <param name="localName">Local name of the element to position as start.</param>
        /// <param name="namespaceUri">Namespace of the element to position as start.</param>
        public static void MoveToRequiredStartElement(this XmlReader reader, string localName, string namespaceUri)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            if (reader.IsEmptyElement && reader.LocalName == localName && reader.NamespaceURI == namespaceUri ||
                reader.NodeType == XmlNodeType.Element && reader.LocalName == localName &&
                reader.NamespaceURI == namespaceUri)
            {
                return;
            }

            reader.MoveToElement();

            while (reader.Read())
            {
                if (reader.IsEmptyElement && reader.LocalName == localName && reader.NamespaceURI == namespaceUri ||
                    reader.NodeType == XmlNodeType.Element && reader.LocalName == localName &&
                    reader.NamespaceURI == namespaceUri)
                {
                    return;
                }
            }

            throw new SerializationException(string.Format(CultureInfo.InvariantCulture,
                "Required element {0} in namespace {1} not found", localName, namespaceUri));
        }

        public static void MoveToStartElement(this XmlReader reader)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            if (reader.NodeType == XmlNodeType.Element)
            {
                return;
            }

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    return;
                }
            }
        }
    }
}