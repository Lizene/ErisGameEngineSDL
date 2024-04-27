﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2;
using ErisMath;
using System.Runtime;
using ErisGameEngineSDL.ErisLibraries;
using ErisLibraries;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using System.Collections;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ErisGameEngineSDL
{
    internal class Game
    {
        //Instance
        public static Game instance;

        //SDL
        SDL.SDL_bool sdltrue = SDL.SDL_bool.SDL_TRUE;
        SDL.SDL_bool sdlfalse = SDL.SDL_bool.SDL_FALSE;

        //Events
        public bool quit = false;
        SDL.SDL_Event SDLEvent;

        //Window
        public IntPtr window, renderer, windowSurface, GL = IntPtr.Zero;
        private nint renderSurface;
        private nint renderTexture;
        private SDL.SDL_Rect renderRect;
        Vec2int windowSize, screenSize, halfScreenSize, targetResolution;
        readonly int resolutionDownScale = 2;
        float resolutionRatio;
        [AllowNull] Pipeline pipeline;


        //Input
        bool inputEnabled = true;
        Vec2int wasdComposite;
        int udComposite;
        float udRot, lrRot;
        Vec2 mouseDelta;
        const float mouseSensitivity = 0.5f;

        //FPS limit
        ushort fpsLimit = 60000;
        uint deltaTimeFloor;
        float deltaTime;
        uint milliSecondCounter = 0;
        uint frameCounter = 0;

        //Cube transformation
        Vec3 cubeRotAxis = new Vec3(0.3f, 1, 0);
        const float cubeAngleSpeed = 50, morphSpeed = 0.5f;
        float morphPhase = 0f;


        //Camera
        [AllowNull] Camera camera;
        [AllowNull] Transform cameraTransform;
        static readonly float cameraMoveSpeed = 10f;

        //Scene
        Shaped3DObject[] sceneGameObjects = [];
        Shaped3DObject[] twoTriangles = [];

        //Drawing
        delegate uint[,] DrawMethodDelegate(Shaped3DObject[] objects);
        DrawMethodDelegate drawMethod = x => new uint[0,0];
        Vec2int windowFit, fitStart;
        Vec2 fitResRatio;
        int pixelXstart, pixelYstart;
        public byte drawModeNum = 2;
        private int resXtimesY;

        public Game()
        {
            if (instance == null) instance = this;
            else Debug.Fail("There cannot be more than one instance of Game");
            instance = this;
        }
        public void Start()
        {
            InitWindow();
            InitTime();
            InitInput();
            CreateObjects();
            SwitchDrawMethod();
        }
        public void Update()
        {
            ulong frameStartTime = SDL.SDL_GetTicks64(); //Get frame start time
            Events();
            UpdateScene();
            Draw();
            uint realDeltaTime = (uint)(SDL.SDL_GetTicks64() - frameStartTime);
            //Calculate deltaTime
            //If exceeds fpslimit, wait until fpslimit is met
            if (realDeltaTime < deltaTimeFloor)
            {
                SDL.SDL_Delay(deltaTimeFloor - realDeltaTime);
                deltaTime = (float)(SDL.SDL_GetTicks64() - frameStartTime)/1000;
            }
            else
            {
                deltaTime = (float)realDeltaTime/1000;
            }
            //Frame counter
            frameCounter++;
            milliSecondCounter += (uint)(SDL.SDL_GetTicks64() - frameStartTime);
            if (milliSecondCounter >= 1000)
            {
                milliSecondCounter -= 1000;
                Console.WriteLine($"FPS: {frameCounter}");
                frameCounter = 0;
            }
        }

        void InitWindow()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_DisplayMode displayMode;
            SDL.SDL_GetCurrentDisplayMode(0, out displayMode);

            screenSize = new Vec2int(displayMode.w, displayMode.h);
            halfScreenSize = screenSize / 2;
            windowSize = halfScreenSize;
            targetResolution = windowSize / resolutionDownScale;
            resolutionRatio = targetResolution.x / (float)targetResolution.y;
            resXtimesY = targetResolution.x * targetResolution.y;
            Vec2int windowPos = halfScreenSize - windowSize / 2;

            window = SDL.SDL_CreateWindow(
                "Eris Game Engine", windowPos.x, windowPos.y, windowSize.x, windowSize.y,
                SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | 
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            renderer = SDL.SDL_CreateRenderer(window, -1, 0);
            windowSurface = SDL.SDL_GetWindowSurface(window);
            renderSurface = SDL.SDL_CreateRGBSurface(0,targetResolution.x,targetResolution.y,1,0,0,0,0);
            renderTexture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGB888, 1, targetResolution.x,targetResolution.y);
            renderRect = new SDL.SDL_Rect();
            renderRect.x = 0; renderRect.y = 0; renderRect.w = targetResolution.x; renderRect.h = targetResolution.y;
            SDL.SDL_RenderSetScale(renderer, resolutionDownScale, resolutionDownScale);

            Vec2 viewPortSize = new Vec2();
            viewPortSize.y = 1f;
            viewPortSize.x = viewPortSize.y * resolutionRatio;
            cameraTransform = new Transform(new Vec3(0,0,-7));
            float FOV = 100f;
            float farClipPlaneDistance = 50f;
            float nearClipPlaneDistance = 0.1f;
            camera = new Camera(cameraTransform, FOV, nearClipPlaneDistance, farClipPlaneDistance, viewPortSize);
            pipeline = new Pipeline(targetResolution, camera);
            ResizeWindow();
            /*
            GL = SDL.SDL_GL_CreateContext(window);
            if (GL == IntPtr.Zero)
            {
                Debug.Fail("OpenGL context could not be created");
                Quit(1);
            }

            
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 6);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, 2);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 0);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);*/
        }
        void InitTime()
        {
            deltaTimeFloor = (uint)(1000d / fpsLimit);
        }
        void InitInput()
        {
            wasdComposite = Vec2int.zero;
            udComposite = 0;
            SDL.SDL_SetWindowInputFocus(window);
            SDL.SDL_SetWindowKeyboardGrab(window, sdltrue);
            SDL.SDL_CaptureMouse(sdltrue);
            SDL.SDL_SetWindowMouseGrab(window, sdltrue);
            SDL.SDL_SetRelativeMouseMode(sdltrue);
        }
        void CreateObjects()
        {
            Vec3 pillarScale = new Vec3(0.7f, 5, 0.7f);
            sceneGameObjects =
            [
                //Make floor
                Shaped3DObject.CreateCube(new Vec3(0,-2,0), new Vec3(11,1,11), ColorByte.Random()),

                //Make cube
                Shaped3DObject.CreateCube(new Vec3(-4,0,4), Vec3.one, ColorByte.Random()),

                //Make rotating cube
                Shaped3DObject.CreateCube(new Vec3(4,1,4), Vec3.one, ColorByte.Random()),

                //Make size-morphing cube
                Shaped3DObject.CreateCube(new Vec3(4,1,-4), Vec3.one, ColorByte.Random()),

                //Make rotating and size-morphing cube
                Shaped3DObject.CreateCube(new Vec3(-4,1,-4), Vec3.one, ColorByte.Random()),

                //Make singular triangle object
                new Shaped3DObject(
                    Mesh.SingleTriangle(ColorByte.Random()),
                    new Transform()),

                // Make pillars
                Shaped3DObject.CreateCube(new Vec3(10,4f,10), pillarScale, ColorByte.Random()),
                Shaped3DObject.CreateCube(new Vec3(-10,4f,10), pillarScale, ColorByte.Random()),
                Shaped3DObject.CreateCube(new Vec3(10,4f,-10), pillarScale, ColorByte.Random()),
                Shaped3DObject.CreateCube(new Vec3(-10,4f,-10), pillarScale, ColorByte.Random()),
                Shaped3DObject.CreateCube(new Vec3(7,4f,10), pillarScale, ColorByte.Random()),
                Shaped3DObject.CreateCube(new Vec3(-9,4f,10), new Vec3(0.3f, 5, 0.7f), ColorByte.Random()),

                // Make windmill
                Shaped3DObject.CreateCube(new Vec3(0,16f,-24), new Vec3(1.3f, 8, 0.3f), ColorByte.Random()),
                //Shaped3DObject.CreateCube(new Vec3(0,16f,-24), Quaternion.Euler(0,0,90), new Vec3(1.3f, 8, 0.3f), ColorByte.Random()),
                Shaped3DObject.CreateCube(new Vec3(0,9f,-26.5f), new Vec3(2, 10, 2), ColorByte.Random()),

                Shaped3DObject.CreateCube(new Vec3(-6.1f,0,4), new Vec3(1,2,1), ColorByte.Random())
                //Make 
            ];
            sceneGameObjects[2].isRotating = true;
            sceneGameObjects[3].isMorphing = true;
            sceneGameObjects[4].isRotating = true;
            sceneGameObjects[4].isMorphing = true;
            //sceneGameObjects[15].isRotating = true;


            twoTriangles = [
                new Shaped3DObject(
                    Mesh.SingleTriangle(ColorByte.BLUE),
                    new Transform(Vec3.zero)),
                new Shaped3DObject(
                    Mesh.SingleTriangle(ColorByte.GREEN),
                    new Transform(new Vec3(0,0,0.7f), Quaternion.Euler(90,0)))
            ];
        }

        void Events()
        {
            mouseDelta = Vec2.zero;
            bool resized = false;
            while (SDL.SDL_PollEvent(out SDLEvent) != 0)
            {
                switch (SDLEvent.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        quit = true;
                        break;
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                        if (SDLEvent.key.repeat != 0 || !inputEnabled) break;
                        switch (SDLEvent.key.keysym.sym)
                        {
                            case SDL.SDL_Keycode.SDLK_ESCAPE:
                                EscapeWindow(); break;
                            case SDL.SDL_Keycode.SDLK_w:
                                wasdComposite.y++; break;
                            case SDL.SDL_Keycode.SDLK_a:
                                wasdComposite.x--; break;
                            case SDL.SDL_Keycode.SDLK_s:
                                wasdComposite.y--; break;
                            case SDL.SDL_Keycode.SDLK_d:
                                wasdComposite.x++; break;
                            case SDL.SDL_Keycode.SDLK_LCTRL:
                                udComposite--; break;
                            case SDL.SDL_Keycode.SDLK_SPACE:
                                udComposite++; break;
                            case SDL.SDL_Keycode.SDLK_1:
                                drawModeNum = 1; SwitchDrawMethod(); break;
                            case SDL.SDL_Keycode.SDLK_2:
                                drawModeNum = 2; SwitchDrawMethod(); break;
                            case SDL.SDL_Keycode.SDLK_3:
                                drawModeNum = 3; SwitchDrawMethod(); break;
                            case SDL.SDL_Keycode.SDLK_4:
                                drawModeNum = 4; SwitchDrawMethod(); DrawClearAndRenderPresent(); break;
                        }
                        break;
                    case SDL.SDL_EventType.SDL_KEYUP:
                        if (!inputEnabled) break;
                        switch (SDLEvent.key.keysym.sym)
                        {
                            case SDL.SDL_Keycode.SDLK_w:
                                wasdComposite.y--; break;
                            case SDL.SDL_Keycode.SDLK_a:
                                wasdComposite.x++; break;
                            case SDL.SDL_Keycode.SDLK_s:
                                wasdComposite.y++; break;
                            case SDL.SDL_Keycode.SDLK_d:
                                wasdComposite.x--; break;
                            case SDL.SDL_Keycode.SDLK_LCTRL:
                                udComposite++; break;
                            case SDL.SDL_Keycode.SDLK_SPACE:
                                udComposite--; break;
                        }
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEMOTION:
                        mouseDelta = new Vec2(SDLEvent.motion.xrel, SDLEvent.motion.yrel);
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                        switch (SDLEvent.button.button)
                        {
                            case (byte)SDL.SDL_BUTTON_LEFT:
                                Console.WriteLine("Click :)");
                                if (!inputEnabled)
                                {
                                    FocusWindow();
                                } break;
                        }
                        break;
                    case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        switch (SDLEvent.window.windowEvent)
                        {
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                                resized = true; break;
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                                resized = true; break;
                        }
                        break;
                }
            }
            if (resized) ResizeWindow();
        }
        void EscapeWindow()
        {
            SDL.SDL_SetWindowKeyboardGrab(window, sdlfalse);
            SDL.SDL_SetWindowMouseGrab(window, sdlfalse);
            SDL.SDL_SetRelativeMouseMode(sdlfalse);
            SDL.SDL_CaptureMouse(sdlfalse);
            inputEnabled = false;
        }
        void FocusWindow()
        {
            SDL.SDL_SetWindowKeyboardGrab(window, sdltrue);
            SDL.SDL_SetWindowMouseGrab(window, sdltrue);
            SDL.SDL_SetRelativeMouseMode(sdltrue);
            SDL.SDL_CaptureMouse(sdltrue);
            inputEnabled = true;
        }
        void ResizeWindow()
        {
            int windowWidth, windowHeight;
            SDL.SDL_GetWindowSize(window, out windowWidth, out windowHeight);
            windowSize = new Vec2int(windowWidth, windowHeight);
            var sizeRatio = new Vec2(windowSize.x / (float)targetResolution.x, windowSize.y / (float)targetResolution.y);
            windowFit = new Vec2int();
            if (sizeRatio.x < sizeRatio.y)
            {
                windowFit.x = windowSize.x;
                windowFit.y = (int)(windowSize.x / resolutionRatio);
            }
            else
            {
                windowFit.y = windowSize.y;
                windowFit.x = (int)(windowSize.y * resolutionRatio);
            }
            fitStart = new Vec2int((windowSize.x - windowFit.x) / 2, (windowSize.y - windowFit.y) / 2);
            pixelXstart = fitStart.x;
            pixelYstart = windowSize.y - fitStart.y;
            fitResRatio = new Vec2(windowFit.x / (float)targetResolution.x, windowFit.y / (float)targetResolution.y);
        }
        void UpdateScene()
        {
            TransformCamera();
            TransformSceneObjects();
            TransformLight();
        }
        void TransformSceneObjects()
        {
            // Rotate Cubes
            float angle = cubeAngleSpeed * deltaTime;
            foreach (Shaped3DObject go in sceneGameObjects)
            {
                if (!go.isRotating) continue;
                go.transform.Rotate(Quaternion.AngleAxis(angle, cubeRotAxis));
            }
            // Rotate windmill
            /*
            sceneGameObjects[12].transform.Rotate(Quaternion.AngleAxis(angle, Vec3.forward));
            sceneGameObjects[13].transform.Rotate(Quaternion.AngleAxis(angle, Vec3.forward));
            */
            // Morph Cubes
            morphPhase += morphSpeed * deltaTime;
            foreach (Shaped3DObject so in sceneGameObjects)
            {
                if (!so.isMorphing) continue;
                Vec3 newScale = (new Vec3((float)Math.Sin(morphPhase),
                    (float)Math.Sin(morphPhase + Constants.rad120),
                    (float)Math.Sin(morphPhase + 2*Constants.rad120))
                    );
                so.transform.SetScale(newScale);
            }
        }
        void TransformCamera()
        {
            if (!inputEnabled) return;
            Vec3 right = cameraTransform.right;
            Vec3 up = Vec3.up;
            Vec3 forward = Vec3.Cross(right, up);
            Vec3 inputVec = new Vec3(wasdComposite.x, udComposite, wasdComposite.y).normalized();
            Vec3 moveDir = (inputVec.x * right + inputVec.y * up + inputVec.z * forward).normalized();
            if (moveDir.magnitude() > 0.01f)
                camera.Move(moveDir * cameraMoveSpeed * deltaTime);

            if (Math.Abs(mouseDelta.x) > 0.1f || Math.Abs(mouseDelta.y) > 0.1f)
            {
                lrRot += mouseDelta.x * mouseSensitivity;
                lrRot %= 360;
                udRot += mouseDelta.y * mouseSensitivity;
                udRot = Math.Clamp(udRot, -89f, 89f);
                Quaternion camRot = Quaternion.Euler(udRot,lrRot,0);
                camera.SetRotation(camRot);
            }
        }
        void TransformLight()
        {
            float yRotation = 25 * deltaTime;
            pipeline.globalLightDir = Quaternion.RotateVector(pipeline.globalLightDir, Quaternion.Euler(0, yRotation));
        }
        void DrawClear()
        {
            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 1);
            SDL.SDL_RenderClear(renderer);
            /*
            //Draw rect to show the area we actually draw on
            SDL.SDL_Rect rect = new SDL.SDL_Rect();
            rect.w = windowFit.x; rect.h = windowFit.y;
            rect.x = fitStart.x; rect.y = fitStart.y;
            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 1);
            SDL.SDL_RenderFillRect(renderer, ref rect);*/
        }
        void DrawClearAndRenderPresent()
        {
            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 1);
            SDL.SDL_RenderClear(renderer);
            SDL.SDL_RenderPresent(renderer);
        }
        void DrawFrameBuffer(uint[,] frameBuffer)
        {
            SDL.SDL_LockTexture(renderTexture, IntPtr.Zero, out nint pixelsPtr, out int pitch);
            unsafe
            {
                uint* pixelData = (uint*)pixelsPtr;
                int iMult = pitch / sizeof(uint);
                for (int i = 0; i < targetResolution.y;  i++)
                {
                    for (int j = 0; j < targetResolution.x; j++)
                    {
                        int index = i * iMult + j;
                        pixelData[index] = frameBuffer[j,targetResolution.y-1-i];
                    }
                }
            }
            SDL.SDL_UnlockTexture(renderTexture);
            SDL.SDL_RenderCopy(renderer, renderTexture, IntPtr.Zero, IntPtr.Zero);
            SDL.SDL_RenderPresent(renderer);
        }
        void DrawFrameBufferPixelByPixel(uint[,] frameBuffer)
        {
            SDL.SDL_RenderClear(renderer);
            for (int j = 0; j < targetResolution.y; j++)
            {
                for (int i = 0; i < targetResolution.x; i++)
                {
                    uint color = frameBuffer[i,j];
                    byte r = (byte)((color >> 16) & 0xFF); // Extract the first byte
                    byte b = (byte)((color >> 8) & 0xFF);  // Extract the second byte
                    byte g = (byte)(color & 0xFF);         // Extract the third byte
                    SDL.SDL_SetRenderDrawColor(renderer, r, g, b, 1);
                    SDL.SDL_RenderDrawPoint(renderer, i, targetResolution.y - 1 - j);
                    SDL.SDL_RenderPresent(renderer);
                }
            }
        }
        public void DrawPixel(int x, int y, ColorByte color)
        {
            SDL.SDL_SetRenderDrawColor(renderer, color.r, color.g, color.b, 1);
            SDL.SDL_RenderDrawPoint(renderer, x, targetResolution.y - 1 - y);
            SDL.SDL_RenderPresent(renderer);
            Thread.Sleep(4);
        }
        void SwitchDrawMethod()
        {
            drawMethod = drawModeNum switch
            {
                1 => pipeline.RenderTriangleSegmentsNoClip,
                2 => pipeline.RenderTriangleSegments,
                3 => pipeline.RenderTriangles,
                4 => pipeline.RenderTriangles,
                _ => x => new uint[0, 0]
            };
        }
        void Draw()
        {
            uint[,] frameBuffer = drawMethod(sceneGameObjects);
            DrawFrameBuffer(frameBuffer);
            
            if (drawModeNum != 4) return;
            Thread.Sleep(500);
            DrawClearAndRenderPresent();
        }
        public void Quit(int exitCode)
        {
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
            Environment.Exit(exitCode);
        }
    }

}
