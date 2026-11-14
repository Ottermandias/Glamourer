using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using Luna;
using ProfileData = (System.Guid UniqueId, string Name, string VirtualPath,
    System.Collections.Generic.List<(string Name, ushort WorldId, byte CharacterType, ushort CharacterSubType)> Characters,
    int Priority, bool IsEnabled);

namespace Glamourer.Interop.CustomizePlus;

public sealed class CustomizePlusIpc : IService
{
    public readonly record struct Profile(Guid Id, string Name, string Path, bool Enabled);

    private readonly IDalamudPluginInterface _pi;
    private ICallGateSubscriber<IList<ProfileData>>? _getList;
    private ICallGateSubscriber<Guid, int>?          _enable;
    private ICallGateSubscriber<Guid, int>?          _disable;
    private readonly Dictionary<Guid, Profile>              _profiles = [];
    private DateTime _nextRefresh = DateTime.MinValue;
    private DateTime _nextActionErrorLog = DateTime.MinValue;
    private bool _unavailableLogged;

    public CustomizePlusIpc(IDalamudPluginInterface pi)
        => _pi = pi;

    private bool Initialize()
    {
        try
        {
            _getList ??= _pi.GetIpcSubscriber<IList<ProfileData>>("CustomizePlus.Profile.GetList");
            _enable  ??= _pi.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.EnableByUniqueId");
            _disable ??= _pi.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.DisableByUniqueId");
            return true;
        }
        catch (Exception ex)
        {
            if (!_unavailableLogged)
                Glamourer.Log.Warning($"Could not initialize Customize+ profile IPC: {ex}");
            _unavailableLogged = true;
            return false;
        }
    }

    public IReadOnlyCollection<Profile> Profiles
    {
        get
        {
            Refresh();
            return _profiles.Values;
        }
    }

    public void Refresh(bool force = false)
    {
        if (!force && DateTime.UtcNow < _nextRefresh)
            return;

        _nextRefresh = DateTime.UtcNow.AddSeconds(5);
        if (!Initialize())
        {
            _profiles.Clear();
            return;
        }
        try
        {
            var list = _getList!.InvokeFunc();
            _profiles.Clear();
            if (list is not null)
                foreach (var profile in list)
                    if (profile.UniqueId != Guid.Empty)
                        _profiles[profile.UniqueId] = new Profile(profile.UniqueId, profile.Name, profile.VirtualPath, profile.IsEnabled);
            _unavailableLogged = false;
        }
        catch (IpcNotReadyError)
        {
            _profiles.Clear();
            if (!_unavailableLogged)
                Glamourer.Log.Debug("Customize+ profile IPC is unavailable.");
            _unavailableLogged = true;
        }
        catch (Exception ex)
        {
            _profiles.Clear();
            if (!_unavailableLogged)
                Glamourer.Log.Warning($"Could not retrieve Customize+ profiles: {ex}");
            _unavailableLogged = true;
        }
    }

    public void Apply(IReadOnlyDictionary<Guid, CustomizePlusAction> actions)
    {
        if (actions.Count is 0)
            return;

        Refresh();
        foreach (var (id, action) in actions)
        {
            if (action is CustomizePlusAction.Ignore || !_profiles.TryGetValue(id, out var profile))
                continue;

            var enable = action is CustomizePlusAction.Enable;
            if (profile.Enabled == enable)
                continue;

            try
            {
                var result = enable ? _enable!.InvokeFunc(id) : _disable!.InvokeFunc(id);
                if (result is 0)
                    _profiles[id] = profile with { Enabled = enable };
                else if (DateTime.UtcNow >= _nextActionErrorLog)
                {
                    Glamourer.Log.Warning($"Customize+ profile {id} returned error {result} while changing its enabled state.");
                    _nextActionErrorLog = DateTime.UtcNow.AddMinutes(1);
                }
            }
            catch (IpcNotReadyError)
            {
                _profiles.Clear();
                _nextRefresh = DateTime.MinValue;
                return;
            }
            catch (Exception ex)
            {
                if (DateTime.UtcNow >= _nextActionErrorLog)
                {
                    Glamourer.Log.Warning($"Could not change Customize+ profile {id}: {ex}");
                    _nextActionErrorLog = DateTime.UtcNow.AddMinutes(1);
                }
            }
        }
    }
}
