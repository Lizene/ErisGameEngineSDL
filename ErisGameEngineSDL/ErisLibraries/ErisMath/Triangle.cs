using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal interface ITriangle
    {
        // Interface for the common methods needed for
        // both types of triangles: IndexTriangle and ApexTriangle.
        // Also has utility methods for triangles.
        public Vec3 GetNormal();
        public Vec3[] GetApices(Vec3[] vertices);
        public ColorByte GetColor();

        public static Vec3 TriangleNormal(Vec3[] apices) //Calculate a new normal from apices
            => Vec3.Cross(apices[1] - apices[0], apices[2] - apices[1]).normalized();
        
        public static Vec3 Centroid(Vec3[] apices) //Find centroid of triangle from apices
        {
            Vec3 a = apices[0];
            Vec3 b = apices[1];
            Vec3 c = apices[2];
            return new Vec3((a.x + b.x + c.x) / 3, (a.y + b.y + c.y) / 3, (a.z + b.z + c.z) / 3);
        }
    }
}
