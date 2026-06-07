using Glamourer.Api.Enums;
using Glamourer.Designs;
using Glamourer.State;
using Luna;

namespace Glamourer.Gui;

public sealed class NavigationService : IUiService
{
    public event Action?              MainWindowOpened;
    public event Action<bool?>?       ToggleMainWindow;
    public event Action<MainTabType>? MainTabBar;
    public event Action<ActorState?>? Actor;
    public event Action<Design?>?     Design;
    public event Action<bool?>?       ToggleEquipmentBar;
    public event Action<bool?>?       ToggleQuickDesignBar;
    public event Action<Design?>?     QuickDesign;

    public void SetMainWindow(bool? open)
        => ToggleMainWindow?.Invoke(open);

    public void SetEquipmentBar(bool? open)
        => ToggleEquipmentBar?.Invoke(open);

    public void SetQuickDesignBar(bool? open)
        => ToggleQuickDesignBar?.Invoke(open);

    public void SetQuickDesign(Design? design)
        => QuickDesign?.Invoke(design);

    public void MoveTo(MainTabType tab)
    {
        if (tab is not MainTabType.None)
            MainTabBar?.Invoke(tab);
    }

    public void MoveTo(ActorState actor)
    {
        MoveTo(MainTabType.Actors);
        Actor?.Invoke(actor);
    }

    public void MoveTo(Design design)
    {
        MoveTo(MainTabType.Designs);
        Design?.Invoke(design);
    }

    public void OpenTo(MainTabType tab)
    {
        SetMainWindow(true);
        MoveTo(tab);
    }

    public void OpenTo(ActorState actor)
    {
        OpenTo(MainTabType.Actors);
        Actor?.Invoke(actor);
    }

    public void OpenTo(Design design)
    {
        OpenTo(MainTabType.Designs);
        Design?.Invoke(design);
    }

    internal void InvokeMainWindowOpened()
        => MainWindowOpened?.Invoke();
}
