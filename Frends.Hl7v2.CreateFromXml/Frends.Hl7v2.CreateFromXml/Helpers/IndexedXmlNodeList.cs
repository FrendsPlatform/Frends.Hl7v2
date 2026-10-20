using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace Frends.Hl7v2.CreateFromXml.Helpers;

internal sealed class IndexedXmlNodeList : XmlNodeList
{
    private readonly XmlNode firstNode;
    private readonly List<XmlNode> nodes;
    private readonly CancellationToken cancellationToken;

    internal IndexedXmlNodeList(XmlNodeList source, CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        foreach (XmlNode node in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (firstNode == null)
            {
                firstNode = node;
                continue;
            }

            // Most datatype elements contain one text node; avoid allocating a list for them.
            nodes ??= [firstNode];
            nodes.Add(node);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Gets the snapshot's child count in constant time.
    /// </summary>
    /// <example>2</example>
    public override int Count
    {
        get
        {
            cancellationToken.ThrowIfCancellationRequested();
            return nodes?.Count ?? (firstNode == null ? 0 : 1);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Retrieves a child in constant time, retaining XmlNodeList's out-of-range behavior.
    /// </summary>
    public override XmlNode Item(int index)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (nodes == null)
            return index == 0 ? firstNode : null;
        return index >= 0 && index < nodes.Count ? nodes[index] : null;
    }

    /// <inheritdoc />
    /// <summary>
    /// Enumerates the snapshot with cancellation checkpoints.
    /// </summary>
    public override IEnumerator GetEnumerator()
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (nodes == null)
        {
            if (firstNode != null)
                yield return firstNode;
        }
        else
        {
            foreach (var node in nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return node;
            }
        }
    }
}
