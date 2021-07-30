using System;
using Dalamud.Game;

namespace Glamourer.SeFunctions
{
    public delegate IntPtr ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unk1, byte unk2);

    public sealed class ProcessChatBox : SeFunctionBase<ProcessChatBoxDelegate>
    {
        public ProcessChatBox(SigScanner sigScanner)
            : base(sigScanner, "48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9")
        { }
    }
}
