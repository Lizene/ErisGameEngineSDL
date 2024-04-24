using ErisGameEngineSDL.ErisLibraries;
using ErisMath;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                Vec3[] cameraSpaceVertices = ObjectVerticesToCameraSpace(go);

                //Frustum culling
                if (!worldSpaceFrustum.IsGameObjectPartlyInside(go)) continue;

                //If gameobject completely inside frustum, render triangles as they are.
                IndexTriangle[] indexTriangles = go.transformedMesh.triangles;
                Vec3 camFacingDir = cameraTransform.forward;
                if (worldSpaceFrustum.IsGameObjectCompletelyInside(go))
                {
                    //Rasterize triangles to frame buffer
                    foreach (IndexTriangle triangle in indexTriangles)
                    {
                        //if (Vec3.Dot(camFacingDir, triangle.normal) < 0) continue; // Only render front side of objects
                        Vec3[] apices = triangle.GetApices(cameraSpaceVertices);
                        RasterizeTriangle(apices, triangle.color);
                    }
                    continue;
                }
                //Else, clip triangles first
                ITriangle[] clippedTriangles = cameraSpaceFrustum.ClipTriangles(cameraSpaceVertices, indexTriangles);
                //Rasterize triangles to frame buffer
                foreach (ITriangle triangle in clippedTriangles)
                {
                    Vec3 normal = triangle.GetNormal();
                    //if (Vec3.Dot(camFacingDir, normal) < 0) continue; // Only render front side of objects
                    Vec3[] apices = triangle.GetApices(cameraSpaceVertices);
                    ColorByte color = triangle.GetColor();
                    RasterizeTriangle(apices, color);
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
        void RasterizeTriangle(Vec3[] apices, ColorByte color)
        {
            Vec3 ar = apices[0];
            Vec3 br = apices[1];
            Vec3 cr = apices[2];
            
            Vec2int a = Project(ar);
            Vec2int b = Project(br);
            Vec2int c = Project(cr);


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

            Vec3 centroid3d = ITriangle.Centroid(apices);
            Vec2int centroidProj = Project(centroid3d);
            byte c_r = (byte)(255 * centroidProj.x / (float)targetResolution.x);
            byte c_g = (byte)(255 * centroidProj.y / (float)targetResolution.y);
            byte c_b = (byte)(255*Math.Clamp(1f - centroid3d.z / 20f, 0, 1f));
            ColorByte posColor = new ColorByte(c_r, c_g, c_b);

            void FlatTop(Vec2int topleft, Vec2int topright, Vec2int bottom)
            {
                
                int xDiffL = topleft.x - bottom.x;
                int xDiffR = topright.x - bottom.x ;
                int startY = bottom.y;
                int endY = topleft.y;
                int yDiff = endY - startY;
                if (yDiff <= 0) Debug.Fail("ÖOfej");
                int startX, endX;
                for (int j = 0; j <= yDiff; j++)
                {
                    int currentY = startY + j;
                    if (currentY == targetResolution.y) continue;
                    float progress = j / (float)yDiff;
                    startX = bottom.x + (int)(progress * xDiffL);
                    endX = bottom.x + (int)(progress * xDiffR);
                    for (int i = startX; i < endX; i++)
                    {
                        if (i == targetResolution.x) continue;
                        frameBuffer[i, currentY] = posColor.ToUint();
                    }
                }
            }
            void FlatBottom(Vec2int top, Vec2int bottomleft, Vec2int bottomright)
            {
                //Vec2 top2l = (bottomleft - top).ToFloat();
                //Vec2 top2r = (bottomright - top).ToFloat();
                int xDiffL = bottomleft.x - top.x;
                int xDiffR = bottomright.x - top.x;
                int startY = top.y;
                int endY = bottomleft.y;
                int yDiff = startY - endY;
                if (yDiff <= 0) Debug.Fail("ÖOfej");
                int startX, endX;
                for (int j = 0; j <= yDiff; j++)
                {
                    int currentY = startY - j;
                    if (currentY == targetResolution.y) continue;
                    float progress = j / (float)yDiff;
                    startX = top.x+(int)(progress * xDiffL);
                    endX = top.x+(int)(progress * xDiffR);
                    for (int i = startX; i < endX; i++)
                    {
                        if (i == targetResolution.x) continue;
                        frameBuffer[i, currentY] = posColor.ToUint();
                    }
                }
            }
            void Flat(Vec2int flat1, Vec2int flat2, Vec2int third)
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
            if (a.y == b.y && a.y == c.y) return;
            if (a.y == b.y) Flat(a, b, c);
            else if (a.y == c.y) Flat(a, c, b);
            else if (b.y == c.y) Flat(b, c, a);
            else
            {
                
                //Sort projected vertices by height
                Vec2int[] yo = [a, b, c];
                if (b.y > a.y)
                {
                    yo[0] = b;
                    yo[1] = a;
                }
                if (yo[2].y > yo[1].y)
                {
                    Vec2int temp = yo[1];
                    yo[1] = yo[2];
                    yo[2] = temp;
                }
                if (yo[1].y > yo[0].y)
                {
                    Vec2int temp = yo[0];
                    yo[0] = yo[1];
                    yo[1] = temp;
                }
                //Divide into two triangles
                float divX;
                int xDiff = 0;
                if (yo[2].x == yo[0].x) divX = yo[2].x;
                else
                {
                    float lerpT = (yo[1].y - yo[0].y) / (float)(yo[2].y - yo[0].y);
                    xDiff = yo[2].x - yo[0].x;
                    divX = yo[0].x + (xDiff * lerpT);
                }
                Vec2int divPoint = new Vec2int((int)divX, yo[1].y);
                // DIVPOINT NOT CORRECT
                Vec2int midLeft, midRight;
                if (yo[1].x > divX)
                {
                    midLeft = divPoint;
                    midRight = yo[1];
                }
                else
                {
                    midLeft = yo[1];
                    midRight = divPoint;
                }
                FlatBottom(yo[0], midLeft, midRight);
                FlatTop(midLeft, midRight, yo[2]);
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
