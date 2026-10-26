using UnityEngine;

// Data mobil dan koin yang dipakai bersama oleh menu, toko, dan game.
// Tidak perlu dipasang ke objek mana pun.
// Daftar mobil dibaca dari KATALOG (Assets/Resources/CarCatalog.asset).
// Kalau katalog belum dibuat, 6 mobil lama dipakai seperti sebelumnya.
public static class CarData
{
    // Cadangan kalau CarCatalog belum ada (urutan sama dengan Car Models lama)
    static readonly string[] DefaultNames = { "Sedan", "Taxi", "Polisi", "Sedan Sport", "Balap", "Balap Futuristik" };
    static readonly int[] DefaultPrices = { 0, 10, 20, 35, 50, 80 };
    static readonly int[] DefaultRarity = { 0, 0, 1, 1, 2, 3 };

    public static readonly string[] RarityNames = { "BIASA", "LANGKA", "EPIK", "LEGENDARIS" };
    public static readonly string[] RarityColors = { "#B0B0B0", "#4FA8FF", "#C070FF", "#FFC830" };

    private static bool loaded;
    private static string[] names;
    private static int[] prices, rarity;
    private static GameObject[] models;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() { loaded = false; }

    static void Load()
    {
        if (loaded) return;
        loaded = true;

        var catalog = Resources.Load<CarCatalog>("CarCatalog");
        if (catalog != null && catalog.cars != null && catalog.cars.Length > 0)
        {
            int n = catalog.cars.Length;
            names = new string[n]; prices = new int[n]; rarity = new int[n]; models = new GameObject[n];
            for (int i = 0; i < n; i++)
            {
                var c = catalog.cars[i];
                names[i] = string.IsNullOrEmpty(c.displayName) ? "Mobil " + (i + 1) : c.displayName;
                prices[i] = Mathf.Max(0, c.price);
                rarity[i] = Mathf.Clamp(c.rarity, 0, RarityNames.Length - 1);
                models[i] = c.model;
            }
        }
        else
        {
            names = DefaultNames; prices = DefaultPrices; rarity = DefaultRarity;
            models = new GameObject[DefaultNames.Length];   // kosong: model diambil dari Car Models lama
        }
    }

    public static string[] Names { get { Load(); return names; } }
    public static int[] Prices { get { Load(); return prices; } }
    public static int[] Rarity { get { Load(); return rarity; } }
    public static int Count => Names.Length;

    // Model 3D mobil dari katalog. Kalau kosong, pakai cadangan (array Car Models lama di Inspector).
    public static GameObject Model(int index, GameObject[] fallback = null)
    {
        Load();
        if (index >= 0 && index < models.Length && models[index] != null) return models[index];
        if (fallback != null && index >= 0 && index < fallback.Length && fallback[index] != null) return fallback[index];
        if (fallback != null && fallback.Length > 0) return fallback[0];
        return models.Length > 0 ? models[0] : null;
    }

    public static string RarityTag(int index)
    {
        int r = Rarity[index];
        return $"<color={RarityColors[r]}><b>{RarityNames[r]}</b></color>";
    }

    public static int Coins
    {
        get => PlayerPrefs.GetInt("Coins", 0);
        set => PlayerPrefs.SetInt("Coins", value);
    }

    public static int Selected
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("SelectedCar", 0), 0, Count - 1);
        set => PlayerPrefs.SetInt("SelectedCar", value);
    }

    public static bool IsOwned(int index)
    {
        return index == 0 || PlayerPrefs.GetInt("OwnedCar_" + index, 0) == 1;   // mobil pertama selalu dimiliki
    }

    public static void SetOwned(int index)
    {
        PlayerPrefs.SetInt("OwnedCar_" + index, 1);
    }

    // Menghapus semua progres: koin, mobil yang dibeli, mobil yang dipakai, high score, dan nama.
    // Pengaturan suara dan pilihan peta TIDAK ikut dihapus.
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey("Coins");
        PlayerPrefs.DeleteKey("SelectedCar");
        for (int i = 0; i < Mathf.Max(Count, 64); i++)
            PlayerPrefs.DeleteKey("OwnedCar_" + i);
        PlayerPrefs.DeleteKey("HighScore");
        PlayerPrefs.DeleteKey("HighScoreName");
        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.Save();
    }
}
