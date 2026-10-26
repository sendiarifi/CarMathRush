using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pasang ke objek kosong bernama "MainMenu" di scene MainMenu
public class MainMenu : MonoBehaviour
{
    [Header("Referensi (pilih lewat ikon bulat)")]
    public TMP_InputField nameInput;
    public TMP_Text highScoreText;
    public TMP_Text warningText;
    public Button playButton;
    public Button quitButton;

    [Header("Toko (boleh dikosongkan dulu)")]
    public Button shopButton;
    public GameObject shopPanel;

    [Header("Mode DUO VS (boleh kosong: tombol dibuat otomatis dari tombol MAIN)")]
    public Button duoButton;

    [Header("Pengaturan")]
    public string gameSceneName = "SampleScene";

    private Button settingsButton;
    private SettingsMenu settingsMenu;
    private MenuLook look;

    void Start()
    {
        Time.timeScale = 1f;   // jaga-jaga kalau datang dari kondisi pause

        nameInput.characterLimit = 12;
        nameInput.text = PlayerPrefs.GetString("PlayerName", "");

        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        string holder = PlayerPrefs.GetString("HighScoreName", "-");
        highScoreText.text = highScore > 0
            ? $"High Score: {highScore} ({holder})"
            : "High Score: belum ada";

        warningText.text = "";

        SetupDuoButton();
        SetupSettings();

        // Tampilan baru mengikuti logo (logo, warna tombol, posisi kolom kiri)
        Transform canvasTransform = quitButton.transform.parent;
        Transform title = canvasTransform.Find("TitleText");
        look = canvasTransform.gameObject.AddComponent<MenuLook>();
        look.Build(this, duoButton, settingsButton, title != null ? title.gameObject : null);
        if (look.Logo != null) titleObject = look.Logo;   // logo ikut disembunyikan saat toko dibuka

        // Tombol dihubungkan lewat kode, tidak perlu atur OnClick di Inspector
        playButton.onClick.AddListener(PlaySolo);
        duoButton.onClick.AddListener(PlayDuo);
        quitButton.onClick.AddListener(Quit);

        if (shopPanel != null) shopPanel.SetActive(false);
        if (shopButton != null && shopPanel != null)
            shopButton.onClick.AddListener(() => shopPanel.SetActive(true));
    }

    // Tombol MAIN dibelah dua: kiri SOLO, kanan DUO VS
    void SetupDuoButton()
    {
        if (duoButton == null)
            duoButton = SplitButton(playButton, "DuoButton");

        SetLabel(playButton, "SOLO");
        SetLabel(duoButton, "DUO VS");
    }

    // Tombol pengaturan (ikon gir di pojok kanan atas, diatur tampilannya oleh MenuLook) + panelnya
    void SetupSettings()
    {
        var clone = Instantiate(quitButton.gameObject, quitButton.transform.parent);
        clone.name = "SettingsButton";
        settingsButton = clone.GetComponent<Button>();

        // Warna tema diambil dari MenuStyler (kalau ada)
        var styler = GetComponentInParent<MenuStyler>();
        if (styler == null) styler = FindAnyObjectByType<MenuStyler>();
        Color accent = styler != null ? styler.buttonColor : new Color(1f, 0.5f, 0.1f);

        settingsMenu = SettingsMenu.Create(quitButton.transform.parent, quitButton, accent);
        settingsButton.onClick.AddListener(() => settingsMenu.gameObject.SetActive(true));
    }

