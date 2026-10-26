using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// GRAFIK HD. Tidak perlu dipasang ke objek mana pun: berjalan otomatis setiap scene dibuka.
// - Anti-aliasing MSAA 4x (tepi objek tidak bergerigi) dan bayangan lebih tajam & lembut
// - Post-processing: warna sinematik (ACES), bloom (cahaya berpendar), vignette, saturasi & kontras
// - Kabut (fog) dan suasana warna yang berbeda di tiap peta (Kota, Pantai, Pegunungan, Gurun)
// Kamera preview mobil di garasi sengaja tidak diberi efek, agar latarnya tetap transparan.
public static class VisualQuality
{
    private static VolumeProfile profile;
    private static Bloom bloom;
    private static ColorAdjustments colorAdjust;
    private static WhiteBalance whiteBalance;
    private static Vignette vignette;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        MapTheme.Changed -= ApplyTheme;
        MapTheme.Changed += ApplyTheme;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyPipeline();
        CreateVolume();
        ApplyCameras();
        ApplySun();
        ApplyTheme();
    }

    // ---------- Kualitas render ----------

    static void ApplyPipeline()
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) return;
        urp.msaaSampleCount = 4;                 // tepi halus
        urp.renderScale = 1f;                    // resolusi penuh
        urp.shadowDistance = 120f;               // bayangan terlihat lebih jauh
        urp.shadowCascadeCount = 2;              // bayangan dekat lebih tajam
        urp.mainLightShadowmapResolution = 2048; // bayangan tidak kotak-kotak
    }

    static void ApplyCameras()
    {
        foreach (var cam in Camera.allCameras)
        {
            if (cam.targetTexture != null) continue;   // kamera preview garasi: biarkan
            cam.allowMSAA = true;
            cam.allowHDR = true;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
        }
    }

    static void ApplySun()
    {
        var sun = Object.FindAnyObjectByType<Light>();
        if (sun == null || sun.type != LightType.Directional) return;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.8f;
    }

    // ---------- Post-processing ----------

    static void CreateVolume()
    {
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.ACES);   // warna sinematik seperti film

            bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.65f);

            colorAdjust = profile.Add<ColorAdjustments>(true);
            colorAdjust.postExposure.Override(0.3f);        // ACES sedikit menggelapkan, jadi dicerahkan
            colorAdjust.contrast.Override(12f);
            colorAdjust.saturation.Override(14f);

            whiteBalance = profile.Add<WhiteBalance>(true);
            whiteBalance.temperature.Override(0f);

            vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);
        }

        // Volume baru di setiap scene, prioritas tinggi agar mengalahkan Global Volume lama
        var go = new GameObject("HD_Graphics_Volume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.sharedProfile = profile;
    }

    // ---------- Suasana per peta ----------

    static void ApplyTheme()
    {
        int t = MapTheme.Current;

        Color fog;
        float fogStart, fogEnd, temperature, saturation;
        switch (t)
        {
            case MapTheme.Beach:
                fog = new Color(0.72f, 0.86f, 0.93f); fogStart = 80f; fogEnd = 420f; temperature = -6f; saturation = 20f; break;
            case MapTheme.Mountain:
                fog = new Color(0.66f, 0.75f, 0.86f); fogStart = 55f; fogEnd = 460f; temperature = -12f; saturation = 6f; break;
            case MapTheme.Desert:
                fog = new Color(0.94f, 0.8f, 0.6f); fogStart = 45f; fogEnd = 330f; temperature = 18f; saturation = 10f; break;
            default:
                fog = new Color(0.7f, 0.79f, 0.9f); fogStart = 70f; fogEnd = 360f; temperature = 0f; saturation = 14f; break;
        }

        // Kabut mulai cukup jauh agar angka di kubus jawaban tetap jelas terbaca
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fog;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;

        if (whiteBalance != null) whiteBalance.temperature.Override(temperature);
        if (colorAdjust != null) colorAdjust.saturation.Override(saturation);
    }
}
