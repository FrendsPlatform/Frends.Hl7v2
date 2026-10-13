using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Frends.Hl7v2.CreateFromXml.Definitions;
using NHapi.Base.Parser;
using NUnit.Framework;

namespace Frends.Hl7v2.CreateFromXml.Tests;

[TestFixture]
public class ParserCompatibilityTests
{
    [TestCaseSource(nameof(AcceptedMessages))]
    public void Should_match_stock_parser_and_preserve_segment_data(string xml, string[] expectedSegments)
    {
        var expected = StockOutput(xml);
        var result = Hl7v2.CreateFromXml(
            new Input { Xml = xml },
            new Options { LineEnding = LineEnding.LF, ThrowErrorOnFailure = false },
            CancellationToken.None);

        Assert.That(result.Success, Is.True, result.Error?.Message);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Hl7v2Message, Is.EqualTo(expected));
            Assert.That(
                result.Hl7v2Message.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1),
                Is.EqualTo(expectedSegments),
                "Segment values, repetitions, empty positions and order must be retained.");
        }
    }

    [TestCaseSource(nameof(RejectedMessages))]
    public void Should_preserve_stock_rejections_in_additional_info(string xml)
    {
        Action parseStock = () => StockOutput(xml);
        var expected = Assert.Catch<Exception>(parseStock, "Stock NHapi must reject this fixture.");
        var result = Hl7v2.CreateFromXml(
            new Input { Xml = xml },
            new Options { ThrowErrorOnFailure = false },
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Hl7v2Message, Is.Null);
            Assert.That(result.Error.Message, Is.EqualTo(expected.Message));
        }

        var actual = result.Error.AdditionalInfo;
        while (expected != null)
        {
            Assert.That(actual, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual.GetType(), Is.EqualTo(expected.GetType()));
                Assert.That(actual.Message, Is.EqualTo(expected.Message));
            }

            expected = expected.InnerException;
            actual = actual.InnerException;
        }

        Assert.That(actual, Is.Null);
    }

