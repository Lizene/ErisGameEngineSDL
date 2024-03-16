using System;

namespace ErisGameEngineSDL
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Game game = new Game();
            game.Start();
            while (!game.quit)
            {
                game.Update();
            }
            game.Quit();
        }
    }
}
