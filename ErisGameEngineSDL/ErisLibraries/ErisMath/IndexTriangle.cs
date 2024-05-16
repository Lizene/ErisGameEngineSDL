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
        // Struct for storing a 3D space triangle for rendering 3D meshed objects
        // Inherits interface for methods that should work for both types of triangles
        // IndexTriangle stores 3 indices pointing to apices on an array of mesh vertices.
        public int[] indices;
        // A pre-calculated normal for optimised rendering
        public Vec3 normal;
        // A color value to draw the triangle with, the engine only has single colour triangles as
        // a stylistic and time management choice.
        public ColorByte color;
        public IndexTriangle(int[] indices, Vec3[] vertices, ColorByte color)
        {
            this.indices = indices;
            normal = CalculateNormal(vertices);
            this.color = color;
        }
        //Calculate the normal of the plane defined by this triangle
        //by taking a cross product from vectors going from one triangle apex to the next
        Vec3 CalculateNormal(Vec3[] vertices)
        {
            Vec3[] apices = GetApices(vertices);
            return Vec3.Cross(apices[1] - apices[0], apices[2] - apices[1]).normalized();
        }
        // Interface methods
        public Vec3[] GetApices(Vec3[] vertices) //Get apex vectors from an array of vertices using IndexTriangle indices
            => [vertices[indices[0]], vertices[indices[1]], vertices[indices[2]]];
        public Vec3 GetNormal() => normal;
        public ColorByte GetColor() => color;
    }
}
