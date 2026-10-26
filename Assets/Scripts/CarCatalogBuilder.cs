#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Menu editor: "Car Math Rush > Buat Katalog Mobil Otomatis".
// Mencari semua model mobil Kenney di project, lalu membuat Assets/Resources/CarCatalog.asset
// lengkap dengan nama Indonesia, harga, dan kelangkaan. Hanya berjalan di editor (tidak ikut ke .exe).
public static class CarCatalogBuilder
{
    // Nama file model Kenney -> nama tampil, harga, kelangkaan.
    // 6 mobil pertama HARUS tetap di urutan ini (sama dengan versi lama), agar data "sudah dibeli" tidak bergeser.
    static readonly (string file, string name, int price, int rarity)[] Known =
    {
        ("sedan", "Sedan", 0, 0),
        ("taxi", "Taxi", 10, 0),
        ("police", "Polisi", 20, 1),
        ("sedan-sports", "Sedan Sport", 35, 1),
        ("race", "Balap", 50, 2),
        ("race-future", "Balap Futuristik", 80, 3),
        // --- mobil baru ---
        ("van", "Van", 15, 0),
        ("delivery", "Mobil Boks", 25, 0),
        ("hatchback-sports", "Hatchback Sport", 40, 1),
        ("suv", "SUV", 45, 1),
        ("truck", "Truk", 55, 1),
        ("ambulance", "Ambulans", 60, 2),
        ("garbage-truck", "Truk Sampah", 65, 2),
        ("suv-luxury", "SUV Mewah", 75, 2),
        ("delivery-flat", "Pikap Boks", 30, 0),
        ("truck-flat", "Truk Bak", 50, 1),
        ("tractor", "Traktor", 90, 3),
        ("tractor-police", "Traktor Polisi", 110, 3),
        ("tractor-shovel", "Traktor Sekop", 120, 3),
        ("firetruck", "Pemadam Kebakaran", 150, 3),
        ("kart-oobi", "Gokart Oobi", 30, 1),
        ("kart-oodi", "Gokart Oodi", 35, 1),
        ("kart-ooli", "Gokart Ooli", 40, 1),
        ("kart-oopi", "Gokart Oopi", 45, 2),
        ("kart-oozi", "Gokart Oozi", 55, 2),
    };

    // Bagian model yang bukan mobil utuh
    static readonly string[] Skip = { "wheel", "debris", "tire", "cone", "box" };   // roda, pecahan, kerucut, kardus

    [MenuItem("Car Math Rush/Buat Katalog Mobil Otomatis")]
    public static void Build()
    {
        // Semua model 3D di project, dikelompokkan berdasarkan nama file (huruf kecil)
        var models = new Dictionary<string, string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Model"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.ToLower().EndsWith(".fbx")) continue;
            string key = Path.GetFileNameWithoutExtension(path).ToLower();
            if (!models.ContainsKey(key)) models[key] = path;
        }

        if (!models.ContainsKey("sedan"))
        {
            EditorUtility.DisplayDialog("Katalog Mobil",
                "Model 'sedan.fbx' dari Kenney Car Kit tidak ditemukan di project.\nPastikan folder model mobil masih ada.", "OK");
            return;
        }

        string catalogPath = "Assets/Resources/CarCatalog.asset";
        if (File.Exists(catalogPath) &&
            !EditorUtility.DisplayDialog("Katalog Mobil", "CarCatalog sudah ada. Buat ulang dari awal?\n(Nama dan harga yang sudah kamu ubah akan kembali ke bawaan.)", "Buat ulang", "Batal"))
            return;

        var entries = new List<CarCatalog.Entry>();
        var used = new HashSet<string>();
        var missing = new List<string>();

        foreach (var k in Known)
        {
            if (models.TryGetValue(k.file, out string path))
            {
                entries.Add(new CarCatalog.Entry
                {
                    displayName = k.name,
                    model = AssetDatabase.LoadAssetAtPath<GameObject>(path),
                    price = k.price,
                    rarity = k.rarity,
                });
                used.Add(k.file);
            }
            else if (entries.Count < 6)
            {
                // 6 mobil lama wajib ada agar urutan tidak bergeser
                entries.Add(new CarCatalog.Entry { displayName = k.name, model = null, price = k.price, rarity = k.rarity });
                missing.Add(k.file);
            }
            else missing.Add(k.file);
        }

        // Model lain di folder yang sama dengan sedan.fbx (versi Kenney yang lebih baru mungkin punya mobil tambahan)
        string carFolder = Path.GetDirectoryName(models["sedan"]).Replace('\\', '/');
        var extras = new List<string>();
        foreach (var pair in models)
        {
            string folder = Path.GetDirectoryName(pair.Value).Replace('\\', '/');
            if (folder != carFolder || used.Contains(pair.Key)) continue;
            bool skip = false;
            foreach (var s in Skip) if (pair.Key.Contains(s)) skip = true;
            if (!skip) extras.Add(pair.Key);
        }
        extras.Sort();
        foreach (string key in extras)
        {
            entries.Add(new CarCatalog.Entry
            {
                displayName = Title(key),
                model = AssetDatabase.LoadAssetAtPath<GameObject>(models[key]),
                price = 60,
                rarity = 1,
            });
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (File.Exists(catalogPath)) AssetDatabase.DeleteAsset(catalogPath);

        var catalog = ScriptableObject.CreateInstance<CarCatalog>();
        catalog.cars = entries.ToArray();
        AssetDatabase.CreateAsset(catalog, catalogPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = catalog;   // langsung tampil di Inspector

        Debug.Log($"[Katalog Mobil] {entries.Count} mobil masuk katalog." +
                  (extras.Count > 0 ? $" Tambahan tak dikenal: {string.Join(", ", extras)}." : "") +
                  (missing.Count > 0 ? $" Tidak ditemukan (dilewati): {string.Join(", ", missing)}." : ""));
    }

    static string Title(string file)
    {
        var words = file.Replace('-', ' ').Split(' ');
        for (int i = 0; i < words.Length; i++)
            if (words[i].Length > 0) words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        return string.Join(" ", words);
    }
}
#endif
