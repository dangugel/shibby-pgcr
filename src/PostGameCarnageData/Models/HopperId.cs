using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class HopperId
{
    [XmlAttribute("HopperId")]
    public ulong Value { get; set; }
}
