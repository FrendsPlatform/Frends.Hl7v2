using System;

namespace Frends.Hl7v2.Validate.Definitions;

/// <summary>
/// Result of the task.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates if the task completed successfully.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// Indicates if the HL7v2 message is valid.
    /// </summary>
    /// <example>true</example>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors found in the message. Empty if the message is valid.
    /// </summary>
    /// <example>[ "MSH-9 is missing", "PID-3 is required" ]</example>
    public string[] ValidationErrors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}
