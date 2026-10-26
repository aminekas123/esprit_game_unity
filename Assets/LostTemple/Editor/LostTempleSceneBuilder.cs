using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LostTemple.EditorTools
{
    /// <summary>
    /// Génère les 8 scènes (menu + 7 salles, une par capteur) : décor texturé, explorateur jouable, pièges, reliques,
    /// stations d'énigmes et toutes les références déjà branchées. Menu : Lost Temple > Générer toutes les scènes.
    /// Les graphistes peuvent ensuite remplacer les formes simples par de vrais modèles.
    /// Attention : regénérer écrase les scènes existantes.
    /// </summary>
    public static class LostTempleSceneBuilder
    {
        const string SceneDir = "Assets/LostTemple/Scenes";
        const string MatDir = "Assets/LostTemple/Materials";
        const string TexDir = "Assets/LostTemple/Textures";
        const string MeshDir = "Assets/LostTemple/Meshes";
        const float TileSize = 3f;   // une répétition de texture tous les 3 m

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static VolumeProfile postProfile;
        static System.Random rng;

        [MenuItem("Lost Temple/Générer toutes les scènes")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAllNoPrompt();
        }

        public static void BuildAllNoPrompt()
        {
            foreach (var d in new[] { SceneDir, MatDir, TexDir, MeshDir }) Directory.CreateDirectory(d);
            mats.Clear();
            meshes.Clear();
            postProfile = null;
            rng = new System.Random(42);

            var scenes = new List<EditorBuildSettingsScene>
            {
                Save(BuildMenu(), GameManager.MenuScene),
                Save(BuildRoom1(), GameManager.RoomScenes[0]),
                Save(BuildRoom2(), GameManager.RoomScenes[1]),
                Save(BuildRoom3(), GameManager.RoomScenes[2]),
                Save(BuildRoom4(), GameManager.RoomScenes[3]),
                Save(BuildRoom5(), GameManager.RoomScenes[4]),
                Save(BuildRoom6(), GameManager.RoomScenes[5]),
                Save(BuildRoom7(), GameManager.RoomScenes[6]),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene($"{SceneDir}/{GameManager.MenuScene}.unity");
            Debug.Log("[Lost Temple] 8 scènes générées et ajoutées aux Build Settings.");
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Lost Temple";
            PlayerSettings.companyName = "LostTempleTeam";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.losttempleteam.losttemple");
        }

        static EditorBuildSettingsScene Save(Scene s, string name)
        {
            string path = $"{SceneDir}/{name}.unity";
            EditorSceneManager.SaveScene(s, path);
            return new EditorBuildSettingsScene(path, true);
        }

        // ================================================================== Textures procédurales (pierre)

        static float Hash(int x, int y, int seed)
        {
            float h = Mathf.Sin(x * 127.1f + y * 311.7f + seed * 74.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        static float Fbm(float x, float y, int seed)
        {
            float s = 0f, a = 0.5f, f = 1f;
            for (int o = 0; o < 4; o++) { s += a * Mathf.PerlinNoise(x * f + seed * 13.1f, y * f + seed * 7.3f); a *= 0.5f; f *= 2f; }
            return s / 0.9375f;
        }

        /// <summary>Bruit répétable sans couture : mélange de 4 échantillons décalés d'une période.</summary>
        static float SeamlessFbm(float u, float v, float freq, int seed)
        {
            float a = Fbm(u * freq, v * freq, seed), b = Fbm((u - 1) * freq, v * freq, seed);
            float c = Fbm(u * freq, (v - 1) * freq, seed), d = Fbm((u - 1) * freq, (v - 1) * freq, seed);
            return a * (1 - u) * (1 - v) + b * u * (1 - v) + c * (1 - u) * v + d * u * v;
        }

        /// <summary>Crée (une seule fois) une texture de blocs de pierre + sa carte de relief (normal map).</summary>
        static (Texture2D albedo, Texture2D normal) StoneTextures(string name, int cols, int rows, bool offsetRows, int seed)
        {
            string ap = $"{TexDir}/{name}_Albedo.png", np = $"{TexDir}/{name}_Normal.png";
            var at = AssetDatabase.LoadAssetAtPath<Texture2D>(ap);
            var nt = AssetDatabase.LoadAssetAtPath<Texture2D>(np);
            if (at != null && nt != null) return (at, nt);

            const int S = 512;
            var height = new float[S * S];
            var albedo = new Color32[S * S];
            for (int y = 0; y < S; y++)
            {
                float v = y / (float)S;
                int row = Mathf.FloorToInt(v * rows);
                float fy = v * rows - row;
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S;
                    float shift = offsetRows && (row & 1) == 1 ? 0.5f : 0f;
                    float cu = u * cols + shift;
                    int c = Mathf.FloorToInt(cu);
                    float fx = cu - c;
                    c = ((c % cols) + cols) % cols;

                    float dx = Mathf.Min(fx, 1 - fx) * S / cols, dy = Mathf.Min(fy, 1 - fy) * S / rows;
                    float d = Mathf.Min(dx, dy);
                    float grout = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.5f, 7f, d));
                    float bevel = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.5f, 16f, d));
                    float br = Hash(c, row, seed);
                    float n = SeamlessFbm(u, v, 9f, seed);
                    float fine = SeamlessFbm(u, v, 40f, seed + 1);

                    height[y * S + x] = bevel * (0.75f + 0.1f * br) + (n - 0.5f) * 0.3f * grout + (fine - 0.5f) * 0.08f;
                    float lum = Mathf.Lerp(0.25f, 0.62f + 0.28f * br, grout) + (n - 0.5f) * 0.35f + (fine - 0.5f) * 0.12f;
                    lum = Mathf.Clamp01(lum);
                    albedo[y * S + x] = new Color(lum * 1.04f, lum, lum * 0.92f);
                }
            }

            var normal = new Color32[S * S];
            const float strength = 6f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float hl = height[y * S + (x + S - 1) % S], hr = height[y * S + (x + 1) % S];
                    float hd = height[((y + S - 1) % S) * S + x], hu = height[((y + 1) % S) * S + x];
                    var nrm = new Vector3((hl - hr) * strength, (hd - hu) * strength, 1f).normalized;
                    normal[y * S + x] = new Color(nrm.x * 0.5f + 0.5f, nrm.y * 0.5f + 0.5f, nrm.z * 0.5f + 0.5f);
                }

            WritePng(ap, albedo, S, false);
            WritePng(np, normal, S, true);
            return (AssetDatabase.LoadAssetAtPath<Texture2D>(ap), AssetDatabase.LoadAssetAtPath<Texture2D>(np));
        }

        static void WritePng(string path, Color32[] px, int size, bool isNormal)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false, isNormal);
            t.SetPixels32(px);
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.maxTextureSize = 512;
            if (isNormal) imp.textureType = TextureImporterType.NormalMap;
            imp.SaveAndReimport();
        }

        // ================================================================== Matériaux

        static Material Mat(string name, Color c, float metallic = 0f, float smooth = 0.2f, bool glow = false, (Texture2D albedo, Texture2D normal)? tex = null)
        {
            if (mats.TryGetValue(name, out var m)) return m;
            string path = $"{MatDir}/{name}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);
            if (tex.HasValue)
            {
                m.SetTexture("_BaseMap", tex.Value.albedo);
                m.SetTexture("_BumpMap", tex.Value.normal);
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            else
            {
                m.SetTexture("_BaseMap", null);
                m.SetTexture("_BumpMap", null);
                m.DisableKeyword("_NORMALMAP");
            }
            if (glow)
            {
                // Émission activée (noire) pour que la variante de shader soit incluse dans le build Android.
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            mats[name] = m;
            return m;
        }

        static (Texture2D, Texture2D) Slabs => StoneTextures("Slabs", 3, 3, true, 3);
        static (Texture2D, Texture2D) Bricks => StoneTextures("Bricks", 4, 9, true, 5);

        static Material Stone => Mat("Stone", new Color(0.62f, 0.55f, 0.46f), 0f, 0.15f, false, Slabs);
        static Material DarkStone => Mat("DarkStone", new Color(0.32f, 0.29f, 0.26f), 0f, 0.2f, false, Slabs);
        static Material Sand => Mat("Sandstone", new Color(0.86f, 0.7f, 0.5f), 0f, 0.1f, false, Bricks);
        static Material Carved => Mat("Carved", new Color(0.7f, 0.56f, 0.38f), 0f, 0.25f, true, Bricks);
        static Material Pit => Mat("Pit", new Color(0.02f, 0.02f, 0.04f));
        static Material Gold => Mat("Gold", new Color(1f, 0.76f, 0.3f), 0.85f, 0.7f, true);
        static Material Glyph => Mat("Glyph", new Color(0.12f, 0.11f, 0.12f), 0f, 0.4f, true);
        static Material Metal => Mat("TrapMetal", new Color(0.35f, 0.36f, 0.4f), 0.8f, 0.55f, true);
        static Material RelicMat
        {
            get
            {
                var m = Mat("Relic", new Color(1f, 0.8f, 0.35f), 0.9f, 0.8f, true);
                m.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.15f) * 1.6f);
                return m;
            }
        }
        static Material FlameLit
        {
            get
            {
                var m = Mat("Flame", new Color(1f, 0.55f, 0.15f), 0f, 0f, true);
                m.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f) * 4f);
                return m;
            }
        }

        // ================================================================== Post-traitement & particules

        static VolumeProfile PostProfile()
        {
            if (postProfile != null) return postProfile;
            string path = $"{MatDir}/LostTemplePost.asset";
            // Toujours recréé pour que les réglages ci-dessous restent la référence.
            AssetDatabase.DeleteAsset(path);
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);

            var bloom = p.Add<Bloom>(true);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(1.4f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(1f, 0.85f, 0.65f));

            var vig = p.Add<Vignette>(true);
            vig.intensity.Override(0.3f);
            vig.smoothness.Override(0.45f);
            vig.color.Override(new Color(0.02f, 0.02f, 0.06f));

            var tone = p.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            var color = p.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.9f);
            color.contrast.Override(12f);
            color.saturation.Override(8f);

            foreach (var c in p.components) AssetDatabase.AddObjectToAsset(c, p);
            EditorUtility.SetDirty(p);
            return postProfile = p;
        }

        static Material ParticleMat()
        {
            string path = $"{MatDir}/Particle.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = AdditiveMat(Color.white);
            var tex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (tex != null) m.SetTexture("_BaseMap", tex);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Faisceau des stations : additif, sans texture.</summary>
        static Material MarkerMat()
        {
            string path = $"{MatDir}/Marker.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = AdditiveMat(new Color(1f, 0.7f, 0.25f, 0.35f));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material AdditiveMat(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            // Transparent additif : la lumière s'ajoute sans assombrir le décor.
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", c);
            return m;
        }

        static void Dust(Transform root, Vector3 center, Vector3 size)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(root, false);
            go.transform.localPosition = center;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = new Color(1f, 0.8f, 0.55f, 0.35f);
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 25f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = size;
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.08f; noise.frequency = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
        }

        static ParticleSystem Sparks(Transform root)
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.5f, 0.15f));
            main.gravityModifier = 0.6f;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
            return ps;
        }

        // ================================================================== Primitives

        /// <summary>Cube dont les UV suivent les dimensions réelles : la texture garde la même échelle partout.</summary>
        static Mesh BoxMesh(Vector3 s)
        {
            string key = $"box_{s.x:0.##}_{s.y:0.##}_{s.z:0.##}".Replace(',', '.');
            if (meshes.TryGetValue(key, out var cached)) return cached;
            string path = $"{MeshDir}/{key}.asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (m == null)
            {
                // normale, axe U, axe V (cross(U,V) = normale) et axes de texture horizontal/vertical
                var faces = new (Vector3 n, Vector3 u, Vector3 v, Vector3 h, Vector3 t)[]
                {
                    (Vector3.right, Vector3.up, Vector3.forward, Vector3.forward, Vector3.up),
                    (Vector3.left, Vector3.forward, Vector3.up, Vector3.forward, Vector3.up),
                    (Vector3.up, Vector3.forward, Vector3.right, Vector3.right, Vector3.forward),
                    (Vector3.down, Vector3.right, Vector3.forward, Vector3.right, Vector3.forward),
                    (Vector3.forward, Vector3.right, Vector3.up, Vector3.right, Vector3.up),
                    (Vector3.back, Vector3.up, Vector3.right, Vector3.right, Vector3.up),
                };
                var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
                foreach (var f in faces)
                {
                    int b = verts.Count;
                    Vector3 c = f.n * 0.5f;
                    var corners = new[] { c - f.u * 0.5f - f.v * 0.5f, c + f.u * 0.5f - f.v * 0.5f, c + f.u * 0.5f + f.v * 0.5f, c - f.u * 0.5f + f.v * 0.5f };
                    foreach (var p in corners)
                    {
                        verts.Add(p);
                        norms.Add(f.n);
                        Vector3 w = Vector3.Scale(p, s);
                        uvs.Add(new Vector2(Vector3.Dot(w, f.h), Vector3.Dot(w, f.t)) / TileSize);
                    }
                    tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                }
                m = new Mesh { name = key };
                m.SetVertices(verts); m.SetNormals(norms); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
                m.RecalculateTangents();
                m.RecalculateBounds();
                AssetDatabase.CreateAsset(m, path);
            }
            meshes[key] = m;
            return m;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Box(string n, Transform p, Vector3 pos, Vector3 s, Material m, bool col = true)
        {
            var go = Prim(PrimitiveType.Cube, n, p, pos, s, m, col);
            go.GetComponent<MeshFilter>().sharedMesh = BoxMesh(s);
            return go;
        }

        static Transform Empty(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        static Transform Pose(string name, Transform parent, Vector3 pos, Vector3 lookAt)
        {
            var t = Empty(name, parent, pos);
            t.rotation = Quaternion.LookRotation(lookAt - pos);
            return t;
        }

        static TextMesh Text3D(string name, Transform parent, Vector3 pos, string text, float size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return tm;
        }

        static void Torch(Transform parent, Vector3 pos)
        {
            var t = Empty("Torch", parent, pos);
            Prim(PrimitiveType.Cylinder, "Stick", t, new Vector3(0, -0.3f, 0), new Vector3(0.12f, 0.3f, 0.12f), DarkStone, false);
            Prim(PrimitiveType.Cylinder, "Cup", t, new Vector3(0, -0.02f, 0), new Vector3(0.26f, 0.06f, 0.26f), Metal, false);
            Prim(PrimitiveType.Sphere, "Flame", t, new Vector3(0, 0.1f, 0), new Vector3(0.22f, 0.34f, 0.22f), FlameLit, false);
            var lgo = new GameObject("Light");
            lgo.transform.SetParent(t, false);
            lgo.transform.localPosition = new Vector3(0, 0.3f, 0);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.6f, 0.25f); l.range = 8f; l.intensity = 2.8f;
            l.shadows = LightShadows.None;
            lgo.AddComponent<TorchFlicker>();
        }

        // ================================================================== Explorateur

        static GameObject Explorer(Transform root, Vector3 pos, float yaw, bool playable, bool lanternDrains = false)
        {
            var go = new GameObject(playable ? "Explorer" : "Explorer (menu)");
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);

            var skin = Mat("Skin", new Color(0.86f, 0.64f, 0.48f), 0f, 0.35f);
            var shirt = Mat("Shirt", new Color(0.82f, 0.74f, 0.55f), 0f, 0.15f);
            var pants = Mat("Pants", new Color(0.36f, 0.32f, 0.22f), 0f, 0.15f);
            var leather = Mat("Leather", new Color(0.4f, 0.24f, 0.12f), 0f, 0.35f);
            var pack = Mat("Backpack", new Color(0.3f, 0.36f, 0.22f), 0f, 0.15f);
            var black = Mat("Black", new Color(0.05f, 0.05f, 0.05f), 0f, 0.6f);

            var visual = Empty("Visual", go.transform, Vector3.zero);
            var body = Empty("Body", visual, Vector3.zero);

            Transform Leg(string n, float x)
            {
                var pivot = Empty(n, body, new Vector3(x, 0.85f, 0));
                Prim(PrimitiveType.Capsule, "Thigh", pivot, new Vector3(0, -0.38f, 0), new Vector3(0.2f, 0.4f, 0.2f), pants, false);
                Box("Boot", pivot, new Vector3(0, -0.8f, 0.05f), new Vector3(0.2f, 0.12f, 0.3f), leather, false);
                return pivot;
            }
            Transform Arm(string n, float x)
            {
                var pivot = Empty(n, body, new Vector3(x, 1.42f, 0));
                Prim(PrimitiveType.Capsule, "Sleeve", pivot, new Vector3(0, -0.25f, 0), new Vector3(0.15f, 0.26f, 0.15f), shirt, false);
                Prim(PrimitiveType.Sphere, "Hand", pivot, new Vector3(0, -0.55f, 0), Vector3.one * 0.12f, skin, false);
                return pivot;
            }
            var legL = Leg("Leg L", -0.12f);
            var legR = Leg("Leg R", 0.12f);
            Prim(PrimitiveType.Capsule, "Torso", body, new Vector3(0, 1.17f, 0), new Vector3(0.46f, 0.36f, 0.3f), shirt, false);
            Prim(PrimitiveType.Cylinder, "Belt", body, new Vector3(0, 0.9f, 0), new Vector3(0.44f, 0.04f, 0.3f), leather, false);
            var armL = Arm("Arm L", -0.3f);
            var armR = Arm("Arm R", 0.3f);
            Prim(PrimitiveType.Sphere, "Head", body, new Vector3(0, 1.7f, 0), Vector3.one * 0.32f, skin, false);
            Prim(PrimitiveType.Sphere, "Eye L", body, new Vector3(-0.065f, 1.73f, 0.145f), Vector3.one * 0.045f, black, false);
            Prim(PrimitiveType.Sphere, "Eye R", body, new Vector3(0.065f, 1.73f, 0.145f), Vector3.one * 0.045f, black, false);
            Prim(PrimitiveType.Cylinder, "Hat Brim", body, new Vector3(0, 1.83f, 0), new Vector3(0.56f, 0.015f, 0.56f), leather, false);
            Prim(PrimitiveType.Cylinder, "Hat Crown", body, new Vector3(0, 1.92f, 0), new Vector3(0.32f, 0.09f, 0.32f), leather, false);
            Prim(PrimitiveType.Cylinder, "Hat Band", body, new Vector3(0, 1.87f, 0), new Vector3(0.33f, 0.025f, 0.33f), black, false);
            Box("Backpack", body, new Vector3(0, 1.2f, -0.22f), new Vector3(0.36f, 0.42f, 0.18f), pack, false);
            var roll = Prim(PrimitiveType.Cylinder, "Bedroll", body, new Vector3(0, 1.46f, -0.24f), new Vector3(0.14f, 0.2f, 0.14f), leather, false);
            roll.transform.localRotation = Quaternion.Euler(0, 0, 90);

            // Lanterne : éclaire les salles sombres autour de l'explorateur
            var lantern = Empty("Lantern", body, new Vector3(0.36f, 0.95f, 0.12f));
            var lanternFlame = Prim(PrimitiveType.Cylinder, "Lantern Body", lantern, Vector3.zero, new Vector3(0.12f, 0.08f, 0.12f), FlameLit, false);
            var ll = new GameObject("Lantern Light").AddComponent<Light>();
            ll.transform.SetParent(lantern, false);
            ll.type = LightType.Point; ll.color = new Color(1f, 0.75f, 0.45f); ll.range = 5f; ll.intensity = 1.6f; ll.shadows = LightShadows.None;

            var lan = go.AddComponent<Lantern>();
            lan.lightSource = ll; lan.flame = lanternFlame.GetComponent<Renderer>(); lan.drains = lanternDrains;
            go.AddComponent<PlayerCostume>();

            var anim = go.AddComponent<CharacterAnimator>();
            anim.body = body; anim.legL = legL; anim.legR = legR; anim.armL = armL; anim.armR = armR;

            if (playable)
            {
                var cc = go.AddComponent<CharacterController>();
                cc.center = new Vector3(0, 0.95f, 0); cc.height = 1.8f; cc.radius = 0.3f; cc.stepOffset = 0.3f; cc.slopeLimit = 50f;
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                var pc = go.AddComponent<PlayerController>();
                anim.player = pc;
            }
            return go;
        }

        // ================================================================== Éléments de gameplay

        static Station MakeStation(Transform root, string name, Vector3 pos, PuzzleStep step)
        {
            var go = new GameObject("Station - " + name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(2f, 2f, 2f); col.center = new Vector3(0, 1f, 0);
            var marker = Empty("Marker", go.transform, Vector3.zero).gameObject;
            Prim(PrimitiveType.Cylinder, "Beam", marker.transform, new Vector3(0, 3f, 0), new Vector3(1f, 3f, 1f), MarkerMat(), false);
            Prim(PrimitiveType.Cylinder, "Ring", marker.transform, new Vector3(0, 0.03f, 0), new Vector3(1.8f, 0.01f, 1.8f), MarkerMat(), false);
            var st = go.AddComponent<Station>();
            st.step = step; st.marker = marker;
            return st;
        }

        static void SpikeTrap(Transform root, Vector3 pos, Vector2 size, float offset, float period = 2.8f)
        {
            var go = new GameObject("Spike Trap");
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(size.x, 1.2f, size.y); col.center = new Vector3(0, 0.6f, 0);
            var plate = Box("Plate", go.transform, new Vector3(0, 0.01f, 0), new Vector3(size.x, 0.03f, size.y), Mat("TrapPlate", new Color(0.2f, 0.18f, 0.17f), 0.4f, 0.4f, true), false);
            var spikes = Empty("Spikes", go.transform, Vector3.zero);
            for (float x = -size.x / 2f + 0.3f; x < size.x / 2f; x += 0.55f)
                for (float z = -size.y / 2f + 0.3f; z < size.y / 2f; z += 0.55f)
                    Prim(PrimitiveType.Sphere, "Spike", spikes, new Vector3(x, 0.3f, z), new Vector3(0.11f, 0.6f, 0.11f), Metal, false);
            var h = go.AddComponent<Hazard>();
            h.mode = HazardMode.Periodic; h.period = period; h.activeTime = 1.1f; h.offset = offset;
            h.movingPart = spikes; h.hiddenOffset = new Vector3(0, -0.65f, 0);
            h.warnPlate = plate.GetComponent<Renderer>();
            h.message = "Aïe ! Les pics vous ont eu…";
        }

        static void FireJet(Transform root, Vector3 pos, float offset, float width = 2.2f)
        {
            var go = new GameObject("Fire Jet");
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(width, 2.6f, 1f); col.center = new Vector3(0, 1.3f, 0);
            var nozzle = Prim(PrimitiveType.Cylinder, "Nozzle", go.transform, new Vector3(0, 0.12f, 0), new Vector3(0.6f, 0.12f, 0.6f), Mat("TrapPlate", Color.gray, 0.4f, 0.4f, true), false);

            var fx = new GameObject("Flames");
            fx.transform.SetParent(go.transform, false);
            fx.transform.localPosition = new Vector3(0, 0.25f, 0);
            fx.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var ps = fx.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 6.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.3f), new Color(1f, 0.35f, 0.1f));
            main.maxParticles = 300;
            var em = ps.emission; em.rateOverTime = 90f; em.enabled = false;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 10f; sh.radius = 0.2f;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.4f));
            var col2 = ps.colorOverLifetime; col2.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.25f, 0.05f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col2.color = g;
            fx.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();

            var l = new GameObject("Fire Light").AddComponent<Light>();
            l.transform.SetParent(go.transform, false);
            l.transform.localPosition = new Vector3(0, 1.2f, 0);
            l.type = LightType.Point; l.color = new Color(1f, 0.5f, 0.15f); l.range = 5f; l.intensity = 0f; l.shadows = LightShadows.None;

            var h = go.AddComponent<Hazard>();
            h.mode = HazardMode.Periodic; h.period = 3.2f; h.activeTime = 1.4f; h.offset = offset;
            h.fx = ps; h.fxLight = l; h.warnPlate = nozzle.GetComponent<Renderer>();
            h.message = "Brûlé ! Attendez que le feu s'éteigne.";
        }

        static void FloorSweeper(Transform root, Vector3 pos, float length, float speed)
        {
            Prim(PrimitiveType.Cylinder, "Sweeper Axis", root, pos + new Vector3(0, 0.3f, 0), new Vector3(0.45f, 0.3f, 0.45f), Gold, false);
            var pivot = Empty("Floor Sweeper", root, pos);
            var rb = pivot.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            pivot.gameObject.AddComponent<Sweeper>().speed = speed;
            var bar = Box("Blade", pivot, new Vector3(0, 0.45f, 0), new Vector3(length, 0.3f, 0.25f), Metal);
            bar.GetComponent<BoxCollider>().size = new Vector3(1f, 2f, 2.4f);
            var h = bar.AddComponent<Hazard>();
            h.mode = HazardMode.Always;
            h.message = "Fauché par la lame ! Passez quand elle s'éloigne.";
        }

        static void Relic(Transform root, Vector3 pos)
        {
            var go = Box("Relic", root, pos, Vector3.one * 0.32f, RelicMat, false);
            go.transform.rotation = Quaternion.Euler(45, 0, 45);
            var sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true; sc.radius = 2.2f;
            go.AddComponent<LostTemple.Relic>();
        }

        static ExitTrigger Exit(Transform root, Vector3 pos)
        {
            var go = new GameObject("Exit");
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(2.2f, 2f, 1.2f); col.center = new Vector3(0, 1f, 0);
            return go.AddComponent<ExitTrigger>();
        }

        // ================================================================== Scène de base

        static (Scene scene, Transform root, Camera cam) NewRoom(string name, Vector3 camPos, Vector3 lookAt, float fov, Color ambient)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(name).transform;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = camPos;
            camGo.transform.LookAt(lookAt);
            var c = camGo.AddComponent<Camera>();
            c.fieldOfView = fov;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.02f, 0.025f, 0.05f);
            c.farClipPlane = 80f;
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            var vol = new GameObject("Post Processing").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = PostProfile();

            var sun = new GameObject("Moonlight");
            sun.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional; l.color = new Color(0.6f, 0.65f, 0.95f); l.intensity = 0.6f;
            l.shadows = LightShadows.Soft;

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.03f, 0.035f, 0.07f);
            RenderSettings.fogDensity = 0.022f;
            return (scene, root, c);
        }

        /// <summary>Salle rectangulaire : sol, 3 murs (l'avant reste ouvert pour la caméra), plinthes, corniches, piliers et gravats.</summary>
        static void Shell(Transform root, float width, float zMin, float zMax, bool floor = true)
        {
            float depth = zMax - zMin, zc = (zMin + zMax) / 2f, hw = width / 2f;
            if (floor) Box("Floor", root, new Vector3(0, -0.25f, zc), new Vector3(width, 0.5f, depth), Stone);
            Box("Wall Back", root, new Vector3(0, 1.5f, zMax + 0.5f), new Vector3(width + 2f, 9f, 1f), Sand);
            Box("Wall Left", root, new Vector3(-hw - 0.5f, 1.5f, zc), new Vector3(1f, 9f, depth), Sand);
            Box("Wall Right", root, new Vector3(hw + 0.5f, 1.5f, zc), new Vector3(1f, 9f, depth), Sand);
            Decor(root, width, zMin, zMax);
        }

        static void Decor(Transform root, float width, float zMin, float zMax)
        {
            float depth = zMax - zMin, zc = (zMin + zMax) / 2f, hw = width / 2f;
            // Plinthes et corniches
            Box("Trim Back", root, new Vector3(0, 0.2f, zMax - 0.1f), new Vector3(width, 0.4f, 0.25f), DarkStone, false);
            Box("Trim Left", root, new Vector3(-hw + 0.1f, 0.2f, zc), new Vector3(0.25f, 0.4f, depth), DarkStone, false);
            Box("Trim Right", root, new Vector3(hw - 0.1f, 0.2f, zc), new Vector3(0.25f, 0.4f, depth), DarkStone, false);
            Box("Cornice Back", root, new Vector3(0, 5.2f, zMax - 0.15f), new Vector3(width, 0.35f, 0.35f), Carved, false);
            Box("Cornice Left", root, new Vector3(-hw + 0.15f, 5.2f, zc), new Vector3(0.35f, 0.35f, depth), Carved, false);
            Box("Cornice Right", root, new Vector3(hw - 0.15f, 5.2f, zc), new Vector3(0.35f, 0.35f, depth), Carved, false);
            // Bandeau de glyphes lumineux le long des murs
            var strip = Mat("GlyphStrip", new Color(0.15f, 0.13f, 0.1f), 0f, 0.3f, true);
            strip.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.2f) * 0.35f);
            Box("Glyph Strip Back", root, new Vector3(0, 4.6f, zMax - 0.02f), new Vector3(width * 0.8f, 0.12f, 0.05f), strip, false);
            Box("Glyph Strip Left", root, new Vector3(-hw + 0.02f, 4.6f, zc), new Vector3(0.05f, 0.12f, depth * 0.8f), strip, false);
            Box("Glyph Strip Right", root, new Vector3(hw - 0.02f, 4.6f, zc), new Vector3(0.05f, 0.12f, depth * 0.8f), strip, false);
            // Piliers engagés dans les murs
            for (float z = zMin + 3f; z < zMax - 1f; z += 6f)
            {
                Box("Pilaster", root, new Vector3(-hw + 0.25f, 2.6f, z), new Vector3(0.5f, 5.2f, 0.8f), Stone, false);
                Box("Pilaster", root, new Vector3(hw - 0.25f, 2.6f, z), new Vector3(0.5f, 5.2f, 0.8f), Stone, false);
            }
            // Gravats
            for (int i = 0; i < 14; i++)
            {
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                float x = side * (hw - 0.6f - (float)rng.NextDouble() * 0.9f);
                float z = zMin + 1f + (float)rng.NextDouble() * (depth - 2f);
                float s = 0.15f + (float)rng.NextDouble() * 0.35f;
                var r = Box("Rubble", root, new Vector3(x, s * 0.4f, z), new Vector3(s, s * 0.8f, s * 1.2f), rng.NextDouble() < 0.5 ? Stone : Sand, false);
                r.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 20f, (float)rng.NextDouble() * 360f, 0);
            }
        }

        /// <summary>Porte de pierre avec cadre, serrure et effets d'ouverture (poussière, gravats, lumière derrière).</summary>
        static (Transform door, DoorFX fx) Door(Transform root, Vector3 pos)
        {
            Box("Door Frame Top", root, pos + new Vector3(0, 1.75f, 0.05f), new Vector3(3f, 0.45f, 0.9f), Carved);
            Box("Door Frame L", root, pos + new Vector3(-1.3f, 0f, 0.05f), new Vector3(0.4f, 3.1f, 0.9f), Carved);
            Box("Door Frame R", root, pos + new Vector3(1.3f, 0f, 0.05f), new Vector3(0.4f, 3.1f, 0.9f), Carved);
            Box("Door Glyph", root, pos + new Vector3(0, 2.45f, -0.4f), new Vector3(1.6f, 0.5f, 0.1f), Mat("GlyphStrip", Color.gray, 0f, 0.3f, true), false);
            var door = Box("Door", root, pos, new Vector3(2.2f, 3f, 0.5f), Carved).transform;
            Box("Door Ring L", door, new Vector3(-0.25f, 0f, -0.55f), new Vector3(0.12f, 0.12f, 0.1f), Gold, false);
            Box("Door Ring R", door, new Vector3(0.25f, 0f, -0.55f), new Vector3(0.12f, 0.12f, 0.1f), Gold, false);

            var fxGo = new GameObject("Door FX");
            fxGo.transform.SetParent(root, false);
            fxGo.transform.localPosition = pos;
            var fx = fxGo.AddComponent<DoorFX>();
            fx.door = door;
            fx.lightSpill = Box("Light Spill", root, pos + new Vector3(0, 0f, 0.24f), new Vector3(2.15f, 2.95f, 0.02f), Mat("LightSpill", new Color(1f, 0.85f, 0.6f), 0f, 0f, true), false).GetComponent<Renderer>();
            var glow = new GameObject("Door Glow").AddComponent<Light>();
            glow.transform.SetParent(fxGo.transform, false);
            glow.transform.localPosition = new Vector3(0, 0f, -1.2f);
            glow.type = LightType.Point; glow.color = new Color(1f, 0.8f, 0.5f); glow.range = 9f; glow.intensity = 0f; glow.shadows = LightShadows.None;
            fx.glow = glow;
            fx.dust = FxBurst(fxGo.transform, "Dust Cloud", new Vector3(0, -1.4f, -0.6f), new Color(0.75f, 0.65f, 0.5f, 0.5f), 0.4f, 1.2f, 1.2f, 2.5f, -0.05f, 60, new Vector3(2.6f, 0.2f, 0.6f));
            fx.debris = FxBurst(fxGo.transform, "Debris", new Vector3(0, 1.8f, -0.5f), new Color(0.55f, 0.45f, 0.35f, 1f), 0.05f, 0.14f, 1.5f, 0.5f, 1.5f, 40, new Vector3(2.4f, 0.1f, 0.4f));
            return (door, fx);
        }

        static ParticleSystem FxBurst(Transform parent, string name, Vector3 pos, Color color, float sizeMin, float sizeMax, float life, float speed, float gravity, int burst, Vector3 box)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.playOnAwake = false;
            main.duration = 1.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life * 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = burst / 1.5f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = SoftParticleMat();
            return ps;
        }

        /// <summary>Particules non additives (poussière, gravats).</summary>
        static Material SoftParticleMat()
        {
            string path = $"{MatDir}/SoftParticle.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            var tex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (tex != null) m.SetTexture("_BaseMap", tex);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static RoomManager AddRoomManager(Transform root, int index, (Transform door, DoorFX fx) door, ExitTrigger exit, PuzzleStep[] steps, Station[] stations)
        {
            Dust(root, new Vector3(0, 2.5f, 0f), new Vector3(13f, 4f, 22f));
            var rm = root.gameObject.AddComponent<RoomManager>();
            rm.sparks = Sparks(root);
            rm.roomIndex = index;
            rm.door = door.door; rm.doorFx = door.fx; rm.exit = exit;
            rm.steps = steps; rm.stations = stations;
            return rm;
        }

        static void FollowCamera(Camera cam, GameObject player)
        {
            var rig = cam.gameObject.AddComponent<CameraRig>();
            rig.target = player.transform;
            rig.offset = new Vector3(0f, 8.5f, -6.5f);
            Vector3 focus = player.transform.position + Vector3.up + Vector3.forward * rig.lookAhead;
            cam.transform.position = focus + rig.offset;
            cam.transform.LookAt(focus);
        }

        // ================================================================== Menu

        static Scene BuildMenu()
        {
            var (scene, root, cam) = NewRoom("Temple Facade", new Vector3(0, 2.4f, -9.5f), new Vector3(0, 3.2f, 0), 60f, new Color(0.1f, 0.11f, 0.2f));
            cam.gameObject.AddComponent<CameraSway>().amplitude = 0.12f;
            Box("Ground", root, new Vector3(0, -0.25f, 0), new Vector3(30, 0.5f, 30), Stone);
            for (int i = 0; i < 3; i++)
                Box("Step", root, new Vector3(0, 0.2f + i * 0.4f, 1.5f + i * 0.8f), new Vector3(10 - i, 0.4f, 1.6f), DarkStone);
            Box("Facade", root, new Vector3(0, 5f, 5f), new Vector3(12, 10, 1.5f), Sand);
            Box("Entrance", root, new Vector3(0, 2.8f, 4.2f), new Vector3(3.2f, 4.4f, 0.2f), Pit, false);
            Box("Lintel", root, new Vector3(0, 5.3f, 4.1f), new Vector3(4.4f, 0.6f, 0.4f), Carved);
            Box("Pediment", root, new Vector3(0, 7.2f, 4.1f), new Vector3(9f, 0.5f, 0.6f), Carved);
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                Prim(PrimitiveType.Cylinder, "Column", root, new Vector3(i * 2.4f, 3.4f, 3.6f), new Vector3(0.8f, 3.2f, 0.8f), Stone);
                Box("Capital", root, new Vector3(i * 2.4f, 6.7f, 3.6f), new Vector3(1.1f, 0.3f, 1.1f), Carved);
            }
            var art = Box("Artefact", root, new Vector3(0, 2.6f, 4f), new Vector3(0.5f, 0.5f, 0.5f), Gold, false);
            art.transform.rotation = Quaternion.Euler(45, 0, 45);
            var al = new GameObject("Artefact Light").AddComponent<Light>();
            al.transform.position = new Vector3(0, 2.6f, 3.5f);
            al.type = LightType.Point; al.color = new Color(1f, 0.75f, 0.35f); al.range = 6f; al.intensity = 4f;
            Torch(root, new Vector3(-2.6f, 2.6f, 3.2f));
            Torch(root, new Vector3(2.6f, 2.6f, 3.2f));
            Explorer(root, new Vector3(-1.6f, 0f, -1.5f), 160f, false);
            Dust(root, new Vector3(0, 3f, 1f), new Vector3(12f, 6f, 8f));
            return scene;
        }

        // ================================================================== Nouveautés : secrets, rocher, silence, momies

        static void Secret(Transform root, Vector3 pos, int roomIndex)
        {
            var go = new GameObject("Secret Relic");
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            var relicGo = Box("Secret Relic Gem", go.transform, Vector3.zero, Vector3.one * 0.42f, Mat("SecretRelic", new Color(0.6f, 0.85f, 1f), 0.9f, 0.9f, true), false);
            relicGo.GetComponent<Renderer>().sharedMaterial.SetColor("_EmissionColor", new Color(0.4f, 0.8f, 1f) * 2.5f);
            relicGo.transform.rotation = Quaternion.Euler(45, 0, 45);
            var sc = relicGo.AddComponent<SphereCollider>();
            sc.isTrigger = true; sc.radius = 2f;
            relicGo.AddComponent<LostTemple.Relic>();
            var sr = go.AddComponent<SecretRelic>();
            sr.gesture = Journal.Secrets[roomIndex];
            sr.relic = relicGo;
            sr.revealFx = FxBurst(go.transform, "Reveal FX", Vector3.zero, new Color(0.5f, 0.85f, 1f, 1f), 0.05f, 0.18f, 1.2f, 3f, -0.2f, 80, Vector3.one * 0.5f);
            sr.revealFx.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
        }

        static Boulder MakeBoulder(Transform root, float endZ)
        {
            var go = new GameObject("Boulder Chase");
            go.transform.SetParent(root, false);
            var rock = Prim(PrimitiveType.Sphere, "Boulder", go.transform, new Vector3(0, 1.4f, -11f), Vector3.one * 2.8f, Mat("BoulderStone", new Color(0.5f, 0.42f, 0.32f), 0f, 0.15f, false, Slabs), false);
            var b = go.AddComponent<Boulder>();
            b.rock = rock.transform; b.endZ = endZ;
            b.crashDust = FxBurst(go.transform, "Crash Dust", Vector3.zero, new Color(0.7f, 0.6f, 0.45f, 0.6f), 0.4f, 1.3f, 1.8f, 3f, -0.05f, 120, new Vector3(3f, 1f, 1f));
            b.crashDust.GetComponent<ParticleSystemRenderer>().sharedMaterial = SoftParticleMat();
            return b;
        }

        static void Quiet(Transform root, Vector3 center, Vector3 size)
        {
            var go = new GameObject("Quiet Zone");
            go.transform.SetParent(root, false);
            go.transform.localPosition = center;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = size; col.center = new Vector3(0, size.y / 2f, 0);
            var qz = go.AddComponent<QuietZone>();
            qz.darts = FxBurst(go.transform, "Darts", Vector3.zero, new Color(0.9f, 0.85f, 0.7f, 1f), 0.05f, 0.1f, 0.5f, 9f, 2f, 40, new Vector3(1.5f, 0.2f, 1.5f));
            // Indices visuels : dalles à motif d'oreille et fentes à fléchettes dans les murs
            Box("Quiet Floor Mark", root, center + new Vector3(0, 0.01f, 0), new Vector3(size.x * 0.9f, 0.02f, size.z * 0.9f), Mat("QuietMark", new Color(0.2f, 0.25f, 0.4f), 0f, 0.3f), false);
            for (float z = -size.z / 2f + 1f; z < size.z / 2f; z += 1.5f)
            {
                Box("Dart Slot", root, center + new Vector3(-size.x / 2f + 0.05f, 1.2f, z), new Vector3(0.1f, 0.12f, 0.5f), Pit, false);
                Box("Dart Slot", root, center + new Vector3(size.x / 2f - 0.05f, 1.2f, z), new Vector3(0.1f, 0.12f, 0.5f), Pit, false);
            }
        }

        static void Mummy(Transform root, Vector3[] path)
        {
            var bandage = Mat("Bandage", new Color(0.78f, 0.72f, 0.58f), 0f, 0.15f, false, Bricks);
            var eyeMat = Mat("MummyEye", new Color(0.3f, 0.05f, 0.02f), 0f, 0.9f, true);
            eyeMat.SetColor("_EmissionColor", new Color(1f, 0.25f, 0.05f) * 3f);
            var go = new GameObject("Mummy Guardian");
            go.transform.SetParent(root, false);
            go.transform.localPosition = path[0];
            Transform Limb(string n, Vector3 p, Vector3 child, Vector3 scale)
            {
                var pivot = Empty(n, go.transform, p);
                Prim(PrimitiveType.Capsule, n + " Wrap", pivot, child, scale, bandage, false);
                return pivot;
            }
            var legL = Limb("Leg L", new Vector3(-0.15f, 0.9f, 0), new Vector3(0, -0.45f, 0), new Vector3(0.24f, 0.45f, 0.24f));
            var legR = Limb("Leg R", new Vector3(0.15f, 0.9f, 0), new Vector3(0, -0.45f, 0), new Vector3(0.24f, 0.45f, 0.24f));
            Prim(PrimitiveType.Capsule, "Torso", go.transform, new Vector3(0, 1.3f, 0), new Vector3(0.55f, 0.45f, 0.38f), bandage, false);
            var armL = Limb("Arm L", new Vector3(-0.35f, 1.55f, 0), new Vector3(0, -0.35f, 0), new Vector3(0.17f, 0.35f, 0.17f));
            var armR = Limb("Arm R", new Vector3(0.35f, 1.55f, 0), new Vector3(0, -0.35f, 0), new Vector3(0.17f, 0.35f, 0.17f));
            Prim(PrimitiveType.Sphere, "Head", go.transform, new Vector3(0, 1.95f, 0), new Vector3(0.36f, 0.4f, 0.36f), bandage, false);
            Prim(PrimitiveType.Sphere, "Eye L", go.transform, new Vector3(-0.08f, 1.98f, 0.16f), Vector3.one * 0.07f, eyeMat, false);
            Prim(PrimitiveType.Sphere, "Eye R", go.transform, new Vector3(0.08f, 1.98f, 0.16f), Vector3.one * 0.07f, eyeMat, false);
            var vision = new GameObject("Vision").AddComponent<Light>();
            vision.transform.SetParent(go.transform, false);
            vision.transform.localPosition = new Vector3(0, 1.9f, 0.2f);
            vision.transform.localRotation = Quaternion.Euler(28f, 0, 0);
            vision.type = LightType.Spot; vision.spotAngle = 70f; vision.range = 6f; vision.intensity = 6f;
            vision.color = new Color(1f, 0.45f, 0.2f); vision.shadows = LightShadows.None;

            var wp = new Transform[path.Length];
            for (int i = 0; i < path.Length; i++) wp[i] = Empty("Waypoint " + i, root, path[i]);
            var e = go.AddComponent<Enemy>();
            e.waypoints = wp; e.legL = legL; e.legR = legR; e.armL = armL; e.armR = armR; e.visionLight = vision;
        }

        // ================================================================== Salle 1 — Accéléromètre

        static Scene BuildRoom1()
        {
            var (scene, root, cam) = NewRoom("Room1", new Vector3(0, 10f, -16f), new Vector3(0, 0, -6f), 60f, new Color(0.17f, 0.18f, 0.27f));
            // Sol : bande avant, passerelle à gauche, bande du fond ; le reste est un gouffre où roule la boule.
            Box("Floor Front", root, new Vector3(0, -0.25f, -9f), new Vector3(14f, 0.5f, 6f), Stone);
            Box("Walkway", root, new Vector3(-5f, -0.25f, 0f), new Vector3(4f, 0.5f, 12f), Stone);
            Box("Floor Back", root, new Vector3(0, -0.25f, 8f), new Vector3(14f, 0.5f, 4f), Stone);
            Box("Pit", root, new Vector3(2f, -6f, 0f), new Vector3(10f, 1f, 12f), Pit);
            Box("Pit Edge", root, new Vector3(-3.05f, -0.3f, 0f), new Vector3(0.15f, 0.6f, 12f), DarkStone, false);
            Shell(root, 14f, -12f, 10f, false);

            Box("Start Platform", root, new Vector3(0, 0, -4.8f), new Vector3(2.2f, 0.4f, 2f), Stone);
            Box("Bridge 1", root, new Vector3(0, 0, -2.2f), new Vector3(1.5f, 0.4f, 3.2f), Sand);
            Box("Bridge 2", root, new Vector3(1.75f, 0, 0), new Vector3(5f, 0.4f, 1.5f), Sand);
            Box("Bridge 3", root, new Vector3(3.5f, 0, 2.4f), new Vector3(1.5f, 0.4f, 3.4f), Sand);
            Box("Goal Platform", root, new Vector3(3.5f, 0, 5f), new Vector3(2.6f, 0.4f, 1.8f), Stone);
            foreach (var p in new[] { new Vector3(-0.9f, 0, -4f), new Vector3(4.4f, 0, 0f), new Vector3(2.6f, 0, 4.4f) })
                Box("Support", root, new Vector3(p.x, -3f, p.z), new Vector3(0.6f, 6f, 0.6f), DarkStone, false);
            var goalPlate = Box("Goal Plate", root, new Vector3(3.5f, 0.22f, 5f), new Vector3(1.3f, 0.05f, 1.3f), Gold, false).GetComponent<Renderer>();
            var goal = new GameObject("Goal Trigger");
            goal.transform.SetParent(root, false);
            goal.transform.localPosition = new Vector3(3.5f, 0.7f, 5f);
            var gcol = goal.AddComponent<BoxCollider>();
            gcol.isTrigger = true; gcol.size = new Vector3(1.3f, 1f, 1.3f);

            // Piège sur le pont de la boule
            Prim(PrimitiveType.Cylinder, "Sweeper Axis", root, new Vector3(3.5f, 0.45f, 2.4f), new Vector3(0.35f, 0.25f, 0.35f), Gold, false);
            var sweeper = Box("Sweeper", root, new Vector3(3.5f, 0.5f, 2.4f), new Vector3(2.2f, 0.25f, 0.22f), Metal);
            sweeper.AddComponent<Rigidbody>();
            sweeper.AddComponent<Sweeper>().speed = 55f;

            SpikeTrap(root, new Vector3(-5f, 0, -1.5f), new Vector2(4f, 1.6f), 0f);
            SpikeTrap(root, new Vector3(-5f, 0, 2.5f), new Vector2(4f, 1.6f), 1.4f);
            Relic(root, new Vector3(-6.3f, 0.8f, 0.5f));
            Relic(root, new Vector3(6f, 0.8f, -10.5f));
            Relic(root, new Vector3(6f, 0.8f, 8.5f));

            foreach (var t in new[] { new Vector3(-6.7f, 2.6f, -9f), new Vector3(6.7f, 2.6f, -9f), new Vector3(-6.7f, 2.6f, 3f), new Vector3(6.7f, 2.6f, 3f), new Vector3(-6.7f, 2.6f, 8f), new Vector3(6.7f, 2.6f, 8f) })
                Torch(root, t);

            var door = Door(root, new Vector3(-1f, 1.5f, 9.75f));
            var exit = Exit(root, new Vector3(-1f, 0f, 8.7f));

            Prim(PrimitiveType.Cylinder, "Lever Base", root, new Vector3(-3.6f, 0.4f, -4.8f), new Vector3(0.7f, 0.4f, 0.7f), Carved);
            var lever = Box("Lever", root, new Vector3(-3.6f, 1.1f, -4.8f), new Vector3(0.12f, 0.8f, 0.12f), Gold, false);
            lever.transform.rotation = Quaternion.Euler(0, 0, -25);

            var spawn = Empty("Ball Spawn", root, new Vector3(0, 0.6f, -4.8f));
            var ball = Prim(PrimitiveType.Sphere, "Ball", root, spawn.localPosition, Vector3.one * 0.5f, Gold);
            var rb = ball.AddComponent<Rigidbody>();
            rb.mass = 1f; rb.linearDamping = 0.4f; rb.angularDamping = 0.6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.isKinematic = true;

            var puzzle = root.gameObject.AddComponent<TiltBallPuzzle>();
            puzzle.sensor = SensorKind.Accelerometer;
            puzzle.objective = "Guidez la boule d'or jusqu'à la plaque lumineuse";
            puzzle.tutorial = "Tenez le téléphone devant vous, comme pour lire.\n\nInclinez-le doucement vers l'avant, l'arrière, la gauche ou la droite : la boule roule dans ce sens.\n\nSi elle tombe, elle revient au départ.";
            puzzle.hint = "Faites de petits mouvements et redressez le téléphone pour freiner. Attendez que la barre tournante soit passée avant de traverser.";
            puzzle.fxAnchor = goalPlate.transform;
            puzzle.cameraPose = Pose("Cam Ball", root, new Vector3(1.75f, 12.5f, -8.5f), new Vector3(1.75f, 0f, 0.8f));
            puzzle.ball = rb; puzzle.spawn = spawn; puzzle.goal = gcol; puzzle.goalPlate = goalPlate;
            goal.AddComponent<GoalTrigger>().puzzle = puzzle;

            var st = MakeStation(root, "Levier", new Vector3(-4.6f, 0f, -4.8f), puzzle);
            var player = Explorer(root, new Vector3(-3f, 0.05f, -10f), 0f, true);
            FollowCamera(cam, player);

            // Cinématique d'intro : la grille de l'entrée (mur gauche) se referme derrière l'explorateur
            Box("Entrance Frame", root, new Vector3(-6.9f, 3.3f, -10f), new Vector3(0.6f, 0.5f, 3f), Carved);
            Box("Entrance Hole", root, new Vector3(-6.98f, 1.5f, -10f), new Vector3(0.05f, 3f, 2.4f), Pit, false);
            var slab = Box("Entrance Slab", root, new Vector3(-6.75f, 9f, -10f), new Vector3(0.4f, 3.1f, 2.6f), Carved);
            var intro = root.gameObject.AddComponent<IntroCinematic>();
            intro.slab = slab.transform;
            intro.slabClosedPosition = new Vector3(-6.75f, 1.55f, -10f);
            intro.landingDust = FxBurst(root, "Landing Dust", Vector3.zero, new Color(0.75f, 0.65f, 0.5f, 0.6f), 0.3f, 0.9f, 1.2f, 2f, -0.05f, 60, new Vector3(1.5f, 0.2f, 1.5f));
            intro.landingDust.GetComponent<ParticleSystemRenderer>().sharedMaterial = SoftParticleMat();
            Secret(root, new Vector3(-5f, 0.9f, 6.5f), 0);
            AddRoomManager(root, 0, door, exit, new PuzzleStep[] { puzzle }, new[] { st });
            return scene;
        }

        // ================================================================== Salle 2 — Boussole

        static Scene BuildRoom2()
        {
            var (scene, root, cam) = NewRoom("Room2", new Vector3(0, 10f, -16f), new Vector3(0, 0, -6f), 60f, new Color(0.15f, 0.16f, 0.25f));
            Shell(root, 14f, -12f, 12f);

            // Labyrinthe
            Box("Maze Wall 1", root, new Vector3(-2f, 1.5f, -8.5f), new Vector3(10f, 3f, 0.7f), Sand);
            Box("Maze Wall 2", root, new Vector3(4.5f, 1.5f, -4.8f), new Vector3(5f, 3f, 0.7f), Sand);
            Box("Maze Wall 3", root, new Vector3(-4.5f, 1.5f, 4.5f), new Vector3(5f, 3f, 0.7f), Sand);
            Box("Maze Wall 4", root, new Vector3(3f, 1.5f, 4.5f), new Vector3(2f, 3f, 0.7f), Sand);

            Prim(PrimitiveType.Cylinder, "Compass Pedestal", root, new Vector3(-3f, 0.5f, -5.5f), new Vector3(1.4f, 0.5f, 1.4f), Carved);
            Prim(PrimitiveType.Cylinder, "Compass Dial", root, new Vector3(-3f, 1.02f, -5.5f), new Vector3(1.3f, 0.02f, 1.3f), Glyph, false);
            var needle = Empty("Needle Pivot", root, new Vector3(-3f, 1.1f, -5.5f));
            Box("Needle", needle, new Vector3(0, 0, 0.3f), new Vector3(0.12f, 0.06f, 0.9f), Gold, false);
            var gem = Prim(PrimitiveType.Sphere, "Needle Gem", needle, new Vector3(0, 0, 0.8f), Vector3.one * 0.22f, Mat("Gem", new Color(0.3f, 0.5f, 1f), 0.2f, 0.9f, true), false);

            Box("Gate Left", root, new Vector3(-4.5f, 1.5f, -1.5f), new Vector3(5f, 3f, 0.8f), Sand);
            Box("Gate Right", root, new Vector3(4.5f, 1.5f, -1.5f), new Vector3(5f, 3f, 0.8f), Sand);
            Box("Gate Lintel", root, new Vector3(0, 3.2f, -1.5f), new Vector3(14f, 0.4f, 1f), Carved, false);
            var passage = Box("Passage Wall", root, new Vector3(0, 1.5f, -1.5f), new Vector3(4f, 3f, 0.8f), Carved).transform;

            var compass = root.gameObject.AddComponent<CompassPuzzle>();
            compass.sensor = SensorKind.Compass;
            compass.objective = "Trouvez la direction du passage secret";
            compass.tutorial = "L'aiguille magique indique de quel côté tourner.\n\nTenez le téléphone à plat ou droit devant vous et tournez sur vous-même lentement, jusqu'à ce que l'aiguille pointe vers le fond de la salle et devienne dorée.\n\nRestez immobile un instant.";
            compass.hint = "Tournez dans le sens où pointe l'aiguille. Éloignez-vous des objets métalliques et des aimants (coques).";
            compass.fxAnchor = needle;
            compass.cameraPose = Pose("Cam Compass", root, new Vector3(-3f, 6.5f, -11f), new Vector3(-2f, 0.8f, -3f));
            compass.needle = needle; compass.needleGem = gem.GetComponent<Renderer>(); compass.passageWall = passage;

            FireJet(root, new Vector3(-4.5f, 0, 8.5f), 0f);
            FireJet(root, new Vector3(-1.5f, 0, 8.5f), 1.6f);
            FireJet(root, new Vector3(1.5f, 0, 8.5f), 0.8f);
            FireJet(root, new Vector3(4.5f, 0, 8.5f), 2.4f);

            Relic(root, new Vector3(-6f, 0.8f, -10.5f));
            Relic(root, new Vector3(6f, 0.8f, 1.5f));
            Relic(root, new Vector3(-6f, 0.8f, 6.5f));

            foreach (var t in new[] { new Vector3(-6.7f, 2.6f, -6f), new Vector3(6.7f, 2.6f, -9f), new Vector3(-6.7f, 2.6f, 2f), new Vector3(6.7f, 2.6f, 2f), new Vector3(-6.7f, 2.6f, 10f), new Vector3(6.7f, 2.6f, 10f) })
                Torch(root, t);

            var door = Door(root, new Vector3(0f, 1.5f, 11.75f));
            var exit = Exit(root, new Vector3(0f, 0f, 10.7f));
            var st = MakeStation(root, "Boussole", new Vector3(-1.4f, 0f, -5.5f), compass);
            var player = Explorer(root, new Vector3(-4f, 0.05f, -10.5f), 0f, true);
            FollowCamera(cam, player);
            Secret(root, new Vector3(5f, 0.9f, -10.5f), 1);
            AddRoomManager(root, 1, door, exit, new PuzzleStep[] { compass }, new[] { st });
            return scene;
        }

        // ================================================================== Salle 3 — Gyroscope

        static Scene BuildRoom3()
        {
            var (scene, root, cam) = NewRoom("Room3", new Vector3(0, 10f, -16f), new Vector3(0, 0, -6f), 60f, new Color(0.15f, 0.16f, 0.25f));
            Shell(root, 14f, -12f, 12f);

            FloorSweeper(root, new Vector3(-2.5f, 0, -7.5f), 4.6f, 70f);
            FloorSweeper(root, new Vector3(2.5f, 0, -3.5f), 4.6f, -85f);

            var statueAlign = root.gameObject.AddComponent<StatueAlignPuzzle>();
            statueAlign.sensor = SensorKind.Gyroscope;
            statueAlign.objective = "Orientez chaque statue vers son symbole au sol";
            statueAlign.tutorial = "Tenez le téléphone devant vous et faites-le pivoter comme un volant : la statue en surbrillance tourne avec lui.\n\nAlignez son rayon doré sur le symbole lumineux, puis attendez qu'il se fige.";
            statueAlign.hint = "Tournez lentement le téléphone vers la gauche ou la droite, sans le pencher vers l'avant.";
            statueAlign.fxAnchor = Empty("Statues FX", root, new Vector3(0, 1.5f, 4.5f));
            statueAlign.cameraPose = Pose("Cam Statues", root, new Vector3(0, 10f, -3f), new Vector3(0, 0, 4.8f));
            var xs = new[] { -4f, 0f, 4f };
            statueAlign.targetAngles = new[] { 60f, -90f, 150f };
            statueAlign.statues = new Transform[3]; statueAlign.beams = new Renderer[3]; statueAlign.targets = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                var c = new Vector3(xs[i], 0, 4.5f);
                Prim(PrimitiveType.Cylinder, "Ring", root, c + new Vector3(0, 0.01f, 0), new Vector3(3.6f, 0.005f, 3.6f), DarkStone, false);
                Prim(PrimitiveType.Cylinder, "Base", root, c + new Vector3(0, 0.2f, 0), new Vector3(0.9f, 0.2f, 0.9f), Carved);
                var pivot = Empty("Statue " + (i + 1), root, c + new Vector3(0, 0.4f, 0));
                pivot.localRotation = Quaternion.Euler(0, 180f, 0);
                Box("Body", pivot, new Vector3(0, 0.8f, 0), new Vector3(0.6f, 1.6f, 0.5f), Sand, false);
                Box("Head", pivot, new Vector3(0, 1.85f, 0), new Vector3(0.5f, 0.5f, 0.5f), Sand, false);
                Box("Headdress", pivot, new Vector3(0, 2.15f, -0.05f), new Vector3(0.6f, 0.15f, 0.6f), Gold, false);
                Box("Face", pivot, new Vector3(0, 1.85f, 0.26f), new Vector3(0.3f, 0.15f, 0.05f), DarkStone, false);
                var beam = Box("Beam", pivot, new Vector3(0, 0.15f, 1f), new Vector3(0.1f, 0.06f, 1.3f), Mat("Beam", new Color(1f, 0.8f, 0.4f), 0f, 0.5f, true), false);
                float a = statueAlign.targetAngles[i] * Mathf.Deg2Rad;
                var target = Prim(PrimitiveType.Cylinder, "Target", root, c + new Vector3(Mathf.Sin(a) * 1.7f, 0.04f, Mathf.Cos(a) * 1.7f), new Vector3(0.5f, 0.03f, 0.5f), Mat("Target" + i, GameManager.GlyphColors[i] * 0.5f, 0f, 0.5f, true), false);
                statueAlign.statues[i] = pivot; statueAlign.beams[i] = beam.GetComponent<Renderer>(); statueAlign.targets[i] = target.GetComponent<Renderer>();
            }

            SpikeTrap(root, new Vector3(0, 0, 9f), new Vector2(6f, 1.6f), 0.5f, 2.6f);
            Relic(root, new Vector3(-6f, 0.8f, -10.5f));
            Relic(root, new Vector3(6f, 0.8f, -6f));
            Relic(root, new Vector3(6f, 0.8f, 10.5f));

            foreach (var t in new[] { new Vector3(-6.7f, 2.6f, -9f), new Vector3(6.7f, 2.6f, -9f), new Vector3(-6.7f, 2.6f, 2f), new Vector3(6.7f, 2.6f, 2f), new Vector3(-6.7f, 2.6f, 10f), new Vector3(6.7f, 2.6f, 10f) })
                Torch(root, t);

            var door = Door(root, new Vector3(0f, 1.5f, 11.75f));
            var exit = Exit(root, new Vector3(0f, 0f, 10.7f));
            var st = MakeStation(root, "Statues", new Vector3(0f, 0f, 1.6f), statueAlign);
            var player = Explorer(root, new Vector3(0f, 0.05f, -10.5f), 0f, true);
            FollowCamera(cam, player);
            Secret(root, new Vector3(-5.5f, 0.9f, 8f), 2);
            var rm3 = AddRoomManager(root, 2, door, exit, new PuzzleStep[] { statueAlign }, new[] { st });
            rm3.boulder = MakeBoulder(root, 9.4f);
            return scene;
        }

        // ================================================================== Salle 4 — Micro : le monstre

        static MonsterController Monster(Transform root, Vector3 pos, bool shadow = false)
        {
            var skin = shadow ? Mat("ShadowSkin", new Color(0.16f, 0.1f, 0.22f), 0.1f, 0.6f) : Mat("MonsterSkin", new Color(0.24f, 0.36f, 0.2f), 0.05f, 0.55f);
            var belly = shadow ? Mat("ShadowBelly", new Color(0.35f, 0.2f, 0.4f), 0f, 0.5f) : Mat("MonsterBelly", new Color(0.62f, 0.52f, 0.33f), 0f, 0.4f);
            var horn = Mat("MonsterHorn", new Color(0.88f, 0.82f, 0.7f), 0f, 0.5f);
            var teeth = Mat("Teeth", new Color(0.95f, 0.93f, 0.85f), 0f, 0.6f);
            var claw = Mat("Claw", new Color(0.12f, 0.1f, 0.1f), 0.3f, 0.7f);
            var eyeMat = Mat("MonsterEye", new Color(0.3f, 0.05f, 0.02f), 0f, 0.9f, true);

            var go = new GameObject(shadow ? "Shadow Guardian" : "Monster");
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, 180f, 0);   // il regarde vers l'explorateur

            // Jambes (restent au sol)
            Prim(PrimitiveType.Capsule, "Leg L", go.transform, new Vector3(-0.75f, 0.6f, 0f), new Vector3(0.7f, 0.6f, 0.7f), skin, false);
            Prim(PrimitiveType.Capsule, "Leg R", go.transform, new Vector3(0.75f, 0.6f, 0f), new Vector3(0.7f, 0.6f, 0.7f), skin, false);
            Prim(PrimitiveType.Sphere, "Foot L", go.transform, new Vector3(-0.8f, 0.12f, 0.35f), new Vector3(0.6f, 0.25f, 0.9f), skin, false);
            Prim(PrimitiveType.Sphere, "Foot R", go.transform, new Vector3(0.8f, 0.12f, 0.35f), new Vector3(0.6f, 0.25f, 0.9f), skin, false);

            var body = Empty("Body", go.transform, new Vector3(0, 1.7f, 0));
            Prim(PrimitiveType.Sphere, "Torso", body, Vector3.zero, new Vector3(2.5f, 2.3f, 1.8f), skin, false);
            Prim(PrimitiveType.Sphere, "Belly", body, new Vector3(0, -0.2f, 0.62f), new Vector3(1.6f, 1.6f, 0.7f), belly, false);
            for (int i = 0; i < 5; i++)
            {
                var sp = Prim(PrimitiveType.Sphere, "Back Spike", body, new Vector3(0, 0.9f - i * 0.4f, -0.75f - i * 0.05f), new Vector3(0.22f, 0.7f, 0.22f), horn, false);
                sp.transform.localRotation = Quaternion.Euler(-50f, 0, 0);
            }

            var head = Empty("Head", body, new Vector3(0, 1.35f, 0.55f));
            Prim(PrimitiveType.Sphere, "Skull", head, Vector3.zero, new Vector3(1.35f, 1.1f, 1.2f), skin, false);
            Box("Snout", head, new Vector3(0, -0.15f, 0.6f), new Vector3(0.95f, 0.45f, 0.7f), skin, false);
            Box("Brow", head, new Vector3(0, 0.3f, 0.48f), new Vector3(1.1f, 0.18f, 0.3f), skin, false);
            for (int s = -1; s <= 1; s += 2)
            {
                var h = Prim(PrimitiveType.Capsule, "Horn", head, new Vector3(s * 0.5f, 0.6f, -0.05f), new Vector3(0.2f, 0.5f, 0.2f), horn, false);
                h.transform.localRotation = Quaternion.Euler(-25f, 0, -s * 35f);
                for (int t = 0; t < 3; t++)
                    Prim(PrimitiveType.Sphere, "Fang", head, new Vector3(s * (0.12f + t * 0.12f), -0.4f, 0.85f), new Vector3(0.07f, 0.18f, 0.07f), teeth, false);
            }
            var eyeL = Prim(PrimitiveType.Sphere, "Eye L", head, new Vector3(-0.3f, 0.12f, 0.55f), Vector3.one * 0.22f, eyeMat, false);
            var eyeR = Prim(PrimitiveType.Sphere, "Eye R", head, new Vector3(0.3f, 0.12f, 0.55f), Vector3.one * 0.22f, eyeMat, false);
            var el = new GameObject("Eye Light").AddComponent<Light>();
            el.transform.SetParent(head, false);
            el.transform.localPosition = new Vector3(0, 0.1f, 1.2f);
            el.type = LightType.Point; el.color = new Color(1f, 0.2f, 0.1f); el.range = 5f; el.intensity = 0f; el.shadows = LightShadows.None;

            var jaw = Empty("Jaw", head, new Vector3(0, -0.35f, 0.25f));
            Box("Jaw Bone", jaw, new Vector3(0, -0.08f, 0.35f), new Vector3(0.85f, 0.2f, 0.75f), skin, false);
            for (int t = -2; t <= 2; t++)
                Prim(PrimitiveType.Sphere, "Tooth", jaw, new Vector3(t * 0.15f, 0.05f, 0.65f), new Vector3(0.07f, 0.15f, 0.07f), teeth, false);

            Transform Arm(string n, float x)
            {
                var pivot = Empty(n, body, new Vector3(x, 0.55f, 0.15f));
                Prim(PrimitiveType.Capsule, "Upper", pivot, new Vector3(0, -0.6f, 0), new Vector3(0.5f, 0.75f, 0.5f), skin, false);
                Prim(PrimitiveType.Capsule, "Fore", pivot, new Vector3(0, -1.45f, 0.15f), new Vector3(0.42f, 0.6f, 0.42f), skin, false);
                for (int c = -1; c <= 1; c++)
                {
                    var cl = Prim(PrimitiveType.Sphere, "Claw", pivot, new Vector3(c * 0.13f, -2.05f, 0.35f), new Vector3(0.08f, 0.3f, 0.08f), claw, false);
                    cl.transform.localRotation = Quaternion.Euler(35f, 0, 0);
                }
                return pivot;
            }
            var armL = Arm("Arm L", -1.25f);
            var armR = Arm("Arm R", 1.25f);

            var death = FxBurst(go.transform, "Death Dust", new Vector3(0, 2f, 0), new Color(0.35f, 0.15f, 0.45f, 0.8f), 0.4f, 1.1f, 2f, 3f, -0.1f, 150, new Vector3(2.5f, 3f, 2f));

            var m = go.AddComponent<MonsterController>();
            m.body = body; m.head = head; m.jaw = jaw; m.armL = armL; m.armR = armR;
            m.eyes = new[] { eyeL.GetComponent<Renderer>(), eyeR.GetComponent<Renderer>() };
            m.eyeLight = el; m.deathFx = death;
            return m;
        }

        static Scene BuildRoom4()
        {
            var (scene, root, cam) = NewRoom("Room4", new Vector3(0, 10f, -16f), new Vector3(0, 0, -6f), 60f, new Color(0.13f, 0.12f, 0.2f));
            Shell(root, 14f, -12f, 12f);

            SpikeTrap(root, new Vector3(-3.5f, 0, -6.5f), new Vector2(7f, 1.6f), 0f, 2.6f);
            SpikeTrap(root, new Vector3(3.5f, 0, -4f), new Vector2(7f, 1.6f), 1.3f, 2.6f);

            // Arène : couloir où roule la boule d'énergie jusqu'au monstre
            Box("Ball Track", root, new Vector3(0, 0.02f, 4f), new Vector3(1.6f, 0.04f, 7f), DarkStone, false);
            Box("Track Rail L", root, new Vector3(-0.9f, 0.12f, 4f), new Vector3(0.15f, 0.25f, 7f), Gold, false);
            Box("Track Rail R", root, new Vector3(0.9f, 0.12f, 4f), new Vector3(0.15f, 0.25f, 7f), Gold, false);
            Prim(PrimitiveType.Cylinder, "Monster Dais", root, new Vector3(0, 0.15f, 8.6f), new Vector3(5f, 0.15f, 3.5f), Carved);
            var monster = Monster(root, new Vector3(0, 0.3f, 8.6f));

            var start = Empty("Ball Start", root, new Vector3(0, 0.95f, 1f));
            var end = Empty("Ball End", root, new Vector3(0, 0.95f, 6.9f));
            var ball = Prim(PrimitiveType.Sphere, "Energy Ball", root, start.localPosition, Vector3.one * 0.9f, Mat("Energy", new Color(0.4f, 0.75f, 1f), 0.2f, 0.9f, true), false);
            var bl = new GameObject("Ball Light").AddComponent<Light>();
            bl.transform.SetParent(ball.transform, false);
            bl.type = LightType.Point; bl.color = new Color(0.4f, 0.8f, 1f); bl.range = 5f; bl.intensity = 2f; bl.shadows = LightShadows.None;
            ball.SetActive(false);
            var impact = FxBurst(root, "Impact", new Vector3(0, 1.5f, 7f), new Color(0.5f, 0.85f, 1f, 1f), 0.15f, 0.5f, 1.2f, 7f, 0.2f, 120, new Vector3(0.5f, 0.5f, 0.5f));
            impact.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();

            var wake = root.gameObject.AddComponent<MonsterWakePuzzle>();
            wake.sensor = SensorKind.Microphone;
            wake.objective = "Réveillez le monstre endormi";
            wake.tutorial = "Un monstre dort au fond de la salle et bloque la sortie.\n\nParlez fort ou soufflez sur le micro du téléphone (en bas de l'appareil) pendant quelques secondes pour le réveiller.";
            wake.hint = "Parlez fort de façon continue près du bas du téléphone. Si rien ne se passe, augmentez la sensibilité du micro dans Pause.";
            wake.monster = monster;
            wake.fxAnchor = monster.head;
            wake.cameraPose = Pose("Cam Monster", root, new Vector3(0, 4.5f, -2.5f), new Vector3(0, 2.6f, 8f));

            var push = root.gameObject.AddComponent<VoiceBallPuzzle>();
            push.sensor = SensorKind.Microphone;
            push.objective = "Poussez la boule d'énergie avec votre voix pour vaincre le monstre";
            push.tutorial = "Le monstre est réveillé et furieux !\n\nCriez, chantez ou soufflez : plus votre voix est forte, plus la boule d'énergie avance vers lui. Si vous vous taisez, il la repousse.\n\nQuand elle le touche, il est détruit.";
            push.hint = "Gardez un son fort et continu (un « Aaaaah ! » prolongé) jusqu'à ce que la boule touche le monstre.";
            push.ball = ball.transform; push.ballRenderer = ball.GetComponent<Renderer>(); push.ballLight = bl;
            push.startPoint = start; push.monsterPoint = end; push.monster = monster; push.impactFx = impact;
            push.fxAnchor = end;
            push.cameraPose = Pose("Cam Duel", root, new Vector3(0, 5.5f, -3f), new Vector3(0, 1.6f, 5.5f));

            Relic(root, new Vector3(-6f, 0.8f, -10.5f));
            Relic(root, new Vector3(6f, 0.8f, -8.5f));
            Relic(root, new Vector3(-6f, 0.8f, 3f));

            foreach (var t in new[] { new Vector3(-6.7f, 2.6f, -9f), new Vector3(6.7f, 2.6f, -9f), new Vector3(-6.7f, 2.6f, 4f), new Vector3(6.7f, 2.6f, 4f), new Vector3(-3.5f, 2.6f, 11.6f), new Vector3(3.5f, 2.6f, 11.6f) })
                Torch(root, t);

            var door = Door(root, new Vector3(5f, 1.5f, 11.75f));
            var exit = Exit(root, new Vector3(5f, 0f, 10.7f));
            var st = MakeStation(root, "Monstre", new Vector3(0f, 0f, -0.5f), wake);
            var player = Explorer(root, new Vector3(0f, 0.05f, -10.5f), 0f, true);
            FollowCamera(cam, player);
            Quiet(root, new Vector3(0f, 0f, -8.6f), new Vector3(14f, 3f, 3f));
            Secret(root, new Vector3(5.5f, 0.9f, 5f), 3);
            AddRoomManager(root, 3, door, exit, new PuzzleStep[] { wake, push }, new[] { st, null });
            return scene;
        }

        // ================================================================== Salle 5 — Proximité

        static Scene BuildRoom5()
        {
            var (scene, root, cam) = NewRoom("Room5", new Vector3(0, 10f, -16f), new Vector3(0, 0, -6f), 60f, new Color(0.14f, 0.15f, 0.24f));
            Shell(root, 14f, -12f, 12f);

            FireJet(root, new Vector3(-4.5f, 0, -6f), 0f);
            FireJet(root, new Vector3(-1.5f, 0, -6f), 0.8f);
            FireJet(root, new Vector3(1.5f, 0, -6f), 1.6f);
            FireJet(root, new Vector3(4.5f, 0, -6f), 2.4f);

            var runeMat = Mat("Rune", new Color(0.12f, 0.13f, 0.16f), 0f, 0.6f, true);
            var runes = new Renderer[3];
            for (int i = 0; i < 3; i++)
                runes[i] = Prim(PrimitiveType.Cylinder, "Rune " + (i + 1), root, new Vector3(-1.5f + i * 1.5f, 0.03f, 3f), new Vector3(1.1f, 0.03f, 1.1f), runeMat, false).GetComponent<Renderer>();
            Box("Cache", root, new Vector3(0, 0.4f, 5f), new Vector3(1.4f, 0.8f, 1f), Carved);
            var lidPivot = Empty("Cache Lid Pivot", root, new Vector3(0, 0.82f, 5.5f));
            Box("Lid", lidPivot, new Vector3(0, 0.08f, -0.5f), new Vector3(1.5f, 0.16f, 1.05f), Gold, false);
            var treasure = Prim(PrimitiveType.Sphere, "Cache Glow", root, new Vector3(0, 0.75f, 5f), new Vector3(0.5f, 0.15f, 0.5f), Gold, false);

            var prox = root.gameObject.AddComponent<ProximityPuzzle>();
            prox.sensor = SensorKind.Proximity;
            prox.objective = "Activez les trois runes de la cachette";
            prox.tutorial = "Le capteur de proximité se trouve en haut du téléphone, près de l'écouteur.\n\nCouvrez-le avec la main, puis retirez-la. Recommencez trois fois : une rune s'allume à chaque fois.";
            prox.hint = "Passez la main à 2-3 cm au-dessus du haut de l'écran, puis éloignez-la complètement.";
            prox.runes = runes; prox.cacheLid = lidPivot;
            prox.fxAnchor = treasure.transform;
            prox.cameraPose = Pose("Cam Cache", root, new Vector3(0f, 6f, -2f), new Vector3(0f, 0.3f, 4f));

            SpikeTrap(root, new Vector3(-3.5f, 0, 8.5f), new Vector2(7f, 1.6f), 0f, 2.4f);
            SpikeTrap(root, new Vector3(3.5f, 0, 8.5f), new Vector2(7f, 1.6f), 1.2f, 2.4f);
            Relic(root, new Vector3(6f, 0.8f, -10.5f));
            Relic(root, new Vector3(-6f, 0.8f, -2f));
            Relic(root, new Vector3(6f, 0.8f, 6f));

            foreach (var t in new[] { new Vector3(-6.7f, 2.6f, -9f), new Vector3(6.7f, 2.6f, -9f), new Vector3(-6.7f, 2.6f, 2f), new Vector3(6.7f, 2.6f, 2f), new Vector3(-6.7f, 2.6f, 10f), new Vector3(6.7f, 2.6f, 10f) })
                Torch(root, t);

            var door = Door(root, new Vector3(-4.5f, 1.5f, 11.75f));
            var exit = Exit(root, new Vector3(-4.5f, 0f, 10.7f));
            var st = MakeStation(root, "Cachette", new Vector3(0f, 0f, 1f), prox);
            var player = Explorer(root, new Vector3(0f, 0.05f, -10.5f), 0f, true);
            FollowCamera(cam, player);
            Mummy(root, new[] { new Vector3(-5.5f, 0f, -2.5f), new Vector3(5.5f, 0f, -2.5f) });
            Mummy(root, new[] { new Vector3(5.5f, 0f, 6.8f), new Vector3(-5.5f, 0f, 6.8f) });
            Secret(root, new Vector3(-5.5f, 0.9f, 4.5f), 4);
            AddRoomManager(root, 4, door, exit, new PuzzleStep[] { prox }, new[] { st });
            return scene;
        }

        // ================================================================== Salle 6 — Luminosité : le pont invisible

        static Scene BuildRoom6()
        {
            var (scene, root, cam) = NewRoom("Room6", new Vector3(0, 10f, -16f), new Vector3(0, 0, -6f), 60f, new Color(0.06f, 0.065f, 0.12f));
            Box("Floor Front", root, new Vector3(0, -0.25f, -6f), new Vector3(14f, 0.5f, 12f), Stone);
            Box("Floor Back", root, new Vector3(0, -0.25f, 9f), new Vector3(14f, 0.5f, 6f), Stone);
            Box("Chasm", root, new Vector3(0, -7f, 3f), new Vector3(14f, 1f, 6f), Pit);
            Box("Chasm Edge Front", root, new Vector3(0, -0.3f, 0.05f), new Vector3(14f, 0.6f, 0.15f), DarkStone, false);
            Box("Chasm Edge Back", root, new Vector3(0, -0.3f, 5.95f), new Vector3(14f, 0.6f, 0.15f), DarkStone, false);
            Shell(root, 14f, -12f, 12f, false);

            SpikeTrap(root, new Vector3(0, 0, -7f), new Vector2(14f, 1.6f), 0f, 3f);

            // Pont de cristal : invisible et non solide tant que la lumière ne l'a pas révélé
            var crystal = Mat("Crystal", new Color(0.35f, 0.6f, 0.8f), 0.2f, 0.95f, true);
            var parts = new List<Renderer>();
            var solids = new List<Collider>();
            for (int i = 0; i < 6; i++)
            {
                var seg = Box("Crystal Bridge " + (i + 1), root, new Vector3(0, -0.15f, 0.5f + i), new Vector3(2.6f, 0.3f, 0.96f), crystal);
                parts.Add(seg.GetComponent<Renderer>());
                solids.Add(seg.GetComponent<Collider>());
            }
            var rl = new GameObject("Reveal Light").AddComponent<Light>();
            rl.transform.SetParent(root, false);
            rl.transform.localPosition = new Vector3(0, 2f, 3f);
            rl.type = LightType.Point; rl.color = new Color(0.5f, 0.85f, 1f); rl.range = 7f; rl.intensity = 0f;

            var light = root.gameObject.AddComponent<LightRevealPuzzle>();
            light.sensor = SensorKind.Light;
            light.objective = "Révélez le pont de cristal au-dessus du gouffre";
            light.tutorial = "Un pont invisible enjambe le gouffre. Il n'apparaît qu'avec un changement de lumière.\n\nCouvrez le haut de l'écran avec la main pour faire l'obscurité, ou éclairez-le avec une lampe, et maintenez quelques secondes.";
            light.hint = "Le capteur de lumière est près de la caméra avant. Couvrez-le complètement ou approchez-le d'une lampe.";
            light.hiddenParts = parts.ToArray(); light.solidParts = solids.ToArray(); light.revealLight = rl;
            light.fxAnchor = Empty("Bridge FX", root, new Vector3(0, 0.5f, 3f));
            light.cameraPose = Pose("Cam Bridge", root, new Vector3(-3f, 6f, -4f), new Vector3(0, 0f, 3.5f));

            Relic(root, new Vector3(-6f, 0.8f, -10.5f));
            Relic(root, new Vector3(0f, 0.9f, 3f));
            Relic(root, new Vector3(6f, 0.8f, 10.5f));

            foreach (var t in new[] { new Vector3(-6.7f, 2.6f, -9f), new Vector3(6.7f, 2.6f, -9f), new Vector3(-6.7f, 2.6f, 9f), new Vector3(6.7f, 2.6f, 9f) })
                Torch(root, t);

            var door = Door(root, new Vector3(0f, 1.5f, 11.75f));
            var exit = Exit(root, new Vector3(0f, 0f, 10.7f));
            var st = MakeStation(root, "Gouffre", new Vector3(0f, 0f, -1.5f), light);
            var player = Explorer(root, new Vector3(0f, 0.05f, -10.5f), 0f, true, true);
            Secret(root, new Vector3(5.5f, 0.9f, -4f), 5);
            FollowCamera(cam, player);
            AddRoomManager(root, 5, door, exit, new PuzzleStep[] { light }, new[] { st });
            return scene;
        }

        // ================================================================== Salle 7 — Caméra : l'esprit Coyote

        static CoyoteSpirit Coyote(Transform root, Vector3 pos)
        {
            var mat = Mat("Spirit", new Color(0.4f, 0.6f, 0.8f), 0f, 0.7f, true);
            var dark = Mat("Black", new Color(0.05f, 0.05f, 0.05f), 0f, 0.6f);
            var eye = Mat("SpiritEye", new Color(0.9f, 0.95f, 1f), 0f, 0.9f, true);
            eye.SetColor("_EmissionColor", new Color(0.8f, 0.95f, 1f) * 3f);
            var spirit = new List<Renderer>();

            var go = new GameObject("Coyote Spirit");
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, 180f, 0);

            GameObject P(PrimitiveType t, string n, Transform parent, Vector3 p, Vector3 s, Vector3 rot)
            {
                var o = Prim(t, n, parent, p, s, mat, false);
                o.transform.localRotation = Quaternion.Euler(rot);
                spirit.Add(o.GetComponent<Renderer>());
                return o;
            }

            P(PrimitiveType.Sphere, "Haunch", go.transform, new Vector3(0, 0.45f, -0.25f), new Vector3(0.85f, 0.75f, 1f), Vector3.zero);
            P(PrimitiveType.Capsule, "Chest", go.transform, new Vector3(0, 0.95f, 0.2f), new Vector3(0.55f, 0.55f, 0.55f), new Vector3(-20, 0, 0));
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Capsule, "Front Leg", go.transform, new Vector3(s * 0.15f, 0.38f, 0.45f), new Vector3(0.14f, 0.38f, 0.14f), Vector3.zero);
                P(PrimitiveType.Sphere, "Back Paw", go.transform, new Vector3(s * 0.3f, 0.1f, 0.05f), new Vector3(0.22f, 0.15f, 0.4f), Vector3.zero);
            }

            var head = Empty("Head", go.transform, new Vector3(0, 1.5f, 0.4f));
            P(PrimitiveType.Sphere, "Skull", head, Vector3.zero, new Vector3(0.48f, 0.42f, 0.5f), Vector3.zero);
            P(PrimitiveType.Capsule, "Snout", head, new Vector3(0, -0.06f, 0.33f), new Vector3(0.2f, 0.22f, 0.2f), new Vector3(90, 0, 0));
            Prim(PrimitiveType.Sphere, "Nose", head, new Vector3(0, -0.03f, 0.56f), Vector3.one * 0.08f, dark, false);
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Cube, "Ear", head, new Vector3(s * 0.15f, 0.3f, -0.05f), new Vector3(0.12f, 0.32f, 0.05f), new Vector3(0, 0, -s * 15f));
                Prim(PrimitiveType.Sphere, "Eye", head, new Vector3(s * 0.11f, 0.07f, 0.21f), Vector3.one * 0.07f, eye, false);
            }
            var jaw = Empty("Jaw", head, new Vector3(0, -0.15f, 0.2f));
            P(PrimitiveType.Cube, "Jaw Bone", jaw, new Vector3(0, 0, 0.18f), new Vector3(0.15f, 0.05f, 0.32f), Vector3.zero);

            var tail = Empty("Tail", go.transform, new Vector3(0, 0.35f, -0.7f));
            P(PrimitiveType.Capsule, "Tail Fur", tail, new Vector3(0, 0.15f, -0.3f), new Vector3(0.2f, 0.42f, 0.2f), new Vector3(60, 0, 0));

            // Aura d'esprit
            var aura = new GameObject("Aura");
            aura.transform.SetParent(go.transform, false);
            aura.transform.localPosition = new Vector3(0, 0.9f, 0);
            var ps = aura.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = new Color(0.5f, 0.85f, 1f, 0.8f);
            main.gravityModifier = -0.05f;
            var em = ps.emission; em.rateOverTime = 30f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.8f;
            aura.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
            var al = new GameObject("Spirit Light").AddComponent<Light>();
            al.transform.SetParent(go.transform, false);
            al.transform.localPosition = new Vector3(0, 1.4f, 0.8f);
            al.type = LightType.Point; al.color = new Color(0.5f, 0.8f, 1f); al.range = 5f; al.intensity = 2f; al.shadows = LightShadows.None;

            var c = go.AddComponent<CoyoteSpirit>();
            c.head = head; c.jaw = jaw; c.tail = tail;
            c.spiritParts = spirit.ToArray();
            return c;
        }

        static Transform Key(Transform parent, string name, Vector3 pos)
        {
            var k = Empty(name, parent, pos);
            var ring = Prim(PrimitiveType.Cylinder, "Ring", k, new Vector3(0, 0.25f, 0), new Vector3(0.28f, 0.03f, 0.28f), Gold, false);
            ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Box("Shaft", k, new Vector3(0, -0.05f, 0), new Vector3(0.06f, 0.5f, 0.06f), Gold, false);
            Box("Tooth 1", k, new Vector3(0.07f, -0.22f, 0), new Vector3(0.1f, 0.05f, 0.05f), Gold, false);
            Box("Tooth 2", k, new Vector3(0.07f, -0.12f, 0), new Vector3(0.07f, 0.05f, 0.05f), Gold, false);
            return k;
        }

        static Scene BuildRoom7()
        {
            var (scene, root, cam) = NewRoom("Room7", new Vector3(0, 10f, -16f), new Vector3(0, 0, -6f), 60f, new Color(0.13f, 0.14f, 0.24f));
            Shell(root, 14f, -12f, 12f);

            // Mur intérieur avec la dernière porte ; l'artefact est derrière
            Box("Inner Wall L", root, new Vector3(-4.3f, 2.5f, 6f), new Vector3(5.4f, 5f, 0.8f), Sand);
            Box("Inner Wall R", root, new Vector3(4.3f, 2.5f, 6f), new Vector3(5.4f, 5f, 0.8f), Sand);
            Box("Inner Wall Top", root, new Vector3(0, 4.1f, 6f), new Vector3(3.2f, 1.8f, 0.8f), Sand);
            var door = Door(root, new Vector3(0f, 1.5f, 6f));
            var lockPlate = Box("Lock", root, new Vector3(0.7f, 1.4f, 5.7f), new Vector3(0.35f, 0.45f, 0.08f), Glyph, false);
            var keyInLock = Key(root, "Key In Lock", new Vector3(0.7f, 1.4f, 5.55f));
            keyInLock.localRotation = Quaternion.Euler(-90, 0, 0);
            keyInLock.gameObject.SetActive(false);

            Prim(PrimitiveType.Cylinder, "Pedestal", root, new Vector3(0, 0.6f, 9.5f), new Vector3(1.2f, 0.6f, 1.2f), Carved);
            Prim(PrimitiveType.Cylinder, "Pedestal Ring", root, new Vector3(0, 1.22f, 9.5f), new Vector3(1.3f, 0.04f, 1.3f), Gold, false);
            var art = Box("Artefact", root, new Vector3(0, 1.8f, 9.5f), Vector3.one * 0.55f, Gold, false);
            art.transform.rotation = Quaternion.Euler(45, 0, 45);
            art.AddComponent<Spinner>();
            var artL = new GameObject("Artefact Light").AddComponent<Light>();
            artL.transform.SetParent(root, false);
            artL.transform.localPosition = new Vector3(0, 2.4f, 9f);
            artL.type = LightType.Point; artL.color = new Color(1f, 0.75f, 0.35f); artL.range = 6f; artL.intensity = 3f;

            // Esprit Coyote sur son rocher
            Prim(PrimitiveType.Sphere, "Coyote Rock", root, new Vector3(-4f, 0.1f, 1.5f), new Vector3(2.2f, 0.6f, 1.8f), DarkStone);
            var coyote = Coyote(root, new Vector3(-4f, 0.35f, 1.5f));
            coyote.keyProp = Key(root, "Key", new Vector3(-4f, 2.5f, 1.5f));

            var photos = root.gameObject.AddComponent<PhotoPuzzle>();
            photos.sensor = SensorKind.Camera;
            photos.objective = "Accomplissez les 3 missions photo du Coyote";
            photos.tutorial = "« Je suis l'esprit Coyote, gardien de l'artefact. Pour gagner ma clé, rapporte-moi trois images du monde des vivants. »\n\nLa caméra du téléphone s'ouvre : photographiez dans la vraie vie un objet de la couleur demandée, bien au centre du cadre doré.\n\nLes photos sont analysées sur le téléphone puis oubliées : rien n'est enregistré.";
            photos.hint = "Approchez-vous d'un objet de la bonne couleur pour qu'il remplisse le cadre doré, avec assez de lumière.";
            photos.coyote = coyote;
            photos.fxAnchor = coyote.head;
            photos.cameraPose = Pose("Cam Coyote", root, new Vector3(-2.6f, 2.6f, -2.2f), new Vector3(-4f, 1.5f, 1.5f));

            var password = root.gameObject.AddComponent<PasswordPuzzle>();
            password.usesSensor = false;
            password.objective = "Répétez au Coyote son mot de passe";
            password.tutorial = "Le Coyote veut vérifier que vous avez bien retenu son mot de passe.\n\nTouchez les syllabes dans l'ordre où il vous les a données.";
            password.hint = "Le mot de passe est formé des 3 fragments obtenus avec les photos, dans l'ordre des missions.";
            password.coyote = coyote; password.photos = photos;
            password.fxAnchor = coyote.head;
            password.cameraPose = photos.cameraPose;

            // Boss final : le gardien des ténèbres surgit devant la porte
            var boss = Monster(root, new Vector3(0, 0.3f, 4.2f), true);
            var bossFight = root.gameObject.AddComponent<BossFightPuzzle>();
            bossFight.sensor = SensorKind.Light;
            bossFight.objective = "Vainquez le gardien des ténèbres";
            bossFight.tutorial = "Le gardien des ténèbres surgit du sol pour protéger l'artefact !\n\n<b>1.</b> Éblouissez-le : couvrez ou éclairez le capteur de lumière.\n<b>2.</b> Esquivez ses coups : penchez le téléphone du côté opposé à la plaque rouge.\n<b>3.</b> Achevez-le : criez pour pousser la boule d'énergie jusqu'à lui.";
            bossFight.hint = "Regardez la plaque qui rougit : c'est là qu'il va frapper. Penchez-vous de l'autre côté.";
            bossFight.boss = boss; bossFight.coyote = coyote;
            bossFight.bossHidden = Empty("Boss Hidden", root, new Vector3(0, -5f, 4.2f));
            bossFight.bossStand = Empty("Boss Stand", root, new Vector3(0, 0.3f, 4.2f));
            bossFight.arenaCenter = Empty("Arena Center", root, new Vector3(0, 0.05f, -0.5f));
            var slamMat = Mat("SlamMarker", new Color(0.25f, 0.08f, 0.06f), 0.2f, 0.4f, true);
            bossFight.slamMarkerLeft = Box("Slam Marker L", root, new Vector3(-1.6f, 0.02f, -0.5f), new Vector3(2.2f, 0.03f, 2.2f), slamMat, false).GetComponent<Renderer>();
            bossFight.slamMarkerRight = Box("Slam Marker R", root, new Vector3(1.6f, 0.02f, -0.5f), new Vector3(2.2f, 0.03f, 2.2f), slamMat, false).GetComponent<Renderer>();
            var bball = Prim(PrimitiveType.Sphere, "Boss Energy Ball", root, new Vector3(0, 0.95f, 0.6f), Vector3.one * 0.9f, Mat("Energy", new Color(0.4f, 0.75f, 1f), 0.2f, 0.9f, true), false);
            var bbl = new GameObject("Ball Light").AddComponent<Light>();
            bbl.transform.SetParent(bball.transform, false);
            bbl.type = LightType.Point; bbl.color = new Color(0.4f, 0.8f, 1f); bbl.range = 5f; bbl.intensity = 2f; bbl.shadows = LightShadows.None;
            bossFight.ball = bball.transform; bossFight.ballRenderer = bball.GetComponent<Renderer>(); bossFight.ballLight = bbl;
            bossFight.ballStart = Empty("Boss Ball Start", root, new Vector3(0, 0.95f, 0.8f));
            bossFight.ballEnd = Empty("Boss Ball End", root, new Vector3(0, 0.95f, 3.1f));
            var bimpact = FxBurst(root, "Boss Impact", new Vector3(0, 1.5f, 3.2f), new Color(0.6f, 0.4f, 1f, 1f), 0.15f, 0.5f, 1.2f, 8f, 0.2f, 160, new Vector3(0.5f, 0.5f, 0.5f));
            bimpact.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
            bossFight.impactFx = bimpact;
            bossFight.fxAnchor = bossFight.bossStand;
            bossFight.cameraPose = Pose("Cam Boss", root, new Vector3(0, 4.2f, -5.8f), new Vector3(0, 2f, 2.8f));

            var key = root.gameObject.AddComponent<KeyDoorPuzzle>();
            key.usesSensor = false;
            key.objective = "Ouvrez la dernière porte avec la clé";
            key.tutorial = "Vous avez la clé dorée du Coyote. Glissez-la dans la serrure de la porte.";
            key.hint = "Approchez-vous de la porte au fond de la salle.";
            key.keyInLock = keyInLock; key.lockGlow = lockPlate.GetComponent<Renderer>();
            key.fxAnchor = keyInLock;
            key.cameraPose = Pose("Cam Door", root, new Vector3(2.5f, 3.5f, 0.5f), new Vector3(0, 1.8f, 6f));

            Relic(root, new Vector3(-6f, 0.8f, -10.5f));
            Relic(root, new Vector3(6f, 0.8f, -6f));
            Relic(root, new Vector3(-6f, 0.8f, 3f));
            SpikeTrap(root, new Vector3(0, 0, -6f), new Vector2(14f, 1.6f), 0f, 3f);

            foreach (var t in new[] { new Vector3(-6.7f, 2.6f, -9f), new Vector3(6.7f, 2.6f, -9f), new Vector3(-6.7f, 2.6f, 2f), new Vector3(6.7f, 2.6f, 2f), new Vector3(-3f, 2.6f, 5.5f), new Vector3(3f, 2.6f, 5.5f) })
                Torch(root, t);

            var exit = Exit(root, new Vector3(0f, 0f, 8.6f));
            var st1 = MakeStation(root, "Coyote", new Vector3(-4f, 0f, -1.2f), photos);
            var st3 = MakeStation(root, "Porte", new Vector3(0f, 0f, 4.6f), key);
            var player = Explorer(root, new Vector3(0f, 0.05f, -10.5f), 0f, true);
            FollowCamera(cam, player);
            Secret(root, new Vector3(5.5f, 0.9f, 0f), 6);
            AddRoomManager(root, 6, door, exit, new PuzzleStep[] { photos, password, bossFight, key }, new[] { st1, null, null, st3 });
            return scene;
        }
    }
}
