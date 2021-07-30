using Dalamud.Game;

namespace Glamourer.SeFunctions
{
    public sealed class BaseUiObject : SeAddressBase
    {
        public BaseUiObject(SigScanner sigScanner)
            : base(sigScanner, "48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8")
        { }
    }
}
