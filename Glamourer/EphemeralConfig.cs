using Dalamud.Interface.Internal.Notifications;
using Glamourer.Gui;
using Glamourer.Services;
using Newtonsoft.Json;
using OtterGui.Classes;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Glamourer;

public class EphemeralConfig : ISavable
{
    public int                Version             { get; set; } = Configuration.Constants.CurrentVersion;
    public bool               IncognitoMode       { get; set; } = false;
    public bool               UnlockDetailMode    { get; set; } = true;
    public bool               ShowDesignQuickBar  { get; set; } = false;
    public bool               LockDesignQuickBar  { get; set; } = false;
    public bool               LockMainWindow      { get; set; } = false;
    public MainWindow.TabType SelectedTab         { get; set; } = MainWindow.TabType.Settings;
    public Guid               SelectedDesign      { get; set; } = Guid.Empty;
    public Guid               SelectedQuickDesign { get; set; } = Guid.Empty;
    public int                LastSeenVersion     { get; set; } = GlamourerChangelog.LastChangelogVersion;


    [JsonIgnore]
    private readonly SaveService _saveService;

    public EphemeralConfig(SaveService saveService)
    {
        _saveService = saveService;
        Load();
    }

    public void Save()
        => _saveService.DelaySave(this, TimeSpan.FromSeconds(5));

    public void Load()
    {
        static void HandleDeserializationError(object? sender, ErrorEventArgs errorArgs)
        {
            Glamourer.Log.Error(
                $"Error parsing ephemeral Configuration at {errorArgs.ErrorContext.Path}, using default or migrating:\n{errorArgs.ErrorContext.Error}");
            errorArgs.ErrorContext.Handled = true;
        }

        if (!File.Exists(_saveService.FileNames.EphemeralConfigFile))
            return;

        try
        {
            var text = File.ReadAllText(_saveService.FileNames.EphemeralConfigFile);
            JsonConvert.PopulateObject(text, this, new JsonSerializerSettings
            {
                Error = HandleDeserializationError,
            });
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex,
                "Error reading ephemeral Configuration, reverting to default.",
                "Error reading ephemeral Configuration", NotificationType.Error);
        }
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.EphemeralConfigFile;

    public void Save(StreamWriter writer)
    {
        using var jWriter    = new JsonTextWriter(writer);
        jWriter.Formatting = Formatting.Indented;
        var       serializer = new JsonSerializer { Formatting         = Formatting.Indented };
        serializer.Serialize(jWriter, this);
    }
}
