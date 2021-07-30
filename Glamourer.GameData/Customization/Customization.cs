using System.Runtime.InteropServices;

namespace Glamourer.Customization
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct Customization
    {
        [FieldOffset(0)]
        public readonly CustomizationId Id;

        [FieldOffset(1)]
        public readonly byte Value;

        [FieldOffset(2)]
        public readonly ushort CustomizeId;

        [FieldOffset(4)]
        public readonly uint IconId;

        [FieldOffset(4)]
        public readonly uint Color;

        public Customization(CustomizationId id, byte value, uint data = 0, ushort customizeId = 0)
        {
            Id          = id;
            Value       = value;
            IconId      = data;
            Color       = data;
            CustomizeId = customizeId;
        }
    }
}
