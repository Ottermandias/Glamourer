using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using ImSharp;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed unsafe class AdvancedCustomizationDrawer(ActorObjectManager objects) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Advanced Customizations"u8;

    public bool Disabled
        => false;

    public void Draw()
    {
        var (_, data) = objects.PlayerData;
        if (!data.Valid)
        {
            Im.Text("Invalid player."u8);
            return;
        }

        var model = data.Objects[0].Model;
        if (!model.IsHuman)
        {
            Im.Text("Invalid model."u8);
            return;
        }

        DrawCBuffer("Customize"u8, model.AsHuman->CustomizeParameterCBuffer,          0);
        DrawCBuffer("Decal"u8,     model.AsHuman->DecalColorCBuffer,                  1);
        DrawCBuffer("Unk1"u8,      *(ConstantBuffer**)((byte*)model.AsHuman + 0xBA0), 2);
        DrawCBuffer("Unk2"u8,      *(ConstantBuffer**)((byte*)model.AsHuman + 0xBA8), 3);
    }


    private static void DrawCBuffer(ReadOnlySpan<byte> label, ConstantBuffer* cBuffer, int type)
    {
        using var tree = Im.Tree.Node(label);
        if (!tree)
            return;

        if (cBuffer is null)
        {
            Im.Text("Invalid CBuffer."u8);
            return;
        }

        Im.Text($"{cBuffer->ByteSize / 4}");
        Im.Text($"{cBuffer->Flags}");
        Glamourer.Dynamis.DrawPointer(cBuffer);
        var parameters = (float*)cBuffer->UnsafeSourcePointer;
        if (parameters is null)
        {
            Im.Text("No Parameters."u8);
            return;
        }

        var start = parameters;
        using (Im.Group())
        {
            for (var end = start + cBuffer->ByteSize / 4; parameters < end; parameters += 2)
                DrawParameters(parameters, type, (int)(parameters - start));
        }

        Im.Line.Same(0, 50 * Im.Style.GlobalScale);
        parameters = start + 1;
        using (Im.Group())
        {
            for (var end = start + cBuffer->ByteSize / 4; parameters < end; parameters += 2)
                DrawParameters(parameters, type, (int)(parameters - start));
        }
    }

    private static void DrawParameters(float* param, int type, int idx)
    {
        using var id = Im.Id.Push((nint)param);
        ImEx.TextFrameAligned($"{idx:D2}: ");
        Im.Line.SameInner();
        Im.Item.SetNextWidthScaled(200);
        if (TryGetKnown(type, idx, out var known))
        {
            Im.Drag(known, ref *param, float.MinValue, float.MaxValue, 0.01f);
        }
        else
        {
            using var color = ImGuiColor.Text.Push(Im.Style[ImGuiColor.TextDisabled]);
            Im.Drag($"+0x{idx * 4:X2}", ref *param, float.MinValue, float.MaxValue, 0.01f);
        }
    }

    private static bool TryGetKnown(int type, int idx, out ReadOnlySpan<byte> text)
    {
        if (type is 0)
            text = idx switch
            {
                0  => "Diffuse.R"u8,
                1  => "Diffuse.G"u8,
                2  => "Diffuse.B"u8,
                3  => "Muscle Tone"u8,
                8  => "Lipstick.R"u8,
                9  => "Lipstick.G"u8,
                10 => "Lipstick.B"u8,
                11 => "Lipstick.Opacity"u8,
                12 => "Hair.R"u8,
                13 => "Hair.G"u8,
                14 => "Hair.B"u8,
                15 => "Facepaint.Offset"u8,
                20 => "Highlight.R"u8,
                21 => "Highlight.G"u8,
                22 => "Highlight.B"u8,
                23 => "Facepaint.Multiplier"u8,
                24 => "LeftEye.R"u8,
                25 => "LeftEye.G"u8,
                26 => "LeftEye.B"u8,
                27 => "LeftLimbal"u8,
                28 => "RightEye.R"u8,
                29 => "RightEye.G"u8,
                30 => "RightEye.B"u8,
                31 => "RightLimbal"u8,
                32 => "Feature.R"u8,
                33 => "Feature.G"u8,
                34 => "Feature.B"u8,
                _  => [],
            };
        else
            text = [];

        return text.Length > 0;
    }
}
