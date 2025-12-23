using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Board : MonoBehaviour
{
    [Header("Tile Assets")]
    public GameObject tilePrefab;
    public Material baseMaterial;
    public Material pathMaterial;
    public Material[] playerMaterials; // 0: Fire, 1: Earth, 2: Air, 3: Water

    // Pathing Data
    public List<Tile> MainPath { get; private set; }
    public List<List<Tile>> HomePaths { get; private set; }
    public Dictionary<Tile.PlayerType, Tile> StartTiles { get; private set; }

    private readonly Tile.PlayerType[] playerTypes = {
        Tile.PlayerType.FireNation, Tile.PlayerType.EarthKingdom, Tile.PlayerType.AirNomads, Tile.PlayerType.WaterTribe
    };

    private Dictionary<Vector2Int, Tile> _tileMap;

    // Board layout constants
    private const int BoardSize = 15;
    private const int PathWidth = 3;

    public void GenerateBoard()
    {
        // Initialize collections
        _tileMap = new Dictionary<Vector2Int, Tile>();
        MainPath = new List<Tile>(52);
        HomePaths = new List<List<Tile>> { new List<Tile>(), new List<Tile>(), new List<Tile>(), new List<Tile>() };
        StartTiles = new Dictionary<Tile.PlayerType, Tile>();

        // Destroy previous board if any
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        CreateBoardLayout();
        PopulatePaths();
    }

    private void CreateBoardLayout()
    {
        for (int x = 0; x < BoardSize; x++) {
            for (int z = 0; z < BoardSize; z++) {
                var pos = new Vector3(x, 0, z);
                var coord = new Vector2Int(x, z);

                if (IsPath(x, z)) {
                    CreateTile(pos, pathMaterial, $"Path {x},{z}", Tile.TileType.Path, Tile.PlayerType.None);
                } else if (IsPlayerBase(x, z, out int playerIndex)) {
                    var playerType = playerTypes[playerIndex];
                    CreateTile(pos, playerMaterials[playerIndex], $"{playerType} Base", Tile.TileType.Base, playerType);
                }
            }
        }
        CreateHomePathsAndGoal();
    }

    private void CreateHomePathsAndGoal()
    {
        int center = BoardSize / 2;
        int pathStart = (BoardSize - PathWidth) / 2;

        for (int p = 0; p < 4; p++) {
            var playerType = playerTypes[p];
            for (int i = 1; i < pathStart; i++) {
                Vector3 pos;
                if (p == 0) pos = new Vector3(center, 0, i); // Fire
                else if (p == 1) pos = new Vector3(i, 0, center); // Earth
                else if (p == 2) pos = new Vector3(center, 0, BoardSize - 1 - i); // Air
                else pos = new Vector3(BoardSize - 1 - i, 0, center); // Water

                CreateTile(pos, playerMaterials[p], $"{playerType} Home Path", Tile.TileType.Home, playerType);
            }
        }
        CreateTile(new Vector3(center, 0, center), baseMaterial, "Goal", Tile.TileType.Goal, Tile.PlayerType.None);
    }

    private Tile CreateTile(Vector3 position, Material material, string name, Tile.TileType type, Tile.PlayerType owner)
    {
        var tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
        tileObj.name = name;
        tileObj.GetComponent<Renderer>().material = material;

        var tileComp = tileObj.AddComponent<Tile>();
        tileComp.type = type;
        tileComp.owner = owner;
        tileComp.x = (int)position.x;
        tileComp.z = (int)position.z;

        _tileMap[new Vector2Int(tileComp.x, tileComp.z)] = tileComp;
        return tileComp;
    }

    private void PopulatePaths()
    {
        // Manually define the Ludo path sequence based on board coordinates
        var pathCoords = new List<Vector2Int> {
            // Fire Path Segment
            new Vector2Int(6, 1), new Vector2Int(6, 2), new Vector2Int(6, 3), new Vector2Int(6, 4), new Vector2Int(6, 5),
            new Vector2Int(5, 6), new Vector2Int(4, 6), new Vector2Int(3, 6), new Vector2Int(2, 6), new Vector2Int(1, 6), new Vector2Int(0, 6),
            new Vector2Int(0, 7),
            // Earth Path Segment
            new Vector2Int(1, 8), new Vector2Int(2, 8), new Vector2Int(3, 8), new Vector2Int(4, 8), new Vector2Int(5, 8),
            new Vector2Int(6, 9), new Vector2Int(6, 10), new Vector2Int(6, 11), new Vector2Int(6, 12), new Vector2Int(6, 13), new Vector2Int(6, 14),
            new Vector2Int(7, 14),
            // Air Path Segment
            new Vector2Int(8, 13), new Vector2Int(8, 12), new Vector2Int(8, 11), new Vector2Int(8, 10), new Vector2Int(8, 9),
            new Vector2Int(9, 8), new Vector2Int(10, 8), new Vector2Int(11, 8), new Vector2Int(12, 8), new Vector2Int(13, 8), new Vector2Int(14, 8),
            new Vector2Int(14, 7),
            // Water Path Segment
            new Vector2Int(13, 6), new Vector2Int(12, 6), new Vector2Int(11, 6), new Vector2Int(10, 6), new Vector2Int(9, 6),
            new Vector2Int(8, 5), new Vector2Int(8, 4), new Vector2Int(8, 3), new Vector2Int(8, 2), new Vector2Int(8, 1), new Vector2Int(8, 0),
            new Vector2Int(7, 0)
        };

        foreach (var coord in pathCoords) {
            MainPath.Add(_tileMap[coord]);
        }

        // Define Start Tiles and Home Paths from the map
        StartTiles[Tile.PlayerType.FireNation] = _tileMap[new Vector2Int(6, 1)];
        StartTiles[Tile.PlayerType.EarthKingdom] = _tileMap[new Vector2Int(1, 8)];
        StartTiles[Tile.PlayerType.AirNomads] = _tileMap[new Vector2Int(8, 13)];
        StartTiles[Tile.PlayerType.WaterTribe] = _tileMap[new Vector2Int(13, 6)];

        HomePaths[0] = GetPath(new Vector2Int(7,1), new Vector2Int(7,6), true).OrderBy(t => t.z).ToList(); // Fire
        HomePaths[1] = GetPath(new Vector2Int(1,7), new Vector2Int(6,7), true).OrderBy(t => t.x).ToList(); // Earth
        HomePaths[2] = GetPath(new Vector2Int(7,9), new Vector2Int(7,14), true).OrderByDescending(t => t.z).ToList(); // Air
        HomePaths[3] = GetPath(new Vector2Int(9,7), new Vector2Int(14,7), true).OrderByDescending(t => t.x).ToList(); // Water
    }

    private List<Tile> GetPath(Vector2Int start, Vector2Int end, bool isHomePath)
    {
        return _tileMap.Values.Where(t => 
            t.x >= start.x && t.x <= end.x && 
            t.z >= start.y && t.z <= end.y &&
            (!isHomePath || t.type == Tile.TileType.Home)
        ).ToList();
    }

    private bool IsPath(int x, int z) {
        int cs = (BoardSize - PathWidth) / 2; // centerStart
        int ce = cs + PathWidth - 1; // centerEnd
        return (x >= cs && x <= ce) || (z >= cs && z <= ce);
    }

    private bool IsPlayerBase(int x, int z, out int playerIndex) {
        int cornerSize = (BoardSize - PathWidth) / 2;
        playerIndex = -1;
        if (x < cornerSize && z < cornerSize) { playerIndex = 0; return true; } // Fire
        if (x < cornerSize && z > (cornerSize + PathWidth -1)) { playerIndex = 1; return true; } // Earth
        if (x > (cornerSize + PathWidth -1) && z > (cornerSize + PathWidth-1)) { playerIndex = 2; return true; } // Air
        if (x > (cornerSize + PathWidth -1) && z < cornerSize) { playerIndex = 3; return true; } // Water
        return false;
    }
}
