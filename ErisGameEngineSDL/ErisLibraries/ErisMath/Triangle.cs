using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal interface ITriangle
    {
        public Vec3 GetNormal();
        public Vec3[] GetApices(Vec3[] vertices);
        public ColorByte GetColor();
        public static Vec3 TriangleNormal(Vec3[] apices)
            => Vec3.Cross(apices[1] - apices[0], apices[2] - apices[1]).normalized();
        public static Vec3 Centroid(Vec3[] vertices)
        {
            Vec3 a = vertices[0];
            Vec3 b = vertices[1];
            Vec3 c = vertices[2];
            return new Vec3((a.x + b.x + c.x) / 3, (a.y + b.y + c.y) / 3, (a.z + b.z + c.z) / 3);
        }
    }
}