#pragma warning disable SA1117, SA1118 // Keep inline XML fixtures beside their expected segments.
    private static IEnumerable<TestCaseData> AcceptedMessages()
    {
        foreach (var version in new[] { "2.3", "2.5" })
        {
            yield return Accepted(version, "NonContiguousInsuranceGroups", "ADT_A01", """
                <ADT_A01.INSURANCE>
                  <IN1><IN1.1>1</IN1.1><IN1.2><CE.1>PLAN-A</CE.1></IN1.2></IN1>
                  <IN2><IN2.2>MEMBER-A</IN2.2></IN2>
                </ADT_A01.INSURANCE>
                <PV1><PV1.1>1</PV1.1><PV1.2>I</PV1.2></PV1>
                <ADT_A01.INSURANCE>
                  <IN1><IN1.1>2</IN1.1><IN1.2><CE.1>PLAN-B</CE.1></IN1.2></IN1>
                  <IN2><IN2.2>MEMBER-B</IN2.2></IN2>
                </ADT_A01.INSURANCE>
                """,
                "PV1|1|I", "IN1|1|PLAN-A", "IN2||MEMBER-A", "IN1|2|PLAN-B", "IN2||MEMBER-B");

            yield return NestedGroups(version);

            yield return Accepted(version, "NonContiguousFieldsAndEmptyRepetition", "ADT_A01", """
                <PID>
                  <PID.1>1</PID.1>
                  <PID.3><CX.1>FIELD-A</CX.1></PID.3>
                  <PID.8>U</PID.8>
                  <PID.3/>
                  <PID.3><CX.1>FIELD-C</CX.1></PID.3>
                </PID>
                """,
                "PID|1||FIELD-A~~FIELD-C|||||U");

            // NHapi groups Z-segments by first-seen name, after the standard message structure.
            yield return Accepted(version, "ZSegmentNamesRepetitionsAndOrder", "ADT_A01", """
                <ZBB><ZBB.1>FIRST-A</ZBB.1><ZBB.3>THIRD</ZBB.3><ZBB.1>FIRST-B</ZBB.1></ZBB>
                <ZAA><ZAA.1>MIDDLE</ZAA.1></ZAA>
                <PID><PID.3><CX.1>STANDARD</CX.1></PID.3></PID>
                <ZBB><ZBB.1>LAST</ZBB.1></ZBB>
                <ZCC><ZCC.2>TAIL</ZCC.2></ZCC>
                """,
                "PID|||STANDARD", "ZBB|FIRST-A~FIRST-B||THIRD", "ZBB|LAST", "ZAA|MIDDLE", "ZCC||TAIL");

            yield return Accepted(version, "FixedPositionPidAndPid2", "ADT_A17", """
                <PID><PID.3><CX.1>SWAP-A</CX.1></PID.3></PID>
                <PV1><PV1.1>1</PV1.1><PV1.2>I</PV1.2></PV1>
                <PID><PID.3><CX.1>SWAP-B</CX.1></PID.3></PID>
                <PV1><PV1.1>2</PV1.1><PV1.2>O</PV1.2></PV1>
                """,
                "PID|||SWAP-A", "PV1|1|I", "PID|||SWAP-B", "PV1|2|O");

            yield return Accepted(version, "ExtraNonRepeatingSegment", "ADT_A01", """
                <EVN><EVN.1>A01</EVN.1></EVN>
                <EVN><EVN.1>A02</EVN.1></EVN>
                <PID><PID.3><CX.1>EXTRA-SEGMENT</CX.1></PID.3></PID>
                """,
                "EVN|A01", "PID|||EXTRA-SEGMENT", "EVN|A02");

            yield return Accepted(version, "WhitespaceCommentsAndProcessingInstructions", "ADT_A01", """
                <?fixture group?>
                <!-- Between group children. -->
                <PID>
                  <?fixture segment?>
                  <PID.1>  1  </PID.1>
                  <!-- Between fields. -->
                  <PID.3><CX.1>  WHITE
                     SPACE  </CX.1></PID.3>
                </PID>
                <ADT_A01.INSURANCE>
                  <?fixture nested-group?>
                  <!-- Before a segment. -->
                  <IN1>
                    <IN1.1>1</IN1.1>
                    <IN1.2><CE.1>PLAN<!-- Split text. --><?fixture component?>-A</CE.1></IN1.2>
                  </IN1>
                </ADT_A01.INSURANCE>
                """,
                "PID|1||WHITE SPACE", "IN1|1|PLAN-A");

            var prefixed = PrefixElements(Message(version, "ADT_A01", """
                <PID><PID.3><CX.1>PREFIXED</CX.1></PID.3></PID>
                <ADT_A01.INSURANCE>
                  <IN1><IN1.1>1</IN1.1><IN1.2><CE.1>PREFIXED-PLAN</CE.1></IN1.2></IN1>
                </ADT_A01.INSURANCE>
                """));
            yield return new TestCaseData(prefixed, new[] { "PID|||PREFIXED", "IN1|1|PREFIXED-PLAN" })
                .SetName($"PrefixedDocument_v{version}");

            yield return Accepted(version, "DifferentPrefixesForRepeatedFields", "ADT_A01", """
                <PID xmlns:h="urn:hl7-org:v2xml">
                  <h:PID.3><h:CX.1>PREFIX-A</h:CX.1></h:PID.3>
                  <PID.3><CX.1>PREFIX-B</CX.1></PID.3>
                </PID>
                """,
                "PID|||PREFIX-A~PREFIX-B");

            yield return Accepted(version, "CompositeSubcomponentsAndFirstDuplicate", "ADT_A01", """
                <PID>
                  <PID.3>
                    <CX.4><HD.3>ISO</HD.3><HD.1>AUTH-A</HD.1><HD.2>1.2.3.4</HD.2></CX.4>
                    <CX.1>COMPOSITE-A</CX.1><CX.1>IGNORED-DUPLICATE</CX.1>
                  </PID.3>
                  <PID.3><CX.1>COMPOSITE-B</CX.1><CX.4><HD.1>AUTH-B</HD.1></CX.4></PID.3>
                </PID>
                """,
                "PID|||COMPOSITE-A^^^AUTH-A&1.2.3.4&ISO~COMPOSITE-B^^^AUTH-B");

            yield return Accepted(version, "ExtraComponentsAndCompositeSubcomponents", "ADT_A01", """
                <OBX>
                  <OBX.1>1</OBX.1><OBX.2>ST</OBX.2>
                  <OBX.3>
                    <CE.1>EXTENDED</CE.1><CE.7>EXTRA-A</CE.7>
                    <CE.8><ST.1>SUB-A</ST.1><ST.2>SUB-B</ST.2></CE.8>
                  </OBX.3>
                  <OBX.5>VALUE</OBX.5>
                </OBX>
                """,
                "OBX|1|ST|EXTENDED^^^^^^EXTRA-A^SUB-A&SUB-B||VALUE");

            // Typed composites stop looking for extra components at the first missing extra index.
            yield return Accepted(version, "ExtraComponentGapUsesStockBehavior", "ADT_A01", """
                <OBX>
                  <OBX.1>1</OBX.1><OBX.2>ST</OBX.2>
                  <OBX.3><CE.1>GAP</CE.1><CE.8>IGNORED-AFTER-GAP</CE.8></OBX.3>
                  <OBX.5>PRESENT</OBX.5>
                </OBX>
                """,
                "OBX|1|ST|GAP||PRESENT");

            yield return Accepted(version, "PrimitiveObxRepetitionsAndHl7Escapes", "ADT_A01", """
                <OBX>
                  <OBX.1>1</OBX.1><OBX.2>ST</OBX.2><OBX.3><CE.1>TEXT</CE.1></OBX.3>
                  <OBX.5>LEFT<!-- Split text. --><?fixture text?>RIGHT</OBX.5>
                  <OBX.6><CE.1>UNIT</CE.1></OBX.6>
                  <OBX.5>before<escape V="H"/>marked<escape V="N"/>after</OBX.5>
                </OBX>
                """,
                @"OBX|1|ST|TEXT||LEFTRIGHT~before\H\marked\N\after|UNIT");

            yield return Accepted(version, "CompositeCeObxRepetitions", "ADT_A01", """
                <OBX>
                  <OBX.1>1</OBX.1><OBX.2>CE</OBX.2><OBX.3><CE.1>CODE</CE.1></OBX.3>
                  <OBX.5><CE.3>LOCAL</CE.3><CE.1>CODE-A</CE.1><CE.2>Alpha</CE.2></OBX.5>
                  <OBX.5><CE.1>CODE-B</CE.1><CE.2>Beta</CE.2><CE.3>LOCAL</CE.3></OBX.5>
                </OBX>
                """,
                "OBX|1|CE|CODE||CODE-A^Alpha^LOCAL~CODE-B^Beta^LOCAL");

            yield return Accepted(version, "CompositeEdObxWithNestedApplication", "ADT_A01", """
                <OBX>
                  <OBX.1>1</OBX.1><OBX.2>ED</OBX.2><OBX.3><CE.1>DATA</CE.1></OBX.3>
                  <OBX.5>
                    <ED.5>U1lOVEhFVElD</ED.5>
                    <ED.1><HD.1>SOURCE</HD.1><HD.2>1.2.3.4</HD.2><HD.3>ISO</HD.3></ED.1>
                    <ED.2>AP</ED.2><ED.3>PDF</ED.3><ED.4>Base64</ED.4>
                  </OBX.5>
                </OBX>
                """,
                "OBX|1|ED|DATA||SOURCE&1.2.3.4&ISO^AP^PDF^Base64^U1lOVEhFVElD");

            yield return Accepted(version, "XmlEntitiesAndPipeDelimiters", "ADT_A01", """
                <OBX>
                  <OBX.1>1</OBX.1><OBX.2>ST</OBX.2><OBX.3><CE.1>ESCAPES</CE.1></OBX.3>
                  <OBX.5>&lt;tag&gt;&amp;&quot;quoted&quot;&apos;|^~\</OBX.5>
                </OBX>
                """,
                """OBX|1|ST|ESCAPES||<tag>\T\"quoted"'\F\\S\\R\\E\""");

            yield return Accepted(version, "EmptyFieldsVersusExplicitHl7Null", "ADT_A01", """
                <OBX>
                  <OBX.1>1</OBX.1><OBX.2>ST</OBX.2><OBX.3><CE.1>EMPTY-NULL</CE.1></OBX.3>
                  <OBX.5>FIRST</OBX.5><OBX.5/><OBX.5>&quot;&quot;</OBX.5><OBX.5>LAST</OBX.5>
                </OBX>
                <ZEM><ZEM.1/><ZEM.2>&quot;&quot;</ZEM.2><ZEM.3></ZEM.3><ZEM.4>END</ZEM.4></ZEM>
                """,
                "OBX|1|ST|EMPTY-NULL||FIRST~~\"\"~LAST", "ZEM||\"\"||END");

            // Stock field/component searches include descendants, not only direct children.
            yield return Accepted(version, "DescendantFieldAndComponentMatches", "ADT_A01", """
                <PID>
                  <PID.3>
                    <wrapper><CX.1>WRAPPED-FIRST</CX.1></wrapper>
                    <CX.1>IGNORED-LATER</CX.1>
                  </PID.3>
                  <wrapper><PID.3><CX.1>WRAPPED-REP</CX.1></PID.3></wrapper>
                  <PID.3><CX.1>DIRECT-LAST</CX.1></PID.3>
                </PID>
                """,
                "PID|||WRAPPED-FIRST~WRAPPED-REP~DIRECT-LAST");

            // A later field with the same local name but a different namespace is filtered out.
            yield return Accepted(version, "ForeignNamespaceDuplicateFieldUsesStockBehavior", "ADT_A01", """
                <PID>
                  <PID.3><CX.1>KEPT</CX.1></PID.3>
                  <PID.3 xmlns="urn:synthetic:foreign"><CX.1>IGNORED</CX.1></PID.3>
                </PID>
                """,
                "PID|||KEPT");

            // Default XML parsing accepts these lexical values without SI/TS validation.
            yield return Accepted(version, "UnvalidatedSetIdUsesStockBehavior", "ADT_A01",
                "<PID><PID.1>not-a-number</PID.1></PID>", "PID|not-a-number");
            yield return Accepted(version, "UnvalidatedTimestampUsesStockBehavior", "ADT_A01",
                "<PID><PID.7><TS.1>not-a-timestamp</TS.1></PID.7></PID>", "PID|||||||not-a-timestamp");
        }
    }

    private static IEnumerable<TestCaseData> RejectedMessages()
    {
        foreach (var version in new[] { "2.3", "2.5" })
        {
            yield return Rejected(version, "InvalidRootNamespace",
                Message(version, "ADT_A01", string.Empty).Replace("urn:hl7-org:v2xml", "urn:synthetic:foreign"));

            var bodies = new (string Name, string Xml)[]
            {
                ("InvalidGroupNamespace", """
                    <ADT_A01.INSURANCE xmlns="urn:synthetic:foreign">
                      <IN1><IN1.1>1</IN1.1></IN1>
                    </ADT_A01.INSURANCE>
                    """),
                ("InvalidLaterSegmentNamespace", """
                    <PID><PID.3><CX.1>VALID</CX.1></PID.3></PID>
                    <PID xmlns="urn:synthetic:foreign"><PID.3><CX.1>INVALID</CX.1></PID.3></PID>
                    """),
                ("InvalidFirstFieldNamespace", """
                    <PID><PID.3 xmlns="urn:synthetic:foreign"><CX.1>INVALID</CX.1></PID.3></PID>
                    """),
                ("InvalidGenericComponentNamespace", """
                    <OBX><OBX.2>CE</OBX.2>
                      <OBX.5><CE.1 xmlns="urn:synthetic:foreign">INVALID</CE.1></OBX.5>
                    </OBX>
                    """),
                ("InvalidObxValueType", "<OBX><OBX.2>NOT_A_TYPE</OBX.2><OBX.5>VALUE</OBX.5></OBX>"),
            };
            foreach (var (name, body) in bodies)
                yield return Rejected(version, name, Message(version, "ADT_A01", body));

            foreach (var suffix in new[] { "bad", string.Empty, "0", "2147483648" })
            {
                yield return Rejected(version, $"InvalidFieldNumber_{(suffix.Length == 0 ? "empty" : suffix)}",
                    Message(version, "ADT_A01", $"<PID><PID.{suffix}>VALUE</PID.{suffix}></PID>"));
            }

            // GetVersion's prefix detection requires a double-quoted namespace declaration.
            yield return Rejected(version, "SingleQuotedPrefixedNamespace",
                PrefixElements(Message(version, "ADT_A01", string.Empty))
                    .Replace("xmlns:h=\"urn:hl7-org:v2xml\"", "xmlns:h='urn:hl7-org:v2xml'"));
        }
    }
#pragma warning restore SA1117, SA1118

    private static TestCaseData NestedGroups(string version)
    {
        var body = new StringBuilder();
        var expected = new List<string>();
        var responseGroup = version == "2.3" ? "RESPONSE" : "PATIENT_RESULT";
        foreach (var patient in new[] { "PATIENT-A", "PATIENT-B" })
        {
            body.Append($"<ORU_R01.{responseGroup}><ORU_R01.PATIENT>");
            body.Append($"<PID><PID.3><CX.1>{patient}</CX.1></PID.3></PID></ORU_R01.PATIENT>");
            expected.Add($"PID|||{patient}");
            for (var order = 1; order <= 2; order++)
            {
                var orderId = $"{patient}-ORDER-{order}";
                body.Append("<ORU_R01.ORDER_OBSERVATION>");
                body.Append($"<OBR><OBR.1>{order}</OBR.1><OBR.2><EI.1>{orderId}</EI.1></OBR.2></OBR>");
                body.Append($"<NTE><NTE.1>1</NTE.1><NTE.3>{orderId}-NOTE</NTE.3></NTE>");
                expected.Add($"OBR|{order}|{orderId}");
                expected.Add($"NTE|1||{orderId}-NOTE");
                for (var observation = 1; observation <= 2; observation++)
                {
                    var observationId = $"{orderId}-OBS-{observation}";
                    body.Append("<ORU_R01.OBSERVATION>");
                    body.Append($"<OBX><OBX.1>{observation}</OBX.1><OBX.2>ST</OBX.2>");
                    body.Append($"<OBX.3><CE.1>{observationId}</CE.1></OBX.3>");
                    body.Append($"<OBX.5>{observationId}-VALUE</OBX.5></OBX>");
                    body.Append($"<NTE><NTE.1>1</NTE.1><NTE.3>{observationId}-NOTE</NTE.3></NTE>");
                    body.Append("</ORU_R01.OBSERVATION>");
                    expected.Add($"OBX|{observation}|ST|{observationId}||{observationId}-VALUE");
                    expected.Add($"NTE|1||{observationId}-NOTE");
                }

                body.Append("</ORU_R01.ORDER_OBSERVATION>");
            }

            body.Append($"</ORU_R01.{responseGroup}>");
        }

        return Accepted(
            version,
            "NestedRepeatingGroupsKeepNotesInTheirOwnGroup",
            "ORU_R01",
            body.ToString(),
            expected.ToArray());
    }

    private static TestCaseData Accepted(string version, string name, string structure, string body, params string[] segments)
    {
        return new TestCaseData(Message(version, structure, body), segments).SetName($"{name}_v{version}");
    }

    private static TestCaseData Rejected(string version, string name, string xml)
    {
        return new TestCaseData(xml).SetName($"Reject_{name}_v{version}");
    }

    private static string StockOutput(string xml)
    {
        return new PipeParser().Encode(new DefaultXMLParser().Parse(xml)).ReplaceLineEndings("\n");
    }

    private static string PrefixElements(string xml)
    {
        return Regex.Replace(xml.Replace("xmlns=", "xmlns:h="), @"<(/?)([A-Z][A-Z0-9_.]*)", "<$1h:$2");
    }

    private static string Message(string version, string structure, string body)
    {
        var messageType = structure.Split('_');
        var typeName = version == "2.3" ? "CM_MSG" : "MSG";
        var versionValue = version == "2.3" ? version : $"<VID.1>{version}</VID.1>";
        return $"""
            <{structure} xmlns="urn:hl7-org:v2xml">
              <MSH>
                <MSH.1>|</MSH.1>
                <MSH.2>^~\&amp;</MSH.2>
                <MSH.3><HD.1>SYNTHETIC-SOURCE</HD.1></MSH.3>
                <MSH.9><{typeName}.1>{messageType[0]}</{typeName}.1><{typeName}.2>{messageType[1]}</{typeName}.2></MSH.9>
                <MSH.10>COMPATIBILITY-1</MSH.10>
                <MSH.11><PT.1>T</PT.1></MSH.11>
                <MSH.12>{versionValue}</MSH.12>
              </MSH>
              {body}
            </{structure}>
            """;
    }
}
