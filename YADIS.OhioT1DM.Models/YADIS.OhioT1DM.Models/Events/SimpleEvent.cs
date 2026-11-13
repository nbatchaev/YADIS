using MessagePack;

namespace YADIS.OhioT1DM.Models.Events;

[MessagePackObject]
public struct SimpleEvent
{
    [Key(0)] public long TimestampTicks { get; set; }
}