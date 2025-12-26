using UnityEngine;
using System.Collections;
using System;

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
    public Tile.PlayerType owner;
    public PawnState state = PawnState.Base;

    [Header("Location")]
    public int currentTileID = -1; // -1 means in base. 0 is not a valid tile ID for this system.
    public Tile currentTile;

    [Header("Movement")]
    public float moveSpeed = 8f; // Speed of the pawn as it moves from one tile to the next.
    private bool isMoving = false;

    /// <summary>
    /// Instantly places the pawn on a given tile without animation.
    /// Used for setting up the pawn at the start or after being knocked out.
    /// </summary>
    public void PlaceOnTile(Tile newTile)
    {
        if (newTile == null) return;

        transform.position = newTile.transform.position + Vector3.up * 0.5f; // Place it slightly above the tile
        currentTile = newTile;
        currentTileID = newTile.tileID;
    }

    /// <summary>
    /// Starts the process of moving the pawn step-by-step along the path.
    /// </summary>
    /// <param name="board">Reference to the board for pathing data.</param>
    /// <param name="steps">The number of tiles to move forward.</param>
    /// <param name="onMoveComplete">Callback action to invoke when movement is finished.</param>
    public void Move(Board board, int steps, Action onMoveComplete)
    {
        if (isMoving || state != PawnState.OnBoard)
        {
            onMoveComplete?.Invoke();
            return;
        }

        int targetTileID = currentTileID + steps;
        
        // This is a basic implementation. A real game would need to handle moving past the end of the
        // main path and entering a home path. For now, we'll just move on the main path.
        // We also need to ensure the targetTileID is valid.
        if (board.GetTile(targetTileID) != null)
        {
            StartCoroutine(MoveStepByStep(board, targetTileID, onMoveComplete));
        }
        else
        {
            Debug.LogWarning($"Pawn move cancelled. Target Tile ID {targetTileID} is not valid.");
            onMoveComplete?.Invoke();
        }
    }

    /// <summary>
    /// Coroutine that handles the visual, step-by-step movement of the pawn.
    /// </summary>
    private IEnumerator MoveStepByStep(Board board, int targetTileID, Action onMoveComplete)
    {
        isMoving = true;

        // Loop from the next tile to the target tile
        for (int i = currentTileID + 1; i <= targetTileID; i++)
        {
            Tile nextTile = board.GetTile(i);
            if (nextTile == null)
            {
                Debug.LogError($"Path broken at Tile ID {i}. Stopping movement.");
                break;
            }

            Vector3 targetPosition = nextTile.transform.position + Vector3.up * 0.5f;

            // Move towards the next tile until we are very close
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null; // Wait for the next frame
            }

            // Snap to the final position to ensure accuracy
            transform.position = targetPosition;
            currentTileID = i;
            currentTile = nextTile;
        }

        isMoving = false;
        
        // Notify the caller that the move is complete.
        onMoveComplete?.Invoke();
    }
}
