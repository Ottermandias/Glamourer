using Glamourer.Automation;
using Glamourer.Config;
using Glamourer.Events;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class AutomationSelection : IUiService, IDisposable
{
    public static readonly StringU8 NoSelection = new("No Set Selected"u8);

    private readonly AutomationChanged _automationChanged;
    private readonly UiConfig          _config;

    public int DraggedDesignIndex = -1;

    public AutoDesignSet? Set       { get; private set; }
    public int            Index     { get; private set; } = -1;
    public StringU8       Name      { get; private set; } = NoSelection;
    public StringU8       Incognito { get; private set; } = NoSelection;

    public AutomationSelection(AutomationChanged automationChanged, UiConfig config, AutoDesignManager autoDesigns)
    {
        _automationChanged = automationChanged;
        _config            = config;
        _automationChanged.Subscribe(OnAutomationChanged, AutomationChanged.Priority.SetSelector);
        InitialSelect(autoDesigns);
    }

    public void Dispose()
    {
        _automationChanged.Unsubscribe(OnAutomationChanged);
    }

    private void OnAutomationChanged(in AutomationChanged.Arguments arguments)
    {
        if (arguments.Set != Set)
            return;

        switch (arguments.Type)
        {
            case AutomationChanged.Type.DeletedSet: Update(null); break;
            case AutomationChanged.Type.RenamedSet: Name  = new StringU8(arguments.Set!.Name); break;
            case AutomationChanged.Type.MovedSet:   Index = arguments.As<AutomationChanged.MovedSetArguments>().NewIndex; break;
        }
    }

    public void Update(in AutomationCacheItem? item)
    {
        Set = item?.Set;
        if (Set is null)
        {
            _config.SelectedAutomationIndex = -1;
            Index                           = -1;
            Name                            = NoSelection;
            Incognito                       = NoSelection;
        }
        else
        {
            _config.SelectedAutomationIndex = item!.Value.Index;
            Index                           = item!.Value.Index;
            Name                            = item!.Value.Name.Utf8;
            Incognito                       = item!.Value.Incognito;
        }
    }

    private void InitialSelect(AutoDesignManager autoDesigns)
    {
        if (_config.SelectedAutomationIndex < 0)
            return;

        if (autoDesigns.Count <= _config.SelectedAutomationIndex)
        {
            _config.SelectedAutomationIndex = -1;
            return;
        }

        Set       = autoDesigns[_config.SelectedAutomationIndex];
        Index     = _config.SelectedAutomationIndex;
        Name      = new StringU8(Set.Name);
        Incognito = new StringU8($"Auto Design Set #{Index + 1}");
    }
}
