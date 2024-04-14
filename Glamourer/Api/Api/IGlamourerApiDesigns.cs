using Glamourer.Api.Enums;

namespace Glamourer.Api.Api;

public interface IGlamourerApiDesigns
{
    public Dictionary<Guid, string> GetDesignList();

    public GlamourerApiEc ApplyDesign(Guid designId, int objectIndex, uint key, ApplyFlag flags);

    public GlamourerApiEc ApplyDesignName(Guid designId, string objectName, uint key, ApplyFlag flags);
}
