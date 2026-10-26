using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Gaya tampilan bersama untuk menu utama, toko, dan pengaturan, agar semuanya seragam.
// Palet diambil dari logo: KUNING (tombol utama), BIRU (tombol kedua), NAVY (tombol lain & panel).
// Tidak perlu dipasang ke objek mana pun.
public static class UiTheme
{
    public enum Kind { Primary, Secondary, Dark, Danger }   // Danger (merah) hanya untuk hal yang tidak bisa dibatalkan

    public static readonly Color Navy = new Color(0.07f, 0.14f, 0.32f);
    public static readonly Color NavyDeep = new Color(0.03f, 0.06f, 0.15f);
    public static readonly Color Yellow = new Color(1f, 0.78f, 0.12f);
    public static readonly Color Blue = new Color(0.18f, 0.52f, 0.92f);
    public static readonly Color DarkButton = new Color(0.11f, 0.19f, 0.4f);
    public static readonly Color TextSoft = new Color(0.78f, 0.84f, 0.96f);
    public static readonly Color Red = new Color(0.88f, 0.22f, 0.2f);

    private static Sprite rounded, circle, coin, glow, gear, padlock;
    private static Sprite buttonRounded, buttonCircle, iconPlay, iconDuo, iconCar, iconExit, iconTrophy;

    // ---------- Tombol ----------

    // Gaya tombol ala game balap kasual (referensi: Beach Buggy Racing):
    // gradasi atas-terang bawah-gelap, kilau di separuh atas, bentuk sedikit miring,
    // garis tepi navy tebal, bibir bawah gelap, teks putih miring bergaris tepi, dan ikon opsional di kiri.
    public static void StyleButton(Button button, Kind kind, float maxFontSize = 40f, Sprite icon = null)
    {
        Color fill, lip;
        switch (kind)
        {
            case Kind.Primary: fill = new Color(1f, 0.74f, 0.08f); lip = new Color(0.7f, 0.36f, 0.02f); break;
            case Kind.Secondary: fill = Blue; lip = new Color(0.06f, 0.24f, 0.55f); break;
            case Kind.Danger: fill = Red; lip = new Color(0.45f, 0.06f, 0.06f); break;
            default: fill = DarkButton; lip = NavyDeep; break;
        }

        var image = button.GetComponent<Image>();
        bool round = image != null && image.sprite != null && (image.sprite == circle || image.sprite == buttonCircle);
        if (image != null)
        {
            image.sprite = round ? ButtonCircle : ButtonRounded;
            image.type = round ? Image.Type.Simple : Image.Type.Sliced;
            image.color = fill;

            // Bentuk miring (hanya tombol kotak), dipasang sebelum bayangan agar bayangan ikut miring
            if (!round && button.GetComponent<UiSkew>() == null) button.gameObject.AddComponent<UiSkew>().amount = SkewAmount;

            foreach (var old in button.GetComponents<Shadow>()) Object.Destroy(old);
            var shadow = button.gameObject.AddComponent<Shadow>();     // bibir bawah: tombol terlihat tebal
            shadow.effectColor = lip;
            shadow.effectDistance = new Vector2(0f, -7f);
            var outline = button.gameObject.AddComponent<Outline>();    // garis tepi navy tebal seperti logo
            outline.effectColor = NavyDeep;
            outline.effectDistance = new Vector2(3.5f, 3.5f);

            AddShine(button.transform, round);
        }

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.95f, 0.95f, 0.95f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
        colors.selectedColor = new Color(0.95f, 0.95f, 0.95f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 1f);
        button.colors = colors;

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold | FontStyles.Italic;   // miring = kesan cepat
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = maxFontSize;
            label.characterSpacing = 2f;
            label.outlineWidth = 0.28f;
            label.outlineColor = NavyDeep;
            label.transform.SetAsLastSibling();   // teks selalu di atas kilau & ikon
        }

        if (icon != null) SetIcon(button, icon);

