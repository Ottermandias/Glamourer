using System;
using System.Collections.Generic;
using Dalamud.Data.LuminaExtensions;
using Dalamud.Plugin;
using ImGuiScene;
using Lumina.Data.Files;

namespace Glamourer.Util
{
    public class IconStorage : IDisposable
    {
        private readonly DalamudPluginInterface       _pi;
        private readonly Dictionary<int, TextureWrap> _icons;

        public IconStorage(DalamudPluginInterface pi, int size = 0)
        {
            _pi    = pi;
            _icons = new Dictionary<int, TextureWrap>(size);
        }

        public TextureWrap this[int id]
            => LoadIcon(id);

        private TexFile? LoadIconHq(int id)
        {
            var path = $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}_hr1.tex";
            return _pi.Data.GetFile<TexFile>(path);
        }

        public TextureWrap LoadIcon(uint id)
            => LoadIcon((int) id);

        public TextureWrap LoadIcon(int id)
        {
            if (_icons.TryGetValue(id, out var ret))
                return ret;

            var icon     = LoadIconHq(id) ?? _pi.Data.GetIcon(id);
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
