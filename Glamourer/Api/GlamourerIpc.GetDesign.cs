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

    private readonly FuncProvider<(string Name, Guid Identifier)[]> _getDesignListProvider;

    public static FuncSubscriber<(string Name, Guid Identifier)[]> GetDesignListSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelGetDesignList);

    public (string Name, Guid Identifier)[] GetDesignList()
        => _designManager.Designs.Select(x => (x.Name.Text, x.Identifier)).ToArray();
}
