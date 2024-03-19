﻿using ErisGameEngineSDL.ErisLibraries;
using ErisLibraries;
using ErisMath;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErisGameEngineSDL
{
    internal class Pipeline
    {
        Vec2 viewPortSize, halfViewPortSize;
        float viewPortDistance;
        Vec3[,] frameBuffer;
        float[,] depthBuffer;
        static readonly int[] triangleLineIndexes = [0, 1, 1, 2, 2, 0];

        public Vec2int targetResolution;
        float FOV;
        Transform cameraTransform;
        public Pipeline(Vec2int targetResolution, float FOV, Transform cameraTransform) 
        {
            this.targetResolution = targetResolution;
            this.cameraTransform = cameraTransform;
            this.FOV = FOV;
            frameBuffer = new Vec3[targetResolution.x, targetResolution.y];
            depthBuffer = new float[targetResolution.x, targetResolution.y];
            viewPortSize = Vec2.zero;
            viewPortSize.y = 10;
            float ratio = targetResolution.x / (float)targetResolution.y;
            viewPortSize.x = viewPortSize.y * ratio;
            halfViewPortSize = viewPortSize / 2;
            viewPortDistance = (float)(viewPortSize.x / (2 * Math.Tan(Constants.deg2rad*FOV/2)));
        }
        /*
        public Vec3[,] RenderGameObjects(GameObject[] gameObjects)
        {
            frameBuffer = new Vec3[targetResolution.x, targetResolution.y];
            depthBuffer = new float[targetResolution.x, targetResolution.y];
            foreach (GameObject go in gameObjects)
            {
                if (go == null) continue;
                //WireFrameRender(go);
                continue;
                Tuple<bool,bool> isInside = IsGameObjectInsideFrustum(go);
                if (!isInside.Item1)
                {
                    continue;
                } 
                else if (isInside.Item2)
                {
                    //WireFrameRender(go);
                }
                else
                {
                    ClipRender(go);
                }
            }
            return frameBuffer;
        }*/
        public Tuple<Vec2int[],int[]> RenderGameObjectsWireFrame(GameObject[] gameObjects)
        {
            List<Vec2int> pixelPositions = new List<Vec2int>();
            List<int> lines = new List<int>();
            foreach (GameObject go in gameObjects)
            {
                if (go == null) continue;
                Tuple<Vec2int[], int[]> result = RenderWireFrame(go);
                lines.AddRange(result.Item2.Select(x => x+pixelPositions.Count));
                pixelPositions.AddRange(result.Item1);
            }
            return new Tuple<Vec2int[], int[]>(pixelPositions.ToArray(), lines.ToArray());
        }
        void ClipRender(GameObject go)
        {

        }
        Tuple<Vec2int[], int[]> RenderWireFrame(GameObject go)
        {
            Vec3[] vertices = go.deformedMesh.vertices;
            Vec3 relPos = cameraTransform.position - go.transform.position;
            Vec3[] verticesRelPos= vertices.Select(v => relPos+v).ToArray();
            Vec3[] verticesRotated = Quaternion.RotateVectors(verticesRelPos, cameraTransform.rotation.inverted());
            Vec2int[] goVertexPixelPositions =
                verticesRotated.Select(RelToFramePos).ToArray();
            List<int> lines = new List<int>();
            Triangle[] triangles = go.mesh.triangles;
            foreach (Triangle triangle in triangles) 
            {
                int[] indices = triangle.indices;
                int[] triangleLines = triangleLineIndexes.Select(i => indices[i]).ToArray();
                lines.AddRange(triangleLines);
            }
            return new Tuple<Vec2int[], int[]>(goVertexPixelPositions, lines.ToArray());
        }
        Vec2 RelPosToViewport(Vec3 v)
        {
            float xProj = viewPortDistance*v.x/v.z;
            float yProj = viewPortDistance*v.y/v.z;
            return new Vec2(xProj, yProj);
        }
        Vec2int ViewportToFramePos(Vec2 viewPortPos)
        {
            Vec2int framePos = new Vec2int(
                   (int)Math.Round(targetResolution.x * (viewPortPos.x / viewPortSize.x + 0.5f)),
                   (int)Math.Round(targetResolution.y * (viewPortPos.y / viewPortSize.y + 0.5f)));
            return framePos;
        }
        Vec2int RelToFramePos(Vec3 v)
        {
            return ViewportToFramePos(RelPosToViewport(v));
        }

        Tuple<bool,bool> IsGameObjectInsideFrustum(GameObject go)
        {
            bool partiallyInside = false;
            bool completelyInside = true;
            foreach (Vec3 vertex in go.mesh.vertices)
            {
                if (IsVertexInsideFrustum(vertex))
                {
                    partiallyInside = true;
                }
                else
                {
                    completelyInside = false;
                }
            }
            return new Tuple<bool,bool>(partiallyInside,completelyInside);
        }
        bool IsVertexInsideFrustum(Vec3 vertex)
        {
            return true;
        }
    }
}
