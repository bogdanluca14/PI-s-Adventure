using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TexDrawLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Parsarea expresiei matematice
// (transformarea din expresie
// in ecuatie matematica)
public class ExpressionBuilder : MonoBehaviour
{
    // Variabile Globale

    // Referinte Globale
    public TEXDraw texDraw;
    public UnityEvent OnExprChanged;
    public List<string> buttonsToKeep = new List<string>();

    // Variabile ce contin informatii utile
    public struct Context
    {
        public string type;
        public int stage;
    }

    public bool disableButtons = false;
    public bool forceDisable = false;

    // Variabile Locale

    private StringBuilder latexExpr = new StringBuilder(256);
    private Coroutine _holdResetRoutine = null;

    private readonly string[] Operators = { "+", "-", "*", "/" };
    private Stack<Context> contextStack = new Stack<Context>();
    private Stack<int> positionStack = new Stack<int>();

    // Resetarea expresiei
    public void ResetValues()
    {
        latexExpr.Clear();
        contextStack.Clear();
        positionStack.Clear();

        if (OnExprChanged == null)
            OnExprChanged = new UnityEvent();

        RefreshAll();

        if (disableButtons)
            DisableBtns();
    }

    // Dezactivarea butoanelor (in cazul in care se solicita)
    public void DisableBtns(bool noForcedDisable = false)
    {
        if (noForcedDisable)
            forceDisable = false;

        foreach (Transform child in transform)
        {
            if (child.gameObject.TryGetComponent<Button>(out Button btn))
            {
                if (forceDisable || !buttonsToKeep.Contains(child.gameObject.name))
                    btn.interactable = false;
                else
                    btn.interactable = true;
            }
        }
    }

    [Serializable]
    public struct BuilderState
    {
        public string latex;
        public List<Context> contexts;
        public List<int> positions;
    }

    // Salvarea si obtinerea expresiei
    public BuilderState GetState()
    {
        return new BuilderState
        {
            latex = GetClosedLaTeX().ToString(),
            contexts = contextStack.Reverse().ToList(),
            positions = positionStack.Reverse().ToList(),
        };
    }

    // Incarcarea expresiei
    public void LoadState(BuilderState st)
    {
        latexExpr.Clear().Append(st.latex);
        contextStack.Clear();

        if (st.contexts != null)
        {
            foreach (var ctx in st.contexts)
                contextStack.Push(ctx);
        }

        positionStack.Clear();

        if (st.positions != null)
        {
            foreach (var pos in st.positions)
                positionStack.Push(pos);
        }

        RefreshAll();
    }

    // Salvarea pozitiei in expresie
    private void SavePosition()
    {
        positionStack.Push(latexExpr.Length);
    }

    // La scrierea fiecarui caracter
    public void OnCharacter(string s)
    {
        SavePosition();
        latexExpr.Append(s);
        RefreshAll();
    }

    // La scrierea fiecarui operator
    public void OnOperator(string op)
    {
        SavePosition();
        latexExpr.Append(op);
        RefreshAll();
    }

    // La deschiderea parantezei
    public void OnOpenParen() => OnCharacter("(");

    // La inchiderea parantezei
    public void OnCloseParen() => OnCharacter(")");

    // La inceperea fractiei
    public void OnFractionStart()
    {
        SavePosition();
        latexExpr.Append("\\frac{");
        contextStack.Push(new Context { type = "Fraction", stage = 0 });

        RefreshAll();
    }

    // La inceperea radicalului
    public void OnSqrtStart()
    {
        SavePosition();
        latexExpr.Append("\\sqrt{");
        contextStack.Push(new Context { type = "Radical", stage = 0 });
        RefreshAll();
    }

    // La inceperea exponentierii
    public void OnExponentStart()
    {
        SavePosition();
        latexExpr.Append("^{");
        contextStack.Push(new Context { type = "Exponent", stage = 0 });
        RefreshAll();
    }

