using Glamourer.Automation;
using Glamourer.Events;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class AutomationSelection : IUiService, IDisposable
{
    public static readonly StringU8 NoSelection = new("No Set Selected"u8);

    private readonly AutomationChanged _automationChanged;

    public int DraggedDesignIndex = -1;

    public AutoDesignSet? Set { get; private set; }
    public int            Index     { get; private set; } = -1;
    public StringU8       Name      { get; private set; } = NoSelection;
    public StringU8       Incognito { get; private set; } = NoSelection;

    public AutomationSelection(AutomationChanged automationChanged)
    {
        _automationChanged = automationChanged;
        _automationChanged.Subscribe(OnAutomationChanged, AutomationChanged.Priority.SetSelector);
    }

    public void Dispose()
    {
        _automationChanged.Unsubscribe(OnAutomationChanged);
    }

    private void OnAutomationChanged(AutomationChanged.Type type, AutoDesignSet? set, object? data)
    {
        if (set != Set)
            return;

        switch (type)
        {
            case AutomationChanged.Type.DeletedSet: Update(null); break;
            case AutomationChanged.Type.RenamedSet: Name  = new StringU8(set!.Name); break;
            case AutomationChanged.Type.MovedSet:   Index = (((int, int))data!).Item2; break;
        }
    }

    public void Update(in AutomationCacheItem? item)
    {
        Set = item?.Set;
        if (Set is null)
        {
            Index     = -1;
            Name      = NoSelection;
            Incognito = NoSelection;
        }
        else
        {
            Index     = item!.Value.Index;
            Name      = item!.Value.Name.Utf8;
            Incognito = item!.Value.Incognito;
        }
    }
}
