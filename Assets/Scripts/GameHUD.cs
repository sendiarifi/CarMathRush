using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pasang ke objek kosong "HUD" di SampleScene. Tidak perlu mengisi apa pun.
// Mengganti tampilan teks skor dengan ikon: hati, koin, bintang, petir, dan speedometer.
// Semua ikon digambar lewat kode, jadi tidak perlu file gambar.
public class GameHUD : MonoBehaviour
{
    [Header("Warna")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.55f);
    public Color heartColor = new Color(1f, 0.25f, 0.3f);
    public Color emptyHeartColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public Color coinColor = new Color(1f, 0.8f, 0.15f);
    public Color starColor = new Color(1f, 0.95f, 0.4f);
    public Color comboColor = new Color(1f, 0.55f, 0.1f);

    // Diisi otomatis oleh DuoManager di mode DUO. Mode SOLO: dicari sendiri.
    [System.NonSerialized] public GameManager manager;

    private GameManager gm;
    private Image[] hearts;
    private TMP_Text nameLabel, coinLabel, scoreLabel, comboLabel, speedLabel;
    private GameObject comboChip;
    private RectTransform questionBg;
    private Sprite roundSprite, heartSprite, coinSprite, starSprite, boltSprite, gaugeSprite, trophySprite, flagSprite;

    // Kartu hasil (game over / pause)
    private GameObject dimLayer;
    private RectTransform card, badge;
    private TMP_Text cardTitle, primaryLabel;
    private GameObject highScoreRow, coinRow;
    private TMP_Text scoreValue, highScoreValue, coinValue;
    private bool cardVisible;
    private float popTime;

    void Start()
    {
        gm = manager != null ? manager : FindAnyObjectByType<GameManager>();
        if (gm == null || gm.scoreText == null) { enabled = false; return; }

        CreateSprites();
        // SOLO: seluruh Canvas. DUO: panel setengah layar milik pemain ini.
        Transform canvas = gm.IsDuo ? gm.scoreText.transform.parent : gm.scoreText.canvas.transform;

        gm.scoreText.gameObject.SetActive(false);   // teks skor lama disembunyikan

        // Kotak gelap di belakang soal dan layar game over
        questionBg = MakePanel("QuestionBg", canvas, new Vector2(0f, 1f), new Vector2(0f, 1f));
        questionBg.SetSiblingIndex(gm.questionText.transform.GetSiblingIndex());
        gm.gameOverText.enabled = false;   // teks game over lama diganti kartu hasil

        // Kolom kanan atas
        nameLabel = MakeText("NameLabel", canvas, 30, TextAlignmentOptions.TopRight);
        var nameRect = nameLabel.rectTransform;
        nameRect.anchorMin = nameRect.anchorMax = nameRect.pivot = new Vector2(1f, 1f);
        nameRect.anchoredPosition = new Vector2(-30f, -20f);
        nameRect.sizeDelta = new Vector2(400f, 40f);

        // Nyawa (ikon hati)
        int maxLives = Mathf.Max(1, gm.maxLives);
        var heartChip = MakeChip("LivesChip", canvas, new Vector2(1f, 1f), new Vector2(-25f, -70f),
                                 new Vector2(24f + maxLives * 48f, 60f));
        hearts = new Image[maxLives];
        for (int i = 0; i < maxLives; i++)
            hearts[i] = MakeIcon(heartChip, heartSprite, heartColor, new Vector2(12f + i * 48f, 0f), 42f);

        // DUO: koin diganti jarak tempuh (bendera finis)
        if (gm.IsDuo) coinLabel = MakeIconChip("DistanceChip", canvas, flagSprite, Color.white, -140f, 250f);
        else coinLabel = MakeIconChip("CoinChip", canvas, coinSprite, coinColor, -140f);
        scoreLabel = MakeIconChip("ScoreChip", canvas, starSprite, starColor, -210f);
        comboLabel = MakeIconChip("ComboChip", canvas, boltSprite, comboColor, -280f);
        comboChip = comboLabel.transform.parent.gameObject;

        // Speedometer di kanan bawah
        var speedChip = MakeChip("SpeedChip", canvas, new Vector2(1f, 0f), new Vector2(-25f, 25f), new Vector2(280f, 70f));
        MakeIcon(speedChip, gaugeSprite, Color.white, new Vector2(14f, 0f), 48f);
        speedLabel = MakeLabel(speedChip, 40);

        BuildResultCard(canvas);
    }

