using ErisLibraries;
using ErisMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ErisGameEngineSDL.ErisLibraries
{
    internal class GameObject
    {
        public Mesh mesh;
        public Mesh deformedMesh;
        public Transform transform { get; }

        public GameObject(Mesh mesh, Transform transform) 
        {
            this.mesh = mesh;
            deformedMesh = new Mesh();
            deformedMesh.vertices = new Vec3[mesh.vertices.Length];
            deformedMesh.triangles = new Triangle[mesh.triangles.Length];
            mesh.triangles.CopyTo(deformedMesh.triangles,0);
            this.transform = transform;
            this.transform.SetGameObjectReference(this);
            UpdateDeformedMesh();
        }
        public GameObject Copy()
        {
            GameObject go = new GameObject(mesh, transform.Copy());
            go.transform.SetGameObjectReference(go);
            return go;
        }
        public void UpdateDeformedMesh()
        {
            Vec3[] rotatedVecs = Quaternion.RotateVectors(mesh.vertices, transform.rotation);
            deformedMesh.vertices = rotatedVecs;
            var triangles = mesh.triangles;
            for (int i = 0; i <  triangles.Length; i++)
            {
                var tri = triangles[i];
                int[] indices = tri.indices;
                Vec3[] apices = [rotatedVecs[indices[0]], rotatedVecs[indices[1]], rotatedVecs[indices[2]]]; 
                Vec3 newNormal = Triangle.TriangleNormal(apices);
                deformedMesh.triangles[i].normal = newNormal;
            }
        }
    }
}
