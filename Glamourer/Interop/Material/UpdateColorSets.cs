using OtterGui.Services;
using Penumbra.GameData;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop.Material;

public sealed class UpdateColorSets : FastHook<UpdateColorSets.Delegate>
{
    public delegate void Delegate(Model model, uint unk);

    private readonly ThreadLocal<Model> _updatingModel = new(() => Model.Null);

    public UpdateColorSets(HookManager hooks)
        => Task = hooks.CreateHook<Delegate>("Update Color Sets", Sigs.UpdateColorSets, Detour, true);

    private void Detour(Model model, uint unk)
    {
        _updatingModel.Value = model;
        Task.Result.Original(model, unk);
        _updatingModel.Value = Model.Null;
    }

    public Model Get()
        => _updatingModel.Value;
}
