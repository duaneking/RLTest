using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


namespace DunGen
{

    // plain old rectangle room
    class Room
    {
        protected static int nextRoomID = 0;
        public int roomID = -1;

        public Rectangle location;
        public int numTiles = 0;


        public virtual void Init() {
            roomID = nextRoomID++;
        }


        public Point2 GetCenter()
        {
            return new Point2((location.Left + location.Right) / 2, (location.Top + location.Bottom) / 2);
        }
    }


    // plain old rogue-style rectangle room
    class RogueRoom : Room
    {
        public bool connected = false;


        public RogueRoom()
        {
            Init(0, 0, 0, 0);
        }

        public RogueRoom(int x, int y, int w, int h)
        {
            Init(x, y, w, h);
        }

        public void Init(int x, int y, int w, int h)
        {
            base.Init();
            location = new Rectangle(x, y, w, h);
        }

    }


    class Cave : Room
    {
        public List<Space> myTiles;

        public Cave()
        {

        }

        public override void Init()
        {
            base.Init();

            myTiles = new List<Space>();
        }

        public void Flush()
        {
            myTiles = null;
        }
    }

}
