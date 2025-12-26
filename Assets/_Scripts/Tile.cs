using UnityEngine;

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
        Base, // Starting area, not on the path
        Path,
        Home, // Final path for a player
        Goal // The very center
    }

    [Header("Tile Properties")]
    public int tileID;
    public PlayerType owner;
    public TileType type;
    public int x;
    public int z;
}

