using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Glamourer.Customization;
using Glamourer.Services;
using OtterGui.Widgets;
using Penumbra.GameData;

namespace Glamourer.Gui.Tabs;

public class NpcCombo(ActorService actorManager, IdentifierService identifier, IDataManager data)
    : FilterComboBase<CustomizationNpcOptions.NpcData>(new LazyList(actorManager, identifier, data), false, Glamourer.Log)
{
    private class LazyList(ActorService actorManager, IdentifierService identifier, IDataManager data)
        : IReadOnlyList<CustomizationNpcOptions.NpcData>
    {
        private readonly Task<IReadOnlyList<CustomizationNpcOptions.NpcData>> _task 
            = Task.Run(() => CustomizationNpcOptions.CreateNpcData(actorManager.AwaitedService.Data.ENpcs, actorManager.AwaitedService.Data.BNpcs, identifier.AwaitedService, data));

        public IEnumerator<CustomizationNpcOptions.NpcData> GetEnumerator()
            => _task.Result.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public int Count
            => _task.Result.Count;

        public CustomizationNpcOptions.NpcData this[int index]
            => _task.Result[index];
    }

    protected override string ToString(CustomizationNpcOptions.NpcData obj)
        => obj.Name;
}
