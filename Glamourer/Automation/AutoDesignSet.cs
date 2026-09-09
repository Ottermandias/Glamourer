using System.Text.Json;
using Luna;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Actors;

namespace Glamourer.Automation;

public sealed class AutoDesignSet(string name, ActorIdentifier[] identifiers, List<AutoDesign> designs)
{
    public readonly List<AutoDesign> Designs = designs;

    public string            Name        = name;
    public ActorIdentifier[] Identifiers = identifiers;
    public bool              Enabled;
    public Base              BaseState              = Base.Current;
    public bool              ResetTemporarySettings = false;

    public readonly List<ActorIdentifier[]> SecondaryIdentifiers = [];
    public          int                     Priority;

    public void Serialize(Utf8JsonWriter j)
    {
        j.WriteStartObject();
        j.WriteNonEmptyString("Name"u8, Name);
        j.WriteJson("Identifier"u8, Identifiers[0]);
        j.WriteIfNot("Enabled"u8,  Enabled,  false);
        j.WriteIfNot("Priority"u8, Priority, 0);
        j.WriteEnumIfNot("BaseState"u8, BaseState, Base.Current);
        j.WriteIfNot("ResetTemporarySettings"u8, ResetTemporarySettings, false);
        if (Designs.Count > 0)
        {
            j.WriteStartArray("Designs"u8);
            foreach (var design in Designs)
                design.Serialize(j);
            j.WriteEndArray();
        }

        if (SecondaryIdentifiers.Count > 0)
        {
            j.WriteStartArray("SecondaryIdentifiers"u8);
            foreach (var identifier in SecondaryIdentifiers)
            {
                j.WriteStartObject();
                j.WriteJsonProperties(identifier[0]);
                j.WriteEndObject();
            }

            j.WriteEndArray();
        }

        j.WriteEndObject();
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
