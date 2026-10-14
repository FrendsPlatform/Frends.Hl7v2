using System.IO;
using System.Threading;
using NUnit.Framework;
using ConvertInput = Frends.Hl7v2.ConvertToXml.Definitions.Input;
using ConvertLineEnding = Frends.Hl7v2.ConvertToXml.Definitions.LineEnding;
using ConvertOptions = Frends.Hl7v2.ConvertToXml.Definitions.Options;
using CreateInput = Frends.Hl7v2.CreateFromXml.Definitions.Input;
using CreateLineEnding = Frends.Hl7v2.CreateFromXml.Definitions.LineEnding;
using CreateOptions = Frends.Hl7v2.CreateFromXml.Definitions.Options;
using ValidateInput = Frends.Hl7v2.Validate.Definitions.Input;
using ValidateOptions = Frends.Hl7v2.Validate.Definitions.Options;

namespace Frends.Hl7v2.CreateFromXml.Tests;

[TestFixture]
public class RoundTripTests
{
    private string xml;

    [OneTimeSetUp]
    public void Setup()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "RoundTripInput.xml");
        xml = File.ReadAllText(path).Trim();
    }

    [TestCase(CreateLineEnding.CR, ConvertLineEnding.LF)]
    [TestCase(CreateLineEnding.LF, ConvertLineEnding.LF)]
    [TestCase(CreateLineEnding.CRLF, ConvertLineEnding.CRLF)]
    public void Xml_Should_Survive_CreateFromXml_Validate_ConvertToXml_Unchanged(
        CreateLineEnding createEnding,
        ConvertLineEnding convertEnding)
    {
        var created = CreateFromXml.Hl7v2.CreateFromXml(
            new CreateInput { Xml = xml },
            new CreateOptions { LineEnding = createEnding, ThrowErrorOnFailure = false },
            CancellationToken.None);
        Assert.That(created.Success, Is.True, created.Error?.Message);

        var validated = Validate.Hl7v2.Validate(
            new ValidateInput { Hl7v2Message = created.Hl7v2Message },
            new ValidateOptions { ThrowErrorOnFailure = false },
            CancellationToken.None);
        Assert.That(validated.Success, Is.True, validated.Error?.Message);
        Assert.That(validated.IsValid, Is.True, string.Join("; ", validated.ValidationErrors));

        var converted = ConvertToXml.Hl7v2.ConvertToXml(
            new ConvertInput { Hl7v2Message = created.Hl7v2Message },
            new ConvertOptions { LineEnding = convertEnding, ThrowErrorOnFailure = false },
            CancellationToken.None);
        Assert.That(converted.Success, Is.True, converted.Error?.Message);

        Assert.That(converted.Xml, Is.EqualTo(xml.ReplaceLineEndings(convertEnding == ConvertLineEnding.CRLF ? "\r\n" : "\n")));
    }
}
