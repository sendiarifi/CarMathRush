using System.Collections.Generic;
using UnityEngine;

// Menyediakan material yang tetap tampil benar di build (.exe).
// Butuh material bernama "BaseLit" di folder Assets/Resources.
// Tidak perlu dipasang ke objek mana pun.
public static class MaterialHelper
{
    private static Material baseMaterial;
    private static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

    public static Material Base
    {
        get
        {
            if (baseMaterial == null)
            {
                baseMaterial = Resources.Load<Material>("BaseLit");
                if (baseMaterial == null)
                {
                    // Cadangan kalau material BaseLit belum dibuat
                    Debug.LogWarning("Material 'BaseLit' tidak ditemukan di Assets/Resources.");
                    var shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader != null) baseMaterial = new Material(shader);
                }
            }
            return baseMaterial;
        }
    }

    // Material baru dengan warna tertentu
    public static Material Create(Color color)
    {
        var m = new Material(Base);
        color.a = 1f;
        m.color = color;
        return m;
    }

    // Material yang dipakai bersama untuk warna yang sama (lebih hemat)
    public static Material Shared(Color color)
    {
        if (!cache.TryGetValue(color, out var m) || m == null)
        {
            m = Create(color);
            cache[color] = m;
        }
        return m;
    }
}
