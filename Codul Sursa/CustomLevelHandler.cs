using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public struct Obstacle
{
    public GameObject go;
    public ObstacleType type;

    public Obstacle(GameObject Go, ObstacleType Type) => (go, type) = (Go, Type);
}

public class CustomLevelHandler : MonoBehaviour
{
    // Variabile Globale

    // Referinte si instante
    public static CustomLevelHandler instance;

    public List<GameObject> prefabs;
    public List<Button> buttons;
    public PlayerLauncher player;
    public TMP_InputField codeText;

    // Variabile privind fizica folosita
    public float tapMoveStep = 0.1f;
    public float baseMoveSpeed = 0f;
    public float moveAccel = 5f;
    public float minX = -20f,
        maxX = 20f;
    public float minY = -20f,
        maxY = 20f;

    // Variabile privind rotatia
    public int tapRotateStep = 1;
    public float baseRotateSpeed = 0f;
    public float rotateAccel = 200f;

    // Starea Nivelului
    public bool inLoad;
    public string crtCode;

    // Variabile Locale

    // Referinte locale
    private List<Obstacle> created = new List<Obstacle>();
    private Camera cam;
    private Obstacle selected;
    private LevelHandler levelHandler;

    // Temporizator pentru Apasarea butonului
    private Dictionary<KeyCode, float> holdTime = new Dictionary<KeyCode, float>();

    // Variabile pentru axele de Miscare si Rotatie
    private float accumX = 0f;
    private float accumY = 0f;
    private float accumRot = 0f;

    // Salvarea Nivelului Custom
    public void SaveCustom()
    {
        var enc = new LevelEncoder();

        foreach (var crt in created)
            enc.Save(
                crt.type,
                crt.go.transform.position,
                Mathf.RoundToInt(crt.go.transform.eulerAngles.z)
            );

        codeText.text = enc.GetCode();
    }

    // Incarcarea Nivelului Custom
    public void LoadCustom(bool restart = false)
    {
        string code = "";

        if (restart)
            code = crtCode;
        else
            code = codeText.text;

        if (string.IsNullOrEmpty(code))
            return;

        List<ObstacleData> obstacles = LevelEncoder.Load(code);
        if (obstacles == null)
            return;

        levelHandler.stars.Clear();
        foreach (var crt in created)
            Destroy(crt.go);
        created.Clear();
        crtCode = code;

        foreach (var obs in obstacles)
        {
            string prefabName;
            switch (obs.Type)
            {
                case ObstacleType.Triangle:
                    prefabName = "Triangle";
                    break;
                case ObstacleType.Circle:
                    prefabName = "Circle";
                    break;
                case ObstacleType.Diamond:
                    prefabName = "Diamond";
                    break;
                case ObstacleType.Square:
                    prefabName = "Square";
                    break;
                case ObstacleType.Star:
                    prefabName = "Star";
                    break;
                default:
                    prefabName = "Main";
                    break;
            }

            var pf = prefabs.Find(x => x.name == prefabName);
            if (pf == null)
                continue;

            var go = Instantiate(pf);
            go.transform.position = new Vector3(obs.X, obs.Y);
            go.transform.eulerAngles = new Vector3(0f, 0f, obs.Rotation);
            created.Add(new Obstacle(go, obs.Type));

            if (prefabName == "Star")
            {
                levelHandler.stars.Add(go);
                levelHandler.UpdateStarCount();
            }
        }

        player.options.BackBtn();
    }

    // Assignul referintelor
    void Start()
    {
        if (instance == null)
            instance = this;

        cam = Camera.main;
        levelHandler = player.levelH;

        foreach (
            var k in new[] { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Q, KeyCode.E }
        )
            holdTime[k] = 0f;

        for (int i = 0; i < 4; ++i)
            player.lineManager.OnSave();

        codeText.readOnly = !inLoad;
    }

