using ErisMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath;

internal struct ColorByte
{
    byte _r, _g, _b;
    public byte r { get { return _r; } set { _r = value; } }
    public byte g { get { return _g; } set { _g = value; } }
    public byte b { get { return _b; } set { _b = value; } }

    public static ColorByte BLACK = new ColorByte(0, 0, 0); 
    public static ColorByte WHITE = new ColorByte(255, 255, 255);
    public static ColorByte RED = new ColorByte(255, 0, 0);
    public static ColorByte GREEN = new ColorByte(0, 255, 0);
    public static ColorByte BLUE = new ColorByte(0, 0, 255);
    public ColorByte(byte x, byte y, byte z)
    {
        _r = x; _g = y; _b = z;
    }
    static byte ByteUpperClamp(uint a)
    {
        if (a > 255) a = 255;
        return (byte)a;
    }
    static byte ByteLowerClamp(int a)
    {
        if (a < 0) a = 0;
        return (byte)a;
    }
    public static ColorByte operator +(ColorByte c1, ColorByte c2) 
    {
        byte r = ByteUpperClamp((uint)(c1._r + c2._r));
        byte g = ByteUpperClamp((uint)(c1._g + c2._g));
        byte b = ByteUpperClamp((uint)(c1._b + c2._b));
        return new ColorByte(r, g, b);
    }
    public static ColorByte operator -(ColorByte c1, ColorByte c2)
    {
        byte r = ByteLowerClamp(c1._r - c1._r);
        byte g = ByteLowerClamp(c1._g - c1._g);
        byte b = ByteLowerClamp(c1._b - c1._b);
        return new ColorByte(r, g, b);
    }
    public static ColorByte operator *(ColorByte c1, float f)
        => new ColorByte((byte)(c1._r * f), (byte)(c1._g * f), (byte)(c1._b * f));
    public static ColorByte operator *(float f, ColorByte c1) => c1 * f;
    public static ColorByte operator /(ColorByte c1, float f) 
        => new ColorByte((byte)(c1._r / f), (byte)(c1._g / f), (byte)(c1._b / f));
    public override string ToString() => $"Color: ({_r}, {_g}, {_b})";
}
