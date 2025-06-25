using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum FlipMode
{
    RightToLeft,
    LeftToRight,
}

[ExecuteInEditMode]
public class Book : MonoBehaviour
{
    // Variabile Globale

    // Referinte Globale si informatii suplimentare
    [SerializeField]
    RectTransform BookPanel;
    public Sprite background;
    public RectTransform shadow;
    public Sprite[] bookPages;
    public bool interactable = true;
    public bool enableShadowEffect = true;

    public int currentPage = 0;
    public int TotalPageCount
    {
        get { return bookPages.Length; }
    }
    public Vector3 EndBottomLeft
    {
        get { return ebl; }
    }
    public Vector3 EndBottomRight
    {
        get { return ebr; }
    }
    public float Height
    {
        get { return BookPanel.rect.height; }
    }
    public Image ClippingPlane;
    public Image NextPageClip;
    public Image Shadow;
    public Image ShadowLTR;
    public Image Left;
    public Image LeftNext;
    public Image Right;
    public Image RightNext;
    public UnityEvent OnFlip;
    public Canvas canvas;

    // Variabile Locale

    private float radius1,
        radius2;

    private Vector3 sb;
    private Vector3 st;
    private Vector3 c;
    private Vector3 ebr;
    private Vector3 ebl;
    private Vector3 f;

    private bool pageDragging = false;
    private FlipMode mode;

    // Assignul referintelor si variabilelor de baza
    void Start()
    {
        if (!canvas)
            canvas = GetComponentInParent<Canvas>();
        if (!canvas)
            Debug.LogError("Cartea trebuie sa fie copil al canvasului");

        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);
        UpdateSprites();
        CalcCurlCriticalPoints();

        float pageWidth = BookPanel.rect.width / 2.0f;
        float pageHeight = BookPanel.rect.height;
        NextPageClip.rectTransform.sizeDelta = new Vector2(pageWidth, pageHeight + pageHeight * 2);

        ClippingPlane.rectTransform.sizeDelta = new Vector2(
            pageWidth * 2 + pageHeight,
            pageHeight + pageHeight * 2
        );

        float hyp = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
        float shadowPageHeight = pageWidth / 2 + hyp;

        Shadow.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
        Shadow.rectTransform.pivot = new Vector2(1, (pageWidth / 2) / shadowPageHeight);

