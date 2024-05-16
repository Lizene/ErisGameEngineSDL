using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal struct ApexTriangle : ITriangle
    {
        // Struct for storing a 3D space triangle for rendering 3D meshed objects
        // Inherits interface for methods that should work for both types of triangles
        // ApexTriangle stores apices of the triangle separate from a set of vertices (a clipped triangle)
        // as an array of 3D vectors of length 3
        public Vec3[] apices;
        // A pre-calculated normal for optimised rendering
        public Vec3 normal;
        // A color value to draw the triangle with, the engine only has single colour triangles as
        // a stylistic and time management choice.
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
        //Calculate the normal of the plane defined by this triangle
        //by taking a cross product from vectors going from one triangle apex to the next
        Vec3 CalculateNormal(Vec3[] apices) 
            => Vec3.Cross(apices[1] - apices[0], apices[2] - apices[1]).normalized();

        // Interface methods
        public Vec3[] GetApices(Vec3[] vertices) => apices;
        public Vec3 GetNormal() => normal;
        public ColorByte GetColor() => color;
    }
}
