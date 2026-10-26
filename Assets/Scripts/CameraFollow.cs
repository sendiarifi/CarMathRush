using UnityEngine;

// Pasang script ini ke Main Camera, lalu drag objek mobil ke kolom Target
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 4f, -7f);

    private Transform cachedTarget;
    private PlayerCar car;
    private Quaternion baseRotation;
    private bool hasBase;
    private float pitch, smoothY;

    void LateUpdate()
    {
        if (target == null) return;

        if (target != cachedTarget)
        {
            cachedTarget = target;
            car = target.GetComponent<PlayerCar>();
        }
        float centerX = car != null ? car.centerX : 0f;   // SOLO: 0, Pemain 2: jalan miliknya

        if (!hasBase) { baseRotation = transform.rotation; hasBase = true; smoothY = target.position.y; }

        // Tinggi kamera mengikuti mobil dengan halus (tidak kaku saat melewati puncak bukit)
        smoothY = Mathf.Lerp(smoothY, target.position.y, 1f - Mathf.Exp(-8f * Time.deltaTime));

        // Kamera hanya ikut maju (z), tidak ikut goyang kiri-kanan
        transform.position = new Vector3(centerX + offset.x, smoothY + offset.y, target.position.z + offset.z);

        // Menunduk/mendongak mengikuti jalan di depan (setengah dari kemiringan jalan)
        float wanted = TrackShape.PitchDegrees(target.position.z + 15f) * 0.6f;
        pitch = Mathf.Lerp(pitch, wanted, 1f - Mathf.Exp(-4f * Time.deltaTime));
        transform.rotation = Quaternion.Euler(pitch, 0f, 0f) * baseRotation;
    }
}
