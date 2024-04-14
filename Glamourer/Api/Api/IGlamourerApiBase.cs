namespace Glamourer.Api.Api;

public interface IGlamourerApiBase
{
    public (int Major, int Minor) ApiVersion { get; }
}
