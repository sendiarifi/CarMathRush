using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pasang ke objek Canvas di scene MainMenu. Tidak perlu mengisi apa pun.
// Memberi tampilan bertema balap: tombol oranye, teks tebal, judul bergaris tepi.
public class MenuStyler : MonoBehaviour
{
    [Header("Warna tema")]
    public Color buttonColor = new Color(1f, 0.5f, 0.1f);
    public Color titleColor = new Color(1f, 0.82f, 0.2f);
    public Color outlineColor = new Color(0f, 0f, 0f, 0.85f);

    void Awake()
    {
        // Semua tombol, termasuk yang ada di panel toko
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponentInParent<ShopMenu>(true) != null) continue;   // toko punya gaya sendiri (UiTheme)
            var image = button.GetComponent<Image>();
            if (image != null) image.color = Color.white;   // warna diatur lewat ColorBlock

            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = Color.Lerp(buttonColor, Color.white, 0.3f);
            colors.pressedColor = buttonColor * 0.8f;
            colors.selectedColor = buttonColor;
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            button.colors = colors;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = Color.white;
                label.fontStyle = FontStyles.Bold;
            }
        }

        // Semua teks di luar tombol dan kolom input diberi garis tepi agar terbaca di atas latar
        foreach (var text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.GetComponentInParent<Button>(true) != null) continue;
            if (text.GetComponentInParent<TMP_InputField>(true) != null) continue;
            if (text.GetComponentInParent<ShopMenu>(true) != null) continue;
            text.outlineWidth = 0.2f;
            text.outlineColor = outlineColor;
        }

        // Judul game
        var title = FindChild(transform, "TitleText");
        if (title != null)
        {
            var t = title.GetComponent<TMP_Text>();
            t.color = titleColor;
            t.fontStyle = FontStyles.Bold | FontStyles.Italic;
            t.outlineWidth = 0.25f;
        }
    }

    Transform FindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            var found = FindChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
