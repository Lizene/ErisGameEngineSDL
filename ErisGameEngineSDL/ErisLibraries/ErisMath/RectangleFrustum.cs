using ErisGameEngineSDL.ErisLibraries;
using ErisLibraries;
using ErisMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace ErisMath
{
    internal class RectangleFrustum
    {
        public Plane[] planes;
        public Plane near { get { return planes[0]; } }
        public Plane far { get { return planes[1]; } }
        public Plane up { get { return planes[2]; } }
        public Plane down { get { return planes[3]; } }
        public Plane left { get { return planes[4]; } }
        public Plane right { get { return planes[5]; } }
        public RectangleFrustum(Vec2 viewPortSize, float viewPortDistance, float nearClipPlaneDistance, float farClipPlaneDistance)
        {
            Vec3 viewPortCenter = Vec3.forward * viewPortDistance;
            Vec2 halfViewPortSize = viewPortSize / 2;

            Plane near = new Plane(Vec3.forward * nearClipPlaneDistance, Vec3.back);
            Plane far = new Plane(Vec3.forward * farClipPlaneDistance, Vec3.forward);
            Plane up = new Plane(Vec3.zero, Vec3.Cross(viewPortCenter + Vec3.up * halfViewPortSize.y, Vec3.right).normalized());
            Plane down = new Plane(Vec3.zero, Vec3.Cross(viewPortCenter + Vec3.down * halfViewPortSize.y, Vec3.left).normalized());
            Plane left = new Plane(Vec3.zero, Vec3.Cross(viewPortCenter + Vec3.left * halfViewPortSize.x, Vec3.up).normalized());
            Plane right = new Plane(Vec3.zero, Vec3.Cross(viewPortCenter + Vec3.right * halfViewPortSize.x, Vec3.down).normalized());

            planes = [near, far, up, down, left, right];
        }
        public bool IsPointInside(Vec3 point)
        {
            bool isInside = true;
            foreach (Plane plane in planes)
            {
                if (plane.IsPointOnPositiveSide(point)) isInside = false;
            }
            return isInside;
        }
        public bool IsGameObjectPartlyInside(GameObject go)
        {
            bool isInside = true;
            foreach (Plane plane in planes)
            {
                if (!plane.IsPointWithRadiusOnNegativeSide(go.transform.position, go.radius))
                    isInside = false;
            }
            return isInside;
        }
        public bool IsGameObjectCompletelyInside(GameObject go)
        {
            bool isInside = true;
            foreach (Plane plane in planes)
            {
                if (plane.IsPointWithRadiusOnPositiveSide(go.transform.position, go.radius))
                    isInside = false;
            }
            return isInside;
        }
        public Vec3[] SegmentIntersectionPoints(Vec3 A, Vec3 B)
        {
            List<Vec3> intersectionPoints = new List<Vec3>();
            for (int i = 0; i < 6; i++)
            {
                if (!planes[i].SegmentIntersects(A, B)) continue;
                intersectionPoints.Add(planes[i].LineIntersectionPoint(A, B));
            }
            return intersectionPoints.ToArray();
        }
        bool IsIntersectionPointInsideFrustum(Vec3 p, int planeIdx)
        {
            bool isInside = true;
            for (int i = 0; i < 6; i++)
            {
                if (i == planeIdx) continue;
                if (planes[i].IsPointOnPositiveSide(p)) isInside = false;
            }
            return isInside;
        }
        public Vec3 FrustumIntersectionPoint(Vec3 A, Vec3 B) //Assumes there is exactly one
        {
            for (int i = 0; i < 6; i++)
            {
                if (!planes[i].SegmentIntersects(A, B)) continue;
                Vec3 intersectionPoint = planes[i].LineIntersectionPoint(A, B);
                if (IsIntersectionPointInsideFrustum(intersectionPoint, i))
                    return intersectionPoint;
            }
            return Vec3.zero;
        }
        public Tuple<Vec3,Vec3>? ClipSegment(Vec3 A, Vec3 B)
        {
            List<Vec3> frustumIntersectionPoints = new List<Vec3>();
            int intersectingPlaneIndex = -1;
            for (int i = 0; i < 6; i++)
            {
                if (!planes[i].SegmentIntersects(A, B)) continue;
                Vec3 planeIntersectionPoint = planes[i].LineIntersectionPoint(A, B);
                if (!IsIntersectionPointInsideFrustum(planeIntersectionPoint, i)) continue;
                frustumIntersectionPoints.Add(planeIntersectionPoint);
                if (frustumIntersectionPoints.Count == 1) intersectingPlaneIndex = i;
                else break;
            }
            int count = frustumIntersectionPoints.Count;
            if (count == 0) return null;
            else if (count == 1)
            {
                if (planes[intersectingPlaneIndex].IsPointOnPositiveSide(A))
                    return new Tuple<Vec3, Vec3>(frustumIntersectionPoints[0], B);
                else return new Tuple<Vec3, Vec3>(A, frustumIntersectionPoints[0]);
            }
            else return new Tuple<Vec3, Vec3>(frustumIntersectionPoints[0], frustumIntersectionPoints[1]);
        }
        public Triangle[] ClipTriangles(Vec3[] vertices, Triangle[] triangles)
        {
            List<Triangle> clippedTriangles = [];
            bool[] isVerticesInside = new bool[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                isVerticesInside[i] = IsPointInside(vertices[i]);
            }
            for (int i = 0; i < triangles.Length; i++)
            {
                Triangle triangle = triangles[i];
                List<int> indicesInside = [];
                List<int> indicesOutside = [];
                foreach (int index in triangle.indices)
                {
                    if (isVerticesInside[index]) indicesInside.Append(index);
                    else indicesOutside.Append(index);
                }
                int countVerticesInside = indicesInside.Count;
                if (countVerticesInside == 0) continue;
                if (countVerticesInside == 3)
                {
                    clippedTriangles.Add(triangle);
                    continue;
                }
                if (countVerticesInside == 1)
                {
                    int insideIdx = indicesInside[0];
                    Vec3 intersectionPoint1 = FrustumIntersectionPoint(vertices[insideIdx], vertices[indicesOutside[0]]);
                    Vec3 intersectionPoint2 = FrustumIntersectionPoint(vertices[insideIdx], vertices[indicesOutside[1]]);
                    Triangle clippedTriangle = new Triangle(
                        [insideIdx, ],
                        triangle.normal,
                        triangle.color);
                }
            }
            return null;
        }
    }
}
