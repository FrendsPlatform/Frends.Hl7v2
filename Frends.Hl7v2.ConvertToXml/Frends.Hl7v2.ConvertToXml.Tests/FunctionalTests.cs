using System;
using System.IO;
using System.Threading;
using Frends.Hl7v2.ConvertToXml.Definitions;
using NHapi.Base.Parser;
using NUnit.Framework;

namespace Frends.Hl7v2.ConvertToXml.Tests;

[TestFixture]
public class FunctionalTests
{
    private string hl7V2Message;

    private string hl7V2LfMessage;

    private string xmlMessage;

    private string xmlLfMessage;

    [OneTimeSetUp]
    public void Setup()
    {
        var hl7Path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "TestInput.hl7");
        var hl7LfPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "TestInputLF.hl7");
        var xmlPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "TestOutput.xml.test");
        var xmlLfPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "TestOutputLF.xml.test");
        xmlMessage = File.ReadAllText(xmlPath).Trim();
        xmlLfMessage = File.ReadAllText(xmlLfPath).Trim();
        hl7V2Message = File.ReadAllText(hl7Path);
        hl7V2LfMessage = File.ReadAllText(hl7LfPath);
    }

    [Test]
    public void Should_Convert_Hl7v2_Message_To_Xml()
    {
        var input = new Input
        {
            Hl7v2Message = hl7V2Message,
        };

        var options = new Options
        {
            LineEnding = LineEnding.CRLF,
        };

        var result = Hl7v2.ConvertToXml(input, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Xml, Is.EqualTo(xmlMessage));
    }

    [Test]
    public void Should_Convert_Hl7v2_LF_Message_To_Xml()
    {
        var input = new Input
        {
            Hl7v2Message = hl7V2LfMessage,
        };

        var result = Hl7v2.ConvertToXml(input, new Options(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Xml, Is.EqualTo(xmlLfMessage));
    }

    [Test]
    public void Should_Convert_Hl7v2_CR_Message_To_Xml()
    {
        var input = new Input
        {
            Hl7v2Message = hl7V2Message.Replace("\r\n", "\r"),
        };
        var options = new Options
        {
            LineEnding = LineEnding.CRLF,
        };
        var result = Hl7v2.ConvertToXml(input, options, CancellationToken.None);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Xml, Is.EqualTo(xmlMessage));
    }

    [Test]
    public void Should_Fail_With_Invalid_Input()
    {
        var input = new Input
        {
            Hl7v2Message = "Invalid message",
        };
        var opt = new Options
        {
            ThrowErrorOnFailure = false,
        };

        var result = Hl7v2.ConvertToXml(input, opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Contains.Substring("Invalid message"));
        Assert.That(result.Error.AdditionalInfo is EncodingNotSupportedException, Is.True);
    }

    [Test]
    public void Should_AutoDetect_Version_And_MessageType_From_MSH()
    {
        var hl7v23 =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|2.3\r" +
            "PID|1||12345^^^MRN||DOE^JOHN||19800101|M\r";
        var hl7v251 =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|2.5.1\r" +
            "PID|1||12345^^^MRN||DOE^JOHN||19800101|M\r";
        var hl7NoVersion =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|\r" +
            "PID|1||12345^^^MRN||DOE^JOHN||19800101|M\r";

        var lf = new Options { LineEnding = LineEnding.LF };

        var resultV23 = Hl7v2.ConvertToXml(new Input { Hl7v2Message = hl7v23 }, lf, CancellationToken.None);
        var resultV251 = Hl7v2.ConvertToXml(new Input { Hl7v2Message = hl7v251 }, lf, CancellationToken.None);
        var resultNoVersion = Hl7v2.ConvertToXml(
            new Input { Hl7v2Message = hl7NoVersion },
            new Options
            {
            LineEnding = LineEnding.LF,
            ThrowErrorOnFailure = false,
            },
            CancellationToken.None);

        Assert.That(resultV23.Success, Is.True);
        Assert.That(resultV23.Xml, Does.Contain("<ADT_A01"));
        Assert.That(resultV251.Success, Is.True);
        Assert.That(resultV251.Xml, Does.Contain("<ADT_A01"));

        // Different versions produce different output (version-specific field definitions applied)
        Assert.That(resultV23.Xml, Is.Not.EqualTo(resultV251.Xml));

        // Missing version fails cleanly — no silent fallback to a default schema
        Assert.That(resultNoVersion.Success, Is.False);
        Assert.That(resultNoVersion.Error, Is.Not.Null);
    }

    [Test]
    public void Should_AutoDetect_MessageType_And_TriggerEvent_From_MSH9()
    {
        var hl7Oru =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ORU^R01|123|P|2.3\r" +
            "PID|1||12345^^^MRN||DOE^JOHN||19800101|M\r" +
            "OBR|1||ORDER123||\r" +
            "OBX|1|ST|TEST^Test Name||Result Value||||||F\r";
        var hl7Adt =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A03|123|P|2.3\r" +
            "PID|1||12345^^^MRN||DOE^JOHN||19800101|M\r";

        var lf = new Options { LineEnding = LineEnding.LF };

        var resultOru = Hl7v2.ConvertToXml(new Input { Hl7v2Message = hl7Oru }, lf, CancellationToken.None);
        var resultAdt = Hl7v2.ConvertToXml(new Input { Hl7v2Message = hl7Adt }, lf, CancellationToken.None);

        Assert.That(resultOru.Success, Is.True);
        Assert.That(resultOru.Xml, Does.Contain("<ORU_R01"));
        Assert.That(resultAdt.Success, Is.True);

        // Trigger event A03 preserved in MSH.9
        Assert.That(resultAdt.Xml, Does.Contain("<CM_MSG.2>A03</CM_MSG.2>"));
    }

    [Test]
    public void Should_Parse_Unknown_Segments_As_GenericSegment()
    {
        var hl7 =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|2.3\r" +
            "PID|1||12345^^^MRN||DOE^JOHN||19800101|M\r" +
            "ZIN|CONTR1|PAT1|110\r" +
            "QQQ|foo|bar\r" +
            "NK1|1|DOE^JANE\r";

        var result = Hl7v2.ConvertToXml(
            new Input { Hl7v2Message = hl7 },
            new Options { LineEnding = LineEnding.LF },
            CancellationToken.None);

        Assert.That(result.Success, Is.True);

        // Z-segment: fields sequentially numbered, values preserved
        Assert.That(result.Xml, Does.Contain("<ZIN.1>CONTR1</ZIN.1>"));
        Assert.That(result.Xml, Does.Contain("<ZIN.2>PAT1</ZIN.2>"));
        Assert.That(result.Xml, Does.Contain("<ZIN.3>110</ZIN.3>"));

        // Non-Z unrecognized segment: same behavior, not Z-prefix dependent
        Assert.That(result.Xml, Does.Contain("<QQQ.1>foo</QQQ.1>"));
        Assert.That(result.Xml, Does.Contain("<QQQ.2>bar</QQQ.2>"));

        // Standard segments around unknown ones are not corrupted
        Assert.That(result.Xml, Does.Contain("<NK1>"));
        Assert.That(result.Xml, Does.Contain("JANE"));
    }

    [Test]
    public void Should_Parse_MSH_Delimiters_Dynamically()
    {
        var hl7Standard =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|2.3\r" +
            "PID|1||12345^^^MRN&2.16.840.1&ISO||DOE^JOHN||19800101|M\r";
        var hl7NonStandardSep =
            "MSH#^~\\&#SendingApp#SendingFac#ReceivingApp#ReceivingFac#20060228155525##ADT^A01#123#P#2.3\r" +
            "PID#1##12345^^^MRN##DOE^JOHN##19800101#M\r";

        var lf = new Options { LineEnding = LineEnding.LF };

        var resultStd = Hl7v2.ConvertToXml(new Input { Hl7v2Message = hl7Standard }, lf, CancellationToken.None);
        var resultNonStd = Hl7v2.ConvertToXml(new Input { Hl7v2Message = hl7NonStandardSep }, lf, CancellationToken.None);

        // Standard delimiters
        Assert.That(resultStd.Success, Is.True);
        Assert.That(resultStd.Xml, Does.Contain("<MSH.1>|</MSH.1>"));
        Assert.That(resultStd.Xml, Does.Contain("<MSH.2>^~\\&amp;</MSH.2>"));
        Assert.That(resultStd.Xml, Does.Contain("<XPN.1>DOE</XPN.1>"));
        Assert.That(resultStd.Xml, Does.Contain("<CM_MSG.1>ADT</CM_MSG.1>"));

        // Subcomponent separator & works for schema-defined subcomponents (CX.4 = HD type)
        Assert.That(resultStd.Xml, Does.Contain("<HD.1>MRN</HD.1>"));
        Assert.That(resultStd.Xml, Does.Contain("<HD.2>2.16.840.1</HD.2>"));
        Assert.That(resultStd.Xml, Does.Contain("<HD.3>ISO</HD.3>"));

        // Non-standard field separator # read dynamically from MSH-1
        Assert.That(resultNonStd.Success, Is.True);
        Assert.That(resultNonStd.Xml, Does.Contain("<MSH.1>#</MSH.1>"));
        Assert.That(resultNonStd.Xml, Does.Contain("SendingApp"));
    }

    [Test]
    public void Should_Handle_Trailing_Delimiters_And_Repetition_Operator()
    {
        var hl7Trailing =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|2.3\r" +
            "PID|1||12345^^^MRN||DOE^JOHN||19800101|M|||||||||||\r";
        var hl7Repetition =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|2.3\r" +
            "PID|1||12345^^^MRN~67890^^^SS||DOE^JOHN~SMITH^JANE||19800101|M\r";

        var lf = new Options { LineEnding = LineEnding.LF };

        var resultTrailing = Hl7v2.ConvertToXml(new Input { Hl7v2Message = hl7Trailing }, lf, CancellationToken.None);
        var resultRepetition = Hl7v2.ConvertToXml(new Input { Hl7v2Message = hl7Repetition }, lf, CancellationToken.None);

        // Trailing delimiters: core data intact
        Assert.That(resultTrailing.Success, Is.True);
        Assert.That(resultTrailing.Xml, Does.Contain("<CX.1>12345</CX.1>"));
        Assert.That(resultTrailing.Xml, Does.Contain("<XPN.1>DOE</XPN.1>"));

        // Repetition ~
        Assert.That(resultRepetition.Success, Is.True);
        Assert.That(resultRepetition.Xml, Does.Contain("<CX.1>12345</CX.1>"));
        Assert.That(resultRepetition.Xml, Does.Contain("<CX.1>67890</CX.1>"));
        Assert.That(resultRepetition.Xml, Does.Contain("<XPN.1>DOE</XPN.1>"));
        Assert.That(resultRepetition.Xml, Does.Contain("<XPN.1>SMITH</XPN.1>"));
    }

    [Test]
    public void Should_Handle_HL7_Escape_Sequences()
    {
        var hl7 =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|2.3\r" +
            "NTE|1||\\F\\\r" +
            "NTE|2||\\S\\\r" +
            "NTE|3||\\T\\\r" +
            "NTE|4||\\R\\\r" +
            "NTE|5||\\E\\\r";

        var result = Hl7v2.ConvertToXml(
            new Input { Hl7v2Message = hl7 },
            new Options { LineEnding = LineEnding.LF },
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Xml, Does.Contain("<NTE.3>|</NTE.3>"));       // \F\ ? |
        Assert.That(result.Xml, Does.Contain("<NTE.3>^</NTE.3>"));       // \S\ ? ^
        Assert.That(result.Xml, Does.Contain("<NTE.3>&amp;</NTE.3>"));   // \T\ ? & (xml-escaped)
        Assert.That(result.Xml, Does.Contain("<NTE.3>~</NTE.3>"));       // \R\ ? ~
        Assert.That(result.Xml, Does.Contain("<NTE.3>\\</NTE.3>"));      // \E\ ? \
    }

    [Test]
    public void Should_Preserve_Extra_Z_Field_On_Known_Segment()
    {
        // Z-field appended beyond the standard field count of a known segment (PID in v2.3).
        // Confirms extra trailing fields on known segments survive, not just entire Z-segments.
        var hl7 =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|2.3\r" +
            "PID|1||12345^^^MRN||DOE^JOHN||19800101|M|||||||||||||||||||CUSTOM_Z_VALUE\r";

        var result = Hl7v2.ConvertToXml(
            new Input { Hl7v2Message = hl7 },
            new Options { LineEnding = LineEnding.LF },
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Xml, Does.Contain("CUSTOM_Z_VALUE"));
    }

    [Test]
    public void Should_Handle_HL7_Escape_Sequences_Subcomponent_Only_Where_Schema_Defines_Them()
    {
        var hl7 =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|2.3\r" +
            "PID|1||12345^4^ISO&HL7||DOE^JOHN||19800101|M\r";

        var result = Hl7v2.ConvertToXml(
            new Input { Hl7v2Message = hl7 },
            new Options { LineEnding = LineEnding.LF },
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Xml, Does.Contain("<CX.3>ISO</CX.3>"));
        Assert.That(result.Xml, Does.Not.Contain("HL7"));
    }

    [Test]
    public void Should_Handle_Large_50MB_Message()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|2.3\r");
        sb.Append("PID|1||12345^^^MRN||DOE^JOHN||19800101|M\r");

        var noteContent = new string('A', 80);
        int segmentCount = (50 * 1024 * 1024) / 100;
        for (int i = 1; i <= segmentCount; i++)
            sb.Append($"NTE|{i}||{noteContent}\r");

        var hl7 = sb.ToString();
        var memBefore = GC.GetTotalMemory(forceFullCollection: true);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var result = Hl7v2.ConvertToXml(
            new Input { Hl7v2Message = hl7 },
            new Options { LineEnding = LineEnding.LF },
            CancellationToken.None);

        sw.Stop();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Xml, Does.Contain("<ADT_A01"));
    }
}
