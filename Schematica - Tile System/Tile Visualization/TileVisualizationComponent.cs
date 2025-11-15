using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base component responsible for visualizing a tile, including it's base sprite and associated 
/// modifiers that change the appearance. 
/// </summary>
public abstract class TileVisualizationComponent : MonoBehaviour
{
    [SerializeField] private TileData tileData;
    protected TileCollisionCatagory collisionCatagory;
    private List<GameObject> statVisualizationLayers;
    private Dictionary<TileStatAttribute, float> attributeOpacitySettings;
    private bool inited;

    [SerializeField] private List<TileStatAttributeVisualParams> visualParams;

    private void Awake()
    {
        InitOpacitySettings();
    }

    private void InitOpacitySettings()
    {
        if (inited) return;
        inited = true;

        // Here we associate all possible modifiers with their opacity settings.
        // The associated float represents at what value the given attribute will result in a opacity/alpha
        // of 1 (fully visible).
        attributeOpacitySettings = new Dictionary<TileStatAttribute, float>();
        attributeOpacitySettings.Add(TileStatAttribute.TileHealth, 5);
        attributeOpacitySettings.Add(TileStatAttribute.CollisionDamage, 3);
        attributeOpacitySettings.Add(TileStatAttribute.EntityHealth, 1);
        attributeOpacitySettings.Add(TileStatAttribute.EntityMovementSpeed, 20);
        attributeOpacitySettings.Add(TileStatAttribute.EntityFireRate, 20);
    }

    public void SetData(TileData data, TileCollisionCatagory collisionCatagory = TileCollisionCatagory.none)
    {
        tileData = data;
        this.collisionCatagory = collisionCatagory;
    }

    /// <summary>
    /// Creates visualization for  
    /// </summary>
    public void CreateTileVisualization()
    {
        if (tileData != null)
        {
            ClearAttributeVisualizationLayers();

            // 1. Set base sprite
            SetBaseTileSprite(TileFactory.Instance?.GetTileBaseSprite(tileData.TileType));

            // 2. Render bonus attribute layers on top of base sprite
            foreach(TileStat<TileStatAttribute> attribute in tileData.GetBonusAttributes())
            {
                statVisualizationLayers.Add(CreateAttributeVisualizationLayer(attribute.Attribute, attribute.Ammount));
            }
        }
    }

    /// <summary>
    /// Removes existing bonus attribute visualization layers.
    /// </summary>
    protected virtual void ClearAttributeVisualizationLayers()
    {
        if (statVisualizationLayers == null)
        {
            statVisualizationLayers = new List<GameObject>();
            return;
        }

        foreach(GameObject layer in statVisualizationLayers)
        {
            Destroy(layer);
        }
        statVisualizationLayers.Clear();
    }

    /// <summary>
    /// Sets the base tile sprite.
    /// </summary>
    protected abstract void SetBaseTileSprite(Sprite sprite);

    /// <summary>
    /// Creates a layer on top of the base sprite representing a bonus attribute applied to this tile, returning a 
    /// reference to the new layer.
    /// </summary>
    /// <param name="tileStatAttribute">Attribute to use the sprite of.</param>
    /// <param name="value">Value of bonus attribute, will determine opacity of layer.</param>
    protected abstract GameObject CreateAttributeVisualizationLayer(TileStatAttribute tileStatAttribute, float value);

    /// <summary>
    /// Finds the corresponding TileStatAttributeVisualParam for given TileStatAttribute.
    /// </summary>
    protected virtual TileStatAttributeVisualParams GetParamForAttribute(TileStatAttribute tileStatAttribute)
    {
        foreach(TileStatAttributeVisualParams param in visualParams)
        {
            if (param.StatAttribute == tileStatAttribute) return param;
        }
        return new TileStatAttributeVisualParams();
    }

    /// <summary>
    /// Gets the opacity for a given attribute to use in a visualization layer. The closer to 0 (fully transparent)
    /// the lower the bonus stat.
    /// </summary>
    /// <param name="tileStatAttribute">Given attribute.</param>
    /// <param name="value">Value of attribute.</param>
    /// <returns>Resulting opacity (alpha) from 0-1, where 0 is fully transparent and 1 is fully visible.</returns>
    protected float GetOpacityAlphaForAttribute(TileStatAttribute tileStatAttribute, float value)
    {
        InitOpacitySettings();
        if (attributeOpacitySettings.ContainsKey(tileStatAttribute))
        {
            return Mathf.Lerp(0, 1f, Mathf.InverseLerp(0, attributeOpacitySettings[tileStatAttribute], value));
        } else
        {
            return 0.5f; // arbitrary fallback value
        }
    }
}

/// <summary>
/// Represents the visual paramters of an individual tile stat.
/// </summary>
[System.Serializable]
public struct TileStatAttributeVisualParams
{
    public TileStatAttribute StatAttribute;
    public Sprite StatSprite;
}
