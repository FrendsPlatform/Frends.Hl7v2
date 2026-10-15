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
    public void Supports_Repeated_Indexed_Access()
    {
        var document = new XmlDocument();
        document.LoadXml("<root>" + string.Concat(Enumerable.Repeat("<field>value</field>", 1000)) + "</root>");
        var indexed = new IndexedXmlNodeList(document.DocumentElement.ChildNodes, CancellationToken.None);

        for (var i = 0; i < indexed.Count; i++)
            Assert.That(indexed[i].InnerText, Is.EqualTo("value"));
        Assert.That(indexed.Cast<XmlNode>().Count(), Is.EqualTo(1000));
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
}
