using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Glamourer.Services;

public class PhrasingService
{
    private readonly Configuration _config;
    private readonly SHA256        _hasher = SHA256.Create();

    public bool Phrasing1 { get; private set; }
    public bool Phrasing2 { get; private set; }

    public PhrasingService(Configuration config)
    {
        _config   = config;
        Phrasing1 = CheckPhrasing(_config.Phrasing1, P1);
        Phrasing2 = CheckPhrasing(_config.Phrasing2, P2);
    }

    public void SetPhrasing1(string newPhrasing)
    {
        if (_config.Phrasing1 == newPhrasing)
            return;

        _config.Phrasing1 = newPhrasing;
        _config.Save();
        Phrasing1 = CheckPhrasing(newPhrasing, P1);
    }

    public void SetPhrasing2(string newPhrasing)
    {
        if (_config.Phrasing2 == newPhrasing)
            return;

        _config.Phrasing2 = newPhrasing;
        _config.Save();
        Phrasing2 = CheckPhrasing(newPhrasing, P2);
    }

    private bool CheckPhrasing(string phrasing, ReadOnlySpan<byte> data)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(phrasing));
        var       sha    = _hasher.ComputeHash(stream);
        return data.SequenceEqual(sha);
    }

    // @formatter:off
    private static ReadOnlySpan<byte> P1 => new byte[] { 0xD1, 0x35, 0xD7, 0x18, 0xBE, 0x45, 0x42, 0xBD, 0x88, 0x77, 0x7E, 0xC4, 0x41, 0x06, 0x34, 0x4D, 0x71, 0x3A, 0xC5, 0xCC, 0xA4, 0x1B, 0x7D, 0x3F, 0x3B, 0x86, 0x07, 0xCB, 0x63, 0xD7, 0xF9, 0xDB };
    private static ReadOnlySpan<byte> P2 => new byte[] { 0x6A, 0x84, 0x12, 0xEA, 0x3B, 0x03, 0x2E, 0xD9, 0xA3, 0x51, 0xB0, 0x4F, 0xE7, 0x4D, 0x59, 0x87, 0xA9, 0xA1, 0x6E, 0x08, 0xC7, 0x3E, 0xD3, 0x15, 0xEE, 0x40, 0x2C, 0xB3, 0x44, 0x78, 0x1F, 0xA0 };  
    // @formatter:on
}
