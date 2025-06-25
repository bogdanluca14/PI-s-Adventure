using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LineMeshGenerator : MonoBehaviour
{
    // Sistem pentru generarea formei graficului functiei

    [HideInInspector]
    public Vector3[] points;

    [HideInInspector]
    public float thickness;

    private MeshFilter mf;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
    }

    public void GenerateMesh()
    {
        if (points == null || points.Length < 2)
            return;

        int n = points.Length;
        int[] tris = new int[(n - 1) * 6];
        Vector2[] uv = new Vector2[n * 2];
        Vector3[] verts = new Vector3[n * 2];

        for (int i = 0; i < n; i++)
        {
            Vector3 fwd = (
                i == n - 1 ? (points[i] - points[i - 1]) : (points[i + 1] - points[i])
            ).normalized;
            Vector3 nor = Vector3.Cross(fwd, Vector3.forward).normalized * (thickness * 0.5f);

            verts[2 * i] = points[i] - nor;
            verts[2 * i + 1] = points[i] + nor;

            float v = i / (n - 1f);

            uv[2 * i] = new Vector2(0, v);
            uv[2 * i + 1] = new Vector2(1, v);
        }

        int ti = 0;

        for (int i = 0; i < n - 1; i++)
        {
            int i0 = 2 * i,
                i1 = 2 * i + 1,
                i2 = 2 * (i + 1),
                i3 = 2 * (i + 1) + 1;

            tris[ti++] = i0;
            tris[ti++] = i2;
            tris[ti++] = i3;
            tris[ti++] = i0;
            tris[ti++] = i3;
            tris[ti++] = i1;
        }

        Mesh m = new Mesh();
        m.vertices = verts;
        m.uv = uv;
        m.triangles = tris;

        m.RecalculateNormals();
        mf.mesh = m;
    }
}