    // Menggandakan tombol, lalu keduanya dibuat setengah lebar dan diletakkan bersebelahan
    Button SplitButton(Button original, string cloneName)
    {
        var clone = Instantiate(original.gameObject, original.transform.parent);
        clone.name = cloneName;
        clone.transform.SetSiblingIndex(original.transform.GetSiblingIndex() + 1);

        // Kalau tombol tidak diatur oleh Layout Group, geser manual agar bersebelahan
        if (original.transform.parent.GetComponent<LayoutGroup>() == null)
        {
            var left = (RectTransform)original.transform;
            var right = (RectTransform)clone.transform;
            const float gap = 20f;
            float width = left.rect.width;
            float half = (width - gap) / 2f;
            float pivotX = left.pivot.x;
            float leftEdge = left.anchoredPosition.x - pivotX * width;

            left.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, half);
            right.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, half);
            left.anchoredPosition = new Vector2(leftEdge + pivotX * half, left.anchoredPosition.y);
            right.anchoredPosition = new Vector2(leftEdge + half + gap + pivotX * half, left.anchoredPosition.y);
        }
        return clone.GetComponent<Button>();
    }

    void SetLabel(Button button, string text)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        label.enableAutoSizing = true;   // tombol lebih sempit, teks mengecil sendiri kalau perlu
        label.fontSizeMax = label.fontSize;
        label.fontSizeMin = 18f;
        label.text = text;
    }

    private GameObject titleObject;
    private bool shopWasOpen;

    void Update()
    {
        // Saat toko ATAU pengaturan terbuka, isi menu utama disembunyikan
        bool shopOpen = (shopPanel != null && shopPanel.activeSelf)
                     || (settingsMenu != null && settingsMenu.gameObject.activeSelf);
        if (shopOpen == shopWasOpen) return;
        shopWasOpen = shopOpen;

        // Sembunyikan isi menu utama saat toko dibuka, agar tidak bertumpuk
        if (titleObject == null)
        {
            var canvas = quitButton.transform.parent;
            var title = canvas != null ? canvas.Find("TitleText") : null;
            if (title != null) titleObject = title.gameObject;
        }
        GameObject[] menuItems =
        {
            titleObject, highScoreText.gameObject, nameInput.gameObject, warningText.gameObject,
            playButton.gameObject, quitButton.gameObject, shopButton != null ? shopButton.gameObject : null,
            duoButton != null ? duoButton.gameObject : null,
            settingsButton != null ? settingsButton.gameObject : null,
        };
        foreach (var item in menuItems)
            if (item != null) item.SetActive(!shopOpen);
        if (look != null) look.SetVisible(!shopOpen);
    }

    void PlaySolo()
    {
        string playerName = nameInput.text.Trim();
        if (playerName.Length == 0)
        {
            warningText.text = "Isi nama dulu sebelum main!";
            // Kotak nama bergetar + garis tepi berkedip merah, lalu kursor langsung aktif di kotak nama
            if (nameShake != null) StopCoroutine(nameShake);
            nameShake = StartCoroutine(ShakeNameInput());
            nameInput.Select();
            nameInput.ActivateInputField();
            return;
        }

        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();
        GameMode.Duo = false;
        SceneManager.LoadScene(gameSceneName);
    }

    private Coroutine nameShake;

    IEnumerator ShakeNameInput()
    {
        var rect = (RectTransform)nameInput.transform;
        Vector2 home = rect.anchoredPosition;
        var outline = nameInput.GetComponent<Outline>();
        Color normal = outline != null ? outline.effectColor : Color.black;
        var warn = warningText.rectTransform;

        for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / 0.6f;
            rect.anchoredPosition = home + new Vector2(Mathf.Sin(t * 60f) * 14f * k, 0f);
            if (outline != null) outline.effectColor = Mathf.Repeat(t, 0.2f) < 0.1f ? new Color(1f, 0.2f, 0.2f) : normal;
            float s = 1f + 0.25f * k;   // tulisan peringatan "meletup" lalu kembali normal
            warn.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rect.anchoredPosition = home;
        if (outline != null) outline.effectColor = new Color(1f, 0.25f, 0.25f);   // tetap merah sampai nama diisi
        warn.localScale = Vector3.one;

        while (nameInput.text.Trim().Length == 0) yield return null;
        if (outline != null) outline.effectColor = normal;
        warningText.text = "";
        nameShake = null;
    }

    void PlayDuo()
    {
        // Nama boleh kosong di mode DUO (Pemain 1 memakai nama ini, Pemain 2 = "PEMAIN 2")
        string playerName = nameInput.text.Trim();
        if (playerName.Length > 0)
        {
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
        }
        GameMode.Duo = true;
        SceneManager.LoadScene(gameSceneName);
    }

    void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // agar berfungsi juga di editor
#endif
    }
}
