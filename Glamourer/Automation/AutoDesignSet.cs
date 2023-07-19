using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Glamourer.Designs;
using Glamourer.State;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Actors;

namespace Glamourer.Automation;

public class AutoDesignSet
{
    public readonly List<AutoDesign> Designs;

    public string            Name;
    public ActorIdentifier[] Identifiers;
    public bool              Enabled;
    public Base              BaseState = Base.Current;

    public JObject Serialize()
    {
        var list = new JArray();
        foreach (var design in Designs)
            list.Add(design.Serialize());

        return new JObject()
        {
            ["Name"]       = Name,
            ["Identifier"] = Identifiers[0].ToJson(),
            ["Enabled"]    = Enabled,
            ["BaseState"]  = BaseState.ToString(),
            ["Designs"]    = list,
        };
    }

    public AutoDesignSet(string name, params ActorIdentifier[] identifiers)
        : this(name, identifiers, new List<AutoDesign>())
    { }

    public AutoDesignSet(string name, ActorIdentifier[] identifiers, List<AutoDesign> designs)
    {
        Name        = name;
        Identifiers = identifiers;
        Designs     = designs;
    }

    public enum Base : byte
    {
        Current,
        Game,
    }
}
