using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using Penumbra.GameData.Files;
using Penumbra.String.Functions;
using SharpGen.Runtime;
using Vortice.Direct3D11;
using Vortice.DXGI;
using MapFlags = Vortice.Direct3D11.MapFlags;

namespace Glamourer.Interop.Material;

public static unsafe class DirectXTextureHelper
{
    /// <summary> Try to turn a color table GPU-loaded texture (R16G16B16A16Float, 4 Width, 16 Height) into an actual color table. </summary>
    /// <param name="texture"> A pointer to the internal texture struct containing the GPU handle. </param>
    /// <param name="table"> The returned color table. </param>
    /// <returns> Whether the table could be fetched. </returns>
    public static bool TryGetColorTable(Texture* texture, out MtrlFile.ColorTable table)
    {
        if (texture == null)
        {
            table = default;
            return false;
        }

        try
        {
            // Create direct x resource and ensure that it is kept alive.
            using var tex = new ID3D11Texture2D1((nint)texture->D3D11Texture2D);
            tex.AddRef();

            table = GetResourceData(tex, CreateStagedClone, GetTextureData);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary> Create a staging clone of the existing texture handle for stability reasons. </summary>
    private static ID3D11Texture2D1 CreateStagedClone(ID3D11Texture2D1 resource)
    {
        var desc = resource.Description1 with
        {
            Usage = ResourceUsage.Staging,
            BindFlags = 0,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = 0,
        };

        return resource.Device.As<ID3D11Device3>().CreateTexture2D1(desc);
    }

    /// <summary> Turn a mapped texture into a color table. </summary>
    private static MtrlFile.ColorTable GetTextureData(ID3D11Texture2D1 resource, MappedSubresource map)
    {
        var desc = resource.Description1;

        if (desc.Format is not Format.R16G16B16A16_Float
         || desc.Width != MaterialService.TextureWidth
         || desc.Height != MaterialService.TextureHeight
         || map.DepthPitch != map.RowPitch * desc.Height)
            throw new InvalidDataException("The texture was not a valid color table texture.");

        return ReadTexture(map.DataPointer, map.DepthPitch, desc.Height, map.RowPitch);
    }

    /// <summary> Transform the GPU data into the color table. </summary>
    /// <param name="data"> The pointer to the raw texture data. </param>
    /// <param name="length"> The size of the raw texture data. </param>
    /// <param name="height"> The height of the texture. (Needs to be 16).</param>
    /// <param name="pitch"> The stride in the texture data. </param>
    /// <returns></returns>
    private static MtrlFile.ColorTable ReadTexture(nint data, int length, int height, int pitch)
    {
        // Check that the data has sufficient dimension and size.
        var expectedSize = sizeof(Half) * MaterialService.TextureWidth * height * 4;
        if (length < expectedSize || sizeof(MtrlFile.ColorTable) != expectedSize || height != MaterialService.TextureHeight)
            return default;

        var ret    = new MtrlFile.ColorTable();
        var target = (byte*)&ret;
        // If the stride is the same as in the table, just copy.
        if (pitch == MaterialService.TextureWidth)
            MemoryUtility.MemCpyUnchecked(target, (void*)data, length);
        // Otherwise, adapt the stride.
        else

            for (var y = 0; y < height; ++y)
            {
                MemoryUtility.MemCpyUnchecked(target + y * MaterialService.TextureWidth * sizeof(Half) * 4, (byte*)data + y * pitch,
                    MaterialService.TextureWidth * sizeof(Half) * 4);
            }

        return ret;
    }

    /// <summary> Get resources of a texture. </summary>
    private static TRet GetResourceData<T, TRet>(T res, Func<T, T> cloneResource, Func<T, MappedSubresource, TRet> getData)
        where T : ID3D11Resource
    {
        using var stagingRes = cloneResource(res);

        res.Device.ImmediateContext.CopyResource(stagingRes, res);
        stagingRes.Device.ImmediateContext.Map(stagingRes, 0, MapMode.Read, MapFlags.None, out var mapInfo).CheckError();

        try
        {
            return getData(stagingRes, mapInfo);
        }
        finally
        {
            stagingRes.Device.ImmediateContext.Unmap(stagingRes, 0);
        }
    }
}
