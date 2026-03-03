using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.Hl7v2.ConvertToXml.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// The input string containing a message in Hl7v2 format.
    /// </summary>
    /// <example>MSH|^~\&amp;|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20060228155525||ADT^A01|123|P|2.3</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string Hl7v2Message { get; set; } = string.Empty;
}
