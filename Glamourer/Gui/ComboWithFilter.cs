using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace Glamourer.Gui
{
    public class ComboWithFilter<T>
    {
        private readonly string                       _label;
        private readonly string                       _filterLabel;
        private readonly string                       _listLabel;
        private          string                       _currentFilter      = string.Empty;
        private          string                       _currentFilterLower = string.Empty;
        private          bool                         _focus;
        private readonly float                        _size;
        private          float                        _previewSize;
        private readonly IReadOnlyList<T>             _items;
        private readonly IReadOnlyList<(string, int)> _itemNamesLower;
        private readonly Func<T, string>              _itemToName;
        private          IReadOnlyList<(string, int)> _currentItemNames;
        private          bool                         _needsClear;

        public Action?        PrePreview;
        public Action?        PostPreview;
        public Func<T, bool>? CreateSelectable;
        public Action?        PreList;
        public Action?        PostList;
        public float?         HeightPerItem;

        private float _heightPerItem;

        public ImGuiComboFlags Flags       { get; set; } = ImGuiComboFlags.None;
        public int             ItemsAtOnce { get; set; } = 12;

        private void UpdateFilter(string newFilter)
        {
            if (newFilter == _currentFilter)
                return;

            var lower = newFilter.ToLowerInvariant();
            if (_currentFilterLower.Any() && lower.Contains(_currentFilterLower))
                _currentItemNames = _currentItemNames.Where(p => p.Item1.Contains(lower)).ToArray();
            else if (lower.Any())
                _currentItemNames = _itemNamesLower.Where(p => p.Item1.Contains(lower)).ToArray();
            else
                _currentItemNames = _itemNamesLower;
            _currentFilter      = newFilter;
            _currentFilterLower = lower;
        }

        public ComboWithFilter(string label, float size, float previewSize, IReadOnlyList<T> items, Func<T, string> itemToName)
        {
            _label       = label;
            _filterLabel = $"##_{label}_filter";
            _listLabel   = $"##_{label}_list";
            _itemToName  = itemToName;
            _items       = items;
            _size        = size;
            _previewSize = previewSize;

            _itemNamesLower   = _items.Select((i, idx) => (_itemToName(i).ToLowerInvariant(), idx)).ToArray();
            _currentItemNames = _itemNamesLower;
        }

        public ComboWithFilter(string label, ComboWithFilter<T> other)
        {
            _label            = label;
            _filterLabel      = $"##_{label}_filter";
            _listLabel        = $"##_{label}_list";
            _itemToName       = other._itemToName;
            _items            = other._items;
            _itemNamesLower   = other._itemNamesLower;
            _currentItemNames = other._currentItemNames;
            _size             = other._size;
            _previewSize      = other._previewSize;
            PrePreview        = other.PrePreview;
            PostPreview       = other.PostPreview;
            CreateSelectable  = other.CreateSelectable;
            PreList           = other.PreList;
            PostList          = other.PostList;
            HeightPerItem     = other.HeightPerItem;
            Flags             = other.Flags;
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

                for (var i = scrollY; i < _currentItemNames.Count; ++i)
                {
                    if (++numItems > ItemsAtOnce + 2)
                        continue;

                    nodeIdx = _currentItemNames[i].Item2;
                    var  item = _items[nodeIdx]!;
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

                if (_currentItemNames.Count > ItemsAtOnce + 2)
                    ImGui.Dummy(Vector2.UnitY * (_currentItemNames.Count - ItemsAtOnce - 2 - scrollY) * _heightPerItem);
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
                if (_needsClear)
                {
                    _needsClear = false;
                    _focus      = false;
                    UpdateFilter(string.Empty);
                }

                PostPreview?.Invoke();
                return false;
            }

            _needsClear = true;
            PostPreview?.Invoke();

            _heightPerItem = HeightPerItem ?? ImGui.GetTextLineHeightWithSpacing();

            bool ret;
            try
            {
                ImGui.SetNextItemWidth(-1);
                var tmp = _currentFilter;
                if (ImGui.InputTextWithHint(_filterLabel, "Filter...", ref tmp, 255))
                    UpdateFilter(tmp);

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
