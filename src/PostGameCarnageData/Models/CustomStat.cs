using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class CustomStat
{
    [XmlAttribute("mStatName")]
    public string StatName { get; set; } = "";

    [XmlAttribute("mValueForDisplay")]
    public string ValueForDisplay { get; set; } = "";
}
