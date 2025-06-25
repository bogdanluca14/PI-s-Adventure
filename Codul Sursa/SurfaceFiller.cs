using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SurfaceFiller : MonoBehaviour
{
    // Sistem pentru a lega punctele ce determina graficul functiei

    public Material fillMaterial;

    public void CreateFill(Vector2[] curvePoints)
    {
        int n = curvePoints.Length;
        if (n < 2)
            return;

        Vector3[] vertices = new Vector3[n * 2];
        int[] tris = new int[(n - 1) * 6];

        for (int i = 0; i < n; i++)
        {
            vertices[i] = curvePoints[i];
            vertices[i + n] = new Vector3(curvePoints[i].x, 0, 0);
        }

        int ti = 0;

        for (int i = 0; i < n - 1; i++)
        {
            tris[ti++] = i;
            tris[ti++] = i + 1;
            tris[ti++] = i + 1 + n;
            tris[ti++] = i;
            tris[ti++] = i + 1 + n;
            tris[ti++] = i + n;
        }

        var mesh = new Mesh();

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = fillMaterial;
    }
}
