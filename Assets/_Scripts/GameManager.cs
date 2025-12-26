using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.InputSystem;
using System;

[System.Serializable]
public class Player
{
    public Tile.PlayerType playerType;
    public List<Pawn> pawns;
    public List<Tile> baseTiles;
    public Tile startPathTile;
}

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Initializing,
        WaitingForRoll,
        PawnMoving,
        WaitingForPawnSelection,
        TurnOver,
        GameFinished
    }

    [Header("Game Components")]
    public Board gameBoard;
    public Dice dice;
    public UIManager uiManager;

    [Header("Prefabs & Materials")]
    public GameObject pawnPrefab;
    public Material[] playerMaterials; // 0: Fire, 1: Earth, 2: Air, 3: Water

    [Header("Game State")]
    public GameState currentState;
    public List<Player> players;
    public Tile.PlayerType currentPlayerTurn;
    public bool isAutoPlayActive = false;

    private int _diceResult;
    private Camera _mainCamera;
    
    // --- System Methods ---

    void Start()
    {
        _mainCamera = Camera.main;
        currentState = GameState.Initializing;
        StartCoroutine(SetupGame());
    }

    void Update()
    {
        // Allow pawn selection only if it's the player's turn and they are not an AI
        if (!isAutoPlayActive && currentState == GameState.WaitingForPawnSelection && Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Pointer.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hit))
            {
                var clickedPawn = hit.collider.GetComponent<Pawn>();
                if (clickedPawn != null && clickedPawn.owner == currentPlayerTurn)
                {
                    TryMovePawn(clickedPawn);
                }
            }
        }
    }

    // --- Game Flow ---

    IEnumerator SetupGame()
    {
        if (gameBoard == null) { Debug.LogError("Game Board is not assigned."); yield break; }
        if (uiManager == null) { Debug.LogError("UI Manager is not assigned."); yield break; }
        
        // Board now initializes itself in Awake(), we just need to wait a frame to be sure it's ready.
        yield return null; 

        // Find all tiles once, as Board.cs has already organized them
        var allTiles = FindObjectsOfType<Tile>().ToList();

        players = new List<Player>();
        var playerTypes = new[] { Tile.PlayerType.FireNation, Tile.PlayerType.EarthKingdom, Tile.PlayerType.AirNomads, Tile.PlayerType.WaterTribe };

        for (int i = 0; i < playerTypes.Length; i++)
        {
            var pType = playerTypes[i];
            var newPlayer = new Player
            {
                playerType = pType,
                pawns = new List<Pawn>(),
                baseTiles = allTiles.Where(t => t.owner == pType && t.type == Tile.TileType.Base).OrderBy(t => t.z).ThenBy(t => t.x).ToList(),
                startPathTile = gameBoard.StartTiles.ContainsKey(pType) ? gameBoard.StartTiles[pType] : null
            };

            if (newPlayer.startPathTile == null) {
                Debug.LogError($"Start tile for {pType} is not set in Board's inspector!");
            }

            for (int j = 0; j < 4; j++)
            {
                var pawnObj = Instantiate(pawnPrefab, newPlayer.baseTiles[j].transform.position + Vector3.up * 0.5f, Quaternion.identity);
                pawnObj.name = $"{pType} Pawn {j + 1}";
                if (pawnObj.GetComponent<Collider>() == null) pawnObj.AddComponent<CapsuleCollider>();
                
                var pawnComp = pawnObj.AddComponent<Pawn>();
                pawnComp.owner = pType;
                pawnComp.state = Pawn.PawnState.Base;
                pawnComp.PlaceOnTile(newPlayer.baseTiles[j]); // Use new placement method
                
                pawnObj.GetComponent<Renderer>().material = playerMaterials[i];
                newPlayer.pawns.Add(pawnComp);
            }
            players.Add(newPlayer);
        }

        currentPlayerTurn = playerTypes[0];
        currentState = GameState.WaitingForRoll;
        uiManager.UpdateStatus($"Sıra: {currentPlayerTurn}\nZar atmak için butona tıkla.");
        UpdateScoreboard();
    }

    public void OnDiceRolled(int result)
    {
        if (currentState != GameState.WaitingForRoll) return;
        _diceResult = result;
        uiManager.UpdateDiceResult("Zar: " + _diceResult);

        if (CanPlayerMove(currentPlayerTurn, _diceResult))
        {
            currentState = GameState.WaitingForPawnSelection;
            if (isAutoPlayActive)
            {
                StartCoroutine(AI_ChooseAndMovePawn());
            }
            else
            {
                uiManager.UpdateStatus($"Sıra: {currentPlayerTurn}\nLütfen hareket ettirmek için bir piyon seç.");
            }
        }
        else
        {
            uiManager.UpdateStatus($"Sıra: {currentPlayerTurn}\nGeçerli hamle yok, sıra atlanıyor...");
            currentState = GameState.TurnOver;
            StartCoroutine(AutoEndTurnAfterDelay());
        }
    }

    private void EndTurn()
    {
        if (currentState == GameState.GameFinished) return;
        
        UpdateScoreboard();

        int currentPlayerIndex = (int)currentPlayerTurn - 1;
        int nextPlayerIndex = (_diceResult == 6) ? currentPlayerIndex : (currentPlayerIndex + 1) % players.Count;

        currentPlayerTurn = (Tile.PlayerType)(nextPlayerIndex + 1);
        currentState = GameState.WaitingForRoll;
        uiManager.UpdateStatus($"Sıra: {currentPlayerTurn}\nZar atmak için butona tıkla.");

        if (isAutoPlayActive)
        {
            StartCoroutine(AutoPlayTurn());
        }
    }

    // --- Movement and Rules Logic ---

    private void TryMovePawn(Pawn pawn)
    {
        if (currentState != GameState.WaitingForPawnSelection) return;

        Action onMoveComplete = () => { EndTurn(); };

        switch (pawn.state)
        {
            case Pawn.PawnState.Base:
                if (_diceResult == 6)
                {
                    currentState = GameState.PawnMoving;
                    var targetTile = players.First(p => p.playerType == currentPlayerTurn).startPathTile;
                    CheckForCapture(targetTile, pawn);
                    pawn.state = Pawn.PawnState.OnBoard;
                    pawn.PlaceOnTile(targetTile); // Instant move from base
                    onMoveComplete();
                }
                else
                {
                    uiManager.UpdateStatus("Bu piyonu çıkarmak için 6 atmalısın.");
                }
                break;

            case Pawn.PawnState.OnBoard:
                // TODO: Add logic for entering home path. For now, we move along the main path.
                int targetTileID = pawn.currentTileID + _diceResult;
                var destinationTile = gameBoard.GetTile(targetTileID);

                if (destinationTile != null)
                {
                    currentState = GameState.PawnMoving;
                    CheckForCapture(destinationTile, pawn);
                    pawn.Move(gameBoard, _diceResult, onMoveComplete);
                }
                else
                {
                    uiManager.UpdateStatus("Hamle geçersiz. Yolun sonuna ulaştın.");
                }
                break;

            case Pawn.PawnState.InHomePath:
                 // TODO: Implement home path movement
                uiManager.UpdateStatus("Evdeki piyonların hareketi henüz kodlanmadı.");
                break;
        }
    }
    
    private bool CanPlayerMove(Tile.PlayerType playerType, int diceResult)
    {
        var player = players.First(p => p.playerType == playerType);
        if (player.pawns.All(p => p.state == Pawn.PawnState.Finished)) return false;

        // If a 6 is rolled, a pawn can always move from base (if any are there)
        if (diceResult == 6 && player.pawns.Any(p => p.state == Pawn.PawnState.Base)) return true;
        
        // Check if any pawns on the board can move
        foreach (var pawn in player.pawns.Where(p => p.state == Pawn.PawnState.OnBoard))
        {
            int targetTileID = pawn.currentTileID + diceResult;
            // Basic check: is the target tile valid? More complex logic needed for home paths.
            if (gameBoard.GetTile(targetTileID) != null) return true;
        }

        // TODO: Add check for pawns in home path
        return false;
    }


    private void CheckForCapture(Tile targetTile, Pawn movingPawn)
    {
        // Find any pawn that is not owned by the current player and is on the target tile
        var pawnOnTarget = players.SelectMany(p => p.pawns)
                                  .FirstOrDefault(p => p.currentTile == targetTile && 
                                                       p.owner != movingPawn.owner && 
                                                       p.state == Pawn.PawnState.OnBoard);

        if (pawnOnTarget != null)
        {
            var ownerPlayer = players.First(p => p.playerType == pawnOnTarget.owner);
            // Find an empty base tile for the captured pawn
            var emptyBaseTile = ownerPlayer.baseTiles.FirstOrDefault(bt => 
                !ownerPlayer.pawns.Any(p => p.currentTile == bt && p != pawnOnTarget)
            );

            if (emptyBaseTile != null)
            {
                pawnOnTarget.state = Pawn.PawnState.Base;
                pawnOnTarget.PlaceOnTile(emptyBaseTile);
                Debug.Log($"{movingPawn.owner}'s pawn captured {pawnOnTarget.owner}'s pawn!");
            }
        }
    }

    private void CheckForWin()
    {
        var winner = players.FirstOrDefault(p => p.pawns.All(pawn => pawn.state == Pawn.PawnState.Finished));
        if (winner != null)
        {
            currentState = GameState.GameFinished;
            uiManager.UpdateStatus($"{winner.playerType} oyunu kazandı! Tebrikler!");
            dice.rollButton.interactable = false;
            isAutoPlayActive = false;
        }
    }

    // --- Auto-Play AI ---

    public void SetAutoPlay(bool isActive)
    {
        isAutoPlayActive = isActive;
        if (isActive && currentState == GameState.WaitingForRoll)
        {
            StartCoroutine(AutoPlayTurn());
        }
    }

    private IEnumerator AutoPlayTurn()
    {
        yield return new WaitForSeconds(0.75f);
        if (currentState == GameState.WaitingForRoll)
        {
            dice.RollForButton();
        }
    }

    private IEnumerator AI_ChooseAndMovePawn()
    {
        yield return new WaitForSeconds(0.75f);
        var pawnToMove = AI_FindBestPawnToMove();
        if (pawnToMove != null)
        {
            TryMovePawn(pawnToMove);
        }
        else
        {
            EndTurn();
        }
    }

    private Pawn AI_FindBestPawnToMove()
    {
        // AI logic needs to be updated to be compatible with the new systems.
        // For now, let's just find the first valid move.
        var player = players.First(p => p.playerType == currentPlayerTurn);
        
        // Priority 1: Move pawn from base with a 6
        if (_diceResult == 6)
        {
            var pawnInBase = player.pawns.FirstOrDefault(p => p.state == Pawn.PawnState.Base);
            if (pawnInBase != null) return pawnInBase;
        }
        
        // Priority 2: Move any pawn on the board
        return player.pawns.FirstOrDefault(p => p.state == Pawn.PawnState.OnBoard);
    }

    private IEnumerator AutoEndTurnAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        EndTurn();
    }

    private void UpdateScoreboard()
    {
        var sb = new StringBuilder("Skor:\n");
        foreach (var p in players)
        {
            sb.AppendLine($"{p.playerType}: {p.pawns.Count(pawn => pawn.state == Pawn.PawnState.Finished)} / 4");
        }
        uiManager.UpdateScore(sb.ToString());
    }
}