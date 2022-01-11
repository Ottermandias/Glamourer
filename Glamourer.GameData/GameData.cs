using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using Penumbra.GameData.Enums;

namespace Glamourer
{
    public static class GameData
    {
        private static Dictionary<byte, Stain>?           _stains;
        private static Dictionary<EquipSlot, List<Item>>? _itemsBySlot;
        private static Dictionary<byte, Job>?             _jobs;
        private static Dictionary<ushort, JobGroup>?        _jobGroups;
        private static SortedList<uint, ModelChara>?      _models;

        public static IReadOnlyDictionary<uint, ModelChara> Models(DataManager dataManager)
        {
            if (_models != null)
                return _models;

            var sheet = dataManager.GetExcelSheet<ModelChara>()!;

            _models = new SortedList<uint, ModelChara>((int) sheet.RowCount);
            foreach (var model in sheet.Where(m => m.Type != 0))
                _models.Add(model.RowId, model);
            return _models;
        }

        public static IReadOnlyDictionary<byte, Stain> Stains(DataManager dataManager)
        {
            if (_stains != null)
                return _stains;

            var sheet = dataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Stain>()!;
            _stains = sheet.Where(s => s.Color != 0).ToDictionary(s => (byte) s.RowId, s => new Stain((byte) s.RowId, s));
            return _stains;
        }

        public static IReadOnlyDictionary<EquipSlot, List<Item>> ItemsBySlot(DataManager dataManager)
        {
            if (_itemsBySlot != null)
                return _itemsBySlot;

            var sheet = dataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()!;

            Item EmptySlot(EquipSlot slot)
                => new(sheet.First(), "Nothing", slot);

            static Item EmptyNpc(EquipSlot slot)
                => new(new Lumina.Excel.GeneratedSheets.Item() { ModelMain = 9903 }, "Smallclothes (NPC)", slot);

            _itemsBySlot = new Dictionary<EquipSlot, List<Item>>()
            {
                [EquipSlot.Head]     = new(200) { EmptySlot(EquipSlot.Head), EmptyNpc(EquipSlot.Head) },
                [EquipSlot.Body]     = new(200) { EmptySlot(EquipSlot.Body), EmptyNpc(EquipSlot.Body) },
                [EquipSlot.Hands]    = new(200) { EmptySlot(EquipSlot.Hands), EmptyNpc(EquipSlot.Hands) },
                [EquipSlot.Legs]     = new(200) { EmptySlot(EquipSlot.Legs), EmptyNpc(EquipSlot.Legs) },
                [EquipSlot.Feet]     = new(200) { EmptySlot(EquipSlot.Feet), EmptyNpc(EquipSlot.Feet) },
                [EquipSlot.RFinger]  = new(200) { EmptySlot(EquipSlot.RFinger), EmptyNpc(EquipSlot.RFinger) },
                [EquipSlot.Neck]     = new(200) { EmptySlot(EquipSlot.Neck), EmptyNpc(EquipSlot.Neck) },
                [EquipSlot.MainHand] = new(1000) { EmptySlot(EquipSlot.MainHand) },
                [EquipSlot.OffHand]  = new(200) { EmptySlot(EquipSlot.OffHand) },
                [EquipSlot.Wrists]   = new(200) { EmptySlot(EquipSlot.Wrists), EmptyNpc(EquipSlot.Wrists) },
                [EquipSlot.Ears]     = new(200) { EmptySlot(EquipSlot.Ears), EmptyNpc(EquipSlot.Ears) },
            };

            foreach (var item in sheet)
            {
                var name = item.Name.ToString();
                if (!name.Any())
                    continue;

                var slot = (EquipSlot) item.EquipSlotCategory.Row;
                if (slot == EquipSlot.Unknown)
                    continue;

                slot = slot.ToSlot();
                if (!_itemsBySlot.TryGetValue(slot, out var list))
                    continue;

                list.Add(new Item(item, name, slot));
            }

            foreach (var list in _itemsBySlot.Values)
                list.Sort((i1, i2) => string.Compare(i1.Name, i2.Name, StringComparison.InvariantCulture));

            _itemsBySlot[EquipSlot.LFinger] = _itemsBySlot[EquipSlot.RFinger];
            return _itemsBySlot;
        }

        public static IReadOnlyDictionary<byte, Job> Jobs(DataManager dataManager)
        {
            if (_jobs != null)
                return _jobs;

            var sheet = dataManager.GetExcelSheet<ClassJob>()!;
            _jobs = sheet.ToDictionary(j => (byte)j.RowId, j => new Job(j));
            return _jobs;
        }

        public static IReadOnlyDictionary<ushort, JobGroup> JobGroups(DataManager dataManager)
        {
            if (_jobGroups != null)
                return _jobGroups;

            var sheet = dataManager.GetExcelSheet<ClassJobCategory>()!;
            var jobs = dataManager.GetExcelSheet<ClassJob>(ClientLanguage.English)!;

            static bool ValidIndex(uint idx)
            {
                if (idx > 0 && idx < 36)
                    return true;

                return idx switch
                {
                    91  => true,
                    92  => true,
                    96  => true,
                    98  => true,
                    99  => true,
                    111 => true,
                    112 => true,
                    129 => true,
                    149 => true,
                    150 => true,
                    156 => true,
                    157 => true,
                    158 => true,
                    159 => true,
                    180 => true,
                    181 => true,
                    _   => false,
                };
            }

            _jobGroups = sheet.Where(j => ValidIndex(j.RowId))
                .ToDictionary(j => (ushort) j.RowId, j => new JobGroup(j, jobs));
            return _jobGroups;
        }
    }
}
