using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class HasNetworkMembersInParty
{
    [XmlAttribute("mHasNetworkMembersInParty")]
    public bool Value { get; set; }
}
