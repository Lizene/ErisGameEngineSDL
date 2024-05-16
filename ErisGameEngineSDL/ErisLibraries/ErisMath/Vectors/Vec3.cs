using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErisMath;

namespace ErisMath
{
    internal struct Vec3
    {
        // Struct for containg a 3D vector of floating point values and doing vector operations.
        float _x, _y, _z;
        public float x { get { return _x; } set { _x = value; } }
        public float y { get { return _y; } set { _y = value; } }
        public float z { get { return _z; } set { _z = value; } }

        //Shorthands for common vectors.
        public static Vec3 zero =       new Vec3(0, 0, 0);
        public static Vec3 one =         new Vec3(1, 1, 1);
        public static Vec3 left =        new Vec3(-1, 0, 0);
        public static Vec3 right =      new Vec3(1, 0, 0);
        public static Vec3 down =      new Vec3(0, -1, 0);
        public static Vec3 up =          new Vec3(0, 1, 0);
        public static Vec3 forward = new Vec3(0, 0, 1);
        public static Vec3 back =      new Vec3(0, 0, -1);
        public Vec3(float x, float y, float z)
        {
            _x = x; _y = y; _z = z;
        }
        public float magnitude() =>
            (float)Math.Sqrt(Math.Pow(_x, 2) + Math.Pow(_y, 2) + Math.Pow(_z, 2)); //Formula for magnitude of 3D vector
        public Vec3 normalized()
        {
            float m = magnitude();
            return new Vec3(_x / m, _y / m, _z / m); //Divide elements by magnitude
        }
        public static float Dot(Vec3 a, Vec3 b) => a._x * b._x + a._y * b._y + a._z * b._z; //3D dot product
        public static Vec3 Cross(Vec3 a, Vec3 b) =>
            new Vec3(a._y * b._z - a._z * b._y, a._z * b._x - a._x * b._z, a._x * b._y - a._y * b._x); //Unmatricised formula for cross product

        public static float Angle(Vec3 a, Vec3 b)
            => (float)Math.Acos(Dot(a, b) / (a.magnitude() * b.magnitude())); //Formula for calculating unsigned angle between two 3D vectors
        public void RotateAroundAxis(float angle, Vec3 axis)
        {
            this = Quaternion.RotateVector(this, Quaternion.AngleAxis(angle, axis)); //Rotate vector by quaternion formed from angle and axis.
        }
        public Vec3int ToInt() => new Vec3int((int)_x, (int)_y, (int)_z); //Truncate to 3D integer vector

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a._x + b._x, a._y + b._y, a._z + b._z); //Elementwise addition of two vectors
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a._x - b._x, a._y - b._y, a._z - b._z); //Elementwise subtraction of two vectors
        public static Vec3 operator -(Vec3 a) => new Vec3(-a._x, -a._y, -a._z); //Negation

        public static Vec3 operator *(Vec3 a, int b) => new Vec3(a._x * b, a._y * b, a._z * b); //Multiply vector length by integer
        public static Vec3 operator *(int b, Vec3 a) => a * b; //Commutativity
        public static Vec3 operator *(Vec3 a, float b) => new Vec3(a._x * b, a._y * b, a._z * b); //Multiply vector length by float
        public static Vec3 operator *(float b, Vec3 a) => a * b; //Commutativity
        public static Vec3 operator *(Vec3 a, Vec3 b) => new Vec3(a._x*b._x, a._y*b._y, a._z*b._z); //Elementwise multiplication of two vectors

        public static Vec3 operator /(Vec3 a, int b) => new Vec3(a._x / b, a._y / b, a._z / b); //Divide vector length by integer
        public static Vec3 operator /(Vec3 a, float b) => new Vec3(a._x / b, a._y / b, a._z / b); //Divide vector length by float
        public override string ToString() => $"Vector 3D: ({_x}, {_y}, {_z})";
    }
}
