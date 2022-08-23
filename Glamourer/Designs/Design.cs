using System;
using System.Runtime.InteropServices;
using Glamourer.State;
using Glamourer.Structs;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public class Design
{
    public string Name { get; }
    public bool   ReadOnly;

    public DateTimeOffset CreationDate   { get; }
    public DateTimeOffset LastUpdateDate { get; }
    public CharacterSave  Data           { get; }

    public override string ToString()
        => Name;
}

public struct ArmorData
{
    public CharacterArmor Model;
    public bool           Ignore;
}
