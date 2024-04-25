using ErisGameEngineSDL.ErisLibraries;
using ErisMath;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ErisGameEngineSDL
{
    internal class Pipeline
    {
        Vec2 viewPortSize, halfViewPortSize;
        float viewPortDistance;
        uint[,] frameBuffer;
        float[,] depthBuffer;
        static readonly ushort[] triangleLineIndices = [0, 1, 1, 2, 2, 0];

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
        
        public uint[,] RenderTriangleLinesNoClip(Shaped3DObject[] gameObjects)
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

                int[] lines = GetLinesFromIndexTriangles(go.transformedMesh.triangles);
                FrustumClipAndRasterizeLines(lines, cameraSpaceVertices);
            }
            return frameBuffer;
        }
        public uint[,] RenderTriangleLines(Shaped3DObject[] gameObjects)
        {
            frameBuffer = new uint[targetResolution.x, targetResolution.y];
            depthBuffer = new float[targetResolution.x, targetResolution.y];
            cameraRotation = camera.transform.rotation;
            invertedCameraRotation = cameraRotation.inverted();
            foreach (Shaped3DObject go in gameObjects)
            {
                if (go == null) continue;
                //transformed vertices from object space to camera space
                Vec3[] cameraSpaceVertices = ObjectVerticesToCameraSpace(go);

                //Frustum culling
                if (!worldSpaceFrustum.IsGameObjectPartlyInside(go)) continue;

                //If gameobject completely inside frustum, render triangle lines as they are.
                IndexTriangle[] indexTriangles = go.transformedMesh.triangles;
                if (worldSpaceFrustum.IsGameObjectCompletelyInside(go))
                {
                    RasterizeLines(GetLinesFromIndexTriangles(indexTriangles), cameraSpaceVertices);
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
                    RasterizeLine(a, b);
                    RasterizeLine(b, c);
                    RasterizeLine(c, a);
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
        
        int[] GetLinesFromIndexTriangles(IndexTriangle[] triangles)
        {
            List<int> lines = new List<int>();
            foreach (IndexTriangle triangle in triangles)
            {
                int[] indices = triangle.indices;
                int[] triangleLines = triangleLineIndices.Select(i => indices[i]).ToArray();
                lines.AddRange(triangleLines);
            }
            return lines.ToArray();
        }
        Vec3[] ObjectVerticesToCameraSpace(Shaped3DObject go)
        {
            Vec3[] vertices = go.transformedMesh.vertices;
            Vec3 relPos = go.transform.position - cameraTransform.position;
            Vec3[] verticesRelPos = vertices.Select(v => relPos + v).ToArray();
            return Quaternion.RotateVectors(verticesRelPos, invertedCameraRotation, cameraRotation);
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

            //Find centroid of the 3D triangle and set triangle color based on resolution position and depth
            Vec3 centroid3d = ITriangle.Centroid(apices);
            Vec2int centroidProj = Project(centroid3d);
            byte color_r = (byte)(255 * centroidProj.x / (float)targetResolution.x);
            byte color_g = (byte)(255 * centroidProj.y / (float)targetResolution.y);
            byte color_b = (byte)(255*Math.Clamp(1f - centroid3d.z / camera.farClipPlaneDistance, 0, 1f));
            ColorByte posColor = new ColorByte(color_r, color_g, color_b);
            //My methods involve taking the vertex with the different y value,
            //taking the vectors from that vertex to the flat left and right vertices,
            //and multiplying them by the y-progress to get the start x and end x for the x-row.
            
            //Method for rasterizing a projected triangle with a flat top
            void FlatTop(Vec3 topleft, Vec3 topright, Vec3 bottom)
            {
                
                float xDiffL = topleft.x - bottom.x;
                float xDiffR = topright.x - bottom.x;
                float zDiffL = topleft.z - bottom.z;
                float zDiffR = topright.z - bottom.z;
                int yStart = (int)Math.Floor(bottom.y);
                if (yStart == -1) yStart++;
                int yDiff = (int)topleft.y - yStart;
                if (yDiff <= 0) Debug.Fail("yDiff should always be positive");
                bool isTopLeft = xDiffL > 0;
                for (int j = 0; j <= yDiff; j++) //comparison might be wrong
                {
                    int yCurrent = yStart + j; //Screen pixelheight of the current row
                    if (yCurrent == targetResolution.y) continue; 
                    // "continue" because it should cause an error when it's higher than the resolution y,
                    // because that means there's something wrong with the code.
                    // It can skip if and only if it's exactly the same as the resolution y

                    //Get percent progress of the y-loop and multiply by
                    //x differences to the left and right to get the start and end x of the row
                    float yProgress = j / (float)yDiff;
                    RasterizeRow(bottom.x, bottom.z, isTopLeft, xDiffL, xDiffR, zDiffL, zDiffR, yCurrent, yProgress);
                }
            }

            //Method for rasterizing a projected triangle with a flat bottom
            //Same as FlatTop but instead of going from bottom to top, this one goes from top to bottom
            void FlatBottom(Vec3 top, Vec3 bottomleft, Vec3 bottomright)
            {
                
                float xDiffL = bottomleft.x - top.x;
                float xDiffR = bottomright.x - top.x;
                float zDiffL = bottomleft.z - top.z;
                float zDiffR = bottomright.z - top.z;
                int yStart = (int)top.y;
                if (yStart == -1) yStart++;
                int yDiff = yStart - (int)bottomleft.y;
                if (yDiff <= 0) Debug.Fail("yDiff should always be positive");
                bool isTopLeft = xDiffL < 0;
                for (int j = 0; j <= yDiff; j++)
                {
                    int yCurrent = yStart - j;
                    if (yCurrent == targetResolution.y) continue;
                    float yProgress = j / (float)yDiff;
                    RasterizeRow(top.x, top.z, isTopLeft, xDiffL, xDiffR, zDiffL, zDiffR, yCurrent, yProgress);
                }
            }

            //Method for rasterizing a row in the forloop of the triangle y-value.
            //I differentiated this from FlatTop() and FlatBottom() because it's the same in both
            void RasterizeRow(float thirdVertexX, float thirdVertexZ, bool isTopLeft, float xDiffL, float xDiffR, float zDiffL, float zDiffR, int yCurrent, float yProgress)
            {
                int rowStart = (int)(Math.Floor(thirdVertexX) + (yProgress * xDiffL));
                if (rowStart == -1) rowStart++;
                int rowEnd = (int)(Math.Floor(thirdVertexX) + (yProgress * xDiffR));
                if (rowEnd == targetResolution.x + 1) rowEnd--;
                int rowXDiff = rowEnd - rowStart;
                //Third vertex point
                if (rowXDiff == 0)
                {
                    if (rowStart == targetResolution.x || yCurrent == targetResolution.y) return;
                    DepthWrite(rowStart, yCurrent, thirdVertexZ, diffuse, preCalculatedNormal);
                    return;
                }
                //Get interpolated z value by interpolating both the bottom to top vectors by the y-progress value,
                //then interpolating between those by the x-progress value
                float zStart = thirdVertexZ + (yProgress * zDiffL);
                float zEnd = thirdVertexZ + (yProgress * zDiffR);
                float zRowDiff = zEnd - zStart;
                for (int i = 0; i <= rowXDiff; i++) //comparison might be wrong
                {
                    int xCurrent = rowStart + i;
                    if (xCurrent == targetResolution.x) continue; // Same reason for "continue" here

                    float xProgress = i / (float)rowXDiff;
                    float depth = zStart + (xProgress * zRowDiff);
                    //Console.WriteLine(depth);
                    DepthWrite(xCurrent, yCurrent, depth, diffuse, preCalculatedNormal);
                }
            }

            //Method for determining order of vertices when two of the y-values are the same
            void Flat(Vec3 flat1, Vec3 flat2, Vec3 third)
            {
                if (third.y < flat1.y)
                {
                    if (flat1.x < flat2.x) FlatTop(flat1, flat2, third);
                    else FlatTop(flat2, flat1, third);
                }
                else
                {
                    if (flat1.x < flat2.x) FlatBottom(third, flat1, flat2);
                    else FlatBottom(third, flat2, flat1);
                }
            }

            //Check for a flat top or bottom, if doens't have,
            //divide into two triangles with a flat bottom and flat top
            if ((aPixel.y == bPixel.y && aPixel.y == cPixel.y) || (aPixel.x == bPixel.x && aPixel.x == cPixel.x)) 
                return; //If no y- or x- difference, don't draw the triangle
            if (aPixel.y == bPixel.y) Flat(a, b, c);
            else if (aPixel.y == cPixel.y) Flat(a, c, b);
            else if (bPixel.y == cPixel.y) Flat(b, c, a);
            else
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
                
                //Divide into two triangles with a divpoint with the same height as the
                //middle vertex and an interpolated x and depth value
                float lerpT = (mid.y - top.y) / (bot.y - top.y);
                float divPointX = (int)bot.x == (int)top.x ? bot.x : top.x + ((bot.x-top.x) * lerpT);
                float divPointDepth = top.z + ((bot.z - top.z) * lerpT);
                Vec3 divPoint = new Vec3(divPointX, mid.y, divPointDepth); //depth not correct

                //Sort mid and divPoint by x-value
                Vec3 midLeft, midRight;
                if (mid.x > divPointX) { midLeft = divPoint; midRight = mid; }
                else { midLeft = mid; midRight = divPoint; }

                FlatBottom(top, midLeft, midRight);
                FlatTop(midLeft, midRight, bot);
            }
        }
        void DepthWrite(int pixelPosX, int pixelPosY, float depth, ColorByte color, Vec3 normal)
        {
            if (depth is float.NaN) Debug.Fail("NAN");
            float depthBufferValue = depthBuffer[pixelPosX, pixelPosY];
            if (depthBufferValue == 0 || depth <= depthBufferValue)
            {
                depthBuffer[pixelPosX, pixelPosY] = depth;
                
                frameBuffer[pixelPosX, pixelPosY] = color.ToUint();
            }
        }
        void FrustumClipAndRasterizeLines(int[] lines, Vec3[] cameraSpaceVertices)
        {
            for (int i = 0; i < lines.Length - 1; i += 2)
            {
                int index1 = lines[i];
                int index2 = lines[i + 1];
                Vec3 a = cameraSpaceVertices[index1];
                Vec3 b = cameraSpaceVertices[index2];

                // Frustum clipping
                if (!(cameraSpaceFrustum.IsPointInside(a) && cameraSpaceFrustum.IsPointInside(b)))
                {
                    var result2 = cameraSpaceFrustum.ClipSegment(a, b);
                    if (result2 == null) continue;
                    a = result2.Item1; b = result2.Item2;
                }
                RasterizeLine(a, b);
            }
        }
        void RasterizeLines(int[] lines, Vec3[] cameraSpaceVertices)
        {
            for (int i = 0; i < lines.Length - 1; i += 2)
            {
                int index1 = lines[i];
                int index2 = lines[i + 1];
                Vec3 a = cameraSpaceVertices[index1];
                Vec3 b = cameraSpaceVertices[index2];
                RasterizeLine(a, b);
            }
        }
        void RasterizeLine(Vec3 a, Vec3 b)
        {
            // Draw line with depth to frame buffer
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
        public Tuple<Vec2int[],int[]> TriangleLinesSDLDrawLine(Shaped3DObject[] gameObjects)
        {
            invertedCameraRotation = cameraTransform.rotation.inverted();
            List<Vec2int> pixelPositions = new List<Vec2int>();
            List<int> lines = new List<int>();
            foreach (Shaped3DObject go in gameObjects)
            {
                if (go == null) continue;
                Tuple<Vec2int[], int[]> result = new Tuple<Vec2int[], int[]>(new Vec2int[1], new int[1]);
                int performanceTest = 1;
                for (int i = 0; i < performanceTest; i++)
                {
                    result = GetLinesPixelPositionsNoFrustumCull(go);
                }
                int count = pixelPositions.Count;
                lines.AddRange(result.Item2.Select(x => x+count));
                pixelPositions.AddRange(result.Item1);
            }
            return new Tuple<Vec2int[], int[]>(pixelPositions.ToArray(), lines.ToArray());
        }
        Tuple<Vec2int[], int[]> GetLinesPixelPositionsNoFrustumCull(Shaped3DObject go)
        {
            Vec3[] vertices = go.transformedMesh.vertices;
            Vec3 goPos = go.transform.position;
            Vec2int[] goVertexPixelPositions;
            Vec3 relPos = cameraTransform.position - goPos;
            Vec3[] verticesRelPos = vertices.Select(v => relPos + v).ToArray();
            Vec3[] verticesRotated = Quaternion.RotateVectors(verticesRelPos, invertedCameraRotation, camera.transform.rotation);
            goVertexPixelPositions = verticesRotated.Select(Project).ToArray();
            
            List<int> lines = new List<int>();
            IndexTriangle[] triangles = go.mesh.triangles;
            foreach (IndexTriangle triangle in triangles) 
            {
                int[] indices = triangle.indices;
                int[] triangleLines = triangleLineIndices.Select(i => indices[i]).ToArray();
                lines.AddRange(triangleLines);
            }
            return new Tuple<Vec2int[], int[]>(goVertexPixelPositions, lines.ToArray());
        }
        
    }
}
