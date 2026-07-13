using System.IO;
using System.Threading;
using Frends.Hl7v2.Validate.Definitions;
using NUnit.Framework;

namespace Frends.Hl7v2.Validate.Tests;

[TestFixture]
public class FunctionalTests
{
    private string validHl7Message;
    private string invalidHl7Message;

    [OneTimeSetUp]
    public void Setup()
    {
        var hl7Path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "TestInput.hl7");
        validHl7Message = File.ReadAllText(hl7Path);
        invalidHl7Message = "PID|1||12345^^^MRN||DOE^JOHN||19800101|M\r";
    }

    [Test]
    public void Should_Return_Valid_For_Correct_Hl7_Message()
    {
        var result = Hl7v2.Validate(
            new Input { Hl7v2Message = validHl7Message },
            new Options { ThrowErrorOnFailure = false },
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ValidationErrors, Is.Empty);
    }

    [Test]
    public void Should_Return_Invalid_For_Message_Missing_MSH()
    {
        var result = Hl7v2.Validate(
            new Input { Hl7v2Message = invalidHl7Message },
            new Options { ThrowErrorOnFailure = false },
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsValid, Is.False);
    }
}
