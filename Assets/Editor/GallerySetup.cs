using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GallerySetup
{
    const string ScenePath = "Assets/Scenes/SampleScene_3d.unity";
    const string ModelPath = "Assets/Gallery/Lernraum.fbx";
    const string FrameMaterialPath = "Assets/Gallery/Rahmen_Mat.mat";

    static readonly string[] WallNames = { "Wand_Hinten", "Wand_Links", "Wand_Rechts", "Wand_Vorne" };

    struct PaintingDef
    {
        public string node;
        public string label;
        public string texPath;
        public PaintingDef(string node, string label, string texPath)
        {
            this.node = node;
            this.label = label;
            this.texPath = texPath;
        }
    }

    static readonly PaintingDef[] Paintings =
    {
        new PaintingDef("Gemälde_01_Blumen", "Gemälde 01 – Blumen", "Assets/Gallery/Pictures/Blumen.png"),
        new PaintingDef("Gemälde_02_feld", "Gemälde 02 – Feld", "Assets/Gallery/Pictures/feld.png"),
        new PaintingDef("Gemälde_03_Les_mangeurs_de_pommes_de_terre", "Gemälde 03 – Les mangeurs de pommes de terre", "Assets/Gallery/Pictures/Les_mangeurs_de_pommes_de_terre.jpg"),
        new PaintingDef("Gemälde_04_Nacht.jpg", "Gemälde 04 – Nacht", "Assets/Gallery/Pictures/Nacht_1.png"),
        new PaintingDef("Gemälde_05_nacht", "Gemälde 05 – Nacht", "Assets/Gallery/Pictures/Nacht_2.png"),
        new PaintingDef("Gemälde_06_Vase", "Gemälde 06 – Vase", "Assets/Gallery/Pictures/Vase.png"),
    };

    [MenuItem("Gallery/Setup Scene (Collider, Wände, Bilder, Rahmen)")]
    public static void Setup()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("Bitte zuerst " + ScenePath + " öffnen und aktiv setzen, dann erneut ausführen. Aktive Szene war: " + scene.path);
            return;
        }
        var root = FindOrInstantiateModel();

        AddColliders(root);
        HideWalls(root);
        AssignPaintings(root);
        AddFrames(root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Gallery-Setup abgeschlossen: Collider, unsichtbare Wände, Bilder und Rahmen wurden eingerichtet.");
    }

    static GameObject FindOrInstantiateModel()
    {
        foreach (var t in Object.FindObjectsOfType<Transform>())
        {
            if (t.parent == null && FindDeep(t, "Boden") != null && FindDeep(t, "Wand_Hinten") != null)
                return t.gameObject;
        }

        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        Undo.RegisterCreatedObjectUndo(instance, "Lernraum instanziiert");
        return instance;
    }

    static void AddColliders(GameObject root)
    {
        var names = new string[WallNames.Length + 1];
        WallNames.CopyTo(names, 0);
        names[WallNames.Length] = "Boden";

        foreach (var n in names)
        {
            var t = FindDeep(root.transform, n);
            if (t == null)
            {
                Debug.LogWarning("Objekt nicht gefunden: " + n);
                continue;
            }
            if (t.GetComponent<Collider>() == null)
            {
                Undo.AddComponent<MeshCollider>(t.gameObject);
            }
        }
    }

    static void HideWalls(GameObject root)
    {
        foreach (var n in WallNames)
        {
            var t = FindDeep(root.transform, n);
            if (t == null) continue;
            var mr = t.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Undo.RecordObject(mr, "Wand unsichtbar schalten");
                mr.enabled = false;
            }
        }
    }

    static void AssignPaintings(GameObject root)
    {
        var comp = root.GetComponentInChildren<GalleryPaintings>(true);
        if (comp == null)
            comp = Undo.AddComponent<GalleryPaintings>(root);

        var slots = new GalleryPaintings.Slot[Paintings.Length];
        for (int i = 0; i < Paintings.Length; i++)
        {
            var def = Paintings[i];
            var t = FindDeep(root.transform, def.node);
            var slot = new GalleryPaintings.Slot();
            slot.label = def.label;
            slot.target = t != null ? t.GetComponent<Renderer>() : null;
            slot.bild = AssetDatabase.LoadAssetAtPath<Texture2D>(def.texPath);
            if (t == null)
                Debug.LogWarning("Gemälde-Slot nicht gefunden: " + def.node);
            slots[i] = slot;
        }
        comp.gemaelde = slots;
        comp.Apply();
        EditorUtility.SetDirty(comp);
    }

    static void AddFrames(GameObject root)
    {
        var frameMat = GetOrCreateFrameMaterial();
        foreach (var def in Paintings)
        {
            var t = FindDeep(root.transform, def.node);
            if (t == null) continue;
            if (t.Find("Rahmen") != null) continue;
            BuildFrame(t, frameMat);
        }
    }

    static Material GetOrCreateFrameMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(FrameMaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.25f, 0.15f, 0.08f);
            AssetDatabase.CreateAsset(mat, FrameMaterialPath);
        }
        return mat;
    }

    static void BuildFrame(Transform picture, Material frameMat)
    {
        var mf = picture.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;
        var bounds = mf.sharedMesh.bounds;

        float ax = bounds.size.x, ay = bounds.size.y, az = bounds.size.z;
        int thin = (ax <= ay && ax <= az) ? 0 : (ay <= ax && ay <= az) ? 1 : 2;

        Vector3 size = bounds.size;
        Vector3 center = bounds.center;

        float border = 0.06f * Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        if (border <= 0f) border = 0.02f;
        float depth = Mathf.Max(size[thin], border * 0.3f);

        var frameRoot = new GameObject("Rahmen");
        frameRoot.transform.SetParent(picture, false);
        Undo.RegisterCreatedObjectUndo(frameRoot, "Rahmen hinzugefügt");

        if (thin == 2)
        {
            AddBar(frameRoot.transform, frameMat, "Oben", new Vector3(center.x, center.y + size.y / 2f + border / 2f, center.z), new Vector3(size.x + 2 * border, border, depth));
            AddBar(frameRoot.transform, frameMat, "Unten", new Vector3(center.x, center.y - size.y / 2f - border / 2f, center.z), new Vector3(size.x + 2 * border, border, depth));
            AddBar(frameRoot.transform, frameMat, "Links", new Vector3(center.x - size.x / 2f - border / 2f, center.y, center.z), new Vector3(border, size.y, depth));
            AddBar(frameRoot.transform, frameMat, "Rechts", new Vector3(center.x + size.x / 2f + border / 2f, center.y, center.z), new Vector3(border, size.y, depth));
        }
        else if (thin == 1)
        {
            AddBar(frameRoot.transform, frameMat, "Oben", new Vector3(center.x, center.y, center.z + size.z / 2f + border / 2f), new Vector3(size.x + 2 * border, depth, border));
            AddBar(frameRoot.transform, frameMat, "Unten", new Vector3(center.x, center.y, center.z - size.z / 2f - border / 2f), new Vector3(size.x + 2 * border, depth, border));
            AddBar(frameRoot.transform, frameMat, "Links", new Vector3(center.x - size.x / 2f - border / 2f, center.y, center.z), new Vector3(border, depth, size.z));
            AddBar(frameRoot.transform, frameMat, "Rechts", new Vector3(center.x + size.x / 2f + border / 2f, center.y, center.z), new Vector3(border, depth, size.z));
        }
        else
        {
            AddBar(frameRoot.transform, frameMat, "Oben", new Vector3(center.x, center.y + size.y / 2f + border / 2f, center.z), new Vector3(depth, border, size.z + 2 * border));
            AddBar(frameRoot.transform, frameMat, "Unten", new Vector3(center.x, center.y - size.y / 2f - border / 2f, center.z), new Vector3(depth, border, size.z + 2 * border));
            AddBar(frameRoot.transform, frameMat, "Links", new Vector3(center.x, center.y, center.z - size.z / 2f - border / 2f), new Vector3(depth, size.y, border));
            AddBar(frameRoot.transform, frameMat, "Rechts", new Vector3(center.x, center.y, center.z + size.z / 2f + border / 2f), new Vector3(depth, size.y, border));
        }
    }

    static void AddBar(Transform parent, Material mat, string name, Vector3 localPos, Vector3 localScale)
    {
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = name;
        Object.DestroyImmediate(bar.GetComponent<BoxCollider>());
        bar.transform.SetParent(parent, false);
        bar.transform.localPosition = localPos;
        bar.transform.localScale = localScale;
        bar.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
