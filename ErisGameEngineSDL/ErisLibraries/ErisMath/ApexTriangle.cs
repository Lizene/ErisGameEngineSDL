using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal struct ApexTriangle : ITriangle
    {
        public Vec3[] apices;
        public Vec3 normal;
        public ColorByte color;
        public ApexTriangle(Vec3[] apices, Vec3 normal, ColorByte color)
        {
            this.apices = apices;
            this.normal = normal;
            this.color = color;
        }
        public ApexTriangle(Vec3[] apices, ColorByte color)
        {
            this.apices = apices;
            normal = CalculateNormal(apices);
            this.color = color;
        }
        Vec3 CalculateNormal(Vec3[] apices)
            => Vec3.Cross(apices[1] - apices[0], apices[2] - apices[1]).normalized();
        public Vec3[] GetApices(Vec3[] vertices) => apices;
    }
}
