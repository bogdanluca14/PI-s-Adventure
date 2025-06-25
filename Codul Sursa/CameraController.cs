using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.PostProcessing;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // Variabile Globale

    public bool canMove = true;

    // Miscarea camerei
    public float panSpeedMouse = 1f;
    public float panSpeedTouch = 0.1f;
    public float panSmoothTime = 0.1f;

    // Zoomul camerei
    public float zoomSpeedMouse = 5f;
    public float zoomSpeedTouch = 0.02f;
    public float minOrthographicSize = 3f;
    public float maxOrthographicSize = 20f;
    public float zoomSmoothTime = 0.1f;

    // Referinte in Scena

    public TextMeshProUGUI tmpTitle;
    public TextMeshProUGUI tmpContent;

    public Rect panBounds = new Rect(-10, -10, 20, 20);

    public LayerMask backgroundLayer;

    // Variabile Locale

    // Miscare si Zoom
    private bool isPanning = false;
    private float targetOrtho;
    private float zoomVelocity;

    // Vectori de pozitie
    private Vector2 panOrigin;
    private Vector3 targetPanPosition;
    private Vector3 panVelocity;

    // Referinte locale
    private Camera cam;
    private Animator anim;
    private PostProcessLayer pplayer;

    // Dam assign la referinte
    void Awake()
    {
        anim = GetComponent<Animator>();
        pplayer = GetComponent<PostProcessLayer>();
        cam = GetComponent<Camera>();

        anim.enabled = false;

        targetPanPosition = transform.position;
        targetOrtho = cam.orthographicSize;
    }

    void Update()
    {
        if (!canMove)
            return;

        // Ne ocupam de Miscarea si Zoomul camerei

        HandleMousePan2D();
        HandleMouseZoom();
        HandleTouchPanAndZoom2D();

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        targetPanPosition.x = Mathf.Clamp(
            targetPanPosition.x,
            panBounds.xMin + halfW,
            panBounds.xMax - halfW
        );
        targetPanPosition.y = Mathf.Clamp(
            targetPanPosition.y,
            panBounds.yMin + halfH,
            panBounds.yMax - halfH
        );

        targetOrtho = Mathf.Clamp(targetOrtho, minOrthographicSize, maxOrthographicSize);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPanPosition,
            ref panVelocity,
            panSmoothTime
        );
        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize,
            targetOrtho,
            ref zoomVelocity,
            zoomSmoothTime
        );
    }

    // Activam Miscarea
    public void EnableMovement()
    {
        canMove = true;
    }

    // Setam titlul PopUpului
    public void PopUpTitlu(string s)
    {
        tmpTitle.text = s;
    }

    // Setam continutul PopUpului
    public void PopUpText(string s)
    {
        tmpContent.text = s;
    }

    // Ne ocupam de Miscare
    void HandleMousePan2D()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                isPanning = false;
                return;
            }

            Vector2 worldPoint = cam.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(
                worldPoint,
                Vector2.zero,
                Mathf.Infinity,
                backgroundLayer
            );

            if (hit.collider != null)
            {
                isPanning = true;
                panOrigin = worldPoint;
            }
        }
        if (Input.GetMouseButtonUp(0))
            isPanning = false;

        if (Input.GetMouseButton(0) && isPanning)
        {
            Vector2 current = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 diff = panOrigin - current;

            targetPanPosition += (Vector3)(diff * panSpeedMouse);
            panOrigin = current;
        }
    }

    // Ne ocupam de Zoom
    void HandleMouseZoom()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.0001f)
            targetOrtho -= scroll * zoomSpeedMouse;
    }

    // Ne ocupam de Miscare si Zoom (atingerea propriu-zisa)
    void HandleTouchPanAndZoom2D()
    {
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                if (
                    EventSystem.current != null
                    && EventSystem.current.IsPointerOverGameObject(t.fingerId)
                )
                {
                    isPanning = false;
                    return;
                }

                Vector2 worldPoint = cam.ScreenToWorldPoint(t.position);
                var hit = Physics2D.Raycast(
                    worldPoint,
                    Vector2.zero,
                    Mathf.Infinity,
                    backgroundLayer
                );

                if (hit.collider != null)
                {
                    isPanning = true;
                    panOrigin = worldPoint;
                }
            }
            else if (t.phase == TouchPhase.Moved && isPanning)
            {
                Vector2 curr = cam.ScreenToWorldPoint(t.position);
                Vector2 delta = panOrigin - curr;

                targetPanPosition += (Vector3)(delta * panSpeedTouch);
                panOrigin = curr;
            }
            else if (t.phase == TouchPhase.Ended)
                isPanning = false;
        }
        else if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0),
                t2 = Input.GetTouch(1);
            Vector2 p1 = t1.position - t1.deltaPosition;
            Vector2 p2 = t2.position - t2.deltaPosition;

            float prevDist = Vector2.Distance(p1, p2);
            float currDist = Vector2.Distance(t1.position, t2.position);
            float delta = prevDist - currDist;

            targetOrtho += delta * zoomSpeedTouch;
        }
    }

    // Bluram backgroundul
    public void StartBlur()
    {
        anim.enabled = true;
        anim.Play("StartBlur", 0, 0f);
    }

    // Oprim blurul backgroundului
    public void StopBlur()
    {
        anim.enabled = true;
        anim.Play("StopBlur", 0, 0f);
    }
}
