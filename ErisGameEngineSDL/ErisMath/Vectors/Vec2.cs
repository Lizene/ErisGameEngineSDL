using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisGameEngineSDL.ErisMath.Vectors
{
    internal class Vec2
    {
        float _x, _y;
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
        public float x
        {
            get { return _x; }
            set { _x = value; }
        }
        public float y
        {
            get { return _y; }
            set { _y = value; }
        }
        public float magnitude()
        {
            return (float)Math.Sqrt(Math.Pow(_x, 2) + Math.Pow(_y, 2));
        }
        public Vec2 normalized()
        {
            float m = magnitude();
            return new Vec2(_x / m, _y / m);
        }
        public static float Dot(Vec2 a, Vec2 b) => a.x * b.x + a.y * b.y;

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.y - b.y);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.x, -a.y);

        public static Vec2 operator *(Vec2 a, int b) => new Vec2(a.x * b, a.y * b);
        public static Vec2 operator *(int b, Vec2 a) => new Vec2(a.x * b, a.y * b);
        public static Vec2 operator *(Vec2 a, float b) => new Vec2(a.x * b, a.y * b);
        public static Vec2 operator *(float b, Vec2 a) => new Vec2(a.x * b, a.y * b);

        public static Vec2 operator /(Vec2 a, int b) => new Vec2(a.x / b, a.y / b);
        public static Vec2 operator /(Vec2 a, float b) => new Vec2(a.x / b, a.y / b);
    }
}
