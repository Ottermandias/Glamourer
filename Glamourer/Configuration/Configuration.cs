using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using Glamourer.Gui;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Services;
using ImSharp;
using Luna;
using Newtonsoft.Json;
using OtterGui.Filesystem;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Glamourer.Configuration;

public class Configuration : IPluginConfiguration, ISavable
{
    [JsonIgnore]
    public readonly EphemeralConfig Ephemeral;

    [JsonIgnore]
    public readonly UiConfig Ui;

    public bool   AttachToPcp                      { get; set; } = true;
    public bool   UseRestrictedGearProtection      { get; set; } = false;
    public bool   OpenFoldersByDefault             { get; set; } = false;
    public bool   AutoRedrawEquipOnChanges         { get; set; } = false;
    public bool   EnableAutoDesigns                { get; set; } = true;
    public bool   HideApplyCheckmarks              { get; set; } = false;
    public bool   SmallEquip                       { get; set; } = false;
    public bool   UnlockedItemMode                 { get; set; } = false;
    public byte   DisableFestivals                 { get; set; } = 1;
    public bool   EnableGameContextMenu            { get; set; } = true;
    public bool   HideWindowInCutscene             { get; set; } = false;
    public bool   ShowAutomationSetEditing         { get; set; } = true;
    public bool   ShowAllAutomatedApplicationRules { get; set; } = true;
    public bool   ShowUnlockedItemWarnings         { get; set; } = true;
    public bool   RevertManualChangesOnZoneChange  { get; set; } = false;
    public bool   ShowQuickBarInTabs               { get; set; } = true;
    public bool   OpenWindowAtStart                { get; set; } = false;
    public bool   ShowWindowWhenUiHidden           { get; set; } = false;
    public bool   KeepAdvancedDyesAttached         { get; set; } = true;
    public bool   ShowPalettePlusImport            { get; set; } = true;
    public bool   UseFloatForColors                { get; set; } = true;
    public bool   UseRgbForColors                  { get; set; } = true;
    public bool   ShowColorConfig                  { get; set; } = true;
    public bool   ChangeEntireItem                 { get; set; } = false;
    public bool   AlwaysApplyAssociatedMods        { get; set; } = true;
    public bool   UseTemporarySettings             { get; set; } = true;
    public bool   AllowDoubleClickToApply          { get; set; } = false;
    public bool   RespectManualOnAutomationUpdate  { get; set; } = false;
    public bool   PreventRandomRepeats             { get; set; } = false;
    public string PcpFolder                        { get; set; } = "PCP";
    public string PcpColor                         { get; set; } = "";

    public DesignPanelFlag HideDesignPanel       { get; set; } = 0;
    public DesignPanelFlag AutoExpandDesignPanel { get; set; } = 0;

    public DefaultDesignSettings DefaultDesignSettings { get; set; } = new();

    public HeightDisplayType    HeightDisplayType    { get; set; } = HeightDisplayType.Centimetre;
    public RenameField          ShowRename           { get; set; } = RenameField.BothDataPrio;
    public ModifiableHotkey     ToggleQuickDesignBar { get; set; } = new(VirtualKey.NO_KEY);
    public DoubleModifier       DeleteDesignModifier { get; set; } = new(ModifierHotkey.Control, ModifierHotkey.Shift);
    public DoubleModifier       IncognitoModifier    { get; set; } = new(ModifierHotkey.Control);
    public ChangeLogDisplayType ChangeLogDisplayType { get; set; } = ChangeLogDisplayType.New;

    public QdbButtons QdbButtons { get; set; } =
        QdbButtons.ApplyDesign | QdbButtons.RevertAll | QdbButtons.RevertAutomation | QdbButtons.RevertAdvancedDyes;

    [JsonConverter(typeof(SortModeConverter))]
    [JsonProperty(Order = int.MaxValue)]
    public ISortMode<Design> SortMode { get; set; } = ISortMode<Design>.FoldersFirst;

