using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErisMath;

namespace ErisGameEngineSDL.ErisLibraries
{
    internal class Camera
    {
        public Transform transform { get; }
        public RectangleFrustum cameraSpaceFrustum { get; } // original
        public RectangleFrustum worldSpaceFrustum { get; } // transformed
        public Vec2 viewPortSize { get; }
        public readonly float viewPlaneDistance, nearClipPlaneDistance, farClipPlaneDistance;
        public Camera(Transform transform, float FOV, float nearClipPlaneDistance, float farClipPlaneDistance, Vec2 viewPortSize) 
        {
            this.transform = transform;
            this.viewPortSize = viewPortSize;
            viewPlaneDistance = (float)(viewPortSize.x / (2 * Math.Tan(Constants.deg2rad * FOV / 2)));
            this.nearClipPlaneDistance = nearClipPlaneDistance; 
            this.farClipPlaneDistance = farClipPlaneDistance; 
            cameraSpaceFrustum = new RectangleFrustum(viewPortSize, viewPlaneDistance, nearClipPlaneDistance, farClipPlaneDistance);
            worldSpaceFrustum = new RectangleFrustum(viewPortSize, viewPlaneDistance, nearClipPlaneDistance, farClipPlaneDistance);
            TransformFrustum();
        }
        void TransformFrustum()
        {
            Vec3[] toBeRotated = new Vec3[12];
            int i = 0;
            foreach (Plane plane in cameraSpaceFrustum.planes)
            {
                toBeRotated[i++] = plane.point;
                toBeRotated[i++] = plane.normal;
            }
            Vec3[] rotatedVecs = Quaternion.RotateVectors(toBeRotated, transform.rotation);
            int j = 0;
            for (i = 0; i < 12; i += 2)
            {
                worldSpaceFrustum.planes[j++] = new Plane(transform.position + rotatedVecs[i], rotatedVecs[i + 1]);
            }
        }
        public void Move(Vec3 movement)
        {
            transform.position += movement;
            for (int i = 0;i < 6; i++)
            {
                Vec3 newPoint = worldSpaceFrustum.planes[i].point + movement;
                worldSpaceFrustum.planes[i].point = newPoint;
                worldSpaceFrustum.planes[i].d = Vec3.Dot(worldSpaceFrustum.planes[i].normal, -newPoint);
            }
        }
        public void SetRotation(Quaternion rotation)
        {
            transform.SetRotation(rotation);
            TransformFrustum();
        }
    }
}
