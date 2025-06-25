using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LineManager : MonoBehaviour
{
    // Variabile Globale

    // Referinte Globale si informatii utile

    public RectTransform listPanel;
    public Button itemPrefab;
    public AnimationHandler animationHandler;
    public FunctionPlotter plotter;
    public LevelHandler levelH;

    public Sprite selectedImg;
    public Sprite deselectedImg;
    public Sprite finishImg;
    public Sprite editImg;

    public bool onTutorial = false;
    public bool onTutorial2 = false;

    [System.Serializable]
    public class Entry
    {
        public Button ui;
        public GameObject go;
    }

    public List<Entry> entries = new List<Entry>();
    public bool inEdit = false;

    private Entry selected;

    // La crearea/salvarea unei noi functii
    public void OnSave(FunctionPlotter.GraphData graph = null)
    {
        GameObject go;

        if (graph == null)
            go = plotter.CreateGraph();
        else
            go = plotter.CreateGraph(
                graph.builderState.latex,
                graph.xMin,
                graph.xMax,
                graph.builderState
            );

        var btn = Instantiate(itemPrefab, listPanel);

        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>();

        if (tmp != null)
        {
            tmp.text = go.name;
        }
        else
        {
            Text legacy = btn.GetComponentInChildren<Text>();

            if (legacy != null)
                legacy.text = go.name;
            else
                Debug.LogWarning("LineManager: n-avem text");
        }

        btn.onClick.AddListener(() => Select(btn, go));

        entries.Add(new Entry { ui = btn, go = go });

        if (entries.Count == 1)
            Select(btn, go);
    }

    // Selectarea unei functii anume
    public void Select(Button btn, GameObject go)
    {
        if (selected != null)
        {
            selected.ui.image.sprite = deselectedImg;
            selected.ui.gameObject.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
        }

        selected = entries.First(e => e.ui == btn);
        selected.ui.image.sprite = selectedImg;
        selected.ui.gameObject.GetComponentInChildren<TextMeshProUGUI>().color = plotter
            .highlightMaterial
            .color;

        plotter.ClearHighlight();
        plotter.HighlightGraphGO(go);

        OnEditToggle();
    }

    // Editarea functiei selectate
    public void OnEditToggle()
    {
        if (selected == null)
            return;

        if (entries.Count > 1)
            plotter.EndEdit();

        plotter.BeginEdit(selected.go);
    }

    // Stergerea functiei selectate
    void OnDelete()
    {
        if (selected == null || inEdit)
            return;

        plotter.RemoveGraph(selected.go);

        Destroy(selected.go);
        Destroy(selected.ui.gameObject);

        entries.Remove(selected);

        plotter.ClearHighlight();
        selected = null;

        if (entries.Any())
            Select(entries.Last().ui, entries.Last().go);
    }

    // Generarea functiilor pentru un nou nivel
    public void NewLevel()
    {
        plotter.EndEdit();

        while (entries.Count > 0)
        {
            selected = entries[0];
            OnDelete();
        }

        foreach (var prefab in levelH.obstacles)
        {
            Destroy(prefab);
        }

        foreach (var prefab in levelH.stars)
        {
            Destroy(prefab);
        }

        levelH.obstacles.Clear();
        levelH.stars.Clear();

        if (PlayerLauncher.ind >= 0)
            levelH.ResetLevel();

        plotter.nrFct = 0;
        plotter.ResetValues();
    }

    // Obtinerea datelor functiilor create
    public List<FunctionPlotter.GraphData> GetGraphDatas()
    {
        return plotter.graphDatas.Values.ToList();
    }
}
