using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class Player
{
    [XmlAttribute("mXboxUserId")]
    public string XboxUserId { get; set; } = "";

    [XmlAttribute("isGuest")]
    public bool IsGuest { get; set; }

    [XmlAttribute("mGameMode")]
    public int GameMode { get; set; }

    [XmlAttribute("mGamertagText")]
    public string Gamertag { get; set; } = "";

    [XmlAttribute("ClantagText")]
    public string ClanTag { get; set; } = "";

    [XmlAttribute("EmblemTexture0")]
    public int EmblemTexture0 { get; set; }

    [XmlAttribute("EmblemTexture1")]
    public int EmblemTexture1 { get; set; }

    [XmlAttribute("EmblemColor0")]
    public int EmblemColor0 { get; set; }

    [XmlAttribute("EmblemColor1")]
    public int EmblemColor1 { get; set; }

    [XmlAttribute("EmblemColor2")]
    public int EmblemColor2 { get; set; }

    [XmlAttribute("Nameplate")]
    public int Nameplate { get; set; }

    [XmlAttribute("Avatar")]
    public int Avatar { get; set; }

    [XmlAttribute("ServiceId")]
    public string ServiceId { get; set; }

    [XmlAttribute("mTeamId")]
    public int TeamId { get; set; }

    [XmlAttribute("Score")]
    public int Score { get; set; }

    [XmlAttribute("mStanding")]
    public int Standing { get; set; }

    [XmlAttribute("mTotalMedalCount")]
    public int TotalMedalCount { get; set; }

    [XmlAttribute("mKills")]
    public int Kills { get; set; }

    [XmlAttribute("mDeaths")]
    public int Deaths { get; set; }

    [XmlAttribute("mAssists")]
    public int Assists { get; set; }

    [XmlAttribute("mBetrayals")]
    public int Betrayals { get; set; }

    [XmlAttribute("mSuicides")]
    public int Suicides { get; set; }

    [XmlAttribute("mMostKillsInARow")]
    public int MostKillsInARow { get; set; }

    [XmlAttribute("mSecondsAlive")]
    public int SecondsAlive { get; set; }

    [XmlAttribute("mKillsWeapon")]
    public int KillsWeapon { get; set; }

    [XmlAttribute("mKillsGrenade")]
    public int KillsGrenade { get; set; }

    [XmlAttribute("mKillsMelee")]
    public int KillsMelee { get; set; }

    [XmlAttribute("mKillsOther")]
    public int KillsOther { get; set; }

    [XmlAttribute("mCompletedGame")]
    public int CompletedGame { get; set; }

    [XmlAttribute("mSecondsPlayed")]
    public int SecondsPlayed { get; set; }

    [XmlAttribute("mKilledMostPlayerIndex")]
    public int KilledMostPlayerIndex { get; set; }

    [XmlAttribute("mKilledMostPlayerCount")]
    public int KilledMostPlayerCount { get; set; }

    [XmlAttribute("mMostKilledByPlayerIndex")]
    public int MostKilledByPlayerIndex { get; set; }

    [XmlAttribute("mMostKilledByPlayerCount")]
    public int MostKilledByPlayerCount { get; set; }

    [XmlAttribute("mMostUsedWeapon")]
    public int MostUsedWeapon { get; set; }

    [XmlAttribute("mMostUsedWeaponCount")]
    public int MostUsedWeaponCount { get; set; }

    [XmlArray("CustomStats")]
    [XmlArrayItem("CustomStat")]
    public List<CustomStat> CustomStats { get; set; } = [];

    [XmlArray("MedalsCount")]
    [XmlArrayItem("Medal")]
    public List<Medal> Medals { get; set; } = [];
}