    void LateUpdate()
    {
        nameLabel.text = gm.PlayerName;
        coinLabel.text = gm.IsDuo ? $"{Mathf.FloorToInt(gm.Distance)} m" : CarData.Coins.ToString();
        scoreLabel.text = gm.Score.ToString();
        speedLabel.text = $"{gm.SpeedKmh} km/j";

        comboChip.SetActive(gm.Combo >= 2);
        comboLabel.text = $"x{gm.Combo}";

        for (int i = 0; i < hearts.Length; i++)
            hearts[i].color = i < gm.Lives ? heartColor : emptyHeartColor;

        // Kotak soal menyesuaikan panjang teks
        var q = gm.questionText;
        questionBg.gameObject.SetActive(q.gameObject.activeInHierarchy);
        if (q.gameObject.activeInHierarchy)
        {
            var qRect = q.rectTransform;
            questionBg.anchoredPosition = qRect.anchoredPosition + new Vector2(-22f, 14f);
            questionBg.sizeDelta = new Vector2(Mathf.Min(q.preferredWidth, qRect.sizeDelta.x) + 44f,
                                               q.preferredHeight + 28f);
        }

        UpdateResultCard();
    }

    // ---------- Kartu hasil ----------

    void BuildResultCard(Transform canvas)
    {
        // Layar meredup
        var dim = new GameObject("ResultDim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(canvas, false);
        var dimRect = dim.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
        dimLayer = dim;

        // Kartu
        card = MakePanel("ResultCard", dim.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        card.sizeDelta = new Vector2(720f, 600f);
        card.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.13f, 0.95f);

        cardTitle = MakeText("Title", card, 96, TextAlignmentOptions.Center);
        Place(cardTitle.rectTransform, new Vector2(0f, 215f), new Vector2(700f, 120f));
        cardTitle.fontStyle = FontStyles.Bold | FontStyles.Italic;
        cardTitle.outlineWidth = 0.25f;

        // Lencana rekor baru
        badge = MakePanel("NewRecordBadge", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        badge.GetComponent<Image>().color = coinColor;
        Place(badge, new Vector2(245f, 300f), new Vector2(240f, 62f));
        badge.localRotation = Quaternion.Euler(0f, 0f, -10f);
        var badgeText = MakeText("Text", badge, 32, TextAlignmentOptions.Center);
        Stretch(badgeText.rectTransform);
        badgeText.text = "REKOR BARU!";
        badgeText.color = new Color(0.25f, 0.12f, 0f);
        badgeText.outlineWidth = 0f;

        // Baris statistik
        if (gm.IsDuo)
        {
            // DUO: jarak (penentu menang) di atas, skor di bawahnya, tanpa high score & koin
            highScoreValue = MakeStatRow(flagSprite, Color.white, "Jarak", 95f, out highScoreRow);
            scoreValue = MakeStatRow(starSprite, starColor, "Skor", 15f, out _);
            coinValue = MakeStatRow(heartSprite, heartColor, "Sisa nyawa", -65f, out coinRow);
        }
        else
        {
            scoreValue = MakeStatRow(starSprite, starColor, "Skor", 95f, out _);
            highScoreValue = MakeStatRow(trophySprite, coinColor, "High Score", 15f, out highScoreRow);
            coinValue = MakeStatRow(coinSprite, coinColor, "Koin didapat", -65f, out coinRow);
        }

        // Tombol
        primaryLabel = MakeButton("PrimaryButton", new Vector2(-160f, -205f), new Color(1f, 0.5f, 0.1f), OnPrimary);
        var menuLabel = MakeButton("MenuButton", new Vector2(160f, -205f), new Color(0.3f, 0.35f, 0.5f), gm.GoToMenu);
        menuLabel.text = "MENU (M)";

        dimLayer.SetActive(false);
    }

    TMP_Text MakeStatRow(Sprite icon, Color iconColor, string title, float y, out GameObject row)
    {
        var rect = MakePanel("Row_" + title, card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rect.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
        Place(rect, new Vector2(0f, y), new Vector2(600f, 66f));
        MakeIcon(rect, icon, iconColor, new Vector2(16f, 0f), 44f);

        var label = MakeText("Title", rect, 34, TextAlignmentOptions.MidlineLeft);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(76f, 0f);
        label.text = title;
        label.color = new Color(0.85f, 0.85f, 0.9f);

        var value = MakeText("Value", rect, 40, TextAlignmentOptions.MidlineRight);
        Stretch(value.rectTransform);
        value.rectTransform.offsetMax = new Vector2(-22f, 0f);

        row = rect.gameObject;
        return value;
    }

    TMP_Text MakeButton(string objName, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(card, false);
        var rect = go.GetComponent<RectTransform>();
        Place(rect, pos, new Vector2(290f, 84f));

        var image = go.GetComponent<Image>();
        image.sprite = roundSprite;
        image.type = Image.Type.Sliced;

        var button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
        colors.pressedColor = color * 0.8f;
        colors.selectedColor = color;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var label = MakeText("Text", rect, 34, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return label;
    }

    void OnPrimary()
    {
        if (gm.IsGameOver) gm.Restart();
        else if (gm.IsPaused) gm.TogglePause();
    }

    void UpdateResultCard()
    {
        bool show = gm.IsGameOver || gm.IsPaused;

        if (show && !cardVisible)
        {
            cardVisible = true;
            popTime = 0f;
            dimLayer.SetActive(true);

            bool over = gm.IsGameOver;
            primaryLabel.text = over ? "MAIN LAGI (R)" : "LANJUT (P)";
            scoreValue.text = gm.Score.ToString();

            if (gm.IsDuo)
            {
                string result = gm.MatchResult;
                cardTitle.text = !over ? "PAUSE" : (string.IsNullOrEmpty(result) ? "SELESAI" : result);
                cardTitle.color = !over ? starColor
                                : result == "MENANG" ? new Color(0.35f, 1f, 0.4f)
                                : result == "KALAH" ? new Color(1f, 0.35f, 0.25f)
                                : starColor;
                highScoreRow.SetActive(true);
                coinRow.SetActive(true);
                badge.gameObject.SetActive(false);
                highScoreValue.text = $"{Mathf.FloorToInt(gm.Distance)} m";
                coinValue.text = gm.Lives.ToString();
            }
            else
            {
                cardTitle.text = over ? "GAME OVER" : "PAUSE";
                cardTitle.color = over ? new Color(1f, 0.35f, 0.25f) : starColor;
                highScoreRow.SetActive(over);
                coinRow.SetActive(over);
                badge.gameObject.SetActive(over && gm.NewRecord);
                highScoreValue.text = gm.HighScore.ToString();
                coinValue.text = $"+{gm.CoinsThisRun}";
            }
        }
        else if (!show && cardVisible)
        {
            cardVisible = false;
            dimLayer.SetActive(false);
        }

        if (!cardVisible) return;

        // Animasi muncul "pop" (tetap jalan walau game di-pause)
        popTime += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(popTime / 0.3f);
        float scale = 1f + 2.2f * Mathf.Pow(t - 1f, 3f) + 1.2f * Mathf.Pow(t - 1f, 2f);   // ease-out-back
        card.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, scale);

        // Lencana rekor berdenyut
        if (badge.gameObject.activeSelf)
            badge.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.06f);
    }

    void Place(RectTransform rect, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }

    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    // ---------- Pembuat elemen UI ----------

    RectTransform MakePanel(string objName, Transform parent, Vector2 anchor, Vector2 pivot)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = roundSprite;
        image.type = Image.Type.Sliced;
        image.color = panelColor;
        image.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        return rect;
    }

    RectTransform MakeChip(string objName, Transform parent, Vector2 corner, Vector2 pos, Vector2 size)
    {
        var rect = MakePanel(objName, parent, corner, corner);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return rect;
    }

    TMP_Text MakeIconChip(string objName, Transform parent, Sprite icon, Color iconColor, float y, float width = 200f)
    {
        var chip = MakeChip(objName, parent, new Vector2(1f, 1f), new Vector2(-25f, y), new Vector2(width, 60f));
        MakeIcon(chip, icon, iconColor, new Vector2(12f, 0f), 42f);
        return MakeLabel(chip, 38);
    }

    Image MakeIcon(RectTransform parent, Sprite sprite, Color color, Vector2 pos, float size)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(size, size);
        return image;
    }

