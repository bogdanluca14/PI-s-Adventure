using System.Collections.Generic;
using TexDrawLib;
using TMPro;
using UnityEngine;

public class Statistics : MonoBehaviour
{
    // Variabile Globale (referinte)

    public TextMeshProUGUI efficiencyText;
    public TextMeshProUGUI efficiencyDetailsText;
    public TextMeshProUGUI maxFctText;
    public TextMeshProUGUI detailsText;
    public List<TEXDraw> functionsText;
    public CameraController cameraController;
    public CalcHandler calcHandler;
    public PlayerLauncher player;

    // Variabile Locale (referinte)

    private Animator animator;
    private LevelsManager last,
        next;
    private List<string> functions,
        fctlatex;

    // Assignul animatorului
    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.enabled = false;
    }

    // Generarea statisticii de la finalul nivelului
    public void GenerateStatistics(
        LevelsManager _last,
        LevelsManager _next,
        List<FunctionPlotter.GraphData> graphData,
        ref int eff
    )
    {
        last = _last;
        next = _next;

        functions = new List<string>();
        fctlatex = new List<string>();

        foreach (var graph in graphData)
        {
            functions.Add(graph.cleanExpr);
            fctlatex.Add(graph.latexExpr);
        }
        for (int i = 0; i < functions.Count; i++)
        {
            functions[i] = functions[i].Replace("0.", "");
            functions[i] = functions[i].Replace(".0", "");
            functions[i] = functions[i].Replace(".", "");
            functions[i] = functions[i].Replace("(", "");
            functions[i] = functions[i].Replace(")", "");
            functions[i] = functions[i].Replace("/", "/.");
        }
        if (last != null && next != null)
        {
            eff = (int)GenerateEfficiency();

            efficiencyText.text = eff.ToString();
            efficiencyText.color = GetGradientColor(eff);

            maxFctText.text = next.nrFctAllowed.ToString();
            detailsText.text = next.details;
        }
        else
        {
            efficiencyText.text = "-";
            efficiencyText.color = Color.gray;

            maxFctText.text = string.Empty;
            detailsText.text = string.Empty;

            efficiencyDetailsText.text =
                "Nivelurile introduse prin intermediul unui cod nu pot calcula eficiența soluției.";
        }

        ShowFunctions();

        OpenUI();
    }

    // Generarea inteligenta a eficientei solutiei utilizatorului
    private float GenerateEfficiency()
    {
        float user = 0f,
            pot = 0f;

        foreach (var func in functions)
        {
            user += func.Length;
        }

        foreach (var func in last.solutions)
        {
            pot += func.function.Length;
        }

        if (user <= pot)
        {
            efficiencyDetailsText.text =
                "Felicitări! Ai obținut eficiența maximă, ești un adevărat matematician!";
            return 100f;
        }
        else
        {
            efficiencyDetailsText.text =
                "Pentru a spori eficiența, încearcă să scurtezi inteligent ecuațiile funcțiilor!";
            return 100f * pot / user;
        }
    }

    // Generarea culorii eficientei
    public static Color GetGradientColor(float p)
    {
        ColorUtility.TryParseHtmlString("#AF0000", out Color red);
        ColorUtility.TryParseHtmlString("#C5B81A", out Color yellow);
        ColorUtility.TryParseHtmlString("#0E8A0C", out Color green);

        if (p <= 50)
        {
            float t = (p - 1) / 49f;
            return Color.Lerp(red, yellow, t);
        }
        else
        {
            float t = (p - 51) / 49f;
            return Color.Lerp(yellow, green, t);
        }
    }

    // Afisarea functiilor utilizate
    private void ShowFunctions()
    {
        int indice = 0;

        foreach (var func in fctlatex)
        {
            if (string.IsNullOrEmpty(func))
                continue;

            functionsText[indice].text = "$f_" + (indice + 1).ToString() + "(x)=" + func + "$";
            ++indice;
        }
        while (indice < 4)
            functionsText[indice++].text = string.Empty;
    }

    // Deschiderea meniului de statistica
    public void OpenUI(bool startBlur = true)
    {
        animator.enabled = true;
        animator.Play("StatisticsOpen", 0, 0f);

        if (startBlur)
            cameraController.StartBlur();

        if (calcHandler.isOpen)
            calcHandler.OnToggleCalculator();

        player.DisableImportantBtns(true);
    }

    // Inchiderea meniului de statistica
    public void CloseUI()
    {
        player.LevelHNewLevel();
        animator.enabled = true;

        animator.Play("StatisticsClose", 0, 0f);

        cameraController.StopBlur();

        player.EnableImportantBtns();
    }

    // Restartarea nivelului curent
    public void RestartUI()
    {
        --PlayerLauncher.ind;

        player.LevelHNewLevel();
        animator.enabled = true;
        animator.Play("StatisticsClose", 0, 0f);

        cameraController.StopBlur();

        player.EnableImportantBtns();
    }

    // Restartarea nivelului personalizat (custom)
    public void CustomRestartUI()
    {
        CustomLevelHandler.instance.LoadCustom(true);

        player.levelH.ResetPlayerPos();
        player.levelEnding = false;

        animator.enabled = true;
        animator.Play("StatisticsClose", 0, 0f);

        cameraController.StopBlur();

        player.EnableImportantBtns();

        for (int i = 0; i < 4; ++i)
            player.lineManager.OnSave();
    }
}
