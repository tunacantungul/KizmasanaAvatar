using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
        GameFinished,
        Minigame
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
    public bool isAutoPlayActive = false; // For player's choice, starts as OFF

    private int _diceResult;
    private Camera _mainCamera;
    private Dictionary<Tile.PlayerType, int> _homeEntryIndices;
    private bool _isRestoring = false;
    private int _consecutiveSixes = 0;

    // --- System Methods ---

    void Awake()
    {
        _mainCamera = Camera.main;
    }

    void Start()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.stateData.hasState)
        {
            _isRestoring = true;
            RestoreGameState();
        }
        else
        {
            if (dice == null) Debug.LogError("DICE IS NOT ASSIGNED IN THE INSPECTOR on GameManager!");
            if (uiManager == null) Debug.LogError("UI MANAGER IS NOT ASSIGNED IN THE INSPECTOR on GameManager!");
            if (gameBoard == null) Debug.LogError("GAME BOARD IS NOT ASSIGNED IN THE INSPECTOR on GameManager!");
            SetupGame();
        }
    }

    void Update()
    {
        if (_isRestoring || currentState != GameState.WaitingForPawnSelection) return;

        // Player manual input is only for WaterTribe when auto play is off
        if (currentPlayerTurn == Tile.PlayerType.WaterTribe && !isAutoPlayActive && Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
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

    void BasicSetup()
    {
        gameBoard.GenerateBoard();
        var allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None).ToList();
        
        _homeEntryIndices = new Dictionary<Tile.PlayerType, int> {
            { Tile.PlayerType.FireNation, 51 }, { Tile.PlayerType.EarthKingdom, 12 },
            { Tile.PlayerType.AirNomads, 25 }, { Tile.PlayerType.WaterTribe, 38 }
        };
        
        players = new List<Player>();
        var playerTypes = new[] { Tile.PlayerType.FireNation, Tile.PlayerType.EarthKingdom, Tile.PlayerType.AirNomads, Tile.PlayerType.WaterTribe };
        
        for (int i = 0; i < playerTypes.Length; i++) {
            var pType = playerTypes[i];
            players.Add(new Player { 
                playerType = pType, 
                pawns = new List<Pawn>(), 
                baseTiles = allTiles.Where(t => t.owner == pType && t.type == Tile.TileType.Base).OrderBy(t => t.z).ThenBy(t => t.x).ToList(), 
                startPathTile = gameBoard.StartTiles[pType] 
            });
        }
    }

    void SetupGame()
    {
        BasicSetup();
        
        for (int i = 0; i < players.Count; i++) {
            var player = players[i];
            for (int j = 0; j < 4; j++) {
                var pawnObj = Instantiate(pawnPrefab, player.baseTiles[j].transform.position + Vector3.up * 0.5f, Quaternion.identity);
                pawnObj.name = $"{player.playerType} Pawn {j + 1}";
                 if (pawnObj.GetComponent<Collider>() == null) pawnObj.AddComponent<CapsuleCollider>();
                var pawnComp = pawnObj.AddComponent<Pawn>();
                pawnComp.owner = player.playerType;
                pawnComp.pawnId = j;
                pawnComp.startTile = player.baseTiles[j];
                pawnComp.MoveToTile(player.baseTiles[j]);
                pawnComp.hasCompletedLap = false; // RULE: Must be false at start
                pawnObj.GetComponent<Renderer>().material = gameBoard.playerMaterials[i];
                player.pawns.Add(pawnComp);
            }
        }

        currentPlayerTurn = Tile.PlayerType.FireNation;
        currentState = GameState.WaitingForRoll;
        uiManager.UpdateStatus($"Sıra: {currentPlayerTurn}\nZar atmak için butona tıkla.");
        UpdateScoreboard();
        
        HandleTurnStart();
    }

    void RestoreGameState()
    {
        BasicSetup();
        var state = GameStateManager.Instance.stateData;

        // Create pawns based on saved state
        foreach(var pawnData in state.pawnsState)
        {
            var player = players[pawnData.pawnOwnerPlayerIndex];
            var pawnObj = Instantiate(pawnPrefab, Vector3.zero, Quaternion.identity); 
            pawnObj.name = $"{player.playerType} Pawn {pawnData.pawnId + 1}";
            if (pawnObj.GetComponent<Collider>() == null) pawnObj.AddComponent<CapsuleCollider>();
            var pawnComp = pawnObj.AddComponent<Pawn>();
            pawnComp.owner = player.playerType;
            pawnComp.pawnId = pawnData.pawnId;
            pawnComp.state = pawnData.state;
            pawnComp.pathPosition = pawnData.pathPosition;
            pawnComp.hasCompletedLap = pawnData.hasCompletedLap; // RULE: Restore lap status
            pawnComp.startTile = player.baseTiles[pawnData.pawnId];
            pawnObj.GetComponent<Renderer>().material = gameBoard.playerMaterials[pawnData.pawnOwnerPlayerIndex];
            player.pawns.Add(pawnComp);
        }

        // Handle minigame result
        if (GameStateManager.Instance.challenger != null && GameStateManager.Instance.defender != null)
        {
            var challengerData = GameStateManager.Instance.challenger;
            var defenderData = GameStateManager.Instance.defender;
            
            var winnerOwnerIndex = (int)GameStateManager.Instance.minigameWinner - 1;
            
            PawnStateData winnerData = winnerOwnerIndex == challengerData.pawnOwnerPlayerIndex ? challengerData : defenderData;
            PawnStateData loserData = winnerData == challengerData ? defenderData : challengerData;

            Pawn actualWinner = players[winnerData.pawnOwnerPlayerIndex].pawns.First(p => p.pawnId == winnerData.pawnId);
            Pawn actualLoser = players[loserData.pawnOwnerPlayerIndex].pawns.First(p => p.pawnId == loserData.pawnId);

            actualLoser.state = Pawn.PawnState.Base;
            actualLoser.pathPosition = -1;
            actualLoser.hasCompletedLap = false; // RULE: Reset lap status
            
            actualWinner.state = challengerData.state;
            actualWinner.pathPosition = challengerData.pathPosition;
            actualWinner.hasCompletedLap = challengerData.hasCompletedLap;
        }

        foreach(var p in players.SelectMany(pl => pl.pawns)) MovePawnToSavedPosition(p);

        currentPlayerTurn = state.currentPlayerTurn;
        _diceResult = state.diceResult;
        
        UpdateScoreboard();
        
        GameStateManager.Instance.ClearState();
        _isRestoring = false;

        uiManager.UpdateStatus("Mini oyun bitti! Sıradaki oyuncuya geçiliyor...");
        StartCoroutine(AutoEndTurnAfterDelay());
    }
    
    public void SetAutoPlay(bool isActive)
    {
        isAutoPlayActive = isActive;
        HandleTurnStart(); // Re-evaluate turn start in case it's now auto
    }

    public void OnDiceRolled(int result)
    {
        if (currentState != GameState.WaitingForRoll) return;
        _diceResult = result;
        
        if (result == 6) {
            _consecutiveSixes++;
        } else {
            _consecutiveSixes = 0;
        }

        if(uiManager) uiManager.UpdateDiceResult("Zar: " + _diceResult);

        if (CanPlayerMove(currentPlayerTurn, _diceResult)) {
            currentState = GameState.WaitingForPawnSelection;
            
            bool isPlayerTurn = currentPlayerTurn == Tile.PlayerType.WaterTribe;
            if (!isPlayerTurn || (isPlayerTurn && isAutoPlayActive)) { // AI's turn or Player on auto
                StartCoroutine(AI_ChooseAndMovePawn());
            } else { // Player's manual turn
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

        bool grantExtraTurn = _diceResult == 6 && _consecutiveSixes < 3 && !_isRestoring;
        
        int currentPlayerIndex = (int)currentPlayerTurn - 1;
        int nextPlayerIndex = grantExtraTurn ? currentPlayerIndex : (currentPlayerIndex + 1) % players.Count;
        
        // Reset the counter if the turn is actually changing
        if (nextPlayerIndex != currentPlayerIndex) {
            _consecutiveSixes = 0;
        }

        currentPlayerTurn = (Tile.PlayerType)(nextPlayerIndex + 1);
        currentState = GameState.WaitingForRoll;
        if (uiManager) uiManager.UpdateStatus($"Sıra: {currentPlayerTurn}\nZar atmak için butona tıkla.");

        HandleTurnStart();
    }
    
    private void HandleTurnStart()
    {
        if (currentState == GameState.GameFinished) return;
        
        if (dice == null || dice.rollButton == null)
        {
            Debug.LogError("CRITICAL ERROR: Dice veya Roll Button objesi GameManager'a atanmamış! Lütfen Unity Inspector'dan atamayı yapın.");
            return;
        }
        
        bool isPlayerTurn = currentPlayerTurn == Tile.PlayerType.WaterTribe;

        // If it's not the player's turn OR if it is the player's turn but autoplay is on, play automatically
        if (!isPlayerTurn || (isPlayerTurn && isAutoPlayActive)) {
             dice.rollButton.interactable = false;
             StartCoroutine(AutoPlayTurn());
        }
        else { // It's the player's manual turn
            dice.rollButton.interactable = true;
        }
    }

    // --- Movement and Rules Logic ---

    private void TryMovePawn(Pawn pawn)
    {
        if (pawn == null) return;

        Tile targetTile = null;
        var futurePawnState = new PawnStateData { 
            pawnId = pawn.pawnId,
            pawnOwnerPlayerIndex = (int)pawn.owner -1, 
            state = pawn.state, 
            pathPosition = pawn.pathPosition,
            hasCompletedLap = pawn.hasCompletedLap 
        };

        switch (pawn.state)
        {
            case Pawn.PawnState.Base:
                if (_diceResult == 6) {
                    targetTile = players.First(p => p.playerType == currentPlayerTurn).startPathTile;
                    futurePawnState.state = Pawn.PawnState.OnBoard;
                    futurePawnState.pathPosition = gameBoard.MainPath.IndexOf(targetTile);
                    futurePawnState.hasCompletedLap = false;
                } else {
                    if (uiManager) uiManager.UpdateStatus("Bu piyonu çıkarmak için 6 atmalısın.");
                    return;
                }
                break;

            case Pawn.PawnState.OnBoard:
                if (!pawn.hasCompletedLap && pawn.pathPosition + _diceResult >= gameBoard.MainPath.Count)
                {
                    futurePawnState.hasCompletedLap = true;
                }

                int homeEntryIndex = _homeEntryIndices[pawn.owner];
                int nextPosUnwrapped = pawn.pathPosition + _diceResult;
                
                bool canEnterHome = futurePawnState.hasCompletedLap || pawn.hasCompletedLap;
                bool isPassingHome = canEnterHome && (pawn.pathPosition <= homeEntryIndex && nextPosUnwrapped > homeEntryIndex);

                if (isPassingHome) {
                    int stepsIntoHome = nextPosUnwrapped - homeEntryIndex - 1;
                    var homePath = gameBoard.HomePaths[(int)pawn.owner -1];
                    if (stepsIntoHome < homePath.Count) {
                        targetTile = homePath[stepsIntoHome];
                        futurePawnState.state = Pawn.PawnState.InHomePath;
                        futurePawnState.pathPosition = stepsIntoHome;
                    } else {
                        if (uiManager) uiManager.UpdateStatus("Hamle geçersiz, eve girmek için tam zar atmalısın.");
                        return;
                    }
                } else {
                    int nextPos = (pawn.pathPosition + _diceResult) % gameBoard.MainPath.Count;
                    targetTile = gameBoard.MainPath[nextPos];
                    futurePawnState.pathPosition = nextPos;
                }
                break;

            case Pawn.PawnState.InHomePath:
                var currentPlayerHomePath = gameBoard.HomePaths[(int)pawn.owner - 1];
                int nextHomePos = pawn.pathPosition + _diceResult;

                if (nextHomePos < currentPlayerHomePath.Count) {
                    targetTile = currentPlayerHomePath[nextHomePos];
                    futurePawnState.pathPosition = nextHomePos;
                } else if (nextHomePos == currentPlayerHomePath.Count - 1) {
                    targetTile = gameBoard.GoalTile;
                    futurePawnState.state = Pawn.PawnState.Finished;
                    futurePawnState.pathPosition = -1;
                } else {
                    if (uiManager) uiManager.UpdateStatus("Eve girmek için tam zar atmalısın.");
                    return;
                }
                break;
        }
        
        if (targetTile != null)
        {
            if (CheckForCapture(targetTile, pawn, futurePawnState)) {
                return;
            }
            
            pawn.state = futurePawnState.state;
            pawn.pathPosition = futurePawnState.pathPosition;
            pawn.hasCompletedLap = futurePawnState.hasCompletedLap;
            pawn.MoveToTile(targetTile);
            
            if (pawn.state == Pawn.PawnState.Finished) CheckForWin();

            EndTurn();
        }
    }

    private bool CanPlayerMove(Tile.PlayerType playerType, int diceResult)
    {
        var player = players.First(p => p.playerType == playerType);
        return player.pawns.Any(pawn => IsMoveValidAI(pawn, diceResult));
    }

    private bool CheckForCapture(Tile targetTile, Pawn movingPawn, PawnStateData futureChallengerState)
    {
        var pawnOnTarget = players.SelectMany(p => p.pawns).FirstOrDefault(p => p.currentTile == targetTile && p.owner != movingPawn.owner && p.state != Pawn.PawnState.Finished);
        if (pawnOnTarget != null) {
            bool isPlayerInvolved = movingPawn.owner == Tile.PlayerType.WaterTribe || pawnOnTarget.owner == Tile.PlayerType.WaterTribe;

            if (isPlayerInvolved && movingPawn.owner != pawnOnTarget.owner)
            {
                if (GameStateManager.Instance == null)
                {
                    Debug.LogWarning("GameStateManager bulunamadı! Mini oyun atlanıyor, standart piyon yeme uygulandı.");
                    pawnOnTarget.state = Pawn.PawnState.Base;
                    pawnOnTarget.pathPosition = -1;
                    pawnOnTarget.hasCompletedLap = false; 
                    pawnOnTarget.MoveToTile(pawnOnTarget.startTile);
                    return false; 
                }

                var defenderState = new PawnStateData {
                    pawnId = pawnOnTarget.pawnId,
                    pawnOwnerPlayerIndex = (int)pawnOnTarget.owner - 1,
                    state = pawnOnTarget.state,
                    pathPosition = pawnOnTarget.pathPosition,
                    hasCompletedLap = pawnOnTarget.hasCompletedLap
                };

                currentState = GameState.Minigame;
                GameStateManager.Instance.SaveState(players, currentPlayerTurn, _diceResult);
                GameStateManager.Instance.challenger = futureChallengerState;
                GameStateManager.Instance.defender = defenderState;
                SceneManager.LoadScene("MinigameScene");
                return true; 
            }
            else
            {
                pawnOnTarget.state = Pawn.PawnState.Base;
                pawnOnTarget.pathPosition = -1;
                pawnOnTarget.hasCompletedLap = false; 
                pawnOnTarget.MoveToTile(pawnOnTarget.startTile);
            }
        }
        return false;
    }

    private void CheckForWin()
    {
        var winner = players.FirstOrDefault(p => p.pawns.All(pawn => pawn.state == Pawn.PawnState.Finished));
        if (winner != null) {
            currentState = GameState.GameFinished;
            if (uiManager) uiManager.UpdateStatus($"{winner.playerType} oyunu kazandı! Tebrikler!");
            if (dice) dice.rollButton.interactable = false;
        }
    }
    
    private void MovePawnToSavedPosition(Pawn pawn)
    {
        if (pawn == null) return;
        switch(pawn.state)
        {
            case Pawn.PawnState.Base:
                pawn.MoveToTile(pawn.startTile);
                break;
            case Pawn.PawnState.OnBoard:
                if (pawn.pathPosition >= 0 && pawn.pathPosition < gameBoard.MainPath.Count)
                    pawn.MoveToTile(gameBoard.MainPath[pawn.pathPosition]);
                break;
            case Pawn.PawnState.InHomePath:
                if (pawn.pathPosition >= 0 && pawn.pathPosition < gameBoard.HomePaths[(int)pawn.owner -1].Count)
                    pawn.MoveToTile(gameBoard.HomePaths[(int)pawn.owner -1][pawn.pathPosition]);
                break;
            case Pawn.PawnState.Finished:
                pawn.MoveToTile(gameBoard.GoalTile);
                break;
        }
    }

    // --- Auto-Play AI ---

    private IEnumerator AutoPlayTurn()
    {
        if (dice == null || dice.rollButton == null)
        {
            Debug.LogError("CRITICAL ERROR: Dice veya Roll Button objesi GameManager'a atanmamış! Otomatik oyun çalıştırılamıyor.");
            yield break; 
        }
        
        dice.rollButton.interactable = false;
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
            EndTurn();
        }
    }

    private Pawn AI_FindBestPawnToMove()
    {
        var player = players.First(p => p.playerType == currentPlayerTurn);
        var movablePawns = player.pawns.Where(p => IsMoveValidAI(p, _diceResult)).ToList();
        
        if (!movablePawns.Any()) return null;

        Pawn bestPawn = null;
        int bestScore = -1000;

        foreach (var pawn in movablePawns) {
            int score = 0;

            if (pawn.state == Pawn.PawnState.Base && _diceResult == 6) {
                score = 20; 
            } else if (pawn.state == Pawn.PawnState.OnBoard) {
                score = 10 + pawn.pathPosition; 
                int nextPos = (pawn.pathPosition + _diceResult) % gameBoard.MainPath.Count;
                var targetTile = gameBoard.MainPath[nextPos];
                
                if (players.SelectMany(pl => pl.pawns).Any(op => op.currentTile == targetTile && op.owner != pawn.owner)) {
                    score += 100;
                }

                int homeEntryIndex = _homeEntryIndices[pawn.owner];
                bool willCompleteLap = !pawn.hasCompletedLap && (pawn.pathPosition + _diceResult >= gameBoard.MainPath.Count);
                bool canEnterHome = pawn.hasCompletedLap || willCompleteLap;
                
                if (canEnterHome && pawn.pathPosition <= homeEntryIndex && (pawn.pathPosition + _diceResult) > homeEntryIndex) {
                     score += 50;
                }
            } else if (pawn.state == Pawn.PawnState.InHomePath) {
                score = 200; 
                var homePath = gameBoard.HomePaths[(int)pawn.owner - 1];
                if (pawn.pathPosition + _diceResult == homePath.Count - 1) score += 500;
            }

            if (score > bestScore) {
                bestScore = score;
                bestPawn = pawn;
            }
        }
        return bestPawn ?? movablePawns.First();
    }

    private bool IsMoveValidAI(Pawn pawn, int diceResult)
    {
        if (pawn.state == Pawn.PawnState.Finished) return false;
        if (pawn.state == Pawn.PawnState.Base) return diceResult == 6;

        if (pawn.state == Pawn.PawnState.OnBoard)
        {
            int homeEntryIndex = _homeEntryIndices[pawn.owner];
            int nextPosUnwrapped = pawn.pathPosition + diceResult;
            bool isTryingToPassHome = (pawn.pathPosition <= homeEntryIndex && nextPosUnwrapped > homeEntryIndex);
            
            bool willCompleteLap = !pawn.hasCompletedLap && (pawn.pathPosition + diceResult >= gameBoard.MainPath.Count);
            bool canEnterHome = pawn.hasCompletedLap || willCompleteLap;

            if (isTryingToPassHome && !canEnterHome)
            {
                return false; 
            }
        }
        
        if (pawn.state == Pawn.PawnState.InHomePath) {
            return pawn.pathPosition + diceResult < gameBoard.HomePaths[(int)pawn.owner - 1].Count;
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
        if (uiManager == null || players == null) return;
        var sb = new StringBuilder("Skor:\n");
        foreach (var p in players) {
            if (p == null) continue;
            sb.AppendLine($"{p.playerType}: {p.pawns.Count(pawn => pawn.state == Pawn.PawnState.Finished)} / 4");
        }
        uiManager.UpdateScore(sb.ToString());
    }
}