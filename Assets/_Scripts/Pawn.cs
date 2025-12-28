using UnityEngine;

public class Pawn : MonoBehaviour
{
    public enum PawnState
    {
        Base,
        OnBoard,
        InHomePath,
        Finished
    }

    [Header("Pawn Properties")]
    public int pawnId;
    public Tile.PlayerType owner;
    public PawnState state = PawnState.Base;

    [Header("Location")]
    public Tile startTile;
    public Tile currentTile;
    
    [Header("Movement")]
    public int pathPosition = -1; // Index on the main path loop
    public bool hasCompletedLap = false;
    public bool isSafe = false;

    public void MoveToTile(Tile newTile)
    {
        transform.position = newTile.transform.position + Vector3.up * 0.5f; // Place it slightly above the tile
        currentTile = newTile;
    }
}
