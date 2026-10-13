using System.Collections;
using System.Threading;
using System.Xml;

namespace Frends.Hl7v2.CreateFromXml.Helpers;

internal sealed class ParsingXmlDocument : XmlDocument
{
    private readonly CancellationToken cancellationToken;
    private bool indexedTraversal;

    internal ParsingXmlDocument(CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
    }

    /// <inheritdoc />
    /// <summary>
    /// Creates elements with cancellation checkpoints in their child-node views.
    /// </summary>
    public override XmlElement CreateElement(string prefix, string localName, string namespaceUri)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ParsingXmlElement(prefix, localName, namespaceUri, this, cancellationToken);
    }

    internal void EnableIndexedTraversal()
    {
        cancellationToken.ThrowIfCancellationRequested();
        indexedTraversal = true;
    }

    private sealed class ParsingXmlElement : XmlElement
    {
        private readonly CancellationToken cancellationToken;

        internal ParsingXmlElement(
            string prefix,
            string localName,
            string namespaceUri,
            ParsingXmlDocument document,
            CancellationToken cancellationToken)
            : base(prefix, localName, namespaceUri, document)
        {
            this.cancellationToken = cancellationToken;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets a cancellable child view, indexed during the read-only parsing phase.
        /// </summary>
        /// <example>A list containing the field elements of a segment.</example>
        public override XmlNodeList ChildNodes =>
            ((ParsingXmlDocument)OwnerDocument).indexedTraversal
                ? new IndexedXmlNodeList(base.ChildNodes, cancellationToken)
                : new CancellableNodeList(base.ChildNodes, cancellationToken);
    }

    private sealed class CancellableNodeList : XmlNodeList
    {
        private readonly XmlNodeList nodes;
        private readonly CancellationToken cancellationToken;

        internal CancellableNodeList(XmlNodeList nodes, CancellationToken cancellationToken)
        {
            this.nodes = nodes;
            this.cancellationToken = cancellationToken;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the child count unless cancellation was requested.
        /// </summary>
        /// <example>2</example>
        public override int Count
        {
            get
            {
                cancellationToken.ThrowIfCancellationRequested();
                return nodes.Count;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Retrieves a child unless cancellation was requested.
        /// </summary>
        public override XmlNode Item(int index)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return nodes.Item(index);
        }

        /// <inheritdoc />
        /// <summary>
        /// Checks cancellation between enumerated children.
        /// </summary>
        public override IEnumerator GetEnumerator()
        {
            foreach (XmlNode node in nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return node;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
