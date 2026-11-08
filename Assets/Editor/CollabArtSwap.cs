using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// 2026-11-05 协作版素材：把 ReefScene_Unity.fbx 中的珊瑚、小鱼、水母网格烘焙为统一朝向/尺寸的资源，
/// 并只替换 Main 中对应对象的网格与材质。位置、尺寸外框、动画脚本和流程保持原样。
/// </summary>
public static class CollabArtSwap
{
    public const string Dir = "Assets/UltrasoundReef/CollabArt/";
    public const string Fbx = Dir + "ReefScene_Unity.fbx";
    public const string Baked = Dir + "Baked/";
    public const string FishPath = Baked + "CollabFish.asset";
    const string Mats = Dir + "Materials/";

    static readonly Dictionary<string, string> CoralTextures = new Dictionary<string, string>
    {
        { "09", "09" }, { "10", "10" }, { "10_alternate", "10_alternate" }, { "11", "11" },
        { "11_alternate", "11_alternate.001" }, { "12", "12" }, { "13", "13" }, { "13_alternate", "13_alternate" },
    };

    // ---------- import ----------
    public static void ConfigureImport()
    {
        var model = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        if (!model) throw new InvalidOperationException("Collaborator FBX missing: " + Fbx);
        bool dirty = !model.bakeAxisConversion || model.importAnimation || model.importCameras || model.importLights
            || !model.isReadable || model.importBlendShapes;
        if (dirty)
        {
            model.bakeAxisConversion = true; model.importAnimation = false; model.importCameras = false;
            model.importLights = false; model.isReadable = true; model.importBlendShapes = false;
            model.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            model.SaveAndReimport();
        }
        foreach (var name in CoralTextures.Values)
        {
            var tex = (TextureImporter)AssetImporter.GetAtPath(Dir + "Textures/" + name + ".png");
            if (!tex) throw new InvalidOperationException("Coral texture missing: " + name);
            if (tex.maxTextureSize != 2048)
            {
                tex.maxTextureSize = 2048; tex.mipmapEnabled = true; tex.sRGBTexture = true;
                tex.SaveAndReimport();
            }
        }
        Directory.CreateDirectory(Baked); Directory.CreateDirectory(Mats); AssetDatabase.Refresh();
    }

