using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonGenerator
{
    

    enum BlockType
    {
        Wall,
        Empty,
        Floor,
        Room
    }

    class Point2d
    {
        public Point2d(int x, int y) {
            this.X = x;
            this.Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }
    }

    class Area : IComparable<Area>
    {
        public Area(Point2d topLeft, Point2d size)
        {
            this.TopLeft = topLeft;
            this.Size = size;
        }

        public Area(int x, int y, int w, int h)
        {
            this.TopLeft = new Point2d(x, y);
            this.Size = new Point2d(w, h);
        }

        public Point2d TopLeft { get; set; }
        public Point2d BottomRight
        {
            get
            {
                return new Point2d(TopLeft.X + Size.X, TopLeft.Y + Size.Y);
            }
        }
        public Point2d Size { get; set; }

        public int AreaSize
        {
            get
            {
                return Size.X * Size.Y;
            }
        }

        public Area[] Split(Area area)
        {
            List<Area> newAreas = new List<Area>();
            //newAreas.Add(area);
            //In questa suddivisione c'è la possiblità che venga persa l'area topleft
            if (this.TopLeft.X != area.TopLeft.X)
                newAreas.Add(new Area(TopLeft.X, TopLeft.Y, area.TopLeft.X - TopLeft.X, Size.Y));
            if (this.TopLeft.Y != area.TopLeft.Y)
                newAreas.Add(new Area(TopLeft.X, TopLeft.Y, Math.Abs(Size.X - (area.TopLeft.X - TopLeft.X)), Math.Abs(area.TopLeft.Y - TopLeft.Y)));
            if (this.BottomRight.X != area.BottomRight.X)
                newAreas.Add(new Area(area.BottomRight.X, area.TopLeft.Y, Math.Abs(area.BottomRight.X - BottomRight.X), Math.Abs(BottomRight.Y - area.TopLeft.Y)));
            if (this.BottomRight.Y != area.BottomRight.Y)
                newAreas.Add(new Area(area.TopLeft.X, area.BottomRight.Y, area.Size.X, Math.Abs(BottomRight.Y - area.BottomRight.Y)));

            return newAreas.ToArray();
        }

        public override string ToString()
        {
            return string.Format("{0} {1} - {2} {3}", TopLeft.X, TopLeft.Y, Size.X, Size.Y);
        }

        public int CompareTo(Area a)
        {
            return this.AreaSize - a.AreaSize;
        }
    }

    class GameAreaMatrix
    {
        public BlockType[][] DisplacementMap { get; set; }
        public Point2d Size { get; set; }

        public GameAreaMatrix(Point2d dimension)
        {
            Size = dimension;
            DisplacementMap = new BlockType[dimension.X][];
            for (int i = 0; i < DisplacementMap.Count(); i++) {
                DisplacementMap[i] = new BlockType[dimension.Y];
                for (int j = 0; j < dimension.Y; j++) {
                    DisplacementMap[i][j] = BlockType.Empty;
                }
            }
        }

        public bool IsEmpty(int x, int y)
        {
            return DisplacementMap[x][y] == BlockType.Empty;
        }

        public void addRoom(Room room) {
            for (int i = room.Area.TopLeft.X; i < room.Area.BottomRight.X; i++) {
                for (int j = room.Area.TopLeft.Y; j < room.Area.BottomRight.Y; j++) {
                    if (i == room.Area.TopLeft.X || i == room.Area.BottomRight.X - 1
                        || j == room.Area.TopLeft.Y || j == room.Area.BottomRight.Y - 1)
                        DisplacementMap[i][j] = BlockType.Wall;
                    else
                        DisplacementMap[i][j] = BlockType.Room;
                }
            }
        }

        public override string ToString()
        {
 	        StringBuilder builder = new StringBuilder();

            for (int j = 0; j < Size.Y; j++) {
                for (int i = 0; i < Size.X; i++) {
                    BlockType t = DisplacementMap[i][j];
                    if (t == BlockType.Floor)
                        builder.Append("_");
                    else if (t == BlockType.Empty)
                        builder.Append(" ");
                    else if (t == BlockType.Room)
                        builder.Append("+");
                    else if (t == BlockType.Wall)
                        builder.Append("#");
                }
                builder.AppendLine();
            }

            return builder.ToString();
        }
    }

    class Room
    {
        public Area Area { get; private set; }
        public List<Corridor> Corridors;

        public Room(int x, int y, int w, int h)
        {
            Area = new Area(x, y, w, h);
            Corridors = new List<Corridor>();
        }

        public static Room RandomRoom(int x, int y, int maxWidth, int maxHeight, int minRoomArea, int maxRoomArea, Random rnd) {
            int w = 0, h = 0, area = 0;
            do
            {
                w = rnd.Next(3, maxWidth);
                h = rnd.Next(3, maxHeight);
                area = w * h;
            } while (area > maxRoomArea && area < minRoomArea);
            
            int nx = rnd.Next(x, x + maxWidth - w);
            int ny = rnd.Next(y, y + maxHeight - h);

            return new Room(nx, ny, w, h);
        }
    }

    class Corridor
    {
        private Room Room1;
        private Room Room2;
    }

    class Generator
    {
        private static int RoomWallSize = 1;
        private static int CooridorWallSize = 1;
        private int MinRoomSize;
        private int MaxRoomArea;

        private Point2d AreaDimension;
        public GameAreaMatrix DisplacementMap;

        public List<Area> EmptyArea;
        public List<Room> Rooms;
        public List<Corridor> Corridors;

        private Random Random;

        public Generator(Point2d dungeonSize, int minRoomSize, int maxRoomArea)
        {
            AreaDimension = dungeonSize;
            DisplacementMap = new GameAreaMatrix(AreaDimension);
            Rooms = new List<Room>();
            Corridors = new List<Corridor>();
            EmptyArea = new List<Area>
            {
                new Area(0,0,dungeonSize.X, dungeonSize.Y)
            };
            this.MinRoomSize = minRoomSize;
            this.MaxRoomArea = maxRoomArea;
            Random = new Random();
        }

        public void Generate()
        {
            while (EmptyArea.Count > 0 && EmptyArea[0].AreaSize > MinRoomSize)
            {
                Area area = EmptyArea[0];
                EmptyArea.RemoveAt(0);
                Room r = Room.RandomRoom(area.TopLeft.X, area.TopLeft.Y, area.Size.X, area.Size.Y, MinRoomSize, MaxRoomArea, Random);
                
                Rooms.Add(r);
                DisplacementMap.addRoom(r);

                Area[] newAreas = area.Split(r.Area);

                foreach (Area a in newAreas) {
                    if (a.AreaSize > MinRoomSize && a.Size.X > 2 && a.Size.Y > 2)
                        EmptyArea.Add(a);
                }

                
            }
        }

        public override string ToString()
        {
 	        return DisplacementMap.ToString();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Point2d dungeonSize = new Point2d(50, 50);
            int minRoomArea = 5*5;
            int maxRoomArea = 20*20;

            Generator g = new Generator(dungeonSize, minRoomArea, maxRoomArea);
            g.Generate();

            if (System.IO.File.Exists(@"WriteText.txt"))
                System.IO.File.Delete(@"WriteText.txt");
            System.IO.File.WriteAllText(@"WriteText.txt", g.ToString());
            Process.Start("notepad.exe", @"WriteText.txt");
            Console.ReadLine();
        }
    }
}
