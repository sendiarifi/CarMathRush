using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Pasang ke objek ShopPanel di scene MainMenu.
// Tampilan GARASI: pita judul + koin di atas, mobil besar di panggung bercahaya (warna cahaya mengikuti
// kelangkaan), mobil terkunci tampil gelap bergembok, titik penanda + "3 / 6", dan satu tombol aksi besar.
// Membeli mobil memunculkan confetti + "MOBIL BARU!". Koin kurang: tombol bergetar.
// Semua elemen lama di scene dipakai ulang dan diatur ulang posisinya lewat kode.
public class ShopMenu : MonoBehaviour
{
    [Header("Referensi (pilih lewat ikon bulat)")]
    public TMP_Text coinText;
    public TMP_Text carNameText;
    public TMP_Text carInfoText;
    public Button prevButton;
    public Button nextButton;
    public Button actionButton;
    public Button backButton;

    [Header("Preview mobil 3D (cadangan: model diambil dari Katalog Mobil)")]
    public RawImage previewImage;
    public GameObject[] carModels;
    public float previewDistance = 4f;
    public float rotateSpeed = 40f;

    // Layer khusus panggung preview: kamera preview HANYA melihat layer ini,
    // jadi tanah, pohon, dan jalan di latar menu tidak ikut terekam
    private const int PreviewLayer = 30;
    private const float ContentTop = 520f, ContentBottom = 500f;

    private Transform stage;
    private Transform turntable;
    private GameObject previewCar;

    private TMP_Text actionLabel;
    private int index;       // nomor mobil di katalog
    private int pos;         // urutan tampil di garasi (dari harga termurah)
    [System.NonSerialized] private int[] order;   // dibuat ulang setiap kali jumlah mobil berbeda

    // Elemen tambahan yang dibuat lewat kode
    private RectTransform content, panelRect;
    private Image rarityPill, actionCoin, spotlight, padlock, coinChipImage;
    private TMP_Text rarityLabel, counterText, toastText;
    private Image[] dots;
    private Material rimMaterial;
    private float namePop = 1f, shakeTime, toastTime = 1f, flashTime;
    private Vector2 actionPos;

    // Confetti saat membeli
    private RectTransform[] confetti;
    private Vector2[] confettiVelocity;
    private float confettiTime = 99f;

    void Awake()
    {
        actionLabel = actionButton.GetComponentInChildren<TMP_Text>();

        prevButton.onClick.AddListener(() => Change(-1));
        nextButton.onClick.AddListener(() => Change(1));
        actionButton.onClick.AddListener(OnAction);
        backButton.onClick.AddListener(() => gameObject.SetActive(false));

        StylePanel();
        SetupPreview();
        BuildLayout();
    }

