using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal struct Vec3int
    {
        // Struct for containg a 3D vector of integer values and doing vector operations.
        int _x, _y, _z;
        public int x { get { return _x; } set { _x = value; } }
        public int y { get { return _y; } set { _y = value; } }
        public int z { get { return _z; } set { _z = value; } }

        //Shorthands for common vectors.
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
            (float) Math.Sqrt(Math.Pow(_x, 2) + Math.Pow(_y, 2) + Math.Pow(_z, 2)); //Formula for magnitude of 3D vector
        public Vec3 normalized()
        {
            float m = magnitude();
            if (m < 0.001f) return Vec3.zero; //0-div handling, because integer vectors can have length 0
            return new Vec3(_x / m, _y / m, _z / m); //Divide components by magnitude and return a floating point vector
        }
        public static float Dot(Vec3int a, Vec3int b) => a._x * b._x + a._y * b._y + a._z * b._z; //3D dot product
        public static Vec3int Cross(Vec3int a, Vec3int b) =>
            new Vec3int(a._y * b._z - a._z * b._y, a._z * b._x - a._x * b._z, a._x * b._y - a._y * b._x); //Unmatricised formula for cross product

        public Vec3 ToFloat() => new Vec3(_x, _y, _z); //Convert to float vector

        public static Vec3int operator +(Vec3int a, Vec3int b) => new Vec3int(a._x + b._x, a._y + b._y, a._z + b._z); //Elementwise addition of two integer vectors
        public static Vec3 operator +(Vec3int a, Vec3 b) => new Vec3(a._x + b.x, a._y + b.y, a._z + b.z); //Elementwise addition of an integer vector and a float vector
        public static Vec3 operator +(Vec3 a, Vec3int b) => b + a; //Commutativity
        public static Vec3int operator -(Vec3int a, Vec3int b) => new Vec3int(a._x - b._x, a._y - b._y, a._z - b._z); //Elementwise subtraction of two integer vectors
        public static Vec3 operator -(Vec3int a, Vec3 b) => new Vec3(a._x - b.x, a._y - b.y, a._z - b.z); //Elementwise subtraction of a float vector from an integer vector
        public static Vec3 operator -(Vec3 a, Vec3int b) => new Vec3(a.x - b._x, a.y - b._y, a.z - b._z); //Elementwise subtraction of an integer vector from a float vector
        public static Vec3int operator -(Vec3int a) => new Vec3int(-a._x, -a._y, -a._z); //Negation

        public static Vec3int operator *(Vec3int a, int b) => new Vec3int(a._x * b, a._y * b, a._z * b); //Multiply vector length by integer
        public static Vec3int operator *(int b, Vec3int a) => a * b; //Commutativity
        public static Vec3 operator *(Vec3int a, float b) => new Vec3(a._x * b, a._y * b, a._z * b); //Multiply vector length by float
        public static Vec3 operator *(float b, Vec3int a) => a * b; //Commutativity

        public static Vec3int operator /(Vec3int a, int b) => new Vec3int(a._x / b, a._y / b, a._z / b); //Divide vector length by integer (integer division)
        public static Vec3 operator /(Vec3int a, float b) => new Vec3(a._x / b, a._y / b, a._z / b); //Divide vector length by float
        public override string ToString() => $"Vector 3D: ({_x}, {_y}, {_z})";
    }
}
