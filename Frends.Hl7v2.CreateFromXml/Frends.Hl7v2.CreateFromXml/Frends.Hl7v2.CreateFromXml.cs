using System;
using System.ComponentModel;
using System.Threading;
using Frends.Hl7v2.CreateFromXml.Definitions;
using Frends.Hl7v2.CreateFromXml.Helpers;
using NHapi.Base.Parser;

namespace Frends.Hl7v2.CreateFromXml;

/// <summary>
/// Task Class for Hl7v2 operations.
/// </summary>
public static class Hl7v2
{
    /// <summary>
    /// Task to create Hl7v2 message from Xml
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-Hl7v2-CreateFromXml)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string Hl7v2Message, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static Result CreateFromXml(
        [PropertyTab] Input input,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(input.Xml))
                throw new ArgumentNullException(nameof(input), "You must provide an XML.");
            var xmlParser = new DefaultXMLParser();
            var parsedMessage = xmlParser.Parse(input.Xml);

            var pipeParser = new PipeParser();
            var lineEnding = options.LineEnding switch
            {
                LineEnding.CRLF => "\r\n",
                LineEnding.LF => "\n",
                _ => throw new ArgumentOutOfRangeException(nameof(options), "options.LineEnding is not valid."),
            };
            var hl7Output = pipeParser.Encode(parsedMessage).ReplaceLineEndings(lineEnding);

            cancellationToken.ThrowIfCancellationRequested();

            return new Result
            {
                Success = true,
                Hl7v2Message = hl7Output,
            };
        }
        catch (Exception ex)
        {
            return ErrorHandler.Handle(ex, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }
}
