using Glamourer.Designs;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignSelection : IUiService, IDisposable
{
    public Design? Design { get; private set; }

    public void Dispose()
    { }
}
