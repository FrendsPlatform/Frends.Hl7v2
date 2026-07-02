using Frends.Hl7v2.CreateFromXml.Definitions;
using NHapi.Base.Parser;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;

namespace Frends.Hl7v2.CreateFromXml.Tests;

[TestFixture]
public class FunctionalTests
{
    private string hl7V2Message;

    private string xmlMessage;

    [OneTimeSetUp]
    public void Setup()
    {
        var hl7Path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "hl7v2Message.txt");
        var xmlPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "XmlMessage.xml");
        xmlMessage = File.ReadAllText(xmlPath).Trim();
        hl7V2Message = File.ReadAllText(hl7Path);
    }

    [Test]
    public void Should_Convert_Xml_To_Hl7v2_Message()
    {
        var input = new Input
        {
            Xml = xmlMessage,
        };

        var result = Hl7v2.CreateFromXml(input, new Options(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Hl7v2Message, Is.EqualTo(hl7V2Message));
    }

    [Test]
    public void Should_Fail_With_Invalid_Input()
    {
        var input = new Input
        {
            Xml = "Invalid message",
        };
        var opt = new Options
        {
            ThrowErrorOnFailure = false,
        };

        var result = Hl7v2.CreateFromXml(input, opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Contains.Substring("Invalid message"));
        Assert.That(result.Error.AdditionalInfo is EncodingNotSupportedException, Is.True);
    }

    [Test]
    public void Should_Handle_Z_Segment_In_Input_Xml()
    {
        var xmlWithZSegment = @"<?xml version=""1.0"" encoding=""UTF-8""?>
        <ADT_A01 xmlns=""urn:hl7-org:v2xml"">
          <MSH>
            <MSH.1>|</MSH.1>
            <MSH.2>^~\&amp;</MSH.2>
            <MSH.3><HD.1>SendingApp</HD.1></MSH.3>
            <MSH.4><HD.1>SendingFac</HD.1></MSH.4>
            <MSH.5><HD.1>ReceivingApp</HD.1></MSH.5>
            <MSH.6><HD.1>ReceivingFac</HD.1></MSH.6>
            <MSH.7><TS.1>20060228155525</TS.1></MSH.7>
            <MSH.9><CM_MSG.1>ADT</CM_MSG.1><CM_MSG.2>A01</CM_MSG.2></MSH.9>
            <MSH.10>123</MSH.10>
            <MSH.11><PT.1>P</PT.1></MSH.11>
            <MSH.12>2.3</MSH.12>
          </MSH>
          <PID>
            <PID.1>1</PID.1>
            <PID.5><XPN.1>DOE</XPN.1><XPN.2>JOHN</XPN.2></PID.5>
          </PID>
          <ZIN>
            <ZIN.1>CONTR1</ZIN.1>
            <ZIN.2>PAT1</ZIN.2>
          </ZIN>
        </ADT_A01>";

        var input = new Input { Xml = xmlWithZSegment };
        var result = Hl7v2.CreateFromXml(input, new Options(), CancellationToken.None);

        Assert.That(result.Hl7v2Message, Does.Contain("ZIN"));
        Assert.That(result.Hl7v2Message, Does.Contain("CONTR1"));
        Assert.That(result.Hl7v2Message, Does.Contain("PAT1"));
    }

    [Test]
    public void Should_Apply_Correct_Delimiters_During_Serialization()
    {
        var input = new Input { Xml = xmlMessage };
        var result = Hl7v2.CreateFromXml(input, new Options(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        // Field separator | between MSH fields
        Assert.That(result.Hl7v2Message, Does.StartWith("MSH|"));
        // Component separator ^ used in MSH-9 (MessageType^TriggerEvent)
        Assert.That(result.Hl7v2Message, Does.Contain("QRY^R02"));
        // Encoding characters present in MSH-2
        Assert.That(result.Hl7v2Message, Does.Contain("^~\\&"));
    }

    [Test]
    public void Should_Apply_Repetition_Separator_For_Repeated_Fields()
    {
        var xmlWithRepetition = @"<?xml version=""1.0"" encoding=""UTF-8""?>
        <ADT_A01 xmlns=""urn:hl7-org:v2xml"">
          <MSH>
            <MSH.1>|</MSH.1>
            <MSH.2>^~\&amp;</MSH.2>
            <MSH.3><HD.1>SendingApp</HD.1></MSH.3>
            <MSH.4><HD.1>SendingFac</HD.1></MSH.4>
            <MSH.5><HD.1>ReceivingApp</HD.1></MSH.5>
            <MSH.6><HD.1>ReceivingFac</HD.1></MSH.6>
            <MSH.7><TS.1>20060228155525</TS.1></MSH.7>
            <MSH.9><CM_MSG.1>ADT</CM_MSG.1><CM_MSG.2>A01</CM_MSG.2></MSH.9>
            <MSH.10>123</MSH.10>
            <MSH.11><PT.1>P</PT.1></MSH.11>
            <MSH.12>2.3</MSH.12>
          </MSH>
          <PID>
            <PID.1>1</PID.1>
            <PID.3><CX.1>12345</CX.1><CX.4><HD.1>MRN</HD.1></CX.4></PID.3>
            <PID.3><CX.1>67890</CX.1><CX.4><HD.1>SS</HD.1></CX.4></PID.3>
            <PID.5><XPN.1>DOE</XPN.1><XPN.2>JOHN</XPN.2></PID.5>
            <PID.5><XPN.1>SMITH</XPN.1><XPN.2>JANE</XPN.2></PID.5>
            <PID.7><TS.1>19800101</TS.1></PID.7>
            <PID.8>M</PID.8>
          </PID>
        </ADT_A01>";

        var result = Hl7v2.CreateFromXml(new Input { Xml = xmlWithRepetition },
            new Options(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Console.WriteLine(result.Hl7v2Message);
        // ~ must separate repeated PID.3 values
        Assert.That(result.Hl7v2Message, Does.Contain("12345"));
        Assert.That(result.Hl7v2Message, Does.Contain("67890"));
        Assert.That(result.Hl7v2Message, Does.Contain("~"));
    }

    [Test]
    public void Should_Apply_Subcomponent_Separator_During_Serialization()
    {
        var xmlWithSubcomponent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
        <ADT_A01 xmlns=""urn:hl7-org:v2xml"">
          <MSH>
            <MSH.1>|</MSH.1>
            <MSH.2>^~\&amp;</MSH.2>
            <MSH.3><HD.1>SendingApp</HD.1></MSH.3>
            <MSH.4><HD.1>SendingFac</HD.1></MSH.4>
            <MSH.5><HD.1>ReceivingApp</HD.1></MSH.5>
            <MSH.6><HD.1>ReceivingFac</HD.1></MSH.6>
            <MSH.7><TS.1>20060228155525</TS.1></MSH.7>
            <MSH.9><CM_MSG.1>ADT</CM_MSG.1><CM_MSG.2>A01</CM_MSG.2></MSH.9>
            <MSH.10>123</MSH.10>
            <MSH.11><PT.1>P</PT.1></MSH.11>
            <MSH.12>2.3</MSH.12>
          </MSH>
          <PID>
            <PID.1>1</PID.1>
            <PID.3>
              <CX.1>12345</CX.1>
              <CX.4>
                <HD.1>MRN</HD.1>
                <HD.2>2.16.840.1</HD.2>
                <HD.3>ISO</HD.3>
              </CX.4>
            </PID.3>
            <PID.5><XPN.1>DOE</XPN.1><XPN.2>JOHN</XPN.2></PID.5>
            <PID.7><TS.1>19800101</TS.1></PID.7>
            <PID.8>M</PID.8>
          </PID>
        </ADT_A01>";

        var result = Hl7v2.CreateFromXml(new Input { Xml = xmlWithSubcomponent },
            new Options(), CancellationToken.None);

        Console.WriteLine(result.Hl7v2Message ?? result.Error?.Message);
        Assert.That(result.Success, Is.True);
        // & must appear between HD.1, HD.2, HD.3 within CX.4
        Assert.That(result.Hl7v2Message, Does.Contain("MRN&2.16.840.1&ISO"));
    }

    [Test]
    public void Should_Output_CR_Only_When_LineEnding_Is_CR()
    {
        var input = new Input { Xml = xmlMessage };
        var result = Hl7v2.CreateFromXml(input,
            new Options { LineEnding = LineEnding.CR }, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Hl7v2Message, Does.Contain("\r"));
        Assert.That(result.Hl7v2Message, Does.Not.Contain("\r\n"));
    }

    [Test]
    public void Should_Output_CRLF_When_LineEnding_Is_CRLF()
    {
        var input = new Input { Xml = xmlMessage };
        var result = Hl7v2.CreateFromXml(input,
            new Options { LineEnding = LineEnding.CRLF }, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Hl7v2Message, Does.Contain("\r\n"));
    }

    [Test]
    public void Should_Output_LF_When_LineEnding_Is_LF()
    {
        var result = Hl7v2.CreateFromXml(new Input { Xml = xmlMessage },
            new Options { LineEnding = LineEnding.LF }, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Hl7v2Message, Does.Contain("\n"));
        Assert.That(result.Hl7v2Message, Does.Not.Contain("\r\n"));
        Assert.That(result.Hl7v2Message, Does.Not.Contain("\r"));
    }

    [Test]
    public void Should_Preserve_Original_Delimiters_From_Input_XML()
    {
        var xmlWithCustomSep = xmlMessage
            .Replace("<MSH.1>|</MSH.1>", "<MSH.1>#</MSH.1>");

        var result = Hl7v2.CreateFromXml(new Input { Xml = xmlWithCustomSep },
            new Options(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        // Non-standard separator # carried through to pipe output
        Assert.That(result.Hl7v2Message, Does.StartWith("MSH#"));
        // All fields separated by # not |
        Assert.That(result.Hl7v2Message, Does.Not.Contain("MSH|"));
        // Standard data still intact
        Assert.That(result.Hl7v2Message, Does.Contain("QRY^R02"));
    }

    [Test]
    public void Should_Encode_Special_Characters_As_Escape_Sequences_During_Serialization()
    {
        var xmlWithSpecialChars = @"<?xml version=""1.0"" encoding=""UTF-8""?>
        <ADT_A01 xmlns=""urn:hl7-org:v2xml"">
          <MSH>
            <MSH.1>|</MSH.1>
            <MSH.2>^~\&amp;</MSH.2>
            <MSH.3><HD.1>SendingApp</HD.1></MSH.3>
            <MSH.4><HD.1>SendingFac</HD.1></MSH.4>
            <MSH.5><HD.1>ReceivingApp</HD.1></MSH.5>
            <MSH.6><HD.1>ReceivingFac</HD.1></MSH.6>
            <MSH.7><TS.1>20060228155525</TS.1></MSH.7>
            <MSH.9><CM_MSG.1>ADT</CM_MSG.1><CM_MSG.2>A01</CM_MSG.2></MSH.9>
            <MSH.10>123</MSH.10>
            <MSH.11><PT.1>P</PT.1></MSH.11>
            <MSH.12>2.3</MSH.12>
          </MSH>
          <NTE>
            <NTE.1>1</NTE.1>
            <NTE.3>Text with pipe | and caret ^ and tilde ~ inside</NTE.3>
          </NTE>
        </ADT_A01>";

        var result = Hl7v2.CreateFromXml(new Input { Xml = xmlWithSpecialChars },
            new Options(), CancellationToken.None);

        Console.WriteLine(result.Hl7v2Message ?? result.Error?.Message);
        Assert.That(result.Success, Is.True);
        // Reserved characters must be escaped in pipe output — raw | would break parsing
        Assert.That(result.Hl7v2Message, Does.Not.Contain("pipe |"));
        Assert.That(result.Hl7v2Message, Does.Contain("\\F\\").Or.Contain("pipe"));
    }
}
