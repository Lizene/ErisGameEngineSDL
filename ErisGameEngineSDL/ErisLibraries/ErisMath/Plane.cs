using ErisMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal struct Plane
    {
        // Struct for storing information that defines a plane
        public Vec3 point; //A point on the plane
        public Vec3 normal; //The normal of the plane
        public float d; //The fourth value of the scalar form equation of the plane, used in calculations
        public Plane(Vec3 point, Vec3 normal)
        {
            this.point = point;
            this.normal = normal;
            d = Vec3.Dot(normal,-point);
        }
        public bool IsPointOnPositiveSide(Vec3 p) => Vec3.Dot(normal, p) + d >= 0;
        public bool IsPointWithRadiusOnNegativeSide(Vec3 p, float r) => Vec3.Dot(normal, p) + d - r < 0; //Used for frustum culling (Is object completely outside?)
        public bool IsPointWithRadiusOnPositiveSide(Vec3 p, float r) => Vec3.Dot(normal, p) + d + r >= 0; //Used for frustum culling (Is object completely inside?)

        public bool SegmentIntersects(Vec3 A, Vec3 B) //Does the segment from A to B intersect with the plane?
            => IsPointOnPositiveSide(A) != IsPointOnPositiveSide(B);
        public Vec3 LineIntersectionPoint(Vec3 A, Vec3 B) //Assumes there is one
        {
            //Formula for calculating the intersection point of a line and a plane.
            Vec3 AB = B - A;
            float f = Vec3.Dot(point-A,normal)/Vec3.Dot(AB,normal);
            return A + AB*f; 
        }
    }
}
