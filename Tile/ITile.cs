using UnityEngine;

/// <summary>
/// Represents a tile in the game.
/// </summary>
public interface ITile
{
    /// <summary>
    /// Sets the owner of this tile. 
    /// </summary>
    /// <param name="owner">Owner of this tile.</param>
    void SetOwner(ITileOwner owner);

    /// <summary>
    /// Removes current owner of this tile (if any).
    /// </summary>
    void RemoveOwner();

    /// <summary>
    /// Returns the GameObject associated with this tile.
    /// </summary>
    /// <returns>GameObject associated with this tile.</returns>
    GameObject GetTileObject();

    /// <summary>
    /// Returns the TileData associated with this tile.
    /// </summary>
    /// <returns>TileData associated with this tile.</returns>
    TileData GetTileData();

    /// <summary>
    /// Returns the current owner of this tile. 
    /// </summary>
    /// <returns>The owner of this tile or null if there isn't one.</returns>
    ITileOwner GetTileOwner();

    /// <summary>
    /// Changes tile health by given positive or negative ammount.
    /// </summary>
    /// <param name="ammount">Ammount to change health by.</param>
    void ChangeTileHealth(float ammount);

    /// <summary>
    /// Deals damage of a particular type to this tile.
    /// </summary>
    /// <param name="damage">Damage, including type and value.</param>
    void DamageTile(TileStat<TileDamageCategory> damage);

    /// <summary>
    /// Destroys this tile, automatically removing ownership from any tile owner. 
    /// </summary>
    void DestroyTile();

    /// <summary>
    /// Receives and processes given action event.
    /// </summary>
    /// <param name="actionEvent">Action event to process.</param>
    void RecieveEntityActionEvent(EntityActionEvent actionEvent);
}
