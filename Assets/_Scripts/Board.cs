using UnityEngine;
using System.Collections.Generic;

public class Board : MonoBehaviour
{
    [Header("Tile Assets")]
    public GameObject tilePrefab;
    public Material baseMaterial;
    public Material pathMaterial;
    public Material[] playerMaterials; // 0: Fire, 1: Earth, 2: Air, 3: Water

    private readonly List<GameObject> boardTiles = new List<GameObject>();
    private readonly Tile.PlayerType[] playerTypes = {
        Tile.PlayerType.FireNation,
        Tile.PlayerType.EarthKingdom,
        Tile.PlayerType.AirNomads,
        Tile.PlayerType.WaterTribe
    };

    // Board layout constants
    private const int BoardSize = 15;
    private const int PathWidth = 3;

    public void GenerateBoard()
    {
        // Clear existing board if any
        foreach (var tile in boardTiles)
        {
            if (tile != null) Destroy(tile);
        }
        boardTiles.Clear();

        CreateBoardLayout();
    }

    private void CreateBoardLayout()
    {
        for (int x = 0; x < BoardSize; x++)
        {
            for (int z = 0; z < BoardSize; z++)
            {
                // Is this position part of the path?
                if (IsPath(x, z))
                {
                    Vector3 position = new Vector3(x, 0, z);
                    CreateTile(position, pathMaterial, "Path Tile", Tile.TileType.Path, Tile.PlayerType.None);
                }
                // Is this position a player base?
                else if (IsPlayerBase(x, z, out int playerIndex))
                {
                    Vector3 position = new Vector3(x, 0, z);
                    var playerType = playerTypes[playerIndex];
                    CreateTile(position, playerMaterials[playerIndex], $"{playerType} Base", Tile.TileType.Base, playerType);
                }
            }
        }

        CreateHomePaths();
        // Optionally create the central goal tile
        CreateTile(new Vector3(BoardSize / 2, 0, BoardSize / 2), baseMaterial, "Goal", Tile.TileType.Goal, Tile.PlayerType.None);
    }

    private void CreateTile(Vector3 position, Material material, string name, Tile.TileType type, Tile.PlayerType owner)
    {
        GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
        tileObj.name = name;
        
        Renderer renderer = tileObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }

        Tile tileComp = tileObj.AddComponent<Tile>();
        tileComp.type = type;
        tileComp.owner = owner;

        boardTiles.Add(tileObj);
    }

    private bool IsPath(int x, int z)
    {
        int centerStart = (BoardSize - PathWidth) / 2; // 6
        int centerEnd = centerStart + PathWidth - 1; // 8

        if ((x >= centerStart && x <= centerEnd) || (z >= centerStart && z <= centerEnd))
        {
             // Exclude the center square which is the goal
            if (x >= centerStart && x <= centerEnd && z >= centerStart && z <= centerEnd)
            {
                return false;
            }
            return true;
        }

        return false;
    }

    private bool IsPlayerBase(int x, int z, out int playerIndex)
    {
        int cornerSize = (BoardSize - PathWidth) / 2; // 6x6 corners
        playerIndex = -1;

        // Bottom-Left (Player 0 - FireNation)
        if (x < cornerSize && z < cornerSize)
        {
            playerIndex = 0; 
            return true;
        }
        // Top-Left (Player 1 - EarthKingdom)
        if (x < cornerSize && z > (cornerSize + PathWidth - 1))
        {
            playerIndex = 1;
            return true;
        }
        // Top-Right (Player 2 - AirNomads)
        if (x > (cornerSize + PathWidth - 1) && z > (cornerSize + PathWidth - 1))
        {
            playerIndex = 2;
            return true;
        }
        // Bottom-Right (Player 3 - WaterTribe)
        if (x > (cornerSize + PathWidth - 1) && z < cornerSize)
        {
            playerIndex = 3;
            return true;
        }

        return false;
    }
    
    private void CreateHomePaths()
    {
        int center = BoardSize / 2; // 7
        int pathStart = (BoardSize - PathWidth) / 2; // 6
        
        // Player 0 (FireNation) Home Path - Bottom
        for (int i = 1; i < pathStart; i++)
        {
            CreateTile(new Vector3(center, 0, i), playerMaterials[0], "FireNation Home Path", Tile.TileType.Home, playerTypes[0]);
        }

        // Player 1 (EarthKingdom) Home Path - Left
        for (int i = 1; i < pathStart; i++)
        {
            CreateTile(new Vector3(i, 0, center), playerMaterials[1], "EarthKingdom Home Path", Tile.TileType.Home, playerTypes[1]);
        }
        
        // Player 2 (AirNomads) Home Path - Top
        for (int i = pathStart + PathWidth; i < BoardSize - 1; i++)
        {
             CreateTile(new Vector3(center, 0, i), playerMaterials[2], "AirNomads Home Path", Tile.TileType.Home, playerTypes[2]);
        }
        
        // Player 3 (WaterTribe) Home Path - Right
        for (int i = pathStart + PathWidth; i < BoardSize - 1; i++)
        {
            CreateTile(new Vector3(i, 0, center), playerMaterials[3], "WaterTribe Home Path", Tile.TileType.Home, playerTypes[3]);
        }
    }
}
