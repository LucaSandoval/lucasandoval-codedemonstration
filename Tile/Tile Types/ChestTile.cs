using UnityEngine;
using System.Collections.Generic;

public class ChestTile : ATile
{
    [SerializeField] private ChestLootTable LootTable;

    private const float LootSpawnForce = 5f;

    public void SetLootTable(ChestLootTable lootTable)
    {
        LootTable = lootTable;
    }

    public override void DestroyTile()
    {
        CameraController.Instance?.ShakeCamera(0.1f, 0.14f);

        // Drop tiles first
        foreach(TileRarity rarity in LootTable.TileDrops)
        {
            TileData newTileDrop = LootFactory.Instance?.GenerateTileOfRarity(rarity);
            TileLootCollectable newCollectable = LootFactory.Instance?.SpawnTileLootCollectable(newTileDrop, transform.position);
            ApplyRandomRadialSpawnForce(newCollectable, false, LootSpawnForce);
        }

        // Then Spawn XP
        List<ExpirienceOrbCollectable> spawnedXP = 
            LootFactory.Instance?.SpawnDistributedValueExpirienceOrbs(20, LootTable.Expirience, transform.position);
        foreach(var orb in spawnedXP)
        {
            ApplyRandomRadialSpawnForce(orb, true, LootSpawnForce);   
        }

        // Then destroy the tile
        base.DestroyTile();
    }
}
