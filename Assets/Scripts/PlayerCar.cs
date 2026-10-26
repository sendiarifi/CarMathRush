using UnityEngine;
using UnityEngine.InputSystem;

// Tombol yang dipakai mobil. SOLO = Both (A/D dan panah). DUO: Pemain 1 = AD, Pemain 2 = Arrows.
public enum CarControls { Both, AD, Arrows }

// Pasang script ini ke objek mobil (PlayerCar)
public class PlayerCar : MonoBehaviour
{
    public float forwardSpeed = 10f;     // kecepatan maju
    public float laneDistance = 3f;      // jarak antar jalur (x = -3, 0, 3)
    public float laneChangeSpeed = 15f;  // seberapa cepat pindah jalur

    [Header("Kontrol (biarkan Both untuk mode SOLO)")]
    public CarControls controls = CarControls.Both;

    // Titik tengah jalan. SOLO selalu 0. Mobil Pemain 2 diberi nilai lain oleh DuoManager.
    [System.NonSerialized] public float centerX = 0f;

    private int currentLane = 1;         // 0 = kiri, 1 = tengah, 2 = kanan
    private float baseY;                 // tinggi mobil di atas aspal (diambil dari posisi awal)

    void Awake()
    {
        baseY = transform.position.y - TrackShape.Height(transform.position.z);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            bool useAD = controls != CarControls.Arrows;
            bool useArrows = controls != CarControls.AD;

            bool left = (useAD && kb.aKey.wasPressedThisFrame) || (useArrows && kb.leftArrowKey.wasPressedThisFrame);
            bool right = (useAD && kb.dKey.wasPressedThisFrame) || (useArrows && kb.rightArrowKey.wasPressedThisFrame);

            if (left) currentLane = Mathf.Max(0, currentLane - 1);
            if (right) currentLane = Mathf.Min(2, currentLane + 1);
        }

        Vector3 pos = transform.position;

        // Maju otomatis
        pos.z += forwardSpeed * Time.deltaTime;

        // Bergerak halus ke jalur tujuan
        float targetX = centerX + (currentLane - 1) * laneDistance;
        pos.x = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * Time.deltaTime);

        // Ikut naik-turun bukit dan miring sesuai tanjakan
        pos.y = baseY + TrackShape.Height(pos.z);
        transform.position = pos;
        transform.rotation = Quaternion.Euler(TrackShape.PitchDegrees(pos.z), 0f, 0f);
    }
}
