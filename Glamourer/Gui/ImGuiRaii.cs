using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace Glamourer.Gui
{
    public sealed class ImGuiRaii : IDisposable
    {
        private int   _colorStack;
        private int   _fontStack;
        private int   _styleStack;
        private float _indentation;

        private Stack<Action>? _onDispose;

        public static ImGuiRaii NewGroup()
            => new ImGuiRaii().Group();

        public ImGuiRaii Group()
            => Begin(ImGui.BeginGroup, ImGui.EndGroup);

        public static ImGuiRaii NewTooltip()
            => new ImGuiRaii().Tooltip();

        public ImGuiRaii Tooltip()
            => Begin(ImGui.BeginTooltip, ImGui.EndTooltip);

        public ImGuiRaii PushColor(ImGuiCol which, uint color)
        {
            ImGui.PushStyleColor(which, color);
            ++_colorStack;
            return this;
        }

        public ImGuiRaii PushColor(ImGuiCol which, Vector4 color)
        {
            ImGui.PushStyleColor(which, color);
            ++_colorStack;
            return this;
        }

        public ImGuiRaii PopColors(int n = 1)
        {
            var actualN = Math.Min(n, _colorStack);
            if (actualN > 0)
            {
                ImGui.PopStyleColor(actualN);
                _colorStack -= actualN;
            }

            return this;
        }

        public ImGuiRaii PushStyle(ImGuiStyleVar style, Vector2 value)
        {
            ImGui.PushStyleVar(style, value);
            ++_styleStack;
            return this;
        }

        public ImGuiRaii PushStyle(ImGuiStyleVar style, float value)
        {
            ImGui.PushStyleVar(style, value);
            ++_styleStack;
            return this;
        }

        public ImGuiRaii PopStyles(int n = 1)
        {
            var actualN = Math.Min(n, _styleStack);
            if (actualN > 0)
            {
                ImGui.PopStyleVar(actualN);
                _styleStack -= actualN;
            }

            return this;
        }

        public ImGuiRaii PushFont(ImFontPtr font)
        {
            ImGui.PushFont(font);
            ++_fontStack;
            return this;
        }

        public ImGuiRaii PopFonts(int n = 1)
        {
            var actualN = Math.Min(n, _fontStack);

            while (actualN-- > 0)
            {
                ImGui.PopFont();
                --_fontStack;
            }

            return this;
        }

        public ImGuiRaii Indent(float width)
        {
            if (width != 0)
            {
                ImGui.Indent(width);
                _indentation += width;
            }

            return this;
        }

        public ImGuiRaii Unindent(float width)
            => Indent(-width);

        public bool Begin(Func<bool> begin, Action end)
        {
            if (begin())
            {
                _onDispose ??= new Stack<Action>();
                _onDispose.Push(end);
                return true;
            }

            return false;
        }

        public ImGuiRaii Begin(Action begin, Action end)
        {
            begin();
            _onDispose ??= new Stack<Action>();
            _onDispose.Push(end);
            return this;
        }

        public void End(int n = 1)
        {
            var actualN = Math.Min(n, _onDispose?.Count ?? 0);
            while (actualN-- > 0)
                _onDispose!.Pop()();
        }

        public void Dispose()
        {
            Unindent(_indentation);
            PopColors(_colorStack);
            PopStyles(_styleStack);
            PopFonts(_fontStack);
            if (_onDispose != null)
            {
                End(_onDispose.Count);
                _onDispose = null;
            }
        }
    }
}
