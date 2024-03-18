using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ErisMath;
using System.Diagnostics.CodeAnalysis;

namespace ErisGameEngineSDL.ErisLibraries
{
    internal class Transform
    {
        public Vec3 position, scale;
        public Quaternion rotation;
        [AllowNull] public Transform parent;
        public List<Transform> children;
        public static Transform zero = 
            new Transform(Vec3.zero, Quaternion.Identity, Vec3.one, null);
        public Transform(Vec3 position, Quaternion rotation, Vec3 scale, Transform? parent) 
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            children = new List<Transform>();
            SetParent(parent);
        }
        public Transform(Vec3 position)
        {
            this.position = position;
            rotation = Quaternion.Identity;
            scale = Vec3.one;
            children = new List<Transform>();
            parent = null;
        }
        public Transform(Vec3 position, Vec3 scale) 
        {
            this.position = position;
            rotation = Quaternion.Identity;
            this.scale = scale;
            children = new List<Transform>();
            parent = null;

        }
        public Transform(Vec3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
            scale = Vec3.one;
            children = new List<Transform>();
            parent = null;
        }
        public Transform()
        {
            position = Vec3.zero;
            rotation = Quaternion.Identity;
            scale = Vec3.one;
            children = new List<Transform>();
            parent = null;
        }
        public void SetParent(Transform? newParent)
        {
            if (newParent == null)
            {
                if (parent != null)
                {
                    parent.children.Remove(this);
                }
                parent = null;
            }
            else
            {
                if (parent != null)
                {
                    parent.children.Remove(this);
                }
                parent = newParent;
                parent.children.Add(this);
            }
        }
        public Transform Copy()
        {
            return new Transform(position, rotation, scale, parent);
        }
    }
}
