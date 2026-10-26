using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Pasang ke objek kosong bernama "GameManager"
public class GameManager : MonoBehaviour
{
    [Header("Referensi (drag dari Hierarchy)")]
    public PlayerCar player;
    public Transform road;
    public TMP_Text questionText;
    public TMP_Text scoreText;
    public TMP_Text gameOverText;   // dipakai juga untuk tulisan PAUSE

    [Header("Pengaturan")]
    public float spawnDistance = 40f;   // jarak minimum kubus di depan mobil
    public int maxLives = 3;
    public float maxSpeed = 25f;

    [Header("Tantangan")]
    public float speedIncrease = 0.3f;            // tambahan kecepatan tiap jawaban benar
    public float accelerationPerSecond = 0.1f;    // makin lama makin cepat
    public float wrongSlowdown = 1.5f;            // jawaban salah membuat mobil melambat
    public float minThinkTime = 2.5f;             // waktu berpikir minimum (detik)
    [Range(0f, 1f)] public float goldenChance = 0.15f;   // peluang soal emas

    [Header("Mode DUO VS (jawaban lebih terasa pengaruhnya ke jarak)")]
    public float duoSpeedIncrease = 1f;
    public float duoWrongSlowdown = 2f;

    [Header("Uji DUO langsung dari SampleScene (matikan lagi setelah uji)")]
    public bool testDuoInEditor = false;

    [Header("Suara (semua boleh dikosongkan)")]
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip goldenSound;
    public AudioClip levelUpSound;
    public AudioClip gameOverSound;
    public AudioClip engineSound;     // suara mesin, diputar terus, nadanya naik sesuai kecepatan

    [Header("Scene")]
    public string menuSceneName = "MainMenu";

    // Diisi otomatis oleh DuoManager. Kosong = mode SOLO.
    [System.NonSerialized] public DuoManager duo;
    [System.NonSerialized] public string nameOverride;

    private AudioSource sfxSource, engineSource;

    private int score, lives, combo, correctAnswer, coinsThisRun, level;
    private bool gameOver, answered, paused, isGolden;
    private float rowZ, startSpeed, startZ;
    private string playerName, currentQuestion;
    private readonly List<GameObject> currentRow = new List<GameObject>();
    private Coroutine feedbackRoutine;

    // Dibaca oleh GameHUD untuk menampilkan ikon
    public int Score => score;
    public int Lives => lives;
    public int Combo => combo;
    public string PlayerName => playerName;
    public int SpeedKmh => Mathf.RoundToInt(player.forwardSpeed * 10f);
    public bool IsGameOver => gameOver;
    public bool IsPaused => paused;
    public int CoinsThisRun => coinsThisRun;
    public int HighScore { get; private set; }
    public bool NewRecord { get; private set; }

    // Khusus DUO
    public bool IsDuo => duo != null;
    public float Distance => player.transform.position.z - startZ;
    public string MatchResult { get; private set; }   // "MENANG", "KALAH", atau "SERI"

    // Dipanggil oleh tombol di kartu hasil
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    private static readonly Color NormalCubeColor = new Color(0.35f, 0.75f, 1f);
    private static readonly Color GoldCubeColor = new Color(1f, 0.8f, 0.1f);
    private static readonly Color GoldTextColor = new Color(1f, 0.85f, 0.2f);

    void Awake()
    {
#if UNITY_EDITOR
        if (testDuoInEditor) GameMode.Duo = true;
#endif
        // Mode DUO: GameManager pertama membuat DuoManager, yang lalu menggandakan
        // mobil, jalan, kamera, HUD, dan GameManager untuk Pemain 2.
        // (GameManager kedua lahir saat DuoManager.Building = true, jadi tidak membuat lagi)
        if (GameMode.Duo && !DuoManager.Building)
            DuoManager.Create(this);
    }

