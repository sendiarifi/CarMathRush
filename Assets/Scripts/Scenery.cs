using System.Collections.Generic;
using UnityEngine;

// Pasang ke objek kosong bernama "Scenery" di SampleScene.
// Membuat pemandangan di kiri-kanan jalan sesuai peta yang dipilih (MapTheme):
// KOTA (rumah, gedung, pohon), PANTAI (laut, nyiur, payung), PEGUNUNGAN (pinus, batu, gunung),
// GURUN PASIR (kaktus, batu, bukit pasir, tebing). Semua dibuat lewat kode.
public class Scenery : MonoBehaviour
{
    [Header("Opsional untuk peta KOTA: model dari Kenney (boleh kosong)")]
    public GameObject[] treePrefabs;
    public GameObject[] buildingPrefabs;

    [Header("Untuk menu (biarkan kosong di scene game)")]
    public Transform target;   // kosong = otomatis mencari PlayerCar

    [Header("Pengaturan")]
    public float spacing = 10f;          // jarak antar baris benda
    public float viewDistance = 250f;    // seberapa jauh ke depan benda dimunculkan
    [Range(0f, 1f)] public float buildingChance = 0.35f;

    private const float BigSpacing = 70f;       // jarak antar gunung / tebing di kejauhan
    private const float BigViewDistance = 450f;

    private Transform player;
    private float nextZ, nextBigZ;
    private float centerX;   // titik tengah jalan (SOLO: 0)
    private int theme;
    private readonly List<GameObject> props = new List<GameObject>();
    private readonly List<GameObject> bigProps = new List<GameObject>();
    private Transform sea;

    void Start()
    {
        if (target != null) player = target;
        else
        {
            var car = FindAnyObjectByType<PlayerCar>();
            if (car == null) { enabled = false; return; }
            player = car.transform;
        }
        var pc = player.GetComponent<PlayerCar>();
        centerX = pc != null ? pc.centerX : player.position.x;

        Rebuild();
        MapTheme.Changed += Rebuild;
    }

    void OnDestroy()
    {
        MapTheme.Changed -= Rebuild;
    }

