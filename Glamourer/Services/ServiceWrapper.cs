using Dalamud.Data;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Plugin;
using Penumbra.GameData.Actors;
using System;
using System.Threading.Tasks;
using Dalamud.Game;
using Glamourer.Customization;
using Glamourer.Interop.Penumbra;
using Penumbra.GameData.Data;
using Penumbra.GameData;
using Penumbra.GameData.Enums;

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
    public IdentifierService(DalamudPluginInterface pi, DataManager data)
        : base(nameof(IdentifierService), () => Penumbra.GameData.GameData.GetIdentifier(pi, data))
    { }
}

public sealed class ItemService : AsyncServiceWrapper<ItemData>
{
    public ItemService(DalamudPluginInterface pi, DataManager gameData)
        : base(nameof(ItemService), () => new ItemData(pi, gameData, gameData.Language))
    { }
}

public sealed class ActorService : AsyncServiceWrapper<ActorManager>
{
    public ActorService(DalamudPluginInterface pi, ObjectTable objects, ClientState clientState, Framework framework, DataManager gameData,
        GameGui gui, PenumbraService penumbra)
        : base(nameof(ActorService),
            () => new ActorManager(pi, objects, clientState, framework, gameData, gui, idx => (short)penumbra.CutsceneParent(idx)))
    { }
}

public sealed class CustomizationService : AsyncServiceWrapper<ICustomizationManager>
{
    public CustomizationService(DalamudPluginInterface pi, DataManager gameData)
        : base(nameof(CustomizationService), () => CustomizationManager.Create(pi, gameData))
    { }

    /// <summary> In languages other than english the actual clan name may depend on gender. </summary>
    public string ClanName(SubRace race, Gender gender)
    {
        if (gender == Gender.FemaleNpc)
            gender = Gender.Female;
        if (gender == Gender.MaleNpc)
            gender = Gender.Male;
        return (gender, race) switch
        {
            (Gender.Male, SubRace.Midlander)         => AwaitedService.GetName(CustomName.MidlanderM),
            (Gender.Male, SubRace.Highlander)        => AwaitedService.GetName(CustomName.HighlanderM),
            (Gender.Male, SubRace.Wildwood)          => AwaitedService.GetName(CustomName.WildwoodM),
            (Gender.Male, SubRace.Duskwight)         => AwaitedService.GetName(CustomName.DuskwightM),
            (Gender.Male, SubRace.Plainsfolk)        => AwaitedService.GetName(CustomName.PlainsfolkM),
            (Gender.Male, SubRace.Dunesfolk)         => AwaitedService.GetName(CustomName.DunesfolkM),
            (Gender.Male, SubRace.SeekerOfTheSun)    => AwaitedService.GetName(CustomName.SeekerOfTheSunM),
            (Gender.Male, SubRace.KeeperOfTheMoon)   => AwaitedService.GetName(CustomName.KeeperOfTheMoonM),
            (Gender.Male, SubRace.Seawolf)           => AwaitedService.GetName(CustomName.SeawolfM),
            (Gender.Male, SubRace.Hellsguard)        => AwaitedService.GetName(CustomName.HellsguardM),
            (Gender.Male, SubRace.Raen)              => AwaitedService.GetName(CustomName.RaenM),
            (Gender.Male, SubRace.Xaela)             => AwaitedService.GetName(CustomName.XaelaM),
            (Gender.Male, SubRace.Helion)            => AwaitedService.GetName(CustomName.HelionM),
            (Gender.Male, SubRace.Lost)              => AwaitedService.GetName(CustomName.LostM),
            (Gender.Male, SubRace.Rava)              => AwaitedService.GetName(CustomName.RavaM),
            (Gender.Male, SubRace.Veena)             => AwaitedService.GetName(CustomName.VeenaM),
            (Gender.Female, SubRace.Midlander)       => AwaitedService.GetName(CustomName.MidlanderF),
            (Gender.Female, SubRace.Highlander)      => AwaitedService.GetName(CustomName.HighlanderF),
            (Gender.Female, SubRace.Wildwood)        => AwaitedService.GetName(CustomName.WildwoodF),
            (Gender.Female, SubRace.Duskwight)       => AwaitedService.GetName(CustomName.DuskwightF),
            (Gender.Female, SubRace.Plainsfolk)      => AwaitedService.GetName(CustomName.PlainsfolkF),
            (Gender.Female, SubRace.Dunesfolk)       => AwaitedService.GetName(CustomName.DunesfolkF),
            (Gender.Female, SubRace.SeekerOfTheSun)  => AwaitedService.GetName(CustomName.SeekerOfTheSunF),
            (Gender.Female, SubRace.KeeperOfTheMoon) => AwaitedService.GetName(CustomName.KeeperOfTheMoonF),
            (Gender.Female, SubRace.Seawolf)         => AwaitedService.GetName(CustomName.SeawolfF),
            (Gender.Female, SubRace.Hellsguard)      => AwaitedService.GetName(CustomName.HellsguardF),
            (Gender.Female, SubRace.Raen)            => AwaitedService.GetName(CustomName.RaenF),
            (Gender.Female, SubRace.Xaela)           => AwaitedService.GetName(CustomName.XaelaF),
            (Gender.Female, SubRace.Helion)          => AwaitedService.GetName(CustomName.HelionM),
            (Gender.Female, SubRace.Lost)            => AwaitedService.GetName(CustomName.LostM),
            (Gender.Female, SubRace.Rava)            => AwaitedService.GetName(CustomName.RavaF),
            (Gender.Female, SubRace.Veena)           => AwaitedService.GetName(CustomName.VeenaF),
            _                                        => "Unknown",
        };
    }
}
