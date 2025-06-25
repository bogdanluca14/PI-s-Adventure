using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public enum ObstacleType : byte
{
    Triangle = 0,
    Circle = 1,
    Diamond = 2,
    Square = 3,
    Star = 4,
    Main = 5,
}

[Serializable]
public struct ObstacleData
{
    public float X,
        Y;
    public ushort Rotation;
    public ObstacleType Type;

    public ObstacleData(float x, float y, ushort rot, ObstacleType t) =>
        (X, Y, Rotation, Type) = (x, y, rot, t);
}

public class LevelEncoder
{
    // In acest script, ne vom ocupa de "criptarea" si "encriptarea" nivelului
    // creat in cod si viceversa. Avem un intreg sistem de Encoding facut de la
    // zero. Vom genera eficient acest cod folosind doar caractere ASCII.

    // Variabile Locale

    private const int BufferSize = 512;
    private readonly List<ObstacleData> list = new List<ObstacleData>();
    private static readonly string Base85Table =
        "!0#$%&:()*+,-;/\"123456789'.<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstu";

    // Salvam obstacolul in functie de parametri negrupati
    public void Save(ObstacleType type, Vector2 pos, int rotation)
    {
        float x = Mathf.Round(pos.x * 10f) / 10f;
        float y = Mathf.Round(pos.y * 10f) / 10f;
        ushort rot = (ushort)rotation;
        list.Add(new ObstacleData(x, y, rot, type));
    }

    // Salvam obstacolul in functie de parametri grupati
    public void Save(ObstacleData data)
    {
        float x = Mathf.Round(data.X * 10f) / 10f;
        float y = Mathf.Round(data.Y * 10f) / 10f;
        list.Add(new ObstacleData(x, y, data.Rotation, data.Type));
    }

    // Preluam codul generat in urma salvarilor
    public string GetCode()
    {
        if (list.Count == 0)
            return string.Empty;

        var writer = new BitWriter(new byte[BufferSize]);
        writer.WriteVarUInt((uint)list.Count);
        ObstacleData? prev = null;

        foreach (var cur in list)
        {
            int xi = Mathf.RoundToInt(cur.X * 10f);
            int yi = Mathf.RoundToInt(cur.Y * 10f);
            int pdx = prev.HasValue ? Mathf.RoundToInt(prev.Value.X * 10f) : 0;
            int pdy = prev.HasValue ? Mathf.RoundToInt(prev.Value.Y * 10f) : 0;
            int pr = prev.HasValue ? prev.Value.Rotation : 0;

            int dx = prev.HasValue ? xi - pdx : xi;
            int dy = prev.HasValue ? yi - pdy : yi;
            int dr = cur.Rotation - pr;

            writer.WriteVarUInt(ZigZagInt.Encode(dx));
            writer.WriteVarUInt(ZigZagInt.Encode(dy));
            writer.WriteVarUInt(ZigZagInt.Encode(dr));
            writer.WriteBits((uint)cur.Type, 3);
            prev = cur;
        }

        // Grupam bitii cate 4 pentru a ne incadra in tabel
        byte[] raw = writer.GetBytes();
        int pad = (4 - (raw.Length % 4)) % 4;

        if (pad > 0)
        {
            var tmp = new byte[raw.Length + pad];
            Array.Copy(raw, tmp, raw.Length);
            raw = tmp;
        }

        return Base85Encode(raw);
    }

    // Incarcam nivelul in functie de codul introdus
    public static List<ObstacleData> Load(string code)
    {
        try
        {
            var raw = Base85Decode(code);
            var reader = new BitReader(raw);
            int count = (int)reader.ReadVarUInt();
            var outList = new List<ObstacleData>(count);
            int px = 0,
                py = 0;
            ushort pr = 0;

            for (int i = 0; i < count; i++)
            {
                int dx = ZigZagInt.Decode(reader.ReadVarUInt());
                int dy = ZigZagInt.Decode(reader.ReadVarUInt());
                int dr = ZigZagInt.Decode(reader.ReadVarUInt());
                ObstacleType type = (ObstacleType)reader.ReadBits(3);

                int xi = px + dx;
                int yi = py + dy;
                ushort rot = (ushort)(pr + dr);
                float x = xi / 10f;
                float y = yi / 10f;

                outList.Add(new ObstacleData(x, y, rot, type));
                px = xi;
                py = yi;
                pr = rot;
            }
            return outList;
        }
        catch
        {
            // In cazul unui cod invalid, nu facem nimic
            return null;
        }
    }

