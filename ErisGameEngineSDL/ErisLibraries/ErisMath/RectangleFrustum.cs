using ErisGameEngineSDL.ErisLibraries;
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
        public Plane[] planes; //Array containing the frustum bounding planes
        Vec3[][] diagonalLines; //Lines going from the corners of the near plane square to the corners of the far plane square

        //Properties to fetch a specific frustum plane
        public Plane near { get { return planes[0]; } }
        public Plane far { get { return planes[1]; } }
        public Plane up { get { return planes[2]; } }
        public Plane down { get { return planes[3]; } }
        public Plane left { get { return planes[4]; } }
        public Plane right { get { return planes[5]; } }
        public RectangleFrustum(Vec2 viewPortSize, float viewPortDistance, float nearClipPlaneDistance, float farClipPlaneDistance)
        {
            //I may be using the word viewport wrong, but what I mean by it here
            //is the square slice of the frustum made by the projection plane
            Vec3 viewPortCenter = Vec3.forward * viewPortDistance;
            Vec2 halfViewPortSize = viewPortSize / 2;

            //Define frustum planes from parameters
            Plane near = new Plane(Vec3.forward * nearClipPlaneDistance, Vec3.back);
            Plane far = new Plane(Vec3.forward * farClipPlaneDistance, Vec3.forward);
            Vec3 pointUp = viewPortCenter + Vec3.up * halfViewPortSize.y;
            Plane up = new Plane(pointUp, Vec3.Cross(pointUp, Vec3.right).normalized());
            Vec3 pointDown = viewPortCenter + Vec3.down * halfViewPortSize.y;
            Plane down = new Plane(pointDown, Vec3.Cross(pointDown, Vec3.left).normalized());
            Vec3 pointLeft = viewPortCenter + Vec3.left * halfViewPortSize.x;
            Plane left = new Plane(pointLeft, Vec3.Cross(pointLeft, Vec3.up).normalized());
            Vec3 pointRight = viewPortCenter + Vec3.right * halfViewPortSize.x;
            Plane right = new Plane(pointRight, Vec3.Cross(pointRight, Vec3.down).normalized());
            planes = [near, far, up, down, left, right];

            //Define far plane square corner points
            float farToViewRatio = farClipPlaneDistance / viewPortDistance;
            Vec2 halfFarSize = new Vec2(farToViewRatio* halfViewPortSize.x, farToViewRatio*halfViewPortSize.y);

            Vec3[] farSquarePoints = [
                new Vec3(-halfFarSize.x,halfFarSize.y,farClipPlaneDistance),
                new Vec3(halfFarSize.x,halfFarSize.y,farClipPlaneDistance),
                new Vec3(halfFarSize.x,-halfFarSize.y,farClipPlaneDistance),
                new Vec3(-halfFarSize.x,-halfFarSize.y,farClipPlaneDistance),
            ];

            //Define near plane square corner points
            float nearToViewRatio = nearClipPlaneDistance / viewPortDistance;
            Vec2 halfNearSize = new Vec2(nearToViewRatio * halfViewPortSize.x, nearToViewRatio * halfViewPortSize.y);

            Vec3[] nearSquarePoints = [
                new Vec3(-halfNearSize.x,halfNearSize.y,nearClipPlaneDistance),
                new Vec3(halfNearSize.x,halfNearSize.y,nearClipPlaneDistance),
                new Vec3(halfNearSize.x,-halfNearSize.y,nearClipPlaneDistance),
                new Vec3(-halfNearSize.x,-halfNearSize.y,nearClipPlaneDistance),
            ];

            //Define diagonal lines
            diagonalLines = [
                [nearSquarePoints[0], farSquarePoints[0]],
                [nearSquarePoints[1], farSquarePoints[1]],
                [nearSquarePoints[2], farSquarePoints[2]],
                [nearSquarePoints[3], farSquarePoints[3]],
            ];
        }
        public bool IsPointInside(Vec3 point) //Is a point in 3D space inside the frustum?
        {
            bool isInside = true;
            foreach (Plane plane in planes)
            {
                if (plane.IsPointOnPositiveSide(point)) isInside = false;
            }
            return isInside;
        }

        //Determine if an object is only partially inside this frustum, using its radius.
        public bool IsObjectPartlyInside(Shaped3DObject go)
        {
            bool isInside = true;
            foreach (Plane plane in planes)
            {
                if (!plane.IsPointWithRadiusOnNegativeSide(go.transform.position, go.radius))
                    isInside = false;
            }
            return isInside;
        }

        //Determine if an object is completely inside this frustum, using its radius.
        public bool IsObjectCompletelyInside(Shaped3DObject go) 
        {
            bool isInside = true;
            foreach (Plane plane in planes)
            {
                if (plane.IsPointWithRadiusOnPositiveSide(go.transform.position, go.radius))
                    isInside = false;
            }
            return isInside;
        }

        //Get all intersection points a segment makes with the planes of the frustum
        public Vec3[] PlaneIntersectionPoints(Vec3 A, Vec3 B)
        {
            List<Vec3> intersectionPoints = new List<Vec3>();
            for (int i = 0; i < 6; i++)
            {
                if (!planes[i].SegmentIntersects(A, B)) continue;
                intersectionPoints.Add(planes[i].LineIntersectionPoint(A, B));
            }
            return intersectionPoints.ToArray();
        }
        //Get all intersection points a segment makes with the frustum
        public Vec3[] FrustumIntersectionPoints(Vec3 A, Vec3 B)
        {
            List<Vec3> intersectionPoints = new List<Vec3>();
            for (int i = 0; i < 6; i++)
            {
                if (!planes[i].SegmentIntersects(A, B)) continue;
                Vec3 intersectionPoint = planes[i].LineIntersectionPoint(A, B);
                if (IsIntersectionPointInsideFrustum(intersectionPoint, i))
                {
                    intersectionPoints.Add(planes[i].LineIntersectionPoint(A, B));
                }
            }
            return intersectionPoints.ToArray();
        }
        bool IsIntersectionPointInsideFrustum(Vec3 p, int planeIdx) //Is a plane intersection point inside all the other planes of the frustum than the given plane
        {
            bool isInside = true;
            for (int i = 0; i < 6; i++)
            {
                if (i == planeIdx) continue;
                if (planes[i].IsPointOnPositiveSide(p)) isInside = false;
            }
            return isInside;
        }

        //Get the intersection point that lies on the frustum, when known that there is exactly one
        public Vec3 FrustumIntersectionPoint(Vec3 A, Vec3 B)
        {
            for (int i = 0; i < 6; i++)
            {
                if (!planes[i].SegmentIntersects(A, B)) continue;
                Vec3 intersectionPoint = planes[i].LineIntersectionPoint(A, B);
                if (IsIntersectionPointInsideFrustum(intersectionPoint, i))
                    return intersectionPoint;
            }
            Debug.Fail("Couldn't find intersection point");
            return Vec3.zero;
        }
        //The above method but also returns index of intersected plane
        public Tuple<Vec3,int> FrustumIntersectionPointAndPlane(Vec3 A, Vec3 B) 
        {
            for (int i = 0; i < 6; i++)
            {
                if (!planes[i].SegmentIntersects(A, B)) continue;
                Vec3 intersectionPoint = planes[i].LineIntersectionPoint(A, B);
                if (IsIntersectionPointInsideFrustum(intersectionPoint, i))
                    return new Tuple<Vec3,int>(intersectionPoint,i);
            }
            Debug.Fail("Couldn't find intersection point");
            return new Tuple<Vec3, int>(Vec3.zero,0);
        }

        //Get all intersection points of a segment and the frustum
        public Tuple<Vec3[], int[]> FrustumIntersectionPointsAndPlanes(Vec3 A, Vec3 B) 
        {
            List<Vec3> intersectionPoints = [];
            List<int> intersectionPlanes = [];
            for (int i = 0; i < 6; i++)
            {
                if (!planes[i].SegmentIntersects(A, B)) continue;
                Vec3 intersectionPoint = planes[i].LineIntersectionPoint(A, B);
                if (IsIntersectionPointInsideFrustum(intersectionPoint, i))
                {
                    intersectionPoints.Add(intersectionPoint);
                    intersectionPlanes.Add(i);
                }
            }
            Debug.Fail("Couldn't find intersection point");
            return new Tuple<Vec3[], int[]>(intersectionPoints.ToArray(), intersectionPlanes.ToArray());
        }

        //Clip a segment to the frustum and return the new segment
        //Returns null if segment is completely outside frustum
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

        // Get the intersection of a frustum diagonal line between two adjacent planes and a plane defined by a triangle
        Vec3? FrustumCornerIntersectionWithPlane(int[] frustumPlaneIndices, ref Plane p)
        {
            if (frustumPlaneIndices.Contains(0) || frustumPlaneIndices.Contains(1))
            {
                return null;
            }
            else
            {
                //A weird way to find if the two planes are adjacent
                int product = frustumPlaneIndices[0] * frustumPlaneIndices[1];
                if (product == 6 || product == 20)
                {
                    return null;
                }
                int lineNum = 0;
                switch (product)
                {
                    case 8: lineNum = 0; break;
                    case 10: lineNum = 1; break;
                    case 15: lineNum = 2; break;
                    case 12: lineNum = 3; break;
                }
                Vec3[] line = diagonalLines[lineNum];
                return p.LineIntersectionPoint(line[0], line[1]);
            }
        }

        //Algorithm for clipping triangles to the frustum
        public ITriangle[] ClipTriangles(Vec3[] vertices, IndexTriangle[] triangles)
        {
            List<ITriangle> clippedTriangles = [];
            bool[] isVerticesInside = new bool[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                isVerticesInside[i] = IsPointInside(vertices[i]);
            }
            void ClipTriangle(IndexTriangle triangle)
            {
                
                List<int> indicesInside = [];
                List<int> indicesOutside = [];
                foreach (int index in triangle.indices)
                {
                    if (isVerticesInside[index])
                    {
                        indicesInside.Add(index);
                    }
                    else indicesOutside.Add(index);
                }
                int countApicesInside = indicesInside.Count;
                switch (countApicesInside)
                {
                    
                    case 0:
                        ZeroApicesInside();
                        break;

                    case 1:
                        OneApexInside(vertices[indicesInside[0]],vertices[indicesOutside[0]], vertices[indicesOutside[1]]); 
                        break;
                    case 2:
                        TwoApicesInside(vertices[indicesInside[0]], vertices[indicesInside[1]], vertices[indicesOutside[0]]); break;
                    case 3:
                        clippedTriangles.Add(triangle); break;
                }
                void ZeroApicesInside()
                {
                    //If none of the apices are inside and triangle still intersects frustum,
                    //pick a point that is inside from a side of the triangle that is partly inside,
                    //divide triangle to two triangles by that point and the opposing apex,
                    //and do 1-apex-inside clip for both triangles
                    Vec3[] apices = [vertices[indicesOutside[0]], vertices[indicesOutside[1]], vertices[indicesOutside[2]]];
                    Vec3[] frustumIntersectionPoints = FrustumIntersectionPoints(apices[0], apices[1]);
                    Vec3 insideSegmentA, insideSegmentB, opposingApex;
                    if (frustumIntersectionPoints.Length == 2)
                    {
                        insideSegmentA = apices[0];
                        insideSegmentB = apices[1];
                        opposingApex = apices[2];
                    }
                    else
                    {
                        frustumIntersectionPoints = FrustumIntersectionPoints(apices[1], apices[2]);
                        if (frustumIntersectionPoints.Length == 2)
                        {
                            insideSegmentA = apices[1];
                            insideSegmentB = apices[2];
                            opposingApex = apices[0];
                        }
                        else
                        {
                            frustumIntersectionPoints = FrustumIntersectionPoints(apices[0], apices[2]);
                            if (frustumIntersectionPoints.Length == 2)
                            {
                                insideSegmentA = apices[0];
                                insideSegmentB = apices[2];
                                opposingApex = apices[1];
                            }
                            else
                            {
                                Console.WriteLine("No intersection found");
                                return;
                            }
                        }
                    }
                    Vec3 insidePoint = Vec3.Lerp(frustumIntersectionPoints[0], frustumIntersectionPoints[1], 0.5f);

                    OneApexInside(insidePoint, opposingApex, insideSegmentA);
                    OneApexInside(insidePoint, insideSegmentB, opposingApex);
                }
                void OneApexInside(Vec3 insideApex, Vec3 outsideApex1, Vec3 outsideApex2)
                {
                    var intersectionResult1 = FrustumIntersectionPointAndPlane(insideApex, outsideApex1);
                    var intersectionResult2 = FrustumIntersectionPointAndPlane(insideApex, outsideApex2);
                    Vec3 intersectionPoint1 = intersectionResult1.Item1;
                    Vec3 intersectionPoint2 = intersectionResult2.Item1;
                    clippedTriangles.Add(new ApexTriangle(
                        [insideApex, intersectionPoint1, intersectionPoint2],
                        triangle.normal, triangle.color));

                    int planeIdx1 = intersectionResult1.Item2;
                    int planeIdx2 = intersectionResult2.Item2;
                    int[] planeIndexes = [planeIdx1, planeIdx2];
                    // If intersections were with different planes:
                    if (planeIdx1 != planeIdx2)
                    {
                        if (planeIdx1 < 2 || planeIdx2 < 2) return; //Skip this part if either of them is the near or far plane.

                        // If line connecting outside apices intersects with frustum, cut the shape to that line
                        
                        Vec3 outsideLineIntersection1 = planes[planeIdx1].LineIntersectionPoint(outsideApex1, outsideApex2);
                        if (IsIntersectionPointInsideFrustum(outsideLineIntersection1, planeIdx1))
                        {
                            Vec3 outsideLineIntersection2 = planes[planeIdx2].LineIntersectionPoint(outsideApex1, outsideApex2);
                            if (IsIntersectionPointInsideFrustum(outsideLineIntersection2, planeIdx2)) //Floating point error fix
                            {
                                if ((planeIndexes.Contains(2) && planeIndexes.Contains(3)) 
                                    || (planeIndexes.Contains(4) && planeIndexes.Contains(5)))
                                {
                                    //If opposing planes and outsideLineIntersection connection intersects any other plane,
                                    //cut triangle into two by insidepoint and another point inside the frustum on the outside connection
                                    int[] planesToCheck = Enumerable.Range(0,6).Where(i => !(i == planeIdx1 || i == planeIdx2)).ToArray();
                                    bool cutToTwo = false;
                                    foreach (int i in planesToCheck)
                                    {
                                        if (planes[i].SegmentIntersects(outsideApex1, outsideApex2))
                                        {
                                            cutToTwo = true;
                                            Vec3 OpposingSidePoint = Vec3.Lerp(intersectionPoint1, intersectionPoint2, 0.5f);
                                            //If opposing side point is outside this plane, do one apex inside, otherwise do two apices inside
                                            if (planes[i].IsPointOnPositiveSide(OpposingSidePoint))
                                            {
                                                OneApexInside(insideApex, outsideApex1, OpposingSidePoint);
                                                OneApexInside(insideApex, OpposingSidePoint, outsideApex2);
                                            }
                                            else
                                            {
                                                TwoApicesInside(insideApex, OpposingSidePoint, outsideApex1);
                                                TwoApicesInside(insideApex, OpposingSidePoint, outsideApex2);
                                            }
                                            return;
                                        }
                                    }
                                }
                                else
                                {
                                    clippedTriangles.AddRange([
                                        new ApexTriangle(
                                            [outsideLineIntersection1, outsideLineIntersection2, intersectionPoint1],
                                            triangle.normal, triangle.color),
                                        new ApexTriangle(
                                            [intersectionPoint1, outsideLineIntersection2, intersectionPoint2],
                                            triangle.normal, triangle.color),
                                    ]);
                                    return;
                                }
                            }
                        }
                        // Else, add frustum corner triangle
                        Plane trianglePlane = new Plane(insideApex, triangle.normal);
                        Vec3? cornerPointNullable = FrustumCornerIntersectionWithPlane([planeIdx1, planeIdx2], ref trianglePlane);
                        if (cornerPointNullable is not null)
                        {
                            Vec3 cornerPoint = (Vec3)cornerPointNullable;
                            clippedTriangles.Add(new ApexTriangle(
                                [intersectionPoint1, cornerPoint, intersectionPoint2],
                                triangle.normal, triangle.color));
                        }
                    }
                }
                void TwoApicesInside(Vec3 insideApex1, Vec3 insideApex2, Vec3 outsideApex)
                {
                    var intersectionResult1 = FrustumIntersectionPointAndPlane(insideApex1, outsideApex);
                    var intersectionResult2 = FrustumIntersectionPointAndPlane(insideApex2, outsideApex);
                    Vec3 intersectionPoint1 = intersectionResult1.Item1;
                    Vec3 intersectionPoint2 = intersectionResult2.Item1;
                    clippedTriangles.AddRange([
                        new ApexTriangle(
                        [intersectionPoint1, insideApex2, insideApex1],
                        triangle.normal, triangle.color),
                        new ApexTriangle(
                        [intersectionPoint2, insideApex2, intersectionPoint1],
                        triangle.normal, triangle.color),
                    ]);
                    // If intersections were with different planes, add corner triangle
                    int plane1 = intersectionResult1.Item2;
                    int plane2 = intersectionResult2.Item2;
                    if (plane1 != plane2)
                    {
                        if (plane1 < 2 || plane2 < 2) return; //Skip this part if either of them is the near or far plane.
                        Plane trianglePlane = new Plane(insideApex1, triangle.normal);
                        Vec3? cornerPointNullable = FrustumCornerIntersectionWithPlane([plane1, plane2], ref trianglePlane);
                        if (cornerPointNullable is not null)
                        {
                            Vec3 cornerPoint = (Vec3)cornerPointNullable;
                            clippedTriangles.Add(new ApexTriangle(
                                [intersectionPoint1, cornerPoint, intersectionPoint2],
                                triangle.normal, triangle.color));
                        }
                    }
                }
            }
            for (int i = 0; i < triangles.Length; i++)
            {
                ClipTriangle(triangles[i]);
            }
            return clippedTriangles.ToArray();
        }
    }
}
