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
        //Attached to an object,
        //forms the transform hierarchy (not functional yet),
        //stores the position, rotation and scale of a shaped 3D object or camera
        public Vec3 position;
        Vec3 _scale;
        public Vec3 scale { get => _scale; set => SetScale(value); }
        Quaternion _rotation;
        public Quaternion rotation { get { return _rotation; } }
        Shaped3DObject? objectAttachedTo;
        public Transform? parent; //The parent of this node in the hierarchy tree
        public List<Transform> children; //The children of this node in the hierarchy tree

        //Transform space axes
        Vec3 _forward, _right, _up;
        public Vec3 forward { get { return _forward; } }
        public Vec3 right { get { return _right; } }
        public Vec3 up { get { return _up; } }

        public Transform(Vec3 position, Quaternion rotation, Vec3 scale, Transform? parent) 
        {
            this.position = position;
            _scale = scale;
            SetRotation(rotation);
            children = new List<Transform>();
            SetParent(parent);
        }
        public Transform(Vec3 position, Quaternion rotation, Vec3 scale)
        {
            this.position = position;
            _scale = scale;
            SetRotation(rotation);
            children = new List<Transform>();
            SetParent(null);
        }
        public Transform(Vec3 position)
        {
            this.position = position;
            _scale = Vec3.one;
            SetRotation(Quaternion.identity);
            children = new List<Transform>();
            SetParent(null);
        }
        public Transform(Vec3 position, Vec3 scale) 
        {
            this.position = position;
            _scale = scale;
            SetRotation(Quaternion.identity);
            children = new List<Transform>();
            SetParent(null);

        }
        public Transform(Vec3 position, Quaternion rotation)
        {
            this.position = position;
            _scale = Vec3.one;
            SetRotation(rotation);
            children = new List<Transform>();
            SetParent(null);
        }
        public Transform()
        {
            position = Vec3.zero;
            _scale = Vec3.one;
            SetRotation(Quaternion.identity);
            children = new List<Transform>();
            SetParent(null);
        }
        public void SetGameObjectReference(Shaped3DObject so) { objectAttachedTo = so; }
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
        public void Rotate(Quaternion q) => SetRotation(q * _rotation);
        public void SetRotation(Quaternion q)
        {
            _rotation = q;
            UpdateTransformSpace();
            if (objectAttachedTo != null) objectAttachedTo.UpdateTransformedMeshRotation();
        }
        public void SetScale(Vec3 s)
        {
            _scale = s;
            if (objectAttachedTo != null) objectAttachedTo.UpdateTransformedMeshScale();
        }
        void UpdateTransformSpace()
        {
            _forward = _rotation.LookDirection().normalized();
            _up = Quaternion.RotateVector(Vec3.up, _rotation).normalized();
            _right = Vec3.Cross(_up, _forward).normalized();
        }
    }
}
