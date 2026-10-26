using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tampilan menu utama (mengikuti logo dan pola game balap kasual):
// - Satu tombol utama yang menonjol: MAIN SOLO (kuning, besar, baris sendiri, bercahaya)
// - DUO VS sebagai tombol kedua (biru)
// - Tombol lain seragam navy: TOKO, PENGATURAN, KELUAR (kecil, paling bawah)
// - Koin di pojok kanan atas, logo di kiri atas
// TIDAK perlu dipasang manual: dibuat otomatis oleh MainMenu.
// Logo dibaca dari Assets/Resources/Logo.png (kalau tidak ada, judul teks lama tetap dipakai).
public class MenuLook : MonoBehaviour
{
    private const float ColumnX = -560f;   // kolom menu di sisi kiri (layar acuan 1920 x 1080)
    private const float ColumnWidth = 460f;
    private const float ContentTop = 530f, ContentBottom = 490f;

    private readonly List<GameObject> decorations = new List<GameObject>();
    private RectTransform column, canvasRect, logo, glow;
    private float time;

    public GameObject Logo => logo != null ? logo.gameObject : null;

    // Disembunyikan bersama isi menu saat toko / pengaturan dibuka
    public void SetVisible(bool visible)
    {
        foreach (var d in decorations)
            if (d != null && d != Logo) d.SetActive(visible);
    }

