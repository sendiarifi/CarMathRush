using UnityEngine;

// Daftar peta (tema) dan warnanya. Tidak perlu dipasang ke objek mana pun.
// Dipakai oleh RaceTrack (jalan & tanah), Scenery (pohon, gunung, dll.), dan SettingsMenu.
public static class MapTheme
{
    public const int City = 0, Beach = 1, Mountain = 2, Desert = 3;

    public static readonly string[] Names = { "KOTA", "PANTAI", "PEGUNUNGAN", "GURUN PASIR" };
    public static readonly string[] Descriptions =
    {
        "Rumah, gedung, dan pepohonan",
        "Pasir putih, laut, nyiur, dan payung",
        "Hutan pinus dan gunung bersalju",
        "Kaktus, bukit pasir, dan tebing merah",
    };

    public static int Count => Names.Length;

    // Dipanggil saat peta diganti, agar latar menu langsung berubah
    public static event System.Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnStart() { Changed = null; sun = null; sky = null; }

    public static int Current
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("MapTheme", 0), 0, Names.Length - 1);
        set
        {
            PlayerPrefs.SetInt("MapTheme", Mathf.Clamp(value, 0, Names.Length - 1));
            PlayerPrefs.Save();
            ApplyEnvironment();
            Changed?.Invoke();
        }
    }

    // ---------- Warna per peta ----------

    public static Color Ground(int t, Color cityGrass)
    {
        switch (t)
        {
            case Beach: return new Color(0.93f, 0.85f, 0.64f);
            case Mountain: return new Color(0.22f, 0.45f, 0.2f);
            case Desert: return new Color(0.87f, 0.67f, 0.42f);
            default: return cityGrass;
        }
    }

    public static Color Asphalt(int t, Color cityAsphalt)
    {
        switch (t)
        {
            case Desert: return new Color(0.32f, 0.28f, 0.26f);   // aspal berdebu
            case Mountain: return new Color(0.2f, 0.21f, 0.23f);
            default: return cityAsphalt;
        }
    }

    public static Color Kerb(int t, Color cityKerb)
    {
        switch (t)
        {
            case Beach: return new Color(0.1f, 0.45f, 0.9f);     // biru-putih
            case Desert: return new Color(0.95f, 0.45f, 0.1f);   // oranye-putih
            default: return cityKerb;
        }
    }

    static Color SunTint(int t)
    {
        switch (t)
        {
            case Beach: return new Color(1f, 1f, 0.95f);
            case Mountain: return new Color(0.85f, 0.92f, 1f);   // sejuk
            case Desert: return new Color(1f, 0.85f, 0.65f);     // panas
            default: return Color.white;
        }
    }

    static Color SkyTint(int t)
    {
        switch (t)
        {
            case Beach: return new Color(0.35f, 0.6f, 0.8f);
            case Mountain: return new Color(0.45f, 0.55f, 0.7f);
            case Desert: return new Color(0.65f, 0.55f, 0.42f);
            default: return new Color(0.5f, 0.5f, 0.5f);         // bawaan Unity
        }
    }

    // ---------- Cahaya & langit ----------

    private static Light sun;
    private static Color sunBase;
    private static Material sky;

    public static void ApplyEnvironment()
    {
        int t = Current;

        // Warna cahaya matahari (dikalikan warna asli Directional Light)
        if (sun == null)
        {
            var light = Object.FindAnyObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                sun = light;
                sunBase = light.color;
            }
        }
        if (sun != null) sun.color = sunBase * SunTint(t);

        // Warna langit (hanya kalau scene memakai skybox Procedural bawaan Unity).
        // Material langit disalin dulu agar file aslinya di project tidak ikut berubah.
        var current = RenderSettings.skybox;
        if (current != null && current != sky && current.HasProperty("_SkyTint"))
        {
            sky = new Material(current) { name = current.name + " (Tema)" };
            RenderSettings.skybox = sky;
        }
        if (sky != null && RenderSettings.skybox == sky)
        {
            sky.SetColor("_SkyTint", SkyTint(t));
            if (sky.HasProperty("_AtmosphereThickness"))
                sky.SetFloat("_AtmosphereThickness", t == Desert ? 1.4f : 1f);
            DynamicGI.UpdateEnvironment();   // cahaya sekitar ikut menyesuaikan langit
        }
    }

    // ---------- Bentuk kerucut (untuk gunung & pohon pinus) ----------

    private static Mesh cone;

    // Kerucut: alas di y = 0 (jari-jari 0.5), puncak di y = 1. Sisi rata agar terlihat low-poly.
    public static Mesh Cone
    {
        get
        {
            if (cone != null) return cone;
            const int sides = 10;
            var verts = new Vector3[sides * 6];
            var tris = new int[sides * 6];
            Vector3 apex = new Vector3(0f, 1f, 0f), center = Vector3.zero;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                int v = i * 6;
                // Sisi miring
                verts[v] = p0; verts[v + 1] = apex; verts[v + 2] = p1;
                // Alas
                verts[v + 3] = p0; verts[v + 4] = p1; verts[v + 5] = center;
                for (int k = 0; k < 6; k++) tris[v + k] = v + k;
            }
            cone = new Mesh { name = "Cone", vertices = verts, triangles = tris };
            cone.RecalculateNormals();
            cone.RecalculateBounds();
            return cone;
        }
    }
}
