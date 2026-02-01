using Dalamud.Interface;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.Special;
using Glamourer.Events;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class RandomRestrictionDrawer : IService, IDisposable
{
    private AutoDesignSet? _set;
    private int            _designIndex = -1;

    private readonly AutomationChanged _automationChanged;
    private readonly Configuration     _config;
    private readonly AutoDesignManager _autoDesignManager;
    private readonly RandomDesignCombo _randomDesignCombo;
    private readonly SetSelector       _selector;
    private readonly DesignStorage     _designs;
    private readonly DesignFileSystem  _designFileSystem;

    private string  _newText = string.Empty;
    private string? _newDefinition;
    private Design? _newDesign;

    public RandomRestrictionDrawer(AutomationChanged automationChanged, Configuration config, AutoDesignManager autoDesignManager,
        RandomDesignCombo randomDesignCombo, SetSelector selector, DesignFileSystem designFileSystem, DesignStorage designs)
    {
        _automationChanged = automationChanged;
        _config            = config;
        _autoDesignManager = autoDesignManager;
        _randomDesignCombo = randomDesignCombo;
        _selector          = selector;
        _designFileSystem  = designFileSystem;
        _designs           = designs;
        _automationChanged.Subscribe(OnAutomationChange, AutomationChanged.Priority.RandomRestrictionDrawer);
    }

    public void Dispose()
    {
        _automationChanged.Unsubscribe(OnAutomationChange);
    }

    public void DrawButton(AutoDesignSet set, int designIndex)
    {
        var isOpen = set == _set && designIndex == _designIndex;
        using (ImGuiColor.Button.Push(Im.Style[ImGuiColor.ButtonActive], isOpen)
                   .Push(ImGuiColor.Text,   ColorId.HeaderButtons.Value(), isOpen)
                   .Push(ImGuiColor.Border, ColorId.HeaderButtons.Value(), isOpen))
        {
            using var frame = ImStyleSingle.FrameBorderThickness.Push(2 * Im.Style.GlobalScale, isOpen);
            if (ImEx.Icon.Button(LunaStyle.EditIcon))
            {
                if (isOpen)
                    Close();
                else
                    Open(set, designIndex);
            }
        }

        Im.Tooltip.OnHover("Edit restrictions for this random design."u8);
    }

    private void Open(AutoDesignSet set, int designIndex)
    {
        if (designIndex < 0 || designIndex >= set.Designs.Count)
            return;

        var design = set.Designs[designIndex];
        if (design.Design is not RandomDesign)
            return;

        _set         = set;
        _designIndex = designIndex;
    }

    private void Close()
    {
        _set         = null;
        _designIndex = -1;
    }

    public void Draw()
    {
        if (_set is null || _designIndex < 0 || _designIndex >= _set.Designs.Count)
            return;

        if (_set != _selector.Selection)
        {
            Close();
            return;
        }

        var design = _set.Designs[_designIndex];
        if (design.Design is not RandomDesign random)
            return;

        DrawWindow(random);
    }

    private void DrawWindow(RandomDesign random)
    {
        var flags = WindowFlags.NoFocusOnAppearing
          | WindowFlags.NoCollapse
          | WindowFlags.NoResize;

        // Set position to the right of the main window when attached
        // The downwards offset is implicit through child position.
        if (_config.KeepAdvancedDyesAttached)
        {
            var position = Im.Window.Position;
            position.X += Im.Window.Size.X + Im.Style.WindowPadding.X;
            Im.Window.SetNextPosition(position);
            flags |= WindowFlags.NoMove;
        }

        using var color = ImGuiColor.TitleBackgroundActive.Push(Im.Style[ImGuiColor.TitleBackground]);

        var size = new Vector2(7 * Im.Style.FrameHeight + 3 * Im.Style.ItemInnerSpacing.X + 300 * Im.Style.GlobalScale,
            18 * Im.Style.FrameHeightWithSpacing + Im.Style.WindowPadding.Y + Im.Style.ItemSpacing.Y);
        Im.Window.SetNextSize(size);

        var open   = true;
        var window = Im.Window.Begin($"{_set!.Name} #{_designIndex + 1:D2}###Glamourer Random Design", ref open, flags);
        try
        {
            if (window)
                DrawContent(random);
        }
        finally
        {
            window.Dispose();
        }

        if (!open)
            Close();
    }

    private void DrawTable(RandomDesign random, List<IDesignPredicate> list)
    {
        using var table = Im.Table.Begin("##table"u8, 3);
        if (!table)
            return;

        using var spacing    = ImStyleDouble.ItemSpacing.Push(Im.Style.ItemInnerSpacing);
        var       buttonSize = new Vector2(Im.Style.FrameHeight);
        var       descWidth  = Im.Font.CalculateSize("or that are set to the color"u8).X;
        table.SetupColumn("desc"u8,  TableColumnFlags.WidthFixed, descWidth);
        table.SetupColumn("input"u8, TableColumnFlags.WidthStretch);
        table.SetupColumn("del"u8,   TableColumnFlags.WidthFixed, buttonSize.X * 2 + Im.Style.ItemInnerSpacing.X);

        var orSize = Im.Font.CalculateSize("or "u8);
        for (var i = 0; i < random.Predicates.Count; ++i)
        {
            using var id        = Im.Id.Push(i);
            var       predicate = random.Predicates[i];
            table.NextColumn();
            if (i is not 0)
                ImEx.TextFrameAligned("or "u8);
            else
                Im.Dummy(orSize);
            Im.Line.NoSpacing();
            switch (predicate)
            {
                case RandomPredicate.Contains contains:
                {
                    ImEx.TextFrameAligned("that contain"u8);
                    table.NextColumn();
                    var data = contains.Value.Text;
                    Im.Item.SetNextWidthFull();
                    if (Im.Input.Text("##match"u8, ref data, "Name, Path, or Identifier Contains..."u8))
                    {
                        if (data.Length is 0)
                            list.RemoveAt(i);
                        else
                            list[i] = new RandomPredicate.Contains(data);
                        _autoDesignManager.ChangeData(_set!, _designIndex, list);
                    }

                    break;
                }
                case RandomPredicate.StartsWith startsWith:
                {
                    ImEx.TextFrameAligned("whose path starts with"u8);
                    table.NextColumn();
                    var data = startsWith.Value.Text;
                    Im.Item.SetNextWidthFull();
                    if (Im.Input.Text("##startsWith"u8, ref data, "Path Starts With..."u8))
                    {
                        if (data.Length is 0)
                            list.RemoveAt(i);
                        else
                            list[i] = new RandomPredicate.StartsWith(data);
                        _autoDesignManager.ChangeData(_set!, _designIndex, list);
                    }

                    break;
                }
                case RandomPredicate.Exact { Which: RandomPredicate.Exact.Type.Tag } exact:
                {
                    ImEx.TextFrameAligned("that contain the tag"u8);
                    table.NextColumn();
                    Im.Item.SetNextWidthFull();
                    var data = exact.Value.Text;
                    if (Im.Input.Text("##color"u8, ref data, "Contained tag..."u8))
                    {
                        if (data.Length is 0)
                            list.RemoveAt(i);
                        else
                            list[i] = new RandomPredicate.Exact(RandomPredicate.Exact.Type.Tag, data);
                        _autoDesignManager.ChangeData(_set!, _designIndex, list);
                    }

                    break;
                }
                case RandomPredicate.Exact { Which: RandomPredicate.Exact.Type.Color } exact:
                {
                    ImEx.TextFrameAligned("that are set to the color"u8);
                    table.NextColumn();
                    Im.Item.SetNextWidthFull();
                    var data = exact.Value.Text;
                    if (Im.Input.Text("##color"u8, ref data, "Assigned Color is..."u8))
                    {
                        if (data.Length is 0)
                            list.RemoveAt(i);
                        else
                            list[i] = new RandomPredicate.Exact(RandomPredicate.Exact.Type.Color, data);
                        _autoDesignManager.ChangeData(_set!, _designIndex, list);
                    }

                    break;
                }
                case RandomPredicate.Exact exact:
                {
                    ImEx.TextFrameAligned("that are exactly"u8);
                    table.NextColumn();
                    if (_randomDesignCombo.Draw(exact, Im.ContentRegion.Available.X) && _randomDesignCombo.Design is Design d)
                    {
                        list[i] = new RandomPredicate.Exact(RandomPredicate.Exact.Type.Identifier, d.Identifier.ToString());
                        _autoDesignManager.ChangeData(_set!, _designIndex, list);
                    }

                    break;
                }
            }

            table.NextColumn();
            if (ImEx.Icon.Button(LunaStyle.DeleteIcon, "Delete this restriction."u8))
            {
                list.RemoveAt(i);
                _autoDesignManager.ChangeData(_set!, _designIndex, list);
            }

            Im.Line.Same();
            DrawLookup(predicate);
        }
    }

    private void DrawLookup(IDesignPredicate predicate)
    {
        ImEx.Icon.Button(FontAwesomeIcon.MagnifyingGlassChart.Icon(), StringU8.Empty);
        if (!Im.Item.Hovered())
            return;

        var designs = predicate.Get(_designs, _designFileSystem);
        LookupTooltip(designs);
    }

    private void LookupTooltip(IEnumerable<Design> designs)
    {
        using var _          = Im.Tooltip.Begin();
        using var enumerator = designs.GetEnumerator();
        while (enumerator.MoveNext())
        {
            Im.Text("Matches the following designs:"u8);
            var name = _designFileSystem.TryGetValue(enumerator.Current, out var l) ? l.FullName() : enumerator.Current.Name.Text;
            Im.Separator();
            Im.BulletText(name);
            while (enumerator.MoveNext())
            {
                name = _designFileSystem.TryGetValue(enumerator.Current, out l) ? l.FullName() : enumerator.Current.Name.Text;
                Im.BulletText(name);
            }
            return;
        }
        Im.Text("Matches no currently existing designs."u8);
    }

    private void DrawNewButtons(List<IDesignPredicate> list)
    {
        Im.Item.SetNextWidthFull();
        Im.Input.Text("##newText"u8, ref _newText, "Add New Restriction..."u8);
        var invalid = _newText.Length is 0;

        var buttonSize = new Vector2((Im.ContentRegion.Available.X - 3 * Im.Style.ItemInnerSpacing.X) / 4, 0);
        var changed = ImEx.Button("Starts With"u8, buttonSize,
                "Add a new condition that design paths must start with the given text."u8, invalid)
         && Add(new RandomPredicate.StartsWith(_newText));

        Im.Line.SameInner();
        changed |= ImEx.Button("Contains"u8, buttonSize,
                "Add a new condition that design paths, names or identifiers must contain the given text."u8, invalid)
         && Add(new RandomPredicate.Contains(_newText));

        Im.Line.SameInner();
        changed |= ImEx.Button("Has Tag"u8, buttonSize,
                "Add a new condition that the design must contain the given tag."u8, invalid)
         && Add(new RandomPredicate.Exact(RandomPredicate.Exact.Type.Tag, _newText));

        Im.Line.SameInner();
        changed |= ImEx.Button("Assigned Color"u8, buttonSize,
                "Add a new condition that the design must be assigned to the given color."u8, invalid)
         && Add(new RandomPredicate.Exact(RandomPredicate.Exact.Type.Color, _newText));

        if (_randomDesignCombo.Draw(_newDesign, Im.ContentRegion.Available.X - Im.Style.ItemInnerSpacing.X - buttonSize.X))
            _newDesign = _randomDesignCombo.CurrentSelection?.Item1 as Design;
        Im.Line.SameInner();
        if (ImEx.Button("Exact Design"u8, buttonSize, "Add a single, specific design."u8, _newDesign is null))
        {
            Add(new RandomPredicate.Exact(RandomPredicate.Exact.Type.Identifier, _newDesign!.Identifier.ToString()));
            changed    = true;
            _newDesign = null;
        }

        if (changed)
            _autoDesignManager.ChangeData(_set!, _designIndex, list);

        return;

        bool Add(IDesignPredicate predicate)
        {
            list.Add(predicate);
            return true;
        }
    }

    private void DrawManualInput(IReadOnlyList<IDesignPredicate> list)
    {
        Im.Dummy(Vector2.Zero);
        Im.Separator();
        Im.Dummy(Vector2.Zero);
        DrawTotalPreview(list);
        var currentDefinition = RandomPredicate.GeneratePredicateString(list);
        var definition        = _newDefinition ?? currentDefinition;
        definition = definition.Replace(";", ";\n\t").Replace("{", "{\n\t").Replace("}", "\n}");
        var lines = definition.Count(c => c is '\n');
        if (Im.Input.MultiLine("##definition"u8, ref definition, Im.ContentRegion.Available with { Y = (lines + 1) * Im.Style.TextHeight + Im.Style.FrameHeight },
                InputTextFlags.CtrlEnterForNewLine))
            _newDefinition = definition;
        if (Im.Item.DeactivatedAfterEdit && _newDefinition is not null && _newDefinition != currentDefinition)
        {
            var predicates = RandomPredicate.GeneratePredicates(_newDefinition.Replace("\n", string.Empty).Replace("\t", string.Empty));
            _autoDesignManager.ChangeData(_set!, _designIndex, predicates);
            _newDefinition = null;
        }

        if (Im.Button("Copy to Clipboard Without Line Breaks"u8, Im.ContentRegion.Available with { Y = 0 }))
        {
            try
            {
                Im.Clipboard.Set(currentDefinition);
            }
            catch
            {
                // ignored
            }
        }
    }

    private void DrawTotalPreview(IReadOnlyList<IDesignPredicate> list)
    {
        var designs = IDesignPredicate.Get(list, _designs, _designFileSystem).ToList();
        Im.Button(designs.Count > 0
                ? $"All Restrictions Combined Match {designs.Count} Designs"
                : "None of the Restrictions Matches Any Designs"u8, Im.ContentRegion.Available with { Y = 0 });
        if (Im.Item.Hovered())
            LookupTooltip(designs);
    }

    private void DrawContent(RandomDesign random)
    {
        Im.Cursor.Y += Im.Style.GlobalScale - Im.Style.WindowPadding.Y;
        Im.Separator();
        Im.Dummy(Vector2.Zero);
        var reset = random.ResetOnRedraw;
        if (Im.Checkbox("Reset Chosen Design On Every Redraw"u8, ref reset))
            _autoDesignManager.ChangeData(_set!, _designIndex, reset);
        Im.Separator();
        Im.Dummy(Vector2.Zero);

        var list = random.Predicates.ToList();
        if (list.Count is 0)
        {
            Im.Text("No Restrictions Set. Selects among all existing Designs."u8);
        }
        else
        {
            Im.Text("Select among designs..."u8);
            DrawTable(random, list);
        }

        Im.Dummy(Vector2.Zero);
        Im.Separator();
        Im.Dummy(Vector2.Zero);

        DrawNewButtons(list);
        DrawManualInput(list);
    }

    private void OnAutomationChange(AutomationChanged.Type type, AutoDesignSet? set, object? data)
    {
        if (set != _set || _set is null)
            return;

        switch (type)
        {
            case AutomationChanged.Type.DeletedSet:
            case AutomationChanged.Type.DeletedDesign when data is int index && _designIndex == index:
                Close();
                break;
            case AutomationChanged.Type.MovedDesign when data is (int from, int to):
                if (_designIndex == from)
                    _designIndex = to;
                else if (_designIndex < from && _designIndex > to)
                    _designIndex++;
                else if (_designIndex > to && _designIndex < from)
                    _designIndex--;
                break;
            case AutomationChanged.Type.ChangedDesign when data is (int index, IDesignStandIn _, IDesignStandIn _) && index == _designIndex:
                Close();
                break;
        }
    }
}
