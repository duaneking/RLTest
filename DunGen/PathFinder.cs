using System;
using System.Collections.Generic;
using System.Linq;


namespace DunGen
{
    class PathFinder
    {

         // STRUCTS AND ENUMS
        internal struct PathFinderNodeFast
        {
            public int F; // f = gone + heuristic
            public int G;
            public ushort PX; // Parent
            public ushort PY;
            public byte Status;
        }
        public enum PathFinderNodeType
        {
            Start = 1,
            End = 2,
            Open = 4,
            Close = 8,
            Current = 16,
            Path = 32
        }

        /*
        public enum HeuristicFormula
        {
            Manhattan = 1,
            MaxDXDY = 2,
            DiagonalShortCut = 3,
            Euclidean = 4,
            EuclideanNoSQR = 5,
            Custom1 = 6
        }*/

        // CLASS VARIABLES
        private Level mLevel = null;
        private Space[,] mGrid = null;
        private PriorityQueueB<int> mOpen = null;
        private List<PathFinderNode> mClose = new List<PathFinderNode>();
       // private HeuristicFormula mFormula = HeuristicFormula.Manhattan;
        private bool mDiagonals = true;
        private const int mHEstimate = 2;
        private bool mHeavyDiagonals = true;
        private const int mSearchLimit = 2000;
        private PathFinderNodeFast[] mCalcGrid = null;
        private byte mOpenNodeValue = 1;
        private byte mCloseNodeValue = 2;

