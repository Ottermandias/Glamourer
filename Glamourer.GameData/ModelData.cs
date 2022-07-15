using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.GeneratedSheets;
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
    }

    private readonly SortedList<uint, Data> _models;

    public IReadOnlyDictionary<uint, Data> Models
        => _models;

    public ModelData(DataManager dataManager)
    {
        var modelSheet = dataManager.GetExcelSheet<ModelChara>();

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
    }
}
