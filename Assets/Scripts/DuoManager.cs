using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Menyimpan mode permainan yang dipilih di menu. Tidak perlu dipasang ke objek mana pun.
public static class GameMode
{
    public static bool Duo;

    // Setiap kali game dijalankan, mulai dari mode SOLO
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnStart() { Duo = false; }
}

// Mode DUO VS (satu keyboard, layar dibagi dua).
// TIDAK perlu dipasang ke objek mana pun: dibuat otomatis oleh GameManager saat mode DUO dipilih.
// Script ini menggandakan mobil, jalan, pemandangan, kamera, HUD, dan GameManager untuk Pemain 2,
// lalu mengatur hitung mundur, timer 90 detik, pause, dan siapa yang menang.
[DefaultExecutionOrder(100)]
public class DuoManager : MonoBehaviour
{
    public static DuoManager Instance { get; private set; }

    // Dunia Pemain 2 diletakkan jauh di samping agar kedua jalan tidak saling terlihat
    public const float WorldOffset = 2000f;

    public float matchDuration = 90f;
    public float countdownDuration = 3f;
    public float duoFieldOfView = 70f;   // layar setengah lebih sempit, jadi pandangan dibuat lebih lebar

    private enum Phase { Countdown, Playing, Paused, Ended }
    private Phase phase = Phase.Countdown;

    private GameManager p1, p2;
    private float timeLeft, countdownLeft, goTimer;
    private TMP_Text timerText, leadText, countdownText;
    private Image timerChip;

    // true selama proses menggandakan, agar GameManager kedua tidak membuat DuoManager lagi
    public static bool Building { get; private set; }

    public static void Create(GameManager first)
    {
        var go = new GameObject("DuoManager");
        var dm = go.AddComponent<DuoManager>();
        Instance = dm;
        Building = true;
        try { dm.Build(first); }
        finally { Building = false; }
    }

    public bool IsPlayerOne(GameManager gm) => gm == p1;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    // ---------- Menggandakan perangkat permainan ----------

    void Build(GameManager gm1)
    {
        p1 = gm1;
        Vector3 shift = new Vector3(WorldOffset, 0f, 0f);

        // Nama di HUD
        string typed = PlayerPrefs.GetString("PlayerName", "").Trim();
        string name1 = typed.Length > 0 ? typed : "PEMAIN 1";

        // --- Mobil ---
        PlayerCar car1 = gm1.player;
        car1.controls = CarControls.AD;
        car1.enabled = false;   // diam selama hitung mundur

        var car2Obj = Instantiate(car1.gameObject, car1.transform.position + shift, car1.transform.rotation);
        car2Obj.name = "PlayerCar_P2";
        PlayerCar car2 = car2Obj.GetComponent<PlayerCar>();
        car2.controls = CarControls.Arrows;
        car2.centerX = WorldOffset;
        car2.enabled = false;

        // Pemain 2 memakai mobil yang berbeda: Taxi, atau Polisi kalau Pemain 1 sedang memakai Taxi
        var selector2 = car2Obj.GetComponent<CarSelector>();
        if (selector2 != null) selector2.overrideIndex = Mathf.Min(CarData.Selected == 1 ? 2 : 1, CarData.Count - 1);

        // --- Jalan ---
        Transform road2 = null;
        if (gm1.road != null)
        {
            road2 = Instantiate(gm1.road.gameObject, gm1.road.position + shift, gm1.road.rotation).transform;
            road2.name = "Road_P2";
            var track1 = gm1.road.GetComponent<RaceTrack>();
            if (track1 != null) track1.target = car1.transform;
            var track2 = road2.GetComponent<RaceTrack>();
            if (track2 != null) track2.target = car2.transform;
        }

        // --- Pohon & gedung ---
        var scenery1 = FindAnyObjectByType<Scenery>();
        if (scenery1 != null)
        {
            scenery1.target = car1.transform;
            var scenery2 = Instantiate(scenery1.gameObject);
            scenery2.name = "Scenery_P2";
            scenery2.GetComponent<Scenery>().target = car2.transform;
        }

        // --- Kamera: kiri untuk Pemain 1, kanan untuk Pemain 2 ---
        var follow1 = FindAnyObjectByType<CameraFollow>();
        if (follow1 != null)
        {
            follow1.target = car1.transform;
            var camObj2 = Instantiate(follow1.gameObject);
            camObj2.name = "Camera_P2";
            camObj2.tag = "Untagged";
            camObj2.GetComponent<CameraFollow>().target = car2.transform;

            // Hanya boleh ada satu "telinga" suara di scene
            var listener = camObj2.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;

            SetupCamera(follow1.GetComponent<Camera>(), new Rect(0f, 0f, 0.5f, 1f));
            SetupCamera(camObj2.GetComponent<Camera>(), new Rect(0.5f, 0f, 0.5f, 1f));
        }

        // --- UI: Canvas dibagi menjadi panel kiri dan kanan ---
        Canvas canvas = gm1.scoreText.canvas.rootCanvas;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        RectTransform panel1 = MakeHalf("P1Panel", canvas.transform, 0f);
        RectTransform panel2 = MakeHalf("P2Panel", canvas.transform, 0.5f);

        gm1.questionText.transform.SetParent(panel1, false);
        gm1.scoreText.transform.SetParent(panel1, false);
        gm1.gameOverText.transform.SetParent(panel1, false);
        LimitQuestionWidth(gm1.questionText);

        // --- GameManager Pemain 2 ---
        var gm2 = Instantiate(gm1.gameObject).GetComponent<GameManager>();
        gm2.name = "GameManager_P2";
        gm2.player = car2;
        gm2.road = road2;
        gm2.questionText = Instantiate(gm1.questionText.gameObject, panel2, false).GetComponent<TMP_Text>();
        gm2.scoreText = Instantiate(gm1.scoreText.gameObject, panel2, false).GetComponent<TMP_Text>();
        gm2.gameOverText = Instantiate(gm1.gameOverText.gameObject, panel2, false).GetComponent<TMP_Text>();
        p2 = gm2;

        gm1.duo = this;
        gm2.duo = this;
        gm1.nameOverride = name1 + "  [A/D]";
        gm2.nameOverride = "PEMAIN 2  [PANAH]";

        // --- HUD ---
        var hud1 = FindAnyObjectByType<GameHUD>();
        if (hud1 != null)
        {
            hud1.manager = gm1;
            var hud2 = Instantiate(hud1.gameObject);
            hud2.name = "HUD_P2";
            hud2.GetComponent<GameHUD>().manager = gm2;
        }

        BuildMatchUI(canvas.transform);

        timeLeft = matchDuration;
        countdownLeft = countdownDuration;
    }

