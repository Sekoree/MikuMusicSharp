using System;
using MikuMusicSharp.BotClass.BotNew;

namespace MikuMusicSharp
{
    class Program
    {
        static void Main()
        {
            using (var b = new Bot())
            {
                b.RunAsync().Wait();
            }
        }
    }
}
