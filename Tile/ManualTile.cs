using UnityEngine;

/// <summary>
/// Allows for the creation of a tile in the editor at Start without the use of the 
/// TileFactory. Provide a TilePreset through the inspector and the tile will be created.
/// </summary>
public class ManualTile : MonoBehaviour
{
    [Header("Tile Preset")]
    [SerializeField] protected TilePreset TilePreset;

    void Start()
    {
        if (TilePreset)
        {
            InitManualTile();
        }
    }

    protected virtual void InitManualTile()
    {
        // Check if there is any Tile Owner parenting this tile
        ITileOwner ownedEntity = GetComponentInParent<ITileOwner>();

        if (ownedEntity != null)
        {
            // Then, check if it's an enemy
            Enemy parentEnemyScript = GetComponentInParent<Enemy>();
            if (parentEnemyScript)
            {
                ITile newTile = SpawnTile(parentEnemyScript, TileCollisionCatagory.enemyBody, true);
                parentEnemyScript.AddTile(newTile);
            } else
            {
                // Otherwise, try and add it to the owner
                ITile newTile = SpawnTile(ownedEntity, TileCollisionCatagory.none, true);
                ownedEntity.AddTile(newTile);
            }
        } else
        {
            SpawnTile(null, TileCollisionCatagory.none, false);
        }
        Destroy(gameObject);
    }

    private ITile SpawnTile(ITileOwner owner, TileCollisionCatagory collisionCatagory, bool retainWorldPosition)
    {
        return TileFactory.Instance?.SpawnTile(TilePreset.TileData.TileType, owner, transform.position, collisionCatagory, retainWorldPosition);
    }
}
