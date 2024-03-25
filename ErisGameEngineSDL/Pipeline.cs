using ErisGameEngineSDL.ErisLibraries;
using ErisLibraries;
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
        static readonly int[] triangleLineIndices = [0, 1, 1, 2, 2, 0];

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
        public Tuple<Vec2int[],int[]> GetGameObjectsLinesPixelPositionsNoFrustumCull(GameObject[] gameObjects)
        {
            invertedCameraRotation = cameraTransform.rotation.inverted();
            List<Vec2int> pixelPositions = new List<Vec2int>();
            List<int> lines = new List<int>();
            foreach (GameObject go in gameObjects)
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
        Tuple<Vec2int[], int[]> GetLinesPixelPositionsNoFrustumCull(GameObject go)
        {
            Vec3[] vertices = go.deformedMesh.vertices;
            bool rotateEveryVertexToCameraRelative = true;
            Vec3 goPos = go.transform.position;
            Vec2int[] goVertexPixelPositions;
            if (rotateEveryVertexToCameraRelative)
            {
                Vec3 relPos = cameraTransform.position - goPos;
                Vec3[] verticesRelPos = vertices.Select(v => relPos + v).ToArray();
                Vec3[] verticesRotated = Quaternion.RotateVectors(verticesRelPos, invertedCameraRotation, camera.transform.rotation);
                goVertexPixelPositions = verticesRotated.Select(RelToFramePos).ToArray();
            }
            else
            {
                Plane viewPlane = camera.worldSpaceFrustum.near;
                Vec3[] verticesWorldPos = vertices.Select(v => goPos + v).ToArray();
                goVertexPixelPositions = vertices.Select(v=>WorldToFramePos(v,viewPlane)).ToArray();
            }
            
            List<int> lines = new List<int>();
            Triangle[] triangles = go.mesh.triangles;
            foreach (Triangle triangle in triangles) 
            {
                int[] indices = triangle.indices;
                int[] triangleLines = triangleLineIndices.Select(i => indices[i]).ToArray();
                lines.AddRange(triangleLines);
            }
            return new Tuple<Vec2int[], int[]>(goVertexPixelPositions, lines.ToArray());
        }
        public Tuple<Vec2int[], int[]> GetGameObjectsLinesPixelPositions(GameObject[] gameObjects)
        {
            invertedCameraRotation = cameraTransform.rotation.inverted();
            List<Vec2int> pixelPositions = new List<Vec2int>();
            List<int> lines = new List<int>();
            foreach (GameObject go in gameObjects)
            {
                if (go == null) continue;
                bool clipMode = false;
                var insideFrustumTuple = GetEachVertexInsideFrustum(go); // 0 = false, 1 = true, 2 = partially
                int isInside = insideFrustumTuple.Item1;
                bool[] isInsideBools = Array.Empty<bool>();
                if (isInside == 0) continue;
                else if (isInside == 2)
                {
                    clipMode = true;
                    isInsideBools = insideFrustumTuple.Item2;
                }
                var pixelPositionsTuple = GetLinesPixelPositions(go, clipMode);
                int count = pixelPositions.Count;
                lines.AddRange(pixelPositionsTuple.Item2.Select(x => x + count));
                pixelPositions.AddRange(pixelPositionsTuple.Item1);
            }
            return new Tuple<Vec2int[], int[]>(pixelPositions.ToArray(), lines.ToArray());
        }
        Tuple<Vec2int[], int[]> GetLinesPixelPositions(GameObject go, bool clipMode)
        {
            Vec3[] vertices = go.deformedMesh.vertices;
            bool rotateEveryVertexToCameraRelative = true;
            Vec3 goPos = go.transform.position;

            Vec2int[] goVertexPixelPositions;
            if (rotateEveryVertexToCameraRelative)
            {
                Vec3 relPos = cameraTransform.position - goPos;
                Vec3[] verticesRelPos = vertices.Select(v => relPos + v).ToArray();
                Vec3[] verticesRotated = Quaternion.RotateVectors(verticesRelPos, invertedCameraRotation, camera.transform.rotation);
                goVertexPixelPositions = verticesRotated.Select(RelToFramePos).ToArray();
            }
            else
            {
                Plane viewPlane = camera.worldSpaceFrustum.near;
                Vec3[] verticesWorldPos = vertices.Select(v => goPos + v).ToArray();
                goVertexPixelPositions = vertices.Select(v => WorldToFramePos(v, viewPlane)).ToArray();
            }
            List<int> lines = new List<int>();
            Triangle[] triangles = go.mesh.triangles;
            foreach (Triangle triangle in triangles)
            {
                int[] indices = triangle.indices;
                int[] triangleLines = triangleLineIndices.Select(i => indices[i]).ToArray();
                lines.AddRange(triangleLines);
            }
            return new Tuple<Vec2int[], int[]>(goVertexPixelPositions, lines.ToArray());
        }
        public uint[,] GetFrameBufferTriangleLines(GameObject[] gameObjects)
        {
            frameBuffer = new uint[targetResolution.x,targetResolution.y];
            depthBuffer = new float[targetResolution.x,targetResolution.y];
            Quaternion camRotation = camera.transform.rotation;
            Quaternion camRotationInverse = camRotation.inverted();
            foreach (GameObject go in gameObjects)
            {
                if (go == null) continue;
                Vec3[] vertices = go.deformedMesh.vertices;
                Vec3 relPos = go.transform.position - cameraTransform.position;
                Vec3[] verticesRelPos = vertices.Select(v => relPos + v).ToArray();
                Vec3[] cameraRelativeVertices = Quaternion.RotateVectors(verticesRelPos, camRotationInverse, camRotation);
                if (!worldSpaceFrustum.IsGameObjectInside(go)) continue;
                //Check if game object is inside frustum completely, inside partially, or not inside.
                
                
                //Get triangles
                List<int> linesList = new List<int>();
                Triangle[] triangles = go.mesh.triangles;
                foreach (Triangle triangle in triangles)
                {
                    int[] indices = triangle.indices;
                    int[] triangleLines = triangleLineIndices.Select(i => indices[i]).ToArray();
                    linesList.AddRange(triangleLines);
                }
                int[] lines = linesList.ToArray();

                //Render lines to frame buffer
                for (int i = 0; i < lines.Length - 1; i += 2)
                {
                    int index1 = lines[i];
                    int index2 = lines[i + 1];
                    Vec3 a = cameraRelativeVertices[index1];
                    Vec3 b = cameraRelativeVertices[index2];
                    /*
                    if (clipMode)
                    {
                        if (!(isInsideBools[index1] || isInsideBools[index2])) continue;
                        else if (isInsideBools[index1] != isInsideBools[index2])
                        {
                            var result2 = camera.cameraSpaceFrustum.ClipSegment(a,b);
                            a = result2.Item1; b = result2.Item2;
                        }
                    }*/
                    if (!(cameraSpaceFrustum.IsPointInside(a) && cameraSpaceFrustum.IsPointInside(b)))
                    {
                        var result2 = cameraSpaceFrustum.ClipSegment(a, b);
                        if (result2 == null) continue;
                        a = result2.Item1; b = result2.Item2;
                    }
                    
                    //Console.WriteLine($"Is inside frustum: {worldSpaceFrustum.IsPointInside(a)}");
                    //Console.WriteLine($"Is inside frustum: {worldSpaceFrustum.IsPointInside(b)}");
                    float zStart = a.z;
                    float zDiff = b.z - a.z;
                    Vec2int frameA = RelToFramePos(a);
                    Vec2int frameB = RelToFramePos(b);

                    Vec2int frameA2B = frameB - frameA;
                    int amountOfPoints = Math.Abs(frameA2B.x)+Math.Abs(frameA2B.y);
                    if (amountOfPoints == 0) amountOfPoints = 1;
                    for (int j = 0; j <= amountOfPoints; j++)
                    {
                        float progress = ((float)j / amountOfPoints);
                        Vec2 frameProgressVector = frameA + frameA2B * progress;
                        float z = zStart + zDiff * progress;
                        Vec2int pixelCoords = new Vec2int((int)MathF.Truncate(frameProgressVector.x), (int)MathF.Truncate(frameProgressVector.y));

                        ColorByte color = new ColorByte(
                            (byte)(255 * pixelCoords.x / (float)targetResolution.x),
                            (byte)(255 * pixelCoords.y / (float)targetResolution.y),
                            (byte)(255 * z / 10));
                        frameBuffer[pixelCoords.x, pixelCoords.y] = color.ToUint();
                    }
                }
            }
            return frameBuffer;
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
        Vec2int WorldToFramePos(Vec3 v, Plane viewPlane)
        {
            return ViewportToFramePos(WorldPosToViewport(v, viewPlane));
        }
        Vec2 RelPosToViewport(Vec3 v)
        {
            float xProj = (camera.viewPlaneDistance-0.1f)*v.x/v.z;
            float yProj = (camera.viewPlaneDistance-0.1f)*v.y/v.z;
            Vec2 viewPortPos = new Vec2(xProj, yProj);
            return viewPortPos;
        }

        Vec2 WorldPosToViewport(Vec3 pos, Plane viewPlane)
        {
            Vec3 intersectionPoint = viewPlane.LineIntersectionPoint(pos, camera.transform.position);
            /*Vec3 rel = intersectionPoint - viewPlane.point;
            float relM = rel.magnitude();
            Vec3 relN = rel/relM;
            double angleRad = Math.Acos(Vec3.Dot(relN, cameraTransform.right));
            if (Vec3.Dot(relN, cameraTransform.up) <0)
            {
                angleRad = -angleRad;
            }
            float x = (float)(relM*Math.Cos(angleRad));
            float y = (float)(relM*Math.Sin(angleRad));*/
            return Vec2.zero;
        }
        Vec2int ViewportToFramePos(Vec2 viewPortPos)
        {
            Vec2int framePos = new Vec2int(
                   (int)(targetResolution.x * (((viewPortPos.x / (viewPortSize.x / 2)) + 1) / 2)),
                   (int)(targetResolution.y * (((viewPortPos.y / (viewPortSize.y / 2)) + 1) / 2)));
            return framePos;
        }
        Tuple<int,bool[]> GetEachVertexInsideFrustum(GameObject go) // Int state is inside, bool array to see if vertex is inside
        {
            bool partiallyInside = false;
            bool completelyInside = true;
            Vec3[] vertices = go.deformedMesh.vertices;
            int lenVerts = vertices.Length;
            bool[] verticesIsInside = new bool[lenVerts];
            Vec3 goPos = go.transform.position;
            for(int i = 0; i < lenVerts; i++)
            {
                Vec3 vertex = vertices[i];
                bool vertexIsInside = worldSpaceFrustum.IsPointInside(goPos+vertex);
                verticesIsInside[i] = vertexIsInside;
                if (vertexIsInside) partiallyInside = true;
                else completelyInside = false;
            }
            int isInsideInt = -1;
            if (!partiallyInside) isInsideInt = 0;
            else if (!completelyInside) isInsideInt = 2;
            else isInsideInt = 1;
            return new Tuple<int, bool[]>(isInsideInt, verticesIsInside);
        }
    }
}
