using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using Lumina.Data.Files;
using OtterGui.Services;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.String.Functions;
using SharpGen.Runtime;
using Vortice.Direct3D11;
using Vortice.DXGI;
using MapFlags = Vortice.Direct3D11.MapFlags;
using Texture = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture;

namespace Glamourer.Interop.Material;

public unsafe class DirectXService(IFramework framework) : IService
{
    private readonly object                                                                _lock     = new();
    private readonly ConcurrentDictionary<nint, (DateTime Update, LegacyColorTable Table)> _textures = [];

    /// <summary> Generate a color table the way the game does inside the original texture, and release the original. </summary>
    /// <param name="original"> The original texture that will be replaced with a new one. </param>
    /// <param name="colorTable"> The input color table. </param>
    /// <returns> Success or failure. </returns>
    public bool ReplaceColorTable(Texture** original, in LegacyColorTable colorTable)
    {
        if (original == null)
            return false;

        var textureSize = stackalloc int[2];
        textureSize[0] = MaterialService.TextureWidth;
        textureSize[1] = MaterialService.TextureHeight;

        lock (_lock)
        {
            using var texture = new SafeTextureHandle(Device.Instance()->CreateTexture2D(textureSize, 1,
                (uint)TexFile.TextureFormat.R16G16B16A16F,
                (uint)(TexFile.Attribute.TextureType2D | TexFile.Attribute.Managed | TexFile.Attribute.Immutable), 7), false);
            if (texture.IsInvalid)
                return false;

            fixed (LegacyColorTable* ptr = &colorTable)
            {
                if (!texture.Texture->InitializeContents(ptr))
                    return false;
            }

            Glamourer.Log.Verbose($"[{Thread.CurrentThread.ManagedThreadId}] Replaced texture {(ulong)*original:X} with new ColorTable.");
            texture.Exchange(ref *(nint*)original);
        }

        return true;
    }

    public bool TryGetColorTable(Texture* texture, out LegacyColorTable table)
    {
        if (_textures.TryGetValue((nint)texture, out var p) && framework.LastUpdateUTC == p.Update)
        {
            table = p.Table;
            return true;
        }

        lock (_lock)
        {
            if (!TextureColorTable(texture, out table))
                return false;
        }

        _textures[(nint)texture] = (framework.LastUpdateUTC, table);
        return true;
    }

    /// <summary> Try to turn a color table GPU-loaded texture (R16G16B16A16Float, 4 Width, 16 Height) into an actual color table. </summary>
    /// <param name="texture"> A pointer to the internal texture struct containing the GPU handle. </param>
    /// <param name="table"> The returned color table. </param>
    /// <returns> Whether the table could be fetched. </returns>
    private static bool TextureColorTable(Texture* texture, out LegacyColorTable table)
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

        var ret = resource.Device.As<ID3D11Device3>().CreateTexture2D1(desc);
        Glamourer.Log.Excessive(
            $"[{Thread.CurrentThread.ManagedThreadId}] Cloning resource {resource.NativePointer:X} to {ret.NativePointer:X}");
        return ret;
    }

    /// <summary> Turn a mapped texture into a color table. </summary>
    private static LegacyColorTable GetTextureData(ID3D11Texture2D1 resource, MappedSubresource map)
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
    private static LegacyColorTable ReadTexture(nint data, int length, int height, int pitch)
    {
        // Check that the data has sufficient dimension and size.
        var expectedSize = sizeof(Half) * MaterialService.TextureWidth * height * 4;
        if (length < expectedSize || sizeof(LegacyColorTable) != expectedSize || height != MaterialService.TextureHeight)
            return default;

        var ret    = new LegacyColorTable();
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
        Glamourer.Log.Excessive(
            $"[{Thread.CurrentThread.ManagedThreadId}] Copied resource data {res.NativePointer:X} to {stagingRes.NativePointer:X}");
        stagingRes.Device.ImmediateContext.Map(stagingRes, 0, MapMode.Read, MapFlags.None, out var mapInfo).CheckError();
        Glamourer.Log.Excessive(
            $"[{Thread.CurrentThread.ManagedThreadId}] Mapped resource data for {stagingRes.NativePointer:X} to {mapInfo.DataPointer:X}");

        try
        {
            return getData(stagingRes, mapInfo);
        }
        finally
        {
            Glamourer.Log.Excessive($"[{Thread.CurrentThread.ManagedThreadId}] Obtained resource data.");
            stagingRes.Device.ImmediateContext.Unmap(stagingRes, 0);
            Glamourer.Log.Excessive($"[{Thread.CurrentThread.ManagedThreadId}] Unmapped resource data for {stagingRes.NativePointer:X}");
        }
    }

    private static readonly Result WasStillDrawing = new(0x887A000A);
}
