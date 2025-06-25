using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIButtonCalcPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // Sistem pentru a accentua apasarea unui buton in UI

    public float pressOffset = 2f;

    [Range(0.95f, 1f)]
    public float pressedScale = 0.98f;
    public float pressDuration = 0.05f;

    public float releaseDuration = 0.2f;
    public float bounceElasticity = 1.1f;

    [Range(1, 3)]
    public int bounceVibrato = 1;

    private RectTransform rt;
    private Vector3 normalScale;
    private float normalPosY;
    private Sequence seq;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        normalScale = rt.localScale;
        normalPosY = rt.anchoredPosition.y;
    }

    public void OnPointerDown(PointerEventData e)
    {
        seq?.Kill();

        seq = DOTween
            .Sequence()
            .Append(rt.DOAnchorPosY(normalPosY - pressOffset, pressDuration).SetEase(Ease.OutQuad))
            .Join(rt.DOScale(normalScale * pressedScale, pressDuration).SetEase(Ease.OutQuad));

        AudioManager.instance.PlaySound("buttonClick");
    }

    public void OnPointerUp(PointerEventData e)
    {
        seq?.Kill();

        seq = DOTween
            .Sequence()
            .Append(
                rt.DOScale(normalScale, releaseDuration)
                    .SetEase(Ease.OutElastic, bounceElasticity, bounceVibrato)
            )
            .Join(
                rt.DOAnchorPosY(normalPosY, releaseDuration)
                    .SetEase(Ease.OutElastic, bounceElasticity, bounceVibrato)
            );
    }
}
