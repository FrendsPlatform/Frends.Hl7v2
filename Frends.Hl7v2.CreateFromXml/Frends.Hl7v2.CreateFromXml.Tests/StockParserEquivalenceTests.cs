using System.Linq;
using System.Text;
using System.Threading;
using Frends.Hl7v2.CreateFromXml.Definitions;
using NHapi.Base.Parser;
using NUnit.Framework;

namespace Frends.Hl7v2.CreateFromXml.Tests;

/// <summary>
/// Proves that the CustomXmlParser yields exactly what stock DefaultXMLParser yields,
/// for XML produced by NHapi itself from real pipe-delimited messages of several versions and types.
/// </summary>
[TestFixture]
public class StockParserEquivalenceTests
{
    private const string Escaped = @"Smith\F\Jr\S\\T\\R\\E\";

    [TestCaseSource(nameof(Versions))]
    public void Adt_A01_matches_stock_parser_and_original_message(string version)
    {
        var pipe = Join(
            $"MSH|^~\\&|SRC|FAC|DST|FAC2|20240101120000||ADT^A01^ADT_A01|MSG1|P|{version}",
            "EVN|A01|20240101120000",
            $"PID|1||12345^^^HOSP&1.2.3&ISO^MR~67890^^^OTHER^PI||{Escaped}^John^^^~Doe^Jane||19800101|M",
            "NK1|1|Smith^Anna|SPO",
            "NK1|2|Smith^Bob|CHD",
            "PV1|1|I|WARD^101^A||||1234^House^Gregory");

        AssertEquivalentAndLossless(pipe, version != "2.3");
    }

    [TestCaseSource(nameof(Versions))]
    public void Oru_R01_with_nested_groups_empty_and_repeated_fields_matches_stock_parser(string version)
    {
        var pipe = Join(
            $"MSH|^~\\&|LAB|FAC|EHR|FAC2|20240101120000||ORU^R01^ORU_R01|MSG2|P|{version}",
            "PID|1||999^^^HOSP^MR",
            "OBR|1|ORD-1||CBC^Blood count^L",
            "OBX|1|NM|WBC^White cells^L||7.2|10*9/L|4-10|N|||F",
            "NTE|1||first note",
            "OBX|2|ST|TXT^Text^L||a~b~c~d||||||F",
            "OBR|2|ORD-2||BMP^Panel^L",
            "OBX|1|ST|NA^Sodium^L||140||||||F",
            "OBX|2|ST|K^Potassium^L||4.1||||||F");

        AssertEquivalentAndLossless(pipe, version != "2.3");
    }

    [TestCaseSource(nameof(Versions))]
    public void Orm_O01_matches_stock_parser(string version)
    {
        var pipe = Join(
            $"MSH|^~\\&|APP|FAC|DST|FAC2|20240101120000||ORM^O01^ORM_O01|MSG3|P|{version}",
            "PID|1||1^^^H^MR||Test^Patient",
            "ORC|NW|A1|B1",
            "OBR|1|A1|B1|XRAY^Chest^L");

        AssertEquivalentAndLossless(pipe, version != "2.3");
    }

    [Test]
    public void Special_xml_characters_survive_round_trip()
    {
        var pipe = Join(
            "MSH|^~\\&|SRC|FAC|DST|FAC2|20240101120000||ORU^R01^ORU_R01|MSG4|P|2.5",
            "OBR|1|ORD-1",
            "OBX|1|ST|X^Y^L||<a href=\"x\">Tom \\T\\ Jerry's</a> \u0105\u0119\u0142\u00f3\u017c\u017a\u0107 \\F\\ \\S\\ \\R\\ \\E\\");

        var ours = AssertEquivalentAndLossless(pipe);

        Assert.That(ours,
            Does.Contain("<a href=\"x\">Tom \\T\\ Jerry's</a> \u0105\u0119\u0142\u00f3\u017c\u017a\u0107"));
    }

    [TestCase(2000, 5)]
    [TestCase(5000, 1)]
    public void Large_real_message_matches_stock_parser_and_original(int observations, int repetitions)
    {
        var lines = new StringBuilder();
        lines.Append("MSH|^~\\&|LAB|FAC|EHR|FAC2|20240101120000||ORU^R01^ORU_R01|BIG|P|2.5\r");
        lines.Append("PID|1||1^^^H^MR||Big^Patient\r");
        lines.Append("OBR|1|ORD-1||PANEL^Panel^L\r");
        for (var i = 1; i <= observations; i++)
        {
            var values = string.Join("~", Enumerable.Range(1, repetitions).Select(r => $"v{i}-{r}"));
            lines.Append($"OBX|{i}|ST|T{i}^Test {i}^L||{values}|||||F\r");
        }

        AssertEquivalentAndLossless(lines.ToString());
    }

    private static string[] Versions() => new[] { "2.3", "2.3.1", "2.4", "2.5", "2.5.1", "2.6", "2.7", "2.8" };

    private static string Join(params string[] segments) => string.Join("\r", segments) + "\r";

    // Returns our output; asserts it equals both stock-parser output and NHapi's own encoding of the source message.
    private static string AssertEquivalentAndLossless(string pipe, bool compareWithSource = true)
    {
        var pipeParser = new PipeParser();
        var original = pipeParser.Parse(pipe);
        var xml = new DefaultXMLParser().Encode(original);

        var stock = pipeParser.Encode(new DefaultXMLParser().Parse(xml));
        var canonical = pipeParser.Encode(original);

        var result = Hl7v2.CreateFromXml(
            new Input { Xml = xml },
            new Options
            {
                LineEnding = LineEnding.CR,
                ThrowErrorOnFailure = false
            },
            CancellationToken.None);

        Assert.That(result.Success, Is.True, result.Error?.Message);
        Assert.That(result.Hl7v2Message, Is.EqualTo(stock.ReplaceLineEndings("\r")), "differs from stock parser");
        if (compareWithSource)
            Assert.That(result.Hl7v2Message, Is.EqualTo(canonical.ReplaceLineEndings("\r")), "differs from source");

        return result.Hl7v2Message;
    }
}