    TMP_Text MakeLabel(RectTransform parent, float fontSize)
    {
        var label = MakeText("Label", parent, fontSize, TextAlignmentOptions.MidlineRight);
        var rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(64f, 0f);
        rect.offsetMax = new Vector2(-16f, 0f);
        return label;
    }

    TMP_Text MakeText(string objName, Transform parent, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.outlineWidth = 0.15f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        return text;
    }

    // ---------- Ikon digambar lewat kode ----------

    void CreateSprites()
    {
        roundSprite = MakeSprite(64, RoundedRect, new Vector4(22f, 22f, 22f, 22f));
        heartSprite = MakeSprite(64, Heart, Vector4.zero);
        coinSprite = MakeSprite(64, Coin, Vector4.zero);
        starSprite = MakeSprite(64, Star, Vector4.zero);
        boltSprite = MakeSprite(64, Bolt, Vector4.zero);
        gaugeSprite = MakeSprite(64, Gauge, Vector4.zero);
        trophySprite = MakeSprite(64, Trophy, Vector4.zero);
        flagSprite = MakeSprite(64, Flag, Vector4.zero);
    }

    float Flag(float x, float y)
    {
        // Tiang
        if (x > -0.78f && x < -0.62f && y > -0.92f && y < 0.92f) return 0.85f;
        // Bendera kotak-kotak hitam putih
        if (x >= -0.62f && x < 0.8f && y > 0.02f && y < 0.86f)
        {
            int cx = Mathf.FloorToInt((x + 0.62f) / 0.284f);
            int cy = Mathf.FloorToInt((y - 0.02f) / 0.28f);
            return (cx + cy) % 2 == 0 ? 1f : 0.2f;
        }
        return -1f;
    }

