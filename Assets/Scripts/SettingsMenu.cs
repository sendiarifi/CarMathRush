using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Panel PENGATURAN di MainMenu: volume musik, volume efek suara, pilihan peta, dan reset data game.
// TIDAK perlu dipasang manual: dibuat otomatis oleh MainMenu.
// Kotak pengaturan diletakkan di kiri, sehingga latar menu (yang ikut berganti peta) terlihat di kanan.
public class SettingsMenu : MonoBehaviour
{
    private Button template;
    private static readonly Color Dim = new Color(0.25f, 0.25f, 0.3f, 0.9f);

    private Image[] musicBar, sfxBar;
    private TMP_Text musicPercent, sfxPercent, musicMuteLabel, sfxMuteLabel, mapName, mapDesc;
    private Image musicMuteImage, sfxMuteImage;
    private RectTransform root, card, dialogBox;
    private GameObject dialog;

    // Dipanggil MainMenu. template = tombol menu yang dijadikan contoh bentuk & huruf.
    public static SettingsMenu Create(Transform canvas, Button template, Color accent)
    {
        var root = new GameObject("SettingsPanel", typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var menu = root.AddComponent<SettingsMenu>();
        menu.template = template;
        menu.Build(rect);
        root.SetActive(false);
        return menu;
    }

    void Build(RectTransform rootRect)
    {
        root = rootRect;

        // Kotak utama di sebelah kiri
        card = NewImage("Card", root, new Color(UiTheme.Navy.r, UiTheme.Navy.g, UiTheme.Navy.b, 0.94f));
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = new Vector2(-400f, -10f);
        card.sizeDelta = new Vector2(840f, 900f);
        var cardImage = card.GetComponent<Image>();
        cardImage.sprite = UiTheme.Rounded;
        cardImage.type = Image.Type.Sliced;
        card.gameObject.AddComponent<Outline>().effectColor = UiTheme.NavyDeep;

        var title = NewText("Title", card, "PENGATURAN", 72, new Vector2(0f, 330f), new Vector2(800f, 100f));
        title.color = UiTheme.Yellow;
        title.fontStyle = FontStyles.Bold | FontStyles.Italic;

        // --- Musik ---
        Label(card, "MUSIK", 230f);
        BuildVolumeRow(card, 170f, out musicBar, out musicPercent, out musicMuteLabel, out musicMuteImage,
            () => { SoundSettings.MusicVolume -= SoundSettings.Step; SoundSettings.MusicMuted = false; Refresh(); },
            () => { SoundSettings.MusicVolume += SoundSettings.Step; SoundSettings.MusicMuted = false; Refresh(); },
            () => { SoundSettings.MusicMuted = !SoundSettings.MusicMuted; Refresh(); });

        // --- Efek suara ---
        Label(card, "EFEK SUARA  (mesin, benar/salah)", 90f);
        BuildVolumeRow(card, 30f, out sfxBar, out sfxPercent, out sfxMuteLabel, out sfxMuteImage,
            () => { SoundSettings.SfxVolume -= SoundSettings.Step; SoundSettings.SfxMuted = false; Refresh(); },
            () => { SoundSettings.SfxVolume += SoundSettings.Step; SoundSettings.SfxMuted = false; Refresh(); },
            () => { SoundSettings.SfxMuted = !SoundSettings.SfxMuted; Refresh(); });

        // --- Peta ---
        Label(card, "PETA", -60f);
        MakeButton(card, "<", new Vector2(-340f, -130f), new Vector2(90f, 90f), () => ChangeMap(-1));
        MakeButton(card, ">", new Vector2(340f, -130f), new Vector2(90f, 90f), () => ChangeMap(1));
        mapName = NewText("MapName", card, "", 56, new Vector2(0f, -130f), new Vector2(560f, 90f));
        mapName.color = UiTheme.Yellow;
        mapDesc = NewText("MapDesc", card, "", 28, new Vector2(0f, -200f), new Vector2(760f, 50f));
        mapDesc.color = new Color(0.8f, 0.82f, 0.9f);
        mapDesc.fontStyle = FontStyles.Normal;

        MakeButton(card, "KEMBALI", new Vector2(0f, -295f), new Vector2(360f, 86f), Close, UiTheme.Kind.Primary);

        // --- Reset data: dipisah garis, kecil, dan selalu minta konfirmasi ---
        var line = NewImage("Divider", card, new Color(1f, 1f, 1f, 0.12f));
        line.anchorMin = line.anchorMax = line.pivot = new Vector2(0.5f, 0.5f);
        line.anchoredPosition = new Vector2(0f, -360f);
        line.sizeDelta = new Vector2(740f, 3f);
        var reset = MakeButton(card, "RESET DATA GAME", new Vector2(0f, -405f), new Vector2(330f, 56f), OpenDialog);
        var resetLabel = reset.GetComponentInChildren<TMP_Text>(true);
        if (resetLabel != null) { resetLabel.color = new Color(1f, 0.55f, 0.5f); resetLabel.fontSizeMax = 26f; }

        BuildDialog();
        FitToScreen();
    }

    // Kotak konfirmasi "Yakin?" sebelum menghapus data
    void BuildDialog()
    {
        var overlay = NewImage("ResetDialog", root, new Color(0f, 0f, 0f, 0.65f));
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().raycastTarget = true;   // klik di luar kotak tidak tembus ke belakang
        dialog = overlay.gameObject;

        var box = UiTheme.Panel("Box", overlay, UiTheme.Navy, Vector2.zero, new Vector2(780f, 430f));
        box.gameObject.AddComponent<Outline>().effectColor = UiTheme.Red;
        dialogBox = box.rectTransform;

        var title = NewText("Title", dialogBox, "RESET SEMUA DATA?", 54, new Vector2(0f, 140f), new Vector2(740f, 80f));
        title.color = new Color(1f, 0.45f, 0.4f);
        var body = NewText("Body", dialogBox,
            "Koin, mobil yang sudah dibeli, high score, dan nama akan DIHAPUS.\n" +
            "Volume dan pilihan peta tidak berubah.\n<color=#FFC81F>Tindakan ini tidak bisa dibatalkan.</color>",
            28, new Vector2(0f, 30f), new Vector2(720f, 140f));
        body.fontStyle = FontStyles.Normal;
        body.color = UiTheme.TextSoft;

        MakeButton(dialogBox, "BATAL", new Vector2(-175f, -130f), new Vector2(310f, 86f), CloseDialog);
        MakeButton(dialogBox, "YA, RESET", new Vector2(175f, -130f), new Vector2(310f, 86f), ConfirmReset, UiTheme.Kind.Danger);

        dialog.SetActive(false);
    }

    void OpenDialog() { dialog.SetActive(true); }
    void CloseDialog() { dialog.SetActive(false); }

    void ConfirmReset()
    {
        CarData.ResetProgress();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // muat ulang menu agar semua tampilan kembali awal
    }

    // Kecilkan kotak kalau layar terlalu pendek
    void FitToScreen()
    {
        if (root == null) return;
        float halfHeight = root.rect.height / 2f - 10f;
        if (halfHeight <= 0f) return;
        float scale = Mathf.Min(1f, halfHeight / 465f);
        card.localScale = new Vector3(scale, scale, 1f);
        dialogBox.localScale = new Vector3(scale, scale, 1f);
    }

    void BuildVolumeRow(RectTransform card, float y, out Image[] bar, out TMP_Text percent,
                        out TMP_Text muteLabel, out Image muteImage,
                        UnityEngine.Events.UnityAction minus, UnityEngine.Events.UnityAction plus,
                        UnityEngine.Events.UnityAction mute)
    {
        MakeButton(card, "-", new Vector2(-345f, y), new Vector2(80f, 80f), minus);

        // Bar 10 kotak (tiap kotak = 10%)
        bar = new Image[10];
        for (int i = 0; i < 10; i++)
        {
            var seg = NewImage("Bar" + i, card, Dim);
            seg.anchorMin = seg.anchorMax = seg.pivot = new Vector2(0.5f, 0.5f);
            seg.anchoredPosition = new Vector2(-270f + i * 30f, y);
            seg.sizeDelta = new Vector2(24f, 22f + i * 4f);   // makin ke kanan makin tinggi
            bar[i] = seg.GetComponent<Image>();
        }

        percent = NewText("Percent", card, "", 34, new Vector2(85f, y), new Vector2(110f, 60f));
        MakeButton(card, "+", new Vector2(185f, y), new Vector2(80f, 80f), plus);

        var muteButton = MakeButton(card, "ON", new Vector2(320f, y), new Vector2(150f, 80f), mute, UiTheme.Kind.Secondary);
        muteLabel = muteButton.GetComponentInChildren<TMP_Text>(true);
        muteImage = muteButton.GetComponent<Image>();
    }

    void OnEnable()
    {
        if (dialog != null) dialog.SetActive(false);
        Refresh();
    }

    void Update()
    {
        FitToScreen();

        // Esc = tutup kotak konfirmasi dulu, lalu kembali
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (dialog != null && dialog.activeSelf) CloseDialog();
            else Close();
        }
    }

