using UnityEngine;

// BENTUK JALAN & TANAH (bukit naik-turun ala Beach Buggy). Tidak perlu dipasang ke objek mana pun.
// Semua script memakai rumus yang sama: jalan (RaceTrack), mobil (PlayerCar), kubus jawaban (GameManager),
// pemandangan (Scenery), kamera (CameraFollow), dan latar menu (MenuBackground).
// Tinggi jalan hanya bergantung pada posisi maju (z), jadi di mode DUO kedua pemain mendapat bukit yang sama.
public static class TrackShape
{
    // Bukit: dua gelombang yang dijumlahkan agar tidak terlihat berulang
    const float HillA = 5.5f, HillLength = 230f;     // bukit besar (diuji: kubus jawaban tidak pernah tertutup puncak)
    const float BumpA = 1.6f, BumpLength = 97f;      // bukit kecil
    const float FlatStart = 30f, RampEnd = 110f;     // area start tetap rata, lalu bukit muncul perlahan

    public const float SeaLevel = -0.7f;             // permukaan laut di peta Pantai
    public const float RoadHalfWidth = 5f;

    // Tinggi jalan di posisi z (selalu >= 0, jadi jalan tidak pernah di bawah laut)
    public static float Height(float z)
    {
        float ramp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FlatStart, RampEnd, z));
        if (ramp <= 0f) return 0f;
        float big = HillA * (1f - Mathf.Cos(2f * Mathf.PI * z / HillLength)) * 0.5f;
        float small = BumpA * (1f - Mathf.Cos(2f * Mathf.PI * z / BumpLength + 1.3f)) * 0.5f;
        return (big + small) * ramp;
    }

    // Kemiringan jalan (naik = positif)
    public static float Slope(float z)
    {
        const float d = 0.5f;
        return (Height(z + d) - Height(z - d)) / (2f * d);
    }

    // Sudut tanjakan dalam derajat, untuk memiringkan mobil & kamera
    public static float PitchDegrees(float z)
    {
        return -Mathf.Atan(Slope(z)) * Mathf.Rad2Deg;
    }

    // Tinggi tanah di samping jalan. dx = jarak dari tengah jalan (negatif = kiri, positif = kanan).
    public static float Ground(float dx, float z, int theme)
    {
        float road = Height(z) - 0.04f;              // bahu jalan sedikit di bawah aspal
        float a = Mathf.Abs(dx);
        if (a < RoadHalfWidth + 1.2f) return road;

        // Makin jauh dari jalan, gelombang makin tinggi
        float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RoadHalfWidth + 1.2f, 45f, a));
        float n1 = Mathf.PerlinNoise(dx * 0.025f + 100f, z * 0.025f) - 0.35f;
        float n2 = Mathf.PerlinNoise(dx * 0.08f + 50f, z * 0.08f) - 0.5f;

        float amp;
        switch (theme)
        {
            case MapTheme.Mountain: amp = 4f + a * 0.12f; break;   // perbukitan tinggi
            case MapTheme.Desert: amp = 3f + a * 0.05f; break;     // gundukan pasir
            case MapTheme.Beach: amp = 1.2f; break;                // pantai landai
            default: amp = 2f + a * 0.04f; break;                  // kota: bukit rendah
        }

        float g = road + t * (n1 * amp + n2 * amp * 0.15f);

        // Pantai: sisi kanan menurun ke bawah permukaan laut
        if (theme == MapTheme.Beach && dx > 0f)
        {
            float s = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(12f, 30f, dx));
            g = Mathf.Lerp(g, SeaLevel - 0.8f, s);
        }
        return g;
    }
}
