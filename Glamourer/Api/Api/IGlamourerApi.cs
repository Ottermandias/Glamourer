namespace Glamourer.Api.Api;

public interface IGlamourerApi : IGlamourerApiBase
{
    public IGlamourerApiDesigns Designs { get; }
    public IGlamourerApiItems   Items   { get; }
    public IGlamourerApiState   State   { get; }
}