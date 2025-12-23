using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [Header("Game Components")]
    public Board gameBoard;
    public Dice dice;

    [Header("Prefabs")]
    public GameObject pawnPrefab;

    [System.Serializable]
    public class Player
    {
        public Tile.PlayerType playerType;
        public List<Pawn> pawns;
        public List<Tile> baseTiles;
        public Tile startPathTile;
    }

    [Header("Game State")]
    public List<Player> players;
    public Tile.PlayerType currentPlayerTurn;
    private int _diceResult;

    void Start()
    {
        SetupGame();
    }

    void SetupGame()
    {
        if (gameBoard == null)
        {
            Debug.LogError("Game Board is not assigned in the GameManager.");
            return;
        }

        // 1. Generate the board visuals
        gameBoard.GenerateBoard();

        // 2. Find all tile components and sort them
        var allTiles = FindObjectsOfType<Tile>().ToList();
        
        // 3. Initialize players
        players = new List<Player>();
        var playerTypes = new[] { Tile.PlayerType.FireNation, Tile.PlayerType.EarthKingdom, Tile.PlayerType.AirNomads, Tile.PlayerType.WaterTribe };

        for (int i = 0; i < playerTypes.Length; i++)
        {
            var playerType = playerTypes[i];
            var newPlayer = new Player
            {
                playerType = playerType,
                pawns = new List<Pawn>(),
                baseTiles = allTiles.Where(t => t.owner == playerType && t.type == Tile.TileType.Base).ToList(),
                // startPathTile will be set later by finding the specific starting tile for each path
            };

            // 4. Spawn pawns for the player
            for (int j = 0; j < newPlayer.baseTiles.Count; j++)
            {
                if (j < 4) // Assuming 4 pawns per player
                {
                    Tile baseTile = newPlayer.baseTiles[j];
                    GameObject pawnObj = Instantiate(pawnPrefab, baseTile.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                    pawnObj.name = $"{playerType} Pawn {j + 1}";
                    
                    Pawn pawnComp = pawnObj.GetComponent<Pawn>();
                    if (pawnComp == null) pawnComp = pawnObj.AddComponent<Pawn>();

                    pawnComp.owner = playerType;
                    pawnComp.startTile = baseTile;
                    pawnComp.MoveToTile(baseTile);

                    // Set pawn color (requires materials on the Board)
                    Renderer pawnRenderer = pawnObj.GetComponent<Renderer>();
                    if (pawnRenderer != null && gameBoard.playerMaterials.Length > i)
                    {
                        pawnRenderer.material = gameBoard.playerMaterials[i];
                    }
                    
                    newPlayer.pawns.Add(pawnComp);
                }
            }
            players.Add(newPlayer);
        }

        // 5. Set initial turn
        currentPlayerTurn = playerTypes[0]; // Fire Nation starts
        Debug.Log($"{currentPlayerTurn} starts the game!");
    }

    /// <summary>
    /// Called by the Dice script when the roll button is clicked.
    /// </summary>
    /// <param name="result">The number rolled on the die.</param>
    public void OnDiceRolled(int result)
    {
        _diceResult = result;
        Debug.Log($"{currentPlayerTurn} rolled a {_diceResult}!");

        // Next: Implement logic for moving pawns based on the dice result.
    }

    void Update()
    {
        // Game loop will be managed here
    }
}
