using ErisLibraries;
using ErisMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ErisGameEngineSDL.ErisLibraries
{
    internal class Shaped3DObject
    {
        public Mesh mesh, transformedMesh;
        public Vec3[] scaledVertices;
        public Transform transform { get; }
        public float radius;
        public bool isRotating = false, isMorphing = false;

        public Shaped3DObject(Mesh mesh, Transform transform) 
        {
            this.transform = transform;
            this.transform.SetGameObjectReference(this);

            this.mesh = mesh;
            transformedMesh = new Mesh();
            transformedMesh.triangles = new IndexTriangle[mesh.triangles.Length];
            mesh.triangles.CopyTo(transformedMesh.triangles, 0);
            UpdateTransformedMeshScale();
            UpdateTransformedMeshRotation();
        }
        public static Shaped3DObject CreateCube(Vec3 position, Vec3 scale, ColorByte color)
            => CreateCube(position, Quaternion.identity, scale, color);
        public static Shaped3DObject CreateCube(Vec3 position, Quaternion rotation, Vec3 scale, ColorByte color)
            => new Shaped3DObject(
                    Mesh.Cube(color),
                    new Transform(position, rotation, scale));
        
        public Shaped3DObject Copy()
        {
            Shaped3DObject go = new Shaped3DObject(mesh, transform.Copy());
            go.transform.SetGameObjectReference(go);
            return go;
        }
        void SetRadius()
        {
            float r = 0;
            foreach (Vec3 vertex in transformedMesh.vertices)
            {
                float m = vertex.magnitude();
                if (m > r) r = m;
            }
            radius = r;
        }
        public void UpdateTransformedMeshRotation()
        {
            //Update the transformed mesh according to the new transform.rotation

            //Rotate each vertex of the already scaled vertices according to
            //transform.rotation and apply them to transformedMesh
            Vec3[] transformedVertices = Quaternion.RotateVectors(scaledVertices, transform.rotation);
            transformedMesh.vertices = transformedVertices;

            //Update the normals of the triangles of transformedMesh
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i++)
            {
                //Get triangle indices
                var tri = triangles[i];
                int[] indices = tri.indices;
                //Get triangle apices from rotated vertices
                Vec3[] apices = [transformedVertices[indices[0]], transformedVertices[indices[1]], transformedVertices[indices[2]]];
                //Calculate new normal vector for the rotated triangle
                Vec3 newNormal = ITriangle.TriangleNormal(apices);
                transformedMesh.triangles[i].normal = newNormal;
            }
        }
        public void UpdateTransformedMeshScale()
        {
            //Update scaled vertices, rotate the scaled vertices,
            //apply to transformedMesh and set the radius of the object again.
            Vec3 scale = transform.scale;
            scaledVertices = mesh.vertices.Select(v => v * scale).ToArray();
            transformedMesh.vertices = Quaternion.RotateVectors(scaledVertices, transform.rotation);
            SetRadius();
        }
    }
}
