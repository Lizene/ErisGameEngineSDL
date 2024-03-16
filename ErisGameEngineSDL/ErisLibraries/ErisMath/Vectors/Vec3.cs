using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal class Vec3
    {
        float _x, _y, _z;
        public float x { get { return _x; } set { _x = value; } }
        public float y { get { return _y; } set { _y = value; } }
        public float z { get { return _z; } set { _z = value; } }

        public static Vec3 zero = new Vec3(0, 0, 0);
        public static Vec3 one = new Vec3(1, 1, 1);
        public static Vec3 left = new Vec3(-1, 0, 0);
        public static Vec3 right = new Vec3(1, 0, 0);
        public static Vec3 down = new Vec3(0, -1, 0);
        public static Vec3 up = new Vec3(0, 1, 0);
        public static Vec3 forward = new Vec3(0, 0, -1);
        public static Vec3 back = new Vec3(0, 0, 1);
        public Vec3(float x, float y, float z)
        {
            _x = x; _y = y; _z = z;
        }
        public float magnitude() =>
            (float)Math.Sqrt(Math.Pow(_x, 2) + Math.Pow(_y, 2) + Math.Pow(_z, 2));
        public Vec3 normalized()
        {
            float m = magnitude();
            return new Vec3(_x / m, _y / m, _z / m);
        }
        public static float Dot(Vec3 a, Vec3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vec3 Cross(Vec3 a, Vec3 b) =>
            new Vec3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vec3 operator -(Vec3 a) => new Vec3(-a.x, -a.y, -a.z);
        public static Vec3 operator *(Vec3 a, int b) => new Vec3(a.x * b, a.y * b, a.z * b);
        public static Vec3 operator *(int b, Vec3 a) => new Vec3(a.x * b, a.y * b, a.z * b);
        public static Vec3 operator *(Vec3 a, float b) => new Vec3(a.x * b, a.y * b, a.z * b);
        public static Vec3 operator *(float b, Vec3 a) => new Vec3(a.x * b, a.y * b, a.z * b);
        public static Vec3 operator /(Vec3 a, int b) => new Vec3(a.x / b, a.y / b, a.z / b);
        public static Vec3 operator /(Vec3 a, float b) => new Vec3(a.x / b, a.y / b, a.z / b);
        public override string ToString() => $"Vector 3D: ({_x}, {_y}, {_z})";
    }
}
