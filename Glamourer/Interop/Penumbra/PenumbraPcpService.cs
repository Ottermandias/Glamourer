using Dalamud.Plugin;
using Luna;
using Newtonsoft.Json.Linq;
using Penumbra.Api.IpcSubscribers;

namespace Glamourer.Interop.Penumbra;

public sealed class PenumbraPcpService(IDalamudPluginInterface pluginInterface) : IDisposable
{
    private readonly EventSubscriber<JObject, string, Guid>   _pcpParsed  = ParsingPcp.Subscriber(pluginInterface);
    private readonly EventSubscriber<JObject, ushort, string> _pcpCreated = CreatingPcp.Subscriber(pluginInterface);

    public event Action<JObject, ushort, string> Created
    {
        add => _pcpCreated.Event += value;
        remove => _pcpCreated.Event -= value;
    }

    public event Action<JObject, string, Guid> Parsed
    {
        add => _pcpParsed.Event += value;
        remove => _pcpParsed.Event -= value;
    }

    public void Dispose()
    {
        _pcpCreated.Dispose();
        _pcpParsed.Dispose();
    }
}
