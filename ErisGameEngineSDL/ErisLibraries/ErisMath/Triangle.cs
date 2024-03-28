using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal interface ITriangle
    {
        public Vec3[] GetApices(Vec3[] vertices);
        public static Vec3 TriangleNormal(Vec3[] apices)
            => Vec3.Cross(apices[1] - apices[0], apices[2] - apices[1]).normalized();
    }
}
