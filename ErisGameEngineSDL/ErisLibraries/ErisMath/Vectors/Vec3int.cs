using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal class Vec3int
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
        public static Vec3int forward = new Vec3int(0, 0, -1);
        public static Vec3int back =      new Vec3int(0, 0, 1);
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
        public static float Dot(Vec3int a, Vec3int b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vec3int Cross(Vec3int a, Vec3int b) =>
            new Vec3int(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);

        public static Vec3int operator +(Vec3int a, Vec3int b) => new Vec3int(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vec3int operator -(Vec3int a, Vec3int b) => new Vec3int(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vec3int operator -(Vec3int a) => new Vec3int(-a.x, -a.y, -a.z);
        public static Vec3int operator *(Vec3int a, int b) => new Vec3int(a.x * b, a.y * b, a.z * b);
        public static Vec3int operator *(int b, Vec3int a) => new Vec3int(a.x * b, a.y * b, a.z * b);
        public static Vec3int operator /(Vec3int a, int b) => new Vec3int(a.x / b, a.y / b, a.z / b);
        public override string ToString() => $"Vector 3D: ({_x}, {_y}, {_z})";
    }
}
