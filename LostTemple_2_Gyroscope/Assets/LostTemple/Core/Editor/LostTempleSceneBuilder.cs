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
    /// Génère les 8 scènes (menu + 7 salles, une par capteur). Ce fichier (Core) contient les outils communs ;
    /// la construction de chaque salle est dans le dossier Editor du module de son capteur (classe partielle).
    /// : décor texturé, explorateur jouable, pièges, reliques,
    /// stations d'énigmes et toutes les références déjà branchées. Menu : Lost Temple > Générer toutes les scènes.
    /// Les graphistes peuvent ensuite remplacer les formes simples par de vrais modèles.
    /// Attention : regénérer écrase les scènes existantes.
    /// </summary>
    public static partial class LostTempleSceneBuilder
    {
        const string CoreDir = "Assets/LostTemple/Core";
        const string ModulesDir = "Assets/LostTemple/Modules";
        const string MatDir = CoreDir + "/Materials";
        const string TexDir = CoreDir + "/Textures";
        const string MeshDir = CoreDir + "/Meshes";
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
            foreach (var d in new[] { MatDir, TexDir, MeshDir }) Directory.CreateDirectory(d);
            mats.Clear();
            meshes.Clear();
            postProfile = null;
            rng = new System.Random(42);

            // Le menu, puis chaque salle déclarée par un module ([RoomBuilder]) présente dans ce projet.
            var scenes = new List<EditorBuildSettingsScene> { Save(BuildMenu(), GameManager.MenuScene, CoreDir + "/Scenes") };
            var builders = new List<(RoomBuilderAttribute info, System.Reflection.MethodInfo method)>();
            foreach (var mi in typeof(LostTempleSceneBuilder).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
            {
                var info = (RoomBuilderAttribute)System.Attribute.GetCustomAttribute(mi, typeof(RoomBuilderAttribute));
                if (info != null) builders.Add((info, mi));
            }
            builders.Sort((a, b) => a.info.Order.CompareTo(b.info.Order));
            foreach (var b in builders)
                scenes.Add(Save((Scene)b.method.Invoke(null, null), b.info.Scene, ModulesDir + "/" + b.info.Module + "/Scenes"));
            EditorBuildSettings.scenes = scenes.ToArray();
            var modules = new HashSet<string>();
            foreach (var b in builders) modules.Add(b.info.Module);
            ConfigurePlayer(modules.Count == 1 ? new List<string>(modules)[0] : null);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene($"{CoreDir}/Scenes/{GameManager.MenuScene}.unity");
            Debug.Log($"[Lost Temple] {scenes.Count} scènes générées et ajoutées aux Build Settings.");
        }

        /// <summary>Nom et identifiant de l'application : propres à chaque projet de module (sauvegardes séparées).</summary>
        static void ConfigurePlayer(string singleModule)
        {
            PlayerSettings.productName = singleModule == null ? "Lost Temple" : "Lost Temple — " + singleModule.Substring(2).Replace('_', ' ');
            PlayerSettings.companyName = "LostTempleTeam";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            string id = singleModule == null ? "losttemple" : "losttemple." + singleModule.Substring(2).ToLowerInvariant();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.losttempleteam." + id);
        }

        static EditorBuildSettingsScene Save(Scene s, string name, string dir)
        {
            Directory.CreateDirectory(dir);
            string path = $"{dir}/{name}.unity";
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
            rm.storyIndex = index;
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

        // ================================================================== Monstre (salle du micro et boss final)

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

        static void Secret(Transform root, Vector3 pos, int storyIndex)
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
            sr.gesture = Journal.Secrets[storyIndex];
            sr.relic = relicGo;
            sr.revealFx = FxBurst(go.transform, "Reveal FX", Vector3.zero, new Color(0.5f, 0.85f, 1f, 1f), 0.05f, 0.18f, 1.2f, 3f, -0.2f, 80, Vector3.one * 0.5f);
            sr.revealFx.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
        }
    }

    /// <summary>
    /// Marque la fonction qui construit une salle. Le générateur (Core) crée toutes les salles présentes
    /// dans le projet : le jeu complet en a 7, un projet de module seulement la ou les siennes.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class RoomBuilderAttribute : System.Attribute
    {
        public readonly int Order;
        public readonly string Scene;
        public readonly string Module;
        public RoomBuilderAttribute(int order, string scene, string module) { Order = order; Scene = scene; Module = module; }
    }
}
