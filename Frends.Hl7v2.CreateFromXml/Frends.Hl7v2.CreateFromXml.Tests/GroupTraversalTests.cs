using System;
using System.Threading;
using System.Xml;
using Frends.Hl7v2.CreateFromXml.Helpers;
using NHapi.Base.Parser;
using NUnit.Framework;

namespace Frends.Hl7v2.CreateFromXml.Tests;

[TestFixture]
public class GroupTraversalTests
{
    [TestCase(1, 128)]
    [TestCase(16, 8)]
    public void Maps_Groups_Without_Indexed_Child_Node_Access(int orders, int observations)
    {
        var input = SyntheticHl7Message.Create(orders, observations);
        var document = new CountingXmlDocument();
        document.LoadXml(input.Xml);
        document.TrackAccesses = true;

        var parsed = new CancellableXmlParser(CancellationToken.None).ParseDocument(document, "2.5", new ParserOptions());
        var output = new PipeParser().Encode(parsed).ReplaceLineEndings("\n");

        Assert.That(output, Is.EqualTo(input.ExpectedEr7));
        Assert.That(document.IndexedGroupAccesses, Is.Zero);
    }

    [Test]
    public void Cancels_While_Indexing_Sibling_Groups()
    {
        using var source = new CancellationTokenSource();
        var document = new CountingXmlDocument(source);
        document.LoadXml(SyntheticHl7Message.Create(1, 128).Xml);
        document.TrackAccesses = true;
        var parser = new CancellableXmlParser(source.Token);
        Action parse = () => parser.ParseDocument(document, "2.5", new ParserOptions());

        var exception = Assert.Throws<OperationCanceledException>(parse);

        Assert.That(exception.CancellationToken, Is.EqualTo(source.Token));
        Assert.That(document.ObservationLinksFollowed, Is.EqualTo(10));
    }

    private sealed class CountingXmlDocument : XmlDocument
    {
        private readonly CancellationTokenSource source;

        internal CountingXmlDocument(CancellationTokenSource source = null)
        {
            this.source = source;
        }

        internal bool TrackAccesses { get; set; }

        internal int IndexedGroupAccesses { get; set; }

        internal int ObservationLinksFollowed { get; set; }

        public override XmlElement CreateElement(string prefix, string localName, string namespaceUri)
        {
            return new CountingXmlElement(prefix, localName, namespaceUri, this);
        }

        private sealed class CountingXmlElement : XmlElement
        {
            private readonly CountingXmlDocument document;

            internal CountingXmlElement(string prefix, string localName, string namespaceUri, CountingXmlDocument document)
                : base(prefix, localName, namespaceUri, document)
            {
                this.document = document;
            }

            public override XmlNodeList ChildNodes
            {
                get
                {
                    if (document.TrackAccesses && LocalName.StartsWith("ORU_R01", StringComparison.Ordinal))
                        document.IndexedGroupAccesses++;
                    return base.ChildNodes;
                }
            }

            public override XmlNode NextSibling
            {
                get
                {
                    if (document.TrackAccesses && LocalName == "ORU_R01.OBSERVATION"
                        && ++document.ObservationLinksFollowed == 10)
                        document.source?.Cancel();
                    return base.NextSibling;
                }
            }
        }
    }
}
