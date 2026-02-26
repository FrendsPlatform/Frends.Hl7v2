using dotenv.net;
using Frends.Hl7v2.ConvertToXml.Definitions;

namespace Frends.Hl7v2.ConvertToXml.Tests;

public abstract class TestBase
{
    protected TestBase()
    {
        DotEnv.Load();
    }

    protected static Input DefaultInput() => new();

    protected static Options DefaultOptions() => new()
    {
        ThrowErrorOnFailure = true,
        ErrorMessageOnFailure = string.Empty,
    };
}
