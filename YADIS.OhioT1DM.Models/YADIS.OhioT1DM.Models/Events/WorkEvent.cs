using MessagePack;

namespace YADIS.OhioT1DM.Models.Events;

[MessagePackObject]
public struct WorkEvent
{
    [Key(0)] public long BeginTicks { get; set; }
    [Key(1)] public long EndTicks { get; set; }
    [Key(2)] public int Intensity { get; set; }
}