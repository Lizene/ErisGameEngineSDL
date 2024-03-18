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
using System.Numerics;

namespace ErisGameEngineSDL
{
    internal class Game
    {
        public bool quit = false;
        SDL.SDL_Event e;
        public nint window, renderer = IntPtr.Zero;
        Vec2int windowSize, screenSize, halfScreenSize;
        
        Pipeline pipeline;
        SDL.SDL_FRect square;
        Vec2 squarePos;
        uint deltaTime;
        Vec2int wasdComposite;

        ushort fpsLimit = 60;
        uint deltaTimeFloor;

        bool wireFrameMode = true;

        List<GameObject> sceneGameObjects;
        public Game() 
        {

        }
        public void Start()
        {
            InitWindow();
            InitTime();
            CreateObjects();
            InitInput();
        }
        public void Update()
        {
            ulong frameStartTime = SDL.SDL_GetTicks64();
            InputEvents();
            TransformObjects();
            Draw();
            uint realDeltaTime = (uint)(SDL.SDL_GetTicks64() - frameStartTime);
            if (realDeltaTime < deltaTimeFloor)
            {
                SDL.SDL_Delay(deltaTimeFloor - realDeltaTime);
                deltaTime = (uint)(SDL.SDL_GetTicks64() - frameStartTime);
            }
            else
            {
                deltaTime = realDeltaTime;
            }
        }
        void CreateObjects()
        {
            square = new SDL.SDL_FRect();
            square.w = 40;
            square.h = 40;
            squarePos = Vec2.one * 400;
            sceneGameObjects = new List<GameObject>();
            GameObject cube = new GameObject(Mesh.Cube(Vec3.one), new Transform());
            sceneGameObjects.Add(cube);
            Triangle[] triangles = sceneGameObjects[0].mesh.triangles;
        }
        void InitWindow()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            window = IntPtr.Zero;
            SDL.SDL_DisplayMode displayMode;
            SDL.SDL_GetCurrentDisplayMode(0, out displayMode);
            screenSize = new Vec2int(displayMode.w, displayMode.h);
            Vec2int targetResolution = screenSize / 2;
            Console.WriteLine(targetResolution);
            pipeline = new Pipeline(targetResolution, 80);
            halfScreenSize = screenSize / 2;
            windowSize = halfScreenSize;
            Vec2int leftTop = halfScreenSize - windowSize / 2;
            window = SDL.SDL_CreateWindow(
                "Eris Game Engine", leftTop.x, leftTop.y, windowSize.x, windowSize.y,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            renderer = SDL.SDL_CreateRenderer(window, -1, 0);
        }
        void InitTime()
        {
            deltaTimeFloor = (uint)(1000d / fpsLimit);
        }
        void InitInput()
        {
            wasdComposite = Vec2int.zero;
        }
        void InputEvents()
        {
            while (SDL.SDL_PollEvent(out e) != 0)
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        quit = true;
                        break;
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                        if (e.key.repeat != 0) break;
                        switch (e.key.keysym.sym)
                        {
                            case SDL.SDL_Keycode.SDLK_w:
                                wasdComposite.y++; break;
                            case SDL.SDL_Keycode.SDLK_a:
                                wasdComposite.x--; break;
                            case SDL.SDL_Keycode.SDLK_s:
                                wasdComposite.y--; break;
                            case SDL.SDL_Keycode.SDLK_d:
                                wasdComposite.x++; break;
                        }
                        break;
                    case SDL.SDL_EventType.SDL_KEYUP:
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
                        }
                        break;
                }
            }
        }
        void TransformObjects()
        {
            if (!(wasdComposite.magnitude() < 0.01f))
            {
                var newPos = squarePos + wasdComposite.normalized() * 0.2f * deltaTime;
                if (newPos.x >= 0f && newPos.x < windowSize.x - square.w
                    && newPos.y >= square.h && newPos.y < windowSize.y)
                    squarePos = newPos;
            }
            square.x = squarePos.x; square.y = windowSize.y - squarePos.y;
        }
        void Draw()
        {
            Vec2int res = pipeline.targetResolution;
            float resRatio = res.x / (float)res.y;
            int windowWidth, windowHeight;
            SDL.SDL_GetWindowSize(window, out windowWidth, out windowHeight);
            windowSize = new Vec2int(windowWidth, windowHeight);
            var sizeRatio = new Vec2(windowSize.x / (float)res.x, windowSize.y / (float)res.y);
            Vec2int windowFit = new Vec2int();
            if (sizeRatio.x < sizeRatio.y)
            {
                windowFit.x = windowSize.x;
                windowFit.y = (int)(windowSize.x / resRatio);
            }
            else
            {
                windowFit.y = windowSize.y;
                windowFit.x = (int)(windowSize.y * resRatio);
            }
            Console.WriteLine($"{sizeRatio}, {windowFit}");
            var halfRes = res / 2;
            Vec2 ratio = new Vec2(windowSize.x / (float)res.x, windowSize.y / (float)res.y);

            SDL.SDL_SetRenderDrawColor(renderer, 255,255,255,1);
            SDL.SDL_RenderClear(renderer);
            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 1);
            SDL.SDL_Rect rect = new SDL.SDL_Rect();
            
            rect.w = windowFit.x; rect.h = windowFit.y;
            Vec2int fitStart = new Vec2int((windowSize.x - windowFit.x) / 2, (windowSize.y - windowFit.y) / 2);
            rect.x = fitStart.x; rect.y = fitStart.y;
            /*
            rect.w = 100; rect.h = 50;
            rect.x = windowSize.x/ 2; rect.y = windowSize.y / 2;
            */
            SDL.SDL_RenderFillRect(renderer, ref rect);
            SDL.SDL_SetRenderDrawColor(renderer, 0, 255, 150, 1);
            if (wireFrameMode)
            {
                var renderResult = pipeline.RenderGameObjectsWireFrame(sceneGameObjects.ToArray());
                Vec2int[] pixelPositions = renderResult.Item1;
                foreach ( var pixelPosition in pixelPositions)
                {
                    //Console.WriteLine(pixelPosition);
                }
                int[] lines = renderResult.Item2;
                List<SDL.SDL_Point> points = new List<SDL.SDL_Point>();
                
                for (int i = 0; i < lines.Length; i ++)
                {
                    var point = new SDL.SDL_Point();
                    var pixelPos = pixelPositions[lines[i]];
                    point.x = fitStart.x+(int)((pixelPos.x / (float)res.x)*windowFit.x);
                    point.y = windowSize.y-(fitStart.y+(int)((pixelPos.y/(float)res.y)*windowFit.y));
                    points.Add(point);
                    Console.WriteLine($"{point.x}, {point.y}");
                }
                SDL.SDL_RenderDrawLines(renderer, points.ToArray(), points.Count);
            }
            /*
            else
            {
                Vec3[,] frameBuffer = pipeline.RenderGameObjects(sceneGameObjects.ToArray());
                for (int i = 0; i < frameBuffer.GetLength(0); i++)
                {
                    for (int j = 0; j < frameBuffer.GetLength(1); j++)
                    {
                        //Vec3int pixelColor = frameBuffer[i, j];
                        //SDL.SDL_SetRenderDrawColor(renderer,
                        //SDL.SDL_RenderDrawPoints frameBuffer[i, j];
                    }
                }
            }
            //SDL.SDL_RenderFillRectF(renderer, ref square);*/
            SDL.SDL_RenderPresent(renderer);
        }
        public void Quit()
        {
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }
    }

}
