using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Glamourer.Designs;

public partial class Design
{
    public Guid           Identifier   { get; private init; }
    public DateTimeOffset CreationDate { get; private init; }
    public string         Name         { get; private set; } = string.Empty;
    public string         Description  { get; private set; } = string.Empty;
    public string[]       Tags         { get; private set; } = Array.Empty<string>();
    public int            Index        { get; private set; }

    public JObject JsonSerialize()
    {
        var ret = new JObject
        {
            [nameof(Identifier)]   = Identifier,
            [nameof(CreationDate)] = CreationDate,
            [nameof(Name)]         = Name,
            [nameof(Description)]  = Description,
            [nameof(Tags)]         = JArray.FromObject(Tags),
        };
        return ret;
    }

    public static Design LoadDesign(JObject json)
        => new()
        {
            CreationDate = json[nameof(CreationDate)]?.ToObject<DateTimeOffset>() ?? throw new ArgumentNullException(nameof(CreationDate)),
            Identifier   = json[nameof(Identifier)]?.ToObject<Guid>() ?? throw new ArgumentNullException(nameof(Identifier)),
            Name         = json[nameof(Name)]?.ToObject<string>() ?? throw new ArgumentNullException(nameof(Name)),
            Description  = json[nameof(Description)]?.ToObject<string>() ?? string.Empty,
            Tags         = ParseTags(json),
        };

    private static string[] ParseTags(JObject json)
    {
        var tags = json[nameof(Tags)]?.ToObject<string[]>() ?? Array.Empty<string>();
        return tags.OrderBy(t => t).Distinct().ToArray();
    }
}