    public void Build(MainMenu menu, Button duoButton, Button settingsButton, GameObject titleText)
    {
        Transform canvas = menu.quitButton.transform.parent;
        canvasRect = (RectTransform)canvas;

        // --- Bayangan gelap di kiri (paling belakang), agar kolom menu terbaca di peta mana pun ---
        var shade = new GameObject("MenuShade", typeof(RectTransform), typeof(RawImage));
        shade.transform.SetParent(canvas, false);
        shade.transform.SetSiblingIndex(0);
        var sRect = (RectTransform)shade.transform;
        sRect.anchorMin = Vector2.zero;
        sRect.anchorMax = new Vector2(0.6f, 1f);
        sRect.offsetMin = sRect.offsetMax = Vector2.zero;
        var sImage = shade.GetComponent<RawImage>();
        sImage.texture = MakeShadeTexture();
        sImage.color = UiTheme.NavyDeep;
        sImage.raycastTarget = false;
        decorations.Add(shade);

        // --- Wadah kolom: dikecilkan otomatis kalau layar terlalu pendek ---
        var columnGo = new GameObject("MenuColumn", typeof(RectTransform));
        columnGo.transform.SetParent(canvas, false);
        columnGo.transform.SetSiblingIndex(1);
        column = (RectTransform)columnGo.transform;
        UiTheme.Place(column, Vector2.zero, Vector2.zero);

        // --- Logo ---
        var logoTex = Resources.Load<Texture2D>("Logo");
        if (logoTex != null)
        {
            var go = new GameObject("Logo", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(column, false);
            logo = (RectTransform)go.transform;
            UiTheme.Place(logo, new Vector2(ColumnX, 300f), new Vector2(420f, 420f));
            var raw = go.GetComponent<RawImage>();
            raw.texture = logoTex;
            raw.raycastTarget = false;
            decorations.Add(go);
            if (titleText != null) titleText.SetActive(false);
        }
        else if (titleText != null)
        {
            var t = titleText.GetComponent<TMP_Text>();
            titleText.transform.SetParent(column, false);
            UiTheme.Place((RectTransform)titleText.transform, new Vector2(ColumnX, 330f), new Vector2(700f, 150f));
            if (t != null) { t.color = UiTheme.Yellow; t.outlineColor = UiTheme.NavyDeep; t.outlineWidth = 0.3f; }
        }

        // --- High score: teks kuning di kotak navy ---
        var chip = UiTheme.Panel("HighScoreChip", column, new Color(UiTheme.Navy.r, UiTheme.Navy.g, UiTheme.Navy.b, 0.9f),
                                 new Vector2(ColumnX, 58f), new Vector2(ColumnWidth, 56f));
        decorations.Add(chip.gameObject);
        var hs = menu.highScoreText;
        hs.transform.SetParent(chip.transform, false);
        var hsRect = hs.rectTransform;
        hsRect.anchorMin = Vector2.zero;
        hsRect.anchorMax = Vector2.one;
        hsRect.offsetMin = new Vector2(56f, 0f);    // ruang untuk ikon piala
        hsRect.offsetMax = new Vector2(-16f, 0f);
        var trophy = UiTheme.Icon(chip.transform, UiTheme.IconTrophy, UiTheme.Yellow, new Vector2(-ColumnWidth / 2f + 36f, 0f), 36f);
        trophy.gameObject.AddComponent<Outline>().effectColor = UiTheme.NavyDeep;
        hs.color = UiTheme.Yellow;
        hs.enableAutoSizing = true;
        hs.fontSizeMin = 18f;
        hs.fontSizeMax = 28f;
        hs.alignment = TextAlignmentOptions.Center;
        hs.outlineWidth = 0f;

        // --- Kolom nama ---
        StyleInput(menu.nameInput, new Vector2(ColumnX, -18f), new Vector2(ColumnWidth, 70f));
        var warn = menu.warningText;
        warn.transform.SetParent(column, false);
        UiTheme.Place(warn.rectTransform, new Vector2(ColumnX, -72f), new Vector2(ColumnWidth, 36f));
        warn.color = new Color(1f, 0.5f, 0.45f);
        warn.fontSize = 26;
        warn.outlineColor = UiTheme.NavyDeep;
        warn.outlineWidth = 0.2f;

        // --- Tombol ---
        // Cahaya berdenyut di belakang tombol utama
        // (cahaya lembut berbentuk elips, bukan kotak)
        var glowImage = UiTheme.Icon(column, UiTheme.Glow, new Color(1f, 0.8f, 0.25f, 0.4f), new Vector2(ColumnX, -150f), 100f);
        glowImage.name = "PlayGlow";
        glow = glowImage.rectTransform;
        glow.sizeDelta = new Vector2(ColumnWidth + 220f, 260f);
        decorations.Add(glowImage.gameObject);

        Put(menu.playButton, new Vector2(ColumnX, -150f), new Vector2(ColumnWidth, 100f));
        UiTheme.StyleButton(menu.playButton, UiTheme.Kind.Primary, 50f, UiTheme.IconPlay);
        SetLabel(menu.playButton, "MAIN SOLO");

        Put(duoButton, new Vector2(ColumnX, -255f), new Vector2(ColumnWidth, 80f));
        UiTheme.StyleButton(duoButton, UiTheme.Kind.Secondary, 40f, UiTheme.IconDuo);
        SetLabel(duoButton, "DUO VS");

        if (menu.shopButton != null)
        {
            Put(menu.shopButton, new Vector2(ColumnX, -350f), new Vector2(ColumnWidth, 72f));
            UiTheme.StyleButton(menu.shopButton, UiTheme.Kind.Dark, 34f, UiTheme.IconCar);
            SetLabel(menu.shopButton, "TOKO");
        }

        // Pengaturan: tombol bulat bergambar gir di pojok kanan atas
        if (settingsButton != null) MakeGearButton(settingsButton, canvas);

        // KELUAR: kecil di bawah, bukan merah (merah hanya untuk hal berbahaya)
        Put(menu.quitButton, new Vector2(ColumnX, -440f), new Vector2(200f, 58f));
        UiTheme.StyleButton(menu.quitButton, UiTheme.Kind.Dark, 26f, UiTheme.IconExit);
        var quitImage = menu.quitButton.GetComponent<Image>();
        if (quitImage != null) quitImage.color = new Color(UiTheme.DarkButton.r, UiTheme.DarkButton.g, UiTheme.DarkButton.b, 0.75f);
        SetLabel(menu.quitButton, "KELUAR");

        // --- Petunjuk kontrol, kecil di pojok kanan bawah ---
        var hint = UiTheme.Text("Hint", canvas, "A/D atau panah = pindah jalur     P = pause", 24, UiTheme.TextSoft,
                                Vector2.zero, new Vector2(700f, 36f));
        var hRect = hint.rectTransform;
        hRect.anchorMin = hRect.anchorMax = hRect.pivot = new Vector2(1f, 0f);
        hRect.anchoredPosition = new Vector2(-30f, 22f);
        hint.alignment = TextAlignmentOptions.Center;
        hint.fontStyle = FontStyles.Normal;
        hint.color = Color.white;
        // Kotak navy tipis di belakang petunjuk agar terbaca di peta terang (pantai, gurun)
        var hintBg = UiTheme.Panel("HintBg", canvas, new Color(UiTheme.NavyDeep.r, UiTheme.NavyDeep.g, UiTheme.NavyDeep.b, 0.7f),
                                   Vector2.zero, new Vector2(640f, 44f));
        var hbRect = hintBg.rectTransform;
        hbRect.anchorMin = hbRect.anchorMax = hbRect.pivot = new Vector2(1f, 0f);
        hbRect.anchoredPosition = new Vector2(-24f, 18f);
        hint.transform.SetParent(hintBg.transform, false);
        hRect.anchorMin = Vector2.zero;
        hRect.anchorMax = Vector2.one;
        hRect.offsetMin = hRect.offsetMax = Vector2.zero;
        decorations.Add(hintBg.gameObject);

        FitColumn();
    }

    void LateUpdate()
    {
        FitColumn();
        time += Time.unscaledDeltaTime;

        // Cahaya tombol utama berdenyut pelan
        if (glow != null)
        {
            float pulse = (Mathf.Sin(time * 3f) + 1f) / 2f;
            glow.localScale = new Vector3(1f + pulse * 0.05f, 1f + pulse * 0.15f, 1f);
            glow.GetComponent<Image>().color = new Color(1f, 0.8f, 0.25f, 0.3f + pulse * 0.35f);
        }

        // Logo muncul dengan efek "pop", lalu mengambang pelan
        if (logo != null)
        {
            float t = Mathf.Clamp01(time / 0.5f);
            float pop = 1f + 2.2f * Mathf.Pow(t - 1f, 3f) + 1.2f * Mathf.Pow(t - 1f, 2f);   // ease-out-back
            float scale = Mathf.LerpUnclamped(0.5f, 1f, pop);
            logo.localScale = new Vector3(scale, scale, 1f);
            logo.anchoredPosition = new Vector2(ColumnX, 300f + Mathf.Sin(time * 1.5f) * 6f);
        }
    }

    // Kecilkan kolom kalau tinggi layar (dalam satuan acuan) kurang dari isi kolom
    void FitColumn()
    {
        if (column == null || canvasRect == null) return;
        float halfHeight = canvasRect.rect.height / 2f - 12f;
        float scale = Mathf.Min(1f, halfHeight / ContentTop, halfHeight / ContentBottom);
        column.localScale = new Vector3(scale, scale, 1f);
    }

    void MakeGearButton(Button button, Transform canvas)
    {
        button.transform.SetParent(canvas, false);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-80f, -78f);
        rect.sizeDelta = new Vector2(96f, 96f);

        var image = button.GetComponent<Image>();
        if (image != null) image.sprite = UiTheme.Circle;   // tombol bulat
        UiTheme.StyleButton(button, UiTheme.Kind.Dark);
        SetLabel(button, "");

        var gear = UiTheme.Icon(button.transform, UiTheme.Gear, Color.white, Vector2.zero, 60f);
        gear.gameObject.AddComponent<Outline>().effectColor = UiTheme.NavyDeep;

        // Gir berputar saat disorot mouse
        button.GetComponent<MenuButtonFx>().spinIcon = gear.rectTransform;
    }

