using System;
using System.Collections.Generic;
using System.Linq;

namespace DunGen
{
    public static class LevelType
    {
        public const int None = 0;
        public const int Town = 1;
        public const int NetHack = 2;
        public const int Cavern = 3;
    }


    class Level
    {
        // size of map, or at least the part we're using
        protected int mWidth = 40;
        protected int mHeight = 40;

        protected int mapType = LevelType.None;
        public Space[,] mapData;
        protected List<Room> roomList = null;
        public bool fullyExplored = false;
        
        // pathfinding stuff
        Point2 stairsUp = null;
        Point2 stairsDown = null;
        PathFinder pf = null;
        public bool stairsLinked = false;                       // is there a path from upstairs to downstairs? (SHOULD always be true)
        protected List<PathFinderNode> stairsLinkPath = null;   // this is the path
        
        // list of tiles that PC's need to go investigate
        // essentially they've been seen but not stood on. This should open up more tiles to be seen, etc.
        protected Queue<Space> spotsToExplore = null;


        public virtual void Build(int w = 0, int h = 0)
        {
            roomList = new List<Room>();
            spotsToExplore = new Queue<Space>();

            if (w > 0) mWidth = w;
            if (h > 0) mHeight = h;

            mapData = new Space[mWidth, mHeight];
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    mapData[x, y] = new Space();
                    mapData[x, y].tx = x;
                    mapData[x, y].ty = y;
                }
            }
        }

        protected void DiscoverRoom(Room r)
        {
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if (mapData[x, y].myRoom == r)
                    {
                        mapData[x, y].sFlags |= SpaceFlags.Mapped;
                    }
                }
            }
        }

        public void MagicMap()
        {
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if (!IsVoid(x,y))
                    {
                        mapData[x, y].sFlags |= SpaceFlags.Mapped;
                    }
                }
            }
        }
    
        // LIGHTING AND LINE OF SIGHT
        protected void LightLOS(int startX, int startY, int endX, int endY, int maxRange)
        {
            bool steep = Math.Abs(endY - startY) > Math.Abs(endX - startX);

            if (steep == true)
            {
                startX ^= startY;
                startY ^= startX;
                startX ^= startY;
                endX ^= endY;
                endY ^= endX;
                endX ^= endY;
            }
            int deltaX = Math.Abs(endX - startX);
            int deltaY = Math.Abs(endY - startY);
            int error = 0;
            int deltaError = deltaY;
            int yStep = 0;
            int xStep = 0;
            int y = startY;
            int x = startX;

            if (startY < endY) yStep = 1;
            else yStep = -1;
            if (startX < endX) xStep = 1;
            else xStep = -1;

            int tmpX = 0;
            int tmpY = 0;
            int r = 0;

            while (x != endX)
            {
                x += xStep;
                error += deltaError;

                // if the error exceeds the X delta then move one on the Y axis
                if ((2 * error) > deltaX)
                {
                    y += yStep;
                    error -= deltaX;
                }

                // flip the coords if they're steep
                if (steep == true)
                {
                    tmpX = y;
                    tmpY = x;
                }
                else
                {
                    tmpX = x;
                    tmpY = y;
                }

                // light this spot
                DiscoverTile(tmpX, tmpY);

                // walls and doors (for example) stop LOS
                if (BlocksVis(tmpX, tmpY) == true) return;

                // if we hit a cave space, limit range
                if (IsHall(tmpX, tmpY) == true) maxRange = 2;

                r++;

                // reached the end of vision?
                if (r >= maxRange) return;
            }
        }


        // PATHFINDING!!
        public void ConnectStairs()
        {
            // for now, let's draw a path from up stairs to downstairs
            pf = new PathFinder(this);
            stairsUp = FindStairs(false);
            stairsDown = FindStairs();
            stairsLinkPath = pf.FindPath(stairsUp, stairsDown);

            if (stairsLinkPath != null)
            {
                stairsLinked = true;

                // the path given us is backwards
                stairsLinkPath.Reverse();

                // we now have a rope from upstairs to downstairs
                foreach (PathFinderNode n in stairsLinkPath)
                {
                    //Debug.WriteLine("Drop a path on {0},{1}",
                    //    n.PX, n.PY);
                    mapData[n.X, n.Y].sFlags |= SpaceFlags.OnStairRope;
                }
            }
            else
            {
                stairsLinked = false;
                //Debug.WriteLine("There is no path!");
            }
        }


        // =========== DISPLAY FUNCTIONS ==================
        public virtual void Display(bool showStairPath = false)
        {
            for (int y = 0; y < mHeight; y++)
            {
                string oStr = "";
                for (int x = 0; x < mWidth; x++)
                {
                    oStr += DisplaySpace(mapData[x, y], showStairPath);
                }

                Util.printf(oStr, 0, y);
            }
        }

        protected string DisplaySpace(Space s, bool showStairPath = false)
        {
            int sFlags = s.sFlags;

            // haven't discovered this space yet? bail!
            if ((sFlags & SpaceFlags.Mapped) == 0) return " ";

            // void
            if ((sFlags & SpaceFlags.Void) > 0) return " ";

            // door
            if ((sFlags & SpaceFlags.Door) > 0)
            {
                if ((s.dFlags & DoorFlags.Closed) > 0)
                {
                    return "+";
                }
                else
                {
                    return "O";
                }
            }

            // floor
            if ((sFlags & SpaceFlags.StairsDown) > 0) return ">";
            if ((sFlags & SpaceFlags.StairsUp) > 0) return "<";
            // pathfinding debug
            if ((showStairPath == true) && ((sFlags & SpaceFlags.OnStairRope) > 0)) return "*";
            if ((sFlags & SpaceFlags.Floor) > 0) return "."; // return DrawHex(s.tID);

            // cave floor
            if ((sFlags & SpaceFlags.Cave) > 0) return ((char)9618).ToString(); // or 9617

            // walls
            if ((sFlags & SpaceFlags.Wall) > 0) return DrawWall(s);

            // wtf?
            return "?";
        }


        // ====== SPACE QUERY FUNCTIONS =====================
        public Point2 FindStairs(bool down = true)
        {
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    if ((down == true) && ((mapData[x, y].sFlags & SpaceFlags.StairsDown) > 0)) return new Point2(x, y);
                    if ((down == false) && ((mapData[x, y].sFlags & SpaceFlags.StairsUp) > 0)) return new Point2(x, y);
                }
            }
            return null;
        }

        protected bool SpaceExists(int x, int y)
        {
            if ((x < 0) || (y < 0)) return false;
            if ((x >= mWidth) || (y >= mHeight)) return false;
            return true;
        }

        protected bool CanWalk(Space s)
        {
            if ((s.sFlags & SpaceFlags.StopWalk) == 0) return true;
            return false;
        }
        public bool CanWalk(int x, int y)
        {
            if (!SpaceExists(x, y)) return false;
            return CanWalk(mapData[x, y]);
        }

        protected bool IsVoid(Space s)
        {
            if ((s.sFlags & SpaceFlags.Void) > 0) return true;
            return false;
        }
        protected bool IsVoid(int x, int y)
        {
            if (!SpaceExists(x, y)) return false;
            return IsVoid(mapData[x, y]);
        }

        protected bool IsFloor(Space s)
        {
            if ((s.sFlags & SpaceFlags.Floor) > 0) return true;
            return false;
        }
        protected bool IsFloor(int x, int y)
        {
            if (!SpaceExists(x, y)) return false;
            return IsFloor(mapData[x, y]);
        }

        protected bool IsHall(Space s)
        {
            if ((s.sFlags & SpaceFlags.Cave) > 0) return true;
            return false;
        }
        public bool IsHall(int x, int y)
        {
            if (!SpaceExists(x, y)) return false;
            return IsHall(mapData[x, y]);
        }

        protected bool IsDoor(Space s)
        {
            if ((s.sFlags & SpaceFlags.Door) > 0) return true;
            return false;
        }
        public bool IsDoor(int x, int y)
        {
            if (!SpaceExists(x, y)) return false;
            return IsDoor(mapData[x, y]);
        }

        protected bool IsStairs(Space s)
        {
            if ((s.sFlags & SpaceFlags.StairsUp) > 0) return true;
            if ((s.sFlags & SpaceFlags.StairsDown) > 0) return true;
            return false;
        }
        protected bool IsStairs(int x, int y)
        {
            if (!SpaceExists(x, y)) return false;
            return IsStairs(mapData[x, y]);
        }

        protected bool IsWall(Space s)
        {
            if ((s.sFlags & SpaceFlags.Wall) > 0) return true;
            return false;
        }
        protected bool IsWall(int x, int y)
        {
            if (!SpaceExists(x, y)) return false;
            return IsWall(mapData[x, y]);
        }

        protected bool IsWallCorner(Space s)
        {
            if ((s.sFlags & SpaceFlags.Wall) == 0) return false;
            int tID = s.tID;
            if ((tID == 9484) || (tID == 9488) || (tID == 9492) || (tID == 9496)) return true;
            return false;
        }
        protected bool IsWallCorner(int x, int y)
        {
            if (!SpaceExists(x, y)) return false;
            return IsWallCorner(mapData[x, y]);
        }

        protected bool IsEmpty(Space s)
        {
            if ((s.sFlags & SpaceFlags.StairsUp) > 0) return false;
            if ((s.sFlags & SpaceFlags.StairsDown) > 0) return false;
            return true;           
        }
        protected bool IsEmpty(int x, int y)
        {
            if (!SpaceExists(x, y)) return false;
            return IsEmpty(mapData[x, y]);
        }

        protected bool BlocksVis(int x, int y)
        {
            if (!SpaceExists(x, y)) return true;
            return BlocksVis(mapData[x, y]);
        }
        protected bool BlocksVis(Space s)
        {
            if ((s.sFlags & SpaceFlags.BlockVis) > 0) return true;
            if ((IsDoor(s) == true) && ((s.dFlags & DoorFlags.Closed) > 0)) return true;
            if (IsVoid(s) == true) return true;
            return false;
        }

        // ========== BUILDING FUNCTIONS =========================
        protected void Pave(int x, int y, Room r = null, bool lit = true)
        {
            if (!SpaceExists(x, y)) return;
            Pave(mapData[x, y], r, lit);
        }
        protected void Pave(Space s, Room r = null, bool lit = true)
        {
            s.sFlags = SpaceFlags.Floor;
            s.myRoom = r;

            if (r != null)
            {
                s.tID = r.roomID;
                s.roomID = r.roomID;
            }
            else
            {
                s.tID = -1;
                s.roomID = -1;
            }
            if (lit) s.sFlags |= SpaceFlags.Lit;
        }

        protected void PaveCave(int x, int y)
        {
            if (!SpaceExists(x, y)) return;
            PaveCave(mapData[x, y]);
        }
        protected void PaveCave(Space s)
        {
            s.sFlags = SpaceFlags.Cave;
            s.tID = 3;
        }

        protected void BuildWall(int x, int y, Room r = null, int tID = -1, bool lit = true)
        {
            if (!SpaceExists(x, y)) return;
            BuildWall(mapData[x, y], r, tID, lit);
        }
        protected void BuildWall(Space s, Room r = null, int tID = -1, bool lit = true)
        {
            s.sFlags = SpaceFlags.Wall | SpaceFlags.StopWalk | SpaceFlags.BlockVis;
            if (tID == -1) tID = 9608;
            s.tID = tID;
            if (r != null)
            {
                s.roomID = r.roomID;
              
                s.myRoom = r;
            }
            if (lit) s.sFlags |= SpaceFlags.Lit;
        }

        protected void BuildDoor(int x, int y)
        {
            if (!SpaceExists(x, y)) return;
            BuildDoor(mapData[x, y]);
        }
        protected void BuildDoor(Space s)
        {
            s.sFlags = SpaceFlags.Floor | SpaceFlags.Door;
            s.dFlags = DoorFlags.Closed | DoorFlags.Locked;
            s.tID = 1;
        }

        protected void BuildVoid(int x, int y)
        {
            if (!SpaceExists(x, y)) return;
            BuildVoid(mapData[x, y]);
        }
        protected void BuildVoid(Space s)
        {
            s.sFlags = SpaceFlags.Void | SpaceFlags.StopWalk;
            s.tID = 0;
            s.roomID = -1;
            s.myRoom = null;
        }

        // ========== DISCOVERY, LIGHTING, EXPLORING ==============

        // standing at tile x,y... discover tiles on the map up to maxRange
        protected void DiscoverTiles(int x, int y, int maxRange = 8)
        {
            DiscoverTile(x, y);

            for (int xx = x - maxRange; xx <= x + maxRange; xx++)
            {
                for (int yy = y - maxRange; yy <= y + maxRange; yy++)
                {
                    // check to see if this point appears within the circle
                    if ((Math.Pow(x - xx, 2) + Math.Pow(y - yy, 2)) < Math.Pow(maxRange, 2))
                    {
                        // do a LOS to this point
                        LightLOS(x, y, xx, yy, maxRange);
                    }
                }
            }
        }

        // player has "seen" this tile for the first time
        protected void DiscoverTile(int x, int y)
        {
            if (!SpaceExists(x, y)) return;
            DiscoverTile(mapData[x, y]);
        }
        protected void DiscoverTile(Space s)
        {
            s.sFlags |= SpaceFlags.Mapped;
        }


        // ========== DISPLAY FUNCTIONS ===========================
        
        protected string DrawHex(int tID)
        {
            while (tID > 15) tID -= 16;

            if (tID == -1) return ".";

            if (tID < 10) return tID.ToString();
            else return tID.ToString("X");
        }
        

        protected string DrawWall(Space s)
        {
            return ((char)s.tID).ToString();
        }

    }
}
