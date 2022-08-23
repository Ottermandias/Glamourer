using System.Collections.Generic;
using System.Text;
using Dalamud.Game.ClientState.Objects.Enums;
using Lumina.Excel.GeneratedSheets;
using static Glamourer.Interop.Actor;

namespace Glamourer.Interop;

public static class ObjectManager
{
    private const int CutsceneIndex        = 200;
    private const int GPosePlayerIndex     = 201;
    private const int CharacterScreenIndex = 240;
    private const int ExamineScreenIndex   = 241;
    private const int FittingRoomIndex     = 242;
    private const int DyePreviewIndex      = 243;
    private const int PortraitIndex        = 244;

    public readonly struct ActorData
    {
        public readonly List<Actor> Objects;
        public readonly string      Label;

        public bool Valid
            => Objects.Count > 0;

        public ActorData(Actor actor, string label)
        {
            Objects = new List<Actor> { actor };
            Label   = label;
        }

        public static readonly ActorData Invalid = new(false);

        private ActorData(bool _)
        {
            Objects = new List<Actor>(0);
            Label   = string.Empty;
        }
    }

    public static bool   IsInGPose { get; private set; }
    public static ushort World     { get; private set; }

    public static IReadOnlyDictionary<IIdentifier, ActorData> Actors
        => Identifiers;

    public static IReadOnlyList<(IIdentifier, ActorData)> List
        => ListData;

    private static readonly Dictionary<IIdentifier, ActorData> Identifiers = new(200);
    private static readonly List<(IIdentifier, ActorData)>     ListData    = new(Dalamud.Objects.Length);

    private static void HandleIdentifier(IIdentifier identifier, Actor character)
    {
        if (!character.DrawObject)
            return;

        switch (identifier)
        {
            case PlayerIdentifier p:
                if (!Identifiers.TryGetValue(p, out var data))
                {
                    data = new ActorData(character,
                        World != p.HomeWorld
                            ? $"{p.Name} ({Dalamud.GameData.GetExcelSheet<World>()!.GetRow(p.HomeWorld)!.Name})"
                            : p.Name.ToString());
                    Identifiers[p] = data;
                    ListData.Add((p, data));
                }
                else
                {
                    data.Objects.Add(character);
                }

                break;
            case NpcIdentifier n when !n.Name.IsEmpty:
                if (!Identifiers.TryGetValue(n, out data))
                {
                    data           = new ActorData(character, $"{n.Name} (at {n.ObjectIndex})");
                    Identifiers[n] = data;
                    ListData.Add((n, data));
                }
                else
                {
                    data.Objects.Add(character);
                }

                break;
            case OwnedIdentifier o:
                if (!Identifiers.TryGetValue(o, out data))
                {
                    data = new ActorData(character,
                        World != o.OwnerHomeWorld
                            ? $"{o.OwnerName}s {o.Name} ({Dalamud.GameData.GetExcelSheet<World>()!.GetRow(o.OwnerHomeWorld)!.Name})"
                            : $"{o.OwnerName}s {o.Name}");
                    Identifiers[o] = data;
                    ListData.Add((o, data));
                }
                else
                {
                    data.Objects.Add(character);
                }

                break;
        }
    }

    public static void Update()
    {
        World = (ushort)(Dalamud.ClientState.LocalPlayer?.CurrentWorld.Id ?? 0u);
        Identifiers.Clear();
        ListData.Clear();

        for (var i = 0; i < CutsceneIndex; ++i)
        {
            Actor character = Dalamud.Objects.GetObjectAddress(i);
            if (character.Identifier(out var identifier))
                HandleIdentifier(identifier, character);
        }

        for (var i = CutsceneIndex; i < CharacterScreenIndex; ++i)
        {
            Actor character = Dalamud.Objects.GetObjectAddress(i);
            if (!character.Identifier(out var identifier))
                break;

            HandleIdentifier(identifier, character);
        }

        void AddSpecial(int idx, string label)
        {
            Actor actor = Dalamud.Objects.GetObjectAddress(idx);
            if (actor.Identifier(out var ident))
            {
                var data = new ActorData(actor, label);
                Identifiers.Add(ident, data);
                ListData.Add((ident, data));
            }
        }

        AddSpecial(CharacterScreenIndex, "Character Screen Actor");
        AddSpecial(ExamineScreenIndex,   "Examine Screen Actor");
        AddSpecial(FittingRoomIndex,     "Fitting Room Actor");
        AddSpecial(DyePreviewIndex,      "Dye Preview Actor");
        AddSpecial(PortraitIndex,        "Portrait Actor");

        for (var i = PortraitIndex + 1; i < Dalamud.Objects.Length; ++i)
        {
            Actor character = Dalamud.Objects.GetObjectAddress(i);
            if (character.Identifier(out var identifier))
                HandleIdentifier(identifier, character);
        }


        Actor gPose = Dalamud.Objects.GetObjectAddress(GPosePlayerIndex);
        IsInGPose = gPose && gPose.Utf8Name.Length > 0;
    }

    public static Actor GPosePlayer
        => Dalamud.Objects.GetObjectAddress(GPosePlayerIndex);

    public static Actor Player
        => Dalamud.Objects.GetObjectAddress(0);

    private static unsafe string GetLabel(Actor player, string playerName, int num, bool gPose)
    {
        var sb = new StringBuilder(64);
        sb.Append(playerName);

        if (gPose)
        {
            sb.Append(" (GPose");

            if (player.ObjectKind == ObjectKind.Player)
                sb.Append(')');
            else
                sb.Append(player.ModelId == 0 ? ", NPC)" : ", Monster)");
        }
        else if (player.ObjectKind != ObjectKind.Player)
        {
            sb.Append(player.ModelId == 0 ? " (NPC)" : " (Monster)");
        }

        if (num > 1)
        {
            sb.Append(" #");
            sb.Append(num);
        }

        return sb.ToString();
    }
}
