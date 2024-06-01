using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using OtterGui.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Services;

public unsafe class HeightService : IService
{
    [Signature("E8 ?? ?? ?? FF 48 8B 0D ?? ?? ?? ?? 0F 28 F0")]
    private readonly delegate* unmanaged[Stdcall]<CharacterUtility*, byte, byte, byte, byte, float> _calculateHeight = null!;

    public HeightService(IGameInteropProvider interop)
        => interop.InitializeFromAttributes(this);

    public float Height(CustomizeValue height, SubRace clan, Gender gender, CustomizeValue bodyType)
        => _calculateHeight(CharacterUtility.Instance(), height.Value, (byte)clan, (byte)((byte)gender - 1), bodyType.Value);

    public float Height(in CustomizeArray customize)
        => Height(customize[CustomizeIndex.Height], customize.Clan, customize.Gender, customize.BodyType);
}