    // Gestionarea Miscarii
    void Update()
    {
        if (inLoad || player.launched)
            return;

        HandleSelection();

        if (selected.go == null)
            return;

        float dt = Time.deltaTime;
        float moveY = 0f;
        float moveX = 0f;

        // Apasarea butonului (Click)
        if (Input.GetKeyDown(KeyCode.Delete))
            DeleteSelected();
        if (Input.GetKeyDown(KeyCode.W))
            moveY += tapMoveStep;
        if (Input.GetKeyDown(KeyCode.S))
            moveY -= tapMoveStep;
        if (Input.GetKeyDown(KeyCode.D))
            moveX += tapMoveStep;
        if (Input.GetKeyDown(KeyCode.A))
            moveX -= tapMoveStep;

        // Tinerea apasata a butonului (hold)
        if (Input.GetKey(KeyCode.W))
            holdTime[KeyCode.W] += dt;
        else
            holdTime[KeyCode.W] = 0f;
        if (Input.GetKey(KeyCode.S))
            holdTime[KeyCode.S] += dt;
        else
            holdTime[KeyCode.S] = 0f;
        if (Input.GetKey(KeyCode.D))
            holdTime[KeyCode.D] += dt;
        else
            holdTime[KeyCode.D] = 0f;
        if (Input.GetKey(KeyCode.A))
            holdTime[KeyCode.A] += dt;
        else
            holdTime[KeyCode.A] = 0f;

        float speedW = baseMoveSpeed + moveAccel * holdTime[KeyCode.W];
        float speedS = baseMoveSpeed + moveAccel * holdTime[KeyCode.S];
        float speedD = baseMoveSpeed + moveAccel * holdTime[KeyCode.D];
        float speedA = baseMoveSpeed + moveAccel * holdTime[KeyCode.A];

        accumY += (Input.GetKey(KeyCode.W) ? speedW : 0f) * dt;
        accumY -= (Input.GetKey(KeyCode.S) ? speedS : 0f) * dt;
        accumX += (Input.GetKey(KeyCode.D) ? speedD : 0f) * dt;
        accumX -= (Input.GetKey(KeyCode.A) ? speedA : 0f) * dt;

        // Gestionam viteza miscarii

        int stepsY = Mathf.FloorToInt(Mathf.Abs(accumY) / tapMoveStep);
        if (stepsY > 0)
        {
            moveY += Mathf.Sign(accumY) * stepsY * tapMoveStep;
            accumY -= Mathf.Sign(accumY) * stepsY * tapMoveStep;
        }

        int stepsX = Mathf.FloorToInt(Mathf.Abs(accumX) / tapMoveStep);
        if (stepsX > 0)
        {
            moveX += Mathf.Sign(accumX) * stepsX * tapMoveStep;
            accumX -= Mathf.Sign(accumX) * stepsX * tapMoveStep;
        }

        Vector3 newPos = new Vector3(moveX, moveY, 0f);
        if (selected.go != null)
            newPos += selected.go.transform.position;

        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

        if (selected.go != null)
            selected.go.transform.position = newPos;

        // Rotatia obstacolului

        if (Input.GetKeyDown(KeyCode.Q))
            accumRot += tapRotateStep;
        if (Input.GetKeyDown(KeyCode.E))
            accumRot -= tapRotateStep;

        if (Input.GetKey(KeyCode.Q))
            holdTime[KeyCode.Q] += dt;
        else
            holdTime[KeyCode.Q] = 0f;
        if (Input.GetKey(KeyCode.E))
            holdTime[KeyCode.E] += dt;
        else
            holdTime[KeyCode.E] = 0f;

        float speedQ = baseRotateSpeed + rotateAccel * holdTime[KeyCode.Q];
        float speedE = baseRotateSpeed + rotateAccel * holdTime[KeyCode.E];

        accumRot += (Input.GetKey(KeyCode.Q) ? speedQ : 0f) * dt;
        accumRot -= (Input.GetKey(KeyCode.E) ? speedE : 0f) * dt;

        int rotSteps = Mathf.FloorToInt(Mathf.Abs(accumRot));
        if (rotSteps > 0)
        {
            int dir = accumRot > 0 ? 1 : -1;
            selected.go.transform.Rotate(0f, 0f, dir * rotSteps);
            accumRot -= dir * rotSteps;
        }
    }

    // La inceperea nivelului
    public void LevelStarted()
    {
        ClearOutline();
    }

    // La finalizarea nivelului
    public void LevelStopped()
    {
        if (selected.go != null)
            AddOutline(selected.go);
    }

    // Gestionarea selectiei obstacolului
    void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            var hit = Physics2D.GetRayIntersection(cam.ScreenPointToRay(Input.mousePosition));

            if (
                hit.collider != null
                && created.Any(obstacle => obstacle.go == hit.collider.gameObject)
            )
            {
                ClearOutline();
                selected.go = hit.collider.gameObject;
                AddOutline(selected.go);
            }
        }
    }

    // Crearea obstacolului in functie de numele acestuia
    void Create(string prefabName)
    {
        var pf = prefabs.Find(x => x.name == prefabName);
        if (pf == null)
            return;
        var go = Instantiate(pf);

        if (prefabName == "Star")
        {
            levelHandler.stars.Add(go);
            levelHandler.UpdateStarCount();
        }

        go.transform.position = Vector3.zero;
        if (go.GetComponent<Collider2D>() == null)
            go.AddComponent<BoxCollider2D>();

        ObstacleType type;
        switch (prefabName)
        {
            case "Triangle":
                type = ObstacleType.Triangle;
                break;
            case "Circle":
                type = ObstacleType.Circle;
                break;
            case "Diamond":
                type = ObstacleType.Diamond;
                break;
            case "Square":
                type = ObstacleType.Square;
                break;
            case "Star":
                type = ObstacleType.Star;
                break;
            default:
                type = ObstacleType.Main;
                break;
        }

        created.Add(new Obstacle(go, type));
        ClearOutline();
        selected.go = go;
        AddOutline(go);
    }

    // Stergerea obstacolului selectat
    public void DeleteSelected()
    {
        if (selected.go == null)
            return;
        if (selected.go.CompareTag("Star"))
        {
            levelHandler.stars.Remove(selected.go);
            levelHandler.UpdateStarCount();
        }

        foreach (var crt in created)
            if (crt.go == selected.go)
            {
                created.Remove(crt);
                break;
            }

        Destroy(selected.go);
        ClearOutline();

        if (created.Count > 0)
        {
            selected = created[created.Count - 1];
            AddOutline(selected.go);
        }
        else
        {
            selected.go = null;
        }
    }

    // Dam highlight la obstacolul selectat
    void AddOutline(GameObject go)
    {
        SpriteRenderer sr = null;

        if (go != null)
            sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var o = new GameObject("Outline");
            o.transform.SetParent(go.transform, false);
            var os = o.AddComponent<SpriteRenderer>();

            if (sr != null)
                os.sprite = sr.sprite;

            os.color = Color.black;
            os.sortingOrder = sr.sortingOrder - 1;
            o.transform.localScale = Vector3.one * 1.05f;
        }
    }

    // Stergem highlightul obstacolului selectat
    public void ClearOutline()
    {
        if (selected.go == null)
            return;
        var o = selected.go.transform.Find("Outline");

        if (o)
            Destroy(o.gameObject);
    }
}
