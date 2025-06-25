using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class PlayerLauncher : MonoBehaviour
{
    // Variabile Globale

    // Referinte Globale

    public Button launchButton;
    public FunctionPlotter plotter;
    public PhysicsMaterial2D ballMat;
    public Rigidbody2D rb;
    public AnimationHandler animH;
    public LevelHandler levelH;
    public LineManager lineManager;
    public CalcHandler calcH;
    public CameraController cameraController;
    public TMP_InputField minX;
    public TMP_InputField maxX;
    public Button calcBtn;
    public Button resetBtn;
    public Button optionsBtn;
    public Image boxImg;
    public Options options;
    public GameObject confettiL;
    public GameObject confettiR;
    public Sprite inAir;
    private SpriteRenderer spriteRenderer;

    // Informatii importante pentru fizica jocului

    public float slideScale = 0.2f;
    public float airDrag = 0.15f;
    public float groundDrag = 1.5f;
    public float groundAngularDrag = 3f;
    public bool launched = false;
    public Vector3 startPos;

    // Informatii privind stadiul curent al jocului

    public bool levelEnding = false;
    public bool inTutorial = true;
    public bool inCustom = false;
    public static int ind;
    public List<Sprite> randomGround;
    public List<LevelsManager> levels;

    // Assignul referintelor si al informatiilor utile
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        lineManager = plotter.gameObject.GetComponent<LineManager>();
        startPos = transform.position;
        Physics2D.gravity = new Vector2(0f, -9.81f);

        rb = GetComponent<Rigidbody2D>();
        rb.mass = 1f;
        rb.gravityScale = 0f;
        rb.drag = airDrag;
        rb.angularDrag = groundAngularDrag;

        GetComponent<CircleCollider2D>().sharedMaterial = ballMat;
        ChangeSprite();
        launchButton.onClick.AddListener(OnLaunch);

        if (inCustom)
            return;
        if (ind < 0)
            levelH.NewLevel(levels[0]);
        else
            levelH.NewLevel(levels[ind]);
    }

    // La apasarea butonului "Lanseaza"
    public void OnLaunch()
    {
        if (launched)
            return;

        launched = true;

        if (CustomLevelHandler.instance != null)
            CustomLevelHandler.instance.LevelStarted();
        foreach (var entry in lineManager.entries)
            entry.ui.interactable = false;

        rb.gravityScale = 1f;
        rb.drag = airDrag;

        foreach (var ec in FindObjectsOfType<EdgeCollider2D>())
            if (ec.gameObject.name.StartsWith("Functia"))
                ec.enabled = true;

        plotter.EnableAllGraphPhysicsColliders();
        calcH.OnForceClose();
        options.BackBtn();
        DisableImportantBtns();

        resetBtn.interactable = true;
        spriteRenderer.sprite = inAir;
    }

    // Dezactivarea butoanelor din UI-ul jocului
    public void DisableImportantBtns(bool disableCam = false)
    {
        foreach (var entry in lineManager.entries)
            entry.ui.interactable = false;
        launchButton.interactable = false;

        if (disableCam)
            cameraController.canMove = false;

        calcBtn.interactable = false;
        optionsBtn.interactable = false;
        minX.interactable = false;
        maxX.interactable = false;

        Color color = boxImg.color;
        color.a = 0.66f;

        if (ind >= 0)
            boxImg.color = color;
        if (CustomLevelHandler.instance != null)
            foreach (var button in CustomLevelHandler.instance.buttons)
                button.interactable = false;
    }

    // Activarea butoanelor din UI-ul jocului
    public void EnableImportantBtns()
    {
        foreach (var entry in lineManager.entries)
            entry.ui.interactable = true;

        launchButton.interactable = true;
        cameraController.canMove = true;
        calcBtn.interactable = true;

        if (ind >= 0)
            optionsBtn.interactable = true;

        minX.interactable = true;
        maxX.interactable = true;
        Color color = boxImg.color;
        color.a = 1f;
        boxImg.color = color;

        if (CustomLevelHandler.instance != null)
            foreach (var button in CustomLevelHandler.instance.buttons)
                button.interactable = true;
    }

    // La colizionarea cu un obstacol/grafic
    void OnCollisionEnter2D(Collision2D c)
    {
        var ec = c.collider as EdgeCollider2D;

        AudioManager.instance.PlaySound(ec == null ? "hitGraph" : "hitObstacle");
        ChangeSprite();

        if (!launched || ec == null || !ec.gameObject.name.StartsWith("Functia"))
            return;

        rb.drag = groundDrag;
        rb.angularDrag = groundAngularDrag;
    }

    // Aplicarea fortei de frecare, in contact cu graficul
    void OnCollisionStay2D(Collision2D c)
    {
        var ec = c.collider as EdgeCollider2D;
        if (!launched || ec == null || !ec.gameObject.name.StartsWith("Functia"))
            return;

        Vector2 n = c.contacts[0].normal;
        Vector2 t = new Vector2(-n.y, n.x).normalized;
        Vector2 g = Physics2D.gravity * rb.gravityScale;

        rb.AddForce(Vector2.Dot(g, t) * t * slideScale, ForceMode2D.Force);
    }

    // Iesirea de pe suprafata graficului
    void OnCollisionExit2D(Collision2D c)
    {
        var ec = c.collider as EdgeCollider2D;

        spriteRenderer.sprite = inAir;
        if (!launched || ec == null || !ec.gameObject.name.StartsWith("Functia"))
            return;

        rb.drag = airDrag;
    }

    // La colectarea unei stele
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Star") || !launched)
            return;

        if (!levelH.starsPopped.Contains(other.gameObject.GetInstanceID()))
        {
            levelH.starsPopped.Add(other.gameObject.GetInstanceID());
            levelH.UpdateStars();

            StarCollect starCollect = other.gameObject.GetComponent<StarCollect>();
            starCollect.PlayPop();

            if (levelH.starsPopped.Count == levelH.stars.Count)
            {
                if (inCustom)
                {
                    if (CustomLevelHandler.instance.inLoad)
                    {
                        levelEnding = true;
                        StartConfetti();
                        StartCoroutine(CustomLevelEnd());
                    }

                    return;
                }

                levelEnding = true;
                StartConfetti();
                StartCoroutine(LevelEnd());
            }
        }
    }

    // Pornirea sistemului de Confetti
    void StartConfetti()
    {
        confettiL.SetActive(false);
        confettiR.SetActive(false);
        confettiL.SetActive(true);
        confettiR.SetActive(true);
        AudioManager.instance.PlaySound("finishLevel");
    }

    // Finalizarea nivelului curent
    public IEnumerator LevelEnd()
    {
        yield return new WaitForSeconds(1f);

        if (ind < 0)
        {
            cameraController.StartBlur();
            animH.ContinueSequence();
            yield break;
        }

        int eff = 0;

        plotter.EndEdit();
        List<FunctionPlotter.GraphData> graphData = lineManager.GetGraphDatas();
        lineManager.NewLevel();

        if (ind < levels.Count - 1)
            levelH.stats.GenerateStatistics(levelH.crt, levels[ind + 1], graphData, ref eff);
        else
            eff = 100;

        SaveSystem.SaveLevel(graphData, eff, ind);

        if (ind == levels.Count - 1)
            options.GameEnd();
    }

    // Finalizarea nivelului personalizat (custom)
    public IEnumerator CustomLevelEnd()
    {
        yield return new WaitForSeconds(1f);

        int eff = 0;

        plotter.EndEdit();
        List<FunctionPlotter.GraphData> graphData = lineManager.GetGraphDatas();
        lineManager.NewLevel();

        levelH.stats.GenerateStatistics(null, null, graphData, ref eff);
    }

    // Generarea urmatorului nivel
    public void LevelHNewLevel()
    {
        levelH.NewLevel(levels[++ind]);
        levelH.ResetPlayerPos();

        levelEnding = false;
    }

    // Schimbarea sprite-ului lui "PI"
    public void ChangeSprite()
    {
        int nr = Random.Range(0, randomGround.Count);
        spriteRenderer.sprite = randomGround[nr];
    }
}
