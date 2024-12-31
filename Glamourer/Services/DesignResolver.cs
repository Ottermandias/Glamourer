using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Glamourer.Designs;
using Glamourer.Designs.Special;
using Glamourer.Gui;
using Glamourer.Gui.Tabs.DesignTab;
using ImGuiNET;
using OtterGui.Services;
using OtterGui.Classes;

namespace Glamourer.Services;

public class DesignResolver(
    DesignFileSystemSelector designSelector,
    QuickDesignCombo quickDesignCombo,
    DesignConverter converter,
    DesignManager manager,
    DesignFileSystem designFileSystem,
    RandomDesignGenerator randomDesign,
    IChatGui chat) : IService
{
    private const string RandomString = "random";

    public bool GetDesign(string argument, [NotNullWhen(true)] out DesignBase? design, bool allowSpecial)
    {
        if (GetDesign(argument, out design, out var error, out var message, allowSpecial))
        {
            if (message != null)
                chat.Print(message);
            return true;
        }

        if (error != null)
            chat.Print(error);
        return false;
    }

    public bool GetDesign(string argument, [NotNullWhen(true)] out DesignBase? design, out SeString? error, out SeString? message,
        bool allowSpecial)
    {
        design  = null;
        error   = null;
        message = null;

        if (argument.Length == 0)
            return false;

        if (allowSpecial)
        {
            if (string.Equals("selection", argument, StringComparison.OrdinalIgnoreCase))
                return GetSelectedDesign(ref design, ref error);

            if (string.Equals("quick", argument, StringComparison.OrdinalIgnoreCase))
                return GetQuickDesign(ref design, ref error);

            if (string.Equals("clipboard", argument, StringComparison.OrdinalIgnoreCase))
                return GetClipboardDesign(ref design, ref error);

            if (argument.StartsWith(RandomString, StringComparison.OrdinalIgnoreCase))
                return GetRandomDesign(argument, ref design, ref error, ref message);
        }

        return GetStandardDesign(argument, ref design, ref error);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetSelectedDesign(ref DesignBase? design, ref SeString? error)
    {
        design = designSelector.Selected;
        if (design != null)
            return true;

        error = "You do not have selected any design in the Designs Tab.";
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetQuickDesign(ref DesignBase? design, ref SeString? error)
    {
        design = quickDesignCombo.Design as Design;
        if (design != null)
            return true;

        error = "You do not have selected any design in the Quick Design Bar.";
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetClipboardDesign(ref DesignBase? design, ref SeString? error)
    {
        try
        {
            var clipboardText = ImGui.GetClipboardText();
            if (clipboardText.Length > 0)
                design = converter.FromBase64(clipboardText, true, true, out _);
        }
        catch
        {
            // ignored
        }

        if (design != null)
            return true;

        error = "Your current clipboard did not contain a valid design string.";
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetRandomDesign(string argument, ref DesignBase? design, ref SeString? error, ref SeString? message)
    {
        try
        {
            if (argument.Length == RandomString.Length)
                design = randomDesign.Design();
            else if (argument[RandomString.Length] == ':')
                design = randomDesign.Design(argument[(RandomString.Length + 1)..]);
            if (design == null)
            {
                error = "No design matched your restrictions.";
                return false;
            }

            message = $"Chose random design {((Design)design).Name}.";
        }
        catch (Exception ex)
        {
            error = $"Error in the restriction string: {ex.Message}";
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetStandardDesign(string argument, ref DesignBase? design, ref SeString? error)
    {
        // As Guid
        if (Guid.TryParse(argument, out var guid))
        {
            design = manager.Designs.ByIdentifier(guid);
        }
        else
        {
            var lower = argument.ToLowerInvariant();
            // Search for design by name and partial identifier.
            design = manager.Designs.FirstOrDefault(MatchNameAndIdentifier(lower));
            // Search for design by path, if nothing was found.
            if (design == null && designFileSystem.Find(lower, out var child) && child is DesignFileSystem.Leaf leaf)
                design = leaf.Value;
        }

        if (design != null)
            return true;

        error = new SeStringBuilder().AddText("The token ").AddYellow(argument, true).AddText(" did not resolve to an existing design.")
            .BuiltString;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<Design, bool> MatchNameAndIdentifier(string lower)
    {
        // Check for names and identifiers, prefer names
        if (lower.Length > 3)
            return d => d.Name.Lower == lower || d.Identifier.ToString().StartsWith(lower);

        // Check only for names.
        return d => d.Name.Lower == lower;
    }
}
