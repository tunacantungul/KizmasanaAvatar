using UnityEngine;
using System.Collections.Generic;

// Holds the state of a single pawn
[System.Serializable]
public class PawnStateData
{
    public int pawnOwnerPlayerIndex;
    public int pawnId;
    public Pawn.PawnState state;
    public int pathPosition;
    public bool hasCompletedLap;
}

// Holds the entire game state
[System.Serializable]
public class GameStateData
{
    public List<PawnStateData> pawnsState;
    public Tile.PlayerType currentPlayerTurn;
    public int diceResult;
    public bool hasState;

    public GameStateData()
    {
        pawnsState = new List<PawnStateData>();
        hasState = false;
    }
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public GameStateData stateData;
    public PawnStateData challenger;
    public PawnStateData defender;
    public Tile.PlayerType minigameWinner;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            stateData = new GameStateData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveState(List<Player> players, Tile.PlayerType currentTurn, int dice)
    {
        stateData.pawnsState.Clear();
        for(int i = 0; i < players.Count; i++)
        {
            foreach (var pawn in players[i].pawns)
            {
                stateData.pawnsState.Add(new PawnStateData
                {
                    pawnOwnerPlayerIndex = i,
                    pawnId = pawn.pawnId,
                    state = pawn.state,
                    pathPosition = pawn.pathPosition,
                    hasCompletedLap = pawn.hasCompletedLap
                });
            }
        }
        stateData.currentPlayerTurn = currentTurn;
        stateData.diceResult = dice;
        stateData.hasState = true;
    }

    public void ClearState()
    {
        stateData.hasState = false;
        challenger = null;
        defender = null;
        minigameWinner = Tile.PlayerType.None;
    }
}