    float Trophy(float x, float y)
    {
        // Mangkuk piala
        float bx = x / 0.55f, by = (y - 0.75f) / 0.8f;
        if (y >= -0.05f && y <= 0.75f && bx * bx + by * by <= 1f) return 1f;
        // Pegangan kiri dan kanan
        float hx = Mathf.Abs(x) - 0.55f, hy = y - 0.45f;
        float hr = Mathf.Sqrt(hx * hx + hy * hy);
        if (hr > 0.12f && hr < 0.24f && Mathf.Abs(x) > 0.5f) return 1f;
        // Tiang dan alas
        if (Mathf.Abs(x) < 0.1f && y > -0.5f && y < 0f) return 1f;
        if (Mathf.Abs(x) < 0.42f && y > -0.85f && y < -0.5f) return 0.8f;
        return -1f;
    }

    // Fungsi bentuk: menerima titik (x, y) dari -1..1, mengembalikan kecerahan (0..1) atau -1 kalau kosong
    Sprite MakeSprite(int size, Func<float, float, float> shape, Vector4 border)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        const int samples = 4;   // dihaluskan agar tepi tidak bergerigi

        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                float coverage = 0f, brightness = 0f;
                for (int sy = 0; sy < samples; sy++)
                {
                    for (int sx = 0; sx < samples; sx++)
                    {
                        float x = ((px + (sx + 0.5f) / samples) / size) * 2f - 1f;
                        float y = ((py + (sy + 0.5f) / samples) / size) * 2f - 1f;
                        float v = shape(x, y);
                        if (v >= 0f) { coverage += 1f; brightness += v; }
                    }
                }
                float total = samples * samples;
                float b = coverage > 0f ? brightness / coverage : 1f;
                tex.SetPixel(px, py, new Color(b, b, b, coverage / total));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect, border);
    }

    float RoundedRect(float x, float y)
    {
        const float radius = 0.6f;
        float qx = Mathf.Abs(x) - (1f - radius);
        float qy = Mathf.Abs(y) - (1f - radius);
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
        return outside + inside - radius <= 0f ? 1f : -1f;
    }

    float Heart(float x, float y)
    {
        float hx = x * 1.25f, hy = (y + 0.15f) * 1.25f;
        float a = hx * hx + hy * hy - 1f;
        return a * a * a - hx * hx * hy * hy * hy <= 0f ? 1f : -1f;
    }

    float Coin(float x, float y)
    {
        float r = Mathf.Sqrt(x * x + y * y);
        if (r > 0.92f) return -1f;
        if (r > 0.6f && r < 0.72f) return 0.7f;   // cincin lebih gelap
        return 1f;
    }

    float Star(float x, float y)
    {
        var points = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float radius = i % 2 == 0 ? 0.95f : 0.42f;
            points[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius - 0.05f);
        }
        return InsidePolygon(points, x, y) ? 1f : -1f;
    }

    float Bolt(float x, float y)
    {
        Vector2[] points =
        {
            new Vector2(0.2f, 0.95f), new Vector2(-0.55f, -0.05f), new Vector2(-0.05f, -0.05f),
            new Vector2(-0.25f, -0.95f), new Vector2(0.55f, 0.15f), new Vector2(0.05f, 0.15f),
        };
        return InsidePolygon(points, x, y) ? 1f : -1f;
    }

    float Gauge(float x, float y)
    {
        float r = Mathf.Sqrt(x * x + y * y);
        if (r > 0.62f && r < 0.9f && y > -0.3f) return 1f;   // busur
        if (r < 0.16f) return 1f;                              // poros
        // jarum
        Vector2 a = Vector2.zero, b = new Vector2(0.42f, 0.5f), p = new Vector2(x, y);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (b - a).sqrMagnitude);
        return Vector2.Distance(p, a + t * (b - a)) < 0.09f ? 1f : -1f;
    }

    bool InsidePolygon(Vector2[] poly, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > y) != (poly[j].y > y) &&
                x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }
}
