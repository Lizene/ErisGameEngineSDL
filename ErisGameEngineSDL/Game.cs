using System;
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

namespace ErisGameEngineSDL
{
    internal class Game
    {
        //SDL
        SDL.SDL_bool sdltrue = SDL.SDL_bool.SDL_TRUE;
        SDL.SDL_bool sdlfalse = SDL.SDL_bool.SDL_FALSE;

        //Events
        public bool quit = false;
        SDL.SDL_Event e;

        //Window
        public IntPtr window, renderer, windowSurface, GL = IntPtr.Zero;
        private nint renderSurface;
        private nint renderTexture;
        private SDL.SDL_Rect renderRect;
        Vec2int windowSize, screenSize, halfScreenSize, targetResolution;
        readonly int resolutionDownScale = 2;
        float resolutionRatio;
        [AllowNull] Pipeline pipeline;

        //Moving square
        SDL.SDL_FRect square;
        Vec2 squarePos;
        float deltaTime;

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
        uint secondCounter = 0;
        uint frameCounter = 0;

        //Cube rotation
        Vec3 cubeRotAxis;
        const float cubeAngleSpeed = 0;

        //Camera
        [AllowNull] Camera camera;
        [AllowNull] Transform cameraTransform;
        static readonly float cameraMoveSpeed = 10f;

        //Scene
        [AllowNull] List<GameObject> sceneGameObjects;

        //Drawing
        Vec2int windowFit, fitStart;
        Vec2 fitResRatio;
        int pixelXstart, pixelYstart;
        byte drawMode = 0;
        private int resXtimesY;

        public Game() {}
        public void Start()
        {
            InitWindow();
            InitTime();
            InitInput();
            CreateObjects();
        }
        public void Update()
        {
            ulong frameStartTime = SDL.SDL_GetTicks64();
            Events();
            TransformObjects();
            Draw();
            uint realDeltaTime = (uint)(SDL.SDL_GetTicks64() - frameStartTime);
            if (realDeltaTime < deltaTimeFloor)
            {
                SDL.SDL_Delay(deltaTimeFloor - realDeltaTime);
                deltaTime = (float)(SDL.SDL_GetTicks64() - frameStartTime)/1000;
            }
            else
            {
                deltaTime = (float)realDeltaTime/1000;
            }
            frameCounter++;
            //Console.WriteLine($"Delta time: {realDeltaTime}");
            secondCounter += (uint)(SDL.SDL_GetTicks64() - frameStartTime);
            if (secondCounter > 1000)
            {
                secondCounter -= 1000;
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
            square = new SDL.SDL_FRect();
            square.w = 40;
            square.h = 40;
            squarePos = Vec2.one * 400;
            sceneGameObjects = new List<GameObject>();
            GameObject cube = new GameObject(Mesh.Cube(ColorByte.WHITE), 
                new Transform(Vec3.zero, Quaternion.identity));
            sceneGameObjects.Add(cube);
            cubeRotAxis = new Vec3(0.3f, 1, 0);

            GameObject triangleGameObject = new GameObject(Mesh.SingleTriangle(ColorByte.WHITE), new Transform(Vec3.right*5, Quaternion.identity));
            sceneGameObjects.Add(triangleGameObject);
        }

        void Events()
        {
            mouseDelta = Vec2.zero;
            bool resized = false;
            while (SDL.SDL_PollEvent(out e) != 0)
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        quit = true;
                        break;
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                        if (e.key.repeat != 0 || !inputEnabled) break;
                        switch (e.key.keysym.sym)
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
                        }
                        break;
                    case SDL.SDL_EventType.SDL_KEYUP:
                        if (!inputEnabled) break;
                        switch (e.key.keysym.sym)
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
                        mouseDelta = new Vec2(e.motion.xrel, e.motion.yrel);
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                        switch (e.button.button)
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
                        switch (e.window.windowEvent)
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

        void TransformObjects()
        {
            TransformCamera();
            TransformSceneObjects();

            /* Move a square on screen;
            if (!(wasdComposite.magnitude() < 0.01f))
            {
                var newPos = squarePos + wasdComposite.normalized() * 0.2f * deltaTime;
                if (newPos.x >= 0f && newPos.x < windowSize.x - square.w
                    && newPos.y >= square.h && newPos.y < windowSize.y)
                    squarePos = newPos;
            }
            square.x = squarePos.x; square.y = windowSize.y - squarePos.y;
            */
        }
        void TransformSceneObjects()
        {
            // Rotate Cube
            
            foreach (GameObject go in sceneGameObjects)
            {
                float angle = cubeAngleSpeed * deltaTime;
                go.transform.Rotate(Quaternion.AngleAxis(angle, cubeRotAxis));
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
        void DrawGameObjectsAsSDLLine()
        {
            DrawClear();
            var renderResult = pipeline.TriangleLinesSDLDrawLine(sceneGameObjects.ToArray());
            Vec2int[] pixelPositions = renderResult.Item1;
            int[] lines = renderResult.Item2;
            List<SDL.SDL_Point> points = new List<SDL.SDL_Point>();

            for (int i = 0; i < lines.Length; i++)
            {
                var point = new SDL.SDL_Point();
                var pixelPos = pixelPositions[lines[i]];
                point.x = pixelXstart + (int)(pixelPos.x * fitResRatio.x);
                point.y = pixelYstart - (int)(pixelPos.y * fitResRatio.y);
                points.Add(point);
            }
            SDL.SDL_SetRenderDrawColor(renderer, 0, 255, 150, 1);
            SDL.SDL_RenderDrawLines(renderer, points.ToArray(), points.Count);
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
        }

        void Draw()
        {
            SDL.SDL_RenderClear(renderer);
            if (drawMode == 0)
            {
                //DrawGameObjectsAsSDLLine();
                uint[,] frameBuffer = pipeline.RenderTriangleLines(sceneGameObjects.ToArray());
                DrawFrameBuffer(frameBuffer);
            }
            SDL.SDL_RenderPresent(renderer);
        }

        public void Quit(int exitCode)
        {
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
            Environment.Exit(exitCode);
        }
    }

}