    void SetupCamera(Camera cam, Rect rect)
    {
        if (cam == null) return;
        cam.rect = rect;
        cam.fieldOfView = Mathf.Max(cam.fieldOfView, duoFieldOfView);
    }

    RectTransform MakeHalf(string objName, Transform canvas, float xMin)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(xMin, 0f);
        rect.anchorMax = new Vector2(xMin + 0.5f, 1f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    void LimitQuestionWidth(TMP_Text text)
    {
        // Setengah layar lebih sempit: soal jangan sampai menabrak nama & nyawa di kanan atas
        var rect = text.rectTransform;
        if (Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x) && rect.sizeDelta.x > 480f)
            rect.sizeDelta = new Vector2(480f, rect.sizeDelta.y);
    }

    // ---------- Timer, garis pemisah, hitung mundur ----------

    void BuildMatchUI(Transform canvas)
    {
        // Garis pemisah di tengah layar
        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(canvas, false);
        var dRect = (RectTransform)divider.transform;
        dRect.anchorMin = new Vector2(0.5f, 0f);
        dRect.anchorMax = new Vector2(0.5f, 1f);
        dRect.sizeDelta = new Vector2(8f, 0f);
        var dImage = divider.GetComponent<Image>();
        dImage.color = new Color(0f, 0f, 0f, 0.85f);
        dImage.raycastTarget = false;

        // Kotak timer di tengah bawah (di atas speedometer)
        var chip = new GameObject("TimerChip", typeof(RectTransform), typeof(Image));
        chip.transform.SetParent(canvas, false);
        var cRect = (RectTransform)chip.transform;
        cRect.anchorMin = cRect.anchorMax = cRect.pivot = new Vector2(0.5f, 0f);
        cRect.anchoredPosition = new Vector2(0f, 130f);
        cRect.sizeDelta = new Vector2(230f, 96f);
        timerChip = chip.GetComponent<Image>();
        timerChip.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        timerChip.raycastTarget = false;

        timerText = MakeText("TimerText", cRect, 64);
        Stretch(timerText.rectTransform);

        // Siapa yang unggul, di atas timer
        leadText = MakeText("LeadText", canvas, 30);
        var lRect = leadText.rectTransform;
        lRect.anchorMin = lRect.anchorMax = lRect.pivot = new Vector2(0.5f, 0f);
        lRect.anchoredPosition = new Vector2(0f, 232f);
        lRect.sizeDelta = new Vector2(420f, 44f);

        // Angka hitung mundur besar di tengah layar
        countdownText = MakeText("CountdownText", canvas, 220);
        var kRect = countdownText.rectTransform;
        kRect.anchorMin = kRect.anchorMax = kRect.pivot = new Vector2(0.5f, 0.5f);
        kRect.sizeDelta = new Vector2(900f, 300f);
        countdownText.outlineWidth = 0.25f;
        countdownText.color = new Color(1f, 0.82f, 0.2f);
    }

