using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Actors;

namespace Glamourer.Automation;

public class AutoDesignSet
{
    public readonly List<AutoDesign> Designs;

    public string          Name;
    public ActorIdentifier Identifier;
    public bool            Enabled;

    public JObject Serialize()
    {
        var list = new JArray();
        foreach (var design in Designs)
            list.Add(design.Serialize());

        return new JObject()
        {
            ["Name"]       = Name,
            ["Identifier"] = Identifier.ToJson(),
            ["Enabled"]    = Enabled,
            ["Designs"]    = list,
        };
    }

    public AutoDesignSet(string name, ActorIdentifier identifier)
        : this(name, identifier, new List<AutoDesign>())
    { }

    public AutoDesignSet(string name, ActorIdentifier identifier, List<AutoDesign> designs)
    {
        Name       = name;
        Identifier = identifier;
        Designs    = designs;
    }
}
