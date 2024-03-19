using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Lumina.Data.Files;
using Penumbra.GameData.Interop;
using static Penumbra.GameData.Files.MtrlFile;
using Texture = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture;

namespace Glamourer.Interop.Material;

public static unsafe class MaterialService
{
    public const int TextureWidth      = 4;
    public const int TextureHeight     = ColorTable.NumRows;
    public const int MaterialsPerModel = 4;

    public static bool GenerateNewColorTable(in ColorTable colorTable, out Texture* texture)
    {
        var textureSize = stackalloc int[2];
        textureSize[0] = TextureWidth;
        textureSize[1] = TextureHeight;

        texture = Device.Instance()->CreateTexture2D(textureSize, 1, (uint)TexFile.TextureFormat.R16G16B16A16F,
            (uint)(TexFile.Attribute.TextureType2D | TexFile.Attribute.Managed | TexFile.Attribute.Immutable), 7);
        if (texture == null)
            return false;

        fixed (ColorTable* ptr = &colorTable)
        {
            return texture->InitializeContents(ptr);
        }
    }

    /// <summary> Obtain a pointer to the models pointer to a specific color table texture. </summary>
    /// <param name="model"></param>
    /// <param name="modelSlot"></param>
    /// <param name="materialSlot"></param>
    /// <returns></returns>
    public static Texture** GetColorTableTexture(Model model, int modelSlot, byte materialSlot)
    {
        if (!model.IsCharacterBase)
            return null;

        var index = modelSlot * MaterialsPerModel + materialSlot;
        if (index < 0 || index >= model.AsCharacterBase->ColorTableTexturesSpan.Length)
            return null;

        var texture = (Texture**)Unsafe.AsPointer(ref model.AsCharacterBase->ColorTableTexturesSpan[index]);
        return texture;
    }

    /// <summary> Obtain a pointer to the color table of a certain material from a model. </summary>
    /// <param name="model"> The draw object. </param>
    /// <param name="modelSlot"> The model slot. </param>
    /// <param name="materialSlot"> The material slot in the model. </param>
    /// <returns> A pointer to the color table or null. </returns>
    public static ColorTable* GetMaterialColorTable(Model model, int modelSlot, byte materialSlot)
    {
        if (!model.IsCharacterBase)
            return null;

        var index = modelSlot * MaterialsPerModel + materialSlot;
        if (index < 0 || index >= model.AsCharacterBase->MaterialsSpan.Length)
            return null;

        var material = (MaterialResourceHandle*)model.AsCharacterBase->MaterialsSpan[index].Value;
        if (material == null || material->ColorTable == null)
            return null;

        return (ColorTable*)material->ColorTable;
    }
}
