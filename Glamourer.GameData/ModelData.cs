using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Dalamud.Data;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Lumina.Excel.GeneratedSheets;
using OtterGui.Raii;
using Companion = Lumina.Excel.GeneratedSheets.Companion;

namespace Glamourer;

public class ModelData
{
    public struct Data
    {
        public readonly ModelChara Model;

        public string FirstName { get; }
        public string AllNames  { get; internal set; }

        public Data(ModelChara model, string name)
        {
            Model     = model;
            FirstName = $"{name} #{model.RowId:D4}";
            AllNames  = $"#{model.RowId:D4}\n{name}";
        }

        public uint Id
            => Model.RowId;
    }

    private readonly SortedList<uint, Data>  _models;
    private readonly Dictionary<ulong, Data> _modelByData;

    public IReadOnlyDictionary<uint, Data> Models
        => _models;

    public unsafe ulong KeyFromCharacterBase(CharacterBase* drawObject)
    {
        var type = (*(delegate* unmanaged<CharacterBase*, uint>**)drawObject)[50](drawObject);
        var unk  = (ulong)*((byte*)drawObject + 0x8E8) << 8;
        return type switch
        {
            1 => type | unk,
            2 => type | unk | ((ulong)*(ushort*)((byte*)drawObject + 0x908) << 16),
            3 => type | unk | ((ulong)*(ushort*)((byte*)drawObject + 0x8F0) << 16) | ((ulong)**(ushort**)((byte*)drawObject + 0x910) << 32) | ((ulong)**(ushort**)((byte*)drawObject + 0x910) << 40),
            _ => 0u,
        };
    }

    public unsafe bool FromCharacterBase(CharacterBase* drawObject, out Data data)
        => _modelByData.TryGetValue(KeyFromCharacterBase(drawObject), out data);


    public ModelData(DataManager dataManager)
    {
        var modelSheet = dataManager.GetExcelSheet<ModelChara>()!;

        _models = new SortedList<uint, Data>(NpcNames.ModelCharas.Count);

        void UpdateData(uint model, string name)
        {
            name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
            if (_models.TryGetValue(model, out var data))
                data.AllNames = $"{data.AllNames}\n{name}";
            else
                data = new Data(modelSheet!.GetRow(model)!, name);
            _models[model] = data;
        }

        var companionSheet = dataManager.GetExcelSheet<Companion>()!;
        foreach (var companion in companionSheet.Where(c => c.Model.Row != 0 && c.Singular.RawData.Length > 0))
            UpdateData(companion.Model.Row, companion.Singular.ToDalamudString().TextValue);

        var mountSheet = dataManager.GetExcelSheet<Mount>()!;
        foreach (var mount in mountSheet.Where(c => c.ModelChara.Row != 0 && c.Singular.RawData.Length > 0))
            UpdateData(mount.ModelChara.Row, mount.Singular.ToDalamudString().TextValue);

        var bNpcNames = dataManager.GetExcelSheet<BNpcName>()!;
        foreach (var (model, list) in NpcNames.ModelCharas)
        {
            foreach (var nameId in list)
            {
                var name = nameId >= 0
                    ? bNpcNames.GetRow((uint)nameId)?.Singular.ToDalamudString().TextValue ?? string.Empty
                    : NpcNames.Names[~nameId];
                if (name.Length == 0)
                    continue;

                UpdateData(model, name);
            }
        }

        _modelByData = new Dictionary<ulong, Data>((int)modelSheet.RowCount);
        foreach (var mdl in modelSheet)
        {
            var unk5 = (ulong)mdl.Unknown5 << 8;
            var key = mdl.Type switch
            {
                1 => mdl.Type | unk5,
                2 => mdl.Type | unk5 | ((ulong)mdl.Model << 16),
                3 => mdl.Type | unk5 | ((ulong)mdl.Model << 16) | ((ulong)mdl.Base << 32) | ((ulong)mdl.Base << 40),
                _ => 0u,
            };
            if (key != 0)
                _modelByData.TryAdd(key, _models.TryGetValue(mdl.RowId, out var d) ? d : new Data(mdl, string.Empty));
        }
    }
}
