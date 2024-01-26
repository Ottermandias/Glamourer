using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Glamourer.Interop.Structs;
using Lumina.Data.Files;
using static Penumbra.GameData.Files.MtrlFile;
using Texture = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture;

namespace Glamourer.Interop.Material;

public static unsafe class MaterialService
{
    public const int TextureWidth      = 4;
    public const int TextureHeight     = ColorTable.NumRows;
    public const int MaterialsPerModel = 4;

    /// <summary> Generate a color table the way the game does inside the original texture, and release the original. </summary>
    /// <param name="original"> The original texture that will be replaced with a new one. </param>
    /// <param name="colorTable"> The input color table. </param>
    /// <returns> Success or failure. </returns>
    public static bool ReplaceColorTable(Texture** original, in ColorTable colorTable)
    {
        if (original == null)
            return false;

        var textureSize = stackalloc int[2];
        textureSize[0] = TextureWidth;
        textureSize[1] = TextureHeight;

        using var texture = new SafeTextureHandle(Device.Instance()->CreateTexture2D(textureSize, 1, (uint)TexFile.TextureFormat.R16G16B16A16F,
            (uint)(TexFile.Attribute.TextureType2D | TexFile.Attribute.Managed | TexFile.Attribute.Immutable), 7), false);
        if (texture.IsInvalid)
            return false;

        fixed (ColorTable* ptr = &colorTable)
        {
            if (!texture.Texture->InitializeContents(ptr))
                return false;
        }

        texture.Exchange(ref *(nint*)original);
        return true;
    }

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
