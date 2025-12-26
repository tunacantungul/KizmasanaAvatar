using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Board : MonoBehaviour
{
    // A dictionary to hold all path tiles, keyed by their unique ID for quick lookups.
    private Dictionary<int, Tile> _pathTiles;

    // Public properties to access the organized tiles from other scripts like GameManager.
    public List<Tile> MainPath { get; private set; }
    public Dictionary<Tile.PlayerType, List<Tile>> HomePaths { get; private set; }
    public Dictionary<Tile.PlayerType, Tile> StartTiles { get; private set; }
    public Tile GoalTile { get; private set; }

    [Header("Pathing Configuration")]
    [Tooltip("The tile ID where the Fire Nation pawn enters the main path.")]
    public int fireNationStartTileID = 1;
    [Tooltip("The tile ID where the Earth Kingdom pawn enters the main path.")]
    public int earthKingdomStartTileID = 14;
    [Tooltip("The tile ID where the Air Nomads pawn enters the main path.")]
    public int airNomadsStartTileID = 27;
    [Tooltip("The tile ID where the Water Tribe pawn enters the main path.")]
    public int waterTribeStartTileID = 40;


    void Awake()
    {
        InitializeBoard();
    }

    /// <summary>
    /// Finds all Tile components in the scene and organizes them into paths and other data structures.
    /// This removes the need for procedural board generation.
    /// </summary>
    public void InitializeBoard()
    {
        // Initialize collections
        _pathTiles = new Dictionary<int, Tile>();
        HomePaths = new Dictionary<Tile.PlayerType, List<Tile>>
        {
            { Tile.PlayerType.FireNation, new List<Tile>() },
            { Tile.PlayerType.EarthKingdom, new List<Tile>() },
            { Tile.PlayerType.AirNomads, new List<Tile>() },
            { Tile.PlayerType.WaterTribe, new List<Tile>() }
        };
        StartTiles = new Dictionary<Tile.PlayerType, Tile>();

        // Find all tiles placed in the scene
        Tile[] allTiles = FindObjectsOfType<Tile>();

        foreach (Tile tile in allTiles)
        {
            switch (tile.type)
            {
                case Tile.TileType.Path:
                    // Ensure there are no duplicate IDs
                    if (!_pathTiles.ContainsKey(tile.tileID))
                    {
                        _pathTiles.Add(tile.tileID, tile);
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate Tile ID found: {tile.tileID}. Ignoring tile {tile.name}.");
                    }
                    break;

                case Tile.TileType.Home:
                    if (HomePaths.ContainsKey(tile.owner))
                    {
                        HomePaths[tile.owner].Add(tile);
                    }
                    break;

                case Tile.TileType.Goal:
                    GoalTile = tile;
                    break;
                
                // TileType.Base tiles are just for visuals and starting positions,
                // they don't need to be stored in a pathing structure by the Board itself.
                case Tile.TileType.Base:
                    break;
            }
        }
        
        // Populate the MainPath list by ordering the path tiles by their ID
        MainPath = _pathTiles.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToList();

        // Sort each home path based on their Tile ID to ensure they are in order.
        foreach (var homePathList in HomePaths.Values)
        {
            homePathList.Sort((a, b) => a.tileID.CompareTo(b.tileID));
        }

        // Identify and store the designated start tiles
        if (_pathTiles.ContainsKey(fireNationStartTileID)) StartTiles[Tile.PlayerType.FireNation] = _pathTiles[fireNationStartTileID];
        if (_pathTiles.ContainsKey(earthKingdomStartTileID)) StartTiles[Tile.PlayerType.EarthKingdom] = _pathTiles[earthKingdomStartTileID];
        if (_pathTiles.ContainsKey(airNomadsStartTileID)) StartTiles[Tile.PlayerType.AirNomads] = _pathTiles[airNomadsStartTileID];
        if (_pathTiles.ContainsKey(waterTribeStartTileID)) StartTiles[Tile.PlayerType.WaterTribe] = _pathTiles[waterTribeStartTileID];
        
        Debug.Log("Board Initialized: " + MainPath.Count + " main path tiles found and organized.");
    }

    /// <summary>
    /// Gets a specific tile from the main path using its ID.
    /// </summary>
    /// <param name="tileID">The ID of the tile to retrieve.</param>
    /// <returns>The Tile component if found, otherwise null.</returns>
    public Tile GetTile(int tileID)
    {
        _pathTiles.TryGetValue(tileID, out Tile tile);
        return tile;
    }
}