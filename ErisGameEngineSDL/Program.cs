using System;

namespace ErisGameEngineSDL
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Make new game object and call start on it
            Game game = new Game();
            game.Start();
            //Update the game by calling update on the game obect
            while (!game.quit)
            {
                game.Update();
            }
            //Quit program after the gameloop ends
            game.Quit(0);
        }
    }
}