    void Start()
    {
        Time.timeScale = 1f;   // pastikan game tidak terbawa kondisi pause
        lives = maxLives;
        playerName = !string.IsNullOrEmpty(nameOverride) ? nameOverride : PlayerPrefs.GetString("PlayerName", "Pemain");
        startSpeed = player.forwardSpeed;
        startZ = player.transform.position.z;

        // Mobil perlu Rigidbody kinematic agar tabrakan trigger terdeteksi
        if (player.GetComponent<Rigidbody>() == null)
        {
            var rb = player.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        // Sumber suara dibuat otomatis
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume = SoundSettings.SfxFactor;   // dari menu Pengaturan
        if (engineSound != null)
        {
            engineSource = player.gameObject.AddComponent<AudioSource>();
            engineSource.clip = engineSound;
            engineSource.loop = true;
            float engineVolume = IsDuo ? 0.18f : 0.3f;   // dua mesin sekaligus, jadi lebih pelan
            engineSource.volume = engineVolume * SoundSettings.SfxFactor;
            engineSource.Play();
        }

        // Musik latar (objek "Music"): cukup diatur sekali, oleh GameManager pertama saja
        if (!IsDuo || duo.IsPlayerOne(this))
            ApplyMusicVolume();

        gameOverText.gameObject.SetActive(false);
        questionText.gameObject.SetActive(true);
        NextQuestion();
        UpdateUI();
    }

    void ApplyMusicVolume()
    {
        var musicObj = GameObject.Find("Music");
        var music = musicObj != null ? musicObj.GetComponent<AudioSource>() : null;
        if (music == null) return;
        music.volume *= SoundSettings.MusicFactor;
        if (SoundSettings.MusicFactor <= 0f) music.Stop();   // dibisukan: hentikan sekalian
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
    }

    void Update()
    {
        // Jalan ikut maju agar terlihat tanpa ujung
        if (road != null)
        {
            Vector3 p = road.position;
            p.z = player.transform.position.z + 200f;
            road.position = p;
        }

        // Tombol R, M, P, Esc. Di mode DUO semua tombol ini diurus DuoManager.
        if (!IsDuo)
        {
            var kb = Keyboard.current;

            if (gameOver)
            {
                if (kb != null && kb.rKey.wasPressedThisFrame)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                if (kb != null && kb.mKey.wasPressedThisFrame)
                    SceneManager.LoadScene(menuSceneName);
                return;
            }

            // Saat pause, tekan M untuk kembali ke menu
            if (paused && kb != null && kb.mKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(menuSceneName);
                return;
            }

            // Tombol pause: P atau Esc
            if (kb != null && (kb.pKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                TogglePause();
        }

        if (gameOver || paused) return;

        // Makin lama makin cepat
        player.forwardSpeed = Mathf.Min(maxSpeed, player.forwardSpeed + accelerationPerSecond * Time.deltaTime);

        // Nada mesin naik seiring kecepatan
        if (engineSource != null)
        {
            float t = Mathf.InverseLerp(startSpeed, maxSpeed, player.forwardSpeed);
            engineSource.pitch = Mathf.Lerp(0.8f, 1.6f, t);
        }

        // Mobil melewati kubus tanpa menabrak: aman, tapi combo direset
        if (!answered && player.transform.position.z > rowZ + 2f)
        {
            combo = 0;
            NextQuestion();
        }

        UpdateUI();   // speedometer diperbarui terus
    }

    public void TogglePause()
    {
        // Di mode DUO pause berlaku untuk kedua pemain sekaligus
        if (IsDuo) { duo.TogglePause(); return; }

        SetPaused(!paused);
        Time.timeScale = paused ? 0f : 1f;
    }

    // Dipanggil oleh TogglePause (SOLO) atau DuoManager (DUO)
    public void SetPaused(bool value)
    {
        if (gameOver) return;
        paused = value;

        player.enabled = !paused;   // mobil tidak bisa pindah jalur saat pause

        // Sembunyikan soal dan kubus agar pemain tidak bisa curang menghitung saat pause
        questionText.gameObject.SetActive(!paused);
        foreach (var c in currentRow)
            if (c != null) c.SetActive(!paused);

        if (engineSource != null)
        {
            if (paused) engineSource.Pause();
            else engineSource.UnPause();
        }

        gameOverText.gameObject.SetActive(paused);
        if (paused)
            gameOverText.text = "PAUSE\nP = lanjut    M = menu";
    }

    void NextQuestion()
    {
        ClearRow();
        isGolden = score > 0 && Random.value < goldenChance;   // soal pertama tidak pernah emas
        GenerateQuestion();
        SpawnRow();
        answered = false;
    }

    void GenerateQuestion()
    {
        int max = 10 + level * 10;          // makin tinggi level, angka makin besar
        int op = level >= 2 ? Random.Range(0, 3) : Random.Range(0, 2);

        int a, b;
        string sym;
        if (op == 0)
        {
            a = Random.Range(1, max); b = Random.Range(1, max);
            correctAnswer = a + b; sym = "+";
        }
        else if (op == 1)
        {
            a = Random.Range(1, max); b = Random.Range(1, a + 1);   // hasil tidak negatif
            correctAnswer = a - b; sym = "-";
        }
        else
        {
            a = Random.Range(2, 10); b = Random.Range(2, 10);
            correctAnswer = a * b; sym = "x";
        }

        currentQuestion = (isGolden ? "<size=36>SOAL EMAS x2</size>\n" : "") + $"{a} {sym} {b} = ?";
        questionText.text = currentQuestion;
        questionText.color = BaseQuestionColor();
    }

    Color BaseQuestionColor()
    {
        return isGolden ? GoldTextColor : Color.white;
    }

    List<int> MakeOptions()
    {
        // 1 jawaban benar + 2 jawaban salah yang semuanya berbeda
        var opts = new List<int> { correctAnswer };
        int guard = 0;
        while (opts.Count < 3 && guard < 100)
        {
            guard++;
            int offset = Random.Range(1, 6) * (Random.value < 0.5f ? -1 : 1);
            int wrong = correctAnswer + offset;
            if (wrong >= 0 && !opts.Contains(wrong)) opts.Add(wrong);
        }
        int extra = 1;
        while (opts.Count < 3)
        {
            if (!opts.Contains(correctAnswer + extra)) opts.Add(correctAnswer + extra);
            extra++;
        }

        // Acak posisi jawaban
        for (int i = 0; i < opts.Count; i++)
        {
            int j = Random.Range(i, opts.Count);
            (opts[i], opts[j]) = (opts[j], opts[i]);
        }
        return opts;
    }

    void SpawnRow()
    {
        // Makin cepat mobil, makin jauh kubus muncul, agar selalu ada waktu berpikir
        float distance = Mathf.Max(spawnDistance, player.forwardSpeed * minThinkTime);
        rowZ = player.transform.position.z + distance;
        List<int> opts = MakeOptions();

        for (int lane = 0; lane < 3; lane++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "AnswerCube_" + opts[lane];
            cube.transform.position = new Vector3(player.centerX + (lane - 1) * player.laneDistance, 1f + TrackShape.Height(rowZ), rowZ);
            cube.transform.localScale = new Vector3(2f, 2f, 1f);
            cube.GetComponent<Renderer>().sharedMaterial = MaterialHelper.Shared(isGolden ? GoldCubeColor : NormalCubeColor);
            cube.GetComponent<Collider>().isTrigger = true;

            var rb = cube.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            var ac = cube.AddComponent<AnswerCube>();
            ac.value = opts[lane];
            ac.isCorrect = opts[lane] == correctAnswer;
            ac.manager = this;

            // Angka di depan kubus
            var label = new GameObject("Label");
            label.transform.SetParent(cube.transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.51f);
            var tmp = label.AddComponent<TextMeshPro>();
            tmp.text = opts[lane].ToString();
            tmp.fontSize = 8;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            tmp.rectTransform.sizeDelta = new Vector2(2f, 2f);

            currentRow.Add(cube);
        }
    }

    void ClearRow()
    {
        foreach (var c in currentRow)
            if (c != null) Destroy(c);
        currentRow.Clear();
    }

    public void OnAnswerHit(AnswerCube cube)
    {
        if (answered || gameOver || paused) return;
        answered = true;

        string banner = null;
        AudioClip sound;

        if (cube.isCorrect)
        {
            sound = (isGolden && goldenSound != null) ? goldenSound : correctSound;
            combo++;
            int points = 10 + (combo - 1) * 2;   // bonus combo
            int coins = 1;

            if (isGolden)
            {
                points *= 2;
                coins = 3;
                banner = $"EMAS! +{points} poin";
            }
            if (combo % 5 == 0)
            {
                coins += 2;
                banner = IsDuo ? $"COMBO x{combo}!" : $"COMBO x{combo}! +2 koin bonus";
            }
            if (combo % 10 == 0 && lives < maxLives)
            {
                lives++;
                banner = $"COMBO x{combo}! +1 NYAWA";
            }

            score += points;
            if (!IsDuo)   // mode DUO tidak memberi koin (dompet koin hanya milik satu pemain)
            {
                CarData.Coins += coins;
                coinsThisRun += coins;
            }
            float boost = IsDuo ? duoSpeedIncrease : speedIncrease;
            player.forwardSpeed = Mathf.Min(maxSpeed, player.forwardSpeed + boost);

            int newLevel = score / 50;
            if (newLevel > level)
            {
                level = newLevel;
                banner = $"LEVEL {level + 1}! Soal makin sulit";
                if (levelUpSound != null) sound = levelUpSound;
            }
        }
        else
        {
            sound = wrongSound;
            combo = 0;
            lives--;
            float slow = IsDuo ? duoWrongSlowdown : wrongSlowdown;
            player.forwardSpeed = Mathf.Max(startSpeed, player.forwardSpeed - slow);
            if (lives <= 0)
            {
                GameOver();
                UpdateUI();
                return;
            }
        }

        PlaySound(sound);
        NextQuestion();
        UpdateUI();
        ShowFeedback(cube.isCorrect ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f), banner);
    }

    // Soal berikutnya berkedip hijau (benar) / merah (salah), plus pesan bonus kalau ada
    void ShowFeedback(Color color, string banner)
    {
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(FeedbackRoutine(color, banner));
    }

    IEnumerator FeedbackRoutine(Color color, string banner)
    {
        questionText.color = color;
        if (banner != null)
            questionText.text = $"<size=40>{banner}</size>\n{currentQuestion}";

        yield return new WaitForSeconds(banner != null ? 1.2f : 0.6f);

        questionText.color = BaseQuestionColor();
        questionText.text = currentQuestion;
    }

    // Menghentikan mobil, soal, dan kubus
    void StopRun(AudioClip sound)
    {
        gameOver = true;
        paused = false;
        player.enabled = false;   // mobil berhenti
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        if (engineSource != null) engineSource.Stop();
        PlaySound(sound);
        ClearRow();
        questionText.gameObject.SetActive(false);   // soal disembunyikan
    }

    void GameOver()
    {
        StopRun(gameOverSound);

        // Mode DUO: nyawa habis = kalah. DuoManager yang menentukan hasil kedua pemain.
        if (IsDuo)
        {
            duo.OnPlayerOut(this);
            return;
        }

        // Simpan high score kalau skor sekarang lebih tinggi
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        bool newRecord = score > highScore;
        if (newRecord)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.SetString("HighScoreName", playerName);
        }
        PlayerPrefs.Save();   // simpan high score dan koin
        HighScore = highScore;
        NewRecord = newRecord;

        gameOverText.gameObject.SetActive(true);
        gameOverText.text = "GAME OVER\n"
            + $"Skor: {score}" + (newRecord ? "  REKOR BARU!" : "") + "\n"
            + $"High Score: {highScore}\n"
            + $"+{coinsThisRun} koin\n"
            + "R = main lagi    M = menu";
    }

    // Dipanggil DuoManager saat pertandingan selesai (waktu habis atau lawan kehabisan nyawa)
    public void EndMatch(string result)
    {
        MatchResult = result;
        if (!gameOver)
            StopRun(result == "MENANG" ? levelUpSound : gameOverSound);
    }

    void UpdateUI()
    {
        int kmh = Mathf.RoundToInt(player.forwardSpeed * 10f);
        scoreText.text = $"{playerName}\nSkor: {score}\nNyawa: {lives}\nCombo: {combo}\n"
                       + $"Koin: {CarData.Coins}\n{kmh} km/j";
    }
}
