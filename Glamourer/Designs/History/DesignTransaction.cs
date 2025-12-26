using Glamourer.GameData;
using Glamourer.Interop.Material;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using Penumbra.GameData.Enums;

namespace Glamourer.Designs.History;

/// <remarks> Only Designs. Can not be reverted. </remarks>
public readonly record struct CreationTransaction(string Name, string? Path)
    : ITransaction
{
    public ITransaction? Merge(ITransaction other)
        => null;

    public void Revert(IDesignEditor editor, object data)
    { }
}

/// <remarks> Only Designs. </remarks>
public readonly record struct RenameTransaction(string Old, string New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is RenameTransaction other ? new RenameTransaction(other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
        => ((DesignManager)editor).Rename((Design)data, Old);
}

/// <remarks> Only Designs. </remarks>
public readonly record struct DescriptionTransaction(string Old, string New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is DescriptionTransaction other ? new DescriptionTransaction(other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
        => ((DesignManager)editor).ChangeDescription((Design)data, Old);
}

/// <remarks> Only Designs. </remarks>
public readonly record struct DesignColorTransaction(string Old, string New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is DesignColorTransaction other ? new DesignColorTransaction(other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
        => ((DesignManager)editor).ChangeColor((Design)data, Old);
}

/// <remarks> Only Designs. </remarks>
public readonly record struct TagAddedTransaction(string New, int Index)
    : ITransaction
{
    public ITransaction? Merge(ITransaction other)
        => null;

    public void Revert(IDesignEditor editor, object data)
        => ((DesignManager)editor).RemoveTag((Design)data, Index);
}

/// <remarks> Only Designs. </remarks>
public readonly record struct TagRemovedTransaction(string Old, int Index)
    : ITransaction
{
    public ITransaction? Merge(ITransaction other)
        => null;

    public void Revert(IDesignEditor editor, object data)
        => ((DesignManager)editor).AddTag((Design)data, Old);
}

/// <remarks> Only Designs. </remarks>
public readonly record struct TagChangedTransaction(string Old, string New, int IndexOld, int IndexNew)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is TagChangedTransaction other && other.IndexNew == IndexOld
            ? new TagChangedTransaction(other.Old, New, other.IndexOld, IndexNew)
            : null;

    public void Revert(IDesignEditor editor, object data)
        => ((DesignManager)editor).RenameTag((Design)data, IndexNew, Old);
}

/// <remarks> Only Designs. </remarks>
public readonly record struct ModAddedTransaction(Mod Mod, ModSettings Settings)
    : ITransaction
{
    public ITransaction? Merge(ITransaction other)
        => null;

    public void Revert(IDesignEditor editor, object data)
        => ((DesignManager)editor).RemoveMod((Design)data, Mod);
}

/// <remarks> Only Designs. </remarks>
public readonly record struct ModRemovedTransaction(Mod Mod, ModSettings Settings)
    : ITransaction
{
    public ITransaction? Merge(ITransaction other)
        => null;

    public void Revert(IDesignEditor editor, object data)
        => ((DesignManager)editor).AddMod((Design)data, Mod, Settings);
}

/// <remarks> Only Designs. </remarks>
public readonly record struct ModUpdatedTransaction(Mod Mod, ModSettings Old, ModSettings New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is ModUpdatedTransaction other && Mod == other.Mod ? new ModUpdatedTransaction(Mod, other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
        => ((DesignManager)editor).UpdateMod((Design)data, Mod, Old);
}

/// <remarks> Only Designs. </remarks>
public readonly record struct MaterialTransaction(MaterialValueIndex Index, ColorRow? Old, ColorRow? New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is MaterialTransaction other && Index == other.Index ? new MaterialTransaction(Index, other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
    {
        if (editor is DesignManager e)
            e.ChangeMaterialValue((Design)data, Index, Old);
    }
}

/// <remarks> Only Designs. </remarks>
public readonly record struct MaterialRevertTransaction(MaterialValueIndex Index, bool Old, bool New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction other)
        => null;

    public void Revert(IDesignEditor editor, object data)
        => ((DesignManager)editor).ChangeMaterialRevert((Design)data, Index, Old);
}

/// <remarks> Only Designs. </remarks>
public readonly record struct ApplicationTransaction(object Index, bool Old, bool New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction other)
        => null;

    public void Revert(IDesignEditor editor, object data)
    {
        var manager = (DesignManager)editor;
        var design  = (Design)data;
        switch (Index)
        {
            case CustomizeIndex idx:
                manager.ChangeApplyCustomize(design, idx, Old);
                break;
            case (EquipSlot slot, true):
                manager.ChangeApplyStains(design, slot, Old);
                break;
            case (EquipSlot slot, _):
                manager.ChangeApplyItem(design, slot, Old);
                break;
            case BonusItemFlag slot:
                manager.ChangeApplyBonusItem(design, slot, Old);
                break;
            case CrestFlag slot:
                manager.ChangeApplyCrest(design, slot, Old);
                break;
            case MetaIndex slot:
                manager.ChangeApplyMeta(design, slot, Old);
                break;
            case CustomizeParameterFlag slot:
                manager.ChangeApplyParameter(design, slot, Old);
                break;
            case MaterialValueIndex slot:
                manager.ChangeApplyMaterialValue(design, slot, Old);
                break;
        }
    }
}
