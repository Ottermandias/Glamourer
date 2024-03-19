using FFXIVClientStructs.FFXIV.Shader;
using Glamourer.GameData;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop.Structs;

public static unsafe class ModelExtensions
{
    public static CustomizeParameterData GetParameterData(this Model model)
    {
        if (!model.IsHuman)
            return default;

        var cBuffer1 = model.AsHuman->CustomizeParameterCBuffer;
        var cBuffer2 = model.AsHuman->DecalColorCBuffer;
        var ptr1     = (CustomizeParameter*)(cBuffer1 == null ? null : cBuffer1->UnsafeSourcePointer);
        var ptr2     = (DecalParameters*)(cBuffer2 == null ? null : cBuffer2->UnsafeSourcePointer);
        return CustomizeParameterData.FromParameters(ptr1 != null ? *ptr1 : default, ptr2 != null ? *ptr2 : default);
    }

    public static void ApplyParameterData(this Model model, CustomizeParameterFlag flags, in CustomizeParameterData data)
    {
        if (!model.IsHuman)
            return;

        if (flags.HasFlag(CustomizeParameterFlag.DecalColor))
        {
            var cBufferDecal = model.AsHuman->DecalColorCBuffer;
            var ptrDecal     = (DecalParameters*)(cBufferDecal == null ? null : cBufferDecal->UnsafeSourcePointer);
            if (ptrDecal != null)
                data.Apply(ref *ptrDecal);
        }

        flags &= ~CustomizeParameterFlag.DecalColor;
        var cBuffer = model.AsHuman->CustomizeParameterCBuffer;
        var ptr     = (CustomizeParameter*)(cBuffer == null ? null : cBuffer->UnsafeSourcePointer);
        if (ptr != null)
            data.Apply(ref *ptr, flags);
    }

    public static bool ApplySingleParameterData(this Model model, CustomizeParameterFlag flag, in CustomizeParameterData data)
    {
        if (!model.IsHuman)
            return false;

        if (flag is CustomizeParameterFlag.DecalColor)
        {
            var cBuffer = model.AsHuman->DecalColorCBuffer;
            var ptr     = (DecalParameters*)(cBuffer == null ? null : cBuffer->UnsafeSourcePointer);
            if (ptr == null)
                return false;

            data.Apply(ref *ptr);
            return true;
        }
        else
        {
            var cBuffer = model.AsHuman->CustomizeParameterCBuffer;
            var ptr     = (CustomizeParameter*)(cBuffer == null ? null : cBuffer->UnsafeSourcePointer);
            if (ptr == null)
                return false;

            data.ApplySingle(ref *ptr, flag);
            return true;
        }
    }
}
