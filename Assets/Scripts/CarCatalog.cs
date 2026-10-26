using UnityEngine;

// KATALOG MOBIL: satu-satunya daftar mobil di game (nama, model 3D, harga, kelangkaan).
// File-nya: Assets/Resources/CarCatalog.asset, dibuat otomatis lewat menu
// "Car Math Rush > Buat Katalog Mobil Otomatis" di bagian atas Unity.
// Garasi, mobil di game, mobil Pemain 2, dan latar menu semuanya membaca daftar ini.
//
// PENTING: mobil baru selalu ditambahkan di PALING BAWAH daftar.
// Data "sudah dibeli" disimpan per nomor urut, jadi menyisipkan di tengah akan menggeser kepemilikan.
// Urutan tampil di garasi diatur otomatis dari harga termurah.
[CreateAssetMenu(fileName = "CarCatalog", menuName = "Car Math Rush/Katalog Mobil")]
public class CarCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string displayName = "Mobil Baru";
        public GameObject model;
        [Min(0)] public int price = 50;
        [Tooltip("0 = BIASA, 1 = LANGKA, 2 = EPIK, 3 = LEGENDARIS")]
        [Range(0, 3)] public int rarity = 1;
    }

    public Entry[] cars = new Entry[0];
}
