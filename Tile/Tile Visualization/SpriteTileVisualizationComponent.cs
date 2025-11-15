using UnityEngine;

/// <summary>
/// SpriteRenderer based variant of the TileVisualizationComponent.
/// </summary>
public class SpriteTileVisualizationComponent : TileVisualizationComponent
{
    [SerializeField] private GameObject EnemyOutline;
    [SerializeField] private SpriteRenderer ren;
    private int layerOffset = 1;

    protected override GameObject CreateAttributeVisualizationLayer(TileStatAttribute tileStatAttribute, float value)
    {
        GameObject newLayer = new GameObject();
        newLayer.transform.localScale = transform.localScale;
        newLayer.transform.SetParent(transform);

        SpriteRenderer newLayerRenderer = newLayer.AddComponent<SpriteRenderer>();
        newLayerRenderer.sprite = GetParamForAttribute(tileStatAttribute).StatSprite;
        newLayerRenderer.color = new Color(1, 1, 1, GetOpacityAlphaForAttribute(tileStatAttribute, value));
        newLayerRenderer.sortingOrder = ren.sortingOrder + layerOffset;
        layerOffset++;

        return newLayer;
    }

    protected override void SetBaseTileSprite(Sprite sprite)
    {
        if (ren)
        {
            ren.sprite = sprite;
        }
    }

    private void Start()
    {
        if (collisionCatagory == TileCollisionCatagory.enemyBody ||
            collisionCatagory == TileCollisionCatagory.enemyBullet)
        {
            EnemyOutline.SetActive(true);
        }
    }
}