    // Latar gradasi navy yang sedikit transparan
    void StylePanel()
    {
        var image = GetComponent<Image>();
        if (image == null) return;

        const int height = 256;
        var tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color top = new Color(0.1f, 0.18f, 0.4f);
        Color bottom = new Color(0.02f, 0.04f, 0.12f);
        for (int y = 0; y < height; y++)
            tex.SetPixel(0, y, Color.Lerp(bottom, top, y / (float)(height - 1)));
        tex.Apply();

        image.sprite = Sprite.Create(tex, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f));
        image.type = Image.Type.Simple;
        image.color = new Color(1f, 1f, 1f, 0.92f);
    }

    // ---------- Tata letak GARASI ----------

    void BuildLayout()
    {
        panelRect = (RectTransform)transform;

        // Wadah isi: dikecilkan otomatis kalau layar terlalu pendek
        var go = new GameObject("ShopContent", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        content = (RectTransform)go.transform;
        UiTheme.Place(content, Vector2.zero, Vector2.zero);

        // Pita gelap di atas (selebar layar) dengan garis kuning tipis di bawahnya
        var band = new GameObject("HeaderBand", typeof(RectTransform), typeof(Image));
        band.transform.SetParent(transform, false);
        band.transform.SetSiblingIndex(0);
        var bandRect = (RectTransform)band.transform;
        bandRect.anchorMin = new Vector2(0f, 1f);
        bandRect.anchorMax = new Vector2(1f, 1f);
        bandRect.pivot = new Vector2(0.5f, 1f);
        bandRect.anchoredPosition = Vector2.zero;
        bandRect.sizeDelta = new Vector2(0f, 150f);
        var bandImage = band.GetComponent<Image>();
        bandImage.color = new Color(UiTheme.NavyDeep.r, UiTheme.NavyDeep.g, UiTheme.NavyDeep.b, 0.85f);
        bandImage.raycastTarget = false;
        var stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
        stripe.transform.SetParent(band.transform, false);
        var stripeRect = (RectTransform)stripe.transform;
        stripeRect.anchorMin = Vector2.zero;
        stripeRect.anchorMax = new Vector2(1f, 0f);
        stripeRect.pivot = new Vector2(0.5f, 0f);
        stripeRect.sizeDelta = new Vector2(0f, 5f);
        stripe.GetComponent<Image>().color = UiTheme.Yellow;
        stripe.GetComponent<Image>().raycastTarget = false;

        // Judul
        var title = UiTheme.Text("Title", content, "GARASI", 76, UiTheme.Yellow, new Vector2(0f, 455f), new Vector2(600f, 100f));
        title.fontStyle = FontStyles.Bold | FontStyles.Italic;
        title.outlineWidth = 0.25f;

        // Tombol kembali di kiri atas
        Put(backButton.transform, new Vector2(-780f, 455f), new Vector2(240f, 70f));
        UiTheme.StyleButton(backButton, UiTheme.Kind.Dark, 30f);
        SetText(backButton, "< KEMBALI");

        // Koin di kanan atas (teks koin lama dipindah ke dalam kotak)
        var coinChip = UiTheme.Panel("CoinChip", content, new Color(UiTheme.Navy.r, UiTheme.Navy.g, UiTheme.Navy.b, 0.95f),
                                     new Vector2(780f, 455f), new Vector2(240f, 70f));
        coinChip.gameObject.AddComponent<Outline>().effectColor = UiTheme.NavyDeep;
        coinChipImage = coinChip;
        UiTheme.Icon(coinChip.transform, UiTheme.Coin, Color.white, new Vector2(-78f, 0f), 48f);
        coinText.transform.SetParent(coinChip.transform, false);
        UiTheme.Place(coinText.rectTransform, new Vector2(25f, 0f), new Vector2(150f, 60f));
        coinText.alignment = TextAlignmentOptions.MidlineRight;
        coinText.fontSize = 38;
        coinText.fontStyle = FontStyles.Bold;
        coinText.color = Color.white;
        coinText.enableAutoSizing = false;

        // Sorotan cahaya di belakang mobil
        spotlight = UiTheme.Icon(content, UiTheme.Glow, new Color(1f, 0.85f, 0.45f, 0.35f), new Vector2(0f, 150f), 780f);
        spotlight.name = "Spotlight";

        // Preview mobil besar
        if (previewImage != null)
        {
            previewImage.transform.SetParent(content, false);
            UiTheme.Place(previewImage.rectTransform, new Vector2(0f, 150f), new Vector2(860f, 516f));
            previewImage.raycastTarget = false;
        }

        // Gembok besar di atas mobil yang belum dimiliki
        padlock = UiTheme.Icon(content, UiTheme.Padlock, Color.white, new Vector2(0f, 190f), 130f);
        padlock.gameObject.AddComponent<Outline>().effectColor = UiTheme.NavyDeep;

        // Panah bulat di kiri-kanan mobil
        MakeArrow(prevButton, new Vector2(-560f, 150f), "<");
        MakeArrow(nextButton, new Vector2(560f, 150f), ">");

        // Nama mobil
        carNameText.transform.SetParent(content, false);
        UiTheme.Place(carNameText.rectTransform, new Vector2(0f, -150f), new Vector2(900f, 90f));
        carNameText.fontSize = 66;
        carNameText.enableAutoSizing = false;
        carNameText.fontStyle = FontStyles.Bold;
        carNameText.color = Color.white;
        carNameText.alignment = TextAlignmentOptions.Center;
        carNameText.outlineWidth = 0.2f;
        carNameText.outlineColor = UiTheme.NavyDeep;

        // Label kelangkaan berbentuk pil berwarna
        rarityPill = UiTheme.Panel("RarityPill", content, Color.gray, new Vector2(0f, -220f), new Vector2(230f, 46f));
        rarityLabel = UiTheme.Text("Rarity", rarityPill.transform, "", 26, Color.white, Vector2.zero, new Vector2(220f, 44f));
        rarityLabel.characterSpacing = 4f;

        // Status (sudah dimiliki / harga)
        carInfoText.transform.SetParent(content, false);
        UiTheme.Place(carInfoText.rectTransform, new Vector2(0f, -272f), new Vector2(900f, 44f));
        carInfoText.fontSize = 28;
        carInfoText.enableAutoSizing = false;
        carInfoText.fontStyle = FontStyles.Normal;
        carInfoText.color = UiTheme.TextSoft;
        carInfoText.alignment = TextAlignmentOptions.Center;

        // Titik penanda mobil ke berapa
        int count = CarData.Count;
        float spacing = Mathf.Min(42f, 760f / count);   // banyak mobil: titik lebih rapat
        dots = new Image[count];
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) / 2f) * spacing;
            dots[i] = UiTheme.Icon(content, UiTheme.Circle, Color.white, new Vector2(x, -322f), 18f);
            dots[i].gameObject.AddComponent<Outline>().effectColor = UiTheme.NavyDeep;
        }
        counterText = UiTheme.Text("Counter", content, "", 26, UiTheme.TextSoft, new Vector2((count - 1) / 2f * spacing + 75f, -322f), new Vector2(110f, 36f));
        counterText.fontStyle = FontStyles.Normal;

        // Tombol aksi besar, dengan ruang kosong di sekitarnya
        Put(actionButton.transform, new Vector2(0f, -420f), new Vector2(480f, 100f));
        actionCoin = UiTheme.Icon(actionButton.transform, UiTheme.Coin, Color.white, new Vector2(180f, 0f), 44f);
        var coinEdge = actionCoin.gameObject.AddComponent<Outline>();   // garis navy agar koin terlihat di atas tombol kuning
        coinEdge.effectColor = UiTheme.Navy;
        coinEdge.effectDistance = new Vector2(2.5f, 2.5f);
        actionPos = ((RectTransform)actionButton.transform).anchoredPosition;

        // Tulisan "MOBIL BARU!" yang meletup di atas mobil
        toastText = UiTheme.Text("Toast", content, "", 84, UiTheme.Yellow, new Vector2(0f, 330f), new Vector2(900f, 120f));
        toastText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        toastText.outlineWidth = 0.3f;
        toastText.gameObject.SetActive(false);

        // Potongan confetti (disembunyikan sampai membeli)
        Color[] colors = { UiTheme.Yellow, UiTheme.Blue, UiTheme.Red, new Color(0.25f, 0.8f, 0.35f), Color.white };
        confetti = new RectTransform[48];
        confettiVelocity = new Vector2[confetti.Length];
        for (int i = 0; i < confetti.Length; i++)
        {
            var piece = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
            piece.transform.SetParent(content, false);
            var img = piece.GetComponent<Image>();
            img.color = colors[i % colors.Length];
            img.raycastTarget = false;
            confetti[i] = (RectTransform)piece.transform;
            confetti[i].sizeDelta = new Vector2(Random.Range(10f, 18f), Random.Range(16f, 28f));
            piece.SetActive(false);
        }

        FitContent();
    }

    void MakeArrow(Button button, Vector2 pos, string text)
    {
        Put(button.transform, pos, new Vector2(120f, 120f));
        var image = button.GetComponent<Image>();
        if (image != null) image.sprite = UiTheme.Circle;
        UiTheme.StyleButton(button, UiTheme.Kind.Dark, 64f);
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = text;
            label.color = UiTheme.Yellow;
            label.characterSpacing = 0f;
        }
    }

    void Put(Transform t, Vector2 pos, Vector2 size)
    {
        t.SetParent(content, false);
        UiTheme.Place((RectTransform)t, pos, size);
    }

    void SetText(Button button, string text)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = text;
    }

    void FitContent()
    {
        if (content == null || panelRect == null) return;
        float halfHeight = panelRect.rect.height / 2f - 10f;
        if (halfHeight <= 0f) return;
        float scale = Mathf.Min(1f, halfHeight / ContentTop, halfHeight / ContentBottom);
        content.localScale = new Vector3(scale, scale, 1f);
    }

    // ---------- Preview 3D ----------

    // Membuat "panggung" + kamera kecil yang merekam mobil ke kotak gambar
    void SetupPreview()
    {
        if (previewImage == null) return;

        stage = new GameObject("PreviewStage").transform;
        stage.position = new Vector3(100f, 0f, 0f);   // jauh dari pandangan kamera utama

        var camObj = new GameObject("PreviewCamera");
        var cam = camObj.AddComponent<Camera>();
        // Jarak minimal 5,5 agar panggung utuh dan atap kendaraan tinggi (ambulans, pemadam) tidak terpotong
        float distance = Mathf.Max(previewDistance, 5.5f);
        camObj.transform.position = stage.position + new Vector3(0f, distance * 0.44f, -distance);
        camObj.transform.LookAt(stage.position + new Vector3(0f, 0.4f, 0f));
        cam.clearFlags = CameraClearFlags.SolidColor;
        // Latar transparan: mobil tampil langsung di atas panel tanpa kotak
        cam.backgroundColor = new Color(0.06f, 0.1f, 0.25f, 0f);
        cam.fieldOfView = 36f;
        cam.cullingMask = 1 << PreviewLayer;

        // Kamera utama menu tidak perlu melihat panggung preview
        if (Camera.main != null) Camera.main.cullingMask &= ~(1 << PreviewLayer);

        var renderTexture = new RenderTexture(1000, 600, 24) { antiAliasing = 4 };   // tepi mobil lebih halus
        cam.targetTexture = renderTexture;
        previewImage.texture = renderTexture;
        previewImage.color = Color.white;

        // Panggung putar di bawah mobil
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "Platform";
        Destroy(platform.GetComponent<Collider>());
        platform.transform.SetParent(stage, false);
        platform.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        platform.transform.localScale = new Vector3(3.4f, 0.05f, 3.4f);
        platform.GetComponent<Renderer>().sharedMaterial = MaterialHelper.Create(new Color(0.12f, 0.18f, 0.38f));
        turntable = platform.transform;

        // Cincin tepi panggung berwarna kuning (warna utama tema)
        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "Rim";
        Destroy(rim.GetComponent<Collider>());
        rim.transform.SetParent(stage, false);
        rim.transform.localPosition = new Vector3(0f, -0.09f, 0f);
        rim.transform.localScale = new Vector3(3.6f, 0.04f, 3.6f);
        rimMaterial = MaterialHelper.Create(UiTheme.Yellow);
        rim.GetComponent<Renderer>().sharedMaterial = rimMaterial;

        SetLayer(stage.gameObject);
        camObj.transform.SetParent(stage, true);
    }

    void ShowPreview()
    {
        if (stage == null) return;
        if (previewCar != null) Destroy(previewCar);
        GameObject prefab = CarData.Model(index, carModels);   // dari Katalog Mobil
        if (prefab == null) return;

        previewCar = Instantiate(prefab, stage);
        previewCar.transform.localPosition = Vector3.zero;
        previewCar.transform.localRotation = Quaternion.identity;

        // Pengaman: hanya model yang jauh lebih besar dari mobil Kenney (panjang > 3,6) yang dikecilkan.
        // Semua mobil Kenney (panjang 1,4 - 3,45) tampil dengan ukuran aslinya agar perbandingannya jujur.
        var renderers = previewCar.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            float longest = Mathf.Max(b.size.x, b.size.z);
            if (longest > 3.6f) previewCar.transform.localScale *= 3.6f / longest;
        }

        previewCar.transform.localRotation = Quaternion.Euler(0f, 210f, 0f);   // mulai dari sudut depan-samping
        SetLayer(previewCar);
    }

    void SetLayer(GameObject obj)
    {
        obj.layer = PreviewLayer;
        foreach (Transform child in obj.transform)
            SetLayer(child.gameObject);
    }

    // Urutan tampil: harga termurah dulu (nomor di katalog tetap, agar data pembelian tidak bergeser)
    // Pastikan daftar urutan selalu sesuai jumlah mobil (bukan kosong / sisa lama)
    void EnsureOrder()
    {
        if (order == null || order.Length != CarData.Count) BuildOrder();
    }

    void BuildOrder()
    {
        int count = CarData.Count;
        order = new int[count];
        for (int i = 0; i < count; i++) order[i] = i;
        System.Array.Sort(order, (a, b) =>
        {
            int c = CarData.Prices[a].CompareTo(CarData.Prices[b]);
            return c != 0 ? c : a.CompareTo(b);
        });
    }

    void OnEnable()
    {
        EnsureOrder();
        index = CarData.Selected;   // toko dibuka pada mobil yang sedang dipakai
        pos = Mathf.Max(0, System.Array.IndexOf(order, index));
        if (stage != null) stage.gameObject.SetActive(true);
        Refresh();
    }

    void OnDisable()
    {
        if (stage != null) stage.gameObject.SetActive(false);   // kamera preview berhenti saat toko ditutup
    }

    void Update()
    {
        FitContent();

        // Putar mobil dan panggung pelan-pelan
        if (previewCar != null)
            previewCar.transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
        if (turntable != null)
            turntable.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);

        // Nama mobil "meletup" sedikit saat berganti mobil
        if (namePop < 1f)
        {
            namePop = Mathf.Min(1f, namePop + Time.unscaledDeltaTime / 0.25f);
            float s = 1f + Mathf.Sin(namePop * Mathf.PI) * 0.12f;
            carNameText.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        AnimateFeedback();

        var kb = Keyboard.current;
        if (kb != null)
        {
            // Panah kiri/kanan untuk ganti mobil, Esc untuk kembali
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Change(-1);
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Change(1);
            if (kb.escapeKey.wasPressedThisFrame) { gameObject.SetActive(false); return; }

            // MODE DEMO: tekan F9 untuk +100 koin (untuk presentasi)
            if (kb.f9Key.wasPressedThisFrame)
            {
                CarData.Coins += 100;
                PlayerPrefs.Save();
                Refresh();
            }
        }
    }

    void Change(int direction)
    {
        EnsureOrder();
        int count = order.Length;
        if (count == 0) return;
        pos = (pos + direction + count) % count;   // berputar dari ujung ke ujung
        index = order[pos];
        namePop = 0f;
        Refresh();
    }

    void OnAction()
    {
        if (index == CarData.Selected) return;

        bool bought = false;
        if (!CarData.IsOwned(index))
        {
            int price = CarData.Prices[index];
            if (CarData.Coins < price)
            {
                // Koin kurang: tombol bergetar dan kotak koin berkedip merah
                shakeTime = 0.4f;
                flashTime = 0.6f;
                return;
            }
            CarData.Coins -= price;
            CarData.SetOwned(index);
            bought = true;
        }

        CarData.Selected = index;
        PlayerPrefs.Save();
        Refresh();
        ShowToast(bought ? "MOBIL BARU!" : "DIPAKAI!");
        if (bought) BurstConfetti();
    }

    // ---------- Efek umpan balik ----------

    void ShowToast(string text)
    {
        toastText.text = text;
        toastText.gameObject.SetActive(true);
        toastTime = 0f;
    }

    void BurstConfetti()
    {
        confettiTime = 0f;
        for (int i = 0; i < confetti.Length; i++)
        {
            confetti[i].gameObject.SetActive(true);
            confetti[i].anchoredPosition = new Vector2(Random.Range(-60f, 60f), 120f);
            confetti[i].localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            float angle = Random.Range(30f, 150f) * Mathf.Deg2Rad;
            confettiVelocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(500f, 1100f);
        }
    }

    void AnimateFeedback()
    {
        float dt = Time.unscaledDeltaTime;

        // Getar tombol
        var actionRect = (RectTransform)actionButton.transform;
        if (shakeTime > 0f)
        {
            shakeTime -= dt;
            actionRect.anchoredPosition = actionPos + new Vector2(Mathf.Sin(shakeTime * 70f) * 14f * (shakeTime / 0.4f), 0f);
        }
        else actionRect.anchoredPosition = actionPos;

        // Kotak koin berkedip merah
        if (coinChipImage != null)
        {
            flashTime = Mathf.Max(0f, flashTime - dt);
            bool red = flashTime > 0f && Mathf.Repeat(flashTime, 0.2f) > 0.1f;
            coinChipImage.color = red ? UiTheme.Red : new Color(UiTheme.Navy.r, UiTheme.Navy.g, UiTheme.Navy.b, 0.95f);
        }

        // "MOBIL BARU!" meletup lalu memudar
        if (toastText != null && toastText.gameObject.activeSelf)
        {
            toastTime += dt;
            float t = Mathf.Clamp01(toastTime / 0.3f);
            float pop = 1f + 2.2f * Mathf.Pow(t - 1f, 3f) + 1.2f * Mathf.Pow(t - 1f, 2f);
            float scale = Mathf.LerpUnclamped(0.4f, 1f, pop);
            toastText.rectTransform.localScale = new Vector3(scale, scale, 1f);
            toastText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f);
            float alpha = toastTime < 1.1f ? 1f : Mathf.Clamp01(1f - (toastTime - 1.1f) / 0.4f);
            toastText.alpha = alpha;
            if (toastTime > 1.5f) toastText.gameObject.SetActive(false);
        }

        // Confetti jatuh dengan gravitasi
        if (confettiTime < 3f)
        {
            confettiTime += dt;
            for (int i = 0; i < confetti.Length; i++)
            {
                confettiVelocity[i] += new Vector2(0f, -1600f) * dt;
                confetti[i].anchoredPosition += confettiVelocity[i] * dt;
                confetti[i].Rotate(0f, 0f, 400f * dt * (i % 2 == 0 ? 1f : -1f));
                if (confettiTime >= 3f) confetti[i].gameObject.SetActive(false);
            }
        }
    }

    void Refresh()
    {
        EnsureOrder();
        coinText.text = CarData.Coins.ToString();
        carNameText.text = CarData.Names[index];
        ShowPreview();

        // Label kelangkaan, warna sorotan, dan cincin panggung mengikuti kelangkaan
        bool owned = CarData.IsOwned(index);
        if (rarityPill != null)
        {
            int r = CarData.Rarity[index];
            ColorUtility.TryParseHtmlString(CarData.RarityColors[r], out Color rc);
            rarityPill.color = rc * 0.85f + new Color(0f, 0f, 0f, 0.15f);
            rarityLabel.text = CarData.RarityNames[r];
            Color light = Color.Lerp(rc, Color.white, 0.25f);
            spotlight.color = new Color(light.r, light.g, light.b, owned ? 0.45f : 0.18f);
            if (rimMaterial != null) rimMaterial.color = owned ? rc : rc * 0.5f;
        }

        // Mobil terkunci: gelap + gembok
        if (previewImage != null) previewImage.color = owned ? Color.white : new Color(0.22f, 0.24f, 0.32f);
        if (padlock != null) padlock.gameObject.SetActive(!owned);
        if (counterText != null) counterText.text = $"{pos + 1} / {CarData.Count}";

        // Titik penanda: kuning besar = sedang dilihat, putih = dimiliki, gelap = terkunci
        if (dots != null)
        {
            for (int i = 0; i < dots.Length; i++)
            {
                if (i >= order.Length) { dots[i].gameObject.SetActive(false); continue; }   // pengaman
                bool current = i == pos;
                dots[i].color = current ? UiTheme.Yellow
                              : CarData.IsOwned(order[i]) ? Color.white
                              : new Color(0.3f, 0.36f, 0.5f);
                float size = current ? 26f : 18f;
                dots[i].rectTransform.sizeDelta = new Vector2(size, size);
            }
        }

        bool showCoin = false;
        if (index == CarData.Selected)
        {
            carInfoText.text = "Mobil ini sedang kamu pakai";
            actionLabel.text = "SEDANG DIPAKAI";
            actionButton.interactable = false;
            UiTheme.StyleButton(actionButton, UiTheme.Kind.Dark, 38f);
        }
        else if (CarData.IsOwned(index))
        {
            carInfoText.text = "Sudah kamu miliki";
            actionLabel.text = "PAKAI";
            actionButton.interactable = true;
            UiTheme.StyleButton(actionButton, UiTheme.Kind.Secondary, 44f);
        }
        else
        {
            int price = CarData.Prices[index];
            bool canBuy = CarData.Coins >= price;
            carInfoText.text = canBuy ? "Terkunci. Beli untuk memakainya!" : $"Koinmu kurang {price - CarData.Coins}. Kumpulkan dengan menjawab benar!";
            actionLabel.text = $"BELI   {price}";
            actionButton.interactable = true;   // tetap bisa diklik: kalau koin kurang, tombol bergetar
            UiTheme.StyleButton(actionButton, canBuy ? UiTheme.Kind.Primary : UiTheme.Kind.Dark, 44f);
            showCoin = true;
        }

        if (actionCoin != null)
        {
            actionCoin.gameObject.SetActive(showCoin);
            bool affordable = CarData.Coins >= CarData.Prices[index];
            actionCoin.color = affordable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }
    }
}
