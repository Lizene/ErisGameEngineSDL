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
        // A 4D complex number that has 1 scalar component and 3 imaginary components.
        // A normalized quaternion can be used to represent the rotation of a 3D object
        // in a way that allows for adding another rotation to a stored rotation

        float _w, _x, _y, _z;
        public float w { get => _w; } 
        public float x { get => _x; }
        public float y { get => _y; }
        public float z { get => _z; }

        //The identity rotation is the default rotation (an unrotated 3D object)
        public static readonly Quaternion identity = new Quaternion(1,0,0,0);

        //Quaternion from vector form components
        public Quaternion(float w, float x, float y, float z)
        {
            _w = w; _x = x; _y = y; _z = z;
        }
        //Quaternion from angle-axis form components
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
        //Quaternion from angles around the x, y and z axis
        public static Quaternion Euler(float aroundX, float aroundY, float aroundZ)
        {
            Quaternion xq = AngleAxis(aroundX, Vec3.right);
            Quaternion yq = AngleAxis(aroundY, Vec3.up);
            Quaternion zq = AngleAxis(aroundZ, Vec3.forward);
            return zq * yq * xq;
        }
        public static Quaternion Euler(float aroundX, float aroundY) //Overload without angle around Z-axis
        {
            Quaternion xq = AngleAxis(aroundX, Vec3.right);
            Quaternion yq = AngleAxis(aroundY, Vec3.up);
            return xq * yq;
        }
        public Quaternion inverted() //Get the opposite rotation
        {
            Vec3 v = new Vec3(_x, _y, _z);
            float ms = _w * _w + Vec3.Dot(v, v);
            float w = _w / ms;
            Vec3 v2 = v / (-ms);
            return new Quaternion(w, v2.x, v2.y, v2.z);
        }
        public void Rotate(Quaternion q) { this = q * this; } //Rotate quaternion by another quaternion by performing quaternion multiplication

        //The quaternion sandwich:
        //To rotate a vector by a quaternion, make a quaternion out of the vector,
        //Rotate it by the quaternion, and then rotate the inverse of the rotation by the previous product.
        public static Vec3 RotateVector(Vec3 v, Quaternion bread)
        {
            Quaternion cheese = new Quaternion(0, v.x, v.y, v.z);
            Quaternion sandwich = bread * cheese * bread.inverted();
            return new Vec3(sandwich.x,sandwich.y,sandwich.z);
        }

        //Optimised rotation calculation of many vectors by the same rotation 
        //(the inverse isn't calculated on every iteration)
        public static Vec3[] RotateVectors(Vec3[] vectors, Quaternion bread, Quaternion invertedBread)
        {
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
        public static Vec3[] RotateVectors(Vec3[] vectors, Quaternion bread) //Overload with no inverse pre-calculated
        {
            Quaternion invertedBread = bread.inverted();
            return RotateVectors(vectors, bread, invertedBread);
        }
        public Vec3 LookDirection() => RotateVector(Vec3.forward, this); //The 3D facing direction of a 3D object with this rotation
        public static Quaternion operator *(Quaternion r, Quaternion s) //Quaternion multiplication
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
