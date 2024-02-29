using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.Special;
using Glamourer.Events;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;

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
    private Design? _newDesign = null;

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
        using (var color = ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), isOpen)
                   .Push(ImGuiCol.Text,   ColorId.HeaderButtons.Value(), isOpen)
                   .Push(ImGuiCol.Border, ColorId.HeaderButtons.Value(), isOpen))
        {
            using var frame = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2 * ImGuiHelpers.GlobalScale, isOpen);
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Edit.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                    string.Empty, false, true))
            {
                if (isOpen)
                    Close();
                else
                    Open(set, designIndex);
            }
        }

        ImGuiUtil.HoverTooltip("Edit restrictions for this random design.");
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
        if (_set == null || _designIndex < 0 || _designIndex >= _set.Designs.Count)
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
        var flags = ImGuiWindowFlags.NoFocusOnAppearing
          | ImGuiWindowFlags.NoCollapse
          | ImGuiWindowFlags.NoResize;

        // Set position to the right of the main window when attached
        // The downwards offset is implicit through child position.
        if (_config.KeepAdvancedDyesAttached)
        {
            var position = ImGui.GetWindowPos();
            position.X += ImGui.GetWindowSize().X + ImGui.GetStyle().WindowPadding.X;
            ImGui.SetNextWindowPos(position);
            flags |= ImGuiWindowFlags.NoMove;
        }

        using var color = ImRaii.PushColor(ImGuiCol.TitleBgActive, ImGui.GetColorU32(ImGuiCol.TitleBg));

        var size = new Vector2(7 * ImGui.GetFrameHeight() + 3 * ImGui.GetStyle().ItemInnerSpacing.X + 300 * ImGuiHelpers.GlobalScale,
            18 * ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y + ImGui.GetStyle().ItemSpacing.Y);
        ImGui.SetNextWindowSize(size);

        var open   = true;
        var window = ImGui.Begin($"{_set!.Name} #{_designIndex + 1:D2}###Glamourer Random Design", ref open, flags);
        try
        {
            if (window)
                DrawContent(random);
        }
        finally
        {
            ImGui.End();
        }

        if (!open)
            Close();
    }

    private void DrawTable(RandomDesign random, List<IDesignPredicate> list)
    {
        using var table = ImRaii.Table("##table", 3);
        if (!table)
            return;

        using var spacing    = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemInnerSpacing);
        var       buttonSize = new Vector2(ImGui.GetFrameHeight());
        var       descWidth  = ImGui.CalcTextSize("or that are set to the color").X;
        ImGui.TableSetupColumn("desc",  ImGuiTableColumnFlags.WidthFixed, descWidth);
        ImGui.TableSetupColumn("input", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("del",   ImGuiTableColumnFlags.WidthFixed, buttonSize.X * 2 + ImGui.GetStyle().ItemInnerSpacing.X);

        var orSize = ImGui.CalcTextSize("or ");
        for (var i = 0; i < random.Predicates.Count; ++i)
        {
            using var id        = ImRaii.PushId(i);
            var       predicate = random.Predicates[i];
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            if (i != 0)
                ImGui.TextUnformatted("or ");
            else
                ImGui.Dummy(orSize);
            ImGui.SameLine(0, 0);
            ImGui.AlignTextToFramePadding();
            switch (predicate)
            {
                case RandomPredicate.Contains contains:
                {
                    ImGui.TextUnformatted("that contain");
                    ImGui.TableNextColumn();
                    var data = contains.Value.Text;
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputTextWithHint("##match", "Name, Path, or Identifier Contains...", ref data, 128))
                    {
                        if (data.Length == 0)
                            list.RemoveAt(i);
                        else
                            list[i] = new RandomPredicate.Contains(data);
                        _autoDesignManager.ChangeData(_set!, _designIndex, list);
                    }

                    break;
                }
                case RandomPredicate.StartsWith startsWith:
                {
                    ImGui.TextUnformatted("whose path starts with");
                    ImGui.TableNextColumn();
                    var data = startsWith.Value.Text;
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputTextWithHint("##startsWith", "Path Starts With...", ref data, 128))
                    {
                        if (data.Length == 0)
                            list.RemoveAt(i);
                        else
                            list[i] = new RandomPredicate.StartsWith(data);
                        _autoDesignManager.ChangeData(_set!, _designIndex, list);
                    }

                    break;
                }
                case RandomPredicate.Exact { Which: RandomPredicate.Exact.Type.Tag } exact:
                {
                    ImGui.TextUnformatted("that contain the tag");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    var data = exact.Value.Text;
                    if (ImGui.InputTextWithHint("##color", "Contained tag...", ref data, 128))
                    {
                        if (data.Length == 0)
                            list.RemoveAt(i);
                        else
                            list[i] = new RandomPredicate.Exact(RandomPredicate.Exact.Type.Tag, data);
                        _autoDesignManager.ChangeData(_set!, _designIndex, list);
                    }

                    break;
                }
                case RandomPredicate.Exact { Which: RandomPredicate.Exact.Type.Color } exact:
                {
                    ImGui.TextUnformatted("that are set to the color");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    var data = exact.Value.Text;
                    if (ImGui.InputTextWithHint("##color", "Assigned Color is...", ref data, 128))
                    {
                        if (data.Length == 0)
                            list.RemoveAt(i);
                        else
                            list[i] = new RandomPredicate.Exact(RandomPredicate.Exact.Type.Color, data);
                        _autoDesignManager.ChangeData(_set!, _designIndex, list);
                    }

                    break;
                }
                case RandomPredicate.Exact exact:
                {
                    ImGui.TextUnformatted("that are exactly");
                    ImGui.TableNextColumn();
                    if (_randomDesignCombo.Draw(exact, ImGui.GetContentRegionAvail().X) && _randomDesignCombo.Design is Design d)
                    {
                        list[i] = new RandomPredicate.Exact(RandomPredicate.Exact.Type.Identifier, d.Identifier.ToString());
                        _autoDesignManager.ChangeData(_set!, _designIndex, list);
                    }

                    break;
                }
            }

            ImGui.TableNextColumn();
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), buttonSize, "Delete this restriction.", false, true))
            {
                list.RemoveAt(i);
                _autoDesignManager.ChangeData(_set!, _designIndex, list);
            }

            ImGui.SameLine();
            DrawLookup(predicate, buttonSize);
        }
    }

    private void DrawLookup(IDesignPredicate predicate, Vector2 buttonSize)
    {
        ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.MagnifyingGlassChart.ToIconString(), buttonSize, string.Empty, false, true);
        if (!ImGui.IsItemHovered())
            return;

        var designs = predicate.Get(_designs, _designFileSystem);
        LookupTooltip(designs);
    }

    private void LookupTooltip(IEnumerable<Design> designs)
    {
        using var _ = ImRaii.Tooltip();
        var tt = string.Join('\n', designs.Select(d => _designFileSystem.FindLeaf(d, out var l) ? l.FullName() : d.Name.Text).OrderBy(t => t));
        ImGui.TextUnformatted(tt.Length == 0
            ? "Matches no currently existing designs."
            : "Matches the following designs:");
        ImGui.Separator();
        ImGui.TextUnformatted(tt);
    }

    private void DrawNewButtons(List<IDesignPredicate> list)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputTextWithHint("##newText", "Add New Restriction...", ref _newText, 128);
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var invalid = _newText.Length == 0;

        var buttonSize = new Vector2((ImGui.GetContentRegionAvail().X - 3 * spacing) / 4, 0);
        var changed = ImGuiUtil.DrawDisabledButton("Starts With", buttonSize,
                "Add a new condition that design paths must start with the given text.", invalid)
         && Add(new RandomPredicate.StartsWith(_newText));

        ImGui.SameLine(0, spacing);
        changed |= ImGuiUtil.DrawDisabledButton("Contains", buttonSize,
                "Add a new condition that design paths, names or identifiers must contain the given text.", invalid)
         && Add(new RandomPredicate.Contains(_newText));

        ImGui.SameLine(0, spacing);
        changed |= ImGuiUtil.DrawDisabledButton("Has Tag", buttonSize,
                "Add a new condition that the design must contain the given tag.", invalid)
         && Add(new RandomPredicate.Exact(RandomPredicate.Exact.Type.Tag, _newText));

        ImGui.SameLine(0, spacing);
        changed |= ImGuiUtil.DrawDisabledButton("Assigned Color", buttonSize,
                "Add a new condition that the design must be assigned to the given color.", invalid)
         && Add(new RandomPredicate.Exact(RandomPredicate.Exact.Type.Color, _newText));

        if (_randomDesignCombo.Draw(_newDesign, ImGui.GetContentRegionAvail().X - spacing - buttonSize.X))
            _newDesign = _randomDesignCombo.CurrentSelection?.Item1 as Design;
        ImGui.SameLine(0, spacing);
        if (ImGuiUtil.DrawDisabledButton("Exact Design", buttonSize, "Add a single, specific design.", _newDesign == null))
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
        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);
        DrawTotalPreview(list);
        var currentDefinition = RandomPredicate.GeneratePredicateString(list);
        var definition        = _newDefinition ?? currentDefinition;
        definition = definition.Replace(";", ";\n\t").Replace("{", "{\n\t").Replace("}", "\n}");
        var lines = definition.Count(c => c is '\n');
        if (ImGui.InputTextMultiline("##definition", ref definition, 2000,
                new Vector2(ImGui.GetContentRegionAvail().X, (lines + 1) * ImGui.GetTextLineHeight() + ImGui.GetFrameHeight()),
                ImGuiInputTextFlags.CtrlEnterForNewLine))
            _newDefinition = definition;
        if (ImGui.IsItemDeactivatedAfterEdit() && _newDefinition != null && _newDefinition != currentDefinition)
        {
            var predicates = RandomPredicate.GeneratePredicates(_newDefinition.Replace("\n", string.Empty).Replace("\t", string.Empty));
            _autoDesignManager.ChangeData(_set!, _designIndex, predicates);
            _newDefinition = null;
        }

        if (ImGui.Button("Copy to Clipboard Without Line Breaks", new Vector2(ImGui.GetContentRegionAvail().X, 0)))
        {
            try
            {
                ImGui.SetClipboardText(currentDefinition);
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
        var button = designs.Count > 0
            ? $"All Restrictions Combined Match {designs.Count} Designs"
            : "None of the Restrictions Matches Any Designs";
        ImGuiUtil.DrawDisabledButton(button, new Vector2(ImGui.GetContentRegionAvail().X, 0),
            string.Empty, false, false);
        if (ImGui.IsItemHovered())
            LookupTooltip(designs);
    }

    private void DrawContent(RandomDesign random)
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().WindowPadding.Y + ImGuiHelpers.GlobalScale);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

        var list = random.Predicates.ToList();
        if (list.Count == 0)
        {
            ImGui.TextUnformatted("No Restrictions Set. Selects among all existing Designs.");
        }
        else
        {
            ImGui.TextUnformatted("Select among designs...");
            DrawTable(random, list);
        }

        ImGui.Dummy(Vector2.Zero);
        ImGui.Separator();
        ImGui.Dummy(Vector2.Zero);

        DrawNewButtons(list);
        DrawManualInput(list);
    }

    private void OnAutomationChange(AutomationChanged.Type type, AutoDesignSet? set, object? data)
    {
        if (set != _set || _set == null)
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
