using UnityEngine;

// Pasang ke PlayerCar. Model mobil diambil dari Katalog Mobil (Assets/Resources/CarCatalog).
// Car Models di Inspector hanya cadangan kalau katalog belum dibuat.
public class CarSelector : MonoBehaviour
{
    public GameObject[] carModels;

    [Header("Atur kalau mobil terlihat mengambang / tenggelam")]
    public Vector3 modelOffset = new Vector3(0f, -0.5f, 0f);
    public float modelScale = 1f;

    // -1 = pakai mobil pilihan di toko. Diisi otomatis oleh DuoManager untuk Pemain 2.
    [System.NonSerialized] public int overrideIndex = -1;

    private Transform currentCar;

    void Start()
    {
        int wanted = overrideIndex >= 0 ? overrideIndex : CarData.Selected;
        GameObject prefab = CarData.Model(Mathf.Clamp(wanted, 0, CarData.Count - 1), carModels);
        if (prefab == null) return;

        GameObject car = Instantiate(prefab, transform);
        car.transform.localRotation = Quaternion.identity;
        currentCar = car.transform;
    }

    void Update()
    {
        // Diterapkan terus agar bisa diatur langsung saat Play
        if (currentCar == null) return;
        currentCar.localPosition = modelOffset;
        currentCar.localScale = Vector3.one * modelScale;
    }
}
