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
        //Struct for storing vertices of a 3D object and
        //triangles linking those vertices to form surfaces
        public Vec3[] vertices;
        public IndexTriangle[] triangles;
        public Mesh(Vec3[] vertices, IndexTriangle[] triangles) 
        {
            this.vertices = vertices;
            this.triangles = triangles;
        }
        public static Mesh SingleTriangle(ColorByte color)
        {
            Vec3[] verts = [new Vec3(0.6f, -0.6f, 0), new Vec3(-0.6f, -0.6f, 0), new Vec3(0, 1f, 0)];
            IndexTriangle[] triangles = [new IndexTriangle([0,2,1],verts,color)];
            return new Mesh(verts, triangles);
        }
        public static Mesh Cube(ColorByte color)
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
                3, 6, 7, 3, 2, 6
            ];
            IndexTriangle[] triangles = TrianglesFromTrisInts(verts, tris, color);
            return new Mesh(verts, triangles);
        }
        //Create triangle objects from the traditional style of
        //an array of triangle indexes pointing to an array of vertices
        static IndexTriangle[] TrianglesFromTrisInts(Vec3[] verts, int[] tris, ColorByte color)
        {
            List<IndexTriangle> triangleObjsList = new List<IndexTriangle>();
            for (int i = 0; i < tris.Length - 2; i += 3)
            {
                int a = tris[i];
                int b = tris[i + 1];
                int c = tris[i + 2];
                Vec3 vecB = verts[b];
                triangleObjsList.Add(new IndexTriangle([a, b, c], verts, color));
            }
            return triangleObjsList.ToArray();
        }
    }
}
