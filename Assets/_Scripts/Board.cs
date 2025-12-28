using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages all the tiles in the scene.
/// Finds and categorizes tiles on Awake, providing easy access for other systems.
/// This class does not create any tiles itself; it only reads the scene.
/// </summary>
public class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    // Fast lookup for any tile by its unique ID
    public Dictionary<int, Tile> allTiles = new Dictionary<int, Tile>();

    // Categorized tiles for game logic
    public Dictionary<Tile.PlayerType, List<Tile>> baseTiles = new Dictionary<Tile.PlayerType, List<Tile>>();
    public Dictionary<Tile.PlayerType, List<Tile>> homeTiles = new Dictionary<Tile.PlayerType, List<Tile>>();
    public Dictionary<Tile.PlayerType, Tile> startTiles = new Dictionary<Tile.PlayerType, Tile>();
    
    // The main path, sorted sequentially by TileID
    public List<Tile> pathTiles = new List<Tile>();
    public int PathLength => pathTiles.Count;

    void Awake()
    {
        // Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeBoard();
    }

    private void InitializeBoard()
    {
        // Initialize dictionaries for each player nation
        var playerTypes = System.Enum.GetValues(typeof(Tile.PlayerType)).Cast<Tile.PlayerType>();
        foreach (var player in playerTypes)
        {
            if (player == Tile.PlayerType.None) continue;
            baseTiles[player] = new List<Tile>();
            homeTiles[player] = new List<Tile>();
        }

        // Find all Tile components placed in the current scene
        Tile[] sceneTiles = FindObjectsOfType<Tile>();

        foreach (Tile tile in sceneTiles)
        {
            // Add to the main dictionary, checking for duplicate IDs
            if (allTiles.ContainsKey(tile.tileID))
            {
                Debug.LogWarning($"[Board] Duplicate TileID '{tile.tileID}' found on object '{tile.gameObject.name}'. It will be ignored.", tile.gameObject);
                continue;
            }
            allTiles.Add(tile.tileID, tile);

            // Categorize the tile based on its type and owner
            switch (tile.type)
            {
                case Tile.TileType.Path:
                    pathTiles.Add(tile);
                    break;
                case Tile.TileType.Base:
                    if (baseTiles.ContainsKey(tile.owner))
                        baseTiles[tile.owner].Add(tile);
                    break;
                case Tile.TileType.Home:
                    if (homeTiles.ContainsKey(tile.owner))
                        homeTiles[tile.owner].Add(tile);
                    break;
                case Tile.TileType.Start:
                    if (startTiles.ContainsKey(tile.owner))
                        Debug.LogWarning($"[Board] Duplicate Start tile for '{tile.owner}'. Overwriting with '{tile.gameObject.name}'.", tile.gameObject);
                    startTiles[tile.owner] = tile;
                    break;
            }
        }

        // Sort the path and home tiles by their ID to ensure sequential movement
        pathTiles = pathTiles.OrderBy(t => t.tileID).ToList();
        foreach (var homeList in homeTiles.Values)
        {
            homeList.Sort((a, b) => a.tileID.CompareTo(b.tileID));
        }

        Debug.Log($"[Board] Initialized. Found {allTiles.Count} tiles. Main path has {PathLength} tiles.");
    }
    
    public Tile GetTile(int tileID)
    {
        allTiles.TryGetValue(tileID, out Tile tile);
        return tile;
    }
}