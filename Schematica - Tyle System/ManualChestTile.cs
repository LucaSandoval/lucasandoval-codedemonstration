using UnityEngine;

/// <summary>
/// Allows for the creation of a Chest Tile with preset loot in the editor.
/// </summary>
public class ManualChestTile : ManualTile
{
    [Header("Chest Data")]
    [SerializeField] private bool UseInspectorLootTable;
    [SerializeField] private ChestLootTable ChestLootTable;

    protected override void InitManualTile()
    {
        ChestLootTable table = ChestLootTable;
        if (!UseInspectorLootTable)
        {
            if (LootFactory.Instance && PlayerProgressionComponent.Instance)
            {
                table = LootFactory.Instance.GetChestLootTableForProgression(PlayerProgressionComponent.Instance.GetPlayerRunData().GetProgressionLevel());
            }
        }

        TileFactory.Instance?.SpawnChestTile(table, null, transform.position);
        Destroy(gameObject);
    }
}
