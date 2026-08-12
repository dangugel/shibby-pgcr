using System.Xml.Serialization;

namespace PostGameCarnageData.Models;

public sealed class LastMatchIncomplete
{
    [XmlAttribute("mLastMatchIncomplete")]
    public bool Value { get; set; }
}