    public List<(string Code, bool Enabled)> Codes { get; set; } = [];

#if DEBUG
    public bool DebugMode { get; set; } = true;
#else
    public bool DebugMode { get; set; } = false;
#endif

    public int Version { get; set; } = Constants.CurrentVersion;

    public Dictionary<ColorId, uint> Colors { get; private set; }
        = ColorId.Values.ToDictionary(c => c, c => c.Data().DefaultColor);

    [JsonIgnore]
    private readonly SaveService _saveService;

    public Configuration(SaveService saveService, ConfigMigrationService migrator, EphemeralConfig ephemeral, UiConfig ui)
    {
        _saveService = saveService;
        Ephemeral    = ephemeral;
        Ui           = ui;
        Load(migrator);
    }

    public void Save()
        => _saveService.DelaySave(this);

    private void Load(ConfigMigrationService migrator)
    {
        if (!File.Exists(_saveService.FileNames.ConfigurationFile))
            return;

        if (File.Exists(_saveService.FileNames.ConfigurationFile))
            try
            {
                var text = File.ReadAllText(_saveService.FileNames.ConfigurationFile);
                JsonConvert.PopulateObject(text, this, new JsonSerializerSettings
                {
                    Error = HandleDeserializationError,
                });
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex,
                    "Error reading Configuration, reverting to default.\nYou may be able to restore your configuration using the rolling backups in the XIVLauncher/backups/Glamourer directory.",
                    "Error reading Configuration", NotificationType.Error);
            }

        migrator.Migrate(this);
        return;

        static void HandleDeserializationError(object? sender, ErrorEventArgs errorArgs)
        {
            Glamourer.Log.Error(
                $"Error parsing Configuration at {errorArgs.ErrorContext.Path}, using default or migrating:\n{errorArgs.ErrorContext.Error}");
            errorArgs.ErrorContext.Handled = true;
        }
    }

    public string ToFilePath(FilenameService fileNames)
        => fileNames.ConfigurationFile;

    public void Save(StreamWriter writer)
    {
        using var jWriter = new JsonTextWriter(writer);
        jWriter.Formatting = Formatting.Indented;
        var serializer = new JsonSerializer { Formatting = Formatting.Indented };
        serializer.Serialize(jWriter, this);
    }

    public static class Constants
    {
        public const int CurrentVersion = 8;

        public static readonly ISortMode<Design>[] ValidSortModes =
        [
            ISortMode<Design>.FoldersFirst,
            ISortMode<Design>.Lexicographical,
            new DesignFileSystem.CreationDate(),
            new DesignFileSystem.InverseCreationDate(),
            new DesignFileSystem.UpdateDate(),
            new DesignFileSystem.InverseUpdateDate(),
            ISortMode<Design>.InverseFoldersFirst,
            ISortMode<Design>.InverseLexicographical,
            ISortMode<Design>.FoldersLast,
            ISortMode<Design>.InverseFoldersLast,
            ISortMode<Design>.InternalOrder,
            ISortMode<Design>.InverseInternalOrder,
        ];
    }

    /// <summary> Convert SortMode Types to their name. </summary>
    private class SortModeConverter : JsonConverter<ISortMode<Design>>
    {
        public override void WriteJson(JsonWriter writer, ISortMode<Design>? value, JsonSerializer serializer)
        {
            value ??= ISortMode<Design>.FoldersFirst;
            serializer.Serialize(writer, value.GetType().Name);
        }

        public override ISortMode<Design> ReadJson(JsonReader reader, Type objectType, ISortMode<Design>? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var name = serializer.Deserialize<string>(reader);
            if (name == null || !Constants.ValidSortModes.FindFirst(s => s.GetType().Name == name, out var mode))
                return existingValue ?? ISortMode<Design>.FoldersFirst;

            return mode;
        }
    }
}
