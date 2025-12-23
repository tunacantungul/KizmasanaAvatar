using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        WaitingForRoll,
        WaitingForPawnSelection,
        TurnOver
    }

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
        public Tile startPathTile; // The first tile on the main path for this player
    }

    [Header("Game State")]
    public GameState currentState;
    public List<Player> players;
    public Tile.PlayerType currentPlayerTurn;
    private int _diceResult;
    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
        SetupGame();
    }

    void SetupGame()
    {
        if (gameBoard == null) {
            Debug.LogError("Game Board is not assigned in the GameManager.");
            return;
        }

        gameBoard.GenerateBoard();
        var allTiles = FindObjectsOfType<Tile>().ToList();
        
        players = new List<Player>();
        var playerTypes = new[] { Tile.PlayerType.FireNation, Tile.PlayerType.EarthKingdom, Tile.PlayerType.AirNomads, Tile.PlayerType.WaterTribe };

        for (int i = 0; i < playerTypes.Length; i++)
        {
            var pType = playerTypes[i];
            var newPlayer = new Player {
                playerType = pType,
                pawns = new List<Pawn>(),
                baseTiles = allTiles.Where(t => t.owner == pType && t.type == Tile.TileType.Base).ToList(),
                startPathTile = gameBoard.StartTiles[pType]
            };

            for (int j = 0; j < 4; j++)
            {
                Tile baseTile = newPlayer.baseTiles[j];
                GameObject pawnObj = Instantiate(pawnPrefab, baseTile.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                pawnObj.name = $"{pType} Pawn {j + 1}";

                // Ensure a collider exists for raycasting
                if (pawnObj.GetComponent<Collider>() == null)
                {
                    pawnObj.AddComponent<CapsuleCollider>();
                }
                
                Pawn pawnComp = pawnObj.AddComponent<Pawn>();
                pawnComp.owner = pType;
                pawnComp.startTile = baseTile;
                pawnComp.MoveToTile(baseTile);

                pawnObj.GetComponent<Renderer>().material = gameBoard.playerMaterials[i];
                newPlayer.pawns.Add(pawnComp);
            }
            players.Add(newPlayer);
        }

        currentPlayerTurn = playerTypes[0];
        currentState = GameState.WaitingForRoll;
        Debug.Log($"{currentPlayerTurn} starts the game! Waiting for roll...");
    }

    public void OnDiceRolled(int result)
    {
        if (currentState != GameState.WaitingForRoll) return;

        _diceResult = result;
        Debug.Log($"{currentPlayerTurn} rolled a {_diceResult}!");

        if (CanPlayerMove(currentPlayerTurn, _diceResult))
        {
            Debug.Log("Please select a pawn to move.");
            currentState = GameState.WaitingForPawnSelection;
        }
        else
        {
            Debug.Log("No available moves. Turn will pass automatically.");
            currentState = GameState.TurnOver;
            StartCoroutine(AutoEndTurnAfterDelay());
        }
    }

    private bool CanPlayerMove(Tile.PlayerType player, int diceResult)
    {
        // If you roll a 6, you can always move (either from base or on board).
        if (diceResult == 6) return true;

        // If you didn't roll a 6, you must have a pawn on the board to be able to move.
        Player currentPlayer = players.First(p => p.playerType == player);
        return currentPlayer.pawns.Any(pawn => pawn.state == Pawn.PawnState.OnBoard);
    }

    void Update()
    {
        if (currentState == GameState.WaitingForPawnSelection)
        {
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                Ray ray = _mainCamera.ScreenPointToRay(Pointer.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Pawn clickedPawn = hit.collider.GetComponent<Pawn>();
                    if (clickedPawn != null && clickedPawn.owner == currentPlayerTurn)
                    {
                        TryMovePawn(clickedPawn);
                    }
                }
            }
        }
    }

    private void TryMovePawn(Pawn pawn)
    {
        // Rule: Getting out of base
        if (pawn.state == Pawn.PawnState.Base)
        {
            if (_diceResult == 6)
            {
                Player currentPlayer = players.First(p => p.playerType == currentPlayerTurn);
                Tile targetTile = currentPlayer.startPathTile;

                CheckForCapture(targetTile, pawn);

                pawn.state = Pawn.PawnState.OnBoard;
                pawn.pathPosition = gameBoard.MainPath.IndexOf(targetTile);
                pawn.MoveToTile(targetTile);
                
                Debug.Log($"{pawn.name} moved out of base!");
                EndTurn();
            }
            else
            {
                Debug.Log("You must select a pawn on the board, or roll a 6 to move from base.");
            }
            return;
        }

        // Rule: Moving on the board
        if (pawn.state == Pawn.PawnState.OnBoard)
        {
            int newPathPosition = pawn.pathPosition + _diceResult;

            // Handle path wrapping (for now, ignoring home path entry)
            if (newPathPosition >= gameBoard.MainPath.Count)
            {
                newPathPosition -= gameBoard.MainPath.Count;
            }

            Tile targetTile = gameBoard.MainPath[newPathPosition];

            CheckForCapture(targetTile, pawn);

            pawn.pathPosition = newPathPosition;
            pawn.MoveToTile(targetTile);
            
            Debug.Log($"{pawn.name} moved to tile {newPathPosition}");
            EndTurn();
        }
    }

    private void CheckForCapture(Tile targetTile, Pawn movingPawn)
    {
        var allPawns = FindObjectsOfType<Pawn>();
        Pawn pawnOnTarget = allPawns.FirstOrDefault(p => p.currentTile == targetTile && p != movingPawn);

        if (pawnOnTarget != null && pawnOnTarget.owner != movingPawn.owner)
        {
            Debug.Log($"Capture! {movingPawn.name} captured {pawnOnTarget.name}!");
            pawnOnTarget.state = Pawn.PawnState.Base;
            pawnOnTarget.pathPosition = -1;
            pawnOnTarget.MoveToTile(pawnOnTarget.startTile);
        }
    }

    private IEnumerator AutoEndTurnAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        EndTurn();
    }

    private void EndTurn()
    {
        int currentPlayerIndex = (int)currentPlayerTurn - 1;
        int nextPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        currentPlayerTurn = (Tile.PlayerType)(nextPlayerIndex + 1);

        currentState = GameState.WaitingForRoll;
        Debug.Log($"Turn ended. It's now {currentPlayerTurn}'s turn. Waiting for roll...");
    }
}


