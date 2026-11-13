using MessagePack;

namespace YADIS.OhioT1DM.Models.Events;

[MessagePackObject]
public struct GlucoseEvent
{
    [Key(0)] public long TimestampTicks { get; set; }
    [Key(1)] public double ValueMgDl { get; set; }
    [Key(2)] public double ValueMmol { get; set; }
}