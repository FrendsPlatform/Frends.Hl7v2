using System.IO;
using System.Threading;
using Frends.Hl7v2.CreateFromXml.Definitions;
using NHapi.Base.Parser;
using NUnit.Framework;

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
}
