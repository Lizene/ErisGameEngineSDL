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
        //A class containing all the information that can vary between different cameras,
        //including the transform, FOV and frustums. Currently only one camera is functional.
        public Transform transform { get; } //The position and rotation of the camera
        public RectangleFrustum cameraSpaceFrustum { get; } // A frustum in camera space
        public RectangleFrustum worldSpaceFrustum { get; } // A frustum in world space that follows the camera world position and rotation
        public Vec2 viewPortSize { get; }
        public readonly float viewPlaneDistance, nearClipPlaneDistance, farClipPlaneDistance;
        public readonly float FOV; //Todo: make editable
        public Camera(Transform transform, float FOV, float nearClipPlaneDistance, float farClipPlaneDistance, Vec2 viewPortSize) 
        {
            this.FOV = FOV;
            this.transform = transform;
            this.viewPortSize = viewPortSize;
            //Calculate view plane distance from the field of view angle (viewplane = projectionplane)
            viewPlaneDistance = (float)(viewPortSize.x / (2 * Math.Tan(Constants.deg2rad * FOV / 2)));
            this.nearClipPlaneDistance = nearClipPlaneDistance; 
            this.farClipPlaneDistance = farClipPlaneDistance; 

            //Define camera frustums
            cameraSpaceFrustum = new RectangleFrustum(viewPortSize, viewPlaneDistance, nearClipPlaneDistance, farClipPlaneDistance);
            worldSpaceFrustum = new RectangleFrustum(viewPortSize, viewPlaneDistance, nearClipPlaneDistance, farClipPlaneDistance);
            TransformFrustum();
        }
        //Translate and rotate the world space frustum of the camera
        //according to the camera world position and rotation
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
        //Translate the camera and the world space frustum according to a movement vector
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
        
        public void SetRotation(Quaternion rotation) //Also rotates the world space frustum
        {
            transform.SetRotation(rotation);
            TransformFrustum();
        }
    }
}
