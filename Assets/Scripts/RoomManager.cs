using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
public class RoomManager:MonoBehaviour
{
    public static RoomManager roomManager = null;
    private Tilemap tilemapWall;
    private Tilemap tilemap;
    public bool isTilemapHighlight;
    public bool isTilemapWallHighlight;
    private Grid mainGrid;
    public BoundsInt wallBounds;
    public Map map;
    public Vector2Int[] getTiles { get { return map.tiles.ToArray(); } }
    public TileBase tileAsset;
    public TileBase defaultTile;
    readonly int cameraWidthTiles = 20;
    readonly int cameraHeightTiles = 20;

    public void Awake()
    {
        roomManager = this;
        mainGrid = new GameObject("Grid").AddComponent<Grid>();
        tilemapWall = createTilemap("Walls", collider: true);
        tilemap = createTilemap("Tilemap");

    }


    public static RoomManager get()
    {
        if (roomManager != null)
            return roomManager;
        else
            throw new InvalidOperationException("Se ha intentado acceder al RoomManager sin haberse instanciado"); //Cambiar el tipo de excepcion
    }


    private void fillMapWithWalls(BoundsInt bounds)
    {
        TileBase[] defaultTiles = new TileBase[(bounds.xMax - bounds.xMin) * (bounds.yMax - bounds.yMin)];
        Array.Fill(defaultTiles, defaultTile);
        tilemapWall.SetTilesBlock(bounds, defaultTiles);
    }

    private void clearMapData() {
        map = null;
        tilemap.ClearAllTiles();
        tilemapWall.ClearAllTiles();
    }

    public void addTile(Vector2Int tile) {
        if (!map.bounds.Contains((Vector3Int)tile))
            return;
        map.addTile(tile);
        Vector3Int t = (Vector3Int) tile;
        tilemap.SetTile(t, tileAsset);
        tilemapWall.SetTile(t, null);
    }

    public void deleteTile(Vector2Int tile) {
        map.deleteTile(tile);
        Vector3Int t = (Vector3Int)tile;
        tilemap.SetTile(t, null);
        tilemapWall.SetTile(t, defaultTile);
    }

    public bool isFloor(Vector2Int tile) {
        return map.rawTiles[tile.y, tile.x] == 1;
    
    }
    private void drawTiles(List<Vector2Int> mapTiles, TileBase tileAsset) {
        Vector3Int[] tilePositions = mapTiles.Select(p => (Vector3Int)p).ToArray();
        TileBase[] tilesToDraw = new TileBase[tilePositions.Length];
        Array.Fill(tilesToDraw, tileAsset);
        tilemap.SetTiles(tilePositions, tilesToDraw);
        Array.Fill(tilesToDraw, null);
        tilemapWall.SetTiles(tilePositions, tilesToDraw);
    }

    public void loadMap(Map newMap) { // Aqui tenemos un bug
        clearMapData();
        map = newMap;
        wallBounds = extendMapCameraVision(map.bounds);
        fillMapWithWalls(wallBounds);
        drawTiles(map.tiles.ToList(), tileAsset);
    }

    public void highlightMapArea(Color color) {
        putColorLayer(tilemapWall,map.bounds, color);
        isTilemapWallHighlight = true;
    }

    public void highlightDungeonArea(Color color)
    {
        putColorLayer(tilemap, map.bounds, color);
        isTilemapHighlight = true;
    }
    public void refreshMapTiles() {
        tilemap.RefreshAllTiles();
        tilemapWall.RefreshAllTiles();
    }

   
    private void putColorLayer(Tilemap tm, BoundsInt bounds, Color color) {
        for (int i = 0; i < bounds.yMax; i++)
            for (int j = 0; j < bounds.xMax; j++)
            {
                Vector3Int pos = new Vector3Int(j, i, 0);
                tm.SetTileFlags(pos, TileFlags.None);
                tm.SetColor(pos, color);
            }
    }

