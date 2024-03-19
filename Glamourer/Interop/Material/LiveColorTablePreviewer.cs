using Dalamud.Plugin.Services;
using ImGuiNET;
using OtterGui.Services;
using Penumbra.GameData.Files;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop.Material;

public sealed unsafe class LiveColorTablePreviewer : IService, IDisposable
{
    private readonly global::Penumbra.GameData.Interop.ObjectManager _objects;
    private readonly IFramework                                      _framework;
    private readonly DirectXService                                  _directXService;

    public  MaterialValueIndex  LastValueIndex         { get; private set; } = MaterialValueIndex.Invalid;
    public  MtrlFile.ColorTable LastOriginalColorTable { get; private set; }
    private MaterialValueIndex  _valueIndex      = MaterialValueIndex.Invalid;
    private ObjectIndex         _lastObjectIndex = ObjectIndex.AnyIndex;
    private ObjectIndex         _objectIndex     = ObjectIndex.AnyIndex;
    private MtrlFile.ColorTable _originalColorTable;

    public LiveColorTablePreviewer(global::Penumbra.GameData.Interop.ObjectManager objects, IFramework framework, DirectXService directXService)
    {
        _objects          =  objects;
        _framework        =  framework;
        _directXService   =  directXService;
        _framework.Update += OnFramework;
    }

    private void Reset()
    {
        if (LastValueIndex.DrawObject is MaterialValueIndex.DrawObjectType.Invalid || _lastObjectIndex == ObjectIndex.AnyIndex)
            return;

        var actor = _objects[_lastObjectIndex];
        if (actor.IsCharacter && LastValueIndex.TryGetTexture(actor, out var texture))
            _directXService.ReplaceColorTable(texture, LastOriginalColorTable);

        LastValueIndex   = MaterialValueIndex.Invalid;
        _lastObjectIndex = ObjectIndex.AnyIndex;
    }

    private void OnFramework(IFramework _)
    {
        if (_valueIndex.DrawObject is MaterialValueIndex.DrawObjectType.Invalid || _objectIndex == ObjectIndex.AnyIndex)
        {
            Reset();
            _valueIndex  = MaterialValueIndex.Invalid;
            _objectIndex = ObjectIndex.AnyIndex;
            return;
        }

        var actor = _objects[_objectIndex];
        if (!actor.IsCharacter)
        {
            _valueIndex  = MaterialValueIndex.Invalid;
            _objectIndex = ObjectIndex.AnyIndex;
            return;
        }

        if (_valueIndex != LastValueIndex || _lastObjectIndex != _objectIndex)
        {
            Reset();
            LastValueIndex         = _valueIndex;
            _lastObjectIndex       = _objectIndex;
            LastOriginalColorTable = _originalColorTable;
        }

        if (_valueIndex.TryGetTexture(actor, out var texture))
        {
            var diffuse  = CalculateDiffuse();
            var emissive = diffuse / 8;
            var table    = LastOriginalColorTable;
            if (_valueIndex.RowIndex != byte.MaxValue)
            {
                table[_valueIndex.RowIndex].Diffuse  = diffuse;
                table[_valueIndex.RowIndex].Emissive = emissive;
            }
            else
            {
                for (var i = 0; i < MtrlFile.ColorTable.NumRows; ++i)
                {
                    table[i].Diffuse  = diffuse;
                    table[i].Emissive = emissive;
                }
            }

            _directXService.ReplaceColorTable(texture, table);
        }

        _valueIndex  = MaterialValueIndex.Invalid;
        _objectIndex = ObjectIndex.AnyIndex;
    }

    public void OnHover(MaterialValueIndex index, ObjectIndex objectIndex, MtrlFile.ColorTable table)
    {
        if (_valueIndex.DrawObject is not MaterialValueIndex.DrawObjectType.Invalid)
            return;

        _valueIndex  = index;
        _objectIndex = objectIndex;
        if (LastValueIndex.DrawObject is MaterialValueIndex.DrawObjectType.Invalid
         || _lastObjectIndex == ObjectIndex.AnyIndex
         || LastValueIndex.MaterialIndex != _valueIndex.MaterialIndex
         || LastValueIndex.DrawObject != _valueIndex.DrawObject
         || LastValueIndex.SlotIndex != _valueIndex.SlotIndex)
            _originalColorTable = table;
    }

    private static Vector3 CalculateDiffuse()
    {
        const long frameLength = TimeSpan.TicksPerMillisecond * 5;
        const long steps       = 2000;
        var        frame       = DateTimeOffset.UtcNow.UtcTicks;
        var        hueByte     = frame % (steps * frameLength) / frameLength;
        var        hue         = (float)hueByte / steps;
        ImGui.ColorConvertHSVtoRGB(hue, 1, 1, out var r, out var g, out var b);
        return new Vector3(r, g, b);
    }

    public void Dispose()
    {
        Reset();
        _framework.Update -= OnFramework;
    }
}