        //Promoted local variables to member variables to avoid recreation between calls
        private int mH = 0;
        private int mLocation = 0;
        private int mNewLocation = 0;
        private ushort mLocationX = 0;
        private ushort mLocationY = 0;
        private ushort mNewLocationX = 0;
        private ushort mNewLocationY = 0;
        private int mCloseNodeCounter = 0;
        private ushort mGridX = 0;
        private ushort mGridY = 0;
        private bool mFound = false;
        private sbyte[,] mDirection = new sbyte[8, 2] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 }, { 1, -1 }, { 1, 1 }, { -1, 1 }, { -1, -1 } };
        private int mEndLocation = 0;
        private int mNewG = 0;



        // CONSTRUCTOR
        public PathFinder(Level lvl)
        {
            mLevel = lvl;
            mGrid = lvl.mapData;
            mGridX = (ushort)(mGrid.GetUpperBound(0) + 1);
            mGridY = (ushort)(mGrid.GetUpperBound(1) + 1);

            if (mCalcGrid == null || mCalcGrid.Length != (mGridX * mGridY))
                mCalcGrid = new PathFinderNodeFast[mGridX * mGridY];

            mOpen = new PriorityQueueB<int>(new ComparePFNodeMatrix(mCalcGrid));
        }


        public List<PathFinderNode> FindPath(Point2 start, Point2 end, bool diagOK = true)
        {
            //lock (this)
            //{
                mFound = false;
                mCloseNodeCounter = 0;
                mOpenNodeValue += 2;
                mCloseNodeValue += 2;
                mOpen.Clear();
                mClose.Clear();

                mDiagonals = diagOK;

                mLocation = (mGridX * start.Y) + start.X;
                mEndLocation = (mGridX * end.Y) + end.X;
                mCalcGrid[mLocation].G = 0;
                mCalcGrid[mLocation].F = mHEstimate;
                mCalcGrid[mLocation].PX = (ushort)start.X;
                mCalcGrid[mLocation].PY = (ushort)start.Y;
                mCalcGrid[mLocation].Status = mOpenNodeValue;

                mOpen.Push(mLocation);
                while (mOpen.Count > 0)
                {
                    mLocation = mOpen.Pop();

                    //Is it in closed list? means this node was already processed
                    if (mCalcGrid[mLocation].Status == mCloseNodeValue)
                        continue;

                    mLocationX = (ushort)(mLocation % mGridX);
                    mLocationY = (ushort)(mLocation / mGridX);

                    if (mLocation == mEndLocation)
                    {
                        mCalcGrid[mLocation].Status = mCloseNodeValue;
                        mFound = true;
                        break;
                    }

                    if (mCloseNodeCounter > mSearchLimit)
                    {
                        return null;
                    }

                    //Lets calculate each successors
                    // no diagonals if you're on a door or hallway
                    bool useDiag = mDiagonals;
                    if (mLevel.IsDoor(mLocationX, mLocationY) || mLevel.IsHall(mLocationX, mLocationY)) useDiag = false;
                    for (int i = 0; i < (useDiag ? 8 : 4); i++)
                    {
                        mNewLocationX = (ushort)(mLocationX + mDirection[i, 0]);
                        mNewLocationY = (ushort)(mLocationY + mDirection[i, 1]);
                        mNewLocation = (mNewLocationY * mGridX) + mNewLocationX;

                        // off map?
                        if (mNewLocationX >= mGridX || mNewLocationY >= mGridY) continue;

                        // no-walk?
                        if (mLevel.CanWalk(mNewLocationX, mNewLocationY) == false) continue;

                        // you also can't diag-walk onto a door or hallway space
                        if ((mLevel.IsDoor(mNewLocationX, mNewLocationY) || mLevel.IsDoor(mNewLocationX, mNewLocationY))
                            && (i >= 4)) continue;

                        // right now, all terrain is "1"
                        if (mHeavyDiagonals && i > 3)
                            //mNewG = mCalcGrid[mLocation].G + (int)(mGrid[mNewLocationX, mNewLocationY] * 2.41);
                            mNewG = mCalcGrid[mLocation].G + (int)(1 * 1.41);
                        else
                            //mNewG = mCalcGrid[mLocation].G + mGrid[mNewLocationX, mNewLocationY];
                            mNewG = mCalcGrid[mLocation].G + 1;

                        //Is it open or closed?
                        if (mCalcGrid[mNewLocation].Status == mOpenNodeValue ||
                            mCalcGrid[mNewLocation].Status == mCloseNodeValue)
                        {
                            // The current node has less code than the previous? then skip this node
                            if (mCalcGrid[mNewLocation].G <= mNewG)
                                continue;
                        }

                        mCalcGrid[mNewLocation].PX = mLocationX;
                        mCalcGrid[mNewLocation].PY = mLocationY;
                        mCalcGrid[mNewLocation].G = mNewG;

                        /*
                        switch (mFormula)
                        {
                            default:
                            case HeuristicFormula.Manhattan:
                                mH = mHEstimate * (Math.Abs(mNewLocationX - end.X) + Math.Abs(mNewLocationY - end.Y));
                                break;
                            case HeuristicFormula.MaxDXDY:
                                mH = mHEstimate * (Math.Max(Math.Abs(mNewLocationX - end.X), Math.Abs(mNewLocationY - end.Y)));
                                break;
                            case HeuristicFormula.DiagonalShortCut:
                                int h_diagonal = Math.Min(Math.Abs(mNewLocationX - end.X), Math.Abs(mNewLocationY - end.Y));
                                int h_straight = (Math.Abs(mNewLocationX - end.X) + Math.Abs(mNewLocationY - end.Y));
                                mH = (mHEstimate * 2) * h_diagonal + mHEstimate * (h_straight - 2 * h_diagonal);
                                break;
                            case HeuristicFormula.Euclidean:
                                mH = (int)(mHEstimate * Math.Sqrt(Math.Pow((mNewLocationY - end.X), 2) + Math.Pow((mNewLocationY - end.Y), 2)));
                                break;
                            case HeuristicFormula.EuclideanNoSQR:
                                mH = (int)(mHEstimate * (Math.Pow((mNewLocationX - end.X), 2) + Math.Pow((mNewLocationY - end.Y), 2)));
                                break;
                            case HeuristicFormula.Custom1:
                                Point dxy = new Point(Math.Abs(end.X - mNewLocationX), Math.Abs(end.Y - mNewLocationY));
                                int Orthogonal = Math.Abs(dxy.X - dxy.Y);
                                int Diagonal = Math.Abs(((dxy.X + dxy.Y) - Orthogonal) / 2);
                                mH = mHEstimate * (Diagonal + Orthogonal + dxy.X + dxy.Y);
                                break;
                        }
                         * */
                        // just use manhattan for now
                        mH = mHEstimate * (Math.Abs(mNewLocationX - end.X) + Math.Abs(mNewLocationY - end.Y));

                        mCalcGrid[mNewLocation].F = mNewG + mH;

                        mOpen.Push(mNewLocation);
                        mCalcGrid[mNewLocation].Status = mOpenNodeValue;
                    }

                    mCloseNodeCounter++;
                    mCalcGrid[mLocation].Status = mCloseNodeValue;

                }

                //mCompletedTime = HighResolutionTime.GetTime();
                if (mFound)
                {
                    mClose.Clear();
                    int posX = end.X;
                    int posY = end.Y;

                    PathFinderNodeFast fNodeTmp = mCalcGrid[(end.Y * mGridX) + end.X];
                    PathFinderNode fNode;
                    fNode.F = fNodeTmp.F;
                    fNode.G = fNodeTmp.G;
                    fNode.H = 0;
                    fNode.PX = fNodeTmp.PX;
                    fNode.PY = fNodeTmp.PY;
                    fNode.X = end.X;
                    fNode.Y = end.Y;

                    while (fNode.X != fNode.PX || fNode.Y != fNode.PY)
                    {
                        mClose.Add(fNode);
                        posX = fNode.PX;
                        posY = fNode.PY;
                        fNodeTmp = mCalcGrid[(posY * mGridX) + posX];
                        fNode.F = fNodeTmp.F;
                        fNode.G = fNodeTmp.G;
                        fNode.H = 0;
                        fNode.PX = fNodeTmp.PX;
                        fNode.PY = fNodeTmp.PY;
                        fNode.X = posX;
                        fNode.Y = posY;
                    }

                    mClose.Add(fNode);

                    return mClose;
                }
                return null;
           // }
        }

        // compare class
        internal class ComparePFNodeMatrix : IComparer<int>
        {
            PathFinderNodeFast[] mMatrix;

            public ComparePFNodeMatrix(PathFinderNodeFast[] matrix)
            {
                mMatrix = matrix;
            }

            public int Compare(int a, int b)
            {
                if (mMatrix[a].F > mMatrix[b].F)
                    return 1;
                else if (mMatrix[a].F < mMatrix[b].F)
                    return -1;
                return 0;
            }
        }

    }


    // QUEUE CLASS
    public class PriorityQueueB<T>
    {
        #region Variables Declaration
        protected List<T> InnerList = new List<T>();
        protected IComparer<T> mComparer;
        #endregion

        public PriorityQueueB()
        {
            mComparer = Comparer<T>.Default;
        }

        public PriorityQueueB(IComparer<T> comparer)
        {
            mComparer = comparer;
        }

        public PriorityQueueB(IComparer<T> comparer, int capacity)
        {
            mComparer = comparer;
            InnerList.Capacity = capacity;
        }

        protected void SwitchElements(int i, int j)
        {
            T h = InnerList[i];
            InnerList[i] = InnerList[j];
            InnerList[j] = h;
        }

        protected virtual int OnCompare(int i, int j)
        {
            return mComparer.Compare(InnerList[i], InnerList[j]);
        }

        /// <summary>
        /// Push an object onto the PQ
        /// </summary>
        /// <param name="O">The new object</param>
        /// <returns>The index in the list where the object is _now_. This will change when objects are taken from or put onto the PQ.</returns>
        public int Push(T item)
        {
            int p = InnerList.Count, p2;
            InnerList.Add(item); // E[p] = O
            do
            {
                if (p == 0)
                    break;
                p2 = (p - 1) / 2;
                if (OnCompare(p, p2) < 0)
                {
                    SwitchElements(p, p2);
                    p = p2;
                }
                else
                    break;
            } while (true);
            return p;
        }

        /// <summary>
        /// Get the smallest object and remove it.
        /// </summary>
        /// <returns>The smallest object</returns>
        public T Pop()
        {
            T result = InnerList[0];
            int p = 0, p1, p2, pn;
            InnerList[0] = InnerList[InnerList.Count - 1];
            InnerList.RemoveAt(InnerList.Count - 1);
            do
            {
                pn = p;
                p1 = 2 * p + 1;
                p2 = 2 * p + 2;
                if (InnerList.Count > p1 && OnCompare(p, p1) > 0) // links kleiner
                    p = p1;
                if (InnerList.Count > p2 && OnCompare(p, p2) > 0) // rechts noch kleiner
                    p = p2;

                if (p == pn)
                    break;
                SwitchElements(p, pn);
            } while (true);

            return result;
        }

        /// <summary>
        /// Notify the PQ that the object at position i has changed
        /// and the PQ needs to restore order.
        /// Since you dont have access to any indexes (except by using the
        /// explicit IList.this) you should not call this function without knowing exactly
        /// what you do.
        /// </summary>
        /// <param name="i">The index of the changed object.</param>
        public void Update(int i)
        {
            int p = i, pn;
            int p1, p2;
            do	// aufsteigen
            {
                if (p == 0)
                    break;
                p2 = (p - 1) / 2;
                if (OnCompare(p, p2) < 0)
                {
                    SwitchElements(p, p2);
                    p = p2;
                }
                else
                    break;
            } while (true);
            if (p < i)
                return;
            do	   // absteigen
            {
                pn = p;
                p1 = 2 * p + 1;
                p2 = 2 * p + 2;
                if (InnerList.Count > p1 && OnCompare(p, p1) > 0) // links kleiner
                    p = p1;
                if (InnerList.Count > p2 && OnCompare(p, p2) > 0) // rechts noch kleiner
                    p = p2;

                if (p == pn)
                    break;
                SwitchElements(p, pn);
            } while (true);
        }

        /// <summary>
        /// Get the smallest object without removing it.
        /// </summary>
        /// <returns>The smallest object</returns>
        public T Peek()
        {
            if (InnerList.Count > 0)
                return InnerList[0];
            return default(T);
        }

        public void Clear()
        {
            InnerList.Clear();
        }

        public int Count
        {
            get { return InnerList.Count; }
        }

        public void RemoveLocation(T item)
        {
            int index = -1;
            for (int i = 0; i < InnerList.Count; i++)
            {

                if (mComparer.Compare(InnerList[i], item) == 0)
                    index = i;
            }

            if (index != -1)
                InnerList.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return InnerList[index]; }
            set
            {
                InnerList[index] = value;
                Update(index);
            }
        }
    }


    public struct PathFinderNode
    {
        public int F;
        public int G;
        public int H;  // f = gone + heuristic
        public int X;
        public int Y;
        public int PX; // Parent
        public int PY;
    }
    
    public class Point2
    {
        private int _x;
        private int _y;

        public Point2(int x, int y)
        {
            this._x = x;
            this._y = y;
        }

        public int X { get { return this._x; } set { this._x = value; } }
        public int Y { get { return this._y; } set { this._y = value; } }

        // For debugging
        public override string ToString()
        {
            return string.Format("{0}, {1}", this.X, this.Y);
        }
    }


}
