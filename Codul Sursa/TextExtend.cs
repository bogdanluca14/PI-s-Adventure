using TexDrawLib;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TEXDraw))]
public class TextExtend : MonoBehaviour
{
    // Sistem pentru adaptarea display-ului calculatorului

    public ScrollRect scrollRect;
    public float minWidthOverride = 700f;

    private TEXDraw tex;
    private RectTransform rectTransform;
    private float lastWidth;

    void Awake()
    {
        tex = GetComponent<TEXDraw>();
        rectTransform = GetComponent<RectTransform>();
        if (scrollRect == null)
            scrollRect = GetComponentInParent<ScrollRect>();
        lastWidth = rectTransform.rect.width;
    }

    void Update()
    {
        float contentWidth = tex.orchestrator.outputNativeCanvasSize.x;
        float targetWidth = Mathf.Max(minWidthOverride, contentWidth);

        if (!Mathf.Approximately(targetWidth, lastWidth))
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);

            if (scrollRect != null)
                scrollRect.horizontalNormalizedPosition = 1f;

            lastWidth = targetWidth;
        }
    }
}
