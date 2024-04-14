using Glamourer.Api.Enums;
using Newtonsoft.Json.Linq;

namespace Glamourer.Api.Api;

public interface IGlamourerApiState
{
    public (GlamourerApiEc, JObject?) GetState(int objectIndex, uint key);
    public (GlamourerApiEc, JObject?) GetStateName(string objectName, uint key);

    public GlamourerApiEc ApplyState(object applyState, int objectIndex, uint key, ApplyFlag flags);

    public GlamourerApiEc ApplyStateName(object state, string objectName, uint key, ApplyFlag flags);

    public GlamourerApiEc RevertState(int objectIndex, uint key, ApplyFlag flags);
    public GlamourerApiEc RevertStateName(string objectName, uint key, ApplyFlag flags);

    public GlamourerApiEc UnlockState(int objectIndex, uint key);
    public GlamourerApiEc UnlockStateName(string objectName, uint key);
    public int            UnlockAll(uint key);

    public GlamourerApiEc RevertToAutomation(int objectIndex, uint key, ApplyFlag flags);
    public GlamourerApiEc RevertToAutomationName(string objectName, uint key, ApplyFlag flags);

    public event Action<nint>? StateChanged;

    public event Action<bool>? GPoseChanged;
}
