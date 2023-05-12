using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using Glamourer.Designs;
using Glamourer.Interop;
using Glamourer.Structs;
using Penumbra.GameData.Actors;

namespace Glamourer.State;

public class FixedDesignManager
{
    public class FixedDesign
    {
        public Design  Design;
        public byte?   JobCondition;
        public ushort? TerritoryCondition;

        public bool Applies(byte job, ushort territoryType)
            => (!JobCondition.HasValue || JobCondition.Value == job)
        && (!TerritoryCondition.HasValue || TerritoryCondition.Value == territoryType);
    }

    public IReadOnlyList<FixedDesign> GetDesigns(ActorIdentifier actor)
    {
        return Array.Empty<FixedDesign>();
    }
}
