using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Structs;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public const string LabelGetDesignList = "Glamourer.GetDesignList";

    private readonly FuncProvider<DesignListEntry[]> _getDesignListProvider;

    public static FuncSubscriber<DesignListEntry[]> GetDesignListSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelGetDesignList);

    public DesignListEntry[] GetDesignList()
        => _designManager.Designs.Select(x => new DesignListEntry(x)).ToArray();

    public record class DesignListEntry
    {
        public string Name;
        public Guid Identifier;

        public DesignListEntry(Design design)
        {
            Name = design.Name;
            Identifier = design.Identifier;
        }
    }
}
