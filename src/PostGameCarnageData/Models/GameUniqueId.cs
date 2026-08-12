using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class GameUniqueId
{
    [XmlAttribute("GameUniqueId")]
    public string Value { get; set; } = "";
}
