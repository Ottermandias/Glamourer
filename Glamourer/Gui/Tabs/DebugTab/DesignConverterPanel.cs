using Glamourer.Designs;
using Glamourer.Utility;
using ImSharp;
using Luna;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class DesignConverterPanel : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Design Converter"u8;

    public bool Disabled
        => false;

    public sealed class Cache(DesignConverter designConverter) : BasicCache(TimeSpan.FromMinutes(10), IManagedCache.DirtyFlags.Clean), IService
    {
        private StringU8    _clipboardText    = StringU8.Empty;
        private StringU8    _clipboardData    = StringU8.Empty;
        private StringU8    _dataUncompressed = StringU8.Empty;
        private byte        _version;
        private StringU8    _textUncompressed = StringU8.Empty;
        private JObject?    _json;
        private DesignBase? _tmpDesign;
        private StringU8    _clipboardProblem = StringU8.Empty;

        public override void Update()
            => Dirty = IManagedCache.DirtyFlags.Clean;

        public void Draw()
        {
            if (Im.Button("Import Clipboard"u8))
            {
                _clipboardData    = StringU8.Empty;
                _dataUncompressed = StringU8.Empty;
                _textUncompressed = StringU8.Empty;
                _json             = null;
                _tmpDesign        = null;
                _clipboardProblem = StringU8.Empty;

                try
                {
                    _clipboardText = Im.Clipboard.GetCopy();
                    var textU16       = _clipboardText.ToString();
                    var clipboardData = Convert.FromBase64String(textU16);
                    _version = clipboardData[0];
                    if (_version is 5)
                        clipboardData = clipboardData[DesignBase64Migration.Base64SizeV4..];
                    _clipboardData    = StringU8.Join((byte)' ', clipboardData.Select(b => b.ToString("X2")));
                    _version          = clipboardData.Decompress(out var dataUncompressed);
                    _dataUncompressed = StringU8.Join((byte)' ', dataUncompressed.Select(b => b.ToString("X2")));
                    _textUncompressed = new StringU8(dataUncompressed);
                    var textUncompressed = _textUncompressed.ToString();
                    _json      = JObject.Parse(textUncompressed);
                    _tmpDesign = designConverter.FromBase64(textU16, true, true, out _);
                }
                catch (Exception ex)
                {
                    _clipboardProblem = new StringU8($"{ex}");
                }
            }

            using var mono = Im.Font.PushMono();
            if (_clipboardText.Length > 0)
                Im.TextWrapped(_clipboardText);

            if (_clipboardData.Length > 0)
                Im.TextWrapped(_clipboardData);

            if (_dataUncompressed.Length > 0)
                Im.TextWrapped(_dataUncompressed);

            if (_textUncompressed.Length > 0)
            {
                Im.TextWrapped(_textUncompressed);
                if (Im.Item.Clicked())
                    Im.Clipboard.Set(_textUncompressed);
            }

            mono.Pop();
            if (_json is not null)
                Im.Text("JSON Parsing Successful!"u8);

            if (_tmpDesign is not null)
                DesignManagerPanel.DrawDesign(_tmpDesign, null);

            if (_clipboardProblem.Length > 0)
            {
                mono.Push(Im.Font.Mono);
                Im.Text(_clipboardProblem);
            }
        }
    }

    public void Draw()
    {
        var cache = CacheManager.Instance.GetOrCreateCache<Cache>(Im.Id.Current);
        cache.Draw();
    }
}
