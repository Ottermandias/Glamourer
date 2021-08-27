using System;
using Dalamud.Logging;
using ImGuiNET;

namespace Glamourer.FileSystem
{
    public static class FileSystemImGui
    {
        public const string DraggedObjectLabel = "FSDrag";

        private static unsafe bool IsDropping(string name)
            => ImGui.AcceptDragDropPayload(name).NativePtr != null;

        private static IFileSystemBase? _draggedObject;

        public static bool DragDropTarget(FileSystem fs, IFileSystemBase child, out string oldPath, out IFileSystemBase? draggedChild)
        {
            oldPath      = string.Empty;
            draggedChild = null;
            var ret = false;
            if (!ImGui.BeginDragDropTarget())
                return ret;

            if (IsDropping(DraggedObjectLabel))
            {
                if (_draggedObject != null)
                    try
                    {
                        oldPath      = _draggedObject.FullName();
                        draggedChild = _draggedObject;
                        ret          = fs.Move(_draggedObject, child.IsFolder(out var folder) ? folder : child.Parent, false);
                    }
                    catch (Exception e)
                    {
                        PluginLog.Error($"Could not drag {_draggedObject.Name} onto {child.FullName()}:\n{e}");
                    }

                _draggedObject = null;
            }

            ImGui.EndDragDropTarget();
            return ret;
        }

        public static void DragDropSource(IFileSystemBase child)
        {
            if (!ImGui.BeginDragDropSource())
                return;

            ImGui.SetDragDropPayload(DraggedObjectLabel, IntPtr.Zero, 0);
            ImGui.Text($"Moving {child.Name}...");
            _draggedObject = child;
            ImGui.EndDragDropSource();
        }
    }
}
