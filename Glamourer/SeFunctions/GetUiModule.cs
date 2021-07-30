using System;
using Dalamud.Game;

namespace Glamourer.SeFunctions
{
    public delegate IntPtr GetUiModuleDelegate(IntPtr baseUiObj);

    public sealed class GetUiModule : SeFunctionBase<GetUiModuleDelegate>
    {
        public GetUiModule(SigScanner sigScanner)
            : base(sigScanner, "E8 ?? ?? ?? ?? 48 83 7F ?? 00 48 8B F0")
        { }
    }
}
