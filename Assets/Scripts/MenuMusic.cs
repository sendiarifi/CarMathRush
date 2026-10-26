using System.Collections;
using UnityEngine;

// Pasang ke objek kosong "MenuMusic" di scene MainMenu.
// Musik berganti halus saat toko dibuka atau ditutup.
// Volume mengikuti menu Pengaturan (SoundSettings) dan langsung berubah saat diatur.
public class MenuMusic : MonoBehaviour
{
    public AudioClip menuMusic;
    public AudioClip shopMusic;
    public GameObject shopPanel;
    [Range(0f, 1f)] public float volume = 0.5f;
    public float fadeTime = 0.5f;

    private AudioSource source;
    private bool shopWasOpen;
    private Coroutine fadeRoutine;

    // Volume akhir = volume dasar x pengaturan musik pemain
    float TargetVolume => volume * SoundSettings.MusicFactor;

    void Start()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.volume = TargetVolume;
        source.clip = menuMusic;
        if (menuMusic != null) source.Play();
    }

    void Update()
    {
        // Ikuti pengaturan volume secara langsung (kecuali sedang berganti lagu)
        if (fadeRoutine == null) source.volume = TargetVolume;

        bool shopOpen = shopPanel != null && shopPanel.activeInHierarchy;
        if (shopOpen == shopWasOpen) return;

        shopWasOpen = shopOpen;
        AudioClip next = shopOpen && shopMusic != null ? shopMusic : menuMusic;
        if (next == source.clip) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(SwitchMusic(next));
    }

    IEnumerator SwitchMusic(AudioClip next)
    {
        // Pelankan, ganti lagu, lalu keraskan lagi
        float start = source.volume;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(start, 0f, t / fadeTime);
            yield return null;
        }

        source.clip = next;
        if (next != null) source.Play();

        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(0f, TargetVolume, t / fadeTime);
            yield return null;
        }
        source.volume = TargetVolume;
        fadeRoutine = null;
    }
}