    TMP_Text MakeText(string objName, Transform parent, float size)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.outlineWidth = 0.15f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        return text;
    }

    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    // ---------- Jalannya pertandingan ----------

    void Start()
    {
        Time.timeScale = 0f;   // semua diam selama hitung mundur
    }

    void Update()
    {
        var kb = Keyboard.current;
        bool pausePressed = kb != null && (kb.pKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame);

        switch (phase)
        {
            case Phase.Countdown:
                Time.timeScale = 0f;
                countdownLeft -= Mathf.Min(Time.unscaledDeltaTime, 0.1f);   // jeda loading tidak memakan hitungan
                countdownText.text = Mathf.CeilToInt(Mathf.Max(countdownLeft, 0.01f)).ToString();
                if (countdownLeft <= 0f) StartRace();
                break;

            case Phase.Playing:
                if (pausePressed) { SetPause(true); break; }
                timeLeft -= Time.deltaTime;
                if (timeLeft <= 0f)
                {
                    timeLeft = 0f;
                    FinishByDistance();
                }
                break;

            case Phase.Paused:
                if (pausePressed) SetPause(false);
                else if (kb != null && kb.mKey.wasPressedThisFrame) p1.GoToMenu();
                break;

            case Phase.Ended:
                if (kb != null && kb.rKey.wasPressedThisFrame) p1.Restart();
                else if (kb != null && kb.mKey.wasPressedThisFrame) p1.GoToMenu();
                break;
        }

        // Tulisan "GO!" menghilang sendiri
        if (goTimer > 0f)
        {
            goTimer -= Time.unscaledDeltaTime;
            if (goTimer <= 0f) countdownText.text = "";
        }

        UpdateMatchUI();
    }

    void StartRace()
    {
        phase = Phase.Playing;
        Time.timeScale = 1f;
        p1.player.enabled = true;
        p2.player.enabled = true;
        countdownText.text = "GO!";
        goTimer = 0.8f;
    }

    // Dipanggil oleh GameManager (tombol LANJUT di kartu PAUSE)
    public void TogglePause()
    {
        if (phase == Phase.Playing) SetPause(true);
        else if (phase == Phase.Paused) SetPause(false);
    }

    void SetPause(bool value)
    {
        phase = value ? Phase.Paused : Phase.Playing;
        Time.timeScale = value ? 0f : 1f;
        p1.SetPaused(value);
        p2.SetPaused(value);
    }

    // Waktu habis: yang paling jauh menang
    void FinishByDistance()
    {
        phase = Phase.Ended;
        float d1 = p1.Distance, d2 = p2.Distance;
        if (Mathf.Abs(d1 - d2) < 0.5f)
        {
            p1.EndMatch("SERI");
            p2.EndMatch("SERI");
        }
        else
        {
            p1.EndMatch(d1 > d2 ? "MENANG" : "KALAH");
            p2.EndMatch(d2 > d1 ? "MENANG" : "KALAH");
        }
    }

    // Dipanggil GameManager saat seorang pemain kehabisan nyawa
    public void OnPlayerOut(GameManager loser)
    {
        if (phase == Phase.Ended) return;
        phase = Phase.Ended;
        Time.timeScale = 1f;
        GameManager winner = loser == p1 ? p2 : p1;
        loser.EndMatch("KALAH");
        winner.EndMatch("MENANG");
    }

    void UpdateMatchUI()
    {
        int seconds = Mathf.CeilToInt(timeLeft);
        timerText.text = $"{seconds / 60}:{seconds % 60:00}";
        bool hurry = phase == Phase.Playing && timeLeft <= 10f;
        timerText.color = hurry ? new Color(1f, 0.35f, 0.3f) : Color.white;
        if (hurry) timerChip.rectTransform.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.08f);
        else timerChip.rectTransform.localScale = Vector3.one;

        if (phase == Phase.Countdown) { leadText.text = ""; return; }
        float gap = p1.Distance - p2.Distance;
        int meters = Mathf.Abs(Mathf.RoundToInt(gap));
        leadText.text = meters < 1 ? "IMBANG"
                      : gap > 0f ? $"P1 UNGGUL {meters} m"
                      : $"P2 UNGGUL {meters} m";
    }
}
