using System.Text;

namespace Frends.Hl7v2.CreateFromXml.Tests;

internal sealed class SyntheticHl7Message
{
    private SyntheticHl7Message(string xml, string expectedEr7)
    {
        Xml = xml;
        ExpectedEr7 = expectedEr7;
    }

    internal string Xml { get; }

    internal string ExpectedEr7 { get; }

    internal static SyntheticHl7Message Create(
        int orders,
        int observationsPerOrder,
        int fieldRepetitions = 1,
        int valueLength = 0)
    {
        var xml = new StringBuilder(
            "<?xml version=\"1.0\"?><ORU_R01 xmlns=\"urn:hl7-org:v2xml\">" +
            "<MSH><MSH.1>|</MSH.1><MSH.2>^~\\&amp;</MSH.2>" +
            "<MSH.3><HD.1>BENCH</HD.1></MSH.3><MSH.5><HD.1>RECEIVER</HD.1></MSH.5>" +
            "<MSH.7><TS.1>20200101120000</TS.1></MSH.7>" +
            "<MSH.9><MSG.1>ORU</MSG.1><MSG.2>R01</MSG.2><MSG.3>ORU_R01</MSG.3></MSH.9>" +
            "<MSH.10>BENCH-1</MSH.10><MSH.11><PT.1>P</PT.1></MSH.11>" +
            "<MSH.12><VID.1>2.5</VID.1></MSH.12></MSH><ORU_R01.PATIENT_RESULT>");
        var expected = new StringBuilder(
            "MSH|^~\\&|BENCH||RECEIVER||20200101120000||ORU^R01^ORU_R01|BENCH-1|P|2.5\n");
        var payload = valueLength > 0 ? new string('A', valueLength) : null;
        var sequence = 0;

        for (var order = 1; order <= orders; order++)
        {
            xml.Append("<ORU_R01.ORDER_OBSERVATION><OBR><OBR.1>")
                .Append(order).Append("</OBR.1></OBR>");
            expected.Append("OBR|").Append(order).Append('\n');

            for (var observation = 0; observation < observationsPerOrder; observation++)
            {
                sequence++;
                xml.Append("<ORU_R01.OBSERVATION><OBX><OBX.1>")
                    .Append(sequence).Append("</OBX.1><OBX.2>ST</OBX.2>")
                    .Append("<OBX.3><CE.1>TEST</CE.1></OBX.3>");
                expected.Append("OBX|").Append(sequence).Append("|ST|TEST||");

                for (var repetition = 1; repetition <= fieldRepetitions; repetition++)
                {
                    var value = payload ?? $"value-{sequence}-{repetition}";
                    xml.Append("<OBX.5>").Append(value).Append("</OBX.5>");
                    if (repetition > 1)
                        expected.Append('~');
                    expected.Append(value);
                }

                xml.Append("<OBX.11>F</OBX.11></OBX></ORU_R01.OBSERVATION>");
                expected.Append("||||||F\n");
            }

            xml.Append("</ORU_R01.ORDER_OBSERVATION>");
        }

        xml.Append("</ORU_R01.PATIENT_RESULT></ORU_R01>");
        return new SyntheticHl7Message(xml.ToString(), expected.ToString());
    }
}
