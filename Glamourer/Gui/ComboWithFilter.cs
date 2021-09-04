using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace Glamourer.Gui
{
    public class ComboWithFilter<T>
    {
        private readonly string                _label;
        private readonly string                _filterLabel;
        private readonly string                _listLabel;
        private          string                _currentFilter      = string.Empty;
        private          string                _currentFilterLower = string.Empty;
        private          bool                  _focus;
        private readonly float                 _size;
        private          float                 _previewSize;
        private readonly IReadOnlyList<T>      _items;
        private readonly IReadOnlyList<string> _itemNamesLower;
        private readonly Func<T, string>       _itemToName;

        public Action?        PrePreview;
        public Action?        PostPreview;
        public Func<T, bool>? CreateSelectable;
        public Action?        PreList;
        public Action?        PostList;
        public float?         HeightPerItem;

        private float _heightPerItem;

        public ImGuiComboFlags Flags       { get; set; } = ImGuiComboFlags.None;
        public int             ItemsAtOnce { get; set; } = 12;

        public ComboWithFilter(string label, float size, float previewSize, IReadOnlyList<T> items, Func<T, string> itemToName)
        {
            _label       = label;
            _filterLabel = $"##_{label}_filter";
            _listLabel   = $"##_{label}_list";
            _itemToName  = itemToName;
            _items       = items;
            _size        = size;
            _previewSize = previewSize;

            _itemNamesLower = _items.Select(i => _itemToName(i).ToLowerInvariant()).ToList();
        }

        public ComboWithFilter(string label, ComboWithFilter<T> other)
        {
            _label           = label;
            _filterLabel     = $"##_{label}_filter";
            _listLabel       = $"##_{label}_list";
            _itemToName      = other._itemToName;
            _items           = other._items;
            _itemNamesLower  = other._itemNamesLower;
            _size            = other._size;
            _previewSize     = other._previewSize;
            PrePreview       = other.PrePreview;
            PostPreview      = other.PostPreview;
            CreateSelectable = other.CreateSelectable;
            PreList          = other.PreList;
            PostList         = other.PostList;
            HeightPerItem    = other.HeightPerItem;
            Flags            = other.Flags;
        }

        private bool DrawList(string currentName, out int numItems, out int nodeIdx, ref T? value)
        {
            numItems = ItemsAtOnce;
            nodeIdx  = -1;
            if (!ImGui.BeginChild(_listLabel, new Vector2(_size, ItemsAtOnce * _heightPerItem)))
            {
                ImGui.EndChild();
                return false;
            }

            var ret = false;
            try
            {
                if (!_focus)
                {
                    ImGui.SetScrollY(0);
                    _focus = true;
                }


                var scrollY    = Math.Max((int) (ImGui.GetScrollY() / _heightPerItem) - 1, 0);
                var restHeight = scrollY * _heightPerItem;
                numItems = 0;
                nodeIdx  = 0;

                if (restHeight > 0)
                    ImGui.Dummy(Vector2.UnitY * restHeight);

                for (var i = scrollY; i < _items.Count; ++i)
                {
                    if (!_itemNamesLower[i].Contains(_currentFilterLower))
                        continue;

                    ++numItems;
                    if (numItems <= ItemsAtOnce + 2)
                    {
                        nodeIdx = i;
                        var item    = _items[i]!;
                        bool success;
                        if (CreateSelectable != null)
                        {
                            success = CreateSelectable(item);
                        }
                        else
                        {
                            var name = _itemToName(item);
                            success = ImGui.Selectable(name, name == currentName);
                        }

                        if (success)
                        {
                            value = item;
                            ImGui.CloseCurrentPopup();
                            ret = true;
                        }
                    }
                }

                if (numItems > ItemsAtOnce + 2)
                    ImGui.Dummy(Vector2.UnitY * (numItems - ItemsAtOnce - 2) * _heightPerItem);
            }
            finally
            {
                ImGui.EndChild();
            }

            return ret;
        }

        public bool Draw(string currentName, out T? value, float? size = null)
        {
            if (size.HasValue)
                _previewSize = size.Value;

            value = default;
            ImGui.SetNextItemWidth(_previewSize);
            PrePreview?.Invoke();
            if (!ImGui.BeginCombo(_label, currentName, Flags))
            {
                _focus              = false;
                _currentFilter      = string.Empty;
                _currentFilterLower = string.Empty;
                PostPreview?.Invoke();
                return false;
            }

            PostPreview?.Invoke();

            _heightPerItem = HeightPerItem ?? ImGui.GetTextLineHeightWithSpacing();

            bool ret;
            try
            {
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputTextWithHint(_filterLabel, "Filter...", ref _currentFilter, 255))
                    _currentFilterLower = _currentFilter.ToLowerInvariant();

                var isFocused = ImGui.IsItemActive();
                if (!_focus)
                    ImGui.SetKeyboardFocusHere();

                PreList?.Invoke();
                ret = DrawList(currentName, out var numItems, out var nodeIdx, ref value);
                PostList?.Invoke();

                if (!isFocused && numItems <= 1 && nodeIdx >= 0)
                {
                    value = _items[nodeIdx];
                    ret   = true;
                    ImGui.CloseCurrentPopup();
                }
            }
            finally
            {
                ImGui.EndCombo();
            }

            return ret;
        }
    }
}
