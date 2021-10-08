using System;
using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.Plugin;
using Dalamud.Utility;
using ImGuiScene;
using Lumina.Data.Files;

namespace Glamourer.Util
{
    public class IconStorage : IDisposable
    {
        private readonly DalamudPluginInterface        _pi;
        private readonly DataManager                   _gameData;
        private readonly Dictionary<uint, TextureWrap> _icons;

        public IconStorage(DalamudPluginInterface pi, DataManager gameData, int size = 0)
        {
            _pi       = pi;
            _gameData = gameData;
            _icons    = new Dictionary<uint, TextureWrap>(size);
        }

        public TextureWrap this[int id]
            => LoadIcon(id);

        private TexFile? LoadIconHq(uint id)
        {
            var path = $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}_hr1.tex";
            return _gameData.GetFile<TexFile>(path);
        }

        public TextureWrap LoadIcon(int id)
            => LoadIcon((uint) id);

        public TextureWrap LoadIcon(uint id)
        {
            if (_icons.TryGetValue(id, out var ret))
                return ret;

            var icon     = LoadIconHq(id) ?? _gameData.GetIcon(id)!;
            var iconData = icon.GetRgbaImageData();

            ret        = _pi.UiBuilder.LoadImageRaw(iconData, icon.Header.Width, icon.Header.Height, 4);
            _icons[id] = ret;
            return ret;
        }

        public void Dispose()
        {
            foreach (var icon in _icons.Values)
                icon.Dispose();
            _icons.Clear();
        }

        ~IconStorage()
            => Dispose();
    }
}
