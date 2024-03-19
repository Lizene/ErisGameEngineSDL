using ErisMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal struct Quaternion
    {
        float _w, _x, _y, _z;
        public float w { get => _w; } 
        public float x { get => _x; }
        public float y { get => _y; }
        public float z { get => _z; }
        public static readonly Quaternion identity = new Quaternion(1,0,0,0);
        public Quaternion(float w, float x, float y, float z)
        {
            _w = w; _x = x; _y = y; _z = z;
        }
        public static Quaternion AngleAxis(float angle, Vec3 axis)
        {
            angle = angle % 360f;
            if (angle < -180) angle += 360;
            else if (angle > 180) angle -= 360;
            float angleRad = Constants.deg2rad * angle;

            float w = (float)Math.Cos(angleRad/2);
            Vec3 v = axis.normalized() * (float)Math.Sin(angleRad / 2);

            return new Quaternion(w, v.x, v.y, v.z);
        }
        public static Quaternion Euler(float aroundX, float aroundY, float aroundZ)
        {
            Quaternion xq = AngleAxis(aroundX, Vec3.right);
            Quaternion yq = AngleAxis(aroundY, Vec3.up);
            Quaternion zq = AngleAxis(aroundZ, Vec3.forward);
            return zq * yq * xq;
        }
        public static Quaternion Euler(float aroundX, float aroundY)
        {
            Quaternion xq = AngleAxis(aroundX, Vec3.right);
            Quaternion yq = AngleAxis(aroundY, Vec3.up);
            return xq * yq;
        }
        public Quaternion inverted()
        {
            Vec3 v = new Vec3(_x, _y, _z);
            float ms = _w * _w + Vec3.Dot(v, v);
            float w = _w / ms;
            Vec3 v2 = v / (-ms);
            return new Quaternion(w, v2.x, v2.y, v2.z);
        }
        public void Rotate(Quaternion q) { this = q * this; }
        public float magnitude()
            => (float)Math.Sqrt(_w*_w+_x*_x+_y*_y+_z*_z);
        public Quaternion normalized()
        {
            float m = magnitude();
            return new Quaternion(_w/m, _x/m, _y/m, _z/m);
        }
        public static Vec3 RotateVector(Vec3 v, Quaternion bread)
        {
            Quaternion cheese = new Quaternion(0, v.x, v.y, v.z);
            Quaternion sandwich = bread * cheese * bread.inverted();
            return new Vec3(sandwich.x,sandwich.y,sandwich.z);
        }
        public static Vec3[] RotateVectors(Vec3[] vectors, Quaternion bread)
        {
            Quaternion invertedBread = bread.inverted();
            int lenVecs = vectors.Length;
            Vec3[] rotatedVectors = new Vec3[lenVecs];
            for (int i = 0; i < lenVecs; i++)
            {
                Vec3 v = vectors[i];
                Quaternion cheese = new Quaternion(0, v.x, v.y, v.z);
                Quaternion sandwich = bread * cheese * invertedBread;
                rotatedVectors[i] = new Vec3(sandwich.x, sandwich.y, sandwich.z);
            }
            return rotatedVectors;
        }
        public Vec3 LookDirection() => RotateVector(Vec3.forward, this);
        public static Quaternion operator *(Quaternion r, Quaternion s)
        {
            float w = r.w*s.w - r.x*s.x - r.y*s.y - r.z*s.z;
            float x = r.w*s.x + r.x*s.w + r.y*s.z - r.z*s.y;
            float y = r.w*s.y - r.x*s.z + r.y*s.w + r.z*s.x;
            float z = r.w*s.z + r.x*s.y - r.y*s.x + r.z*s.w;
            return new Quaternion(w, x, y, z);
        }
        public override string ToString() 
            => $"Quaternion: {_w}, {_x}, {_y}, {_z},";
    }
}
