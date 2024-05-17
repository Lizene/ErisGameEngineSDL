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
        public uint[,] frameBuffer;
        float[,] depthBuffer;

        public Vec2int targetResolution;
        Vec2 viewPortSize;
        static readonly ushort[] triangleSegmentIndices = [0, 1, 1, 2, 2, 0];

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
                if (!worldSpaceFrustum.IsObjectPartlyInside(go)) continue;

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
                if (!worldSpaceFrustum.IsObjectPartlyInside(go)) continue;

                //If gameobject completely inside frustum, render triangle segments as they are.
                IndexTriangle[] indexTriangles = go.transformedMesh.triangles;
                if (worldSpaceFrustum.IsObjectCompletelyInside(go))
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
                if (!worldSpaceFrustum.IsObjectPartlyInside(go)) continue;

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
                if (worldSpaceFrustum.IsObjectCompletelyInside(go))
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
            // Get diffuse lighting color
            ColorByte diffuse = color * Math.Clamp((Vec3.Dot(preCalculatedNormal, globalLightDir) + 1) / 2, ambientLighting, 1);

            // Camera-relative apices
            Vec3 aRel = apices[0];
            Vec3 bRel = apices[1];
            Vec3 cRel = apices[2];
            if (aRel.z < 0 || aRel.z < 0 || aRel.z < 0) Console.WriteLine("Depth is negative");


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

            // My algorithm for rasterizing a projected triangle with a flat top side or flat bottom side
            // My algorithm involves taking the vertex with the different y value,
            // taking the vectors from that vertex to the flat left and right vertices,
            // and multiplying them by the y-progress to get the start x and end x for the x-row.
            void RasterizeFlatTriangle(Vec3 flatleft, Vec3 flatright, Vec3 peak, bool isFlatTop)
            {
                //Take vectors going from the peak vertex to the flat left and flat right vertices
                Vec3 LDiff = flatleft - peak;
                Vec3 RDiff = flatright - peak;
                float peakZReci = 1 / peak.z;
                float leftZReci = 1 / flatleft.z;
                float rightZReci = 1 / flatright.z;
                float LzReciDiff = leftZReci - peakZReci;
                float RzReciDiff = rightZReci - peakZReci;
                if (!isFlatTop)
                {
                    LDiff.y = -LDiff.y; // Y difference should be absolute in this algorithm
                    RDiff.y = -RDiff.y;
                }
                int yPixelStart = (int)peak.y;
                int yPixelDiff = Math.Abs((int)flatleft.y - yPixelStart);
                //Start loop from the second pixel row if it starts from -1
                for (int j = yPixelStart == -1 ? 1 : 0; j <= yPixelDiff; j++)
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
                    float yProgressL = pixelFloatYDist / LDiff.y;
                    float yProgressR = pixelFloatYDist / RDiff.y;

                    //Determine start and end values for the pixel row.
                    //Also determine the real float value of the row start and end for accurate interpolation
                    float rowStartFloat = peak.x +
                        (yProgressL > 1f ? LDiff.x :
                        yProgressL < 0 ? 0 :
                        (yProgressL * LDiff.x));
                    int rowStart = (int)rowStartFloat;
                    float rowEndFloat = peak.x +
                        (yProgressR > 1f ? RDiff.x :
                        yProgressR < 0 ? 0 :
                        (yProgressR * RDiff.x));
                    int rowEnd = (int)rowEndFloat;
                    if (rowEnd == targetResolution.x + 1) rowEnd--;
                    int rowXDiff = rowEnd - rowStart;

                    // Get interpolated z value by interpolating both the flatvertex-to-peak vectors by 
                    // the sided y-progress values, then interpolating between those by the x-progress value
                    // The depth interpolation is done with its reciprocal for perspective correct interpolation
                    float zStart = peak.z + (yProgressL * LDiff.z);
                    float zStartReci = peakZReci + (yProgressL * LzReciDiff);
                    float zEnd = peak.z + (yProgressR * RDiff.z);
                    float zEndReci = peakZReci + (yProgressR * RzReciDiff);
                    float zRowDiff = zEnd - zStart;
                    float zReciRowDiff = zEndReci - zStartReci;
                    float rowXFloatDiff = rowEndFloat - rowStartFloat;
                    
                    for (int i = 0; i <= rowXDiff; i++)
                    {
                        int currentPixelX = rowStart + i;
                        if (currentPixelX == targetResolution.x) continue; // Same reason for "continue" here
                        float currentFloatX = currentPixelX + 0.5f;
                        float depthReci;
                        if (rowXFloatDiff == 0)
                        {
                            depthReci = 1/peak.z;
                        }
                        else if (i == 0 && currentFloatX < rowStartFloat) depthReci = zStartReci;
                        else if (i == rowXDiff && currentFloatX > rowEndFloat) depthReci = zEndReci;
                        else
                        {
                            float lerpT = (currentFloatX - rowStartFloat) / rowXFloatDiff;
                            float interpZ = zStart + (lerpT * zRowDiff);
                            depthReci = zStartReci + (lerpT * zReciRowDiff);
                        }
                        float depth = 1 / depthReci;
                        DepthWrite(currentPixelX, yPixelCurrent, depth, diffuse, preCalculatedNormal);
                    }
                }
            }

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
                //The division point also has an reciprocally interpolated depth value.
                float lerpT = (mid.y - top.y) / (bot.y - top.y);
                float divPointX = (int)bot.x == (int)top.x ? bot.x : top.x + ((bot.x - top.x) * lerpT);
                float topZreci = 1 / top.z;
                float divPointDepthReci = topZreci + ((1/bot.z - topZreci) * lerpT);
                Vec3 divPoint = new Vec3(divPointX, mid.y, 1/divPointDepthReci);

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
                return; //If no y- or x- pixel difference, don't draw the triangle
            if (aPixel.y == bPixel.y) Flat(a, b, c);
            else if (aPixel.y == cPixel.y) Flat(a, c, b);
            else if (bPixel.y == cPixel.y) Flat(b, c, a);
            else DivideTriangle();
        }
        //Find centroid of the 3D triangle and get triangle color based on projected screen position and depth
        ColorByte GetTrianglePositionColor(Vec3[] cameraSpaceApices)
        {
            Vec3 centroid3d = ITriangle.Centroid(cameraSpaceApices);
            Vec2int centroidProj = Project(centroid3d);
            byte color_r = (byte)(255 * centroidProj.x / (float)targetResolution.x);
            byte color_g = (byte)(255 * centroidProj.y / (float)targetResolution.y);
            byte color_b = (byte)(255 * Math.Clamp(1f - centroid3d.z / camera.farClipPlaneDistance, 0, 1f));
            return new ColorByte(color_r, color_g, color_b);
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
                   targetResolution.x * ((camera.viewPlaneDistance * v.x / (viewPortSize.x * v.z)) + 0.5f),
                   targetResolution.y * ((camera.viewPlaneDistance * v.y / (viewPortSize.y * v.z)) + 0.5f));
            return framePos;
        }
    }
}
