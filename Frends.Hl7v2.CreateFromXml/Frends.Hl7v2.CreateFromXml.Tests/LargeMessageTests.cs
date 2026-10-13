using System.Threading;
using Frends.Hl7v2.CreateFromXml.Definitions;
using NUnit.Framework;

namespace Frends.Hl7v2.CreateFromXml.Tests;

[TestFixture]
public class LargeMessageTests
{
    [TestCase(1, 1, 1, 0)]
    [TestCase(1, 200, 1, 0)]
    [TestCase(20, 10, 1, 0)]
    [TestCase(1, 1, 200, 0)]
    [TestCase(1, 1, 1, 1024 * 1024)]
    public void Preserves_All_Observations_And_Field_Repetitions(
        int orders,
        int observationsPerOrder,
        int fieldRepetitions,
        int valueLength)
    {
        var message = SyntheticHl7Message.Create(orders, observationsPerOrder, fieldRepetitions, valueLength);

        var result = Hl7v2.CreateFromXml(new Input { Xml = message.Xml }, new Options(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Hl7v2Message, Is.EqualTo(message.ExpectedEr7));
    }
}
