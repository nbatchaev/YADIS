using MessagePack;

namespace YADIS.OhioT1DM.Models.Events;

[MessagePackObject]
public struct ExerciseEvent
{
    [Key(0)] public long TimestampTicks { get; set; }
    [Key(1)] public int DurationMinutes { get; set; }
    [Key(2)] public int Intensity { get; set; }
    [Key(3)] public string Type { get; set; }
}