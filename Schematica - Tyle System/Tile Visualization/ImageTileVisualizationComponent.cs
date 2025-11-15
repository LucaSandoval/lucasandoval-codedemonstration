using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image based variant of the TileVisualizationComponent.
/// </summary>
public class ImageTileVisualizationComponent : TileVisualizationComponent
{
    [SerializeField] private Image icon;
    [SerializeField] private bool DisableRaycast;

    protected override GameObject CreateAttributeVisualizationLayer(TileStatAttribute tileStatAttribute, float value)
    {
        GameObject newLayer = new GameObject();
        newLayer.transform.SetParent(transform, false);

        RectTransform newLayerRect = newLayer.AddComponent<RectTransform>();

        Image newLayerRenderer = newLayer.AddComponent<Image>();
        newLayerRenderer.sprite = GetParamForAttribute(tileStatAttribute).StatSprite;
        newLayerRenderer.color = new Color(1, 1, 1, GetOpacityAlphaForAttribute(tileStatAttribute, value));
        newLayerRenderer.raycastTarget = !DisableRaycast;

        RectTransform parentRect = GetComponent<RectTransform>();
        newLayerRect.anchorMin = Vector2.zero;
        newLayerRect.anchorMax = Vector2.one;
        newLayerRect.offsetMin = Vector2.zero;
        newLayerRect.offsetMax = Vector2.zero;
        newLayerRect.pivot = parentRect.pivot;

        return newLayer;
    }

    protected override void SetBaseTileSprite(Sprite sprite)
    {
        if (icon)
        {
            icon.sprite = sprite;
        }
    }
}
