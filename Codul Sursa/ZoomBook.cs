using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ZoomBook : MonoBehaviour,
    IPointerClickHandler, IPointerDownHandler, IDragHandler
{
    // Sistem pentru a da Zoom pe documentatie

    public float zoomFactor = 2f;
    public float zoomDuration = 0.3f;
    public float doubleClickThreshold = 0.4f;
    public float panSpeed = 1f;
    public float pixelMargin = 300f;

    public Button openBtn;
    public Button closeBtn;
    public Animator canvasAnim;

    private RectTransform rt, parentRT;
    private Vector3 originalScale;
    private Vector2 originalPivot, originalAnchoredPos;
    private Animator animator;

    private bool isZoomed;
    private float lastClickTime;

    private Vector2 pointerDownMousePos;
    private Vector2 clickLocalPos;
    private Vector2 panStartPos;

    private Vector2 lastClickLocalPos;
    public float maxClickDistance = 30f;

    private const string OPEN_TRIGGER = "PopUp";
    private const string CLOSE_TRIGGER = "PopOut";

    private const string ENABLE_ALPHA = "EnableAlpha";
    private const string DISABLE_ALPHA = "DisableAlpha";

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        animator = GetComponent<Animator>();

        canvasAnim.enabled = false;
        animator.enabled = false;

        parentRT = rt.parent as RectTransform;
    }

    public void WakeUp()
    {
        originalScale = rt.localScale;
        originalPivot = new Vector2(0.5f, 0.5f);
        rt.pivot = originalPivot;
        originalAnchoredPos = rt.anchoredPosition;
    }

    public void Open()
    {
        GetComponent<Book>().interactable = false;
        animator.enabled = true;
        canvasAnim.enabled = true;

        animator.Play(OPEN_TRIGGER, 0, 0f);
        canvasAnim.Play(DISABLE_ALPHA, 0, 0f);

        openBtn.interactable = false;
        StartCoroutine(EnableButton(OPEN_TRIGGER, closeBtn, true));
    }

    public void Close()
    {
        if(isZoomed) ToggleZoom();
        GetComponent<Book>().interactable = false;

        animator.enabled = true;
        animator.Play(CLOSE_TRIGGER, 0, 0f);
        canvasAnim.Play(ENABLE_ALPHA, 0, 0f);

        closeBtn.interactable = false;
        StartCoroutine(EnableButton(CLOSE_TRIGGER, openBtn));
    }

    IEnumerator EnableButton(string state, Button _openBtn, bool opened = false)
    {
        yield return AnimationHandler.WaitForStateEnd(animator, state);
        _openBtn.interactable = true;
        animator.enabled = false;

        if(opened)
        {
            WakeUp();
            GetComponent<Book>().interactable = true;
        }
    }

    public void OnPointerClick(PointerEventData ev)
    {
        float now = Time.unscaledTime;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, ev.position, ev.pressEventCamera, out clickLocalPos);

        if (now - lastClickTime < doubleClickThreshold &&
            Vector2.Distance(clickLocalPos, lastClickLocalPos) <= maxClickDistance)
        {
            ToggleZoom();
        }

        lastClickTime = now;
        lastClickLocalPos = clickLocalPos;
    }

    public void OnPointerDown(PointerEventData ev)
    {
        if (!isZoomed) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRT, ev.position, ev.pressEventCamera, out pointerDownMousePos);

        panStartPos = rt.anchoredPosition;
    }

    public void OnDrag(PointerEventData ev)
    {
        if (!isZoomed) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRT, ev.position, ev.pressEventCamera, out Vector2 currentMousePos);

        Vector2 delta = (currentMousePos - pointerDownMousePos) * panSpeed;
        Vector2 newPos = panStartPos + delta;

        newPos.x = Mathf.Clamp(newPos.x, -pixelMargin, pixelMargin);
        newPos.y = Mathf.Clamp(newPos.y, -pixelMargin, pixelMargin);

        rt.anchoredPosition = newPos;
    }

    private void ToggleZoom()
    {
        StopAllCoroutines();
        StartCoroutine(isZoomed ? DoZoomOut() : DoZoomIn());
    }

    private IEnumerator DoZoomIn()
    {
        GetComponent<Book>().interactable = false;
        isZoomed = true;

        Vector2 startPivot = rt.pivot;
        Vector3 startScale = rt.localScale;
        Vector2 startPos = rt.anchoredPosition;

        Vector2 targetPivot = new Vector2(0.5f, 0.5f);
        Vector3 targetScale = originalScale * zoomFactor;

        Vector2 offset = clickLocalPos * (zoomFactor - 1f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / zoomDuration;
            rt.pivot = Vector2.Lerp(startPivot, targetPivot, t);
            rt.localScale = Vector3.Lerp(startScale, targetScale, t);
            rt.anchoredPosition = startPos + Vector2.Lerp(Vector2.zero, -offset, t);
            yield return null;
        }

        rt.pivot = targetPivot;
        rt.localScale = targetScale;
    }

    private IEnumerator DoZoomOut()
    {
        isZoomed = false;

        Vector2 startPivot = rt.pivot;
        Vector3 startScale = rt.localScale;
        Vector2 startPos = rt.anchoredPosition;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / zoomDuration;
            rt.pivot = Vector2.Lerp(startPivot, originalPivot, t);
            rt.localScale = Vector3.Lerp(startScale, originalScale, t);
            rt.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, t);
            yield return null;
        }

        rt.pivot = originalPivot;
        rt.localScale = originalScale;

        GetComponent<Book>().interactable = true;
    }
}
