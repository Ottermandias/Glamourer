using Dalamud.Plugin;
using Glamourer.Api.Api;
using Glamourer.Api.Helpers;
using OtterGui.Services;
using Glamourer.Api.Enums;

namespace Glamourer.Api;

public sealed class IpcProviders : IDisposable, IApiService
{
    private readonly List<IDisposable> _providers;

    private readonly EventProvider _disposedProvider;
    private readonly EventProvider _initializedProvider;

    public IpcProviders(IDalamudPluginInterface pi, IGlamourerApi api)
    {
        _disposedProvider    = IpcSubscribers.Disposed.Provider(pi);
        _initializedProvider = IpcSubscribers.Initialized.Provider(pi);
        _providers =
        [
            new FuncProvider<(int Major, int Minor)>(pi, "Glamourer.ApiVersions", () => api.ApiVersion), // backward compatibility
            new FuncProvider<int>(pi, "Glamourer.ApiVersion", () => api.ApiVersion.Major),               // backward compatibility
            IpcSubscribers.ApiVersion.Provider(pi, api),

            IpcSubscribers.GetDesignList.Provider(pi, api.Designs),
            IpcSubscribers.ApplyDesign.Provider(pi, api.Designs),
            IpcSubscribers.ApplyDesignName.Provider(pi, api.Designs),

            IpcSubscribers.SetItem.Provider(pi, api.Items),
            IpcSubscribers.SetItemName.Provider(pi, api.Items),
            // backward compatibility
            new FuncProvider<int, byte, ulong, byte, uint, ulong, int>(pi, IpcSubscribers.Legacy.SetItemV2.Label,
                (a, b, c, d, e, f) => (int)api.Items.SetItem(a, (ApiEquipSlot)b, c, [d], e, (ApplyFlag)f)),
            new FuncProvider<string, byte, ulong, byte, uint, ulong, int>(pi, IpcSubscribers.Legacy.SetItemName.Label,
                (a, b, c, d, e, f) => (int)api.Items.SetItemName(a, (ApiEquipSlot)b, c, [d], e, (ApplyFlag)f)),
            IpcSubscribers.SetBonusItem.Provider(pi, api.Items),
            IpcSubscribers.SetBonusItemName.Provider(pi, api.Items),
            IpcSubscribers.GetState.Provider(pi, api.State),
            IpcSubscribers.GetStateName.Provider(pi, api.State),
            IpcSubscribers.GetStateBase64.Provider(pi, api.State),
            IpcSubscribers.GetStateBase64Name.Provider(pi, api.State),
            IpcSubscribers.ApplyState.Provider(pi, api.State),
            IpcSubscribers.ApplyStateName.Provider(pi, api.State),
            IpcSubscribers.RevertState.Provider(pi, api.State),
            IpcSubscribers.RevertStateName.Provider(pi, api.State),
            IpcSubscribers.UnlockState.Provider(pi, api.State),
            IpcSubscribers.UnlockStateName.Provider(pi, api.State),
            IpcSubscribers.UnlockAll.Provider(pi, api.State),
            IpcSubscribers.RevertToAutomation.Provider(pi, api.State),
            IpcSubscribers.RevertToAutomationName.Provider(pi, api.State),
            IpcSubscribers.StateChanged.Provider(pi, api.State),
            IpcSubscribers.StateChangedWithType.Provider(pi, api.State),
            IpcSubscribers.StateFinalized.Provider(pi, api.State),
            IpcSubscribers.GPoseChanged.Provider(pi, api.State),
        ];
        _initializedProvider.Invoke();
    }

    public void Dispose()
    {
        foreach (var provider in _providers)
            provider.Dispose();
        _providers.Clear();
        _initializedProvider.Dispose();
        _disposedProvider.Invoke();
        _disposedProvider.Dispose();
    }
}
