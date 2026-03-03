using System;
using System.Threading;
using Frends.Hl7v2.ConvertToXml.Definitions;
using NUnit.Framework;

namespace Frends.Hl7v2.ConvertToXml.Tests;

[TestFixture]
public class ErrorHandlerTest
{
    private const string CustomErrorMessage = "CustomErrorMessage";

    [Test]
    public void Should_Throw_Error_When_ThrowErrorOnFailure_Is_True()
    {
        var ex = Assert.Throws<Exception>(() =>
            Hl7v2.ConvertToXml(new Input(), new Options(), CancellationToken.None));
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void Should_Return_Failed_Result_When_ThrowErrorOnFailure_Is_False()
    {
        var options = new Options();
        options.ThrowErrorOnFailure = false;
        var result = Hl7v2.ConvertToXml(new Input(), options, CancellationToken.None);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Should_Use_Custom_ErrorMessageOnFailure()
    {
        var options = new Options
        {
            ErrorMessageOnFailure = CustomErrorMessage,
        };
        var ex = Assert.Throws<Exception>(() =>
            Hl7v2.ConvertToXml(new Input(), options, CancellationToken.None));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Contains.Substring(CustomErrorMessage));
    }
}
