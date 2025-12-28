using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum PlayerType
    {
        None,
        FireNation, // Red
        EarthKingdom, // Green
        AirNomads, // Yellow
        WaterTribe // Blue
    }

    public enum TileType
    {
        Base,   // A player's starting area, holding pawns not in play
        Path,   // The main path around the board
        Home,   // The final, colored path leading to the goal
        Goal,   // The very center, where pawns finish
        Start   // The tile where a pawn enters the main path from its base
    }

    [Header("Tile Properties")]
    [Tooltip("Unique ID for this tile. Main path should be sequential.")]
    public int tileID;

    [Tooltip("Which nation owns this tile (e.g., for Home or Base tiles).")]
    public PlayerType owner;

    [Tooltip("The functional type of this tile.")]
    public TileType type;

    [Header("Game State")]
    [Tooltip("The pawn currently occupying this tile. Null if empty.")]
    public Pawn pawnOnTile;
}

