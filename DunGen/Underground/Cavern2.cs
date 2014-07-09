using System;
using System.Collections.Generic;

namespace DunGen
{
    class Cavern2 : Level
    {
        int percentCells = 40;
        int minRoomSize = 50;
        int numRooms = 0;


        public override void Build(int w = 0, int h = 0)
        {
            // start the map half size
            base.Build(w / 2, h / 2);

            mapType = LevelType.Cavern;

            // seed map
            FirstSeed();

            // run some iterations
            int runs = 20;
            while (runs > 0)
            {
                CellGeneration();
                runs--;
            }
            // run filter 2
            CellGeneration2();

            // now expand the map to 2x size
            ExplodeMap();

            // run filter 2 one more time
            CellGeneration2();

            FixBorder();

            DeclareRooms();

            KillSmallRooms();

            ConnectRooms();

            PlaceStairs((Cave)roomList[0], true);
            PlaceStairs((Cave)roomList[roomList.Count - 1], false);

            FlushRooms();

            // discover some rooms
            //DiscoverRoom(roomList[0]);
            //MagicMap();


            // pretend i'm standing on the upstairs and discover the map
            Point2 s = FindStairs(false);
            DiscoverTiles(s.X, s.Y);


        }


        protected void FirstSeed()
        {

            percentCells = Util.rnd(16) + 39;   // 40 - 55

            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    Pave(x, y, null, false);
                    mapData[x, y].utility = false;

                    if (Util.rnd(100) <= percentCells)
                    {
                        BuildVoid(x, y);
                        mapData[x, y].utility = true;
                    }
                }
            }
        }

        protected void CellGeneration()
        {
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if (IsVoid(x, y))
                    {
                        // there is a cell here
                        if (CountCells(x, y) < 3)
                        {
                            // cell starves
                            mapData[x, y].utility = false;
                        }
                    }
                    else
                    {
                        // there is no cell here
                        if (CountCells(x, y) >= 6)
                        {
                            // new cell is born
                            mapData[x, y].utility = true;
                        }
                    }
                }
            }

            // update the map based on utility
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if ((mapData[x, y].utility == true) && (!IsVoid(x, y))) BuildVoid(x, y);
                    if ((mapData[x, y].utility == false) && (!IsFloor(x, y))) Pave(x, y, null, false);
                }
            }
        }

        protected void CellGeneration2()
        {
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if (IsVoid(x, y))
                    {
                        // there is a cell here
                        if (CountCells(x, y) < 5)
                        {
                            // cell starves
                            mapData[x, y].utility = false;
                        }
                    }
                    else
                    {
                        // there is no cell here
                        if (CountCells(x, y) >= 6)
                        {
                            // new cell is born
                            mapData[x, y].utility = true;
                        }
                    }
                }
            }

            // update the map based on utility
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if ((mapData[x, y].utility == true) && (!IsVoid(x, y))) BuildVoid(x, y);
                    if ((mapData[x, y].utility == false) && (!IsFloor(x, y))) Pave(x, y, null, false);
                }
            }
        }

        protected int CountCells(int x, int y)
        {
            int c = 0;

            for (int yy = y - 1; yy <= y + 1; yy++)
            {
                for (int xx = x - 1; xx <= x + 1; xx++)
                {
                    if ((xx == x) && (yy == y)) continue;

                    if (IsVoid(xx, yy) || !SpaceExists(xx, yy)) c++;

                }
            }
            return c;
        }

        protected void ExplodeMap()
        {
            mWidth *= 2;
            mHeight *= 2;

            Space[,] newMap = new Space[mWidth, mHeight];
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    Space target = new Space();
                    target.tx = x;
                    target.ty = y;
                    newMap[x, y] = target;

                    Space src = mapData[x / 2, y / 2];

                    target.sFlags = src.sFlags;
                    target.tID = src.tID;
                    target.utility = src.utility;
                }
            }

            mapData = newMap;
        }

        protected void FixBorder()
        {
            for (int x = 0; x < mWidth; x++)
            {
                BuildVoid(x, 0);
                BuildVoid(x, mHeight - 1);
            }
            for (int y = 0; y < mHeight; y++)
            {
                BuildVoid(0, y);
                BuildVoid(mWidth - 1, y);
            }
        }

        protected void DeclareRooms()
        {
            int curRoomMarker = 0;
            int curX = -1;
            int curY = -1;

            do
            {
                curX = -1;
                curY = -1;

                // find an unclaimed spot
                for (int y = 0; y < mHeight; y++)
                {
                    for (int x = 0; x < mWidth; x++)
                    {
                        if (IsFloor(x, y) && (mapData[x, y].roomID == -1))
                        {
                            // got one!
                            numRooms++;
                            curX = x;
                            curY = y;

                            x = 999999;
                            y = 999999;
                            break;
                        }
                    }
                }

                // flood fill on this spot
                FloodFill(curX, curY, curRoomMarker);

                curRoomMarker++;

            } while (curX != -1);

            // ok, set up room list
            for (int i = 0; i < numRooms; i++)
            {
                roomList.Add(new Cave());
                roomList[i].Init();
            }

            // how many tiles are in each room?
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if (mapData[x, y].roomID > -1)
                    {
                        Space s = mapData[x, y];
                        s.myRoom = roomList[s.roomID];
                        s.myRoom.numTiles++;
                        ((Cave)s.myRoom).myTiles.Add(s);
                    }
                }
            }
        }


        protected void KillSmallRooms()
        {
            // 10,20,30,40,50
            minRoomSize = Util.rnd(5) * 10;

        killoop:
            foreach (Room r in roomList)
            {

                if (r.numTiles < minRoomSize)
                {
                    for (int y = 0; y < mHeight; y++)
                    {
                        for (int x = 0; x < mWidth; x++)
                        {
                            if (mapData[x, y].myRoom == r)
                            {
                                BuildVoid(x, y);
                            }
                        }
                    }

                    roomList.Remove(r);
                    goto killoop;
                }
            }
        }

        protected void ConnectRooms()
        {
            if (roomList.Count == 1)
            {
                return;
            }

            for (int r = 0; r < roomList.Count - 1; r++)
            {
                Cave startRoom = (Cave)roomList[r];
                Cave endRoom = (Cave)roomList[r + 1];

                // connect these two rooms at their closest point
                double bestDist = 99999;
                Space tileStart = null;
                Space tileEnd = null;

                foreach (Space sStart in startRoom.myTiles)
                {
                    foreach (Space sEnd in endRoom.myTiles)
                    {
                        double dist = Math.Sqrt(Math.Pow(sEnd.tx - sStart.tx, 2) + Math.Pow(sEnd.ty - sStart.ty, 2));
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            tileStart = sStart;
                            tileEnd = sEnd;
                        }
                    }
                }

                // ok we have the tiles, do it
                DigHall(tileStart, tileEnd);
            }
        }

        protected void DigHall(Space sStart, Space sEnd)
        {
            int startX = sStart.tx;
            int startY = sStart.ty;
            int endX = sEnd.tx;
            int endY = sEnd.ty;

            if (startX == endX)
            {
                // vertical line
                //Debug.WriteLine("vertical line");
                PaveV(startX, startY, endY);
            }
            else if (startY == endY)
            {
                // horizontal line
                //Debug.WriteLine("horizontal line");
                PaveH(startY, startX, endX);
            }
            else
            {
                //Debug.WriteLine("forky");

                // split is a vertical line somewhere between startX and endX
                int split = 0;
                if (startX < endX) split = Util.rnd(endX - startX) + startX - 1;
                else split = Util.rnd(startX - endX) + endX - 1;
                //Debug.WriteLine("split is " + split);
                PaveH(startY, startX, split);
                PaveV(split, startY, endY);
                PaveH(endY, split, endX);
            }
        }

        protected void PaveH(int y, int sx, int ex)
        {
            for (int x = sx; x != ex; )
            {
                PaveHall(mapData[x, y]);

                if (sx < ex) x++;
                else x--;
            }
        }

        protected void PaveV(int x, int sy, int ey)
        {
            for (int y = sy; y != ey; )
            {
                PaveHall(mapData[x, y]);

                if (sy < ey) y++;
                else y--;
            }
        }

        protected void PaveHall(Space s)
        {
            // on floor? ignore
            if ((s.sFlags & SpaceFlags.Floor) > 0) return;

            // put hall here
            s.sFlags = SpaceFlags.Cave;
            s.tID = 3;
        }

        protected void FloodFill(int x, int y, int r)
        {
            if (!SpaceExists(x, y)) return;
            if (!IsFloor(x, y)) return;
            if (mapData[x, y].roomID > -1) return;

            // if this spot is unclaimed, claim it
            if (mapData[x, y].roomID == -1)
            {
                mapData[x, y].roomID = r;
                mapData[x, y].tID = r;
            }

            // recurse
            FloodFill(x - 1, y, r);
            FloodFill(x, y - 1, r);
            FloodFill(x + 1, y, r);
            FloodFill(x, y + 1, r);
        }

        protected void FlushRooms()
        {
            foreach (Cave c in roomList)
            {
                c.Flush();
            }
        }

        protected void PlaceStairs(Cave r, bool up)
        {
            Space s = r.myTiles[Util.rnd(r.numTiles) - 1];
            int x = s.tx;
            int y = s.ty;

            // make sure this space is empty
            while (!IsEmpty(x, y))
            {
                s = r.myTiles[Util.rnd(r.numTiles) - 1];
                x = s.tx;
                y = s.ty;
            }

            if (up == true) s.sFlags |= SpaceFlags.StairsUp;
            else s.sFlags |= SpaceFlags.StairsDown;
        }


    }
}
