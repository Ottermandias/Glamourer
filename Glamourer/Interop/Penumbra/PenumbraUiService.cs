using Dalamud.Plugin;
using Dalamud.Plugin.Ipc.Exceptions;
using Luna;
using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;

namespace Glamourer.Interop.Penumbra;

public sealed class PenumbraUiService(IDalamudPluginInterface pluginInterface) : IDisposable
{
    private readonly EventSubscriber<ChangedItemType, uint>              _tooltipSubscriber = ChangedItemTooltip.Subscriber(pluginInterface);
    private readonly EventSubscriber<MouseButton, ChangedItemType, uint> _clickSubscriber   = ChangedItemClicked.Subscriber(pluginInterface);

    private readonly EventSubscriber<string, string, Dictionary<Assembly, (bool, string)>> _modUsageSubscriber =
        global::Penumbra.Api.IpcSubscribers.ModUsageQueried.Subscriber(pluginInterface);

    private RegisterSettingsSection?   _registerSettingsSection;
    private UnregisterSettingsSection? _unregisterSettingsSection;
    private OpenMainWindow?            _openModPage;

    public void OpenMod(ModIdentifier mod)
        => _openModPage?.Invoke(TabType.Mods, mod.Identifier, mod.Name);

    public event Action<MouseButton, ChangedItemType, uint> Click
    {
        add => _clickSubscriber.Event += value;
        remove => _clickSubscriber.Event -= value;
    }

    public event Action<ChangedItemType, uint> Tooltip
    {
        add => _tooltipSubscriber.Event += value;
        remove => _tooltipSubscriber.Event -= value;
    }

    public event Action<string, string, Dictionary<Assembly, (bool, string)>> ModUsageQueried
    {
        add => _modUsageSubscriber.Event += value;
        remove => _modUsageSubscriber.Event -= value;
    }

    public event Action? DrawSettingsSection;

    public void Dispose()
    {
        Detach();
        _tooltipSubscriber.Dispose();
        _clickSubscriber.Dispose();
        _modUsageSubscriber.Dispose();
    }

    private void DrawSettings()
        => DrawSettingsSection?.Invoke();

    public void Attach()
    {
        _registerSettingsSection   = new RegisterSettingsSection(pluginInterface);
        _unregisterSettingsSection = new UnregisterSettingsSection(pluginInterface);
        _openModPage               = new OpenMainWindow(pluginInterface);
        _registerSettingsSection.Invoke(DrawSettings);
    }

    public void Detach()
    {
        try
        {
            _unregisterSettingsSection?.Invoke(DrawSettings);
        }
        catch (IpcNotReadyError)
        {
            // Ignore.
        }

        _registerSettingsSection   = null;
        _unregisterSettingsSection = null;
        _openModPage               = null;
    }
}
