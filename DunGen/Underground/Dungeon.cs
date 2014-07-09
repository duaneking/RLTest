using System;
using System.Collections.Generic;
using System.Drawing;



namespace DunGen
{

    class Dungeon : Level
    {
        protected int maxRooms = 24;

        public override void Build(int w = 0, int h = 0)
        {
            base.Build(w, h);

            mapType = LevelType.NetHack;
            maxRooms = 6 + Util.rnd(8) + Util.rnd(8);// Util.rnd(9) + 15;
            DigRooms();
            
            ConnectRooms();

            ConnectOuterSpaceDoors();

            KillMultiDoors();

            TrimDeadEnds();

            PlaceStairs(roomList[0], true);
            PlaceStairs(roomList[roomList.Count - 1], false);

            // discover some rooms
            DiscoverRoom(roomList[0]);
            //MagicMap();
        }


        protected void PlaceStairs(Room r, bool up) {
            int x = r.location.X + Util.rnd(r.location.Width) - 1;
            int y = r.location.Y + Util.rnd(r.location.Height) - 1;
            if (up == true) mapData[x, y].sFlags |= SpaceFlags.StairsUp;
            else mapData[x, y].sFlags |= SpaceFlags.StairsDown;
        }

        protected void TrimDeadEnds()
        {
            // erase hallways to nowhere
            bool killed = false;

            do
            {
                killed = false;

                for (int y = 0; y < mHeight; y++)
                {
                    for (int x = 0; x < mWidth; x++)
                    {
                        if ((mapData[x, y].sFlags & SpaceFlags.Cave) == 0) continue;

                        int score = 0;
                        if (IsVoid(x, y - 1)) score++;
                        if (IsVoid(x, y + 1)) score++;
                        if (IsVoid(x - 1, y)) score++;
                        if (IsVoid(x + 1, y)) score++;

                        if (score == 3)
                        {
                            // nuke this space
                            //Debug.WriteLine("killing hallway at " + x + "," + y);
                            BuildVoid(x, y);
                            killed = true;
                        }
                    }
                }
            } while (killed == true);


            // kill any doors to outer space
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if (!IsDoor(x, y)) continue;
                    if (IsVoid(x, y - 1) || IsVoid(x, y + 1) || IsVoid(x - 1, y) || IsVoid(x + 1, y))
                    {
                        BuildWall(x, y);

                        // need to fix tile for this wall
                        if (IsVoid(x - 1, y) || IsVoid(x + 1, y)) mapData[x, y].tID = 9474;
                        else mapData[x, y].tID = 9472;
                    }
                }
            }
        }

        protected void ConnectOuterSpaceDoors()
        {
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if (!IsDoor(x, y)) continue;

                    if (IsVoid(x, y - 1)) PaveCave(x, y - 1);
                    if (IsVoid(x, y + 1)) PaveCave(x, y + 1);
                    if (IsVoid(x - 1, y)) PaveCave(x - 1, y);
                    if (IsVoid(x + 1, y)) PaveCave(x + 1, y);
                }
            }
        }

        protected void KillMultiDoors()
        {
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if (!IsDoor(x, y)) continue;

                    // found a door! now start looking right
                    if (IsDoor(x + 1, y))
                    {
                        while (IsDoor(x + 1, y))
                        {
                            // there is a door to my right, kill it!
                            x++;
                            BuildWall(x, y);
                            // need to fix tile for this wall
                            if (IsWall(x - 1, y) || IsWall(x + 1, y)) mapData[x, y].tID = 9472;
                            else mapData[x, y].tID = 9474;
                        }
                        // we fucked with x and y, so start over
                        KillMultiDoors();
                    }

                    // found a door! now start looking down
                    if (IsDoor(x, y + 1))
                    {
                        while (IsDoor(x, y + 1))
                        {
                            // there is a door below me, kill it!
                            y++;
                            BuildWall(x, y);
                            // need to fix tile for this wall
                            if (IsWall(x - 1, y) || IsWall(x + 1, y)) mapData[x, y].tID = 9472;
                            else mapData[x, y].tID = 9474;
                        }
                        // we fucked with x and y, so start over
                        KillMultiDoors();
                    }

                }
            }
        }


        // draw some random (non-overlapping) rooms
        protected void DigRooms()
        {
            int tries = 0;

            while ((roomList.Count < maxRooms) && (tries < 9999))
            {
                tries++;

                Rectangle proposedRoom = new Rectangle(Util.rnd(mWidth - 6) + 3, Util.rnd(mHeight - 6) + 3,
                    Util.rnd(8) + 4, Util.rnd(8) + 4);

                // make sure proposed room isn't off screen
                if (proposedRoom.Right >= mWidth - 1) continue;
                if (proposedRoom.Bottom >= mHeight - 1) continue;

                // does the proposed room intersect any other room?
                bool isValid = true;
                foreach (Room r in roomList)
                {
                    if (proposedRoom.IntersectsWith(
                        new Rectangle(r.location.X - 3, r.location.Y - 3, r.location.Width + 6, r.location.Height + 6))) isValid = false;
                }

                if (!isValid) continue;
                
                // room looks ok, draw it
                DigRoom(proposedRoom.X, proposedRoom.Y, proposedRoom.Width, proposedRoom.Height);
            }
        }


        protected Room DigRoom(int x, int y, int w, int h)
        {
            Room r = new RogueRoom(x, y, w, h);

            for (int yy = y; yy < (y + h); yy++)
            {
                for (int xx = x; xx < (x + w); xx++)
                {
                    if (SpaceExists(xx, yy))
                    {
                        Pave(xx, yy, r);
                    }
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


        protected void ConnectRooms()
        {
            /*
            // this one is good, but always leaves rooms orphaned
            for (int r = 0; r < roomList.Count; r++)
            {
                if (((RogueRoom)roomList[r]).connected == true) continue;

                ((RogueRoom)roomList[r]).connected = true;

                if (r < roomList.Count - 1) DigHall(roomList[r], roomList[r + 1]);
                else DigHall(roomList[r], roomList[0]);
            }
             * */

            /*
            // this one makes way too damn many halls
            for (int r = 0; r < roomList.Count - 1; r++)
            {
                DigHall(roomList[r], roomList[r + 1]);
            }*/

            Room sRoom = roomList[0];
            Room eRoom = null;

            while (true)
            {
                ((RogueRoom)sRoom).connected = true;

                // find the closest non-connected room
                Point2 sCent = sRoom.GetCenter();
                double bestDist = 99999;

                for (int e = 0; e < roomList.Count; e++)
                {
                    Room t = roomList[e];
                    if (((RogueRoom)t).connected) continue;

                    Point2 eCent = t.GetCenter();
                    double dist = Math.Sqrt(Math.Pow(sCent.X - eCent.X, 2) + Math.Pow(sCent.Y - eCent.Y, 2));
                    if (dist < bestDist)
                    {
                        eRoom = t;
                        bestDist = dist;
                    }
                }

                if (eRoom == null)
                {
                    // everything is connected!
                    return;
                }
                else
                {
                    // connect those fuckers
                    DigHall(sRoom, eRoom);

                    // end room becomes new start room
                    sRoom = eRoom;
                    eRoom = null;
                }
            }
        }


        protected void DigHall(Room sRoom, Room eRoom)
        {
            int startX = sRoom.location.X + 1 + Util.rnd(sRoom.location.Width - 3);
            int startY = sRoom.location.Y + 1 + Util.rnd(sRoom.location.Height - 3);
            int endX = eRoom.location.X + 1 + Util.rnd(eRoom.location.Width - 3);
            int endY = eRoom.location.Y + 1 + Util.rnd(eRoom.location.Height - 3);

            //Debug.WriteLine("connecting rooms " + DrawHex(sRoom.roomID) + " and " + DrawHex(eRoom.roomID));
            //Debug.WriteLine(startX + "," + startY + " to " + endX + "," + endY);

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
            //Debug.WriteLine("want to draw HLine " +
            //    ((sx < ex) ? "right" : "left"));

            for (int x = sx; x != ex; )
            {
                if (IsWallCorner(x, y))
                {
                    if (Util.rnd(2) == 1) y--;
                    else y++;
                    // go back a step and pave that spot so the hall corner is complete
                    if (sx < ex) PaveHall(mapData[x - 1, y]);
                    else PaveHall(mapData[x + 1, y]);
                }
                PaveHall(mapData[x, y]);

                if (sx < ex) x++;
                else x--;
            }
        }

        protected void PaveV(int x, int sy, int ey)
        {
            //Debug.WriteLine("want to draw VLine " +
            //    ((sy < ey) ? "down" : "up"));

            for (int y = sy; y != ey; )
            {
                if (IsWallCorner(x, y))
                {
                    if (Util.rnd(2) == 1) x--;
                    else x++;
                    // go back a step and pave that spot so the hall corner is complete
                    if (sy < ey) PaveHall(mapData[x, y - 1]);
                    else PaveHall(mapData[x, y + 1]);
                }
                PaveHall(mapData[x, y]);

                if (sy < ey) y++;
                else y--;
            }
        }

        protected void PaveHall(Space s)
        {
            // hit a wall? put a door here
            if ((s.sFlags & SpaceFlags.Wall) > 0)
            {
                //Debug.WriteLine("hit wall belonging to room " + s.roomID);
                ((RogueRoom)GetRoomByID(s.roomID)).connected = true;
                BuildDoor(s);
            }

            // on floor? ignore
            if ((s.sFlags & SpaceFlags.Floor) > 0) return;

            // put hall here
            s.sFlags = SpaceFlags.Cave;
            s.tID = 3;
        }

        protected Room GetRoomByID(int rID)
        {
            foreach (Room r in roomList)
            {
                if (r.roomID == rID) return r;
            }
            return null;
        }
    }
}
