using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using MessagePack;
using YADIS.OhioT1DM.Models;
using YADIS.OhioT1DM.Models.Events;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace YADIS.OhioT1DM.Parser.Logic;

public class OhioT1DmDatasetParser
{
    private const string TimeFormat = "dd-MM-yyyy HH:mm:ss";
    private const double MgDlToMmolFactor = 18.016;

    private readonly List<(PropertyInfo Property, int ExpectedKey, string ExpectedXmlTag)> _orderedSchema;

    public OhioT1DmDatasetParser()
    {
        var properties = typeof(OhioT1DmPatient).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        _orderedSchema = new List<(PropertyInfo, int, string)>(properties.Length);

        for (var i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];

            var keyAttr = prop.GetCustomAttribute<KeyAttribute>();
            var xmlAttr = prop.GetCustomAttribute<XmlElementAttribute>();

            if (keyAttr?.IntKey == null || xmlAttr == null)
                throw new InvalidOperationException(
                    $"Property {prop.Name} is missing [Key] or [XmlElement] attribute.");

            if (keyAttr.IntKey != i)
                throw new InvalidOperationException(
                    $"Sequence mismatch on property {prop.Name}. Expected Key({i}), found Key({keyAttr.IntKey}).");

            _orderedSchema.Add((prop, keyAttr.IntKey.Value, xmlAttr.ElementName));
        }
    }

    public static void Fix596Document(XDocument doc)
    {
        foreach (var sleepElem in doc.Descendants("basis_sleep").ToList())
        {
            var parent = sleepElem.Parent;

            if (parent == null)
                continue;

            var hasAir = parent.Elements("basis_air_temperature").Any();
            var hasSteps = parent.Elements("basis_steps").Any();

            if (!hasAir)
                sleepElem.AddBeforeSelf(new XElement("basis_air_temperature"));

            if (!hasSteps)
                sleepElem.AddBeforeSelf(new XElement("basis_steps"));

            var air = sleepElem.ElementsBeforeSelf().FirstOrDefault(e => e.Name == "basis_air_temperature");
            var steps = sleepElem.ElementsBeforeSelf().FirstOrDefault(e => e.Name == "basis_steps");

            if (air == null || steps == null)
                continue;

            var beforeSleep = sleepElem.ElementsBeforeSelf().ToList();

            if (beforeSleep.IndexOf(steps) >= beforeSleep.IndexOf(air))
                continue;

            air.Remove();
            steps.AddBeforeSelf(air);
        }
    }

    public OhioT1DmPatient Parse(XDocument doc)
    {
        if (doc.Root == null)
            throw new ArgumentException("Root XML element is missing.");

        var xmlElements = doc.Root.Elements().ToList();

        if (xmlElements.Count is not (18 or 19)) // 2018 = 18; 2020 = 19 (added: acceleration)
            throw new InvalidDataException(
                $"XML tags count ({xmlElements.Count}) is not 18 or 19.");

        var dataset = new OhioT1DmPatient();

        for (var i = 0; i < xmlElements.Count; i++)
        {
            var (prop, _, expectedXmlTag) = _orderedSchema[i];

            var currentXmlGroup = xmlElements[i];
            var actualXmlTag = currentXmlGroup.Name.LocalName;

            if (actualXmlTag != expectedXmlTag)
                throw new InvalidDataException(
                    $"Sequence mismatch at index {i}. Expected <{expectedXmlTag}>, found <{actualXmlTag}>.");

            var itemType = prop.PropertyType.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(itemType);
            var list = (IList)Activator.CreateInstance(listType)!;

            foreach (var el in currentXmlGroup.Elements())
            {
                if (el.Name.LocalName != "event")
                    throw new InvalidDataException(
                        $"Invalid element <{el.Name.LocalName}> found inside <{actualXmlTag}>. Expected <event>.");

                if (itemType == typeof(GlucoseEvent) || itemType == typeof(FingerStickEvent))
                {
                    EnsureAttributes(el, "ts", "value");

                    var ticks = ParseTs(el, "ts");
                    var val = ParseVal(el, "value");

                    if (itemType == typeof(GlucoseEvent))
                        list.Add(new GlucoseEvent
                            { TimestampTicks = ticks, ValueMgDl = val, ValueMmol = val / MgDlToMmolFactor });
                    else
                        list.Add(new FingerStickEvent
                            { TimestampTicks = ticks, ValueMgDl = val, ValueMmol = val / MgDlToMmolFactor });
                }
                else if (itemType == typeof(BasalEvent))
                {
                    EnsureAttributes(el, "ts", "value");

                    list.Add(new BasalEvent { TimestampTicks = ParseTs(el, "ts"), Value = ParseVal(el, "value") });
                }
                else if (itemType == typeof(TempBasalEvent))
                {
                    EnsureAttributes(el, "ts_begin", "ts_end", "value");

                    list.Add(new TempBasalEvent
                    {
                        BeginTicks = ParseTs(el, "ts_begin"), EndTicks = ParseTs(el, "ts_end"),
                        Value = ParseVal(el, "value")
                    });
                }
                else if (itemType == typeof(BolusEvent))
                {
                    double bwzCarbInput;

                    if (xmlElements.Count is 18)
                    {
                        EnsureAttributes(el, "ts_begin", "ts_end", "dose", "bwz_carb_input", "type");
                        bwzCarbInput = ParseVal(el, "bwz_carb_input");
                    }
                    else
                    {
                        EnsureAttributes(el, "ts_begin", "ts_end", "dose", "type");
                        bwzCarbInput = 0;
                    }

                    list.Add(new BolusEvent
                    {
                        BeginTicks = ParseTs(el, "ts_begin"), EndTicks = ParseTs(el, "ts_end"),
                        Dose = ParseVal(el, "dose"), BwzCarbInput = bwzCarbInput,
                        Type = el.Attribute("type")!.Value
                    });
                }
                else if (itemType == typeof(MealEvent))
                {
                    EnsureAttributes(el, "ts", "carbs", "type");

                    list.Add(new MealEvent
                    {
                        TimestampTicks = ParseTs(el, "ts"), Carbs = ParseVal(el, "carbs"),
                        Type = el.Attribute("type")!.Value
                    });
                }
                else if (itemType == typeof(WorkEvent))
                {
                    EnsureAttributes(el, "ts_begin", "ts_end", "intensity");

                    list.Add(new WorkEvent
                    {
                        BeginTicks = ParseTs(el, "ts_begin"), EndTicks = ParseTs(el, "ts_end"),
                        Intensity = (int)ParseVal(el, "intensity")
                    });
                }
                else if (itemType == typeof(ExerciseEvent))
                {
                    EnsureAttributes(el, "ts", "duration", "intensity");

                    list.Add(new ExerciseEvent
                    {
                        TimestampTicks = ParseTs(el, "ts"), DurationMinutes = (int)ParseVal(el, "duration"),
                        Intensity = (int)ParseVal(el, "intensity"), Type = el.Attribute("type")?.Value ?? string.Empty
                    });
                }
                else if (itemType == typeof(SleepEvent))
                {
                    EnsureAttributes(el, "ts_begin", "ts_end", "quality");

                    list.Add(new SleepEvent
                    {
                        BeginTicks = ParseTs(el, "ts_begin"), EndTicks = ParseTs(el, "ts_end"),
                        Quality = (int)ParseVal(el, "quality")
                    });
                }
                else if (itemType == typeof(BasisSleepEvent))
                {
                    EnsureAttributes(el, "tbegin", "tend", "quality");

                    list.Add(new BasisSleepEvent
                    {
                        BeginTicks = ParseTs(el, "tbegin"), EndTicks = ParseTs(el, "tend"),
                        Quality = (int)ParseVal(el, "quality"), Type = el.Attribute("type")?.Value ?? string.Empty
                    });
                }
                else if (itemType == typeof(BasisGenericEvent))
                {
                    EnsureAttributes(el, "ts", "value");

                    list.Add(new BasisGenericEvent
                        { TimestampTicks = ParseTs(el, "ts"), Value = ParseVal(el, "value") });
                }
                else if (itemType == typeof(SimpleEvent))
                {
                    EnsureAttributes(el, "ts");

                    list.Add(new SimpleEvent { TimestampTicks = ParseTs(el, "ts") });
                }
                else if (itemType == typeof(IllnessEvent))
                {
                    EnsureAttributes(el, "ts_begin", "description");

                    var eStr = el.Attribute("ts_end")?.Value;
                    var s = ParseTs(el, "ts_begin");

                    list.Add(new IllnessEvent
                    {
                        BeginTicks = s, EndTicks = string.IsNullOrWhiteSpace(eStr) ? s : ParseTsStr(eStr),
                        Description = el.Attribute("description")?.Value ?? string.Empty
                    });
                }
            }

            prop.SetValue(dataset, list);
        }

        return dataset;
    }

    private static void EnsureAttributes(XElement el, params string[] attributes)
    {
        foreach (var attr in attributes)
            if (el.Attribute(attr) == null)
                throw new XmlException(
                    $"Missing required attribute '{attr}' in <event> element within <{el.Parent?.Name.LocalName}>.");
    }

    private static long ParseTs(XElement el, string attr)
    {
        return ParseTsStr(el.Attribute(attr)?.Value ?? string.Empty);
    }

    private static long ParseTsStr(string val)
    {
        return DateTime.TryParseExact(val, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt.Ticks
            : throw new FormatException($"Invalid date format: '{val}'. Expected: {TimeFormat}");
    }

    private static double ParseVal(XElement el, string attr)
    {
        var val = el.Attribute(attr)?.Value;

        return double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? d
            : throw new FormatException($"Cannot parse numeric value in attribute '{attr}'='{val}'.");
    }
}