    // Hapus semua pemandangan lalu mulai lagi dengan peta yang sedang dipilih
    void Rebuild()
    {
        if (player == null) return;
        foreach (var p in props) if (p != null) Destroy(p);
        foreach (var p in bigProps) if (p != null) Destroy(p);
        props.Clear();
        bigProps.Clear();
        if (sea != null) Destroy(sea.gameObject);
        sea = null;

        theme = MapTheme.Current;
        nextZ = player.position.z - 20f;
        nextBigZ = player.position.z - 60f;

        if (theme == MapTheme.Beach)
        {
            // Laut di sisi kanan jalan
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Sea";
            Destroy(water.GetComponent<Collider>());
            water.transform.SetParent(transform, true);
            water.transform.localScale = new Vector3(60f, 1f, 90f);   // 600 x 900 satuan
            water.GetComponent<Renderer>().sharedMaterial = Mat(0.15f, 0.55f, 0.85f);
            sea = water.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        // Munculkan benda baru di depan mobil
        while (nextZ < player.position.z + viewDistance)
        {
            SpawnAt(nextZ, -1);
            SpawnAt(nextZ, 1);
            nextZ += spacing;
        }

        // Benda besar di kejauhan (gunung, tebing)
        while (nextBigZ < player.position.z + BigViewDistance)
        {
            SpawnBig(nextBigZ, -1);
            SpawnBig(nextBigZ, 1);
            nextBigZ += BigSpacing;
        }

        if (sea != null)
            sea.position = new Vector3(centerX + 24f + 300f, TrackShape.SeaLevel, player.position.z + 200f);

        // Hapus benda yang sudah jauh di belakang agar game tetap ringan
        Cleanup(props, 30f);
        Cleanup(bigProps, 150f);
    }

    void Cleanup(List<GameObject> list, float behind)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null || list[i].transform.position.z < player.position.z - behind)
            {
                if (list[i] != null) Destroy(list[i]);
                list.RemoveAt(i);
            }
        }
    }

    // ---------- Benda di pinggir jalan ----------

    void SpawnAt(float z, int side)
    {
        if (Random.value < 0.15f) return;   // sesekali kosong agar terlihat alami

        GameObject obj;
        float x;
        float rotY = Random.Range(0f, 360f);

        switch (theme)
        {
            case MapTheme.Beach:
                if (side > 0)
                {
                    // Sisi laut: hanya pantai sempit
                    if (Random.value < 0.5f) return;
                    obj = Random.value < 0.5f ? MakePalm() : MakeUmbrella();
                    x = Random.Range(7f, 16f);
                }
                else
                {
                    float r = Random.value;
                    obj = r < 0.55f ? MakePalm() : r < 0.85f ? MakeUmbrella() : MakeHut();
                    x = Random.Range(7f, 24f);
                    if (obj.name == "Hut") { x = Random.Range(14f, 24f); rotY = 90f; }
                }
                break;

            case MapTheme.Mountain:
                obj = Random.value < 0.75f ? MakePine() : MakeRock(new Color(0.5f, 0.5f, 0.52f));
                x = Random.Range(7f, 30f);
                break;

            case MapTheme.Desert:
                float d = Random.value;
                obj = d < 0.45f ? MakeCactus() : d < 0.75f ? MakeRock(new Color(0.65f, 0.38f, 0.22f)) : MakeDune();
                x = obj.name == "Dune" ? Random.Range(20f, 40f) : Random.Range(7f, 26f);
                break;

            default:
                bool isBuilding = Random.value < buildingChance;
                obj = isBuilding ? MakeBuilding() : MakeTree();
                x = isBuilding ? Random.Range(14f, 30f) : Random.Range(7f, 22f);
                if (isBuilding) rotY = 90f * Random.Range(0, 4);
                break;
        }

        Place(obj, centerX + side * x, z + Random.Range(-3f, 3f), rotY, props);
    }

    void SpawnBig(float z, int side)
    {
        GameObject obj = null;
        float x = 0f;

        if (theme == MapTheme.Mountain)
        {
            obj = MakeMountain();
            x = Random.Range(70f, 140f);
        }
        else if (theme == MapTheme.Desert && Random.value < 0.7f)
        {
            obj = MakeMesa();
            x = Random.Range(60f, 130f);
        }
        if (obj == null) return;

        Place(obj, centerX + side * x, z + Random.Range(-15f, 15f), Random.Range(0f, 360f), bigProps);
    }

    void Place(GameObject obj, float x, float z, float rotY, List<GameObject> list)
    {
        // Berdiri di atas tanah yang bergelombang. Benda besar sedikit dibenamkan agar tidak melayang di lereng.
        bool big = obj.name == "Building" || obj.name == "Mountain" || obj.name == "Mesa" || obj.name == "Hut" || obj.name == "Dune";
        float y = TrackShape.Ground(x - centerX, z, theme) - (big ? 0.8f : 0.1f);
        obj.transform.position = new Vector3(x, y, z);
        obj.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        obj.transform.SetParent(transform, true);
        list.Add(obj);
    }

    // ---------- KOTA ----------

    GameObject MakeTree()
    {
        if (treePrefabs != null && treePrefabs.Length > 0)
        {
            var t = Instantiate(treePrefabs[Random.Range(0, treePrefabs.Length)]);
            t.transform.localScale *= Random.Range(1f, 1.6f);
            return t;
        }

        var tree = new GameObject("Tree");
        float trunkH = Random.Range(1f, 1.8f);
        float leafSize = Random.Range(2.2f, 3.4f);
        Part(PrimitiveType.Cylinder, tree.transform, new Vector3(0f, trunkH, 0f),
             new Vector3(0.4f, trunkH, 0.4f), Mat(0.45f, 0.3f, 0.18f));
        Part(PrimitiveType.Sphere, tree.transform, new Vector3(0f, trunkH * 2f + leafSize * 0.3f, 0f),
             Vector3.one * leafSize, LeafMat());
        return tree;
    }

    GameObject MakeBuilding()
    {
        if (buildingPrefabs != null && buildingPrefabs.Length > 0)
        {
            var b = Instantiate(buildingPrefabs[Random.Range(0, buildingPrefabs.Length)]);
            b.transform.localScale *= Random.Range(1.5f, 2.5f);
            return b;
        }

        Color[] walls =
        {
            new Color(0.95f, 0.85f, 0.7f), new Color(0.75f, 0.85f, 0.95f), new Color(0.95f, 0.75f, 0.7f),
            new Color(0.8f, 0.8f, 0.82f),  new Color(0.85f, 0.95f, 0.8f),  new Color(0.95f, 0.9f, 0.6f),
        };
        Color wall = walls[Random.Range(0, walls.Length)];
        Material wallMat = MaterialHelper.Shared(wall);
        Material roofMat = MaterialHelper.Shared(wall * 0.55f);

        var building = new GameObject("Building");
        if (Random.value < 0.5f)
        {
            // Rumah: badan pendek + atap
            float w = Random.Range(4f, 6f), h = Random.Range(2.5f, 3.5f), d = Random.Range(4f, 6f);
            Part(PrimitiveType.Cube, building.transform, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), wallMat);
            Part(PrimitiveType.Cube, building.transform, new Vector3(0f, h + 0.35f, 0f),
                 new Vector3(w + 0.6f, 0.7f, d + 0.6f), roofMat);
        }
        else
        {
            // Gedung tinggi
            float w = Random.Range(5f, 8f), h = Random.Range(8f, 20f), d = Random.Range(5f, 8f);
            Part(PrimitiveType.Cube, building.transform, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), wallMat);
            Part(PrimitiveType.Cube, building.transform, new Vector3(0f, h + 0.2f, 0f),
                 new Vector3(w * 0.9f, 0.4f, d * 0.9f), roofMat);
        }
        return building;
    }

    Material LeafMat()
    {
        Color[] leaves = { new Color(0.2f, 0.55f, 0.2f), new Color(0.3f, 0.65f, 0.25f), new Color(0.15f, 0.45f, 0.25f) };
        return MaterialHelper.Shared(leaves[Random.Range(0, leaves.Length)]);
    }

    // ---------- PANTAI ----------

    GameObject MakePalm()
    {
        var palm = new GameObject("Palm");
        Material trunk = Mat(0.55f, 0.4f, 0.25f);
        float lean = Random.Range(0.15f, 0.35f) * (Random.value < 0.5f ? -1f : 1f);
        int segments = Random.Range(4, 6);
        Vector3 top = Vector3.zero;

        // Batang melengkung dari beberapa potongan silinder
        for (int i = 0; i < segments; i++)
        {
            float y = i * 1.2f + 0.6f;
            float x = lean * i * i * 0.25f;
            Part(PrimitiveType.Cylinder, palm.transform, new Vector3(x, y, 0f),
                 new Vector3(0.45f - i * 0.04f, 0.62f, 0.45f - i * 0.04f), trunk);
            top = new Vector3(x, y + 0.6f, 0f);
        }

        // Daun: bola gepeng memanjang yang menyebar ke segala arah
        Material leaf = Mat(0.2f, 0.6f, 0.2f);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f + Random.Range(-10f, 10f);
            var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            var p = Part(PrimitiveType.Sphere, palm.transform, top + dir * 1.3f + Vector3.down * 0.3f,
                         new Vector3(0.8f, 0.15f, 2.8f), leaf);
            p.transform.localRotation = Quaternion.Euler(20f, angle, 0f);
        }
        // Kelapa
        Part(PrimitiveType.Sphere, palm.transform, top + new Vector3(0.25f, -0.3f, 0f), Vector3.one * 0.4f, Mat(0.35f, 0.25f, 0.12f));
        return palm;
    }

    GameObject MakeUmbrella()
    {
        var umbrella = new GameObject("Umbrella");
        Part(PrimitiveType.Cylinder, umbrella.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.08f, 1.2f, 0.08f), Mat(0.95f, 0.95f, 0.95f));

        Color[] colors = { new Color(0.95f, 0.25f, 0.25f), new Color(0.2f, 0.5f, 0.95f), new Color(1f, 0.8f, 0.15f), new Color(0.2f, 0.75f, 0.45f) };
        Material cloth = MaterialHelper.Shared(colors[Random.Range(0, colors.Length)]);
        Cone(umbrella.transform, new Vector3(0f, 2.1f, 0f), new Vector3(3.2f, 0.7f, 3.2f), cloth);

        // Handuk di bawah payung
        Part(PrimitiveType.Cube, umbrella.transform, new Vector3(0.9f, 0.02f, 0f), new Vector3(1f, 0.04f, 2f), cloth);
        return umbrella;
    }

    GameObject MakeHut()
    {
        var hut = new GameObject("Hut");
        Material wood = Mat(0.6f, 0.45f, 0.3f);
        Part(PrimitiveType.Cube, hut.transform, new Vector3(0f, 1.25f, 0f), new Vector3(4f, 2.5f, 4f), wood);
        Cone(hut.transform, new Vector3(0f, 2.5f, 0f), new Vector3(6.5f, 2.2f, 6.5f), Mat(0.85f, 0.7f, 0.4f));
        return hut;
    }

    // ---------- PEGUNUNGAN ----------

    GameObject MakePine()
    {
        var pine = new GameObject("Pine");
        float s = Random.Range(0.8f, 1.5f);
        Part(PrimitiveType.Cylinder, pine.transform, new Vector3(0f, 0.6f * s, 0f), new Vector3(0.35f, 0.6f, 0.35f) * s, Mat(0.4f, 0.27f, 0.15f));

        Color[] greens = { new Color(0.1f, 0.38f, 0.2f), new Color(0.13f, 0.45f, 0.22f), new Color(0.08f, 0.32f, 0.18f) };
        Material needles = MaterialHelper.Shared(greens[Random.Range(0, greens.Length)]);
        Cone(pine.transform, new Vector3(0f, 1.0f * s, 0f), new Vector3(3.2f, 2.6f, 3.2f) * s, needles);
        Cone(pine.transform, new Vector3(0f, 2.3f * s, 0f), new Vector3(2.5f, 2.3f, 2.5f) * s, needles);
        Cone(pine.transform, new Vector3(0f, 3.5f * s, 0f), new Vector3(1.7f, 2f, 1.7f) * s, needles);
        return pine;
    }

    GameObject MakeRock(Color color)
    {
        var rock = new GameObject("Rock");
        float s = Random.Range(0.8f, 2.2f);
        var p = Part(PrimitiveType.Sphere, rock.transform, new Vector3(0f, 0.25f * s, 0f),
                     new Vector3(Random.Range(1.2f, 2f), 0.9f, Random.Range(1f, 1.6f)) * s, MaterialHelper.Shared(color));
        p.transform.localRotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), 0f);
        return rock;
    }

    GameObject MakeMountain()
    {
        var mountain = new GameObject("Mountain");
        float h = Random.Range(40f, 85f);
        float r = Random.Range(45f, 80f);
        Color[] stones = { new Color(0.45f, 0.45f, 0.5f), new Color(0.4f, 0.42f, 0.38f), new Color(0.5f, 0.47f, 0.45f) };
        Cone(mountain.transform, Vector3.zero, new Vector3(r * 2f, h, r * 2f), MaterialHelper.Shared(stones[Random.Range(0, stones.Length)]));

        // Salju di puncak: kerucut kecil yang puncaknya sama dengan gunung
        const float snow = 0.3f;
        Cone(mountain.transform, new Vector3(0f, h * (1f - snow), 0f),
             new Vector3(r * 2f * snow * 1.03f, h * snow * 1.01f, r * 2f * snow * 1.03f), Mat(0.95f, 0.97f, 1f));
        return mountain;
    }

    // ---------- GURUN PASIR ----------

    GameObject MakeCactus()
    {
        var cactus = new GameObject("Cactus");
        Material green = Mat(0.25f, 0.55f, 0.25f);
        float h = Random.Range(2.2f, 4f);
        Part(PrimitiveType.Capsule, cactus.transform, new Vector3(0f, h / 2f, 0f), new Vector3(0.7f, h / 2f, 0.7f), green);

        // Satu atau dua lengan
        int arms = Random.Range(1, 3);
        for (int i = 0; i < arms; i++)
        {
            float side = i == 0 ? 1f : -1f;
            float y = h * Random.Range(0.4f, 0.65f);
            Part(PrimitiveType.Capsule, cactus.transform, new Vector3(side * 0.6f, y, 0f), new Vector3(0.4f, 0.4f, 0.4f), green)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Part(PrimitiveType.Capsule, cactus.transform, new Vector3(side * 0.95f, y + 0.6f, 0f), new Vector3(0.4f, 0.6f, 0.4f), green);
        }
        return cactus;
    }

    GameObject MakeDune()
    {
        var dune = new GameObject("Dune");
        Part(PrimitiveType.Sphere, dune.transform, Vector3.zero,
             new Vector3(Random.Range(14f, 24f), Random.Range(3f, 6f), Random.Range(10f, 18f)), Mat(0.9f, 0.72f, 0.46f));
        return dune;
    }

    GameObject MakeMesa()
    {
        var mesa = new GameObject("Mesa");
        float w = Random.Range(20f, 40f), h = Random.Range(12f, 26f), d = Random.Range(18f, 35f);
        Part(PrimitiveType.Cube, mesa.transform, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), Mat(0.72f, 0.36f, 0.22f));
        Part(PrimitiveType.Cube, mesa.transform, new Vector3(0f, h * 0.35f, 0f), new Vector3(w * 1.02f, h * 0.12f, d * 1.02f), Mat(0.82f, 0.5f, 0.3f));
        Part(PrimitiveType.Cube, mesa.transform, new Vector3(0f, h + 0.5f, 0f), new Vector3(w * 0.95f, 1f, d * 0.95f), Mat(0.62f, 0.3f, 0.18f));
        return mesa;
    }

    // ---------- Pembuat bagian ----------

    GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var p = GameObject.CreatePrimitive(type);
        Destroy(p.GetComponent<Collider>());   // dekorasi tidak perlu tabrakan
        p.transform.SetParent(parent, false);
        p.transform.localPosition = pos;
        p.transform.localScale = scale;
        p.GetComponent<Renderer>().sharedMaterial = mat;
        return p;
    }

    GameObject Cone(Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var c = new GameObject("Cone", typeof(MeshFilter), typeof(MeshRenderer));
        c.transform.SetParent(parent, false);
        c.transform.localPosition = pos;
        c.transform.localScale = scale;
        c.GetComponent<MeshFilter>().sharedMesh = MapTheme.Cone;
        c.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return c;
    }

    Material Mat(float r, float g, float b)
    {
        return MaterialHelper.Shared(new Color(r, g, b));
    }
}
