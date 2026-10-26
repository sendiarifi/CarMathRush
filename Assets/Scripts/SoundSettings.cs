using UnityEngine;

// Pengaturan suara yang disimpan permanen (PlayerPrefs).
// Tidak perlu dipasang ke objek mana pun.
public static class SoundSettings
{
    public const float Step = 0.1f;   // tiap klik + / - mengubah 10%

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat("MusicVolume", 1f);
        set { PlayerPrefs.SetFloat("MusicVolume", Mathf.Clamp01(Round(value))); PlayerPrefs.Save(); }
    }

    public static bool MusicMuted
    {
        get => PlayerPrefs.GetInt("MusicMuted", 0) == 1;
        set { PlayerPrefs.SetInt("MusicMuted", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat("SfxVolume", 1f);
        set { PlayerPrefs.SetFloat("SfxVolume", Mathf.Clamp01(Round(value))); PlayerPrefs.Save(); }
    }

    public static bool SfxMuted
    {
        get => PlayerPrefs.GetInt("SfxMuted", 0) == 1;
        set { PlayerPrefs.SetInt("SfxMuted", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    // Pengali volume yang dipakai semua sumber suara (0 kalau dibisukan)
    public static float MusicFactor => MusicMuted ? 0f : MusicVolume;
    public static float SfxFactor => SfxMuted ? 0f : SfxVolume;

    // Hindari angka seperti 0.30000001
    static float Round(float v) => Mathf.Round(v * 10f) / 10f;
}
