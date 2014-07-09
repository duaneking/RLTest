using System;
using System.Collections.Generic;
using System.Drawing;

namespace DunGen
{
    class BigRoom : Level
    {

        public override void Build(int w = 0, int h = 0)
        {
            base.Build(w, h);

            mapType = LevelType.NetHack;

            DigRooms();
            
            PlaceStairs();

            // discover some rooms
            //DiscoverRoom(roomList[0]);
            //MagicMap();
            Point2 s = FindStairs(false);
            DiscoverTiles(s.X, s.Y);
        }


        protected void PlaceStairs() {
            int x = Util.rnd(mWidth - 4) + 1;
            int y = Util.rnd(mHeight - 4) + 1;
            mapData[x, y].sFlags |= SpaceFlags.StairsUp;
            
            x = mWidth - x;
            y = mHeight - y;
            mapData[x, y].sFlags |= SpaceFlags.StairsDown;
        }

        // draw some random (non-overlapping) rooms
        protected void DigRooms()
        {
            Rectangle proposedRoom = new Rectangle(1, 1, mWidth - 2, mHeight - 2);

            // room looks ok, draw it
            DigRoom(proposedRoom.X, proposedRoom.Y, proposedRoom.Width, proposedRoom.Height);
        }


        protected Room DigRoom(int x, int y, int w, int h)
        {
            Room r = new RogueRoom(x, y, w, h);

            for (int yy = y; yy < (y + h); yy++)
            {
                for (int xx = x; xx < (x + w); xx++)
                {
                    Pave(xx, yy, r);
                }
            }

            WallRoom(r);

            roomList.Add(r);
            return r;
        }

        protected void WallRoom(Room r)
        {
            /*
             * WALL CODES
             * heavy
             * 9556     9552    9559
             * 9553
             * 9562             9565
             * 
             * light
             * 9484     9472    9488
             * 9474
             * 9492             9496
             * 
             * ?? 9608
             * */
            Rectangle rr = r.location;

            // corners
            BuildWall(rr.X - 1, rr.Y - 1, r, 9484);
            BuildWall(rr.Right, rr.Y - 1, r, 9488);
            BuildWall(rr.X - 1, rr.Bottom, r, 9492);
            BuildWall(rr.Right, rr.Bottom, r, 9496);

            // top and bottom
            for (int x = rr.X; x < rr.Right; x++)
            {
                BuildWall(x, rr.Y - 1, r, 9472);
                BuildWall(x, rr.Bottom, r, 9472);
            }

            // left and right
            for (int y = rr.Y; y < rr.Bottom; y++)
            {
                BuildWall(rr.X - 1, y, r, 9474);
                BuildWall(rr.Right, y, r, 9474);
            }
        }

    }
}
