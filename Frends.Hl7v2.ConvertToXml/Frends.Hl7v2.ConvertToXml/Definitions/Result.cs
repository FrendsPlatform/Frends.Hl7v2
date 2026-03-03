namespace Frends.Hl7v2.ConvertToXml.Definitions;

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
    /// Message in Xml format.
    /// </summary>
    /// <example>
    /// &lt;QRY_R02 xmlns="urn:hl7-org:v2xml"&gt;
    /// &lt;MSH&gt;
    /// &lt;/MSH&gt;
    /// &lt;/QRY_R02&gt;
    /// </example>
    public string Xml { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}
