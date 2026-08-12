using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class IsTeamsEnabled
{
    [XmlAttribute("IsTeamsEnabled")]
    public bool Value { get; set; }
}
