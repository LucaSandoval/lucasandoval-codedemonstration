using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents all the information about a particular tile, such as stats, graphics,
/// and other behavior. 
/// </summary>
[System.Serializable]
public class TileData
{
    [Header("Type")]
    public TileType TileType;

    [Header("Loot Info")]
    public TileRarity Rarity;
    public bool EligibleAsLoot;

    [Header("Info")]
    public string TileName;
    [TextArea(3, 10)]
    public string TileDescription;

    [Header("Base Attributes")]
    [SerializeField] private TileStat<TileStatAttribute> TileHealth = new TileStat<TileStatAttribute>(TileStatAttribute.TileHealth, 0);
    [SerializeField] private TileStat<TileStatAttribute> CollisionDamage = new TileStat<TileStatAttribute>(TileStatAttribute.CollisionDamage, 0);
    [SerializeField] private TileStat<TileStatAttribute> EntityHealth = new TileStat<TileStatAttribute>(TileStatAttribute.EntityHealth, 0);
    [SerializeField] private TileStat<TileStatAttribute> EntityMovementSpeed = new TileStat<TileStatAttribute>(TileStatAttribute.EntityMovementSpeed, 0);
    [SerializeField] private TileStat<TileStatAttribute> EntityFireRate = new TileStat<TileStatAttribute>(TileStatAttribute.EntityFireRate, 0);
    [SerializeField] private TileStat<TileStatAttribute> CollisionDefence = new TileStat<TileStatAttribute>(TileStatAttribute.CollisionDefence, 0);

    [Header("Bonus Attributes")]
    [SerializeField] private List<TileStat<TileStatAttribute>> BonusAttributes;

    // Copy constructor for loot generation
    public TileData(TileData otherTileData)
    {
        TileType = otherTileData.TileType;
        Rarity = otherTileData.Rarity;
        TileName = otherTileData.TileName;
        TileDescription = otherTileData.TileDescription;
        EligibleAsLoot = otherTileData.EligibleAsLoot;

        // Deep copy each TileStat object
        TileHealth = new TileStat<TileStatAttribute>(otherTileData.TileHealth.Attribute, otherTileData.TileHealth.Ammount);
        CollisionDamage = new TileStat<TileStatAttribute>(otherTileData.CollisionDamage.Attribute, otherTileData.CollisionDamage.Ammount);
        EntityHealth = new TileStat<TileStatAttribute>(otherTileData.EntityHealth.Attribute, otherTileData.EntityHealth.Ammount);
        EntityMovementSpeed = new TileStat<TileStatAttribute>(otherTileData.EntityMovementSpeed.Attribute, otherTileData.EntityMovementSpeed.Ammount);
        EntityFireRate = new TileStat<TileStatAttribute>(otherTileData.EntityFireRate.Attribute, otherTileData.EntityFireRate.Ammount);
        CollisionDefence = new TileStat<TileStatAttribute>(otherTileData.CollisionDefence.Attribute, otherTileData.CollisionDefence.Ammount);

        // Deep copy the BonusAttributes list
        BonusAttributes = new List<TileStat<TileStatAttribute>>();
        foreach (var bonus in otherTileData.BonusAttributes)
        {
            BonusAttributes.Add(new TileStat<TileStatAttribute>(bonus.Attribute, bonus.Ammount));
        }
    }

    /// <summary>
    /// Adds given bonus attribute to tile data.
    /// </summary>
    /// <param name="attribute">Attribute type.</param>
    /// <param name="value">Attribute value.</param>
    public void AddBonusAttribute(TileStatAttribute attribute, float value)
    {
        BonusAttributes.Add(new TileStat<TileStatAttribute>(attribute, value));
    }

    /// <summary>
    /// Gets all attributes associated with this tile
    /// </summary>
    /// <returns></returns>
    public List<TileStat<TileStatAttribute>> GetAllAtributes()
    {
        if (BonusAttributes == null) BonusAttributes = new List<TileStat<TileStatAttribute>>();

        List<TileStat<TileStatAttribute>> finalList = BonusAttributes
        .Select(stat => new TileStat<TileStatAttribute>(stat.Attribute, stat.Ammount)) // Ensure TileStat has a copy constructor
        .ToList();

        TryAddTileStatToList(TileHealth, ref finalList);
        TryAddTileStatToList(CollisionDamage, ref finalList);
        TryAddTileStatToList(EntityHealth, ref finalList);
        TryAddTileStatToList(EntityMovementSpeed, ref finalList);
        TryAddTileStatToList(EntityFireRate, ref finalList);
        TryAddTileStatToList(CollisionDefence, ref finalList);

        return finalList;
    }

    private void TryAddTileStatToList(TileStat<TileStatAttribute> stat, ref List<TileStat<TileStatAttribute>> list)
    {
        foreach(var tileStat in list)
        {
            if (tileStat.Attribute == stat.Attribute)
            {
                tileStat.Ammount += stat.Ammount;
                return;
            }
        }
        list.Add(stat);
    }

    /// <summary>
    /// Gets only the list of bonus attributes applied to this tile.
    /// </summary>
    public List<TileStat<TileStatAttribute>> GetBonusAttributes()
    {
        return BonusAttributes;
    }

    /// <summary>
    /// Returns the cumulative value of the given attribute on this tile.
    /// </summary>
    /// <param name="attribute">Attribute value requested.</param>
    /// <returns>Cumulative value of the given attribute on this tile.</returns>
    public float GetAttributeValue(TileStatAttribute attribute)
    {
        List<TileStat<TileStatAttribute>> allAttributes = GetAllAtributes();
        float total = 0;
        foreach(var atr in allAttributes)
        {
            if (atr.Attribute == attribute) total += atr.Ammount;
        }
        return total;
    }
}

/// <summary>
/// Represents the rarity of a given tile, might take things like its modifiers into account
/// as well as its general utility.
/// 
/// NOTE: the order of these enums is important. They should be ordered in stricly increasing
/// rarity.
/// </summary>
public enum TileRarity
{
    common,
    uncommon,
    rare,
    legendary,
    artifact
}
