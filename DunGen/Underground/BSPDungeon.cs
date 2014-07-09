using System;
using System.Collections.Generic;
using System.Drawing;



namespace DunGen
{

    class BSPNode
    {
        static int nextID = 0;
        public int myID = 0;

        static int minWidth = 8;
        static int minHeight = 8;

        public Rectangle myRect;
        public int splitType = 0;   // 0=none, 1=vert, 2=horiz
        public int split = 0;
        public Room myRoom = null;
        public BSPNode childA = null;
        public BSPNode childB = null;
        public int myLevel = -1;



        public BSPNode()
        {
            Init(0, 0, 0, 0, -1);
        }

        public BSPNode(int x, int y, int w, int h, int l = -1)
        {
            Init(x, y, w, h, l);
        }

        protected void Init(int x, int y, int w, int h, int level)
        {
            // chance to not split as we get lower
            myLevel = level++;
            int bailChance = 10 * level;

            myID = nextID++;
            //Debug.WriteLine("creating node " + myID + ": " + x + "," + y + " " + w + "," + h);

            myRect = new Rectangle(x, y, w, h);
            childA = childB = null;

            // what kind of split should we do?
            splitType = Util.rnd(2);
            if (((w < (minWidth * 2))) && ((h < (minHeight * 2)))) splitType = 0;
            if ((splitType == 1) && (w < (minWidth * 2))) splitType = 2;
            if ((splitType == 2) && (h < (minHeight * 2))) splitType = 1;
            if (Util.rnd(100) <= bailChance) splitType = 0;
            if (splitType == 0) return;

            // ok do it
            if (splitType == 1)
            {
                split = Util.rnd(w);
                if (split < minWidth) split = minWidth;
                if ((w - split) < minWidth) split -= (minWidth - (w - split));
                //Debug.WriteLine("node " + myID + " is splitting width " + w + " into " + split + " and " + (w - split));

                childA = new BSPNode(x, y, split, h, level);
                childB = new BSPNode(x + split, y, w - split, h, level);
            }
            else
            {
                split = Util.rnd(h);
                if (split < minHeight) split = minHeight;
                if ((h - split) < minHeight) split -= (minHeight - (h - split));
                //Debug.WriteLine("node " + myID + " is splitting height " + h + " into " + split + " and " + (h - split));

                childA = new BSPNode(x, y, w, split, level);
                childB = new BSPNode(x, y + split, w, h - split, level);
            }
        }
    }

    class BSPDungeon : Level
    {

        BSPNode nodeTree = null;

        public override void Build(int w = 0, int h = 0)
        {
            base.Build(w, h);

            mapType = LevelType.NetHack;

            nodeTree = new BSPNode(0, 0, mWidth, mHeight);

            DigTree(nodeTree);

            ConnectRooms(nodeTree);

            ConnectOuterSpaceDoors();

            KillMultiDoors();

            TrimDeadEnds();

            PlaceStairs(roomList[0], true);
            PlaceStairs(roomList[roomList.Count - 1], false);

            // discover some rooms
            DiscoverRoom(roomList[0]);
            //MagicMap();
        }


        protected void DigTree(BSPNode n)
        {
            if (n == null) return;

            /*
            // draw the split for this node
            if (n.splitType == 1)
            {
                int split = n.split + n.myRect.X;
                //Debug.WriteLine("vert split is at " + split);

                // draw the split for this node
                for (int y = n.myRect.Top; y < n.myRect.Bottom; y++)
                {
                    BuildVoid(mapData[split, y]);
                    mapData[split, y].tID = 1;
                    mapData[split, y].sFlags |= SpaceFlags.Mapped;
                }

            }
            else if (n.splitType == 2)
            {
                int split = n.split + n.myRect.Y;
                //Debug.WriteLine("horiz split is at " + split);

                // draw the split for the node
                for (int x = n.myRect.Left; x < n.myRect.Right; x++)
                {
                    BuildVoid(mapData[x, split]);
                    mapData[x, split].tID = 2;
                    mapData[x, split].sFlags |= SpaceFlags.Mapped;
                }
            }
        */

            // childA
            DigTree(n.childA);
            DigTree(n.childB);

            // no children? build a room here
            if ((n.childA == null) && (n.childB == null))
            {
                // remember that we still need a wall around this room

                int maxWidth = n.myRect.Width - 3;
                int maxHeight = n.myRect.Height - 3;
                int roomW = Util.rnd(maxWidth - 4) + 4; // Util.rnd(maxWidth - 4) + 4;     // room size is 5 to maxwidth
                int roomH = Util.rnd(maxHeight - 4) + 4;    // Util.rnd(maxHeight - 4) + 4;    // room size is 5 to maxheight
                int roomX = 2 + n.myRect.X;
                int roomY = 2 + n.myRect.Y;
                if (roomW < maxWidth) roomX += Util.rnd(maxWidth - roomW + 1) - 1;
                if (roomH < maxHeight) roomY += Util.rnd(maxHeight - roomH + 1) - 1;

                /*
                Debug.WriteLine("dig room in node " + n.myID + " at " +
                    roomX + "," + roomY + " " +
                    roomW + "x" + roomH);
                 */

                Room r = new RogueRoom(roomX, roomY, roomW, roomH);
                n.myRoom = r;

                for (int y = roomY; y < (roomY + roomH); y++)
                {
                    for (int x = roomX; x < (roomX + roomW); x++)
                    {
                        if (SpaceExists(x, y))
                        {
                            Pave(x, y, r);
                            mapData[x, y].tID = r.roomID;
                        }
                    }
                }

                WallRoom(r);

                roomList.Add(r);
            }
        }