    void Close()
    {
        gameObject.SetActive(false);
    }

    void ChangeMap(int direction)
    {
        int count = MapTheme.Count;
        MapTheme.Current = (MapTheme.Current + direction + count) % count;   // latar menu langsung berganti
        Refresh();
    }

    void Refresh()
    {
        if (musicBar == null) return;
        ShowVolume(musicBar, musicPercent, musicMuteLabel, musicMuteImage, SoundSettings.MusicVolume, SoundSettings.MusicMuted);
        ShowVolume(sfxBar, sfxPercent, sfxMuteLabel, sfxMuteImage, SoundSettings.SfxVolume, SoundSettings.SfxMuted);

        int t = MapTheme.Current;
        mapName.text = MapTheme.Names[t];
        mapDesc.text = MapTheme.Descriptions[t];
    }

    void ShowVolume(Image[] bar, TMP_Text percent, TMP_Text muteLabel, Image muteImage, float volume, bool muted)
    {
        int filled = Mathf.RoundToInt(volume * 10f);
        Color on = muted ? new Color(0.45f, 0.45f, 0.5f) : new Color(1f, 0.8f, 0.15f);
        for (int i = 0; i < bar.Length; i++)
            bar[i].color = i < filled ? on : Dim;

        percent.text = muted ? "MATI" : $"{filled * 10}%";
        percent.color = muted ? new Color(1f, 0.4f, 0.35f) : Color.white;

        // Tombol ON/OFF: oranye = suara menyala, abu-abu = dibisukan
        muteLabel.text = muted ? "OFF" : "ON";
        muteImage.color = muted ? new Color(0.35f, 0.38f, 0.48f) : UiTheme.Blue;
    }

    // ---------- Pembuat elemen UI ----------

    void Label(RectTransform card, string text, float y)
    {
        var label = NewText("Label_" + text, card, text, 32, new Vector2(0f, y), new Vector2(760f, 50f));
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = new Color(0.75f, 0.85f, 1f);
    }

    Button MakeButton(RectTransform parent, string text, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick,
                      UiTheme.Kind kind = UiTheme.Kind.Dark)
    {
        // Disalin dari tombol menu yang sudah ada, agar bentuk dan hurufnya sama
        var go = Instantiate(template.gameObject, parent);
        go.name = "Button_" + text;
        go.SetActive(true);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var button = go.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
        button.interactable = true;
        UiTheme.StyleButton(button, kind, 44f);   // gaya sama dengan menu utama & toko

        var label = go.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = text;
        return button;
    }

    RectTransform NewImage(string objName, Transform parent, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = objName == "Card";   // kotak utama menahan klik ke belakang
        return (RectTransform)go.transform;
    }

    TMP_Text NewText(string objName, Transform parent, string text, float size, Vector2 pos, Vector2 boxSize)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        var rect = t.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = boxSize;
        return t;
    }
}
