using System;
using System.ComponentModel;
using System.Threading;
using Frends.Hl7v2.ConvertToXml.Definitions;
using Frends.Hl7v2.ConvertToXml.Helpers;
using NHapi.Base.Parser;

namespace Frends.Hl7v2.ConvertToXml;

/// <summary>
/// Task Class for  operations.
/// </summary>
public static class Hl7v2
{
    /// <summary>
    /// Task to convert Hl7v2 message to Xml
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-Hl7v2-ConvertToXml)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string Xml, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static Result ConvertToXml(
        [PropertyTab] Input input,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(input.Hl7v2Message))
                throw new ArgumentNullException(nameof(input), "You must provide an HL7 message.");
            var parser = new PipeParser();
            var parsed = parser.Parse(input.Hl7v2Message);

            var xmlParser = new DefaultXMLParser();
            var lineEnding = options.LineEnding switch
            {
                LineEnding.CRLF => "\r\n",
                LineEnding.LF => "\n",
                _ => throw new ArgumentOutOfRangeException(nameof(options), "options.LineEnding is not valid."),
            };

            var encoded = xmlParser.Encode(parsed).ReplaceLineEndings(lineEnding);

            cancellationToken.ThrowIfCancellationRequested();

            return new Result
            {
                Success = true,
                Xml = encoded,
            };
        }
        catch (Exception ex)
        {
            return ErrorHandler.Handle(ex, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }
}
