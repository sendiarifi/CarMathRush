using System.Collections.Generic;
using UnityEngine;

// Pasang ke objek Road di SampleScene. Tidak perlu mengisi apa pun.
// Membuat jalan balap BERBUKIT (aspal, garis jalur, kerb) dan tanah bergelombang di kiri-kanannya.
// Bentuk bukit diambil dari TrackShape; warna mengikuti peta yang dipilih (MapTheme).
// Jalan & tanah dibuat ulang sedikit demi sedikit di depan mobil, jadi tidak pernah habis.
public class RaceTrack : MonoBehaviour
{
    [Header("Warna (untuk peta KOTA)")]
    public Color asphaltColor = new Color(0.18f, 0.18f, 0.21f);
    public Color lineColor = Color.white;
    public Color kerbColor = new Color(0.85f, 0.1f, 0.1f);
    public Color grassColor = new Color(0.3f, 0.6f, 0.25f);

    [Header("(Tidak dipakai lagi: jalan sekarang dibuat lewat kode)")]
    public bool invertScroll = false;

    [Header("Untuk menu (biarkan kosong di scene game)")]
    public Transform target;        // kosong = otomatis mencari PlayerCar
    public bool followTarget;       // jalan ikut bergerak bersama target

    private const float RoadWidth = 10f;
    private const float TileLength = 16f;   // panjang satu pola garis (satuan dunia)

    // Jangkauan jalan & tanah yang dibuat: sedikit di belakang mobil sampai jauh di depan
    private const float Behind = 80f, Ahead = 600f, RebuildStep = 20f;
    private const float RoadStep = 2f, GroundStep = 4f;
    private static readonly float[] GroundColumns =
        { 0f, 6.2f, 8f, 10f, 13f, 17f, 22f, 30f, 40f, 55f, 75f, 100f, 140f, 190f, 250f, 330f };

    private Material roadMaterial;
    private Material groundMaterial;
    private Transform player;
    private Mesh roadMesh, groundMesh;
    private float builtFrom = float.MinValue;
    private int builtTheme = -1;

    void Start()
    {
        if (target != null) player = target;
        else
        {
            var car = FindAnyObjectByType<PlayerCar>();
            if (car != null) player = car.transform;
        }

        // Pola jalan dibuat lewat kode, jadi tidak perlu file gambar
        roadMaterial = MaterialHelper.Create(Color.white);
        roadMaterial.mainTextureScale = Vector2.one;
        groundMaterial = MaterialHelper.Create(grassColor);

        // Plane jalan lama disembunyikan, diganti jalan berbukit
        var oldRenderer = GetComponent<Renderer>();
        if (oldRenderer != null) oldRenderer.enabled = false;

        roadMesh = MakeMeshObject("RoadMesh", roadMaterial);
        groundMesh = MakeMeshObject("GroundMesh", groundMaterial);

        ApplyTheme();
        MapTheme.ApplyEnvironment();
        MapTheme.Changed += ApplyTheme;
    }

    void OnDestroy()
    {
        MapTheme.Changed -= ApplyTheme;
    }