        if (button.GetComponent<MenuButtonFx>() == null)
            button.gameObject.AddComponent<MenuButtonFx>();
    }

    public const float SkewAmount = 0.16f;

    // Kilau putih tipis di separuh atas tombol
    static void AddShine(Transform button, bool round)
    {
        if (button.Find("Shine") != null) return;
        var go = new GameObject("Shine", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(button, false);
        go.transform.SetSiblingIndex(0);
        var rect = (RectTransform)go.transform;
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        if (round)
        {
            image.sprite = Circle;
            image.color = new Color(1f, 1f, 1f, 0.22f);
            rect.anchorMin = new Vector2(0.22f, 0.5f);
            rect.anchorMax = new Vector2(0.78f, 0.9f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        else
        {
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.6f;   // sudut kilau lebih kecil dari sudut tombol
            image.color = new Color(1f, 1f, 1f, 0.24f);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);      // pivot di tengah tombol agar miringnya sejajar tombol
            rect.offsetMin = new Vector2(9f, 2f);
            rect.offsetMax = new Vector2(-9f, -6f);
            go.AddComponent<UiSkew>().amount = SkewAmount;
        }
    }

    // Ikon putih di sisi kiri tombol, teks bergeser ke kanan
    static void SetIcon(Button button, Sprite sprite)
    {
        var rect = (RectTransform)button.transform;
        float h = rect.rect.height > 0f ? rect.rect.height : rect.sizeDelta.y;
        float size = h * 0.58f;

        var existing = button.transform.Find("ButtonIcon");
        Image icon;
        if (existing != null) icon = existing.GetComponent<Image>();
        else
        {
            var go = new GameObject("ButtonIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(button.transform, false);
            icon = go.GetComponent<Image>();
            icon.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = NavyDeep;
            outline.effectDistance = new Vector2(2f, 2f);
        }
        icon.sprite = sprite;
        icon.color = Color.white;
        var iRect = icon.rectTransform;
        iRect.anchorMin = iRect.anchorMax = new Vector2(0f, 0.5f);
        iRect.pivot = new Vector2(0.5f, 0.5f);
        iRect.sizeDelta = new Vector2(size, size);
        iRect.anchoredPosition = new Vector2(h * 0.62f, 0f);

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            var lRect = label.rectTransform;
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.offsetMin = new Vector2(h * 1.05f, 0f);
            lRect.offsetMax = new Vector2(-h * 0.35f, 0f);
            label.transform.SetAsLastSibling();
        }
    }

    // Panel / kotak bersudut bulat
    public static Image Panel(string name, Transform parent, Color color, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = Rounded;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        Place((RectTransform)go.transform, pos, size);
        return image;
    }

    public static TMP_Text Text(string name, Transform parent, string value, float size, Color color, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = value;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.raycastTarget = false;
        t.outlineWidth = 0.18f;
        t.outlineColor = NavyDeep;
        Place(t.rectTransform, pos, box);
        return t;
    }

    public static Image Icon(Transform parent, Sprite sprite, Color color, Vector2 pos, float size)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        Place((RectTransform)go.transform, pos, new Vector2(size, size));
        return image;
    }

    public static void Place(RectTransform rect, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }

    // ---------- Gambar dibuat lewat kode (tanpa file gambar) ----------

    public static Sprite Rounded
    {
        get
        {
            if (rounded == null)
                rounded = MakeSprite(64, (x, y) => RoundedRect(x, y, 64, 24f), new Vector4(24f, 24f, 24f, 24f));
            return rounded;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (circle == null)
                circle = MakeSprite(128, (x, y) => Mathf.Clamp01((64f - Mathf.Sqrt(Sq(x - 64f) + Sq(y - 64f))) + 0.5f), Vector4.zero);
            return circle;
        }
    }

    // Koin emas dengan cincin gelap
    public static Sprite Coin
    {
        get
        {
            if (coin == null)
            {
                coin = MakeSprite(64, (x, y) =>
                {
                    float r = Mathf.Sqrt(Sq(x - 32f) + Sq(y - 32f));
                    return Mathf.Clamp01(30f - r + 0.5f);
                }, Vector4.zero, (x, y) =>
                {
                    float r = Mathf.Sqrt(Sq(x - 32f) + Sq(y - 32f));
                    if (r > 20f && r < 24f) return new Color(0.8f, 0.5f, 0.05f);
                    return y > 34f ? new Color(1f, 0.88f, 0.35f) : new Color(1f, 0.75f, 0.12f);
                });
            }
            return coin;
        }
    }

    // Cahaya bulat lembut (untuk sorotan di belakang mobil)
    public static Sprite Glow
    {
        get
        {
            if (glow == null)
                glow = MakeSprite(128, (x, y) =>
                {
                    float r = Mathf.Sqrt(Sq(x - 64f) + Sq(y - 64f)) / 64f;
                    return Mathf.Pow(Mathf.Clamp01(1f - r), 2f);
                }, Vector4.zero);
            return glow;
        }
    }

    // Kecerahan tombol dari bawah (gelap) ke atas (terang): kesan timbul / bevel
    static float Bevel(float y01)
    {
        if (y01 < 0.32f) return Mathf.Lerp(0.68f, 0.86f, y01 / 0.32f);
        if (y01 < 0.62f) return 0.9f;
        return Mathf.Lerp(0.95f, 1f, (y01 - 0.62f) / 0.38f);
    }

    public static Sprite ButtonRounded
    {
        get
        {
            if (buttonRounded == null)
                buttonRounded = MakeSprite(64, (x, y) => RoundedRect(x, y, 64, 24f), new Vector4(24f, 24f, 24f, 24f),
                    (x, y) => { float b = Bevel(y / 64f); return new Color(b, b, b); });
            return buttonRounded;
        }
    }

    public static Sprite ButtonCircle
    {
        get
        {
            if (buttonCircle == null)
                buttonCircle = MakeSprite(128, (x, y) => Mathf.Clamp01((64f - Mathf.Sqrt(Sq(x - 64f) + Sq(y - 64f))) + 0.5f), Vector4.zero,
                    (x, y) => { float b = Bevel(y / 128f); return new Color(b, b, b); });
            return buttonCircle;
        }
    }

    // ---------- Ikon tombol ----------

    public static Sprite IconPlay => iconPlay != null ? iconPlay : (iconPlay = MakeSprite(128, Smooth((x, y) =>
        InsideTriangle(x, y, new Vector2(38f, 22f), new Vector2(38f, 106f), new Vector2(108f, 64f))), Vector4.zero));

    // Dua orang (mode 2 pemain): kepala bulat terpisah dari badan setengah elips
    public static Sprite IconDuo => iconDuo != null ? iconDuo : (iconDuo = MakeSprite(128, Smooth((x, y) =>
    {
        foreach (float cx in new[] { 38f, 90f })
        {
            if (Sq(x - cx) + Sq(y - 84f) < 16f * 16f) return true;                                   // kepala
            if (y >= 14f && Sq((x - cx) / 25f) + Sq((y - 14f) / 46f) <= 1f) return true;              // badan
        }
        return false;
    }), Vector4.zero));

    // Mobil (garasi / toko): badan, kabin berjendela, dan roda berlubang
    public static Sprite IconCar => iconCar != null ? iconCar : (iconCar = MakeSprite(128, Smooth((x, y) =>
    {
        foreach (float wx in new[] { 36f, 92f })
        {
            float r = Mathf.Sqrt(Sq(x - wx) + Sq(y - 34f));
            if (r < 6f) return false;                 // lubang roda
            if (r < 16f) return true;                 // roda
            if (r < 21f && y < 52f) return false;     // celah antara roda dan badan
        }
        if (x > 46f && x < 82f && y > 80f && y < 98f) return false;                       // jendela
        if (RoundedRectInside(x, y, 8f, 40f, 120f, 76f, 12f)) return true;                 // badan
        float t = (y - 74f) / 32f;                                                         // kabin
        return t >= 0f && t <= 1f && x > Mathf.Lerp(28f, 42f, t) && x < Mathf.Lerp(100f, 86f, t);
    }), Vector4.zero));

    // Pintu dengan panah keluar
    public static Sprite IconExit => iconExit != null ? iconExit : (iconExit = MakeSprite(128, Smooth((x, y) =>
    {
        bool frame = x > 18f && x < 70f && y > 14f && y < 114f && !(x > 30f && x < 70f && y > 26f && y < 102f);
        bool shaft = x > 48f && x < 94f && y > 57f && y < 71f;
        bool head = InsideTriangle(x, y, new Vector2(90f, 38f), new Vector2(90f, 90f), new Vector2(120f, 64f));
        return frame || shaft || head;
    }), Vector4.zero));

    // Piala (high score)
    public static Sprite IconTrophy => iconTrophy != null ? iconTrophy : (iconTrophy = MakeSprite(128, Smooth((x, y) =>
    {
        if (y > 62f && y < 112f && Sq((x - 64f) / 34f) + Sq((y - 112f) / 50f) <= 1f) return true;   // mangkuk
        float hl = Mathf.Sqrt(Sq(x - 30f) + Sq(y - 88f)), hr = Mathf.Sqrt(Sq(x - 98f) + Sq(y - 88f));
        if ((hl > 10f && hl < 17f && x < 34f) || (hr > 10f && hr < 17f && x > 94f)) return true;     // pegangan
        if (x > 57f && x < 71f && y > 36f && y < 66f) return true;                                   // tiang
        return RoundedRectInside(x, y, 38f, 16f, 90f, 36f, 5f);                                       // alas
    }), Vector4.zero));

    static bool InsideTriangle(float x, float y, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (x - b.x) * (a.y - b.y) - (a.x - b.x) * (y - b.y);
        float d2 = (x - c.x) * (b.y - c.y) - (b.x - c.x) * (y - c.y);
        float d3 = (x - a.x) * (c.y - a.y) - (c.x - a.x) * (y - a.y);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    // Ikon gir (pengaturan)
    public static Sprite Gear
    {
        get
        {
            if (gear == null)
                gear = MakeSprite(128, Smooth((x, y) =>
                {
                    float dx = x - 64f, dy = y - 64f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r < 19f) return false;                      // lubang tengah
                    float a = Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 0.5f;
                    float t = (a * 8f) % 1f;                         // 8 gigi
                    bool tooth = t > 0.3f && t < 0.7f;
                    return r < (tooth ? 60f : 46f);
                }), Vector4.zero);
            return gear;
        }
    }

    // Ikon gembok (mobil terkunci)
    public static Sprite Padlock
    {
        get
        {
            if (padlock == null)
                padlock = MakeSprite(128, Smooth((x, y) =>
                {
                    // Lubang kunci
                    if (Sq(x - 64f) + Sq(y - 48f) < 64f) return false;
                    if (x > 60f && x < 68f && y > 28f && y < 48f) return false;
                    // Badan gembok (kotak bulat)
                    if (RoundedRectInside(x, y, 26f, 10f, 102f, 74f, 10f)) return true;
                    // Lengkung pengait
                    float r = Mathf.Sqrt(Sq(x - 64f) + Sq(y - 74f));
                    if (y >= 74f && r > 16f && r < 28f) return true;
                    if (y > 66f && y < 74f && ((x > 36f && x < 48f) || (x > 80f && x < 92f))) return true;
                    return false;
                }), Vector4.zero);
            return padlock;
        }
    }

    static bool RoundedRectInside(float x, float y, float x0, float y0, float x1, float y1, float r)
    {
        float cx = Mathf.Clamp(x, x0 + r, x1 - r), cy = Mathf.Clamp(y, y0 + r, y1 - r);
        return Sq(x - cx) + Sq(y - cy) <= r * r;
    }

    // Menghaluskan tepi bentuk dengan 4x4 titik sampel per piksel
    static System.Func<float, float, float> Smooth(System.Func<float, float, bool> inside)
    {
        return (px, py) =>
        {
            int hits = 0;
            for (int sy = 0; sy < 4; sy++)
                for (int sx = 0; sx < 4; sx++)
                    if (inside(px - 0.5f + (sx + 0.5f) / 4f, py - 0.5f + (sy + 0.5f) / 4f)) hits++;
            return hits / 16f;
        };
    }

    static float Sq(float v) => v * v;

    static float RoundedRect(float x, float y, int size, float radius)
    {
        float half = size / 2f;
        float qx = Mathf.Max(Mathf.Abs(x - half) - (half - radius), 0f);
        float qy = Mathf.Max(Mathf.Abs(y - half) - (half - radius), 0f);
        float d = Mathf.Sqrt(qx * qx + qy * qy) - radius;
        return Mathf.Clamp01(0.5f - d);
    }

    static Sprite MakeSprite(int size, System.Func<float, float, float> alpha, Vector4 border,
                             System.Func<float, float, Color> color = null)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                Color c = color != null ? color(px, py) : Color.white;
                c.a = alpha(px, py);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect, border);
    }
}
