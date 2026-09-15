using UnityEngine;

[ExecuteInEditMode]
public class GalleryPaintings : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public string label;
        public Renderer target;
        public Texture2D bild;
    }

    public Slot[] gemaelde;

    void Start()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    public void Apply()
    {
        if (gemaelde == null) return;
        foreach (var slot in gemaelde)
        {
            if (slot == null || slot.target == null || slot.bild == null) continue;
            var mat = slot.target.sharedMaterial;
            if (mat != null) mat.mainTexture = slot.bild;
        }
    }
}
