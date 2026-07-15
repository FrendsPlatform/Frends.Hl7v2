using Frends.Hl7v2.Validate.Definitions;

namespace Frends.Hl7v2.Validate.Tests;

internal abstract class TestBase
{
    protected static Input DefaultInput() => new();

    protected static Options DefaultOptions() => new();
}
