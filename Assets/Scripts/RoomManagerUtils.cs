using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class Room
{
    protected Vector2Int[] tiles;
    protected int[] doorTilesIndex;
   
    public Vector2Int[] getTiles()
    {
        return tiles;
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

}

public abstract class RoomGeneric : Room
{
    protected abstract void generateTiles();

    //protected abstract void randomGenerateTiles();
}

public class RoomSqr : RoomRectangle
{
    public RoomSqr(int side) : base(side, side) { }

}

public class RoomRectangle : RoomGeneric
{
    private int basis;
    private int height;
    public RoomRectangle(int basis, int height)
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
        foreach (Room aux in rooms) // TODO
            tiles.UnionWith(aux.getTiles());
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


    public void addTile(Vector2Int tile)
    {
        tiles.Add(tile);
        rawTiles[tile.y, tile.x] = 1;
    }

    public void deleteTile(Vector2Int tile)
    {
        tiles.Remove(tile);
        rawTiles[tile.y, tile.x] = 0;
    }

   
}
public class MapFactory
{


    public static int squareSize = 16;
    public static int squareHeightMap = 2;
    public static int squareWidthMap = 4;
    public static int maxNumRooms = squareHeightMap * squareWidthMap;
    public static int maxSizeHab = squareSize / 2;
    public static int minSizeHab = maxSizeHab / 2;


    List<Room> rooms;
    HashSet<Vector2Int> corridorTiles;
    Room[,] occupiedSlots;
   
    public void generateCorridors()
    {

        if (rooms.Count <= 1)
            return;

        foreach (KeyValuePair<Room, HashSet<Room>> entry in generateRoomConnections())
            foreach (Room room in entry.Value)
                corridorTiles.UnionWith(generateCorridor(entry.Key, room));

    }

    private Dictionary<Room, HashSet<Room>> generateRoomConnections()
    {

        Dictionary<Room, HashSet<Room>> connections = new Dictionary<Room, HashSet<Room>>();
        foreach (Room room in rooms)
            connections.Add(room, new HashSet<Room>());

        List<Room> roomListShuffled = ShuffleList<Room>.shuffle(rooms).ToList();

        for (int i = 0; i < roomListShuffled.Count - 1; i++)
            connections[roomListShuffled[i]].Add(roomListShuffled[i + 1]);

        foreach (Room room in ShuffleList<Room>.pickRandomElements(rooms, rooms.Count / 2))
        {
            Room roomEnd = rooms.Find(x => !connections[room].Contains(x));
            connections[room].Add(roomEnd);
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
        initDataStructures();
        Map map = generateNewSimple(numHab); // Cambiar un poco
        deleteDataStructures();
        return map;
    }

    public void initDataStructures() {
        rooms = new List<Room>();
        occupiedSlots = new Room[squareHeightMap, squareWidthMap];
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

        int size = Random.Range(minSizeHab, maxSizeHab);
        Point start = new Point(Random.Range(2, squareSize - size), Random.Range(2, squareSize - size));
        start = normalizeToSlot(start, squareSlot);
        Room room = new RoomSqr(size); //Lo logico es hacer que el random este fuera, no es createRandomRoom
        return addRoom(room, start); // Tambien puede ser ventajoso poner el start en la room
    }

    private bool addRoom(Room room, Point pos)
    {
        room.moveRoomTo(pos);
        Vector2Int slot;
        HashSet<Vector2Int> newOccupiedSlots = new HashSet<Vector2Int>();

        foreach (Vector2Int tile in room.getTiles())
        {
            slot = getSlotOf(tile);

            if (occupiedSlots.GetLength(1) <= slot.x || occupiedSlots.GetLength(0) <= slot.y)
                return false; // No es necesario para algunas por precond.

            if (occupiedSlots[slot.y, slot.x] == null)
                newOccupiedSlots.Add(slot);
            else
                return false;
        }

        rooms.Add(room);
        foreach (Vector2Int slt in newOccupiedSlots)
            occupiedSlots[slt.y, slt.x] = room;
        return true;
    }

    private Vector2Int getSlotOf(Vector2Int tile) => new Vector2Int((tile.x / squareSize),(tile.y / squareSize));
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

