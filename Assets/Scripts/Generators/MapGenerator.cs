using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


public abstract class Room
{
    private static int ID_GENERATOR_INDEX = 0;
    public int id { get; set; }
    protected Vector2Int[] tiles;
    protected int[] doorTilesIndex;
    protected int centerIndex;

    protected Room() {
        id = ID_GENERATOR_INDEX;
        ID_GENERATOR_INDEX++;
    }
    public Vector2Int Center { get { return tiles[centerIndex]; } }
   
    public Vector2Int[] getTiles()
    {
        return tiles;
    }

    public Vector2Int getCenter() {
        return tiles[centerIndex];
    }
    public Vector2Int[] getDoorTiles()
    {
        Vector2Int[] doorTiles = new Vector2Int[doorTilesIndex.Length];
        for (int i = 0; i < doorTilesIndex.Length; i++)
            doorTiles[i] = tiles[doorTilesIndex[i]];
 
        return doorTiles;
    }
    public void moveRoomTo(Point newStart)
    {
        for (int tile = 0; tile < tiles.Length; tile++)
            tiles[tile] = new Vector2Int(tiles[tile].x + newStart.X, tiles[tile].y + newStart.Y);
    }

    public override bool Equals(object obj)
    {
        if (obj is not Room)
            return false;

        Room r = obj as Room;
        return id == r.id;
    }


    public override int GetHashCode()
    {
        return base.GetHashCode(); 
    }

}

public abstract class RoomGeneric : Room
{
    protected abstract void generateTiles();
    protected RoomGeneric(): base() {}
}

public class RoomSqr : RoomRectangle
{
    public RoomSqr(int side) : base(side, side) { }

}

public class RoomRectangle : RoomGeneric
{
    private int basis;
    private int height;
    public RoomRectangle(int basis, int height): base()
    {
        this.basis = basis;
        this.height = height;
        generateTiles();
        generateDoorTiles();

    }

    protected override void generateTiles()
    {
        tiles = new Vector2Int[basis * height];
        for (int i = 0; i < basis; i++)
            for (int j = 0; j < height; j++)
                tiles[i * basis + j] = new Vector2Int(j, i);
        centerIndex = (basis * height) / 2;
    }

    private void generateDoorTiles() {
        List<int> doorTilesIndexAux = new List<int>();

        for (int i = 1; i < height; i = i + 1) {
            doorTilesIndexAux.Add(i * basis);
            doorTilesIndexAux.Add(i * basis + basis - 1);
        }

        for (int j = 1; j < basis; j = j + 1)
        {
            doorTilesIndexAux.Add(j);
            doorTilesIndexAux.Add((height - 1) * basis + j);
        }

        doorTilesIndex = doorTilesIndexAux.ToArray();
    }


    public Vector2Int start() => tiles[0];
    public Vector2Int end() => tiles[tiles.Length - 1];


}


public class Map {
    public HashSet<Vector2Int> tiles { get; set; }

    public List<Room> rooms; // TODO hacer esto 

    public HashSet<Vector2Int> corridorTiles;

    public Dictionary<Vector2Int, Room> indexVectorRoom;
    public BoundsInt bounds { get; set; }

    public int[,] rawTiles { get; set; }

    public Map(List<Room> rooms, HashSet<Vector2Int> corridorTiles, BoundsInt bounds) {
        this.rooms = rooms;
        this.corridorTiles = corridorTiles;
        tiles = new HashSet<Vector2Int>();
        tiles.UnionWith(corridorTiles);
        foreach (Room r in rooms)
            tiles.UnionWith(r.getTiles());

        indexVectorRoom = new Dictionary<Vector2Int, Room>(tiles.Count);

        foreach (Room r in rooms)
            foreach (Vector2Int t in r.getTiles())
                indexVectorRoom.Add(t, r);

        this.bounds = bounds;
        generateRawInformation();
    }


    public Map(int[,] rawTiles) {
        tiles = new HashSet<Vector2Int>();
        int xLength = rawTiles.GetLength(1);
        int yLength = rawTiles.GetLength(0);
        for (int i = 0; i < yLength; i++)
            for (int j = 0; j < xLength; j++)
                if (rawTiles[i, j] == 1)
                    tiles.Add(new Vector2Int(j, i));
        bounds = new BoundsInt(0, 0, 0, xLength, yLength, 1);
        this.rawTiles = rawTiles;
    }


    public int[,] generateRawInformation() // optimizar
    {
        rawTiles = new int[bounds.size.y, bounds.size.x];
        foreach (Vector2Int tile in tiles)
            rawTiles[tile.y, tile.x] = 1;
        return rawTiles;
    }


    public bool addTile(Vector2Int tile)
    {

        if (!bounds.Contains((Vector3Int)tile))
            return false;

        tiles.Add(tile);
        rawTiles[tile.y, tile.x] = 1;
        return true;
    }

    public bool removeTile(Vector2Int tile)
    {
        if (!bounds.Contains((Vector3Int)tile))
            return false;

        bool isRemoved = tiles.Remove(tile);
        if(isRemoved)
            rawTiles[tile.y, tile.x] = 0;
        return isRemoved;
    }

   
}
public class MapGenerator
{


    public static int squareSize = 16;
    public static int squareHeightMap = 2;
    public static int squareWidthMap = 4;
    public static int maxNumRooms { get { return squareHeightMap * squareWidthMap; } }
    public static int maxSizeHab = squareSize / 2;
    public static int minSizeHab = maxSizeHab / 2;


