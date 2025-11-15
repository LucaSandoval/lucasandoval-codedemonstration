using UnityEngine;
using UnityEditor;

/// <summary>
/// ScriptableObject used to define new tiles and associate behavior scripts with their tile data and type.
/// </summary>
[CreateAssetMenu(fileName = "New Tile", menuName = "Pixel Game/New Tile")]
public class TilePreset : ScriptableObject
{
    public TileData TileData;

    [Header("Graphics")]
    public Sprite TileSprite;

    [Header("Script")]
    public string TileScript;
    public string InitializationArgument; // if this is a variant that needs a specific instruction
}

/// <summary>
/// Represents a specific type of tile. 
/// </summary>
public enum TileType
{
    empty,
    wall,
    damage,
    health,
    speed,
    bulletDown,
    obsidian,
    chest,
    hardWall,
    bedrock,
    bulletUp,
    bulletLeft,
    bulletRight,
    bulletOmni,
    bomb
}
