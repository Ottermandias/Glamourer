using Dalamud.Plugin;
using Penumbra.Api.Helpers;

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
