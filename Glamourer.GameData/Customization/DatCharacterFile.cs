using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Memory;

namespace Glamourer.Customization;

[StructLayout(LayoutKind.Explicit, Size = Size)]
public unsafe struct DatCharacterFile
{
    public const int Size = 4 + 4 + 4 + 4 + Penumbra.GameData.Structs.CustomizeData.Size + 2 + 4 + 41 * 4; // 212

    [FieldOffset(0)]
    private fixed byte _data[Size];

    [FieldOffset(0)]
    public readonly uint Magic = 0x2013FF14;

    [FieldOffset(4)]
    public readonly uint Version = 0x05;

    [FieldOffset(8)]
    private uint _checksum;

    [FieldOffset(12)]
    private readonly uint _padding = 0;

    [FieldOffset(16)]
    private Penumbra.GameData.Structs.CustomizeData _customize;

    [FieldOffset(16 + Penumbra.GameData.Structs.CustomizeData.Size)]
    private ushort _voice;

    [FieldOffset(16 + Penumbra.GameData.Structs.CustomizeData.Size + 2)]
    private uint _timeStamp;

    [FieldOffset(Size - 41 * 4)]
    private fixed byte _description[41 * 4];

    public readonly void Write(Stream stream)
    {
        for (var i = 0; i < Size; ++i)
            stream.WriteByte(_data[i]);
    }

    public static bool Read(Stream stream, out DatCharacterFile file)
    {
        if (stream.Length - stream.Position != Size)
        {
            file = default;
            return false;
        }

        file = new DatCharacterFile(stream);
        return true;
    }

    private DatCharacterFile(Stream stream)
    {
        for (var i = 0; i < Size; ++i)
            _data[i] = (byte)stream.ReadByte();
    }

    public DatCharacterFile(in Customize customize, byte voice, string text)
    {
        SetCustomize(customize);
        SetVoice(voice);
        SetTime(DateTimeOffset.UtcNow);
        SetDescription(text);
        _checksum = CalculateChecksum();
    }

    public readonly uint CalculateChecksum()
    {
        var ret = 0u;
        for (var i = 16; i < Size; i++)
            ret ^= (uint)(_data[i] << ((i - 16) % 24));
        return ret;
    }

    public readonly uint Checksum
        => _checksum;

    public Customize Customize
    {
        readonly get => new(_customize);
        set
        {
            SetCustomize(value);
            _checksum = CalculateChecksum();
        }
    }

    public ushort Voice
    {
        readonly get => _voice;
        set
        {
            SetVoice(value);
            _checksum = CalculateChecksum();
        }
    }

    public string Description
    {
        readonly get
        {
            fixed (byte* ptr = _description)
            {
                return MemoryHelper.ReadStringNullTerminated((nint)ptr);
            }
        }
        set
        {
            SetDescription(value);
            _checksum = CalculateChecksum();
        }
    }

    public DateTimeOffset Time
    {
        readonly get => DateTimeOffset.FromUnixTimeSeconds(_timeStamp);
        set
        {
            SetTime(value);
            _checksum = CalculateChecksum();
        }
    }

    private void SetTime(DateTimeOffset time)
        => _timeStamp = (uint)time.ToUnixTimeSeconds();

    private void SetCustomize(in Customize customize)
        => _customize = customize.Data.Clone();

    private void SetVoice(ushort voice)
        => _voice = voice;

    private void SetDescription(string text)
    {
        fixed (byte* ptr = _description)
        {
            var span = new Span<byte>(ptr, 41 * 4);
            Encoding.UTF8.GetBytes(text.AsSpan(0, Math.Min(40, text.Length)), span);
        }
    }
}
