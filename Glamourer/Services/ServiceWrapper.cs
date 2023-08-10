using Dalamud.Plugin;
using Penumbra.GameData.Actors;
using System;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using Glamourer.Interop.Penumbra;
using Penumbra.GameData.Data;
using Penumbra.GameData;

namespace Glamourer.Services;

public abstract class AsyncServiceWrapper<T> : IDisposable
{
    public string Name    { get; }
    public T?     Service { get; private set; }

    public T AwaitedService
    {
        get
        {
            _task?.Wait();
            return Service!;
        }
    }

    public bool Valid
        => Service != null && !_isDisposed;

    public event Action? FinishedCreation;
    private Task?        _task;

    private bool _isDisposed;

    protected AsyncServiceWrapper(string name, Func<T> factory)
    {
        Name = name;
        _task = Task.Run(() =>
        {
            var service = factory();
            if (_isDisposed)
            {
                if (service is IDisposable d)
                    d.Dispose();
            }
            else
            {
                Service = service;
                Glamourer.Log.Verbose($"[{Name}] Created.");
                _task = null;
            }
        });
        _task.ContinueWith((t, x) =>
        {
            if (!_isDisposed)
                FinishedCreation?.Invoke();
        }, null);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _task       = null;
        if (Service is IDisposable d)
            d.Dispose();
        Glamourer.Log.Verbose($"[{Name}] Disposed.");
    }
}

public sealed class IdentifierService : AsyncServiceWrapper<IObjectIdentifier>
{
    public IdentifierService(DalamudPluginInterface pi, IDataManager data, ItemService itemService)
        : base(nameof(IdentifierService), () => Penumbra.GameData.GameData.GetIdentifier(pi, data, itemService.AwaitedService))
    { }
}

public sealed class ItemService : AsyncServiceWrapper<ItemData>
{
    public ItemService(DalamudPluginInterface pi, IDataManager gameData)
        : base(nameof(ItemService), () => new ItemData(pi, gameData, gameData.Language))
    { }
}

public sealed class ActorService : AsyncServiceWrapper<ActorManager>
{
    public ActorService(DalamudPluginInterface pi, IObjectTable objects, IClientState clientState, Framework framework, IDataManager gameData,
        IGameGui gui, PenumbraService penumbra)
        : base(nameof(ActorService),
            () => new ActorManager(pi, objects, clientState, framework, gameData, gui, idx => (short)penumbra.CutsceneParent(idx)))
    { }
}
