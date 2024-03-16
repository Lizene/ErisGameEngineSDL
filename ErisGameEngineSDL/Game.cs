using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2;
using ErisGameEngineSDL.ErisMath.Vectors;
using System.Runtime;

namespace ErisGameEngineSDL
{
    internal class Game
    {
        public bool quit = false;
        SDL.SDL_Event e;
        nint window, renderer = IntPtr.Zero;
        Vec2int windowSize, screenSize, halfScreenSize;
        SDL.SDL_FRect square;
        Vec2 squarePos;
        float deltaTime;
        public Game() { }
        public void Start()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            window = IntPtr.Zero;
            SDL.SDL_DisplayMode displayMode;
            SDL.SDL_GetCurrentDisplayMode(0, out displayMode);
            screenSize = new Vec2int(displayMode.w, displayMode.h);
            halfScreenSize = screenSize / 2;
            windowSize = halfScreenSize;
            Vec2int leftTop = halfScreenSize-windowSize / 2;
            window = SDL.SDL_CreateWindow(
                "Eris Game Engine", leftTop.x, leftTop.y, windowSize.x, windowSize.y,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            renderer = SDL.SDL_CreateRenderer(window, -1, 0);

            square = new SDL.SDL_FRect();
            square.w = 40;
            square.h = 40;
            squarePos = Vec2.zero;
        }
        public void Update()
        {
            InputEvents();
            Draw();
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
                }
            }
        }
        void Draw()
        {
            ulong frameStartTime = SDL.SDL_GetTicks64();
            squarePos += Vec2.one*0.1f*deltaTime;
            square.x = squarePos.x; square.y = squarePos.y;
            SDL.SDL_SetRenderDrawColor(renderer, 255,255,255,1);
            SDL.SDL_RenderClear(renderer);
            SDL.SDL_SetRenderDrawColor(renderer, 0, 255, 150, 1);
            SDL.SDL_RenderFillRectF(renderer, ref square);
            SDL.SDL_RenderPresent(renderer);
            deltaTime = SDL.SDL_GetTicks64() - frameStartTime;
        }
        public void Quit()
        {
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }
    }

}
