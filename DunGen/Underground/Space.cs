using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DunGen
{
    public static class SpaceFlags
    {
        public const int None = 0;
        public const int Void = 1;
        public const int Floor = 2;
        public const int Door = 4;
        public const int Cave = 8;
        public const int Wall = 16;
        public const int StairsUp = 32;
        public const int StairsDown = 64;
        public const int StopWalk = 128;
        public const int BlockVis = 256;
        public const int Lit = 512;         // lit spaces pop up when discovered, unlit are by LOS
        public const int Mapped = 1024;     // displayed?
        public const int Explored = 2048;   // have people stood on this space? Used for exploring, etc.
        public const int OnStairRope = 4096;    // is this on the stair-to-stair path?
    }

    public static class DoorFlags
    {
        public const int None = 0;
        public const int Open = 1;
        public const int Closed = 2;
        public const int Locked = 4;
    }


    class Space
    {
        public int tID;
        public int sFlags;
        public int dFlags;  // used only if we allow door states (open, closed, locked)
        public int roomID;

        public int tx, ty;
        public Room myRoom;

        public bool utility;    // for various functions, completely up to the level to use

        public Space()
        {
            tID = 0;
            sFlags = SpaceFlags.Void | SpaceFlags.StopWalk;
            dFlags = DoorFlags.None;
            roomID = -1;
            tx = ty = -1;
            myRoom = null;

            utility = false;
        }
    }
}
