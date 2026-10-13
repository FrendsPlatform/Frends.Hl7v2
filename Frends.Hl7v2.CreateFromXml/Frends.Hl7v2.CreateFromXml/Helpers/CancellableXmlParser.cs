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
    /// Maps a document with cancellation checks at the message boundary.
    /// </summary>
    public override IMessage ParseDocument(XmlDocument xmlMessage, string version, ParserOptions parserOptions)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var message = base.ParseDocument(xmlMessage, version, parserOptions);
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
            using var reader = new CancellableStringReader(message, cancellationToken);
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
}
