using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal struct Vec2int
    {
        int _x, _y;
        public static Vec2int zero = new Vec2int(0, 0);
        public static Vec2int one = new Vec2int(1, 1);
        public static Vec2int left = new Vec2int(-1, 0);
        public static Vec2int right = new Vec2int(1, 0);
        public static Vec2int down = new Vec2int(0, -1);
        public static Vec2int up = new Vec2int(0, 1);
        public Vec2int(int x, int y)
        {
            _x = x; _y = y;
        }
        public int x
        {
            get { return _x; }
            set { _x = value; }
        }
        public int y
        {
            get { return _y; }
            set {  _y = value; }
        }
        public float magnitude() 
        {
            return (float)Math.Sqrt(Math.Pow(_x, 2) + Math.Pow(_y, 2));
        }
        public Vec2 normalized()
        {
            float m = magnitude();
            if (m < 0.001f) return Vec2.zero;
            return new Vec2(_x / m, _y / m);
        }
        public Vec2 ToFloat() => new Vec2(_x, _y);
        public static float Dot(Vec2int a, Vec2int b) => a._x * b._x + a._y * b._y;

        public static Vec2int operator +(Vec2int a, Vec2int b) => new Vec2int(a._x+b._x, a._y+b._y);
        public static Vec2 operator +(Vec2int a, Vec2 b) => new Vec2(a._x+b.x, a._y+b.y);
        public static Vec2 operator +(Vec2 a, Vec2int b) => b+a;
        public static Vec2int operator -(Vec2int a, Vec2int b) => new Vec2int(a._x-b._x, a._y-b._y);
        public static Vec2 operator -(Vec2int a, Vec2 b) => new Vec2(a._x-b.x, a._y-b.y);
        public static Vec2 operator -(Vec2 a, Vec2int b) => new Vec2(a.x-b._x, a.y-b._y);
        public static Vec2int operator -(Vec2int a) => new Vec2int(-a._x,-a._y);
        public static Vec2int operator *(Vec2int a, int b) => new Vec2int(a._x*b, a._y*b);
        public static Vec2int operator *(int b, Vec2int a) => new Vec2int(a._x*b, a._y*b);
        public static Vec2 operator *(Vec2int a, float b) => new Vec2(a._x * b, a._y * b);
        public static Vec2 operator *(float b, Vec2int a) => new Vec2(a._x * b, a._y * b);
        public static Vec2int operator /(Vec2int a, int b) => new Vec2int(a._x/b, a._y/b);
        public static Vec2 operator /(Vec2int a, float b) => new Vec2(a._x/b, a._y/b);

        public override string ToString() => $"Vector 2D (Int): ({_x}, {_y})";
    }
}
