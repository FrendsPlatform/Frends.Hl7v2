using System;
using System.ComponentModel;
using System.Threading;
using Frends.Hl7v2.Validate.Definitions;
using Frends.Hl7v2.Validate.Helpers;
using NHapi.Base.Parser;
using NHapi.Base.Validation;
using NHapi.Base.Validation.Implementation;

namespace Frends.Hl7v2.Validate;

/// <summary>
/// Task Class for Hl7v2 operations.
/// </summary>
public static class Hl7v2
{
    /// <summary>
    /// Task to validate Hl7v2 message
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-Hl7v2-Validate)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, bool IsValid, string[] ValidationErrors, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static Result Validate(
        [PropertyTab] Input input,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(input.Hl7v2Message))
            {
                throw new ArgumentNullException(nameof(input), "You must provide an HL7 message.");
            }

            var validationContext = new StrictValidation();
            var parser = new PipeParser
            {
                ValidationContext = validationContext,
            };

            var normalizedMessage = input.Hl7v2Message
                .Replace("\r\n", "\r")
                .Replace("\n", "\r");

            var parsed = parser.Parse(normalizedMessage);

            return new Result
            {
                Success = true,
                IsValid = true,
                ValidationErrors = Array.Empty<string>(),
            };
        }
        catch (NHapi.Base.HL7Exception ex)
        {
            return new Result
            {
                Success = true,
                IsValid = false,
                ValidationErrors = [ex.Message],
            };
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }
}