    Mesh MakeMeshObject(string objName, Material mat)
    {
        var go = new GameObject(objName, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.position = Vector3.zero;   // titik-titik mesh memakai koordinat dunia
        var mesh = new Mesh { name = objName };
        mesh.MarkDynamic();
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.receiveShadows = true;
        return mesh;
    }

    // Dipanggil saat mulai dan setiap kali peta diganti di menu Pengaturan
    void ApplyTheme()
    {
        if (roadMaterial == null) return;
        int t = MapTheme.Current;

        var old = roadMaterial.mainTexture;
        roadMaterial.mainTexture = CreateTrackTexture(MapTheme.Asphalt(t, asphaltColor), MapTheme.Kerb(t, kerbColor));
        roadMaterial.color = Color.white;
        if (old != null) Destroy(old);

        Color g = MapTheme.Ground(t, grassColor);
        g.a = 1f;
        groundMaterial.color = g;

        builtFrom = float.MinValue;   // bentuk tanah pantai berbeda: bangun ulang
    }

    void LateUpdate()
    {
        if (followTarget && player != null)
        {
            Vector3 p = transform.position;
            p.z = player.position.z + 200f;
            transform.position = p;
        }

        // Bangun ulang jalan & tanah setiap mobil maju 20 satuan
        float z = player != null ? player.position.z : transform.position.z - 200f;
        float from = Mathf.Floor((z - Behind) / RebuildStep) * RebuildStep;
        int theme = MapTheme.Current;
        if (from != builtFrom || theme != builtTheme)
        {
            builtFrom = from;
            builtTheme = theme;
            BuildRoad(from);
            BuildGround(from, theme);
        }
    }

    // ---------- Jalan berbukit ----------

    void BuildRoad(float from)
    {
        float cx = transform.position.x;
        int rows = Mathf.CeilToInt((Behind + Ahead) / RoadStep) + 1;
        var verts = new Vector3[rows * 2];
        var uvs = new Vector2[rows * 2];
        var tris = new int[(rows - 1) * 6];

        for (int i = 0; i < rows; i++)
        {
            float z = from + i * RoadStep;
            float y = TrackShape.Height(z);
            verts[i * 2] = new Vector3(cx - RoadWidth / 2f, y, z);
            verts[i * 2 + 1] = new Vector3(cx + RoadWidth / 2f, y, z);
            float v = z / TileLength;   // pola menempel di dunia, jadi garis terlihat "berlari" saat mobil melaju
            uvs[i * 2] = new Vector2(0f, v);
            uvs[i * 2 + 1] = new Vector2(1f, v);
        }
        for (int i = 0, t = 0; i < rows - 1; i++)
        {
            int a = i * 2;
            tris[t++] = a; tris[t++] = a + 2; tris[t++] = a + 1;
            tris[t++] = a + 1; tris[t++] = a + 2; tris[t++] = a + 3;
        }

        roadMesh.Clear();
        roadMesh.vertices = verts;
        roadMesh.uv = uvs;
        roadMesh.triangles = tris;
        roadMesh.RecalculateNormals();
        roadMesh.RecalculateBounds();
    }

    // ---------- Tanah bergelombang ----------

    void BuildGround(float from, int theme)
    {
        float cx = transform.position.x;

        // Kolom: dari kiri jauh ke kanan jauh (rapat di dekat jalan, renggang di kejauhan)
        var xs = new List<float>();
        for (int i = GroundColumns.Length - 1; i > 0; i--) xs.Add(-GroundColumns[i]);
        foreach (float c in GroundColumns) xs.Add(c);
        int cols = xs.Count;
        int rows = Mathf.CeilToInt((Behind + Ahead) / GroundStep) + 1;

        var verts = new Vector3[rows * cols];
        for (int r = 0; r < rows; r++)
        {
            float z = from + r * GroundStep;
            for (int c = 0; c < cols; c++)
            {
                float dx = xs[c];
                verts[r * cols + c] = new Vector3(cx + dx, TrackShape.Ground(dx, z, theme), z);
            }
        }

        var tris = new int[(rows - 1) * (cols - 1) * 6];
        for (int r = 0, t = 0; r < rows - 1; r++)
        {
            for (int c = 0; c < cols - 1; c++)
            {
                int a = r * cols + c, b = a + cols;
                tris[t++] = a; tris[t++] = b; tris[t++] = a + 1;
                tris[t++] = a + 1; tris[t++] = b; tris[t++] = b + 1;
            }
        }

        groundMesh.Clear();
        groundMesh.vertices = verts;
        groundMesh.triangles = tris;
        groundMesh.RecalculateNormals();
        groundMesh.RecalculateBounds();
    }

    Texture2D CreateTrackTexture(Color asphalt, Color kerb)
    {
        const int width = 256, height = 256;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;   // tetap tajam saat dilihat dari sudut rendah

        for (int py = 0; py < height; py++)
        {
            float y = (py + 0.5f) / height * TileLength;        // posisi sepanjang jalan
            for (int px = 0; px < width; px++)
            {
                float x = (px + 0.5f) / width * RoadWidth - 5f; // posisi melintang (-5 .. 5)
                float ax = Mathf.Abs(x);
                Color c = asphalt * Random.Range(0.92f, 1.06f);   // aspal sedikit bertekstur
                c.a = 1f;

                if (ax > 4.3f)
                {
                    // Kerb berwarna-putih bergantian tiap 2 satuan
                    c = Mathf.FloorToInt(y / 2f) % 2 == 0 ? kerb : lineColor;
                }
                else if (ax > 4.05f && ax < 4.2f)
                {
                    c = lineColor;   // garis tepi jalan
                }
                else if (ax > 1.4f && ax < 1.6f && (y % 8f) < 4f)
                {
                    c = lineColor;   // garis jalur putus-putus
                }

                tex.SetPixel(px, py, c);
            }
        }
        tex.Apply();
        return tex;
    }
}
