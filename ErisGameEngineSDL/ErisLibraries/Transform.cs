using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErisMath;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ErisGameEngineSDL.ErisLibraries
{
    internal class Transform
    {
        public Vec3 position, scale;
        Quaternion _rotation;
        public Quaternion rotation { get { return _rotation; } }
        [AllowNull] GameObject gameObject;
        [AllowNull] public Transform parent;
        public List<Transform> children;
        public static Transform zero = 
            new Transform(Vec3.zero, Quaternion.identity, Vec3.one, null);

        Vec3 _forward, _right, _up;
        public Vec3 forward { get { return _forward; } }
        public Vec3 right { get { return _right; } }
        public Vec3 up { get { return _up; } }

        public Transform(Vec3 position, Quaternion rotation, Vec3 scale, Transform? parent) 
        {
            this.position = position;
            _rotation = rotation;
            this.scale = scale;
            children = new List<Transform>();
            SetParent(parent);
        }
        public Transform(Vec3 position)
        {
            this.position = position;
            _rotation = Quaternion.identity;
            scale = Vec3.one;
            children = new List<Transform>();
            parent = null;
        }
        public Transform(Vec3 position, Vec3 scale) 
        {
            this.position = position;
            _rotation = Quaternion.identity;
            this.scale = scale;
            children = new List<Transform>();
            parent = null;

        }
        public Transform(Vec3 position, Quaternion rotation)
        {
            this.position = position;
            _rotation = rotation;
            scale = Vec3.one;
            children = new List<Transform>();
            parent = null;
        }
        public Transform()
        {
            position = Vec3.zero;
            _rotation = Quaternion.identity;
            scale = Vec3.one;
            children = new List<Transform>();
            parent = null;
        }
        public void SetGameObjectReference(GameObject go) { gameObject = go; }
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
        public void Rotate(Quaternion q)
        {
            _rotation = q * _rotation;
            if (gameObject != null) gameObject.UpdateDeformedMesh();
            UpdateTransformSpace();
        }
        public void SetRotation(Quaternion q)
        {
            _rotation = q;
            if (gameObject != null) gameObject.UpdateDeformedMesh();
            UpdateTransformSpace();
        }
        void UpdateTransformSpace()
        {
            _forward = _rotation.LookDirection();
            _up = Quaternion.RotateVector(Vec3.up, _rotation);
            _right = Vec3.Cross(_up, _forward);
        }
    }
}
