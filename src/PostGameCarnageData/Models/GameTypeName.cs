using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class GameTypeName
{
    [XmlAttribute("GameTypeName")]
    public string Value { get; set; } = "";
}
