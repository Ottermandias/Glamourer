using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using Luna;
using Newtonsoft.Json.Linq;
using Penumbra.Api.Preset;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;

namespace Glamourer.Services;

public class PcpService : IRequiredService, IDisposable
{
    private readonly Configuration      _config;
    private readonly PenumbraSubscriber _penumbra;
    private readonly ActorObjectManager _objects;
    private readonly StateManager       _state;
    private readonly DesignConverter    _designConverter;
    private readonly DesignManager      _designManager;
    private          bool               _attached;

    public PcpService(Configuration config, PenumbraSubscriber penumbra, ActorObjectManager objects, StateManager state,
        DesignConverter designConverter, DesignManager designManager)
    {
        _config          = config;
        _penumbra        = penumbra;
        _objects         = objects;
        _state           = state;
        _designConverter = designConverter;
        _designManager   = designManager;

        _config.PcpChanged += Set;
        Set(_config.AttachToPcp, false);
    }

    public void CleanPcpDesigns()
    {
        var designs = _designManager.Designs.Where(d => d.Tags.Contains("PCP")).ToList();
        Glamourer.Log.Information($"[PCPService] Deleting {designs.Count} designs containing the tag PCP.");
        foreach (var design in designs)
            _designManager.Delete(design);
    }

    public void Set(bool newValue, bool _)
    {
        if (newValue)
        {
            if (_attached)
                return;

            Glamourer.Log.Information("[PCPService] Attached to PCP handling.");
            _penumbra.Pcp.Created += OnCreation;
            _penumbra.Pcp.Parsed  += OnParse;
            _attached             =  true;
        }
        else
        {
            if (!_attached)
                return;

            Glamourer.Log.Information("[PCPService] Detached from PCP handling.");
            _penumbra.Pcp.Created -= OnCreation;
            _penumbra.Pcp.Parsed  -= OnParse;
            _attached             =  false;
        }
    }

    private void OnParse(JObject jObj, string modDirectory, Guid collection)
    {
        Glamourer.Log.Debug("[PCPService] Parsing PCP file.");
        if (jObj["Glamourer"] is not JObject glamourer)
            return;

        if (glamourer["Version"]!.ToObject<int>() is not 1)
            return;

        var element = (glamourer["Design"] as JObject)?.ToElement();
        if (_designConverter.FromJsonElement(element, true, true) is not { } designBase)
            return;

        var actorIdentifier = _objects.Actors.FromJson(jObj["Actor"] as JObject);
        if (!actorIdentifier.IsValid)
            return;

        var time = new DateTimeOffset(jObj["Time"]?.ToObject<DateTime>() ?? DateTime.UtcNow);
        var design = _designManager.CreateClone(designBase,
            $"{_config.PcpFolder}/{actorIdentifier} - {jObj["Note"]?.ToObject<string>() ?? string.Empty}", true);
        _designManager.AddTag(design, "PCP");
        _designManager.SetWriteProtection(design, true);
        _designManager.AddMod(design, new ModIdentifier(modDirectory, modDirectory), SettingPresetData.Empty);
        _designManager.ChangeDescription(design, $"PCP design created for {actorIdentifier} on {time}.");
        _designManager.ChangeResetAdvancedDyes(design, ModelCombinedSlotsExtensions.All);
        _designManager.SetQuickDesign(design, false);
        _designManager.ChangeColor(design, _config.PcpColor);

        Glamourer.Log.Debug("[PCPService] Created PCP design.");
        if (_state.GetOrCreate(actorIdentifier, _objects.TryGetValue(actorIdentifier, out var data) ? data.Objects[0] : Actor.Null,
                out var state))
        {
            _state.ApplyDesign(state, design, ApplySettings.Manual);
            Glamourer.Log.Debug($"[PCPService] Applied PCP design to {actorIdentifier.Incognito(null)}");
        }
    }

    private void OnCreation(JObject jObj, ushort index, string path)
    {
        Glamourer.Log.Debug("[PCPService] Adding Glamourer data to PCP file.");
        var actorIdentifier = _objects.Actors.FromJson(jObj["Actor"] as JObject);
        if (!actorIdentifier.IsValid)
            return;

        if (!_state.GetOrCreate(actorIdentifier, _objects.Objects[(int)index], out var state))
        {
            Glamourer.Log.Debug($"[PCPService] Could not get or create state for actor {index}.");
            return;
        }

        var design = _designConverter.Convert(state, ApplicationRules.All);
        jObj["Glamourer"] = new JObject
        {
            ["Version"] = 1,
            ["Design"]  = design.ToObject() as JObject,
        };
    }

    public void Dispose()
    {
        _config.PcpChanged -= Set;
        Set(false, false);
    }
}
