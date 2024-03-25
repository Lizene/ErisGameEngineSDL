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
        public Vec3 point;
        public Vec3 normal;
        public float d;
        public Plane(Vec3 point, Vec3 normal)
        {
            this.point = point;
            this.normal = normal;
            d = Vec3.Dot(normal,-point);
        }
        public bool IsPointOnPositiveSide(Vec3 p) => Vec3.Dot(normal, p) + d >= 0;
        public bool IsPointWithRadiusOnNegativeSide(Vec3 p, float r) => Vec3.Dot(normal, p) + d - r < 0;

        public bool SegmentIntersects(Vec3 A, Vec3 B)
            => IsPointOnPositiveSide(A) != IsPointOnPositiveSide(B);
        public Vec3 LineIntersectionPoint(Vec3 A, Vec3 B)
        {
            Vec3 AB = B - A;
            float f = Vec3.Dot(point-A,normal)/Vec3.Dot(AB,normal);

            return A + AB*f;
        }
    }
}
