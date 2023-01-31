using Glamourer.Designs;
using Glamourer.Interop;

namespace Glamourer.State;

public interface IDesign
{
    public ref CharacterData Data { get; }

    public void ApplyToActor(Actor a);
}
