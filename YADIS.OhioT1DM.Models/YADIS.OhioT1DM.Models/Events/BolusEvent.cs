using MessagePack;

namespace YADIS.OhioT1DM.Models.Events;

[MessagePackObject]
public struct BolusEvent
{
    [Key(0)] public long BeginTicks { get; set; }
    [Key(1)] public long EndTicks { get; set; }
    [Key(2)] public double Dose { get; set; }
    [Key(3)] public string Type { get; set; }
    [Key(4)] public double BwzCarbInput { get; set; }
}