        protected void ConnectRooms(BSPNode n)
        {
            if (n == null) return;

            ConnectRooms(n.childA);
            ConnectRooms(n.childB);

            // if i have no no children, i'm a room in a dead-end node, so bail
            if ((n.childA == null) && (n.childB == null)) return;

            if ((n.childA.myRoom != null) && (n.childB.myRoom != null))
            {
                // both my children have rooms, link em!
                //Debug.WriteLine("node " + n.myID + " has 2 child rooms");
                ConnectRoom(n.childA.myRoom, n.childB.myRoom);

            }
            else if (n.childA.myRoom != null)
            {
                // my left child has a room, but no room on the right
                //Debug.WriteLine("node " + n.myID + " has a left child room");
                ConnectRoom(n.childA.myRoom, GetRoomL(n.childB));
            }
            else if (n.childB.myRoom != null)
            {
                // my right child has a room, but no room on the left
                //Debug.WriteLine("node " + n.myID + " has a right child room");
                ConnectRoom(GetRoomR(n.childA), n.childB.myRoom);
            }
            else
            {
                // my children don't have rooms
                //Debug.WriteLine("node " + n.myID + " has no child rooms");
                ConnectRoom(GetRoomR(n.childA), GetRoomL(n.childB));
            }

        }

        protected Room GetRoomL(BSPNode n)
        {
            if (n == null) return null;

            Room found = n.myRoom;

            if (found == null) found = GetRoomL(n.childA);
            if (found == null) found = GetRoomL(n.childB);

            return found;
        }

        protected Room GetRoomR(BSPNode n)
        {
            if (n == null) return null;

            Room found = n.myRoom;

            if (found == null) found = GetRoomR(n.childB);
            if (found == null) found = GetRoomR(n.childA);

            return found;
        }


        protected void ConnectRoom(Room sRoom, Room eRoom)
        {
            //Debug.WriteLine("connecting room " + sRoom.roomID + " and " + eRoom.roomID);

            int startX = sRoom.location.X + 1 + Util.rnd(sRoom.location.Width - 2);
            int startY = sRoom.location.Y + 1 + Util.rnd(sRoom.location.Height - 2);
            int endX = eRoom.location.X + 1 + Util.rnd(eRoom.location.Width - 2);
            int endY = eRoom.location.Y + 1 + Util.rnd(eRoom.location.Height - 2);

            //Debug.WriteLine("from " + startX + "," + startY + " to " +
             //   endX + "," + endY);

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

            ((RogueRoom)sRoom).connected = true;
            ((RogueRoom)eRoom).connected = true;
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
           // Debug.WriteLine("want to draw VLine " +
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
                    //if (r != null) return r;
                }
                PaveHall(mapData[x, y]);

                if (sy < ey) y++;
                else y--;
            }
        }

        protected void PaveHall(Space s)
        {
           // Debug.WriteLine("currently at " + s.tx + "," + s.ty);

            // hit a wall? put a door here
            if ((s.sFlags & SpaceFlags.Wall) > 0)
            {
                //Debug.WriteLine("hit wall belonging to room " + s.roomID);
                BuildDoor(s);
            }

            // on floor? ignore
            if ((s.sFlags & SpaceFlags.Floor) > 0) return;

            // put hall here
            PaveCave(s);
        }


        protected void PlaceStairs(Room r, bool up)
        {
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
