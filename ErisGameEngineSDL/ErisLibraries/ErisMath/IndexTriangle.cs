using ErisMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2;
using System.Diagnostics.CodeAnalysis;

namespace ErisMath
{
    internal struct IndexTriangle : ITriangle
    {
        public int[] indices;
        public Vec3 normal;
        public ColorByte color;
        public IndexTriangle(int[] indices, Vec3[] vertices, ColorByte color)
        {
            this.indices = indices;
            normal = CalculateNormal(vertices);
            this.color = color;
        }
        Vec3 CalculateNormal(Vec3[] vertices)
        {
            Vec3[] apices = GetApices(vertices);
            return Vec3.Cross(apices[1] - apices[0], apices[2] - apices[1]).normalized();
        }
        public Vec3[] GetApices(Vec3[] vertices)
            => [vertices[indices[0]], vertices[indices[1]], vertices[indices[2]]];
        public Vec3 GetNormal() => normal;
        public ColorByte GetColor() => color;
    }
}
