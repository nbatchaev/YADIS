using MessagePack;

namespace YADIS.OhioT1DM.Models.Events;

[MessagePackObject]
public struct BasisSleepEvent
{
    [Key(0)] public long BeginTicks { get; set; }
    [Key(1)] public long EndTicks { get; set; }
    [Key(2)] public int Quality { get; set; }
    [Key(3)] public string Type { get; set; }
}