using System;
using System.Runtime.ExceptionServices;
using Frends.Hl7v2.CreateFromXml.Definitions;

namespace Frends.Hl7v2.CreateFromXml.Helpers;

internal static class ErrorHandler
{
    internal static Result Handle(this Exception exception, Options options, bool throwCanceled = true)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(options);

        ThrowIfCanceled(exception, throwCanceled);
        if (options.ThrowErrorOnFailure) ThrowBaseException(exception, options.ErrorMessageOnFailure);

        return ReturnResult(exception, options.ErrorMessageOnFailure);
    }

    private static void ThrowIfCanceled(Exception exception, bool throwCanceled = true)
    {
        if (throwCanceled && exception is OperationCanceledException) ThrowBaseException(exception);
    }

    private static void ThrowBaseException(Exception exception, string customMessage = null)
    {
        if (string.IsNullOrEmpty(customMessage))
            ExceptionDispatchInfo.Capture(exception).Throw();

        throw new Exception(customMessage, exception);
    }

    private static Result ReturnResult(Exception exception, string customMessage = null)
    {
        var errorMessage = string.IsNullOrEmpty(customMessage)
            ? exception.Message
            : $"{customMessage}: {exception.Message}";

        return new Result
        {
            Success = false,
            Error = new Error
            {
                Message = errorMessage,
                AdditionalInfo = exception,
            },
        };
    }
}
