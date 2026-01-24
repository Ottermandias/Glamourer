using Glamourer.Automation;
using Glamourer.Designs;
using OtterGui.Services;

namespace Glamourer.Interop.Penumbra;

public sealed class ModUsageInformer : IDisposable, IRequiredService
{
    private readonly PenumbraService   _penumbra;
    private readonly DesignManager     _designs;
    private readonly AutoDesignManager _automation;

    public ModUsageInformer(PenumbraService penumbra, DesignManager designs, AutoDesignManager automation)
    {
        _penumbra   = penumbra;
        _designs    = designs;
        _automation = automation;

        _penumbra.ModUsageQueried += OnModUsageQueried;
    }

    private void OnModUsageQueried(string modPath, string modName, Dictionary<Assembly, (bool, string)> notes)
    {
        var sb    = new StringBuilder();
        var inUse = false;
        foreach (var design in _designs.Designs)
        {
            if (!design.AssociatedMods.Any(p => ModMatches(modPath, modName, p.Key)))
                continue;

            sb.AppendLine($"Contained in Design {design.Name}.");
            if (_automation.EnabledSets.Values.Any(s => s.Designs.Any(d => d.Type is not 0 && d.Design == design)))
            {
                inUse = true;
                break;
            }
        }

        if (inUse)
            notes.TryAdd(Assembly.GetAssembly(typeof(ModUsageInformer))!, (true, string.Empty));
        else if (sb.Length > 0)
            notes.TryAdd(Assembly.GetAssembly(typeof(ModUsageInformer))!, (false, sb.ToString()));
    }

    private static bool ModMatches(string modPath, string modName, in Mod mod)
    {
        if (mod.DirectoryName.Equals(modPath, StringComparison.OrdinalIgnoreCase))
            return true;

        if (mod.Name.Equals(modName, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public void Dispose()
        => _penumbra.ModUsageQueried -= OnModUsageQueried;
}
