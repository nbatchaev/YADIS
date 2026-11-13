using MessagePack;

namespace YADIS.OhioT1DM.Models.Events;

[MessagePackObject]
public struct BasisGenericEvent
{
    [Key(0)] public long TimestampTicks { get; set; }
    [Key(1)] public double Value { get; set; }
}