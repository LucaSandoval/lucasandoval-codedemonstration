using UnityEngine;
using System;

/// <summary>
/// Represents an association between a game stat (attribute) and a value.
/// </summary>
/// <typeparam name="T">Type of TileStatAttribute.</typeparam>
[System.Serializable]
public class TileStat<T> where T : Enum
{
    public T Attribute;
    public float Ammount;

    public TileStat(T attribute, float ammount)
    {
        Attribute = attribute;
        Ammount = ammount;
    }
}

public enum TileStatAttribute
{
    TileHealth,
    CollisionDamage,
    EntityMovementSpeed,
    EntityFireRate,
    EntityProjectileSpeed,
    EntityHealth,
    CollisionDefence,
    ExplosionDefence,
    ExplosionDamage,
    ProjectileLifetime
}

public enum TileDamageCategory
{
    CollisionDamage,
    ExplosionDamage
}
