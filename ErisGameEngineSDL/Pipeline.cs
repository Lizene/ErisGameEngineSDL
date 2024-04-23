using ErisGameEngineSDL.ErisLibraries;
using ErisMath;
using SDL2;
using System;
using System.Collections.Generic;
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
                    RasterizeTriangle(apices);
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
        void RasterizeTriangle(Vec3[] apices)
        {
            Vec3 a = apices[0];
            Vec3 b = apices[1];
            Vec3 c = apices[2];


            /*
            RasterizeLine(a, b);
            RasterizeLine(b, c);
            RasterizeLine(c, a);
            */
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
            Vec2int frameA = RelToFramePos(a);
            Vec2int frameA2B = RelToFramePos(b) - frameA;
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
        Vec2int RelToFramePos(Vec3 v)
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
            goVertexPixelPositions = verticesRotated.Select(RelToFramePos).ToArray();
            
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
