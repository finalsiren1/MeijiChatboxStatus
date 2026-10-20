using System.Buffers.Binary;
using System.Text;

namespace MeijiChatboxStatus.Core;

/// <summary>A decoded OSC message. Only the argument types VRChat actually sends are supported.</summary>
public readonly record struct OscMessage(string Address, object?[] Args)
{
    /// <summary>Interprets the first argument as an "on/off" value (bool, int > 0, float > threshold).</summary>
    public bool? AsBool(float threshold = 0.5f) => Args.Length == 0 ? null : Args[0] switch
    {
        bool b => b,
        int i => i > 0,
        float f => f > threshold,
        _ => null,
    };

    public string ArgText => Args.Length == 0 ? "" : Args[0] switch
    {
        bool b => b ? "true" : "false",
        float f => f.ToString("0.###"),
        null => "nil",
        var o => o.ToString() ?? "",
    };
}

/// <summary>Minimal OSC 1.0 encoder/decoder (messages + bundles).</summary>
public static class Osc
{
    public static byte[] Encode(string address, params object[] args)
    {
        using var ms = new MemoryStream(64);
        WriteString(ms, address);
        var tags = new StringBuilder(",");
        foreach (var a in args)
        {
            tags.Append(a switch
            {
                bool b => b ? 'T' : 'F',
                int => 'i',
                float => 'f',
                string => 's',
                _ => throw new ArgumentException($"Unsupported OSC argument type {a.GetType()}"),
            });
        }
        WriteString(ms, tags.ToString());
        Span<byte> buf = stackalloc byte[4];
        foreach (var a in args)
        {
            switch (a)
            {
                case int i: BinaryPrimitives.WriteInt32BigEndian(buf, i); ms.Write(buf); break;
                case float f: BinaryPrimitives.WriteSingleBigEndian(buf, f); ms.Write(buf); break;
                case string s: WriteString(ms, s); break;
            }
        }
        return ms.ToArray();
    }

    /// <summary>Decodes a packet (single message or bundle). Malformed data yields no messages instead of throwing.</summary>
    public static IEnumerable<OscMessage> Decode(byte[] data) => Decode(data, 0, data.Length);

    private static IEnumerable<OscMessage> Decode(byte[] data, int offset, int length)
    {
        var list = new List<OscMessage>(1);
        try { DecodeInto(data, offset, length, list, 0); }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentException) { /* malformed packet: ignore */ }
        return list;
    }

    private static void DecodeInto(byte[] data, int offset, int length, List<OscMessage> output, int depth)
    {
        int end = offset + length;
        if (length < 4 || end > data.Length || depth > 8) return;

        if (data[offset] == (byte)'#')
        {
            int i = offset;
            if (ReadString(data, ref i, end) != "#bundle") return;
            i += 8; // timetag
            while (i + 4 <= end)
            {
                int size = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(i, 4));
                i += 4;
                if (size <= 0 || i + size > end) return;
                DecodeInto(data, i, size, output, depth + 1);
                i += size;
            }
            return;
        }

        int p = offset;
        string address = ReadString(data, ref p, end);
        if (address.Length == 0 || address[0] != '/') return;
        if (p >= end) { output.Add(new OscMessage(address, [])); return; }

        string tags = ReadString(data, ref p, end);
        if (tags.Length == 0 || tags[0] != ',') return;

        var args = new List<object?>(tags.Length - 1);
        for (int t = 1; t < tags.Length; t++)
        {
            switch (tags[t])
            {
                case 'T': args.Add(true); break;
                case 'F': args.Add(false); break;
                case 'N': args.Add(null); break;
                case 'i': args.Add(BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(p, 4))); p += 4; break;
                case 'f': args.Add(BinaryPrimitives.ReadSingleBigEndian(data.AsSpan(p, 4))); p += 4; break;
                case 's': args.Add(ReadString(data, ref p, end)); break;
                default: return; // unknown type: can't safely continue parsing
            }
        }
        output.Add(new OscMessage(address, args.ToArray()));
    }

    private static string ReadString(byte[] data, ref int index, int end)
    {
        int start = index;
        while (index < end && data[index] != 0) index++;
        if (index >= end) throw new ArgumentException("Unterminated OSC string");
        string s = Encoding.UTF8.GetString(data, start, index - start);
        index = (index + 4) & ~3; // skip the terminator and pad to a 4-byte boundary
        return s;
    }

    private static void WriteString(MemoryStream ms, string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        ms.Write(bytes);
        int pad = 4 - (bytes.Length % 4); // always at least one null terminator
        for (int i = 0; i < pad; i++) ms.WriteByte(0);
    }
}
