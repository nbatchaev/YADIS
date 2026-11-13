using MessagePack;

namespace YADIS.OhioT1DM.Models.Events;

[MessagePackObject]
public struct MealEvent
{
    [Key(0)] public long TimestampTicks { get; set; }
    [Key(1)] public double Carbs { get; set; }
    [Key(2)] public string Type { get; set; }
}