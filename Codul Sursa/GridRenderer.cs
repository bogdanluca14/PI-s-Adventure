using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GridRender : MonoBehaviour
{
    // Sistem pentru Generarea axelor si a mediului in care se vor crea graficele

    public Camera cam;
    public Material lineMaterial;
    private List<LineRenderer> lines = new List<LineRenderer>();

    public float spacing = 1f;
    public float lineWidth = 0.02f;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;
        GenerateGrid();
    }

    void GenerateGrid()
    {
        foreach (var lr in lines)
            if (lr && lr.gameObject)
                Destroy(lr.gameObject);
        lines.Clear();

        if (cam == null || lineMaterial == null || spacing <= 0f)
        {
            Debug.LogWarning("GridRenderer are probleme");
            return;
        }

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        float left = cam.transform.position.x - width * 0.5f;
        float bottom = cam.transform.position.y - height * 0.5f;
        float right = left + width;
        float top = bottom + height;

        for (float x = Mathf.Floor(left / spacing) * spacing; x <= right; x += spacing)
        {
            var go = new GameObject("GridLineV");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();

            ConfigureLine(lr);

            lr.SetPosition(0, new Vector3(x, bottom, 0));
            lr.SetPosition(1, new Vector3(x, top, 0));
            lines.Add(lr);
        }

        for (float y = Mathf.Floor(bottom / spacing) * spacing; y <= top; y += spacing)
        {
            var go = new GameObject("GridLineH");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();

            ConfigureLine(lr);

            lr.SetPosition(0, new Vector3(left, y, 0));
            lr.SetPosition(1, new Vector3(right, y, 0));
            lines.Add(lr);
        }
    }

    void ConfigureLine(LineRenderer lr)
    {
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.material = lineMaterial;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;
    }
}
