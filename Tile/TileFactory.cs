using Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the spawning of different types of tiles.
/// </summary>
public class TileFactory : Singleton<TileFactory>
{
    [SerializeField] private GameObject tilePrefab;
    private Dictionary<TileType, TilePreset> tileDataMap;
    public const float tileWidthHeight = 1.0f;

    protected override void _Initialize(System.Action onInitialized)
    {
        LoadTileData();
        onInitialized?.Invoke();
    }

    private void LoadTileData()
    {
        tileDataMap = new Dictionary<TileType, TilePreset>();
        TilePreset[] tileData = Resources.LoadAll<TilePreset>("Tiles");
        foreach(TilePreset data in tileData)
        {
            tileDataMap.Add(data.TileData.TileType, data);
        }
    }

    /// <summary>
    /// WIP- returns base sprite for tile loaded from scriptable objects.
    /// </summary>
    public Sprite GetTileBaseSprite(TileType type)
    {
        if (tileDataMap.ContainsKey(type)) return tileDataMap[type].TileSprite;
        return null;
    } 

    // Given a tyle type enum, returns a new copy of a defualt (unmodified) instance
    // of that data's tile.
    public TileData GetDefaultTileDataForType(TileType type)
    {
        if (tileDataMap.ContainsKey(type))
        {
            return new TileData(tileDataMap[type].TileData);
        } else
        {
            return null;
        }
    }

    /// <summary>
    /// Spawns a new tile in the world using given parameters.
    /// </summary>
    /// <param name="data">Data of the tile to be spawned.</param>
    /// <param name="owner">Owner of the spawned tile. Can be left as null to indicate no owner.</param>
    /// <param name="position">The position of the new tile to be spawned at. If no owner specified, will be considred as world coordinates.</param>
    public ITile SpawnTile(TileData data, ITileOwner owner, Vector2 position,
    TileCollisionCatagory collisionCatagory = TileCollisionCatagory.none, bool retainWorldPosition = false)
    {
        if (!tilePrefab || !tileDataMap.ContainsKey(data.TileType) || data == null) return null;

        // Spawn tile and add tile specific script
        TilePreset newTilePreset = tileDataMap[data.TileType];
        GameObject newTile = Instantiate(tilePrefab);
        newTile.AddComponent(Type.GetType(newTilePreset.TileScript));

        // Init tile script, set owner(if applicable)
        ATile newTileClass = newTile.GetComponent<ATile>();
        newTileClass.InitTile(data, collisionCatagory, newTilePreset.InitializationArgument);
        if (owner != null)
        {
            newTileClass.SetOwner(owner);
            owner.AddTile(newTileClass);
        }

        // Position tile in world/under owner (WIP)
        if (retainWorldPosition)
        {
            newTile.transform.position = position;
        }
        else
        {
            newTile.transform.localPosition = position;
        }

        return newTileClass;
    }

    // Overload for tile type enum
    public ITile SpawnTile(TileType type, ITileOwner owner, Vector2 position, 
        TileCollisionCatagory collisionCatagory = TileCollisionCatagory.none, bool retainWorldPosition = false)
    {
        if (!tilePrefab || !tileDataMap.ContainsKey(type) || type == TileType.empty) return null;
        return SpawnTile(GetDefaultTileDataForType(type), owner, position, collisionCatagory, retainWorldPosition);
    }

    /// <summary>
    /// Spawns a chest tile in the world with a preset loot table.
    /// </summary>
    /// <param name="lootTable">Given loot table.</param>
    /// <param name="owner">Owner of new chest tile.</param>
    /// <param name="position">World position of chest tile.</param>
    /// <returns></returns>
    public ChestTile SpawnChestTile(ChestLootTable lootTable, ITileOwner owner, Vector2 position,
        TileCollisionCatagory collisionCatagory = TileCollisionCatagory.none)
    {
        ITile newTile = SpawnTile(TileType.chest, owner, position, collisionCatagory);
        ChestTile newChestTile = newTile.GetTileObject().GetComponent<ChestTile>();
        if (newChestTile)
        {
            newChestTile.SetLootTable(lootTable);
            return newChestTile;
        } else
        {
            return null;
        }
    }

    /// <summary>
    /// Spawns a given grid of tiles centered around a given center point.
    /// </summary>
    /// <param name="grid">Grid of tiles to spawn. May include null tiles to indicate a blank space.</param>
    /// <param name="owner">Owner of the new tile grid.</param>
    /// <param name="centerPoint">If no owner is provided, this will be used as the world position center point of the grid.</param>
    /// <returns></returns>
    public List<ITile> SpawnTileGridAtLocation(List<List<TileData>> grid, ITileOwner owner, Vector2 centerPoint,
        TileCollisionCatagory collisionCatagory = TileCollisionCatagory.none)
    {
        List<ITile> output = new List<ITile>();

        int xSize = grid[0].Count;
        int ySize = grid.Count;

        int maxHeight = xSize;
        int maxWidth = 0;
        for (int i = 0; i < xSize; i++)
        {
            if (ySize > maxWidth) maxWidth = ySize;
        }

        float horizontalOffset = (maxWidth * tileWidthHeight) / 2 - (tileWidthHeight / 2);
        float verticalOffset = (maxHeight * tileWidthHeight) / 2 - (tileWidthHeight / 2);

        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < ySize; j++)
            {
                if (grid[j][i] != null)
                {
                    Vector2 localGridLocation = new Vector2(
                        (i * tileWidthHeight) - horizontalOffset,
                        (j * tileWidthHeight * -1) + verticalOffset);
                    if (owner == null) localGridLocation += centerPoint;

                    output.Add(SpawnTile(grid[j][i], owner, localGridLocation, collisionCatagory));
                }
            }
        }

        return output;
    }

    // Overload for TArray of enums
    public List<ITile> SpawnTileGridAtLocation(TArray<TileType> grid, ITileOwner owner, Vector2 centerPoint, 
        TileCollisionCatagory collisionCatagory = TileCollisionCatagory.none)
    {
        List<List<TileData>> newGrid = new List<List<TileData>>();

        for (int y = 0; y < grid.Size.y; y++)
        {
            List<TileData> newRow = new List<TileData>();

            for (int x = 0; x < grid.Size.x; x++)
            {
                if (grid.Get(x,y) == TileType.empty)
                {
                    newRow.Add(null);
                } else
                {
                    newRow.Add(GetDefaultTileDataForType(grid.Get(x, y)));
                }
            }

            newGrid.Add(newRow);
        }

        return SpawnTileGridAtLocation(newGrid, owner, centerPoint, collisionCatagory);
    }
}

/// <summary>
/// Represents a collision 'catagory' for a tile- ensuring it only collides with desired other tiles.
/// </summary>
public enum TileCollisionCatagory
{
    none,
    playerOwned,
    enemyBody,
    enemyBullet
}
