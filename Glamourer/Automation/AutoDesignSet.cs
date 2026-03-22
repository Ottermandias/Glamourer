using Newtonsoft.Json.Linq;
using Penumbra.GameData.Actors;

namespace Glamourer.Automation;

public class AutoDesignSet(string name, ActorIdentifier[] identifiers, List<AutoDesign> designs)
{
    public readonly List<AutoDesign> Designs = designs;

    public string            Name        = name;
    public ActorIdentifier[] Identifiers = identifiers;
    public bool              Enabled;
    public Base              BaseState              = Base.Current;
    public bool              ResetTemporarySettings = false;

    public readonly List<ActorIdentifier[]> SecondaryIdentifiers = [];
    public          int                     Priority;

    public JObject Serialize()
    {
        var list = new JArray();
        foreach (var design in Designs)
            list.Add(design.Serialize());


        var ret = new JObject
        {
            ["Name"]                   = Name,
            ["Identifier"]             = Identifiers[0].ToJson(),
            ["Enabled"]                = Enabled,
            ["Priority"]               = Priority,
            ["BaseState"]              = BaseState.ToString(),
            ["ResetTemporarySettings"] = ResetTemporarySettings.ToString(),
            ["Designs"]                = list,
        };

        if (SecondaryIdentifiers.Count > 0)
        {
            var array = new JArray();
            foreach(var identifier in SecondaryIdentifiers)
                array.Add(identifier[0].ToJson());
            ret["SecondaryIdentifiers"] = array;
        }

        return ret;
    }

    public AutoDesignSet(string name, params ActorIdentifier[] identifiers)
        : this(name, identifiers, [])
    { }

    public enum Base : byte
    {
        Current,
        Game,
    }
}