    List<Room> rooms;
    HashSet<Vector2Int> corridorTiles;
    Room[] occupiedSlots;
   
    public void generateCorridors()
    {

        if (rooms.Count <= 1)
            return;

        HashSet<Vector2Int> roomTiles = new HashSet<Vector2Int>();
        IEnumerable<Vector2Int[]> tilesByRoom = rooms.Select(r => r.getTiles());

        foreach (Vector2Int[] tiles in tilesByRoom)
            roomTiles.UnionWith(tiles);

        foreach (KeyValuePair<Room, HashSet<Room>> entry in generateRoomConnections())
            foreach (Room room in entry.Value) {
                corridorTiles.UnionWith(generateCorridor(entry.Key, room).Where(t => !roomTiles.Contains(t)));
            }

    }

    private Dictionary<Room, HashSet<Room>> generateRoomConnections()
    {

        Dictionary<Room, HashSet<Room>> connections = new Dictionary<Room, HashSet<Room>>();
        foreach (Room room in rooms)
            connections.Add(room, new HashSet<Room>());

        List<Room> roomListShuffled = ShuffleList<Room>.shuffle(rooms).ToList();

        for (int i = 0; i < roomListShuffled.Count - 1; i++)
            connections[roomListShuffled[i]].Add(roomListShuffled[i + 1]);

        int extraCorridors = rooms.Count / 2;
        foreach (Room room in ShuffleList<Room>.pickRandomElements(rooms, extraCorridors))
        {
            if(room != rooms[0])
                connections[room].Add(rooms[0]);
        }
        return connections;
    }
    

    public HashSet<Vector2Int> generateCorridor(Room source, Room end) {

        List<Vector2Int> corridorTiles = new List<Vector2Int>();

        Vector2Int sourceTile = ShuffleList<Vector2Int>.pickRandomElement(source.getDoorTiles());
        Vector2Int endTile = ShuffleList<Vector2Int>.pickRandomElement(end.getDoorTiles());

        int step = (sourceTile.x < endTile.x) ? 1:-1; 
        corridorTiles.AddRange(Range(sourceTile.x, endTile.x, step).Select(x => new Vector2Int(x, sourceTile.y)));

        step = (sourceTile.y < endTile.y) ? 1:-1;
        int lastX = corridorTiles[corridorTiles.Count - 1].x;
        corridorTiles.AddRange(Range(sourceTile.y, endTile.y, step).Select(y => new Vector2Int(lastX, y)));

        return corridorTiles.ToHashSet();

    }

    public Map generateNewMap(string tipo, int numHab)
    {
        if (numHab > maxNumRooms) {
            throw new Exception(string.Format("Introduced rooms exceeded max num of the map Max:{0} Introduced:{1}", maxNumRooms, numHab));
        }

        initDataStructures();
        Map map = generateNewSimple(numHab); 
        deleteDataStructures();
        return map;
    }

    public void initDataStructures() {
        rooms = new List<Room>();
        occupiedSlots = new Room[squareHeightMap * squareWidthMap];
        corridorTiles = new HashSet<Vector2Int>();
       
    }

    public void deleteDataStructures() {
        rooms = null;
        occupiedSlots = null;
        corridorTiles = null;
    }

    private Map generateNewSimple(int numHab)
    {
        foreach (int squareSlot in ShuffleList<int>.pickRandomElements(Enumerable.Range(0, maxNumRooms).ToArray(), numHab))
            createRoom(squareSlot);

        generateCorridors();

        BoundsInt bounds = new BoundsInt(0, 0, 0, squareSize * squareWidthMap, squareSize * squareHeightMap, 1);

        return new Map(rooms, corridorTiles, bounds);
    }


    private bool createRoom(int squareSlot)
    {

        //int size = Random.Range(minSizeHab, maxSizeHab);
        int size = maxSizeHab;
        Point start = new Point(Random.Range(2, squareSize - size), Random.Range(2, squareSize - size));
        start = normalizeToSlot(start, squareSlot);
        Room room = new RoomSqr(size); 
        return addRoom(room, start); 
    }


    private bool addRoom(Room room, Point pos)
    {
        room.moveRoomTo(pos);
        int[] newOccupiedSlots = room.getTiles().Select(t => getSlotOf(t)).Distinct().ToArray();

        int[] conflictOccupiedSlots = newOccupiedSlots.Where(s => occupiedSlots[s] != null).ToArray(); 

        if (conflictOccupiedSlots.Length != 0)
            return false;
        else
        {
            rooms.Add(room);
            foreach (int slot in newOccupiedSlots)
                occupiedSlots[slot] = room;
            return true;
        }

    }


    private int getSlotOf(Vector2Int tile) => (tile.y / squareSize) * squareWidthMap + (tile.x / squareSize) % squareWidthMap;
    private Point normalizeToSlot(Point n, int squareSlot)
    {
        int slotWidthPos = (squareSlot % squareWidthMap) * squareSize;
        int slotHeightPos = ((int)Math.Floor((float)squareSlot / squareWidthMap) * squareSize);
        return new Point(n.X + slotWidthPos, n.Y + slotHeightPos);

    }


    public static IEnumerable<int> Range(int first, int end, int step)
    {
        if (step == 0)
            throw new Exception("El step no puede ser 0");

        if (step > 0)
            for (int i = first; i <= end; i += step)
                yield return i;
        else
            for (int i = first; i >= end; i += step)
                yield return i;

    }
}

