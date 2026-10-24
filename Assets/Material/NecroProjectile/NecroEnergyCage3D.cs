using UnityEngine;

/// <summary>
/// 自動生成與 Three.js 工作台 100% 相同的「3D 二十面體網格能量籠 + 暗黑死靈內核」
/// 直接掛在 NecroProjectile_Prefab 底下即可自動建立！
/// </summary>
[ExecuteAlways]
public class NecroEnergyCage3D : MonoBehaviour
{
    [Header("=== 能量籠與黑核尺寸 ===")]
    public float cageRadius = 0.38f;
    public float innerCoreRadius = 0.22f;
    public float wireThickness = 0.025f;

    [Header("=== 3D 自轉速度 ===")]
    public Vector3 cageSpinSpeed = new Vector3(140f, 220f, 90f);

    [Header("=== 顏色與材質 (HDR 發光) ===")]
    [ColorUsage(true, true)]
    public Color cageWireColor = new Color(0.22f, 1.0f, 0.08f, 1f) * 2.5f; // 亮死靈綠 (HDR)
    public Color darkCoreColor = new Color(0.05f, 0.01f, 0.08f, 1f);       // 深淵暗紫黑

    private Transform _cagePivot;
    private Transform _darkCore;
    private Material _wireMat;
    private Material _coreMat;

    private void OnEnable()
    {
        Rebuild3DCage();
    }

    [ContextMenu("重新生成 3D 網格能量籠")]
    public void Rebuild3DCage()
    {
        // 清理舊的子物件
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "_Generated_3DCage" || child.name == "_Generated_DarkCore")
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

        // 1. 建立正中央的「暗黑死靈內核 (壓住亮光對比度)」
        GameObject coreObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreObj.name = "_Generated_DarkCore";
        coreObj.transform.SetParent(transform, false);
        coreObj.transform.localScale = Vector3.one * (innerCoreRadius * 2f);
        if (coreObj.TryGetComponent<Collider>(out var col))
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }

        _coreMat = new Material(unlitShader);
        _coreMat.SetColor("_BaseColor", darkCoreColor);
        _coreMat.color = darkCoreColor;
        coreObj.GetComponent<MeshRenderer>().sharedMaterial = _coreMat;
        _darkCore = coreObj.transform;

        // 2. 建立外層「3D 二十面體幾何線框籠 (Icosahedron Wireframe Cage)」
        GameObject cageObj = new GameObject("_Generated_3DCage");
        cageObj.transform.SetParent(transform, false);
        _cagePivot = cageObj.transform;

        MeshFilter mf = cageObj.AddComponent<MeshFilter>();
        MeshRenderer mr = cageObj.AddComponent<MeshRenderer>();

        _wireMat = new Material(unlitShader);
        _wireMat.SetColor("_BaseColor", cageWireColor);
        _wireMat.color = cageWireColor;
        mr.sharedMaterial = _wireMat;

        mf.sharedMesh = BuildIcosahedronWireframeMesh(cageRadius, wireThickness);
    }

    private void Update()
    {
        if (_cagePivot != null)
        {
            _cagePivot.Rotate(cageSpinSpeed * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// 產生立體粗線條的正二十面體 (Icosahedron) 網格，不依賴任何外部貼圖或模型！
    /// </summary>
    private Mesh BuildIcosahedronWireframeMesh(float radius, float thickness)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;

        Vector3[] verts = new Vector3[]
        {
            new Vector3(-1,  t,  0).normalized * radius,
            new Vector3( 1,  t,  0).normalized * radius,
            new Vector3(-1, -t,  0).normalized * radius,
            new Vector3( 1, -t,  0).normalized * radius,
            new Vector3( 0, -1,  t).normalized * radius,
            new Vector3( 0,  1,  t).normalized * radius,
            new Vector3( 0, -1, -t).normalized * radius,
            new Vector3( 0,  1, -t).normalized * radius,
            new Vector3( t,  0, -1).normalized * radius,
            new Vector3( t,  0,  1).normalized * radius,
            new Vector3(-t,  0, -1).normalized * radius,
            new Vector3(-t,  0,  1).normalized * radius
        };

        // 正二十面體的 30 條稜線
        int[,] edges = new int[,]
        {
            {0,11}, {0,5}, {0,1}, {0,7}, {0,10},
            {1,5}, {5,11}, {11,10}, {10,7}, {7,1},
            {3,9}, {3,4}, {3,2}, {3,6}, {3,8},
            {4,9}, {2,4}, {6,2}, {8,6}, {9,8},
            {4,5}, {2,11}, {6,10}, {8,7}, {9,1},
            {5,9}, {11,4}, {10,2}, {7,6}, {1,8}
        };

        int edgeCount = edges.GetLength(0);
        // 每條稜線用十字交叉的兩個矩形面構成 3D 實體線條，任何角度看都有厚度
        Vector3[] meshVerts = new Vector3[edgeCount * 8];
        int[] tris = new int[edgeCount * 24];

        int vIdx = 0;
        int tIdx = 0;

        for (int i = 0; i < edgeCount; i++)
        {
            Vector3 a = verts[edges[i, 0]];
            Vector3 b = verts[edges[i, 1]];
            Vector3 dir = (b - a).normalized;
            Vector3 normal1 = Vector3.Cross(dir, a.normalized).normalized * (thickness * 0.5f);
            Vector3 normal2 = Vector3.Cross(dir, normal1.normalized).normalized * (thickness * 0.5f);

            // 平面 1
            meshVerts[vIdx + 0] = a - normal1;
            meshVerts[vIdx + 1] = a + normal1;
            meshVerts[vIdx + 2] = b + normal1;
            meshVerts[vIdx + 3] = b - normal1;

            // 平面 2 (正交 90 度)
            meshVerts[vIdx + 4] = a - normal2;
            meshVerts[vIdx + 5] = a + normal2;
            meshVerts[vIdx + 6] = b + normal2;
            meshVerts[vIdx + 7] = b - normal2;

            int[] quadOffsets = { 0, 4 };
            foreach (int off in quadOffsets)
            {
                int bV = vIdx + off;
                // 正反兩面都畫，確保雙面可見
                tris[tIdx++] = bV + 0; tris[tIdx++] = bV + 1; tris[tIdx++] = bV + 2;
                tris[tIdx++] = bV + 0; tris[tIdx++] = bV + 2; tris[tIdx++] = bV + 3;
                tris[tIdx++] = bV + 2; tris[tIdx++] = bV + 1; tris[tIdx++] = bV + 0;
                tris[tIdx++] = bV + 3; tris[tIdx++] = bV + 2; tris[tIdx++] = bV + 0;
            }

            vIdx += 8;
        }

        Mesh m = new Mesh();
        m.name = "NecroIcosahedronWireframe";
        m.vertices = meshVerts;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}