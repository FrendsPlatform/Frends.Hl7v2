using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using NHapi.Base;
using NHapi.Base.Model;
using NHapi.Base.Parser;

namespace Frends.Hl7v2.CreateFromXml.Helpers;

internal class CancellableXmlParser : DefaultXMLParser
{
    private readonly CancellationToken cancellationToken;

    internal CancellableXmlParser(CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
    }

    /// <inheritdoc />
    /// <summary>
    /// Indexes group children while retaining NHapi's segment and datatype mapping.
    /// </summary>
    public override IMessage ParseDocument(XmlDocument xmlMessage, string version, ParserOptions parserOptions)
    {
        ArgumentNullException.ThrowIfNull(xmlMessage);
        cancellationToken.ThrowIfCancellationRequested();

        // NHapi only reads the DOM while mapping it; retain live child lists during loading.
        if (xmlMessage is ParsingXmlDocument document)
            document.EnableIndexedTraversal();

        parserOptions ??= DefaultParserOptions;
        var root = xmlMessage.DocumentElement;
        AssertNamespaceUri(root.NamespaceURI);
        var message = InstantiateMessage(root.LocalName, version, true);

        if (!TryParseGroup(message, root, message.GetStructureName(), parserOptions))
        {
            // NHapi has special alias/fallback rules for repeated non-repeating segments (e.g. PID2).
            // Let it populate a fresh message rather than duplicate those rules.
            cancellationToken.ThrowIfCancellationRequested();
            message = base.ParseDocument(xmlMessage, version, parserOptions);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return message;
    }

    /// <inheritdoc />
    /// <summary>
    /// Retains NHapi segment mapping with cancellation checkpoints.
    /// </summary>
    public override void Parse(ISegment segmentObject, XmlElement segmentElement, ParserOptions parserOptions)
    {
        cancellationToken.ThrowIfCancellationRequested();
        base.Parse(segmentObject, segmentElement, parserOptions);
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <inheritdoc />
    /// <summary>
    /// Checks cancellation between fields, repetitions and components.
    /// </summary>
    public override void Parse(IType datatypeObject, XmlElement datatypeElement, ParserOptions parserOptions)
    {
        cancellationToken.ThrowIfCancellationRequested();
        base.Parse(datatypeObject, datatypeElement, parserOptions);
        cancellationToken.ThrowIfCancellationRequested();
    }

    protected override IMessage DoParse(string message, string version, ParserOptions parserOptions)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var document = new ParsingXmlDocument(cancellationToken);
            using var reader = new StringReader(message);
            document.Load(reader);
            cancellationToken.ThrowIfCancellationRequested();
            return ParseDocument(document, version, parserOptions);
        }
        catch (XmlException exception)
        {
            throw new HL7Exception("XmlException parsing XML", ErrorCode.APPLICATION_INTERNAL_ERROR, exception);
        }
        catch (IOException exception)
        {
            throw new HL7Exception("IOException parsing XML", ErrorCode.APPLICATION_INTERNAL_ERROR, exception);
        }
    }

    private bool TryParseGroup(IGroup group, XmlElement element, string messageName, ParserOptions parserOptions)
    {
        var children = IndexChildren(element, out var elementNames);
        var knownElements = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in group.Names)
        {
            cancellationToken.ThrowIfCancellationRequested();
            knownElements.Add(group.IsGroup(name) ? MakeGroupElementName(messageName, name) : name);

            // These names refer to fixed-position aliases, not separate XML element names.
            if (name.Length == 4 && char.IsDigit(name[3]))
                continue;

            if (!TryParseRepetitions(group, children, messageName, name, name, parserOptions))
                return false;
        }

        foreach (var name in elementNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (knownElements.Contains(name))
                continue;

            var indexName = group.AddNonstandardSegment(name);
            if (!TryParseRepetitions(group, children, messageName, name, indexName, parserOptions))
                return false;
        }

        return true;
    }

    private Dictionary<string, List<XmlElement>> IndexChildren(XmlElement parent, out List<string> names)
    {
        var children = new Dictionary<string, List<XmlElement>>(StringComparer.Ordinal);
        names = new List<string>();
        for (var node = parent.FirstChild; node != null; node = node.NextSibling)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node is not XmlElement element)
                continue;

            if (!children.TryGetValue(element.LocalName, out var repetitions))
            {
                AssertNamespaceUri(element.NamespaceURI);
                repetitions = new List<XmlElement>();
                children.Add(element.LocalName, repetitions);
                names.Add(element.LocalName);
            }

            repetitions.Add(element);
        }

        return children;
    }

    private bool TryParseRepetitions(
        IGroup group,
        Dictionary<string, List<XmlElement>> children,
        string messageName,
        string childName,
        string indexName,
        ParserOptions parserOptions)
    {
        var elementName = MakeGroupElementName(messageName, childName);
        if (!children.TryGetValue(elementName, out var repetitions))
            return true;

        foreach (var element in repetitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AssertNamespaceUri(element.NamespaceURI);
        }

        if (repetitions.Count > 1 && !group.IsRepeating(indexName))
            return false;

        for (var repetition = 0; repetition < repetitions.Count; repetition++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var structure = group.GetStructure(indexName, repetition);
            if (structure is IGroup childGroup)
            {
                if (!TryParseGroup(childGroup, repetitions[repetition], messageName, parserOptions))
                    return false;
            }
            else if (structure is ISegment segment)
            {
                Parse(segment, repetitions[repetition], parserOptions);
            }
        }

        return true;
    }
}
