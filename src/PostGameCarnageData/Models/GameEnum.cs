using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class GameEnum
{
    [XmlAttribute("mGameEnum")]
    public int Value { get; set; }
}
