using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Luna;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Services;

public unsafe class HeightService : IService
{
    [Signature(Sigs.CalculateHeight)]
    private readonly delegate* unmanaged[Stdcall]<CharacterUtility*, byte, byte, byte, byte, float> _calculateHeight = null!;

    public HeightService(IGameInteropProvider interop)
        => interop.InitializeFromAttributes(this);

    public float Height(CustomizeValue height, SubRace clan, Gender gender, CustomizeValue bodyType)
        => _calculateHeight(CharacterUtility.Instance(), height.Value, (byte)clan, (byte)((byte)gender - 1), bodyType.Value);

    public float Height(in CustomizeArray customize)
        => Height(customize[CustomizeIndex.Height], customize.Clan, customize.Gender, customize.BodyType);
}
