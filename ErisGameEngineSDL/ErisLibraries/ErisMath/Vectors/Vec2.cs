using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal struct Vec2
    {
        // Struct for containing a 2D vector of floating point values and doing vector operations.
        float _x, _y;
        public float x { get { return _x; } set { _x = value; } }
        public float y { get { return _y; } set { _y = value; } }

        // Shorthands for common vectors
        public static Vec2 zero = new Vec2(0,0);
        public static Vec2 one = new Vec2(1,1);
        public static Vec2 left = new Vec2(-1,0);
        public static Vec2 right = new Vec2(1,0);
        public static Vec2 down = new Vec2(0,-1);
        public static Vec2 up = new Vec2(0,1);
        public Vec2(float x, float y)
        {
            _x = x; _y = y;
        }
        public float magnitude() =>
            (float)Math.Sqrt(Math.Pow(_x, 2) + Math.Pow(_y, 2)); //Formula for magnitude of vector
        
        public Vec2 normalized()
        {
            float m = magnitude();
            return new Vec2(_x / m, _y / m); //Divide components by vector magnitude
        }
        public Vec2int ToInt() => new Vec2int((int)_x, (int)_y); //Truncate to 2D integer vector
        public static float Dot(Vec2 a, Vec2 b) => a._x * b._x + a._y * b._y; //2D dot product 

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a._x + b._x, a._y + b._y); //Elementwise addition
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a._x - b._x, a._y - b._y); //Elementwise subtraction
        public static Vec2 operator -(Vec2 a) => new Vec2(-a._x, -a._y); //Negation

        public static Vec2 operator *(Vec2 a, int b) => new Vec2(a._x * b, a._y * b); //Multiply vector length by integer
        public static Vec2 operator *(int b, Vec2 a) => a*b; //Commutativity
        public static Vec2 operator *(Vec2 a, float b) => new Vec2(a._x * b, a._y * b); //Multiply vector length by float
        public static Vec2 operator *(float b, Vec2 a) => a*b; //Commutativity
        public static Vec2 operator *(Vec2 a, Vec2 b) => new Vec2(a._x*b._x, a._y*b._y); //Elementwise multiplication

        public static Vec2 operator /(Vec2 a, int b) => new Vec2(a._x / b, a._y / b); //Divide vector length by integer
        public static Vec2 operator /(Vec2 a, float b) => new Vec2(a._x / b, a._y / b); //Divide vector length by float
        public override string ToString() => $"Vector 2D: ({_x}, {_y})";
    }
}
