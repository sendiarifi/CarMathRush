using UnityEngine;
using UnityEngine.EventSystems;

// Efek tombol menu: membesar sedikit saat disorot mouse, mengecil saat ditekan.
// TIDAK perlu dipasang manual: ditambahkan otomatis oleh MenuLook.
public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.06f;
    public float pressScale = 0.95f;

    // Opsional: tulisan kecil yang muncul saat disorot, dan ikon yang berputar saat disorot
    public GameObject tooltip;
    public RectTransform spinIcon;

    private bool hover, pressed;

    public void OnPointerEnter(PointerEventData e) { hover = true; }
    public void OnPointerExit(PointerEventData e) { hover = false; pressed = false; }
    public void OnPointerDown(PointerEventData e) { pressed = true; }
    public void OnPointerUp(PointerEventData e) { pressed = false; }

    void OnDisable()
    {
        hover = pressed = false;
        transform.localScale = Vector3.one;
        if (tooltip != null) tooltip.SetActive(false);
    }

    void Update()
    {
        float target = pressed ? pressScale : hover ? hoverScale : 1f;
        float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        transform.localScale = new Vector3(s, s, 1f);

        if (tooltip != null && tooltip.activeSelf != hover) tooltip.SetActive(hover);
        if (spinIcon != null && hover) spinIcon.Rotate(0f, 0f, -120f * Time.unscaledDeltaTime);
    }
}
