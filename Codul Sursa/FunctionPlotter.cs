using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DynamicExpresso;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class FunctionPlotter : MonoBehaviour
{
    // Variabile Globale

    // Referinte Globale

    public ExpressionBuilder exprBuilder;
    public TMP_InputField minXField,
        maxXField;
    public GameObject lineMeshPrefab;
    public Material lineMaterial,
        highlightMaterial;

    // Variabile importante in afisarea graficului

    public float editDebounceTime = 0.75f;
    public float thickness = 0.05f,
        outlineThickness = 0.1f;
    public float pointsPerUnit = 10f;
    public int minResolution = 50,
        maxResolution = 1000;
    public float clampRange = 20f;
    public int nrFct = 0;
    public float initXmin = -1f;
    public float initXmax = 1f;

    // Variabile Locale

    private Interpreter interp;
    private const float epsilon = 0.01f;

    // Clasa pentru a pastra toate informatiile referitoare graficului

    [Serializable]
    public class GraphData
    {
        public string cleanExpr;
        public string latexExpr;
        public ExpressionBuilder.BuilderState builderState;
        public float xMin,
            xMax;
        public int res;
    }

    public Dictionary<GameObject, GraphData> graphDatas = new();
    private readonly Dictionary<GameObject, GameObject> outlineCopies = new();
    private readonly List<GameObject> savedGraphs = new();

    // Variabile utilizate in implementare

    private bool isEditing = false;
    private GameObject editingGraph = null;
    private Coroutine debounceRoutine = null;
    private Vector3[] voidPoints;

    // Assignul referintelor si a unor variabile importante
    void Awake()
    {
        interp = new Interpreter()
            .Reference(typeof(Math))
            .SetFunction("Log", (Func<double, double>)Math.Log)
            .SetFunction("Log", (Func<double, double, double>)Math.Log)
            .SetFunction("Sin", (Func<double, double>)Math.Sin)
            .SetFunction("Cos", (Func<double, double>)Math.Cos)
            .SetFunction("Tan", (Func<double, double>)Math.Tan)
            .SetFunction("Exp", (Func<double, double>)Math.Exp)
            .SetFunction("Abs", (Func<double, double>)Math.Abs)
            .SetFunction("Sqrt", (Func<double, double>)Math.Sqrt);

        if (exprBuilder == null)
            Debug.LogError("FunctionPlotter: n-avem ExpressionBuilder");

        ResetValues();

        voidPoints = new Vector3[] { new Vector3(50, 50, 0), new Vector3(51, 51, 0) };
    }

    // Crearea graficului propriu-zis
    public GameObject CreateGraph(
        string tex = "",
        float minX = -1f,
        float maxX = 1f,
        ExpressionBuilder.BuilderState state = new ExpressionBuilder.BuilderState()
    )
    {
        string raw = ConvertLatexToRaw(tex).Trim();

        string clean = NormalizeExpr(raw);

        int res = Mathf.Clamp(
            Mathf.CeilToInt((maxX - minX) * pointsPerUnit),
            minResolution,
            maxResolution
        );

        var graphGO = new GameObject($"Funcția {++nrFct}");
        var meshGO = Instantiate(lineMeshPrefab, graphGO.transform);
        meshGO.transform.localPosition = Vector3.zero;
        var meshComp = meshGO.GetComponent<LineMeshGenerator>();

        if (string.IsNullOrEmpty(state.latex))
            meshComp.points = voidPoints;
        else
            meshComp.points = SamplePoints(clean, minX, maxX, res);

        meshComp.thickness = thickness;
        var mr = meshGO.GetComponent<MeshRenderer>();
        mr.material = lineMaterial;
        mr.sortingOrder = 1;
        meshComp.GenerateMesh();

        var outline = Instantiate(meshGO, graphGO.transform);
        outline.name = "Outline";
        var oComp = outline.GetComponent<LineMeshGenerator>();
        oComp.thickness = outlineThickness;
        var omr = outline.GetComponent<MeshRenderer>();

        omr.material = highlightMaterial;
        omr.sortingOrder = 0;
        omr.enabled = false;
        oComp.GenerateMesh();
        outlineCopies[graphGO] = outline;

        var selGO = new GameObject("SelectionCollider");
        selGO.transform.SetParent(graphGO.transform, false);
        var selCol = selGO.AddComponent<EdgeCollider2D>();
        selCol.isTrigger = true;
        selCol.points = meshComp.points.Select(v => (Vector2)v).ToArray();
        var physGO = new GameObject("PhysicsCollider");
        physGO.transform.SetParent(graphGO.transform, false);
        var physCol = physGO.AddComponent<EdgeCollider2D>();

        physCol.isTrigger = false;
        physCol.points = selCol.points;
        physCol.enabled = false;

        graphDatas[graphGO] = new GraphData
        {
            cleanExpr = clean,
            latexExpr = tex,
            builderState = state,
            xMin = minX,
            xMax = maxX,
            res = res,
        };
        savedGraphs.Add(graphGO);

        return graphGO;
    }

    // Inceperea editari graficului dupa creare
    public void BeginEdit(GameObject graphGO)
    {
        if (!graphDatas.ContainsKey(graphGO))
            return;

        isEditing = true;
        editingGraph = graphGO;
        var d = graphDatas[graphGO];

        exprBuilder.LoadState(d.builderState);
        SetField(minXField, d.xMin);
        SetField(maxXField, d.xMax);

        minXField.onValueChanged.AddListener(_ => ScheduleEdit());
        maxXField.onValueChanged.AddListener(_ => ScheduleEdit());
    }

    // Adaugarea task-ului de editare
    public void ScheduleEdit()
    {
        if (debounceRoutine != null)
            StopCoroutine(debounceRoutine);
        debounceRoutine = StartCoroutine(DebounceEdit());
    }

    // Resetarea domeniului si a functiei
    public void ResetValues()
    {
        SetField(minXField, initXmin);
        SetField(maxXField, initXmax);
        exprBuilder.ResetValues();
    }

    // Editarea propriu-zisa a functiei
    IEnumerator DebounceEdit()
    {
        yield return new WaitForSeconds(editDebounceTime);

        if (editingGraph == null)
            yield break;

        if (
            !float.TryParse(
                minXField.text.Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float xMin
            )
        )
            yield break;
        if (
            !float.TryParse(
                maxXField.text.Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float xMax
            )
        )
            yield break;

        string tex = exprBuilder.GetClosedLaTeX();

        if (string.IsNullOrEmpty(tex))
        {
            EmptyGraph(editingGraph);
            graphDatas[editingGraph] = new GraphData
            {
                cleanExpr = string.Empty,
                latexExpr = string.Empty,
                builderState = exprBuilder.GetState(),
                xMin = Mathf.Max(xMin, -clampRange),
                xMax = Mathf.Min(xMax, clampRange),
                res = 0,
            };
            yield break;
        }

        string raw = ConvertLatexToRaw(tex).Trim();
        var state = exprBuilder.GetState();
        string clean = NormalizeExpr(raw);
        Vector2 lim = ComputeDomain(clean);

        xMin = Mathf.Max(xMin, lim.x, -clampRange);
        xMax = Mathf.Min(xMax, lim.y, clampRange);

        if (xMax <= xMin)
            yield break;

        SetField(minXField, xMin);
        SetField(maxXField, xMax);

        int res = Mathf.Clamp(
            Mathf.CeilToInt((xMax - xMin) * pointsPerUnit),
            minResolution,
            maxResolution
        );

        graphDatas[editingGraph] = new GraphData
        {
            cleanExpr = clean,
            latexExpr = tex,
            builderState = state,
            xMin = xMin,
            xMax = xMax,
            res = res,
        };

        UpdateGraph(editingGraph, clean, xMin, xMax, res);
    }

    // Incheierea editarii
    public void EndEdit()
    {
        if (!isEditing)
            return;

        var final = exprBuilder.GetState();

        if (editingGraph != null)
            graphDatas[editingGraph].builderState = final;

        isEditing = false;
        editingGraph = null;

        minXField.onValueChanged.RemoveAllListeners();
        maxXField.onValueChanged.RemoveAllListeners();
    }

    // Actualizarea graficului
    public void UpdateGraph(GameObject go, string clean, float xMin, float xMax, int res)
    {
        var meshComp = go.GetComponentInChildren<LineMeshGenerator>();
        meshComp.points = SamplePoints(clean, xMin, xMax, res);
        meshComp.thickness = thickness;
        meshComp.GenerateMesh();

        if (outlineCopies.TryGetValue(go, out var outline))
        {
            var oComp = outline.GetComponent<LineMeshGenerator>();
            oComp.points = meshComp.points;
            oComp.thickness = outlineThickness;
            oComp.GenerateMesh();
        }

        var sel = go.transform.Find("SelectionCollider").GetComponent<EdgeCollider2D>();
        var phys = go.transform.Find("PhysicsCollider").GetComponent<EdgeCollider2D>();
        sel.points = meshComp.points.Select(v => (Vector2)v).ToArray();
        phys.points = sel.points;
    }

    // Golirea graficului
    void EmptyGraph(GameObject go)
    {
        var meshComp = go.GetComponentInChildren<LineMeshGenerator>();
        meshComp.points = voidPoints;
        meshComp.thickness = thickness;
        meshComp.GenerateMesh();

        if (outlineCopies.TryGetValue(go, out var outline))
        {
            var oComp = outline.GetComponent<LineMeshGenerator>();
            oComp.points = meshComp.points;
            oComp.thickness = outlineThickness;
            oComp.GenerateMesh();
        }

        var sel = go.transform.Find("SelectionCollider").GetComponent<EdgeCollider2D>();
        var phys = go.transform.Find("PhysicsCollider").GetComponent<EdgeCollider2D>();
        sel.points = meshComp.points.Select(v => (Vector2)v).ToArray();
        phys.points = sel.points;
    }

    // Sistem de conversie din LaTeX in expresie matematica
    private string ConvertLatexToRaw(string tex)
    {
        if (string.IsNullOrEmpty(tex))
            return string.Empty;

        tex = tex.Replace("\\cdot ", "*");

        tex = tex.Replace("sin", "Sin");
        tex = tex.Replace("cos", "Cos");
        tex = tex.Replace("ln", "Log");

        tex = Regex.Replace(tex, @"\^\{\s*\}|\^\(\s*\)", "^(1)");
        tex = Regex.Replace(tex, @"\^\{\s*([+\-]?\d+)\s*\}", m => "^(" + m.Groups[1].Value + ")");
        tex = Regex.Replace(tex, @"\^\(\s*([+\-]?\d+)\s*\)", m => "^(" + m.Groups[1].Value + ")");

        var fracRx = new Regex(@"\\frac\{([^{}]+)\}\{([^{}]+)\}");
        while (fracRx.IsMatch(tex))
            tex = fracRx.Replace(tex, "(($1)/($2))");

        tex = Regex.Replace(tex, @"\\sqrt\{([^{}]+)\}", "(Sqrt($1))");
        tex = tex.Replace("{", "(").Replace("}", ")");

        return tex;
    }

    // Crearea unui highlight asupra graficului selectat
    public void HighlightGraphGO(GameObject go)
    {
        ClearHighlight();

        if (go != null && outlineCopies.TryGetValue(go, out var outline))
            outline.GetComponent<MeshRenderer>().enabled = true;
    }

    // Stergerea highlightului asupra graficului selectat
    public void ClearHighlight()
    {
        foreach (var kv in outlineCopies.ToList())
        {
            if (kv.Value != null)
                kv.Value.GetComponent<MeshRenderer>().enabled = false;
            else
                outlineCopies.Remove(kv.Key);
        }
    }

    // Activarea "fizicii" si a coliziunilor cu obstacolele
    public void EnableAllGraphPhysicsColliders()
    {
        foreach (var g in savedGraphs.Where(x => x != null))
        {
            var phys = g.GetComponentsInChildren<EdgeCollider2D>()
                .FirstOrDefault(c => !c.isTrigger);

            if (phys)
                phys.enabled = true;
        }

        ClearHighlight();
    }

    // Dezctivarea "fizicii" si a coliziunilor cu obstacolele
    public void DisableAllGraphPhysicsColliders()
    {
        foreach (var g in savedGraphs.Where(x => x != null))
        {
            var phys = g.GetComponentsInChildren<EdgeCollider2D>()
                .FirstOrDefault(c => !c.isTrigger);

            if (phys)
                phys.enabled = false;
        }
    }

    // Stergerea unui anumit grafic
    public void RemoveGraph(GameObject go)
    {
        savedGraphs.Remove(go);

        if (outlineCopies.TryGetValue(go, out var o))
        {
            Destroy(o);
            outlineCopies.Remove(go);
        }

        graphDatas.Remove(go);
    }

    // Normalizarea expresiei matematice
    private string NormalizeExpr(string expr)
    {
        expr = Regex.Replace(
            expr,
            @"\b(sin|cos|tan|exp|abs)\s*\(",
            m => char.ToUpper(m.Groups[1].Value[0]) + m.Groups[1].Value.Substring(1) + "(",
            RegexOptions.IgnoreCase
        );

        expr = Regex.Replace(expr, @"(?<=[0-9\)])(?=\()", "*");

        expr = Regex.Replace(expr, @"(\d+(\.\d+)?|\))(?=\()", "$1*");

        expr = Regex.Replace(expr, @"(\d+(\.\d+)?)(?=[A-Za-z])", "$1*");

        expr = ExpandParenthesizedExponents(expr);

        expr = Regex.Replace(
            expr,
            @"\(\s*([^()]+?)\s*\)\^\(\s*([+\-]?\d+)\s*\)",
            m =>
            {
                var inner = m.Groups[1].Value.Trim();
                int e = int.Parse(m.Groups[2].Value);
                if (e > 0)
                    return string.Join("*", Enumerable.Repeat($"({inner})", e));
                else if (e < 0)
                    return "1/(" + string.Join("*", Enumerable.Repeat($"({inner})", -e)) + ")";
                else
                    return "1";
            }
        );

        expr = Regex.Replace(
            expr,
            @"(\([^()]+\)|[A-Za-z]\w*|\d+)\^\(\s*([+\-]?\d+)\s*\)",
            m =>
            {
                var baseExpr = m.Groups[1].Value;
                int expVal = int.Parse(m.Groups[2].Value);
                if (expVal > 0)
                    return string.Join("*", Enumerable.Repeat(baseExpr, expVal));
                else if (expVal < 0)
                    return "1/(" + string.Join("*", Enumerable.Repeat(baseExpr, -expVal)) + ")";
                else
                    return "1";
            }
        );

        expr = Regex.Replace(expr, @"(?<=^|[^.\d])(\d+)(?=$|[^.\d])", "$1.0");

        return expr;
    }

    // Calcularea exponentului (care se afla, initial, intre paranteze)
    private string ExpandParenthesizedExponents(string expr)
    {
        int scan = 0;
        while (true)
        {
            int idx = expr.IndexOf(")^(", scan);
            if (idx < 0)
                break;

            int depth = 0;
            int baseOpen = -1;

            for (int i = idx; i >= 0; i--)
            {
                if (expr[i] == ')')
                    depth++;
                else if (expr[i] == '(')
                {
                    depth--;
                    if (depth == 0)
                    {
                        baseOpen = i;
                        break;
                    }
                }
            }

            if (baseOpen < 0)
            {
                scan = idx + 1;
                continue;
            }

            int nameStart = baseOpen - 1;
            while (
                nameStart >= 0 && (char.IsLetterOrDigit(expr[nameStart]) || expr[nameStart] == '_')
            )
                nameStart--;
            nameStart++;

            string baseSub = expr.Substring(nameStart, idx + 1 - nameStart);

            int expOpen = idx + 2;
            int expClose = expr.IndexOf(')', expOpen);
            if (expClose < 0)
                break;

            string expTxt = expr.Substring(expOpen + 1, expClose - expOpen - 1).Trim();
            if (!int.TryParse(expTxt, out int expVal))
            {
                scan = expClose + 1;
                continue;
            }

            string replacement;
            if (expVal > 0)
            {
                replacement = string.Join("*", Enumerable.Repeat("(" + baseSub + ")", expVal));
            }
            else if (expVal < 0)
            {
                replacement =
                    "1/(" + string.Join("*", Enumerable.Repeat("(" + baseSub + ")", -expVal)) + ")";
            }
            else
            {
                replacement = "1";
            }

            expr = expr.Substring(0, nameStart) + replacement + expr.Substring(expClose + 1);

            scan = 0;
        }

        return expr;
    }

    // Calcularea conditiilor de existenta si intersectarea lor cu domeniul de definitie
    private Vector2 ComputeDomain(string expr)
    {
        float domainMin = float.NegativeInfinity;
        float domainMax = float.PositiveInfinity;
        const float eps = 1e-4f;

        var rx = new Regex(@"\b(Log|Sqrt)\s*\(\s*([^\)]+)\)", RegexOptions.IgnoreCase);
        foreach (Match m in rx.Matches(expr))
        {
            string fn = m.Groups[1].Value.ToLower();
            string inner = m.Groups[2].Value.Trim();

            var lin = Regex.Match(inner, @"^([+\-]?\d*\.?\d*)\*?x");
            if (!lin.Success)
                continue;

            float a = 1f;
            if (
                !string.IsNullOrEmpty(lin.Groups[1].Value)
                && lin.Groups[1].Value != "+"
                && lin.Groups[1].Value != "-"
            )
                a = float.Parse(
                    lin.Groups[1].Value,
                    System.Globalization.CultureInfo.InvariantCulture
                );
            else if (lin.Groups[1].Value == "-")
                a = -1f;

            float b = 0f;
            var cm = Regex.Match(inner, @"([+\-]\s*\d+(\.\d+)?)\s*$");

            if (cm.Success)
                b = float.Parse(
                    cm.Groups[1].Value.Replace(" ", ""),
                    System.Globalization.CultureInfo.InvariantCulture
                );

            float threshold = -b / a;
            bool isLowerBound;
            if (fn == "log")
            {
                if (a > 0)
                {
                    isLowerBound = true;
                    threshold += eps;
                }
                else
                {
                    isLowerBound = false;
                    threshold -= eps;
                }
            }
            else
            {
                if (a > 0)
                    isLowerBound = true;
                else
                    isLowerBound = false;
            }

            if (isLowerBound)
                domainMin = Mathf.Max(domainMin, threshold);
            else
                domainMax = Mathf.Min(domainMax, threshold);
        }

        return new Vector2(domainMin, domainMax);
    }

    // Calcularea coordonatelor punctelor ce determina graficul functiei
    Vector3[] SamplePoints(string expr, float xMin, float xMax, int res)
    {
        var pts = new Vector3[res];

        for (int i = 0; i < res; i++)
        {
            float t = i / (res - 1f),
                x = Mathf.Lerp(xMin, xMax, t);
            double yd = 0;

            try
            {
                interp.SetVariable("x", (double)x);
                yd = Convert.ToDouble(interp.Eval(expr));
            }
            catch
            {
                yd = 0;
            }

            float y = ClampY((float)yd);

            if (float.IsNaN(y) || float.IsInfinity(y))
                y = 0f;

            pts[i] = new Vector3(x, y, 0f);
        }

        return pts;
    }

    // Limitarea valorii y la World Space
    float ClampY(float y)
    {
        if (y < -20f)
            return -20f;
        if (y > 20f)
            return 20f;
        return y;
    }

    // Afisarea domeniului de definitie
    void SetField(TMP_InputField f, float val)
    {
        f.text = val.ToString();
    }
}
