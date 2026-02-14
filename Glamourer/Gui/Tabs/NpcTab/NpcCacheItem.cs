using Glamourer.GameData;
using ImSharp;

namespace Glamourer.Gui.Tabs.NpcTab;

public readonly struct NpcCacheItem(in NpcData npc, string colorText, Rgba32 color, bool favorite)
{
    public readonly StringPair Name      = new(npc.Name);
    public readonly StringU8   Id        = new($"({npc.Id})");
    public readonly NpcData    Npc       = npc;
    public readonly string     ColorText = colorText;
    public readonly Vector4    Color     = color.ToVector();
    public readonly bool       Favorite  = favorite;
}
