using UnityEngine;

// Pasang ke objek kosong "MenuBackground" di scene MainMenu.
// Isi Car Models dengan 6 mobil, urutan sama seperti di CarSelector.
// Script membuat sendiri: jalan balap, pohon & gedung, mobil yang melaju, dan kamera sinematik.
public class MenuBackground : MonoBehaviour
{
    public GameObject[] carModels;

    [Header("Pengaturan")]
    public float carSpeed = 12f;
    public float orbitSpeed = 8f;        // kecepatan kamera berputar (derajat per detik)
    public float cameraDistance = 7f;
    public float cameraHeight = 2.5f;
    public float sideShift = 3f;          // geser agar mobil tampil di kanan, tidak tertutup tombol

    private Transform car;
    private Transform cam;
    private float angle = 40f;

    void Start()
    {
        // Mobil yang melaju sendiri (mobil yang sedang dipilih pemain)
        car = new GameObject("MenuCar").transform;
        car.position = Vector3.zero;
        {
            GameObject model = CarData.Model(CarData.Selected, carModels);   // dari Katalog Mobil
            if (model != null)
            {
                var m = Instantiate(model, car);
                m.transform.localPosition = Vector3.zero;
                m.transform.localRotation = Quaternion.identity;
            }
        }

        // Jalan balap (memakai script RaceTrack yang sama dengan game)
        var road = GameObject.CreatePrimitive(PrimitiveType.Plane);
        road.name = "MenuRoad";
        Destroy(road.GetComponent<Collider>());
        road.transform.localScale = new Vector3(1f, 1f, 50f);
        var track = road.AddComponent<RaceTrack>();
        track.target = car;
        track.followTarget = true;

        // Pohon dan gedung (memakai script Scenery yang sama dengan game)
        var scenery = new GameObject("MenuScenery").AddComponent<Scenery>();
        scenery.target = car;

        if (Camera.main != null) cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (car == null) return;

        // Mobil menu ikut naik-turun bukit
        Vector3 p = car.position + Vector3.forward * carSpeed * Time.deltaTime;
        p.y = TrackShape.Height(p.z);
        car.position = p;
        car.rotation = Quaternion.Euler(TrackShape.PitchDegrees(p.z), 0f, 0f);

        if (cam == null) return;

        // Kamera berputar pelan mengelilingi mobil
        angle += orbitSpeed * Time.deltaTime;
        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, -cameraDistance);
        cam.position = car.position + offset + Vector3.up * cameraHeight;
        cam.LookAt(car.position + Vector3.up * 0.8f);
        cam.position -= cam.right * sideShift;
    }
}
