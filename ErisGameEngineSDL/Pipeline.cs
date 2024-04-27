using ErisGameEngineSDL.ErisLibraries;
using ErisMath;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ErisGameEngineSDL
{
    internal class Pipeline
    {
        Vec2 viewPortSize, halfViewPortSize;
        float viewPortDistance;
        public uint[,] frameBuffer;
        float[,] depthBuffer;
        static readonly ushort[] triangleSegmentIndices = [0, 1, 1, 2, 2, 0];

        public Vec2int targetResolution;
        Camera camera;
        Transform cameraTransform;
        Quaternion cameraRotation, invertedCameraRotation;

        RectangleFrustum worldSpaceFrustum, cameraSpaceFrustum;

        float ambientLighting = 0.4f;
        public Vec3 globalLightDir = -Vec3.one;

        public Pipeline(Vec2int targetResolution, Camera camera) 
        {
            this.targetResolution = targetResolution;
            this.camera = camera;
            cameraTransform = camera.transform;
            frameBuffer = new uint[targetResolution.x, targetResolution.y];
            depthBuffer = new float[targetResolution.x, targetResolution.y];

            viewPortSize = camera.viewPortSize;
            halfViewPortSize = viewPortSize / 2;
            cameraTransform = camera.transform;
            worldSpaceFrustum = camera.worldSpaceFrustum;
            cameraSpaceFrustum = camera.cameraSpaceFrustum;
        }
        //Render only segments of triangles, with only frustum segment clipping.
        public uint[,] RenderTriangleSegmentsNoClip(Shaped3DObject[] gameObjects)
        {
            frameBuffer = new uint[targetResolution.x,targetResolution.y];
            depthBuffer = new float[targetResolution.x,targetResolution.y];

            cameraRotation = camera.transform.rotation;
            invertedCameraRotation = cameraRotation.inverted();
            foreach (Shaped3DObject go in gameObjects)
            {
                if (go == null) continue;
                //transformed vertices from object space to camera space
                Vec3[] cameraSpaceVertices = ObjectVerticesToCameraSpace(go);

                //Frustum culling
                if (!worldSpaceFrustum.IsGameObjectPartlyInside(go)) continue;

                int[] segments = GetSegmentsFromIndexTriangles(go.transformedMesh.triangles);
                FrustumClipAndRasterizeSegments(segments, cameraSpaceVertices);
            }
            return frameBuffer;
        }

        //Render only segments of triangles, with frustum triangle clipping visible.
        public uint[,] RenderTriangleSegments(Shaped3DObject[] gameObjects) 
        {
            //Clear frame buffer and depth buffer
            frameBuffer = new uint[targetResolution.x, targetResolution.y];
            depthBuffer = new float[targetResolution.x, targetResolution.y];

            //Get camera rotation for rotating vertices
            cameraRotation = camera.transform.rotation;
            invertedCameraRotation = cameraRotation.inverted();

            foreach (Shaped3DObject go in gameObjects)
            {
                if (go == null) continue;
                //transformed vertices from object space to camera space
                Vec3[] cameraSpaceVertices = ObjectVerticesToCameraSpace(go);

                //Frustum culling
                if (!worldSpaceFrustum.IsGameObjectPartlyInside(go)) continue;

                //If gameobject completely inside frustum, render triangle segments as they are.
                IndexTriangle[] indexTriangles = go.transformedMesh.triangles;
                if (worldSpaceFrustum.IsGameObjectCompletelyInside(go))
                {
                    RasterizeSegments(GetSegmentsFromIndexTriangles(indexTriangles), cameraSpaceVertices);
                    continue;
                }
                //Else, clip triangles first
                ITriangle[] clippedTriangles = cameraSpaceFrustum.ClipTriangles(cameraSpaceVertices, indexTriangles);

                //Rasterize triangles to frame buffer
                foreach (ITriangle triangle in clippedTriangles)
                {
                    Vec3[] apices = triangle.GetApices(cameraSpaceVertices);
                    Vec3 a = apices[0];
                    Vec3 b = apices[1];
                    Vec3 c = apices[2];

                    //Rasterize each segment of the triangle
                    RasterizeSegment(a, b);
                    RasterizeSegment(b, c);
                    RasterizeSegment(c, a);
                }
            }
            return frameBuffer;
        }
        public uint[,] RenderTriangles(Shaped3DObject[] gameObjects)
        {
            frameBuffer = new uint[targetResolution.x, targetResolution.y];
            depthBuffer = new float[targetResolution.x, targetResolution.y];
            cameraRotation = camera.transform.rotation;
            invertedCameraRotation = cameraRotation.inverted();
            foreach (Shaped3DObject go in gameObjects)
            {
                if (go == null) continue;
                //transformed vertices from object space to camera space
                Vec3[] vertices = go.transformedMesh.vertices;
                Vec3[] cameraSpaceVertices = ObjectVerticesToCameraSpace(go);

                //Frustum culling
                if (!worldSpaceFrustum.IsGameObjectPartlyInside(go)) continue;

                //If gameobject completely inside frustum, render triangles as they are.
                IndexTriangle[] indexTriangles = go.transformedMesh.triangles;
                Vec3 camPos = camera.transform.position;
                bool isFacingCamera(IndexTriangle t)
                {
                    Vec3[] apices = t.GetApices(cameraSpaceVertices);
                    Vec3 cameraSpaceNormal = ITriangle.TriangleNormal(apices);
                    return Vec3.Dot(apices[0], cameraSpaceNormal) >= 0
                            || Vec3.Dot(apices[1], cameraSpaceNormal) >= 0
                            || Vec3.Dot(apices[2], cameraSpaceNormal) >= 0;
                }
                IndexTriangle[] cameraFacingTriangles = indexTriangles.Where(t => isFacingCamera(t)).ToArray();
                if (worldSpaceFrustum.IsGameObjectCompletelyInside(go))
                {
                    //Rasterize triangles to frame buffer
                    foreach (IndexTriangle triangle in cameraFacingTriangles)
                    {
                        Vec3[] apices = triangle.GetApices(cameraSpaceVertices);
                        RasterizeTriangle(apices, triangle.color, triangle.normal);
                    }
                    continue;
                }
                //Else, clip triangles first
                ITriangle[] clippedTriangles = cameraSpaceFrustum.ClipTriangles(cameraSpaceVertices, cameraFacingTriangles);
                //Rasterize triangles to frame buffer
                foreach (ITriangle triangle in clippedTriangles)
                {
                    Vec3 normal = triangle.GetNormal();
                    Vec3[] apices = triangle.GetApices(cameraSpaceVertices);
                    ColorByte color = triangle.GetColor();
                    RasterizeTriangle(apices, color, normal);
                }
            }
            return frameBuffer;
        }
        
        int[] GetSegmentsFromIndexTriangles(IndexTriangle[] triangles)
        {
            List<int> segments = new List<int>();
            foreach (IndexTriangle triangle in triangles)
            {
                int[] indices = triangle.indices;
                int[] triangleSegments = triangleSegmentIndices.Select(i => indices[i]).ToArray();
                segments.AddRange(triangleSegments);
            }
            return segments.ToArray();
        }
        Vec3[] ObjectVerticesToCameraSpace(Shaped3DObject go)
        {
            Vec3[] vertices = go.transformedMesh.vertices;
            Vec3 relPos = go.transform.position - cameraTransform.position;
            Vec3[] verticesRelPos = vertices.Select(v => relPos + v).ToArray();
            return Quaternion.RotateVectors(verticesRelPos, invertedCameraRotation, cameraRotation);
        }
        void DepthWrite(int pixelPosX, int pixelPosY, float depth, ColorByte color, Vec3 normal)
        {
            if (depth is float.NaN) Debug.Fail("NAN");
            float depthBufferValue = depthBuffer[pixelPosX, pixelPosY];
            if (depthBufferValue == 0 || depth <= depthBufferValue)
            {
                depthBuffer[pixelPosX, pixelPosY] = depth;
                if (Game.instance.drawModeNum == 4)
                {
                    Console.WriteLine(depth);
                    Game.instance.DrawPixel(pixelPosX, pixelPosY, color);
                }
                frameBuffer[pixelPosX, pixelPosY] = color.ToUint();
            }
        }
        
        void FrustumClipAndRasterizeSegments(int[] segments, Vec3[] cameraSpaceVertices)
        {
            for (int i = 0; i < segments.Length - 1; i += 2)
            {
                int index1 = segments[i];
                int index2 = segments[i + 1];
                Vec3 a = cameraSpaceVertices[index1];
                Vec3 b = cameraSpaceVertices[index2];

                // Frustum clipping
                if (!(cameraSpaceFrustum.IsPointInside(a) && cameraSpaceFrustum.IsPointInside(b)))
                {
                    var result2 = cameraSpaceFrustum.ClipSegment(a, b);
                    if (result2 == null) continue;
                    a = result2.Item1; b = result2.Item2;
                }
                RasterizeSegment(a, b);
            }
        }
        void RasterizeTriangle(Vec3[] apices, ColorByte color, Vec3 preCalculatedNormal)
        {
            ColorByte diffuse = color * Math.Clamp((Vec3.Dot(preCalculatedNormal, globalLightDir) + 1) / 2, ambientLighting, 1);
            // Camera-relative vertices
            Vec3 aRel = apices[0];
            Vec3 bRel = apices[1];
            Vec3 cRel = apices[2];
            
            // Projected vertices (not rounded to pixels)
            Vec2 aProj = ProjectFloat(aRel);
            Vec2 bProj = ProjectFloat(bRel);
            Vec2 cProj = ProjectFloat(cRel);

            // Rounded projected vertices
            Vec2int aPixel = aProj.ToInt();
            Vec2int bPixel = bProj.ToInt();
            Vec2int cPixel = cProj.ToInt();

            // Projected vertices with relative vertices' depth as the third value
            // to carry the depth information attached to the vertex.
            Vec3 a = new Vec3(aProj.x, aProj.y, aRel.z);
            Vec3 b = new Vec3(bProj.x, bProj.y, bRel.z);
            Vec3 c = new Vec3(cProj.x, cProj.y, cRel.z);

            // Optimal find triangle bounding rectangle
            /*
            int startX, endX, startY, endY;
            startX = endX = a.x; 
            startY = endY = a.y;
            
            if (b.x < startX) startX = b.x;
            if (b.x > endX) endX = b.x;
            if (c.x < startX) startX = c.x;
            if (c.x > endX) endX = c.x;
            
            if (b.y < startY) startY = b.y;
            if (b.y > endY) endY = b.y;
            if (c.y < startY) startY = c.y;
            if (c.y > endY) endY = c.y;
            */

            //Find centroid of the 3D triangle and set triangle color based on projected screen position and depth
            /*
            Vec3 centroid3d = ITriangle.Centroid(apices);
            Vec2int centroidProj = Project(centroid3d);
            byte color_r = (byte)(255 * centroidProj.x / (float)targetResolution.x);
            byte color_g = (byte)(255 * centroidProj.y / (float)targetResolution.y);
            byte color_b = (byte)(255*Math.Clamp(1f - centroid3d.z / camera.farClipPlaneDistance, 0, 1f));
            ColorByte posColor = new ColorByte(color_r, color_g, color_b);
            */
            //My methods involve taking the vertex with the different y value,
            //taking the vectors from that vertex to the flat left and right vertices,
            //and multiplying them by the y-progress to get the start x and end x for the x-row.

            //Algorithm for rasterizing a projected triangle with a flat top side or flat bottom side
            void RasterizeFlatTriangle(Vec3 flatleft, Vec3 flatright, Vec3 peak, bool isFlatTop)
            {
                //Imagine vectors going from the peak vertex to the flat left and flat right vertices
                //Get the vectors' x-values, 
                float xDiffL = flatleft.x - peak.x;
                float xDiffR = flatright.x - peak.x;
                float zDiffL = flatleft.z - peak.z;
                float zDiffR = flatright.z - peak.z;
                int yPixelStart = (int)peak.y;
                if (yPixelStart == -1) yPixelStart++;
                int pixelEnd = (int)flatleft.y;
                int yPixelDiff = Math.Abs(pixelEnd - yPixelStart);
                float yFloatDiffL = flatleft.y - peak.y;
                float yFloatDiffR = flatright.y - peak.y;
                bool isTopLeft;
                if (isFlatTop) isTopLeft = xDiffL > 0;
                else
                {
                    yFloatDiffL = -yFloatDiffL;
                    yFloatDiffR = -yFloatDiffR;
                    isTopLeft = xDiffL < 0;
                }
                for (int j = 0; j <= yPixelDiff; j++)
                {
                    int yPixelCurrent = yPixelStart + (isFlatTop ? j : -j); //Screen pixelheight of the current row
                    // "continue" because it should cause an error when it's higher than the resolution y,
                    // because that means there's something wrong with the code.
                    // It can skip if and only if it's exactly the same as the resolution y
                    if (yPixelCurrent == targetResolution.y) continue;

                    //Get percent progress of the y-loop and multiply by
                    //x differences to the left and right to get the start and end x of the row
                    float pixelFloatYDist = peak.y - (yPixelCurrent + 0.5f);
                    if (isFlatTop) pixelFloatYDist = -pixelFloatYDist;
                    float yProgressL = pixelFloatYDist / yFloatDiffL;
                    float yProgressR = pixelFloatYDist / yFloatDiffR;

                    //Determine start and end values for the pixel row.
                    //Also determine the real float value of the row start and end for accurate interpolation
                    float rowStartFloat = peak.x +
                    (yProgressL > 1f ? xDiffL :
                    yProgressL < 0 ? 0 :
                    (yProgressL * xDiffL));
                    int rowStart = (int)rowStartFloat;
                    if (!isTopLeft) rowStart++; //Top left pixels get priority
                    float rowEndFloat = peak.x +
                        (yProgressR > 1f ? xDiffR :
                        yProgressR < 0 ? 0 :
                        (yProgressR * xDiffR));
                    int rowEnd = (int)rowEndFloat;
                    if (rowEnd == targetResolution.x + 1) rowEnd--;
                    int rowXDiff = rowEnd - rowStart;
                    float zStart, zEnd;
                    //If row is one pixel wide
                    if (rowXDiff == 0)
                    {
                        if (rowStart == targetResolution.x) continue;
                        float pixelFloatX = rowStart + 0.5f;
                        float depth;
                        float pixelFloatY = yPixelCurrent + 0.5f;
                        if (!isFlatTop && pixelFloatY >= peak.y
                            || isFlatTop && pixelFloatY <= peak.y) depth = peak.z;
                        else if (pixelFloatX < rowStartFloat) depth = peak.z + yProgressL * zDiffL;
                        else if (pixelFloatX > rowEndFloat) depth = peak.z + yProgressR * zDiffR;
                        else
                        {
                            zStart = peak.z + yProgressL * zDiffL;
                            zEnd = peak.z + yProgressR * zDiffR;
                            float lerpT = (pixelFloatX - rowStartFloat) / (rowEndFloat - rowStartFloat);
                            depth = zStart + lerpT * (zEnd - zStart);
                        }
                        DepthWrite(rowStart, yPixelCurrent, depth, diffuse, preCalculatedNormal);
                        continue;
                    }
                    // Get interpolated z value by interpolating both the flatvertex-to-peak vectors by 
                    // the sided y-progress values, then interpolating between those by the x-progress value
                    zStart = peak.z + (yProgressL * zDiffL);
                    zEnd = peak.z + (yProgressR * zDiffR);
                    float zRowDiff = zEnd - zStart;
                    float rowXFloatDiff = rowEndFloat - rowStartFloat;
                    for (int i = 0; i <= rowXDiff; i++)
                    {
                        int currentPixelX = rowStart + i;
                        if (currentPixelX == targetResolution.x) continue; // Same reason for "continue" here
                        float currentFloatX = currentPixelX + 0.5f;
                        float depth;
                        if (i == 0 && currentFloatX < rowStartFloat) depth = zStart;
                        else if (i == rowXDiff && currentFloatX > rowEndFloat) depth = zEnd;
                        else
                        {
                            float lerpT = (currentFloatX - rowStartFloat) / rowXFloatDiff;
                            depth = zStart + (lerpT * zRowDiff);
                        }
                        DepthWrite(currentPixelX, yPixelCurrent, depth, diffuse, preCalculatedNormal);
                    }
                }
            }
            /*
            {
                
                float xDiffL = topleft.x - bottom.x;
                float xDiffR = topright.x - bottom.x;
                float zDiffL = topleft.z - bottom.z;
                float zDiffR = topright.z - bottom.z;
                int yPixelStart = (int)bottom.y;
                if (yPixelStart == -1) yPixelStart++;
                int yPixelDiff = (int)topleft.y - yPixelStart;
                float yFloatDiffL = topleft.y - bottom.y;
                float yFloatDiffR = topright.y - bottom.y;
                if (yPixelDiff <= 0) Debug.Fail("yPixelDiff should always be positive");
                bool isTopLeft = xDiffL > 0;

                for (int j = 0; j <= yPixelDiff; j++)
                {
                    int yPixelCurrent = yPixelStart + j; //Screen pixelheight of the current row
                    if (yPixelCurrent == targetResolution.y) continue;
                    // "continue" because it should cause an error when it's higher than the resolution y,
                    // because that means there's something wrong with the code.
                    // It can skip if and only if it's exactly the same as the resolution y
                    float pixelFloatYDist = yPixelCurrent + 0.5f - bottom.y;

                    float yFloatProgressL = pixelFloatYDist / yFloatDiffL;
                    float yFloatProgressR = pixelFloatYDist / yFloatDiffR;
                    //Get percent progress of the y-loop and multiply by
                    //x differences to the left and right to get the start and end x of the row
                    RasterizeRow(true, bottom, isTopLeft, xDiffL, xDiffR, zDiffL, zDiffR, yPixelCurrent, yFloatProgressL, yFloatProgressR);
                }
            }*/

            //Method for rasterizing a projected triangle with a flat bottom
            //Same as FlatTop but instead of going from bottom to top, this one goes from top to bottom
            /*
            {
                float xDiffL = bottomleft.x - top.x;
                float xDiffR = bottomright.x - top.x;
                float zDiffL = bottomleft.z - top.z;
                float zDiffR = bottomright.z - top.z;
                int yPixelStart = (int)top.y;
                if (yPixelStart == -1) yPixelStart++;
                int yPixelDiff = yPixelStart - (int)bottomleft.y;
                float yFloatDiffL = top.y - bottomleft.y;
                float yFloatDiffR = top.y - bottomright.y;
                if (yPixelDiff <= 0) Debug.Fail("yPixelDiff should always be positive");
                bool isTopLeft = xDiffL < 0;
                for (int j = 0; j <= yPixelDiff; j++)
                {
                    int yPixelCurrent = yPixelStart - j;
                    if (yPixelCurrent == targetResolution.y) continue;
                    float pixelFloatYDist = top.y - (yPixelCurrent + 0.5f);
                    
                    float yFloatProgressL = pixelFloatYDist / yFloatDiffL;
                    float yFloatProgressR = pixelFloatYDist / yFloatDiffR;
                    RasterizeRow(false, top, isTopLeft, xDiffL, xDiffR, zDiffL, zDiffR, yPixelCurrent, yFloatProgressL, yFloatProgressR);
                }
            }*/

            //Method for rasterizing a row in the forloop of the triangle y-value.
            //I differentiated this from FlatTop() and FlatBottom() because it's the same in both
            /*
            void RasterizeRow(bool isFlatTop, Vec3 thirdVertex, bool isTopLeft, float xDiffL, float xDiffR, float zDiffL, float zDiffR, int yPixelCurrent, float yProgressL, float yProgressR)
            {

                float rowStartFloat = thirdVertex.x + 
                    (yProgressL > 1f  ? xDiffL :
                    yProgressL < 0 ? 0 :
                    (yProgressL * xDiffL));
                int rowStart = (int)rowStartFloat;
                if (!isTopLeft) rowStart++;
                //if (rowStart < 0 && rowStart >= -5) rowStart = 0;
                float rowEndFloat = thirdVertex.x + 
                    (yProgressR > 1f ? xDiffR :
                    yProgressR < 0 ? 0 :
                    (yProgressR * xDiffR));
                int rowEnd = (int)rowEndFloat;
                if (rowEnd == targetResolution.x + 1) rowEnd--;
                int rowXDiff = rowEnd - rowStart;
                float zStart, zEnd;
                //If row is one pixel wide
                if (rowXDiff == 0)
                {
                    if (rowStart == targetResolution.x || yPixelCurrent == targetResolution.y) return;
                    float pixelFloatX = rowStart + 0.5f;
                    float depth;
                    float pixelFloatY = yPixelCurrent + 0.5f;
                    if (!isFlatTop && pixelFloatY >= thirdVertex.y
                        || isFlatTop && pixelFloatY <= thirdVertex.y) depth = thirdVertex.z;
                    else if (pixelFloatX < rowStartFloat) depth = thirdVertex.z + yProgressL * zDiffL;
                    else if (pixelFloatX > rowEndFloat) depth = thirdVertex.z + yProgressR * zDiffR;
                    else
                    {
                        zStart = thirdVertex.z + yProgressL * zDiffL;
                        zEnd = thirdVertex.z + yProgressR * zDiffR;
                        float lerpT = (pixelFloatX - rowStartFloat) / (rowEndFloat - rowStartFloat);
                        depth = zStart + lerpT * (zEnd - zStart);
                    }
                    DepthWrite(rowStart, yPixelCurrent, depth, diffuse, preCalculatedNormal);
                    return;
                }
                //Get interpolated z value by interpolating both the bottom to top vectors by the y-progress value,
                //then interpolating between those by the x-progress value
                zStart = thirdVertex.z + (yProgressL * zDiffL);
                zEnd = thirdVertex.z + (yProgressR * zDiffR);
                float zRowDiff = zEnd - zStart;
                float rowXFloatDiff = rowEndFloat - rowStartFloat;
                for (int i = 0; i <= rowXDiff; i++)
                {
                    int currentPixelX = rowStart + i;
                    if (currentPixelX == targetResolution.x) continue; // Same reason for "continue" here
                    float currentFloatX = currentPixelX+0.5f;
                    float depth;
                    if (i == 0 && currentFloatX < rowStartFloat) depth = zStart;
                    else if (i == rowXDiff && currentFloatX > rowEndFloat) depth = zEnd;
                    else
                    {
                        float lerpT = (currentFloatX - rowStartFloat) / rowXFloatDiff;
                        depth = zStart + (lerpT * zRowDiff);
                    }
                    
                    DepthWrite(currentPixelX, yPixelCurrent, depth, diffuse, preCalculatedNormal);
                }
            }*/

            //Method for determining order of vertices when two of the y-values are the same
            void Flat(Vec3 flat1, Vec3 flat2, Vec3 peak)
            {
                bool isFlatTop = peak.y < flat1.y; //Is the triangle a flat top triangle or a flat bottom triangle
                Vec3 flatleft, flatright;
                //Sort vertices of the flat side by x-value
                if (flat1.x < flat2.x)
                {
                    flatleft = flat1;
                    flatright = flat2;
                }
                else
                {
                    flatleft = flat2;
                    flatright = flat1;
                }
                RasterizeFlatTriangle(flatleft, flatright, peak, isFlatTop);
            }
            //Method for dividing a triangle into a flat bottom triangle and a flat top triangle by the middle vertex line
            void DivideTriangle()
            {
                //Sort projected triangle vertices by height
                Vec3 top = a; Vec3 mid = b; Vec3 bot = c;
                if (b.y > a.y)
                {
                    top = b;
                    mid = a;
                }
                if (bot.y > mid.y)
                {
                    Vec3 temp = mid;
                    mid = bot;
                    bot = temp;
                }
                if (mid.y > top.y)
                {
                    Vec3 temp = top;
                    top = mid;
                    mid = temp;
                }

                //Divide into two triangles with a horizontal line going from the middle vertex
                //to an interpolated division point on the opposing triangle side of the middle vertex.
                //The division point also has an interpolated depth value.
                float lerpT = (mid.y - top.y) / (bot.y - top.y);
                float divPointX = (int)bot.x == (int)top.x ? bot.x : top.x + ((bot.x - top.x) * lerpT);
                float divPointDepth = top.z + ((bot.z - top.z) * lerpT);
                Vec3 divPoint = new Vec3(divPointX, mid.y, divPointDepth);

                //Sort mid and divPoint by x-value
                Vec3 midLeft, midRight;
                if (mid.x > divPointX) { midLeft = divPoint; midRight = mid; }
                else { midLeft = mid; midRight = divPoint; }

                RasterizeFlatTriangle(midLeft, midRight, top, false);
                RasterizeFlatTriangle(midLeft, midRight, bot, true);
            }
            //Check for a flat top or bottom, if doens't have,
            //divide into two triangles with a flat bottom and flat top
            if ((aPixel.y == bPixel.y && aPixel.y == cPixel.y) || (aPixel.x == bPixel.x && aPixel.x == cPixel.x)) 
                return; //If no y- or x- difference, don't draw the triangle
            if (aPixel.y == bPixel.y) Flat(a, b, c);
            else if (aPixel.y == cPixel.y) Flat(a, c, b);
            else if (bPixel.y == cPixel.y) Flat(b, c, a);
            else DivideTriangle();
        }
        void RasterizeSegments(int[] segments, Vec3[] cameraSpaceVertices)
        {
            for (int i = 0; i < segments.Length - 1; i += 2)
            {
                int index1 = segments[i];
                int index2 = segments[i + 1];
                Vec3 a = cameraSpaceVertices[index1];
                Vec3 b = cameraSpaceVertices[index2];
                RasterizeSegment(a, b);
            }
        }
        void RasterizeSegment(Vec3 a, Vec3 b)
        {
            // Rasterize segment with depth to frame buffer
            float zStart = a.z;
            float zDiff = b.z - a.z;
            Vec2int frameA = Project(a);
            Vec2int frameA2B = Project(b) - frameA;
            int amountOfPoints = (int)Math.Ceiling(frameA2B.magnitude() * 3);
            if (amountOfPoints == 0) amountOfPoints = 1;
            for (int j = 0; j <= amountOfPoints; j++)
            {
                float progress = (float)j / amountOfPoints;
                Vec2 frameProgressVector = frameA + frameA2B * progress;
                float depth = zStart + zDiff * progress;
                Vec2int pixelCoords = new Vec2int((int)(frameProgressVector.x), (int)(frameProgressVector.y));
                float depthBufferValue = depthBuffer[pixelCoords.x, pixelCoords.y];
                if (depthBufferValue == 0 || depth < depthBufferValue)
                {
                    depthBuffer[pixelCoords.x, pixelCoords.y] = depth;
                    float fade = Math.Clamp(1f - depth / 10f, 0, 1f);
                    fade = 1; //Temp 
                    byte c_r = (byte)(255 * pixelCoords.x * fade / (float)targetResolution.x);
                    byte c_g = (byte)(255 * pixelCoords.y * fade / (float)targetResolution.y);
                    byte c_b = (byte)(255 * (float)j * fade / amountOfPoints);
                    ColorByte color = new ColorByte(c_r, c_g, c_b);
                    frameBuffer[pixelCoords.x, pixelCoords.y] = color.ToUint();
                }
            }
        }
        Vec2int Project(Vec3 v) //Camera-relative position to resolution pixel coordinates
        {
            Vec2int framePos = new Vec2int(
                   (int)(targetResolution.x * ((camera.viewPlaneDistance * v.x  / (viewPortSize.x*v.z)) + 0.5f)),
                   (int)(targetResolution.y * ((camera.viewPlaneDistance * v.y / (viewPortSize.y*v.z)) + 0.5f)));
            if (framePos.x == targetResolution.x) framePos.x--;
            if (framePos.y == targetResolution.y) framePos.y--;
            return framePos;
        }
        Vec2 ProjectFloat(Vec3 v) //Camera-relative position to unrounded resolution coordinates
        {
            Vec2 framePos = new Vec2(
                   (targetResolution.x * ((camera.viewPlaneDistance * v.x / (viewPortSize.x * v.z)) + 0.5f)),
                   (targetResolution.y * ((camera.viewPlaneDistance * v.y / (viewPortSize.y * v.z)) + 0.5f)));
            return framePos;
        }
    }
}
