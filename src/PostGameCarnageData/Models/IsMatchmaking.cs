using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class IsMatchmaking
{
    [XmlAttribute("IsMatchmaking")]
    public bool Value { get; set; }
}

