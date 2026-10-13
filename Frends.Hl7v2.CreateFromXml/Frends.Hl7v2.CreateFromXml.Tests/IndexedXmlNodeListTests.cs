using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Xml;
using Frends.Hl7v2.CreateFromXml.Helpers;
using NHapi.Base.Parser;
using NUnit.Framework;

namespace Frends.Hl7v2.CreateFromXml.Tests;

[TestFixture]
public class IndexedXmlNodeListTests
{
    [TestCase("<root/>")]
    [TestCase("<root>text</root>")]
    [TestCase("<root>text<one/><!--comment--><?instruction value?><![CDATA[data]]><two/>tail</root>")]
    public void Retains_Node_Identity_Order_And_Out_Of_Range_Behavior(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        var expected = document.DocumentElement.ChildNodes.Cast<XmlNode>().ToArray();
        var indexed = new IndexedXmlNodeList(document.DocumentElement.ChildNodes, CancellationToken.None);

        Assert.That(indexed.Count, Is.EqualTo(expected.Length));
        Assert.That(indexed.Cast<XmlNode>(), Is.EqualTo(expected));
        for (var i = 0; i < expected.Length; i++)
            Assert.That(indexed[i], Is.SameAs(expected[i]));
        Assert.That(indexed[-1], Is.Null);
        Assert.That(indexed[indexed.Count], Is.Null);
    }

    [Test]
    public void Walks_Source_Once_And_Never_Uses_Its_Indexer_Or_Count()
    {
        var document = new XmlDocument();
        document.LoadXml("<root>" + string.Concat(Enumerable.Repeat("<field>value</field>", 1000)) + "</root>");
        var source = new TrackingNodeList(document.DocumentElement.ChildNodes);
        var indexed = new IndexedXmlNodeList(source, CancellationToken.None);

        for (var i = 0; i < indexed.Count; i++)
            Assert.That(indexed[i].InnerText, Is.EqualTo("value"));
        Assert.That(indexed.Cast<XmlNode>().Count(), Is.EqualTo(1000));

        Assert.That(source.Enumerations, Is.EqualTo(1));
        Assert.That(source.NodesVisited, Is.EqualTo(1000));
        Assert.That(source.CountReads, Is.Zero);
        Assert.That(source.IndexReads, Is.Zero);
    }

    [Test]
    public void Cancels_While_Constructing_The_Index()
    {
        using var cancellation = new CancellationTokenSource();
        var document = new XmlDocument();
        document.LoadXml("<root><one/><two/><three/><four/></root>");
        var source = new TrackingNodeList(
            document.DocumentElement.ChildNodes,
            visited =>
            {
                if (visited == 3)
                    cancellation.Cancel();
            });
        Action construct = () => _ = new IndexedXmlNodeList(source, cancellation.Token);

        var exception = Assert.Throws<OperationCanceledException>(construct);

        Assert.That(exception.CancellationToken, Is.EqualTo(cancellation.Token));
        Assert.That(source.NodesVisited, Is.EqualTo(3));
    }

    [Test]
    public void Cancels_All_Indexed_Access_Paths()
    {
        using var cancellation = new CancellationTokenSource();
        var document = new XmlDocument();
        document.LoadXml("<root><one/></root>");
        var indexed = new IndexedXmlNodeList(document.DocumentElement.ChildNodes, cancellation.Token);
        cancellation.Cancel();
        Action count = () => _ = indexed.Count;
        Action item = () => indexed.Item(0);
        Action enumerate = () => indexed.GetEnumerator().MoveNext();

        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<OperationCanceledException>(count);
            Assert.Throws<OperationCanceledException>(item);
            Assert.Throws<OperationCanceledException>(enumerate);
        }
    }

    [Test]
    public void Enables_Snapshots_Only_After_Document_Loading()
    {
        var input = SyntheticHl7Message.Create(1, 1, 1000);
        var document = new ParsingXmlDocument(CancellationToken.None);
        document.LoadXml(input.Xml);
        Assert.That(document.DocumentElement.ChildNodes, Is.Not.TypeOf<IndexedXmlNodeList>());
        var liveChildren = document.DocumentElement.ChildNodes;
        var comment = document.CreateComment("still loading");
        document.DocumentElement.AppendChild(comment);
        Assert.That(liveChildren[liveChildren.Count - 1], Is.SameAs(comment));

        var parsed = new CancellableXmlParser(CancellationToken.None).ParseDocument(document, "2.5", new ParserOptions());

        Assert.That(document.DocumentElement.ChildNodes, Is.TypeOf<IndexedXmlNodeList>());
        Assert.That(new PipeParser().Encode(parsed).ReplaceLineEndings("\n"), Is.EqualTo(input.ExpectedEr7));
    }

    private sealed class TrackingNodeList : XmlNodeList
    {
        private readonly XmlNodeList nodes;
        private readonly Action<int> onVisit;

        internal TrackingNodeList(XmlNodeList nodes, Action<int> onVisit = null)
        {
            this.nodes = nodes;
            this.onVisit = onVisit;
        }

        public override int Count
        {
            get
            {
                CountReads++;
                return nodes.Count;
            }
        }

        internal int Enumerations { get; private set; }

        internal int NodesVisited { get; private set; }

        internal int CountReads { get; private set; }

        internal int IndexReads { get; private set; }

        public override XmlNode Item(int index)
        {
            IndexReads++;
            return nodes.Item(index);
        }

        public override IEnumerator GetEnumerator()
        {
            Enumerations++;
            foreach (XmlNode node in nodes)
            {
                NodesVisited++;
                onVisit?.Invoke(NodesVisited);
                yield return node;
            }
        }
    }
}
