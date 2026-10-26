using UnityEngine;
using UnityEngine.UI;

// Membuat elemen UI miring (jajaran genjang) ala game balap.
// TIDAK perlu dipasang manual: ditambahkan otomatis oleh UiTheme.
[DisallowMultipleComponent]
public class UiSkew : BaseMeshEffect
{
    [Range(-0.5f, 0.5f)] public float amount = 0.18f;   // 0 = tegak, makin besar makin miring

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;
        var v = new UIVertex();
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            v.position.x += v.position.y * amount;
            vh.SetUIVertex(v, i);
        }
    }
}
