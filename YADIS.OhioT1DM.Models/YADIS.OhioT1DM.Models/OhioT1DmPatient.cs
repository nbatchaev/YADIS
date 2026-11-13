using System.Xml.Serialization;
using MessagePack;
using YADIS.OhioT1DM.Models.Events;

namespace YADIS.OhioT1DM.Models;

[MessagePackObject]
public class OhioT1DmPatient
{
    [Key(0)] [XmlElement("glucose_level")] public List<GlucoseEvent>? GlucoseLevel { get; set; }

    [Key(1)] [XmlElement("finger_stick")] public List<FingerStickEvent>? FingerSticks { get; set; }

    [Key(2)] [XmlElement("basal")] public List<BasalEvent>? BasalRates { get; set; }

    [Key(3)] [XmlElement("temp_basal")] public List<TempBasalEvent>? TempBasals { get; set; }

    [Key(4)] [XmlElement("bolus")] public List<BolusEvent>? Boluses { get; set; }

    [Key(5)] [XmlElement("meal")] public List<MealEvent>? Meals { get; set; }

    [Key(6)] [XmlElement("sleep")] public List<SleepEvent>? SleepIntervals { get; set; }

    [Key(7)] [XmlElement("work")] public List<WorkEvent>? WorkIntervals { get; set; }

    [Key(8)] [XmlElement("stressors")] public List<SimpleEvent>? Stressors { get; set; }

    [Key(9)] [XmlElement("hypo_event")] public List<SimpleEvent>? HypoEvents { get; set; }

    [Key(10)] [XmlElement("illness")] public List<IllnessEvent>? Illnesses { get; set; }

    [Key(11)] [XmlElement("exercise")] public List<ExerciseEvent>? ExerciseIntervals { get; set; }

    [Key(12)]
    [XmlElement("basis_heart_rate")]
    public List<BasisGenericEvent>? BasisHeartRate { get; set; }

    [Key(13)] [XmlElement("basis_gsr")] public List<BasisGenericEvent>? BasisGsr { get; set; }

    [Key(14)]
    [XmlElement("basis_skin_temperature")]
    public List<BasisGenericEvent>? BasisSkinTemperature { get; set; }

    [Key(15)]
    [XmlElement("basis_air_temperature")]
    public List<BasisGenericEvent>? BasisAirTemperature { get; set; }

    [Key(16)] [XmlElement("basis_steps")] public List<BasisGenericEvent>? BasisSteps { get; set; }

    [Key(17)] [XmlElement("basis_sleep")] public List<BasisSleepEvent>? BasisSleepIntervals { get; set; }
    
    [Key(18)] [XmlElement("acceleration")] public List<BasisGenericEvent>? BasisAcceleration { get; set; }
}