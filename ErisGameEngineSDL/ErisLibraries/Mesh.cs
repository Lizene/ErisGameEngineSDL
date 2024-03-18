using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ErisMath;

namespace ErisLibraries
{
    internal struct Mesh
    {
        public Vec3[] vertices;
        public Triangle[] triangles;
        public Mesh(Vec3[] vertices, Triangle[] triangles) 
        {
            this.vertices = vertices;
            this.triangles = triangles;
        }
        public static Mesh Cube(Vec3 color)
        {
            Vec3[] verts =
            [
                new Vec3(1,1,1), new Vec3(1,1,-1), new Vec3(1,-1,-1), new Vec3(1,-1,1),
                new Vec3(-1,1,1), new Vec3(-1,1,-1), new Vec3(-1,-1,-1), new Vec3(-1,-1,1)
            ];
            int[] tris =
            [
                0, 1, 2, 0, 2, 3,
                4, 0, 3, 4, 3, 7,
                5, 4, 7, 5, 7, 6,
                1, 5, 6, 1, 6, 2,
                0, 4, 5, 0, 5, 1,
                3, 7, 6, 3, 6, 2
            ];
            Triangle[] triangles = TrianglesFromTrisInts(verts, tris, color);
            return new Mesh(verts, triangles);
        }
        static Triangle[] TrianglesFromTrisInts(Vec3[] verts, int[] tris, Vec3 color)
        {
            List<Triangle> triangleObjsList = new List<Triangle>();
            for (int i = 0; i < tris.Length - 2; i += 3)
            {
                int a = tris[i];
                int b = tris[i + 1];
                int c = tris[i + 2];
                Vec3 vecB = verts[b];
                Vec3 normal = Vec3.Cross(vecB - verts[a], verts[c] - vecB).normalized();
                triangleObjsList.Add(new Triangle([a, b, c], normal, color));
            }
            return triangleObjsList.ToArray();
        }
    }
}
