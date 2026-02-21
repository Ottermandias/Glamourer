using System.Security.AccessControl;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Designs.History;
using Glamourer.Events;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignHeader : SplitButtonHeader, IDisposable
{
    private readonly DesignFileSystem _fileSystem;
    private readonly DesignChanged    _designChanged;
    private readonly Configuration    _config;

    private StringU8 _header    = new("No Selection"u8);
    private StringU8 _incognito = new("No Selection"u8);

    public DesignHeader(DesignFileSystem fileSystem, IncognitoButton incognito, DesignChanged designChanged, Configuration config,
        DesignConverter converter, StateManager stateManager, EditorHistory history, DesignManager manager, ActorObjectManager objects)
    {
        _fileSystem    = fileSystem;
        _designChanged = designChanged;
        _config        = config;
        LeftButtons.AddButton(new SetFromClipboardButton(fileSystem, converter, manager),                      100);
        LeftButtons.AddButton(new DesignUndoButton(fileSystem, manager),                                       90);
        LeftButtons.AddButton(new ExportToClipboardButton(fileSystem, converter),                              80);
        LeftButtons.AddButton(new ApplyCharacterButton(fileSystem, manager, objects, stateManager, converter), 70);
        LeftButtons.AddButton(new UndoButton(fileSystem, history),                                             60);

        RightButtons.AddButton(incognito,                    50);
        RightButtons.AddButton(new LockedButton(fileSystem, manager), 100);
        _fileSystem.Selection.Changed += OnSelectionChanged;
        OnSelectionChanged();
        designChanged.Subscribe(OnDesignChanged, DesignChanged.Priority.DesignHeader);
    }

    private void OnDesignChanged(DesignChanged.Type arg1, Design arg2, ITransaction? arg3)
    {
        if (arg1 is not DesignChanged.Type.Renamed)
            return;

        if (arg2 != _fileSystem.Selection.Selection?.Value)
            return;

        _header = new StringU8(arg2.Name.Text);
    }

    private void OnSelectionChanged()
    {
        if (_fileSystem.Selection.Selection?.GetValue<Design>() is { } selection)
        {
            _header    = new StringU8(selection.Name.Text);
            _incognito = new StringU8(selection.Incognito);
        }
        else if (_fileSystem.Selection.OrderedNodes.Count > 0)
        {
            _header    = new StringU8($"{_fileSystem.Selection.OrderedNodes.Count} Objects Selected");
            _incognito = _header;
        }
        else
        {
            _header    = new StringU8("No Selection"u8);
            _incognito = _header;
        }
    }

    public override void Draw(Vector2 size)
    {
        var       color = ColorId.HeaderButtons.Value();
        using var _     = ImGuiColor.Text.Push(color).Push(ImGuiColor.Border, color);
        base.Draw(size with { Y = Im.Style.FrameHeight });
    }

    public override ReadOnlySpan<byte> Text
        => _config.Ephemeral.IncognitoMode ? _incognito : _header;

    private sealed class LockedButton(DesignFileSystem fileSystem, DesignManager manager) : BaseIconButton<AwesomeIcon>
    {
        public override bool IsVisible
            => fileSystem.Selection.Selection is not null;

        public override AwesomeIcon Icon
            => ((Design)fileSystem.Selection.Selection!.Value).WriteProtected() ? LunaStyle.LockedIcon : LunaStyle.UnlockedIcon;

        public override bool HasTooltip
            => true;

        public override void DrawTooltip()
            => Im.Text(((Design)fileSystem.Selection.Selection!.Value).WriteProtected()
                ? "Make this design editable."u8
                : "Write-protect this design."u8);

        public override void OnClick()
            => manager.SetWriteProtection((Design)fileSystem.Selection.Selection!.Value,
                !((Design)fileSystem.Selection.Selection!.Value).WriteProtected());
    }

    public void Dispose()
    {
        _fileSystem.Selection.Changed -= OnSelectionChanged;
        _designChanged.Unsubscribe(OnDesignChanged);
    }
}
