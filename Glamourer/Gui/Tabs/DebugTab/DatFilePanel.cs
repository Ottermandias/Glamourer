using Glamourer.Interop;
using Penumbra.GameData.Files;
using Penumbra.GameData.Gui.Debug;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class DatFilePanel : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Character Dat File"u8;

    public bool Disabled
        => false;

    public sealed class Cache(ImportService importService) : BasicCache(TimeSpan.FromMinutes(10)), IService
    {
        private string            _datFilePath = string.Empty;
        private DatCharacterFile? _datFile;

        public void Draw()
        {
            using var id = Im.Id.Push("dat"u8);
            Im.Input.Text("##datFilePath"u8, ref _datFilePath, "Dat File Path..."u8);
            var exists = _datFilePath.Length > 0 && File.Exists(_datFilePath);
            if (ImEx.Button("Load"u8, Vector2.Zero, StringU8.Empty, !exists))
                _datFile = importService.LoadDat(_datFilePath, out var tmp) ? tmp : null;

            if (ImEx.Button("Save"u8, Vector2.Zero, StringU8.Empty, _datFilePath.Length is 0 || _datFile is null))
                importService.SaveDesignAsDat(_datFilePath, _datFile!.Value.Customize, _datFile!.Value.Description);

            if (_datFile is null)
                return;

            Im.Text($"{_datFile.Value.Magic}");
            Im.Text($"{_datFile.Value.Version}");
            Im.Text($"{_datFile.Value.Time.LocalDateTime:g}");
            Im.Text($"{_datFile.Value.Voice}");
            Im.Text($"{_datFile.Value.Customize}");
            Im.Text(_datFile.Value.Description);
        }

        public override void Update()
            => Dirty = IManagedCache.DirtyFlags.Clean;
    }


    public void Draw()
    {
        var cache = CacheManager.Instance.GetOrCreateCache<Cache>(Im.Id.Current);
        cache.Draw();
    }
}
