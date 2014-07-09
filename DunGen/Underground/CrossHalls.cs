using System;
using System.Collections.Generic;

namespace DunGen
{
    class CrossHalls : Level
    {
        int left = 30;
        int top = 10;

        public override void Build(int w = 0, int h = 0)
        {
            base.Build(w, h);

            mapType = LevelType.NetHack;

            DigRoom();

            PlaceStairs(true);
            PlaceStairs(false);

            // discover some rooms
            //DiscoverRoom(roomList[0]);
            //MagicMap();
            Point2 s = FindStairs(false);
            DiscoverTiles(s.X, s.Y);
        }


        protected void PlaceStairs(bool up = true)
        {
            int x = -1;
            int y = -1;

            while (IsStairs(x, y) || !IsFloor(x, y))
            {
                x = Util.rnd(mWidth - 2) + 1;
                y = Util.rnd(mHeight - 2) + 1;
            }

            if (up) mapData[x, y].sFlags |= SpaceFlags.StairsUp;
            else mapData[x, y].sFlags |= SpaceFlags.StairsDown;
        }

        protected Room DigRoom()
        {
            Room r = new RogueRoom();

            left = Util.rnd(40) + 1;
            top = Util.rnd(15) + 1;

            for (int x = left; x < mWidth - left; x++)
            {
                for (int y = 1; y < mHeight - 1; y++)
                {
                    Pave(x, y, r);
                }
            }

            for (int y = top; y < mHeight - top; y++)
            {
                for (int x = 1; x < mWidth - 1; x++)
                {
                    Pave(x, y, r);
                }
            }

            roomList.Add(r);

            WallRoom();

            return r;
        }

        protected void WallRoom()
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

            /*
            // corners
            BuildWall(mapData[rr.X - 1, rr.Y - 1], r, 9484);
            BuildWall(mapData[rr.Right, rr.Y - 1], r, 9488);
            BuildWall(mapData[rr.X - 1, rr.Bottom], r, 9492);
            BuildWall(mapData[rr.Right, rr.Bottom], r, 9496);

            // top and bottom
            for (int x = rr.X; x < rr.Right; x++)
            {
                BuildWall(mapData[x, rr.Y - 1], r, 9472);
                BuildWall(mapData[x, rr.Bottom], r, 9472);
            }

            // left and right
            for (int y = rr.Y; y < rr.Bottom; y++)
            {
                BuildWall(mapData[rr.X - 1, y], r, 9474);
                BuildWall(mapData[rr.Right, y], r, 9474);
            }
             * */

            Room r = roomList[0];

            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if (IsFloor(x, y)) continue;

                    // top walls
                    if (IsFloor(x, y + 1)) BuildWall(x, y, r, 9472);
                    // bottom walls
                    if (IsFloor(x, y - 1)) BuildWall(x, y, r, 9472);
                    // left walls
                    if (IsFloor(x - 1, y)) BuildWall(x, y, r, 9474);
                    // right walls
                    if (IsFloor(x + 1, y)) BuildWall(x, y, r, 9474);

                    // just hard-code the fucking corners
                    BuildWall(left - 1, 0, r, 9484);
                    BuildWall(mWidth - left, 0, r, 9488);
                    BuildWall(mWidth - left, top - 1, r, 9492);
                    BuildWall(mWidth - 1, top - 1, r, 9488);
                    BuildWall(mWidth - 1, mHeight - top, r, 9496);
                    BuildWall(mWidth - left, mHeight - top, r, 9484);
                    BuildWall(mWidth - left, mHeight - 1, r, 9496);
                    BuildWall(left - 1, mHeight - 1, r, 9492);
                    BuildWall(left - 1, mHeight - top, r, 9488);
                    BuildWall(0, mHeight - top, r, 9492);
                    BuildWall(0, top - 1, r, 9484);
                    BuildWall(left - 1, top - 1, r, 9496);
                }
            }
        }

    }
}
