using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents anything that can own a collection of tiles. 
/// Includes Entities, Grids etc...
/// </summary>
public interface ITileOwner
{
    /// <summary>
    /// Adds a tile to the ownership of this tile owner. 
    /// </summary>
    /// <param name="tile">Tile to add.</param>
    void AddTile(ITile tile);

    /// <summary>
    /// Removes a tile from ownership of this tile owner. 
    /// </summary>
    /// <param name="tile">Tile to remove.</param>
    void RemoveTile(ITile tile);

    /// <summary>
    /// Returns a list of all tiles currently under the ownership of this tile owner. 
    /// </summary>
    /// <returns>List of owned tiles. </returns>
    List<ITile> GetTiles();
}
