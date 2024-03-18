using ErisLibraries;
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
        Transform _transform;
        public Transform transform { get { return _transform;  } }
        public GameObject(Mesh mesh, Transform transform) 
        {
            this.mesh = mesh;
            _transform = transform;
        }
        public GameObject Copy()
        {
            return new GameObject(mesh, _transform.Copy());
        }
    }
}
