using ErisMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2;
using System.Diagnostics.CodeAnalysis;

namespace ErisLibraries
{
    internal struct Triangle
    {
        public int[] indices;
        public Vec3 normal;
        public ColorByte color;
        public Triangle(int[] indices, Vec3 normal, ColorByte color) 
        {
            this.indices = indices;
            this.normal = normal;
            this.color = color;
        }
        public static Vec3 TriangleNormal(Vec3[] apices)
            => Vec3.Cross(apices[1] - apices[0], apices[2] - apices[1]).normalized();
        
    }
}
