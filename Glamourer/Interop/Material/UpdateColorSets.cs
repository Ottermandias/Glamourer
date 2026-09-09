using Luna;
using Penumbra.GameData;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop.Material;

public sealed class CreateNewModel : FastHook<CreateNewModel.Delegate>
{
    public delegate nint Delegate(Model model, uint unk);

    private readonly ThreadLocal<Model> _updatingModel = new(() => Model.Null);

    public CreateNewModel(HookManager hooks)
        => Task = hooks.CreateHook<Delegate>("Create New Model", Sigs.CreateNewModel, Detour, true);

    private nint Detour(Model model, uint modelSlot)
    {
        _updatingModel.Value = model;
        var ret = Task.Result!.Original(model, modelSlot);
        _updatingModel.Value = Model.Null;
        return ret;
    }

    public Model Get()
        => _updatingModel.Value;
}
