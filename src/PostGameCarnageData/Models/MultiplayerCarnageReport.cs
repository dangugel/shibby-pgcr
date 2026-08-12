using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

/// <summary>
/// Root Multiplayer Carnage Report object.
/// </summary>
[XmlRoot("MultiplayerCarnageReport")]
public sealed class MultiplayerCarnageReport
{
    [XmlElement("GameEnum")]
    public GameEnum GameEnum { get; set; } = new();

    [XmlElement("IsMatchmaking")]
    public IsMatchmaking IsMatchmaking { get; set; } = new();

    /// <summary>
    /// Custom Key populated by the Halo3FilmReader
    /// </summary>
    public string? Map { get; set; }

    [XmlElement("mHasNetworkMembersInParty")]
    public HasNetworkMembersInParty HasNetworkMembersInParty { get; set; } = new();

    [XmlElement("mPartySize")]
    public PartySize PartySize { get; set; } = new();

    [XmlElement("mLastMatchIncomplete")]
    public LastMatchIncomplete LastMatchIncomplete { get; set; } = new();

    [XmlElement("IsTeamsEnabled")]
    public IsTeamsEnabled IsTeamsEnabled { get; set; } = new();

    [XmlElement("HopperId")]
    public HopperId HopperId { get; set; } = new();

    [XmlElement("HopperName")]
    public HopperName HopperName { get; set; } = new();

    [XmlElement("GameTypeName")]
    public GameTypeName GameTypeName { get; set; } = new();

    [XmlElement("GameUniqueId")]
    public GameUniqueId GameUniqueId { get; set; } = new();

    [XmlArray("Players")]
    [XmlArrayItem("Player")]
    public List<Player> Players { get; set; } = [];
}
