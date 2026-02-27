using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.Hl7v2.CreateFromXml.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// The input string containing a message in Xml format.
    /// </summary>
    /// <example>
    /// &lt;QRY_R02 xmlns="urn:hl7-org:v2xml"&gt;
    /// &lt;MSH&gt;
    /// &lt;/MSH&gt;
    /// &lt;/QRY_R02&gt;
    /// </example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string Xml { get; set; } = string.Empty;
}
