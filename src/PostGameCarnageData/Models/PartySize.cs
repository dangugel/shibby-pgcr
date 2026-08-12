using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class PartySize
{
    [XmlAttribute("mPartySize")]
    public int Value { get; set; }
}
