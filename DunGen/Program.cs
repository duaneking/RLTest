using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace DunGen
{
    class Program
    {

        static void Main(string[] args)
        {
            while (true)
            {
                Util.CLS();

                Level pLevel = null;
                int roll = Util.rnd(6);

                switch (roll)
                {
                    case 1: pLevel = new Dungeon(); break;
                    case 2: pLevel = new Cavern(); break;
                    case 3: pLevel = new Cavern2(); break;
                    case 4: pLevel = new BSPDungeon(); break;
                    case 5: pLevel = new BigRoom(); break;
                    case 6: pLevel = new CrossHalls(); break;
                    default: break;
                }
                do
                {
                    pLevel.Build(Console.WindowWidth - 1, Console.WindowHeight - 1);
                    pLevel.ConnectStairs();
                } while (pLevel.stairsLinked == false);

                // comment this line to show plain "Line of Sight" instead of showing the whole map
                pLevel.MagicMap();

                // would you like to see the path from upstairs to downstairs?
                bool showStairPath = false;
                pLevel.Display(showStairPath);

                switch (roll)
                {
                    case 1: Util.printf("Nethack", 0, 0); break;
                    case 2: Util.printf("Cavern 1", 0, 0); break;
                    case 3: Util.printf("Cavern 2", 0, 0); break;
                    case 4: Util.printf("BSP", 0, 0); break;
                    case 5: Util.printf("Big Room", 0, 0); break;
                    case 6: Util.printf("Cross", 0, 0); break;
                    default: break;
                }

                Console.ReadLine();
            }
        }

    }
}
