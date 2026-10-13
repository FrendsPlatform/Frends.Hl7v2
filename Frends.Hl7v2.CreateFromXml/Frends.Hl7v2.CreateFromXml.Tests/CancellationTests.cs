using System;
using System.Threading;
using System.Xml;
using Frends.Hl7v2.CreateFromXml.Definitions;
using Frends.Hl7v2.CreateFromXml.Helpers;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NUnit.Framework;

namespace Frends.Hl7v2.CreateFromXml.Tests;

[TestFixture]
public class CancellationTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void Precancelled_Task_Throws_Cancellation_Instead_Of_Handling_An_Input_Error(bool throwOnFailure)
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        var exception = AssertCancelled(() => Hl7v2.CreateFromXml(
            new Input { Xml = "Not XML" },
            new Options { ThrowErrorOnFailure = throwOnFailure, ErrorMessageOnFailure = "Custom error" },
            source.Token));

        Assert.That(exception.CancellationToken, Is.EqualTo(source.Token));
    }

    [Test]
    public void Stops_Reading_When_Cancelled()
    {
        using var source = new CancellationTokenSource();
        using var reader = new CancellableStringReader("synthetic input", source.Token);
        var buffer = new char[4];
        Assert.That(reader.Read(buffer, 0, buffer.Length), Is.EqualTo(buffer.Length));

        source.Cancel();

        using (Assert.EnterMultipleScope())
        {
            AssertCancelled(() => reader.Read());
            AssertCancelled(() => reader.Read(buffer, 0, buffer.Length));
            AssertCancelled(() => reader.Read(buffer.AsSpan()));
        }
    }

    [Test]
    public void Stops_While_Loading_The_Document()
    {
        using var source = new CancellationTokenSource();
        var document = new ParsingXmlDocument(source.Token);
        var inserted = 0;
        document.NodeInserted += (_, _) =>
        {
            if (++inserted == 3)
                source.Cancel();
        };
        var message = SyntheticHl7Message.Create(1, 100);
        using var reader = new CancellableStringReader(message.Xml, source.Token);

        var exception = AssertCancelled(() => document.Load(reader));

        Assert.That(exception.CancellationToken, Is.EqualTo(source.Token));
        Assert.That(inserted, Is.LessThan(10));
    }

    [Test]
    public void Stops_Inside_NHapi_Child_Node_Loops()
    {
        using var source = new CancellationTokenSource();
        var document = new ParsingXmlDocument(source.Token);
        document.LoadXml("<root><one/><two/></root>");
        var children = document.DocumentElement.ChildNodes;
        Assert.That(children.Count, Is.EqualTo(2));

        source.Cancel();

        using (Assert.EnterMultipleScope())
        {
            AssertCancelled(() => _ = children.Count);
            AssertCancelled(() => children.Item(0));
            AssertCancelled(() => children.GetEnumerator().MoveNext());
        }
    }

    [Test]
    public void Stops_Between_Segments_Without_Processing_The_Remaining_Message()
    {
        using var source = new CancellationTokenSource();
        var parser = new SegmentCancellingParser(source);
        var message = SyntheticHl7Message.Create(1, 100);

        var exception = AssertCancelled(() => parser.Parse(message.Xml));

        Assert.That(exception.CancellationToken, Is.EqualTo(source.Token));
        Assert.That(parser.ObservationsVisited, Is.EqualTo(2));
    }

    [Test]
    public void Stops_Between_Repeated_Fields_Within_One_Segment()
    {
        using var source = new CancellationTokenSource();
        var parser = new FieldCancellingParser(source);
        var message = SyntheticHl7Message.Create(1, 1, 100);

        var exception = AssertCancelled(() => parser.Parse(message.Xml));

        Assert.That(exception.CancellationToken, Is.EqualTo(source.Token));
        Assert.That(parser.ValuesVisited, Is.EqualTo(3));
    }

    private static OperationCanceledException AssertCancelled(Action action)
    {
        return Assert.Throws<OperationCanceledException>(action);
    }

    private sealed class SegmentCancellingParser : CancellableXmlParser
    {
        private readonly CancellationTokenSource source;

        internal SegmentCancellingParser(CancellationTokenSource source)
            : base(source.Token)
        {
            this.source = source;
        }

        internal int ObservationsVisited { get; private set; }

        public override void Parse(ISegment segmentObject, XmlElement segmentElement, ParserOptions parserOptions)
        {
            if (segmentElement.LocalName == "OBX" && ++ObservationsVisited == 2)
                source.Cancel();
            base.Parse(segmentObject, segmentElement, parserOptions);
        }
    }

    private sealed class FieldCancellingParser : CancellableXmlParser
    {
        private readonly CancellationTokenSource source;

        internal FieldCancellingParser(CancellationTokenSource source)
            : base(source.Token)
        {
            this.source = source;
        }

        internal int ValuesVisited { get; private set; }

        public override void Parse(IType datatypeObject, XmlElement datatypeElement, ParserOptions parserOptions)
        {
            if (datatypeElement.LocalName == "OBX.5" && datatypeObject is IPrimitive && ++ValuesVisited == 3)
                source.Cancel();
            base.Parse(datatypeObject, datatypeElement, parserOptions);
        }
    }
}