    void Put(Button button, Vector2 pos, Vector2 size)
    {
        button.transform.SetParent(column, false);
        UiTheme.Place((RectTransform)button.transform, pos, size);
    }

    void SetLabel(Button button, string text)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = text;
    }

    void StyleInput(TMP_InputField input, Vector2 pos, Vector2 size)
    {
        input.transform.SetParent(column, false);
        UiTheme.Place((RectTransform)input.transform, pos, size);

        var image = input.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = UiTheme.Rounded;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var outline = input.gameObject.AddComponent<Outline>();
            outline.effectColor = UiTheme.NavyDeep;
            outline.effectDistance = new Vector2(2.5f, 2.5f);
        }
        if (input.textComponent != null)
        {
            input.textComponent.color = UiTheme.Navy;
            input.textComponent.fontSize = 32;
            input.textComponent.fontStyle = FontStyles.Bold;
            input.textComponent.alignment = TextAlignmentOptions.Center;
        }
        if (input.placeholder is TMP_Text placeholder)
        {
            placeholder.text = "Ketik namamu...";
            placeholder.color = new Color(0.5f, 0.56f, 0.7f);
            placeholder.fontSize = 30;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.alignment = TextAlignmentOptions.Center;
        }
    }

    // Gradasi gelap dari kiri (pekat) ke kanan (bening)
    static Texture2D MakeShadeTexture()
    {
        const int width = 256;
        var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < width; x++)
        {
            float t = x / (float)(width - 1);
            tex.SetPixel(x, 0, new Color(1f, 1f, 1f, Mathf.Lerp(0.8f, 0f, Mathf.SmoothStep(0f, 1f, t))));
        }
        tex.Apply();
        return tex;
    }
}
