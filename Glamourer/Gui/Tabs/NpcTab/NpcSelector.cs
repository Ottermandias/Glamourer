using Glamourer.GameData;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui.Extensions;
using OtterGui.Raii;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Glamourer.Gui.Tabs.NpcTab;

public class NpcSelector : IDisposable
{
    private readonly NpcCustomizeSet _npcs;
    private readonly LocalNpcAppearanceData _favorites;

    private          NpcFilter _filter;
    private readonly List<int> _visibleOrdered = [];
    private          int       _selectedGlobalIndex;
    private          bool      _listDirty = true;
    private          Vector2   _defaultItemSpacing;
    private          float     _width;


    public NpcSelector(NpcCustomizeSet npcs, LocalNpcAppearanceData favorites)
    {
        _npcs                  =  npcs;
        _favorites             =  favorites;
        _filter                =  new NpcFilter(_favorites);
        _favorites.DataChanged += OnFavoriteChange;
    }

    public void Dispose()
    {
        _favorites.DataChanged -= OnFavoriteChange;
    }

    private void OnFavoriteChange()
        => _listDirty = true;

    public void UpdateList()
    {
        if (!_listDirty)
            return;

        _listDirty = false;
        _visibleOrdered.Clear();
        var enumerable = _npcs.WithIndex();
        if (!_filter.IsEmpty)
            enumerable = enumerable.Where(d => _filter.ApplyFilter(d.Value));
        var range = enumerable.OrderByDescending(d => _favorites.IsFavorite(d.Value))
            .ThenBy(d => d.Index)
            .Select(d => d.Index);
        _visibleOrdered.AddRange(range);
    }

    public bool HasSelection
        => _selectedGlobalIndex >= 0 && _selectedGlobalIndex < _npcs.Count;

    public NpcData Selection
        => HasSelection ? _npcs[_selectedGlobalIndex] : default;

    public void Draw(float width)
    {
        _width = width;
        using var group = ImRaii.Group();
        _defaultItemSpacing = ImGui.GetStyle().ItemSpacing;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);

        if (_filter.Draw(width))
            _listDirty = true;
        UpdateList();
        DrawSelector();
    }

    private void DrawSelector()
    {
        using var child = ImRaii.Child("##Selector", new Vector2(_width, ImGui.GetContentRegionAvail().Y), true);
        if (!child)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, _defaultItemSpacing);
        ImGuiClip.ClippedDraw(_visibleOrdered, DrawSelectable, ImGui.GetTextLineHeight());
    }

    private void DrawSelectable(int globalIndex)
    {
        using var id    = ImRaii.PushId(globalIndex);
        using var color = ImGuiColor.Text.Push(_favorites.GetData(_npcs[globalIndex]).Color);
        if (ImGui.Selectable(_npcs[globalIndex].Name, _selectedGlobalIndex == globalIndex, ImGuiSelectableFlags.AllowItemOverlap))
            _selectedGlobalIndex = globalIndex;
    }
}
