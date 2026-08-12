using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class Medal
{
    [XmlAttribute("mId")]
    public int Id { get; set; }

    [XmlAttribute("mCount")]
    public int Count { get; set; }
}
