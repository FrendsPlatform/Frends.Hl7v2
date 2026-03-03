using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.Hl7v2.CreateFromXml.Definitions;

/// <summary>
/// Additional parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// Define the line ending of the output.
    /// </summary>
    /// <example>LineEnding.LF</example>
    public LineEnding LineEnding { get; set; } = LineEnding.LF;

    /// <summary>
    /// Whether to throw an error on failure.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>
    /// Overrides the error message on failure.
    /// </summary>
    /// <example>Custom error message</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; } = string.Empty;
}
