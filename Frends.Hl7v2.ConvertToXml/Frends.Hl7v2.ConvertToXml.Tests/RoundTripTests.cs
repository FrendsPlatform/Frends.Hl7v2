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

namespace Frends.Hl7v2.ConvertToXml.Tests;

[TestFixture]
public class RoundTripTests
{
    private string hl7;

    [OneTimeSetUp]
    public void Setup()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "RoundTripInput.hl7");
        hl7 = File.ReadAllText(path);
    }

    [TestCase(ConvertLineEnding.LF, CreateLineEnding.CR)]
    [TestCase(ConvertLineEnding.LF, CreateLineEnding.LF)]
    [TestCase(ConvertLineEnding.CRLF, CreateLineEnding.CRLF)]
    public void Hl7_Should_Survive_Validate_ConvertToXml_CreateFromXml_Unchanged(
        ConvertLineEnding convertEnding,
        CreateLineEnding createEnding)
    {
        var original = hl7.Replace("\r\n", "\r").Replace("\n", "\r").TrimEnd('\r');

        var validated = Validate.Hl7v2.Validate(
            new ValidateInput { Hl7v2Message = original },
            new ValidateOptions { ThrowErrorOnFailure = false },
            CancellationToken.None);
        Assert.That(validated.Success, Is.True, validated.Error?.Message);
        Assert.That(validated.IsValid, Is.True, string.Join("; ", validated.ValidationErrors));

        var converted = Hl7v2.ConvertToXml(
            new ConvertInput { Hl7v2Message = original },
            new ConvertOptions { LineEnding = convertEnding, ThrowErrorOnFailure = false },
            CancellationToken.None);
        Assert.That(converted.Success, Is.True, converted.Error?.Message);

        var created = CreateFromXml.Hl7v2.CreateFromXml(
            new CreateInput { Xml = converted.Xml },
            new CreateOptions { LineEnding = createEnding, ThrowErrorOnFailure = false },
            CancellationToken.None);
        Assert.That(created.Success, Is.True, created.Error?.Message);

        Assert.That(created.Hl7v2Message.Replace("\r\n", "\r").Replace("\n", "\r").TrimEnd('\r'), Is.EqualTo(original));
    }
}
