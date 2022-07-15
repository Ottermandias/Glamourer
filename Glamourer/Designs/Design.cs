using System;

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
