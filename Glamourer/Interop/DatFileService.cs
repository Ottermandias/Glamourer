using System;
using System.IO;
using System.Linq;
using Dalamud.Interface.DragDrop;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui.Classes;

namespace Glamourer.Interop;

public class DatFileService
{
    private readonly CustomizationService   _customizations;
    private readonly CustomizeUnlockManager _unlocks;
    private readonly IDragDropManager       _dragDropManager;

    public DatFileService(CustomizationService customizations, CustomizeUnlockManager unlocks, IDragDropManager dragDropManager)
    {
        _customizations  = customizations;
        _unlocks         = unlocks;
        _dragDropManager = dragDropManager;
    }

    public void CreateSource()
    {
        _dragDropManager.CreateImGuiSource("DatDragger", m => m.Files.Count == 1 && m.Extensions.Contains(".dat"), m =>
        {
            ImGui.TextUnformatted($"Dragging {Path.GetFileName(m.Files[0])} to import customizations for Glamourer...");
            return true;
        });
    }

    public bool CreateImGuiTarget(out DatCharacterFile file)
    {
        if (!_dragDropManager.CreateImGuiTarget("DatDragger", out var files, out _) || files.Count != 1)
        {
            file = default;
            return false;
        }

        return LoadDesign(files[0], out file);
    }

    public bool LoadDesign(string path, out DatCharacterFile file)
    {
        if (!File.Exists(path))
        {
            file = default;
            return false;
        }

        try
        {
            using var stream = File.OpenRead(path);
            if (!DatCharacterFile.Read(stream, out file))
                return false;

            if (!Verify(file))
                return false;
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not read character data file {path}.", NotificationType.Error);
            file = default;
        }

        return true;
    }

    public bool SaveDesign(string path, in Customize input, string description)
    {
        if (!Verify(input, out var voice))
            return false;

        if (description.Length > 40)
            return false;

        if (path.Length == 0)
            return false;

        try
        {
            var file        = new DatCharacterFile(input, voice, description);
            var directories = Path.GetDirectoryName(path);
            if (directories != null)
                Directory.CreateDirectory(directories);
            using var stream = File.Open(path, File.Exists(path) ? FileMode.Truncate : FileMode.CreateNew);
            file.Write(stream);

            return true;
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not save character data to file {path}.", "Failure", NotificationType.Error);
            return false;
        }
    }

    public bool Verify(in Customize input, out byte voice, byte? inputVoice = null)
    {
        voice = 0;
        if (_customizations.ValidateClan(input.Clan, input.Race, out _, out _).Length > 0)
            return false;
        if (!_customizations.IsGenderValid(input.Race, input.Gender))
            return false;
        if (input.BodyType.Value != 1)
            return false;

        var set = _customizations.AwaitedService.GetList(input.Clan, input.Gender);
        voice = set.Voices[0];
        if (inputVoice.HasValue && !set.Voices.Contains(inputVoice.Value))
            return false;

        foreach (var index in CustomizationExtensions.AllBasic)
        {
            if (!CustomizationService.IsCustomizationValid(set, input.Face, index, input[index]))
                return false;
        }

        if (input[CustomizeIndex.LegacyTattoo].Value != 0)
            return false;

        return true;
    }

    public bool Verify(in DatCharacterFile datFile)
    {
        var customize = datFile.Customize;
        if (!Verify(customize, out _, (byte)datFile.Voice))
            return false;

        if (datFile.Time < DateTimeOffset.UnixEpoch || datFile.Time > DateTimeOffset.UtcNow)
            return false;

        if (datFile.Checksum != datFile.CalculateChecksum())
            return false;

        return true;
    }
}
