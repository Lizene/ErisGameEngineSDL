using ErisMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath;

internal struct ColorByte
{
    // Struct for containing an RGB format color in bytes and doing color operations.
    byte _r, _g, _b;
    public byte r { get { return _r; } set { _r = value; } }
    public byte g { get { return _g; } set { _g = value; } }
    public byte b { get { return _b; } set { _b = value; } }

    public static ColorByte BLACK = new ColorByte(0, 0, 0); 
    public static ColorByte WHITE = new ColorByte(255, 255, 255);
    public static ColorByte RED = new ColorByte(255, 0, 0);
    public static ColorByte GREEN = new ColorByte(0, 255, 0);
    public static ColorByte BLUE = new ColorByte(0, 0, 255);
    public ColorByte(byte r, byte g, byte b)
    {
        _r = r; _g = g; _b = b;
    }
    public ColorByte(byte[] rgb)
    {
        _r = rgb[0]; _g = rgb[1]; _b = rgb[2];
    }
    public static ColorByte Random() => new ColorByte(RandomNumberGenerator.GetBytes(3)); //Generate random color
    static byte ByteUpperClamp(uint a) //For addition
    {
        if (a > 255) a = 255;
        return (byte)a;
    }
    static byte ByteLowerClamp(int a) //For subtraction
    {
        if (a < 0) a = 0;
        return (byte)a;
    }
    public uint ToUint()
    {
        return (uint)((_r << 16) | (_g << 8) | _b); //Bitshift bytes to uint
    }
    public static ColorByte operator +(ColorByte c1, ColorByte c2) //Add channelwise
    {
        byte r = ByteUpperClamp((uint)(c1._r + c2._r));
        byte g = ByteUpperClamp((uint)(c1._g + c2._g));
        byte b = ByteUpperClamp((uint)(c1._b + c2._b));
        return new ColorByte(r, g, b);
    }
    public static ColorByte operator -(ColorByte c1, ColorByte c2) //Subtract channelwise
    {
        byte r = ByteLowerClamp(c1._r - c1._r);
        byte g = ByteLowerClamp(c1._g - c1._g);
        byte b = ByteLowerClamp(c1._b - c1._b);
        return new ColorByte(r, g, b);
    }
    public static ColorByte operator *(ColorByte c1, float f) //Multiply each channel by float and clamp
        => new ColorByte((byte)(c1._r * f), (byte)(c1._g * f), (byte)(c1._b * f));
    public static ColorByte operator *(float f, ColorByte c1) => c1 * f;
    public static ColorByte operator /(ColorByte c1, float f) //Divide each channel by float and clamp
        => new ColorByte((byte)(c1._r / f), (byte)(c1._g / f), (byte)(c1._b / f));
    public static bool operator ==(ColorByte c1, ColorByte c2) => c1.r == c2.r && c1.g == c2.g && c1.b == c2.b; //True if each channel is equal
    public static bool operator !=(ColorByte c1, ColorByte c2) => !(c1==c2); //True if at least one of the channels isn't equal.
    public override string ToString() => $"Color: ({_r}, {_g}, {_b})";
}