    public void putColorVector(Vector3Int[] positions, Color color)
    {
        foreach (Vector3Int pos in positions)
        {
            tilemapWall.SetTileFlags(pos, TileFlags.None);
            tilemapWall.SetColor(pos, color);
        }
    }



    public void putColorLayerOverTilemap(Tilemap tilemap, Color color) {
        tilemap.color = color;
    }
    

    public void disableColorLayer(BoundsInt bounds)
    {
        for (int i = 0; i < bounds.yMax; i++)
            for (int j = 0; j < bounds.xMax; j++)
            {
                Vector3Int pos = new Vector3Int(j, i, 0);
                tilemapWall.SetTileFlags(pos, TileFlags.LockColor);
            }
    }

    public void loadMap(int[,] rawTiles)
    {
        loadMap(new Map(rawTiles));

        //List<Vector3Int> tiles = new List<Vector3Int>();

        //putColorLayer(tilemapWall, map.bounds, Color.green);

        //for (int i = 0; i < 16; i++)
        //    for (int j = 0; j < 16; j++)
        //        tiles.Add(new Vector3Int(i, j));
        //putColorVector(tiles.ToArray(), Color.yellow);
        //tiles.Clear();
        //for (int i = 0; i < 16; i++)
        //    for (int j = 16; j < 32; j++)
        //        tiles.Add(new Vector3Int(i, j));

        //putColorVector(tiles.ToArray(), Color.grey);

        //tiles.Clear();

        //for (int i = 32; i < 48; i++)
        //    for (int j = 16; j < 32; j++)
        //        tiles.Add(new Vector3Int(i, j));
        //putColorVector(tiles.ToArray(), Color.grey);
        //tiles.Clear();

        //for (int i = 32; i < 48; i++)
        //    for (int j = 0; j < 16; j++)
        //        tiles.Add(new Vector3Int(i, j));

        //putColorVector(tiles.ToArray(), Color.yellow);

        //tiles.Clear();

        //for (int i = 48; i < 64; i++)
        //    for (int j = 0; j < 16; j++)
        //        tiles.Add(new Vector3Int(i, j));
        //putColorVector(tiles.ToArray(), Color.grey);
        //tiles.Clear();
        //for (int i = 48; i < 64; i++)
        //    for (int j = 16; j < 32; j++)
        //        tiles.Add(new Vector3Int(i, j));

        //putColorVector(tiles.ToArray(), Color.yellow);

        //tiles.Clear();

        //for (int i = 16; i < 32; i++)
        //    for (int j = 16; j < 32; j++)
        //        tiles.Add(new Vector3Int(i, j));
        //putColorVector(tiles.ToArray(), Color.yellow);
        //tiles.Clear();

        //for (int i = 16; i < 32; i++)
        //    for (int j = 0; j < 16; j++)
        //        tiles.Add(new Vector3Int(i, j));

        //putColorVector(tiles.ToArray(), Color.grey);


    }

    public BoundsInt extendMapCameraVision(BoundsInt bounds) {
        BoundsInt newBounds = new BoundsInt(bounds.position, bounds.size);
        (newBounds.xMin, newBounds.xMax) = (bounds.xMin - cameraWidthTiles, bounds.xMax + cameraWidthTiles);
        (newBounds.yMin, newBounds.yMax) = (bounds.yMin - cameraHeightTiles, bounds.yMax + cameraHeightTiles);
        return newBounds;
    }

    public BoundsInt getCoordinateAxisLimits() {
        return new BoundsInt(map.bounds.position, map.bounds.size); //TODO de momento ni copias ni leches referencia.
    }

    private Tilemap createTilemap(string nombre, bool collider = false)
    {
        var go = new GameObject(nombre);
        var tilemap = go.AddComponent<Tilemap>();
        tilemap.transform.SetParent(mainGrid.transform);
        tilemap.tileAnchor = new Vector3(0, 0, 0);
        go.AddComponent<TilemapRenderer>();
        if (collider)
            go.AddComponent<TilemapCollider2D>();

        return tilemap;

    }

}
