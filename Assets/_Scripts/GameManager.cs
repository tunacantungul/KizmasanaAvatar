using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.InputSystem;

[System.Serializable]
public class Player {
    public Tile.PlayerType playerType;
    public List<Pawn> pawns;
    public List<Tile> baseTiles;
    public Tile startPathTile;
}

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        WaitingForRoll,
        WaitingForPawnSelection,
        TurnOver,
        GameFinished
    }

    [Header("Game Components")]
    public Board gameBoard;
    public Dice dice;
    public UIManager uiManager;

    [Header("Prefabs")]
    public GameObject pawnPrefab;

    [Header("Game State")]
    public GameState currentState;
    public List<Player> players;
    public Tile.PlayerType currentPlayerTurn;
    public bool isAutoPlayActive = false;

    private int _diceResult;
    private Camera _mainCamera;
    private Dictionary<Tile.PlayerType, int> _homeEntryIndices;

    // --- System Methods ---

    void Start()
    {
        _mainCamera = Camera.main;
        SetupGame();
    }

    void Update()
    {
        if (!isAutoPlayActive && currentState == GameState.WaitingForPawnSelection && Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Pointer.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hit))
            {
                var clickedPawn = hit.collider.GetComponent<Pawn>();
                if (clickedPawn != null && clickedPawn.owner == currentPlayerTurn) TryMovePawn(clickedPawn);
            }
        }
    }

    // --- Game Flow ---

    void SetupGame()
    {
        if (gameBoard == null) { Debug.LogError("Game Board is not assigned."); return; }
        if (uiManager == null) { Debug.LogError("UI Manager is not assigned."); return; }

        gameBoard.GenerateBoard();
        var allTiles = FindObjectsOfType<Tile>().ToList();
        
        _homeEntryIndices = new Dictionary<Tile.PlayerType, int> {
            { Tile.PlayerType.FireNation, 51 }, { Tile.PlayerType.EarthKingdom, 12 },
            { Tile.PlayerType.AirNomads, 25 }, { Tile.PlayerType.WaterTribe, 38 }
        };
        
        players = new List<Player>();
        var playerTypes = new[] { Tile.PlayerType.FireNation, Tile.PlayerType.EarthKingdom, Tile.PlayerType.AirNomads, Tile.PlayerType.WaterTribe };

        for (int i = 0; i < playerTypes.Length; i++) {
            var pType = playerTypes[i];
            var newPlayer = new Player { 
                playerType = pType, 
                pawns = new List<Pawn>(), 
                baseTiles = allTiles.Where(t => t.owner == pType && t.type == Tile.TileType.Base).OrderBy(t => t.z).ThenBy(t => t.x).ToList(), 
                startPathTile = gameBoard.StartTiles[pType] 
            };
            for (int j = 0; j < 4; j++) {
                var pawnObj = Instantiate(pawnPrefab, newPlayer.baseTiles[j].transform.position + Vector3.up * 0.5f, Quaternion.identity);
                pawnObj.name = $"{pType} Pawn {j + 1}";
                if (pawnObj.GetComponent<Collider>() == null) pawnObj.AddComponent<CapsuleCollider>();
                var pawnComp = pawnObj.AddComponent<Pawn>();
                pawnComp.owner = pType;
                pawnComp.startTile = newPlayer.baseTiles[j];
                pawnComp.MoveToTile(newPlayer.baseTiles[j]);
                pawnObj.GetComponent<Renderer>().material = gameBoard.playerMaterials[i];
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

        if (CanPlayerMove(currentPlayerTurn, _diceResult)) {
            currentState = GameState.WaitingForPawnSelection;
            if (isAutoPlayActive) {
                StartCoroutine(AI_ChooseAndMovePawn());
            } else {
                uiManager.UpdateStatus($"Sıra: {currentPlayerTurn}\nLütfen hareket ettirmek için bir piyon seç.");
            }
        } else {
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

        if (isAutoPlayActive) {
            StartCoroutine(AutoPlayTurn());
        }
    }

    // --- Movement and Rules Logic ---

    private void TryMovePawn(Pawn pawn)
    {
        switch (pawn.state)
        {
            case Pawn.PawnState.Base:
                if (_diceResult == 6)
                {
                    var targetTile = players.First(p => p.playerType == currentPlayerTurn).startPathTile;
                    CheckForCapture(targetTile, pawn);
                    pawn.state = Pawn.PawnState.OnBoard;
                    pawn.pathPosition = gameBoard.MainPath.IndexOf(targetTile);
                    pawn.MoveToTile(targetTile);
                    EndTurn();
                }
                else
                {
                    uiManager.UpdateStatus("Bu piyonu çıkarmak için 6 atmalısın.");
                }
                break;

            case Pawn.PawnState.OnBoard:
                int homeEntryIndex = _homeEntryIndices[pawn.owner];
                int currentPos = pawn.pathPosition;
                int nextPosUnwrapped = currentPos + _diceResult;

                bool isPassingHome = false;
                // Check for passing home only if the player is on their final board quadrant
                int playerIndex = (int)pawn.owner - 1;
                int startOfPlayerQuadrant = (playerIndex * 13);
                if(currentPos >= startOfPlayerQuadrant) {
                    isPassingHome = currentPos <= homeEntryIndex && nextPosUnwrapped > homeEntryIndex;
                }

                if (isPassingHome)
                {
                    int stepsIntoHome = nextPosUnwrapped - homeEntryIndex - 1;
                    var homePath = gameBoard.HomePaths[playerIndex];
                    if (stepsIntoHome < homePath.Count)
                    {
                        pawn.state = Pawn.PawnState.InHomePath;
                        pawn.pathPosition = stepsIntoHome;
                        pawn.MoveToTile(homePath[stepsIntoHome]);
                        EndTurn();
                    }
                    else
                    {
                        uiManager.UpdateStatus("Hamle geçersiz, eve girmek için tam zar atmalısın.");
                    }
                }
                else
                {
                    int nextPos = (pawn.pathPosition + _diceResult) % gameBoard.MainPath.Count;
                    var newTargetTile = gameBoard.MainPath[nextPos];
                    CheckForCapture(newTargetTile, pawn);
                    pawn.pathPosition = nextPos;
                    pawn.MoveToTile(newTargetTile);
                    EndTurn();
                }
                break;

            case Pawn.PawnState.InHomePath:
                var currentPlayerHomePath = gameBoard.HomePaths[(int)pawn.owner - 1];
                int nextHomePos = pawn.pathPosition + _diceResult;

                if (nextHomePos < currentPlayerHomePath.Count)
                {
                    pawn.pathPosition = nextHomePos;
                    pawn.MoveToTile(currentPlayerHomePath[nextHomePos]);
                    EndTurn();
                }
                else if (nextHomePos == currentPlayerHomePath.Count) 
                {
                    // This is an invalid move, you must land ON the last tile, not after it.
                    // The actual finish is when you land on the last tile of homepath.
                    uiManager.UpdateStatus("Eve girmek için tam zar atmalısın.");
                }
                else if (nextHomePos == currentPlayerHomePath.Count -1) // Exactly reached the goal tile of homepath
                {
                    pawn.state = Pawn.PawnState.Finished;
                    pawn.MoveToTile(gameBoard.GoalTile);
                    CheckForWin();
                    EndTurn();
                }
                else
                {
                    uiManager.UpdateStatus("Eve girmek için tam zar atmalısın.");
                }
                break;
        }
    }

    private bool CanPlayerMove(Tile.PlayerType playerType, int diceResult)
    {
        var player = players.First(p => p.playerType == playerType);
        if (player.pawns.All(p => p.state == Pawn.PawnState.Finished)) return false;
        if (diceResult == 6) return true;
        foreach (var pawn in player.pawns.Where(p => p.state != Pawn.PawnState.Base && p.state != Pawn.PawnState.Finished)) {
            if (pawn.state == Pawn.PawnState.OnBoard) return true;
            if (pawn.state == Pawn.PawnState.InHomePath) {
                if (pawn.pathPosition + diceResult <= gameBoard.HomePaths[(int)playerType - 1].Count) return true;
            }
        }
        return false;
    }

    private void CheckForCapture(Tile targetTile, Pawn movingPawn)
    {
        var pawnOnTarget = players.SelectMany(p => p.pawns).FirstOrDefault(p => p.currentTile == targetTile && p.owner != movingPawn.owner);
        if (pawnOnTarget != null) {
            pawnOnTarget.state = Pawn.PawnState.Base;
            pawnOnTarget.pathPosition = -1;
            pawnOnTarget.MoveToTile(pawnOnTarget.startTile);
        }
    }

    private void CheckForWin()
    {
        var winner = players.FirstOrDefault(p => p.pawns.All(pawn => pawn.state == Pawn.PawnState.Finished));
        if (winner != null) {
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
        if (isActive && currentState == GameState.WaitingForRoll) {
            StartCoroutine(AutoPlayTurn());
        }
    }

    private IEnumerator AutoPlayTurn()
    {
        yield return new WaitForSeconds(0.75f);
        if (currentState == GameState.WaitingForRoll) {
            dice.RollForButton();
        }
    }

    private IEnumerator AI_ChooseAndMovePawn()
    {
        yield return new WaitForSeconds(0.75f);
        var pawnToMove = AI_FindBestPawnToMove();
        if (pawnToMove != null) {
            TryMovePawn(pawnToMove);
        } else {
            // This should not happen if CanPlayerMove is correct, but as a fallback:
            EndTurn();
        }
    }

    private Pawn AI_FindBestPawnToMove()
    {
        var player = players.First(p => p.playerType == currentPlayerTurn);
        var movablePawns = player.pawns.Where(p => IsMoveValid(p, _diceResult)).ToList();
        
        // Simple AI Priorities:
        // 1. Capture a pawn
        // 2. Finish a pawn
        // 3. Enter home path
        // 4. Move from base
        // 5. Move furthest pawn
        
        Pawn bestPawn = null;
        int bestScore = -1;

        foreach (var pawn in movablePawns) {
            int score = 0;
            if (pawn.state == Pawn.PawnState.OnBoard) {
                int nextPos = pawn.pathPosition + _diceResult;
                if (nextPos >= gameBoard.MainPath.Count) nextPos -= gameBoard.MainPath.Count;
                var targetTile = gameBoard.MainPath[nextPos];
                if (players.SelectMany(p => p.pawns).Any(op => op.currentTile == targetTile && op.owner != pawn.owner)) score = 100; // Capture
                
                int homeEntryIndex = _homeEntryIndices[pawn.owner];
                 if ((pawn.pathPosition <= homeEntryIndex && nextPos > homeEntryIndex) || (pawn.pathPosition > nextPos && pawn.pathPosition <= homeEntryIndex)) score = 90; // Enter home
            } else if (pawn.state == Pawn.PawnState.InHomePath) {
                 var homePath = gameBoard.HomePaths[(int)pawn.owner - 1];
                 if (pawn.pathPosition + _diceResult == homePath.Count) score = 110; // Finish pawn
            }

            if (_diceResult == 6 && pawn.state == Pawn.PawnState.Base) score = 80; // Move from base
            
            score += pawn.pathPosition; // Prefer moving pawns further along

            if (score > bestScore) {
                bestScore = score;
                bestPawn = pawn;
            }
        }
        return bestPawn ?? movablePawns.FirstOrDefault();
    }

    private bool IsMoveValid(Pawn pawn, int diceResult)
    {
        if (pawn.state == Pawn.PawnState.Finished) return false;
        if (pawn.state == Pawn.PawnState.Base) return diceResult == 6;
        if (pawn.state == Pawn.PawnState.InHomePath) {
            return pawn.pathPosition + diceResult <= gameBoard.HomePaths[(int)pawn.owner - 1].Count;
        }
        return true;
    }

    private IEnumerator AutoEndTurnAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        EndTurn();
    }
    
    private void UpdateScoreboard()
    {
        var sb = new StringBuilder("Skor:\n");
        foreach (var p in players) {
            sb.AppendLine($"{p.playerType}: {p.pawns.Count(pawn => pawn.state == Pawn.PawnState.Finished)} / 4");
        }
        uiManager.UpdateScore(sb.ToString());
    }
}