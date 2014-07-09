#define GOODRND

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;


namespace DunGen
{
    public static class Util
    {
        // CONSOLE STUFF
        public static void printf(string s)
        {
            Console.WriteLine(s);
        }

        // print a string at a given coord on the screen
        public static void printf(string s, int c, int r)
        {
            Console.SetCursorPosition(c, r);
            Console.Write(s);
        }

        // this is essentially sprintf() from c++
        public static void printf(ref string oStr, string s)
        {
            oStr += s + Environment.NewLine;
        }

        // VIRTUAL CONSOLE
        public const int conSize = 28;
        public static string[] bufLines = new string[conSize];
        public static void ConOut(string s)
        {
            Console.SetCursorPosition(0, Console.WindowHeight - conSize);

            for (int x = (conSize - 1); x >= 1; x--)
            {
                bufLines[x] = bufLines[x - 1];
                if (bufLines[x] == null) bufLines[x] = "";
                Console.WriteLine(bufLines[x].PadRight(Console.WindowWidth-1));
            }
            bufLines[0] = s;
            Console.Write(s.PadRight(Console.WindowWidth-1));
        }

        // clear the screen
        public static void CLS(int c = 120, int r = 40)
        {
            Console.SetBufferSize(c, r);
            Console.SetWindowSize(c, r);
            Console.Clear();
        }


        // MATH STUFF

#if GOODRND

        private static readonly RNGCryptoServiceProvider _generator = new RNGCryptoServiceProvider();
        private static int _rndBetween(int minValue, int maxValue)
        {
            // get some random chars
            byte[] chars = new byte[4];
            _generator.GetBytes(chars);
            // convert them to a double
            double dubVal = (double)BitConverter.ToUInt32(chars, 0) / UInt32.MaxValue;
            // fit that to the int range
            int range = maxValue - minValue + 1;
            double randInRange = Math.Floor(dubVal * range);
            return (int)(minValue + randInRange);
        }
        public static int rnd(int x)
        {
            if (x > 1) return _rndBetween(1, x);
            else return x;
        }

#else
     
        private static Random r = new Random();
        public static int rnd(int x)
        {
            if (x>1) return r.next(x) +1;
            else return x;
        }

#endif
    }

}
