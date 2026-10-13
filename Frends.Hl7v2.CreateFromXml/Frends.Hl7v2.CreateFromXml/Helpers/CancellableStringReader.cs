using System;
using System.IO;
using System.Threading;

namespace Frends.Hl7v2.CreateFromXml.Helpers;

internal sealed class CancellableStringReader : StringReader
{
    private readonly CancellationToken cancellationToken;

    internal CancellableStringReader(string value, CancellationToken cancellationToken)
        : base(value)
    {
        this.cancellationToken = cancellationToken;
    }

    /// <inheritdoc />
    /// <summary>
    /// Reads a character unless cancellation was requested.
    /// </summary>
    public override int Read()
    {
        cancellationToken.ThrowIfCancellationRequested();
        return base.Read();
    }

    /// <inheritdoc />
    /// <summary>
    /// Reads a buffer unless cancellation was requested.
    /// </summary>
    public override int Read(char[] buffer, int index, int count)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return base.Read(buffer, index, count);
    }

    /// <inheritdoc />
    /// <summary>
    /// Reads a span unless cancellation was requested.
    /// </summary>
    public override int Read(Span<char> buffer)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return base.Read(buffer);
    }
}
