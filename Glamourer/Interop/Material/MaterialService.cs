using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Lumina.Data.Files;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Interop;
using Texture = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture;

namespace Glamourer.Interop.Material;

public static unsafe class MaterialService
{
    private const TextureFormat Format = TextureFormat.R16G16B16A16_FLOAT;
    private const TextureFlags  Flags  = TextureFlags.TextureType2D | TextureFlags.Managed | TextureFlags.Immutable;

    public const int TextureWidth      = 8;
    public const int TextureHeight     = ColorTable.NumRows;
    public const int MaterialsPerModel = 10;

    public static Texture* CreateColorTableTexture()
    {
        var textureSize = stackalloc int[2];
        textureSize[0] = TextureWidth;
        textureSize[1] = TextureHeight;
        return Device.Instance()->CreateTexture2D(textureSize, 1, Format, Flags, 7);
    }

    public static bool GenerateNewColorTable(in ColorTable.Table colorTable, out Texture* texture)
    {
        texture = CreateColorTableTexture();
        if (texture == null)
            return false;

        fixed (ColorTable.Table* ptr = &colorTable)
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
    public static ColorTable.Table* GetMaterialColorTable(Model model, int modelSlot, byte materialSlot)
    {
        if (!model.IsCharacterBase)
            return null;

        var index = modelSlot * MaterialsPerModel + materialSlot;
        if (index < 0 || index >= model.AsCharacterBase->MaterialsSpan.Length)
            return null;

        var material = (MaterialResourceHandle*) model.AsCharacterBase->MaterialsSpan[index].Value;
        if (material == null || material->ColorTable == null)
            return null;

        return (ColorTable.Table*)material->ColorTable;
    }
}
