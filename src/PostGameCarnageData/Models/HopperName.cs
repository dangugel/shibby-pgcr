using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class HopperName
{
    [XmlAttribute("HopperName")]
    public string Value { get; set; } = "";
}
