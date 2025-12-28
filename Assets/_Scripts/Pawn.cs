using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class Pawn : MonoBehaviour
{
    public enum PawnState
    {
        InBase,
        OnPath,
        InHome,
        Finished
    }

    [Header("Properties")]
    public Tile.PlayerType owner;
    public PawnState state = PawnState.InBase;

    [Header("Location")]
    public Tile currentTile;

    [Header("Movement")]
    public float moveSpeed = 8f;
    private bool isMoving = false;
    private Action onMoveComplete;

    /// <summary>
    /// Teleports the pawn to a specific tile. Used for initial setup.
    /// </summary>
    public void PlaceOnTile(Tile tile)
    {
        if (tile == null)
        {
            Debug.LogError($"Attempted to place pawn on a null tile.", this);
            return;
        }

        if (currentTile != null)
        {
            currentTile.pawnOnTile = null;
        }

        currentTile = tile;
        currentTile.pawnOnTile = this;
        transform.position = GetTargetPositionForTile(tile);
    }

    /// <summary>
    /// Public method to start the movement process.
    /// </summary>
    public void Move(int steps, Action onComplete)
    {
        if (isMoving)
        {
            onComplete?.Invoke();
            return;
        }

        onMoveComplete = onComplete;
        StartCoroutine(MoveRoutine(steps));
    }

    private IEnumerator MoveRoutine(int steps)
    {
        isMoving = true;
        
        for (int i = 0; i < steps; i++)
        {
            Tile nextTile = GetNextTile();
            if (nextTile == null)
            {
                Debug.LogWarning("Pawn has nowhere to move. Stopping.", this);
                break;
            }

            // Animate movement to the next tile
            yield return StartCoroutine(AnimateToTile(nextTile));

            // Update state after animation
            UpdateTileOccupation(nextTile);
        }

        isMoving = false;
        onMoveComplete?.Invoke();
    }
    
    private Tile GetNextTile()
    {
        // Logic for finding the very next tile based on current state and position
        if (state == PawnState.OnPath)
        {
            List<Tile> path = Board.Instance.pathTiles;
            int currentIndex = path.IndexOf(currentTile);

            // This is a placeholder for home entry logic
            // bool shouldEnterHome = ...

            // if (shouldEnterHome) {
            //     state = PawnState.InHome;
            //     return Board.Instance.homeTiles[owner][0];
            // }

            // Wrap around the path
            int nextIndex = (currentIndex + 1) % path.Count;
            return path[nextIndex];
        }
        else if (state == PawnState.InHome)
        {
            List<Tile> homePath = Board.Instance.homeTiles[owner];
            int currentIndex = homePath.IndexOf(currentTile);
            if (currentIndex < homePath.Count - 1)
            {
                return homePath[currentIndex + 1];
            }
        }
        
        // If in base, finished, or at the end of home path, no next tile
        return null; 
    }

    private void UpdateTileOccupation(Tile newTile)
    {
        if (currentTile != null)
        {
            currentTile.pawnOnTile = null;
        }
        currentTile = newTile;
        currentTile.pawnOnTile = this;
    }

    private IEnumerator AnimateToTile(Tile targetTile)
    {
        Vector3 targetPosition = GetTargetPositionForTile(targetTile);
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition; // Snap to final position
    }

    private Vector3 GetTargetPositionForTile(Tile tile)
    {
        // Places the pawn slightly above the tile's surface.
        return tile.transform.position + Vector3.up * 0.5f;
    }
}