    static GameObject Root()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        if (!root) throw new InvalidOperationException("Collaborator FBX did not import.");
        return root;
    }

    static IEnumerable<MeshRenderer> Nodes(Func<string, bool> match) =>
        Root().GetComponentsInChildren<MeshRenderer>(true).Where(r => match(r.name)).OrderBy(r => r.name, StringComparer.Ordinal);

    static Quaternion RelRotation(Component node) => Quaternion.Inverse(node.transform.root.rotation) * node.transform.rotation;

    static Color Imported(Material m)
    {
        if (!m) return Color.white;
        return m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
    }

    // ---------- mesh baking ----------
    static Mesh Bake(Mesh src, Quaternion rotation, Func<Bounds, Matrix4x4> place, string path)
    {
        var v = src.vertices; var n = src.normals;
        bool hasNormals = n != null && n.Length == v.Length;
        for (int i = 0; i < v.Length; i++) { v[i] = rotation * v[i]; if (hasNormals) n[i] = rotation * n[i]; }
        var b = new Bounds(v[0], Vector3.zero);
        foreach (var p in v) b.Encapsulate(p);
        Matrix4x4 m = place(b);
        for (int i = 0; i < v.Length; i++) { v[i] = m.MultiplyPoint3x4(v[i]); if (hasNormals) n[i] = m.rotation * n[i]; }
        var mesh = new Mesh { name = Path.GetFileNameWithoutExtension(path) };
        mesh.indexFormat = v.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.vertices = v;
        if (hasNormals) mesh.normals = n;
        if (src.uv != null && src.uv.Length == v.Length) mesh.uv = src.uv;
        mesh.subMeshCount = src.subMeshCount;
        for (int s = 0; s < src.subMeshCount; s++) mesh.SetTriangles(src.GetTriangles(s), s);
        if (!hasNormals) mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); return existing; }
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static Material Mat(string name, Shader shader, Action<Material> setup)
    {
        string path = Mats + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader; setup(mat); mat.enableInstancing = true; EditorUtility.SetDirty(mat);
        return mat;
    }

    static Shader Find(string name) => Shader.Find(name) ?? throw new InvalidOperationException("Shader missing: " + name);

    // ---------- corals ----------
    static Material CoralMaterial(Material imported)
    {
        string key = imported ? imported.name.Replace(' ', '_') : "";
        if (!CoralTextures.TryGetValue(key, out var file)) throw new InvalidOperationException("Unmapped coral material: " + key);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Textures/" + file + ".png");
        return Mat("Collab coral " + key, Find("UltrasoundReef/CollabCoral"), m => {
            m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Glow", .12f); m.SetFloat("_GlowIntensity", .02f);
        });
    }

    static Mesh CoralMesh(MeshRenderer node, Dictionary<string, Mesh> cache)
    {
        var src = node.GetComponent<MeshFilter>().sharedMesh;
        var b = src.bounds;
        // Same shape exported several times (Mesh.001…): bake once.
        string key = src.vertexCount + "_" + Vector3Int.RoundToInt(b.size * 1000);
        if (cache.TryGetValue(key, out var mesh)) return mesh;
        // Keep only the axis conversion (no per-instance yaw) so every shape stands upright.
        Quaternion up = RelRotation(node);
        Vector3 e = up.eulerAngles; up = Quaternion.Euler(0f, -e.y, 0f) * up;
        mesh = Bake(src, up, bb => Matrix4x4.Scale(Vector3.one / Mathf.Max(.0001f, bb.size.y))
            * Matrix4x4.Translate(-new Vector3(bb.center.x, bb.min.y, bb.center.z)),
            Baked + "CollabCoral_" + cache.Count + ".asset");
        cache[key] = mesh;
        return mesh;
    }

    /// <summary>Fit new geometry into the old object's footprint: same largest dimension, same bottom-centre.</summary>
    static void Swap(Renderer renderer, Mesh mesh, Material[] materials)
    {
        Bounds old = renderer.bounds;
        var t = renderer.transform;
        renderer.GetComponent<MeshFilter>().sharedMesh = mesh;
        renderer.sharedMaterials = materials;
        t.localScale = Vector3.one;
        Bounds now = renderer.bounds;
        float k = Max(old.size) / Mathf.Max(.0001f, Max(now.size));
        t.localScale = Vector3.one * k;
        now = renderer.bounds;
        t.position += new Vector3(old.center.x - now.center.x, old.min.y - now.min.y, old.center.z - now.center.z);
    }

    static float Max(Vector3 v) => Mathf.Max(v.x, Mathf.Max(v.y, v.z));

    public static void ApplyCorals(Transform coralGardens, Transform patient, Transform mirror, out int decorCount)
    {
        var cache = new Dictionary<string, Mesh>();
        var player = Nodes(n => n.StartsWith("Player_Coral")).FirstOrDefault()
            ?? throw new InvalidOperationException("Player_Coral missing in collaborator FBX.");
        var playerMat = CoralMaterial(player.sharedMaterial);
        var playerMesh = CoralMesh(player, cache);
        Swap(patient.GetComponent<Renderer>(), playerMesh, new[] { playerMat });
        if (mirror) Swap(mirror.GetComponent<Renderer>(), playerMesh, new[] { playerMat }); // 镜像珊瑚已删除,Main 传 null

        var combos = Nodes(n => n.StartsWith("Coral") || n.StartsWith("Plate"))
            .Select(r => (mesh: CoralMesh(r, cache), mat: CoralMaterial(r.sharedMaterial))).ToList();
        if (combos.Count == 0) throw new InvalidOperationException("No decorative corals in collaborator FBX.");
        decorCount = 0;
        if (!coralGardens) return;
        var renderers = coralGardens.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var c = combos[(i * 7) % combos.Count]; // stride spreads shapes/colours around the reef
            Swap(renderers[i], c.mesh, new[] { c.mat });
            renderers[i].transform.Rotate(0f, (i * 137.5f) % 360f, 0f, Space.World);
            decorCount++;
        }
    }

    // ---------- anemones (2026-11-05 第二轮:demo1 有海葵,Main 原来没有) ----------
    /// <summary>
    /// 把 FBX 的海葵烘焙成"单位高度、底部在 y=0"的网格(和珊瑚一样),并配上 CollabCoral 材质(无贴图,用原色),
    /// 这样场景4 的尖端发光(_ReefBreathGlow)对海葵同样有效。返回网格、材质和 FBX 中每只海葵的世界位置与高度。
    /// </summary>
    public static Mesh BakeAnemone(out Material[] materials, out List<(Vector3 pos, float height)> placements)
    {
        var nodes = Nodes(n => n.StartsWith("Anemone")).ToList();
        if (nodes.Count == 0) throw new InvalidOperationException("No anemones in collaborator FBX.");
        var node = nodes[0];
        Quaternion up = RelRotation(node);
        Vector3 e = up.eulerAngles; up = Quaternion.Euler(0f, -e.y, 0f) * up;
        var mesh = Bake(node.GetComponent<MeshFilter>().sharedMesh, up,
            bb => Matrix4x4.Scale(Vector3.one / Mathf.Max(.0001f, bb.size.y)) * Matrix4x4.Translate(-new Vector3(bb.center.x, bb.min.y, bb.center.z)),
            Baked + "CollabAnemone.asset");
        var shader = Find("UltrasoundReef/CollabCoral");
        materials = node.sharedMaterials.Select((m, i) => Mat("Collab anemone " + (m ? m.name : i.ToString()), shader, x => {
            x.SetTexture("_BaseMap", null); x.SetColor("_BaseColor", Imported(m));
            x.SetFloat("_Glow", .14f); x.SetFloat("_GlowIntensity", .03f);
        })).ToArray();
        placements = nodes.Select(r => (r.bounds.center, r.bounds.size.y)).ToList();
        return mesh;
    }

    /// <summary>FBX 中参与者珊瑚(Player_Coral)的世界位置,用于把 demo1 的布局对齐到 Main 的主角珊瑚。</summary>
    public static Vector3 PlayerCoralPosition() =>
        (Nodes(n => n.StartsWith("Player_Coral")).FirstOrDefault() ?? throw new InvalidOperationException("Player_Coral missing")).transform.position;

    /// <summary>FBX 中 demo1 远处水母(Jelly_*)的世界位置。</summary>
    public static List<Vector3> FarJellyPositions() =>
        Nodes(n => n.StartsWith("Jelly_")).Select(r => r.transform.position).ToList();

    // ---------- fish ----------
    public static Material[] FishMaterials;
    public static Mesh BuildFish()
    {
        var node = Nodes(n => n.StartsWith("Fish_")).First();
        var src = node.GetComponent<MeshFilter>().sharedMesh;
        var reference = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/UltrasoundReef/Environment/PastelReefFish.asset");
        if (!reference) throw new InvalidOperationException("PastelReefFish reference size missing.");
        Quaternion rot = RelRotation(node);
        // Upright, then turn the head (side with the eye submesh) to +Z as the swimming code expects.
        var v = src.vertices.Select(p => rot * p).ToArray();
        var b = new Bounds(v[0], Vector3.zero); foreach (var p in v) b.Encapsulate(p);
        bool alongX = b.size.x >= b.size.z;
        var imported = node.sharedMaterials;
        int eyes = Array.FindIndex(imported, m => m && m.name.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0);
        float sign = 1f;
        if (eyes >= 0)
        {
            var idx = src.GetTriangles(eyes).Distinct().ToArray();
            Vector3 c = Vector3.zero; foreach (int i in idx) c += v[i]; c /= Mathf.Max(1, idx.Length);
            sign = Mathf.Sign((alongX ? c.x - b.center.x : c.z - b.center.z) + 1e-6f);
        }
        Vector3 head = (alongX ? Vector3.right : Vector3.forward) * sign;
        rot = Quaternion.FromToRotation(head, Vector3.forward) * rot;
        Bounds r = reference.bounds;
        var mesh = Bake(src, rot, bb => Matrix4x4.Translate(r.center)
            * Matrix4x4.Scale(Vector3.one * (r.size.z / Mathf.Max(.0001f, bb.size.z)))
            * Matrix4x4.Translate(-bb.center), FishPath);
        var shader = Find("UltrasoundReef/CollabFish");
        FishMaterials = imported.Select((m, i) => Mat("Collab fish " + (m ? m.name : i.ToString()), shader, x => {
            x.SetColor("_BaseColor", Imported(m));
            x.SetFloat("_Gloss", m && m.name.StartsWith("Eyes") ? .8f : .25f);
        })).ToArray();
        return mesh;
    }

    // ---------- jellyfish ----------
    public static void ApplyJellies(Transform[] jellies)
    {
        if (jellies == null || jellies.Length == 0) return;
        var node = Nodes(n => n == "Interactive_Jellyfish").FirstOrDefault() ?? Nodes(n => n.StartsWith("Jelly_")).First();
        var src = node.GetComponent<MeshFilter>().sharedMesh;
        // Old four-part jelly bounds in the jelly root's own space (all jellies share the prefab geometry).
        var first = jellies[0];
        Bounds old = default; bool any = false;
        foreach (var r in first.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.name == "Collab jellyfish") continue;
            var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
            Matrix4x4 toRoot = first.worldToLocalMatrix * r.transform.localToWorldMatrix;
            var mb = mf.sharedMesh.bounds;
            for (int c = 0; c < 8; c++)
            {
                var p = toRoot.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents,
                    new Vector3((c & 1) * 2 - 1, (c & 2) - 1, (c & 4) / 2 - 1)));
                if (!any) { old = new Bounds(p, Vector3.zero); any = true; } else old.Encapsulate(p);
            }
        }
        if (!any) throw new InvalidOperationException("Original jellyfish geometry not found.");
        float scale = 1f, bellRadius = .5f, depth = 1.8f;
        var mesh = Bake(src, RelRotation(node), bb => {
            // Keep the authored origin (bell rim) at y = 0 so the shader can tell bell from tentacles.
            scale = old.size.y / Mathf.Max(.0001f, bb.size.y);
            bellRadius = Mathf.Max(bb.extents.x, bb.extents.z) * scale;
            depth = Mathf.Max(.1f, -bb.min.y * scale);
            return Matrix4x4.Scale(Vector3.one * scale) * Matrix4x4.Translate(new Vector3(-bb.center.x, 0f, -bb.center.z));
        }, Baked + "CollabJellyfish.asset");
        // Top-align with the old jelly so docking points and hand distance stay the same.
        float offsetY = old.max.y - mesh.bounds.max.y;
        Vector3 offset = new Vector3(old.center.x, offsetY, old.center.z);
        var shader = Find("UltrasoundReef/CollabJelly");
        var mats = node.sharedMaterials.Select((m, i) => {
            bool inner = m && m.name.IndexOf("Rose", StringComparison.OrdinalIgnoreCase) >= 0;
            return Mat("Collab jelly " + (m ? m.name : i.ToString()), shader, x => {
                x.SetColor("_BaseColor", Imported(m));
                x.SetFloat("_Opacity", inner ? .7f : .46f); x.SetFloat("_Glow", inner ? 1.4f : .6f);
                x.SetFloat("_BellRadius", bellRadius); x.SetFloat("_TentacleLength", depth);
            });
        }).ToArray();
        foreach (var jelly in jellies)
        {
            foreach (var r in jelly.GetComponentsInChildren<Renderer>(true))
                if (r.name != "Collab jellyfish") r.gameObject.SetActive(false);
            var existing = jelly.Find("Collab jellyfish");
            var go = existing ? existing.gameObject : new GameObject("Collab jellyfish", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(jelly, false);
            go.transform.localPosition = offset; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = mats; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            go.SetActive(true);
        }
    }
}
