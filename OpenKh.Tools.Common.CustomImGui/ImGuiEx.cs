﻿using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;

namespace OpenKh.Tools.Common.CustomImGui
{
    public static class ImGuiEx
    {
        public struct GridElement
        {
            public string Name;
            public float Size;
            public float MaxSize;
            public bool Border;
            public Action Action;

            public GridElement(string name, float size, float maxSize, bool gridVisible, Action action)
            {
                Name = name;
                Size = size;
                MaxSize = maxSize;
                Border = gridVisible;
                Action = action;
            }
        }
        
        public const float FontSizeMultiplier = 96f / 72f;

        public unsafe static void SetWpfStyle()
        {
            ImGuiStyle* pStyle = ImGui.GetStyle();
            pStyle->FrameBorderSize = 1.0f;
            pStyle->FramePadding = new Vector2(7, 5);
            pStyle->ScrollbarRounding = 0.0f;
            pStyle->ScrollbarSize = 20.0f;
            pStyle->WindowRounding = 0.0f;
            pStyle->TabRounding = 0.0f;
            pStyle->TabBorderSize = 1.0f;
            pStyle->WindowMenuButtonPosition = ImGuiDir.Right;
            SetWpfDarkTheme();
        }

        private static unsafe void SetWpfLightTheme()
        {
            const uint Opacity = 0xffu << 24;
            const uint Black = Opacity | 0x000000u;
            const uint White = Opacity | 0xffffffu;
            const uint ActionBg = Opacity | 0xddddddu;
            const uint ActionActive = Opacity | 0xf6e5c4u;
            const uint ActionHovered = Opacity | 0xfde6beu;
            const uint SliderGrab = Opacity | 0xcdcdcdu;
            const uint SliderGrabActive = Opacity | 0xa6a6a6u;
            const uint SlideGrabHovered = Opacity | 0x606060u;
            const uint MenuBg = Opacity | 0xf0f0f0u;
            const uint MenuBgHovered = Opacity | 0xfcf3e9u;

            ImGui.PushStyleColor(ImGuiCol.Text, Black);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, White);
            ImGui.PushStyleColor(ImGuiCol.MenuBarBg, MenuBg);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, White);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ActionHovered);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ActionActive);
            ImGui.PushStyleColor(ImGuiCol.PopupBg, White);
            ImGui.PushStyleColor(ImGuiCol.Button, ActionBg);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ActionHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ActionActive);
            ImGui.PushStyleColor(ImGuiCol.Header, ActionBg);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ActionHovered);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, ActionActive);
            ImGui.PushStyleColor(ImGuiCol.CheckMark, Black);
            ImGui.PushStyleColor(ImGuiCol.Tab, MenuBg);
            ImGui.PushStyleColor(ImGuiCol.TabActive, White);
            ImGui.PushStyleColor(ImGuiCol.TabHovered, MenuBgHovered);
            ImGui.PushStyleColor(ImGuiCol.SliderGrab, SliderGrab);
            ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, SliderGrabActive);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, MenuBg);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, SliderGrab);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, SliderGrabActive);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, SlideGrabHovered);
            ImGui.PushStyleColor(ImGuiCol.TitleBg, SliderGrab);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, White);
        }

        private static unsafe void SetWpfDarkTheme()
        {
            const uint Opacity = 0xffu << 24;
            const uint Black = Opacity | ~0x000000u;
            const uint White = Opacity | ~0xffffffu;
            const uint ActionBg = Opacity | ~0xddddddu;
            const uint ActionActive = Opacity | ~0xf6e5c4u;
            const uint ActionHovered = Opacity | ~0xfde6beu;
            const uint SliderGrab = Opacity | ~0xcdcdcdu;
            const uint SliderGrabActive = Opacity | ~0xa6a6a6u;
            const uint SlideGrabHovered = Opacity | ~0x606060u;
            const uint MenuBg = Opacity | ~0xf0f0f0u;
            const uint MenuBgHovered = Opacity | ~0xfcf3e9u;

            ImGui.PushStyleColor(ImGuiCol.Text, Black);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, White);
            ImGui.PushStyleColor(ImGuiCol.MenuBarBg, MenuBg);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, White);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ActionHovered);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ActionActive);
            ImGui.PushStyleColor(ImGuiCol.PopupBg, White);
            ImGui.PushStyleColor(ImGuiCol.Button, ActionBg);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ActionHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ActionActive);
            ImGui.PushStyleColor(ImGuiCol.Header, ActionBg);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ActionHovered);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, ActionActive);
            ImGui.PushStyleColor(ImGuiCol.CheckMark, Black);
            ImGui.PushStyleColor(ImGuiCol.Tab, MenuBg);
            ImGui.PushStyleColor(ImGuiCol.TabActive, White);
            ImGui.PushStyleColor(ImGuiCol.TabHovered, MenuBgHovered);
            ImGui.PushStyleColor(ImGuiCol.SliderGrab, SliderGrab);
            ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, SliderGrabActive);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, MenuBg);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, SliderGrab);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, SliderGrabActive);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, SlideGrabHovered);
            ImGui.PushStyleColor(ImGuiCol.TitleBg, SliderGrab);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, White);
        }

        public static ImFontPtr OpenFont(IImGuiRenderer renderer, string internalFontName, float fontSize)
        {
            var font = ImGui.GetIO().Fonts.AddFontFromFileTTF(@$"C:\Windows\Fonts\{internalFontName}.ttf", fontSize);
            renderer.RebuildFontAtlas();

            return font;
        }

        public static ImFontPtr OpenFontSegoeUi(IImGuiRenderer renderer, float fontSize = 16.0f) =>
            OpenFont(renderer, "segoeui", fontSize);

        public static bool MainWindow(Action action, bool noBackground = false) =>
            ForControl(() =>
            {
                var ret = ImGui.Begin("MainWindow",
                    ImGuiWindowFlags.MenuBar |
                    ImGuiWindowFlags.NoDecoration |
                    ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.NoMove |
                    (noBackground ? ImGuiWindowFlags.NoBackground : ImGuiWindowFlags.None));
                ImGui.SetWindowPos(new System.Numerics.Vector2(0, 0));
                ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);
                return ret;
            }, ImGui.End, action);

        public static bool ForControl(Func<bool> begin, Action end, Action action)
        {
            if (!begin()) return false;
            action();
            end();

            return true;
        }

        public static bool ForControl(Func<bool> begin, Action action)
        {
            if (!begin()) return false;
            action();

            return true;
        }

        public static bool ForMenuBar(Action action) =>
            ForControl(() => ImGui.BeginMenuBar(), ImGui.EndMenuBar, action);

        public static bool ForMenu(string name, Action action) =>
            ForControl(() => ImGui.BeginMenu(name), ImGui.EndMenu, action);

        public static bool ForMenuItem(string name, Action action, bool enabled = true) =>
            ForControl(() => ImGui.MenuItem(name, enabled), action);

        public static bool ForMenuItem(string name, string shortcut, Action action, bool enabled = true) =>
            ForControl(() => ImGui.MenuItem(name, shortcut, false, enabled), action);

        public static bool ForPopup(string name, Action action) =>
            ForControl(() => ImGui.BeginPopup(name), ImGui.EndPopup, action);

        public static bool ForWindow(string name, Action action) =>
            ForControl(() =>
            {
                var dummy = true;
                return ImGui.Begin(name, ref dummy);
            }, ImGui.End, action);

        public static void ForEdit(string name, Func<bool> getter, Action<bool> setter)
        {
            var value = getter();
            if (ImGui.Checkbox(name, ref value))
                setter(value);
        }

        public static void ForEdit(string name, Func<float> getter, Action<float> setter)
        {
            var value = getter();
            if (ImGui.DragFloat(name, ref value))
                setter(value);
        }

        public static void ForEdit2(string name, Func<Vector2> getter, Action<Vector2> setter)
        {
            var value = getter();
            if (ImGui.DragFloat2(name, ref value))
                setter(value);
        }

        public static void ForEdit3(string name, Func<Vector3> getter, Action<Vector3> setter)
        {
            var value = getter();
            if (ImGui.DragFloat3(name, ref value))
                setter(value);
        }

        public static bool ForChild(string name, float w, float h, bool border, Action action)
        {
            var ret = ImGui.BeginChild(name, new Vector2(w, h), border);
            action();
            ImGui.EndChild();

            return ret;
        }
    }
}
