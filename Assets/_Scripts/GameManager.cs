using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

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
    public Dice dice;
    public UIManager uiManager;

    [Header("Prefabs")]
    public GameObject pawnPrefab;
    
    [Header("Game State")]
    public GameState currentState;
    public Tile.PlayerType currentPlayerTurn;
    public bool isAutoPlayActive = false;
    
    private Dictionary<Tile.PlayerType, List<Pawn>> _playerPawns = new Dictionary<Tile.PlayerType, List<Pawn>>();
    private int _diceResult;
    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
        currentState = GameState.Initializing;
        StartCoroutine(SetupGame());
    }

    void Update()
    {
        // Handle player input for pawn selection
        if (currentState == GameState.WaitingForPawnSelection && !isAutoPlayActive && Input.GetMouseButtonDown(0))
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit) && hit.collider.TryGetComponent<Pawn>(out var clickedPawn))
            {
                if (clickedPawn.owner == currentPlayerTurn)
                {
                    TryMovePawn(clickedPawn);
                }
            }
        }
    }

    private IEnumerator SetupGame()
    {
        // Wait for the Board to initialize itself
        yield return new WaitUntil(() => Board.Instance != null);

        var playerTypes = Enum.GetValues(typeof(Tile.PlayerType)).Cast<Tile.PlayerType>()
                              .Where(p => p != Tile.PlayerType.None).ToList();

        foreach (var playerType in playerTypes)
        {
            _playerPawns[playerType] = new List<Pawn>();
            List<Tile> playerBaseTiles = Board.Instance.baseTiles[playerType];

            for (int i = 0; i < playerBaseTiles.Count; i++)
            {
                Tile baseTile = playerBaseTiles[i];
                GameObject pawnObj = Instantiate(pawnPrefab, baseTile.transform.position, Quaternion.identity);
                pawnObj.name = $"{playerType} Pawn {i + 1}";
                
                Pawn pawn = pawnObj.GetComponent<Pawn>();
                if (pawn == null)
                {
                    Debug.LogError($"Pawn prefab does not have a Pawn component!");
                    yield break;
                }

                pawn.owner = playerType;
                pawn.state = Pawn.PawnState.InBase;
                pawn.PlaceOnTile(baseTile); // Correctly place and associate pawn with tile
                
                _playerPawns[playerType].Add(pawn);
            }
        }

        currentPlayerTurn = playerTypes[0];
        TransitionToState(GameState.WaitingForRoll);
    }
    
    public void OnDiceRolled(int result)
    {
        if (currentState != GameState.WaitingForRoll) return;

        _diceResult = result;
        uiManager.UpdateDiceResult("Zar: " + _diceResult);

        List<Pawn> movablePawns = GetMovablePawns(currentPlayerTurn, _diceResult);

        if (movablePawns.Count > 0)
        {
            TransitionToState(GameState.WaitingForPawnSelection);
            if (isAutoPlayActive)
            {
                TryMovePawn(movablePawns.First()); // Simple AI: move the first available pawn
            }
        }
        else
        {
            uiManager.UpdateStatus("Oynanacak hamle yok.");
            StartCoroutine(EndTurnAfterDelay(1.5f));
        }
    }

    private void TryMovePawn(Pawn pawn)
    {
        if (currentState != GameState.WaitingForPawnSelection) return;

        TransitionToState(GameState.PawnMoving);

        Action onMoveComplete = () => {
            if (_diceResult == 6)
            {
                TransitionToState(GameState.WaitingForRoll); // Roll again
            }
            else
            {
                SwitchToNextPlayer();
                TransitionToState(GameState.WaitingForRoll);
            }
        };

        // --- Game Rule Logic ---
        if (pawn.state == Pawn.PawnState.InBase && _diceResult == 6)
        {
            Tile startTile = Board.Instance.startTiles[pawn.owner];
            CheckForCapture(startTile, pawn);
            pawn.PlaceOnTile(startTile);
            pawn.state = Pawn.PawnState.OnPath;
            onMoveComplete();
        }
        else if (pawn.state == Pawn.PawnState.OnPath || pawn.state == Pawn.PawnState.InHome)
        {
            // The intelligent Pawn.Move method now handles all the pathing logic.
            pawn.Move(_diceResult, onMoveComplete);
        }
    }

    private void CheckForCapture(Tile destinationTile, Pawn movingPawn)
    {
        if (destinationTile.pawnOnTile != null && destinationTile.pawnOnTile.owner != movingPawn.owner)
        {
            Pawn capturedPawn = destinationTile.pawnOnTile;
            Debug.Log($"{movingPawn.owner} captured {capturedPawn.owner}!");

            // Find an empty base tile for the captured pawn
            Tile emptyBaseTile = Board.Instance.baseTiles[capturedPawn.owner]
                                       .FirstOrDefault(t => t.pawnOnTile == null);
            if (emptyBaseTile != null)
            {
                capturedPawn.PlaceOnTile(emptyBaseTile);
                capturedPawn.state = Pawn.PawnState.InBase;
            }
        }
    }
    
    private List<Pawn> GetMovablePawns(Tile.PlayerType player, int diceRoll)
    {
        List<Pawn> pawns = _playerPawns[player];
        List<Pawn> movable = new List<Pawn>();

        // If roll is 6, any pawn in base is movable
        if (diceRoll == 6 && pawns.Any(p => p.state == Pawn.PawnState.InBase))
        {
            movable.AddRange(pawns.Where(p => p.state == Pawn.PawnState.InBase));
        }
        
        // Any pawn on a path is potentially movable
        movable.AddRange(pawns.Where(p => p.state == Pawn.PawnState.OnPath || p.state == Pawn.PawnState.InHome));

        // TODO: Add more refined logic here, e.g., pawns at the end of home path cannot move.
        return movable.Distinct().ToList();
    }
    
    private void SwitchToNextPlayer()
    {
        int currentIndex = (int)currentPlayerTurn -1;
        int nextIndex = (currentIndex + 1) % _playerPawns.Count;
        currentPlayerTurn = (Tile.PlayerType)(nextIndex + 1);
    }
    
    private void TransitionToState(GameState newState)
    {
        currentState = newState;
        switch (currentState)
        {
            case GameState.WaitingForRoll:
                uiManager.UpdateStatus($"Sıra: {currentPlayerTurn}\nZar atmak için tıkla.");
                dice.rollButton.interactable = true;
                break;
            case GameState.WaitingForPawnSelection:
                 uiManager.UpdateStatus($"Sıra: {currentPlayerTurn}\nHareket ettirmek için bir piyon seç.");
                dice.rollButton.interactable = false;
                break;
            case GameState.PawnMoving:
                uiManager.UpdateStatus($"{currentPlayerTurn} piyonunu hareket ettiriyor...");
                dice.rollButton.interactable = false;
                break;
        }
    }

    private IEnumerator EndTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SwitchToNextPlayer();
        TransitionToState(GameState.WaitingForRoll);
    }
}