    // La apasarea butonului "Next"
    public void OnNext()
    {
        if (contextStack.Count == 0)
            return;

        var temp = new Stack<Context>();
        bool applied = false;

        while (contextStack.Count > 0)
        {
            var ctx = contextStack.Pop();
            int maxStage = ctx.type == "Fraction" ? 2 : 1;

            if (!applied && ctx.stage < maxStage)
            {
                latexExpr.Append("}");
                if (ctx.type == "Fraction" && ctx.stage == 0)
                    latexExpr.Append("{");

                ctx.stage++;
                applied = true;
            }

            temp.Push(ctx);
            if (applied)
                break;
        }
        while (temp.Count > 0)
            contextStack.Push(temp.Pop());

        if (applied)
            RefreshAll();
    }

    // La apasarea butonului "Back"
    public void OnBack()
    {
        if (positionStack.Count == 0)
            return;

        latexExpr.Length = positionStack.Pop();

        while (contextStack.Count > 0)
        {
            var ctx = contextStack.Peek();

            if (ctx.type == "Fraction" && !latexExpr.ToString().Contains("\\frac{"))
                contextStack.Pop();
            else if (ctx.type == "Radical" && !latexExpr.ToString().Contains("\\sqrt{"))
                contextStack.Pop();
            else if (ctx.type == "Exponent" && !latexExpr.ToString().Contains("^{"))
                contextStack.Pop();
            else
                break;
        }

        if (contextStack.Count > 0)
        {
            var ctx = contextStack.Pop();
            ctx.stage = 0;
            contextStack.Push(ctx);
        }

        RefreshAll();
    }

    // La tinerea apasata a butonului "Back"
    public void OnBackPointerDown()
    {
        if (_holdResetRoutine != null)
            StopCoroutine(_holdResetRoutine);

        _holdResetRoutine = StartCoroutine(HoldResetCoroutine());
    }

    // La oprirea apasarii a butonului "Back"
    public void OnBackPointerUp()
    {
        if (_holdResetRoutine != null)
            StopCoroutine(_holdResetRoutine);

        _holdResetRoutine = null;
    }

    // Resetarea temporizatorului de apasare pentru butonul "Back"
    private IEnumerator HoldResetCoroutine()
    {
        yield return new WaitForSeconds(1.5f);

        latexExpr.Clear();
        contextStack.Clear();
        positionStack.Clear();
        RefreshAll();
    }

    // Actualizarea display-ului calculatorului
    private void RefreshAll()
    {
        string tex = " \\(f(x) = " + latexExpr.ToString() + "\\triangleleft\\)";

        texDraw.text = tex;
        texDraw.orchestrator.Parse(tex);
        texDraw.orchestrator.Box();
        texDraw.orchestrator.Render();

        OnExprChanged.Invoke();
    }

    // Obtinerea expresiei dupa auto-completarea inteligenta
    public string GetClosedLaTeX()
    {
        var sb = new StringBuilder(latexExpr.ToString());
        if (contextStack.Count > 0)
        {
            var stack = new Stack<Context>(contextStack.Reverse());

            while (stack.Count > 0)
            {
                var ctx = stack.Pop();

                if (ctx.type == "Fraction")
                {
                    if (ctx.stage == 0)
                        sb.Append("}{");
                    sb.Append("}");
                }
                else
                {
                    sb.Append("}");
                }
            }
        }

        return BalanceBraces(sb.ToString());
    }

    // Inchiderea automata a parantezelor unde este nevoie
    public string BalanceBraces(string tex)
    {
        var sb = new StringBuilder(tex.Length);
        int openCount = 0;

        foreach (char c in tex)
        {
            if (c == '{')
            {
                openCount++;
                sb.Append(c);
            }
            else if (c == '}')
            {
                if (openCount > 0)
                {
                    openCount--;
                    sb.Append(c);
                }
            }
            else
                sb.Append(c);
        }

        for (int i = 0; i < openCount; i++)
            sb.Append('}');

        return sb.ToString();
    }
}
