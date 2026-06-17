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
}
