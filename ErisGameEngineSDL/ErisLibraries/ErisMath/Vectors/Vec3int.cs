using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal struct Vec3int
    {
        int _x, _y, _z;
        public int x { get { return _x; } set { _x = value; } }
        public int y { get { return _y; } set { _y = value; } }
        public int z { get { return _z; } set { _z = value; } }

        public static Vec3int zero =       new Vec3int(0, 0, 0);
        public static Vec3int one =         new Vec3int(1, 1, 1);
        public static Vec3int left =        new Vec3int(-1, 0, 0);
        public static Vec3int right =      new Vec3int(1, 0, 0);
        public static Vec3int down =      new Vec3int(0, -1, 0);
        public static Vec3int up =          new Vec3int(0, 1, 0);
        public static Vec3int forward = new Vec3int(0, 0, 1);
        public static Vec3int back =      new Vec3int(0, 0, -1);
        public Vec3int(int x, int y, int z)
        {
            _x = x; _y = y; _z = z;
        }
        public float magnitude() =>
            (float) Math.Sqrt(Math.Pow(_x, 2) + Math.Pow(_y, 2) + Math.Pow(_z, 2));
        public Vec3 normalized()
        {
            float m = magnitude();
            return new Vec3(_x / m, _y / m, _z / m);
        }
        public static float Dot(Vec3int a, Vec3int b) => a._x * b._x + a._y * b._y + a._z * b._z;
        public static Vec3int Cross(Vec3int a, Vec3int b) =>
            new Vec3int(a._y * b._z - a._z * b._y, a._z * b._x - a._x * b._z, a._x * b._y - a._y * b._x);

        public Vec3 ToFloat() => new Vec3(_x, _y, _z);

        public static Vec3int operator +(Vec3int a, Vec3int b) => new Vec3int(a._x + b._x, a._y + b._y, a._z + b._z);
        public static Vec3int operator -(Vec3int a, Vec3int b) => new Vec3int(a._x - b._x, a._y - b._y, a._z - b._z);
        public static Vec3int operator -(Vec3int a) => new Vec3int(-a._x, -a._y, -a._z);
        public static Vec3int operator *(Vec3int a, int b) => new Vec3int(a._x * b, a._y * b, a._z * b);
        public static Vec3int operator *(int b, Vec3int a) => new Vec3int(a._x * b, a._y * b, a._z * b);
        public static Vec3int operator /(Vec3int a, int b) => new Vec3int(a._x / b, a._y / b, a._z / b);
        public override string ToString() => $"Vector 3D: ({_x}, {_y}, {_z})";
    }
}