        ShadowLTR.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
        ShadowLTR.rectTransform.pivot = new Vector2(0, (pageWidth / 2) / shadowPageHeight);
    }

    // Calculeaza punctele care vor fi folosite in animatie
    private void CalcCurlCriticalPoints()
    {
        sb = new Vector3(0, -BookPanel.rect.height / 2);
        ebr = new Vector3(BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
        ebl = new Vector3(-BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
        st = new Vector3(0, BookPanel.rect.height / 2);
        radius1 = Vector2.Distance(sb, ebr);
        float pageWidth = BookPanel.rect.width / 2.0f;
        float pageHeight = BookPanel.rect.height;
        radius2 = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
    }

    // Transforma punctul din Click-ul Mouse-ului in World Space
    public Vector3 transformPoint(Vector3 mouseScreenPos)
    {
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Vector3 mouseWorldPos = canvas.worldCamera.ScreenToWorldPoint(
                new Vector3(mouseScreenPos.x, mouseScreenPos.y, canvas.planeDistance)
            );
            Vector2 localPos = BookPanel.InverseTransformPoint(mouseWorldPos);

            return localPos;
        }
        else if (canvas.renderMode == RenderMode.WorldSpace)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 globalEBR = transform.TransformPoint(ebr);
            Vector3 globalEBL = transform.TransformPoint(ebl);
            Vector3 globalSt = transform.TransformPoint(st);
            Plane p = new Plane(globalEBR, globalEBL, globalSt);
            float distance;

            p.Raycast(ray, out distance);
            Vector2 localPos = BookPanel.InverseTransformPoint(ray.GetPoint(distance));

            return localPos;
        }
        else
        {
            Vector2 localPos = BookPanel.InverseTransformPoint(mouseScreenPos);
            return localPos;
        }
    }

    // Se pot da paginile daca este permis
    void Update()
    {
        if (pageDragging && interactable)
        {
            UpdateBook();
        }
    }

    // Actualizarea propriu-zisa a cartii
    public void UpdateBook()
    {
        f = Vector3.Lerp(f, transformPoint(Input.mousePosition), Time.deltaTime * 10);

        if (mode == FlipMode.RightToLeft)
            UpdateBookRTLToPoint(f);
        else
            UpdateBookLTRToPoint(f);
    }

    // Miscarea paginii din stanga spre unde este trasa
    public void UpdateBookLTRToPoint(Vector3 followLocation)
    {
        mode = FlipMode.LeftToRight;
        f = followLocation;

        ShadowLTR.transform.SetParent(ClippingPlane.transform, true);
        ShadowLTR.transform.localPosition = new Vector3(0, 0, 0);
        ShadowLTR.transform.localEulerAngles = new Vector3(0, 0, 0);
        Left.transform.SetParent(ClippingPlane.transform, true);

        Right.transform.SetParent(BookPanel.transform, true);
        Right.transform.localEulerAngles = Vector3.zero;
        LeftNext.transform.SetParent(BookPanel.transform, true);
        shadow.SetAsFirstSibling();

        c = Calc_C_Position(followLocation);
        Vector3 t1;
        float clipAngle = CalcClipAngle(c, ebl, out t1);
        clipAngle = (clipAngle + 180) % 180;

        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        Left.transform.position = BookPanel.TransformPoint(c);
        float C_T1_dy = t1.y - c.y;
        float C_T1_dx = t1.x - c.x;
        float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
        Left.transform.localEulerAngles = new Vector3(0, 0, C_T1_Angle - 90 - clipAngle);

        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
        LeftNext.transform.SetParent(NextPageClip.transform, true);
        Right.transform.SetParent(ClippingPlane.transform, true);
        Right.transform.SetAsFirstSibling();

        ShadowLTR.rectTransform.SetParent(Left.rectTransform, true);
    }

    // Miscarea paginii din dreapta spre unde este trasa
    public void UpdateBookRTLToPoint(Vector3 followLocation)
    {
        mode = FlipMode.RightToLeft;
        f = followLocation;
        Shadow.transform.SetParent(ClippingPlane.transform, true);
        Shadow.transform.localPosition = Vector3.zero;
        Shadow.transform.localEulerAngles = Vector3.zero;
        Right.transform.SetParent(ClippingPlane.transform, true);

        Left.transform.SetParent(BookPanel.transform, true);
        Left.transform.localEulerAngles = Vector3.zero;
        RightNext.transform.SetParent(BookPanel.transform, true);
        shadow.SetAsFirstSibling();

        c = Calc_C_Position(followLocation);
        Vector3 t1;
        float clipAngle = CalcClipAngle(c, ebr, out t1);
        if (clipAngle > -90)
            clipAngle += 180;

        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);
        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        Right.transform.position = BookPanel.TransformPoint(c);
        float C_T1_dy = t1.y - c.y;
        float C_T1_dx = t1.x - c.x;
        float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
        Right.transform.localEulerAngles = new Vector3(0, 0, C_T1_Angle - (clipAngle + 90));

        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
        RightNext.transform.SetParent(NextPageClip.transform, true);
        Left.transform.SetParent(ClippingPlane.transform, true);
        Left.transform.SetAsFirstSibling();

        Shadow.rectTransform.SetParent(Right.rectTransform, true);
    }

    // Calcularea unghiului dintre punctul tragerii si origine
    private float CalcClipAngle(Vector3 c, Vector3 bookCorner, out Vector3 t1)
    {
        Vector3 t0 = (c + bookCorner) / 2;
        float T0_CORNER_dy = bookCorner.y - t0.y;
        float T0_CORNER_dx = bookCorner.x - t0.x;
        float T0_CORNER_Angle = Mathf.Atan2(T0_CORNER_dy, T0_CORNER_dx);
        float T0_T1_Angle = 90 - T0_CORNER_Angle;

        float T1_X = t0.x - T0_CORNER_dy * Mathf.Tan(T0_CORNER_Angle);
        T1_X = normalizeT1X(T1_X, bookCorner, sb);
        t1 = new Vector3(T1_X, sb.y, 0);

        float T0_T1_dy = t1.y - t0.y;
        float T0_T1_dx = t1.x - t0.x;
        T0_T1_Angle = Mathf.Atan2(T0_T1_dy, T0_T1_dx) * Mathf.Rad2Deg;

        return T0_T1_Angle;
    }

    // Normalizare
    private float normalizeT1X(float t1, Vector3 corner, Vector3 sb)
    {
        if (t1 > sb.x && sb.x > corner.x)
            return sb.x;
        if (t1 < sb.x && sb.x < corner.x)
            return sb.x;
        return t1;
    }

    // Calcularea pozitiei cartii
    private Vector3 Calc_C_Position(Vector3 followLocation)
    {
        Vector3 c;
        f = followLocation;
        float F_SB_dy = f.y - sb.y;
        float F_SB_dx = f.x - sb.x;
        float F_SB_Angle = Mathf.Atan2(F_SB_dy, F_SB_dx);
        Vector3 r1 =
            new Vector3(radius1 * Mathf.Cos(F_SB_Angle), radius1 * Mathf.Sin(F_SB_Angle), 0) + sb;

        float F_SB_distance = Vector2.Distance(f, sb);
        if (F_SB_distance < radius1)
            c = f;
        else
            c = r1;
        float F_ST_dy = c.y - st.y;
        float F_ST_dx = c.x - st.x;
        float F_ST_Angle = Mathf.Atan2(F_ST_dy, F_ST_dx);
        Vector3 r2 =
            new Vector3(radius2 * Mathf.Cos(F_ST_Angle), radius2 * Mathf.Sin(F_ST_Angle), 0) + st;
        float C_ST_distance = Vector2.Distance(c, st);

        if (C_ST_distance > radius2)
            c = r2;

        return c;
    }

    // La tragerea paginii din dreapta
    public void DragRightPageToPoint(Vector3 point)
    {
        if (currentPage >= bookPages.Length)
            return;
        pageDragging = true;
        mode = FlipMode.RightToLeft;
        f = point;

        NextPageClip.rectTransform.pivot = new Vector2(0, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);

        Left.gameObject.SetActive(true);
        Left.rectTransform.pivot = new Vector2(0, 0);
        Left.transform.position = RightNext.transform.position;
        Left.transform.eulerAngles = new Vector3(0, 0, 0);
        Left.sprite = (currentPage < bookPages.Length) ? bookPages[currentPage] : background;
        Left.transform.SetAsFirstSibling();

        Right.gameObject.SetActive(true);
        Right.transform.position = RightNext.transform.position;
        Right.transform.eulerAngles = new Vector3(0, 0, 0);
        Right.sprite =
            (currentPage < bookPages.Length - 1) ? bookPages[currentPage + 1] : background;

        RightNext.sprite =
            (currentPage < bookPages.Length - 2) ? bookPages[currentPage + 2] : background;

        LeftNext.transform.SetAsFirstSibling();
        if (enableShadowEffect)
            Shadow.gameObject.SetActive(true);
        UpdateBookRTLToPoint(f);
    }

    // La tinerea apasata a mouse-ului de la pagina din dreapta
    public void OnMouseDragRightPage()
    {
        if (interactable)
            DragRightPageToPoint(transformPoint(Input.mousePosition));
    }

    // La tragerea paginii din stanga
    public void DragLeftPageToPoint(Vector3 point)
    {
        if (currentPage <= 0)
            return;
        pageDragging = true;
        mode = FlipMode.LeftToRight;
        f = point;

        NextPageClip.rectTransform.pivot = new Vector2(1, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(0, 0.35f);

        Right.gameObject.SetActive(true);
        Right.transform.position = LeftNext.transform.position;
        Right.sprite = bookPages[currentPage - 1];
        Right.transform.eulerAngles = new Vector3(0, 0, 0);
        Right.transform.SetAsFirstSibling();

        Left.gameObject.SetActive(true);
        Left.rectTransform.pivot = new Vector2(1, 0);
        Left.transform.position = LeftNext.transform.position;
        Left.transform.eulerAngles = new Vector3(0, 0, 0);
        Left.sprite = (currentPage >= 2) ? bookPages[currentPage - 2] : background;

        LeftNext.sprite = (currentPage >= 3) ? bookPages[currentPage - 3] : background;

        RightNext.transform.SetAsFirstSibling();
        if (enableShadowEffect)
            ShadowLTR.gameObject.SetActive(true);
        UpdateBookLTRToPoint(f);
    }

    // La tinerea apasata a mouse-ului de la pagina din dreapta
    public void OnMouseDragLeftPage()
    {
        if (interactable)
            DragLeftPageToPoint(transformPoint(Input.mousePosition));
    }

    // La lasarea click-ului
    public void OnMouseRelease()
    {
        if (interactable)
            ReleasePage();
    }

    // La lasarea paginii (resetarea pozitiei/miscarii)
    public void ReleasePage()
    {
        if (pageDragging)
        {
            pageDragging = false;
            float distanceToLeft = Vector2.Distance(c, ebl);
            float distanceToRight = Vector2.Distance(c, ebr);

            if (distanceToRight < distanceToLeft && mode == FlipMode.RightToLeft)
                TweenBack();
            else if (distanceToRight > distanceToLeft && mode == FlipMode.LeftToRight)
                TweenBack();
            else
                TweenForward();
        }
    }

    Coroutine currentCoroutine;

    // Schimbarea paginilor (continutului)
    void UpdateSprites()
    {
        LeftNext.sprite =
            (currentPage > 0 && currentPage <= bookPages.Length)
                ? bookPages[currentPage - 1]
                : background;

        RightNext.sprite =
            (currentPage >= 0 && currentPage < bookPages.Length)
                ? bookPages[currentPage]
                : background;
    }

    // Animatia miscarii (spre fata)
    public void TweenForward()
    {
        if (mode == FlipMode.RightToLeft)
            currentCoroutine = StartCoroutine(
                TweenTo(
                    ebl,
                    0.15f,
                    () =>
                    {
                        Flip();
                    }
                )
            );
        else
            currentCoroutine = StartCoroutine(
                TweenTo(
                    ebr,
                    0.15f,
                    () =>
                    {
                        Flip();
                    }
                )
            );
    }

    // Flipul paginii
    void Flip()
    {
        if (mode == FlipMode.RightToLeft)
            currentPage += 2;
        else
            currentPage -= 2;

        LeftNext.transform.SetParent(BookPanel.transform, true);
        Left.transform.SetParent(BookPanel.transform, true);
        LeftNext.transform.SetParent(BookPanel.transform, true);
        Left.gameObject.SetActive(false);

        Right.gameObject.SetActive(false);
        Right.transform.SetParent(BookPanel.transform, true);
        RightNext.transform.SetParent(BookPanel.transform, true);
        shadow.SetAsFirstSibling();

        UpdateSprites();
        Shadow.gameObject.SetActive(false);
        ShadowLTR.gameObject.SetActive(false);

        if (OnFlip != null)
            OnFlip.Invoke();
    }

    // Animatia miscarii (spre spate)
    public void TweenBack()
    {
        if (mode == FlipMode.RightToLeft)
        {
            currentCoroutine = StartCoroutine(
                TweenTo(
                    ebr,
                    0.15f,
                    () =>
                    {
                        UpdateSprites();
                        RightNext.transform.SetParent(BookPanel.transform);
                        Right.transform.SetParent(BookPanel.transform);
                        shadow.SetAsFirstSibling();
                        Left.gameObject.SetActive(false);
                        Right.gameObject.SetActive(false);
                        pageDragging = false;
                    }
                )
            );
        }
        else
        {
            currentCoroutine = StartCoroutine(
                TweenTo(
                    ebl,
                    0.15f,
                    () =>
                    {
                        UpdateSprites();

                        LeftNext.transform.SetParent(BookPanel.transform);
                        Left.transform.SetParent(BookPanel.transform);
                        shadow.SetAsFirstSibling();
                        Left.gameObject.SetActive(false);
                        Right.gameObject.SetActive(false);
                        pageDragging = false;
                    }
                )
            );
        }
    }

    // Animatie catre pozitie (cu duratie)
    public IEnumerator TweenTo(Vector3 to, float duration, System.Action onFinish)
    {
        int steps = (int)(duration / 0.025f);
        Vector3 displacement = (to - f) / steps;

        for (int i = 0; i < steps - 1; i++)
        {
            if (mode == FlipMode.RightToLeft)
                UpdateBookRTLToPoint(f + displacement);
            else
                UpdateBookLTRToPoint(f + displacement);

            yield return new WaitForSeconds(0.025f);
        }

        if (onFinish != null)
            onFinish();
    }
}