    // Encodarea propriu-zisa a bitilor in Baza 85 (Tabelul anterior)
    private static string Base85Encode(byte[] data)
    {
        var sb = new StringBuilder(data.Length / 4 * 5);

        for (int i = 0; i < data.Length; i += 4)
        {
            uint chunk = (uint)(data[i] << 24 | data[i + 1] << 16 | data[i + 2] << 8 | data[i + 3]);
            char[] enc = new char[5];

            for (int k = 4; k >= 0; k--)
            {
                enc[k] = Base85Table[(int)(chunk % 85)];
                chunk /= 85;
            }

            sb.Append(enc);
        }

        return sb.ToString();
    }

    // Decodarea propriu-zisa a codului in Baza 85 (Tabelul anterior)
    private static byte[] Base85Decode(string code)
    {
        int blocks = code.Length / 5;
        var raw = new byte[blocks * 4];

        for (int b = 0; b < blocks; b++)
        {
            uint chunk = 0;

            for (int k = 0; k < 5; k++)
            {
                chunk = chunk * 85 + (uint)Base85Table.IndexOf(code[b * 5 + k]);
            }

            raw[b * 4 + 0] = (byte)(chunk >> 24);
            raw[b * 4 + 1] = (byte)(chunk >> 16);
            raw[b * 4 + 2] = (byte)(chunk >> 8);
            raw[b * 4 + 3] = (byte)(chunk);
        }

        return raw;
    }
}

// Folosim o metoda cunoscuta pentru a imparti bitii in calupuri
static class ZigZagInt
{
    public static uint Encode(int n) => (uint)((n << 1) ^ (n >> 31));

    public static int Decode(uint n) => (int)((n >> 1) ^ -(int)(n & 1));
}

class BitWriter
{
    // Variabile Globale

    public BitWriter(byte[] buffer) => (buf, bytePos, bitPos, curr) = (buffer, 0, 0, 0);

    // Variabile Locale

    private readonly byte[] buf;
    private int bytePos,
        bitPos;
    private byte curr;

    // Scriem bitii in calupul respectiv
    public void WriteBits(uint v, int n)
    {
        for (int i = n - 1; i >= 0; i--)
        {
            curr |= (byte)(((v >> i) & 1) << (7 - bitPos));

            if (++bitPos == 8)
            {
                buf[bytePos++] = curr;
                curr = 0;
                bitPos = 0;
            }
        }
    }

    // Scriem numarul in biti
    public void WriteVarUInt(uint v)
    {
        while (v >= 0x80)
        {
            WriteBits((v & 0x7F) | 0x80, 8);
            v >>= 7;
        }

        WriteBits(v & 0x7F, 8);
    }

    // Preluam bitii
    public byte[] GetBytes()
    {
        int len = bytePos + (bitPos > 0 ? 1 : 0);

        if (bitPos > 0)
            buf[bytePos] = curr;
        var res = new byte[len];
        Array.Copy(buf, res, len);

        return res;
    }
}

class BitReader
{
    // Variabile Locale

    private readonly byte[] buf;
    private int bytePos,
        bitPos;

    public BitReader(byte[] buffer) => (buf, bytePos, bitPos) = (buffer, 0, 0);

    public bool HasMore() => bytePos < buf.Length;

    // Citim bitii dupa numar
    public uint ReadBits(int count)
    {
        uint v = 0;

        for (int i = 0; i < count; i++)
        {
            v = (v << 1) | (uint)((buf[bytePos] >> (7 - bitPos)) & 1);

            if (++bitPos == 8)
            {
                bitPos = 0;
                bytePos++;
            }
        }

        return v;
    }

    // Citim numarul
    public uint ReadVarUInt()
    {
        uint res = 0;
        int shift = 0;

        while (true)
        {
            uint b = ReadBits(8);
            res |= (b & 0x7Fu) << shift;

            if ((b & 0x80) == 0)
                break;
            shift += 7;
        }

        return res;
    }
}
