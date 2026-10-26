using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LostTemple.EditorTools
{
    /// <summary>
    /// Génère les 5 scènes du prototype (menu + 4 salles) avec des formes simples, les lumières et
    /// toutes les références des énigmes déjà branchées. Menu : Lost Temple > Générer toutes les scènes.
    /// Les graphistes peuvent ensuite remplacer les cubes par de vrais modèles sans toucher au code.
    /// Attention : regénérer écrase les scènes existantes.
    /// </summary>
    public static class LostTempleSceneBuilder
    {
        const string SceneDir = "Assets/LostTemple/Scenes";
        const string MatDir = "Assets/LostTemple/Materials";

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        [MenuItem("Lost Temple/Générer toutes les scènes")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAllNoPrompt();
        }

        public static void BuildAllNoPrompt()
        {
            System.IO.Directory.CreateDirectory(SceneDir);
            System.IO.Directory.CreateDirectory(MatDir);
            mats.Clear();
            postProfile = null;

            var scenes = new List<EditorBuildSettingsScene>
            {
                Save(BuildMenu(), GameManager.MenuScene),
                Save(BuildRoom1(), GameManager.RoomScenes[0]),
                Save(BuildRoom2(), GameManager.RoomScenes[1]),
                Save(BuildRoom3(), GameManager.RoomScenes[2]),
                Save(BuildRoom4(), GameManager.RoomScenes[3]),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene($"{SceneDir}/{GameManager.MenuScene}.unity");
            Debug.Log("[Lost Temple] 5 scènes générées et ajoutées aux Build Settings.");
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

        // ================================================================== Matériaux

        static Material Mat(string name, Color c, float metallic = 0f, float smooth = 0.2f, bool glow = false)
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

        static Material Stone => Mat("Stone", new Color(0.36f, 0.31f, 0.25f));
        static Material DarkStone => Mat("DarkStone", new Color(0.16f, 0.14f, 0.12f));
        static Material Sand => Mat("Sandstone", new Color(0.58f, 0.47f, 0.32f));
        static Material Carved => Mat("Carved", new Color(0.45f, 0.36f, 0.24f), 0f, 0.3f, true);
        static Material Pit => Mat("Pit", new Color(0.02f, 0.02f, 0.04f));
        static Material Gold => Mat("Gold", new Color(1f, 0.76f, 0.3f), 0.85f, 0.7f, true);
        static Material Glyph => Mat("Glyph", new Color(0.12f, 0.11f, 0.12f), 0f, 0.4f, true);
        static Material Flame => Mat("Flame", new Color(1f, 0.55f, 0.15f), 0f, 0f, true);

        // ================================================================== Post-traitement & particules

        static VolumeProfile postProfile;

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
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            var tex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (tex != null) m.SetTexture("_BaseMap", tex);
            // Transparent additif : les étincelles et la poussière s'illuminent sans assombrir le décor.
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(m, path);
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

        static GameObject Box(string n, Transform p, Vector3 pos, Vector3 s, Material m, bool col = true) => Prim(PrimitiveType.Cube, n, p, pos, s, m, col);

        static Transform Empty(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go.transform;
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
            Prim(PrimitiveType.Sphere, "Flame", t, new Vector3(0, 0.05f, 0), new Vector3(0.22f, 0.32f, 0.22f), FlameLit(), false);
            var lgo = new GameObject("Light");
            lgo.transform.SetParent(t, false);
            lgo.transform.localPosition = new Vector3(0, 0.3f, 0);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.6f, 0.25f); l.range = 7f; l.intensity = 2.5f;
            l.shadows = LightShadows.None;
            lgo.AddComponent<TorchFlicker>();
        }

        static Material FlameLit()
        {
            var m = Flame;
            m.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f) * 4f);
            return m;
        }

        // ================================================================== Scène de base

        static (Scene scene, Transform root) NewRoom(string name, Vector3 camPos, Vector3 lookAt, float fov, Color ambient)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(name).transform;

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.transform.position = camPos;
            cam.transform.LookAt(lookAt);
            var c = cam.AddComponent<Camera>();
            c.fieldOfView = fov;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.02f, 0.025f, 0.05f);
            c.farClipPlane = 80f;
            cam.AddComponent<AudioListener>();
            cam.AddComponent<CameraSway>().amplitude = 0.08f;
            var camData = cam.AddComponent<UniversalAdditionalCameraData>();
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
            RenderSettings.fogDensity = 0.025f;
            return (scene, root);
        }

        static void Shell(Transform root, float width, float depth, float backZ)
        {
            Box("Floor", root, new Vector3(0, -0.25f, backZ - depth / 2f), new Vector3(width, 0.5f, depth), Stone);
            Box("Wall Back", root, new Vector3(0, 3f, backZ + 0.5f), new Vector3(width, 6f, 1f), Sand);
            Box("Wall Left", root, new Vector3(-width / 2f - 0.5f, 3f, backZ - depth / 2f), new Vector3(1f, 6f, depth), Sand);
            Box("Wall Right", root, new Vector3(width / 2f + 0.5f, 3f, backZ - depth / 2f), new Vector3(1f, 6f, depth), Sand);
            for (int i = -1; i <= 1; i += 2)
            {
                Box("Pillar", root, new Vector3(i * (width / 2f - 0.6f), 3f, backZ - 0.6f), new Vector3(0.9f, 6f, 0.9f), Stone);
            }
        }

        static Transform Door(Transform root, Vector3 pos, out TextMesh symbolText, out Renderer plate)
        {
            Box("Door Frame", root, pos + new Vector3(0, 1.75f, 0.05f), new Vector3(2.6f, 0.4f, 0.8f), DarkStone);
            var door = Box("Door", root, pos, new Vector3(2f, 3f, 0.5f), Carved).transform;
            plate = Box("Symbol Plate", root, pos + new Vector3(0, 2.45f, -0.1f), new Vector3(1.8f, 0.7f, 0.15f), Glyph, false).GetComponent<Renderer>();
            symbolText = Text3D("Symbol Text", root, pos + new Vector3(0, 2.45f, -0.25f), "", 0.09f, Color.white);
            return door;
        }

        static RoomManager AddRoomManager(Transform root, int index, Transform door, TextMesh text, Renderer plate, params PuzzleStep[] steps)
        {
            Dust(root, new Vector3(0, 2.5f, 1f), new Vector3(11f, 4f, 13f));
            var rm = root.gameObject.AddComponent<RoomManager>();
            rm.sparks = Sparks(root);
            rm.roomIndex = index;
            rm.door = door; rm.symbolText = text; rm.symbolPlate = plate;
            rm.steps = steps;
            return rm;
        }

        // ================================================================== Menu

        static Scene BuildMenu()
        {
            var (scene, root) = NewRoom("Temple Facade", new Vector3(0, 2.2f, -9f), new Vector3(0, 3.2f, 0), 60f, new Color(0.07f, 0.08f, 0.15f));
            Box("Ground", root, new Vector3(0, -0.25f, 0), new Vector3(30, 0.5f, 30), Stone);
            for (int i = 0; i < 3; i++)
                Box("Step", root, new Vector3(0, 0.2f + i * 0.4f, 1.5f + i * 0.8f), new Vector3(10 - i, 0.4f, 1.6f), DarkStone);
            Box("Facade", root, new Vector3(0, 5f, 5f), new Vector3(12, 10, 1.5f), Sand);
            Box("Entrance", root, new Vector3(0, 2.8f, 4.2f), new Vector3(3.2f, 4.4f, 0.2f), Pit, false);
            Box("Lintel", root, new Vector3(0, 5.3f, 4.1f), new Vector3(4.4f, 0.6f, 0.4f), Stone);
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                Prim(PrimitiveType.Cylinder, "Column", root, new Vector3(i * 2.4f, 3.2f, 3.6f), new Vector3(0.8f, 3.2f, 0.8f), Stone);
            }
            var art = Box("Artefact", root, new Vector3(0, 2.6f, 4f), new Vector3(0.5f, 0.5f, 0.5f), Gold, false);
            art.transform.rotation = Quaternion.Euler(45, 0, 45);
            var al = new GameObject("Artefact Light").AddComponent<Light>();
            al.transform.position = new Vector3(0, 2.6f, 3.5f);
            al.type = LightType.Point; al.color = new Color(1f, 0.75f, 0.35f); al.range = 6f; al.intensity = 4f;
            Torch(root, new Vector3(-2.6f, 2.6f, 3.2f));
            Torch(root, new Vector3(2.6f, 2.6f, 3.2f));
            Dust(root, new Vector3(0, 3f, 1f), new Vector3(12f, 6f, 8f));
            return scene;
        }

        // ================================================================== Salle 1 — Accéléromètre

        static Scene BuildRoom1()
        {
            var (scene, root) = NewRoom("Room1", new Vector3(1f, 13f, -11f), new Vector3(1f, 0f, 1f), 55f, new Color(0.16f, 0.17f, 0.26f));
            Box("Pit", root, new Vector3(1, -6f, 1f), new Vector3(16, 1, 22), Pit);
            Box("Wall Back", root, new Vector3(1, 1f, 8f), new Vector3(14, 10, 1), Sand);
            Box("Wall Left", root, new Vector3(-6f, 0f, 1f), new Vector3(1, 12, 15), Sand);
            Box("Wall Right", root, new Vector3(8f, 0f, 1f), new Vector3(1, 12, 15), Sand);

            Box("Start Platform", root, new Vector3(0, 0, -6f), new Vector3(3, 0.4f, 2.2f), Stone);
            Box("Bridge 1", root, new Vector3(0, 0, -2.75f), new Vector3(1.5f, 0.4f, 4.6f), Sand);
            Box("Bridge 2", root, new Vector3(1, 0, 0), new Vector3(3.5f, 0.4f, 1.5f), Sand);
            Box("Bridge 3", root, new Vector3(2, 0, 2.75f), new Vector3(1.5f, 0.4f, 4.6f), Sand);
            Box("Goal Platform", root, new Vector3(2, 0, 6.2f), new Vector3(3.2f, 0.4f, 2.6f), Stone);
            var goalPlate = Box("Goal Plate", root, new Vector3(2, 0.22f, 6.2f), new Vector3(1.3f, 0.05f, 1.3f), Gold, false).GetComponent<Renderer>();
            var goal = Box("Goal Trigger", root, new Vector3(2, 0.7f, 6.2f), new Vector3(1.3f, 1f, 1.3f), Gold);
            Object.DestroyImmediate(goal.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(goal.GetComponent<MeshFilter>());
            goal.GetComponent<Collider>().isTrigger = true;

            foreach (var p in new[] { new Vector3(-3, 0, -4), new Vector3(5, 0, -4), new Vector3(-3, 0, 5), new Vector3(5, 0, 5) })
            {
                Box("Pillar", root, new Vector3(p.x, -3f, p.z), new Vector3(0.9f, 7f, 0.9f), Stone);
                Torch(root, new Vector3(p.x, 1.1f, p.z));
            }

            // Piège : barre qui tourne au-dessus du dernier pont
            Prim(PrimitiveType.Cylinder, "Sweeper Axis", root, new Vector3(2, 0.45f, 3f), new Vector3(0.35f, 0.25f, 0.35f), Gold, false);
            var sweeper = Box("Sweeper", root, new Vector3(2, 0.5f, 3f), new Vector3(2.4f, 0.25f, 0.22f), Carved);
            sweeper.AddComponent<Rigidbody>();
            sweeper.AddComponent<Sweeper>().speed = 55f;

            var door = Door(root, new Vector3(2, 1.5f, 7.3f), out var symText, out var plate);

            var spawn = Empty("Spawn", root, new Vector3(0, 0.6f, -6f));
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
            puzzle.ball = rb; puzzle.spawn = spawn; puzzle.goal = goal.GetComponent<Collider>(); puzzle.goalPlate = goalPlate;
            goal.AddComponent<GoalTrigger>().puzzle = puzzle;

            AddRoomManager(root, 0, door, symText, plate, puzzle);
            return scene;
        }

        // ================================================================== Salle 2 — Boussole + Gyroscope

        static Scene BuildRoom2()
        {
            var (scene, root) = NewRoom("Room2", new Vector3(0, 10f, -9f), new Vector3(0, 0, 2f), 60f, new Color(0.08f, 0.09f, 0.16f));
            Shell(root, 12f, 16f, 7.5f);

            // Boussole magique
            Prim(PrimitiveType.Cylinder, "Compass Pedestal", root, new Vector3(0, 0.5f, -2f), new Vector3(1.4f, 0.5f, 1.4f), Stone);
            Prim(PrimitiveType.Cylinder, "Compass Dial", root, new Vector3(0, 1.02f, -2f), new Vector3(1.3f, 0.02f, 1.3f), Glyph, false);
            var needle = Empty("Needle Pivot", root, new Vector3(0, 1.1f, -2f));
            Box("Needle", needle, new Vector3(0, 0, 0.3f), new Vector3(0.12f, 0.06f, 0.9f), Gold, false);
            var gem = Prim(PrimitiveType.Sphere, "Needle Gem", needle, new Vector3(0, 0, 0.8f), Vector3.one * 0.22f, Mat("Gem", new Color(0.3f, 0.5f, 1f), 0.2f, 0.9f, true), false);

            // Passage secret (s'ouvre avec la boussole) et sa fresque
            Box("Fresco", root, new Vector3(-3.5f, 1.6f, 7.45f), new Vector3(2.2f, 2.8f, 0.05f), Mat("Fresco", new Color(0.3f, 0.45f, 0.6f), 0f, 0.4f, true), false)
                .GetComponent<Renderer>().sharedMaterial.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 0.8f));
            var passage = Box("Passage Wall", root, new Vector3(-3.5f, 1.5f, 7.0f), new Vector3(2.4f, 3f, 0.6f), Carved).transform;

            var compass = root.gameObject.AddComponent<CompassPuzzle>();
            compass.sensor = SensorKind.Compass;
            compass.objective = "Trouvez la direction du passage secret";
            compass.tutorial = "L'aiguille magique indique de quel côté tourner.\n\nTenez le téléphone à plat ou droit devant vous et tournez sur vous-même lentement, jusqu'à ce que l'aiguille pointe vers le fond de la salle et devienne dorée.\n\nRestez immobile un instant.";
            compass.hint = "Tournez dans le sens où pointe l'aiguille. Éloignez-vous des objets métalliques et des aimants (coques).";
            compass.fxAnchor = needle;
            compass.needle = needle; compass.needleGem = gem.GetComponent<Renderer>(); compass.passageWall = passage;

            // Statues
            var statueAlign = root.gameObject.AddComponent<StatueAlignPuzzle>();
            statueAlign.sensor = SensorKind.Gyroscope;
            statueAlign.objective = "Orientez chaque statue vers son symbole au sol";
            statueAlign.tutorial = "Tenez le téléphone devant vous et faites-le pivoter comme un volant : la statue en surbrillance tourne avec lui.\n\nAlignez son rayon doré sur le symbole lumineux, puis attendez qu'il se fige.";
            statueAlign.hint = "Tournez lentement le téléphone vers la gauche ou la droite, sans le pencher vers l'avant.";
            var xs = new[] { -3.5f, 0f, 3.5f };
            statueAlign.targetAngles = new[] { 60f, -90f, 150f };
            statueAlign.fxAnchor = Empty("Statues FX", root, new Vector3(0, 1.5f, 3f));
            statueAlign.statues = new Transform[3]; statueAlign.beams = new Renderer[3]; statueAlign.targets = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                var c = new Vector3(xs[i], 0, 3f);
                Prim(PrimitiveType.Cylinder, "Ring", root, c + new Vector3(0, 0.01f, 0), new Vector3(3.6f, 0.005f, 3.6f), DarkStone, false);
                Prim(PrimitiveType.Cylinder, "Base", root, c + new Vector3(0, 0.2f, 0), new Vector3(0.9f, 0.2f, 0.9f), Stone);
                var pivot = Empty("Statue " + (i + 1), root, c + new Vector3(0, 0.4f, 0));
                pivot.localRotation = Quaternion.Euler(0, 180f, 0);
                Box("Body", pivot, new Vector3(0, 0.8f, 0), new Vector3(0.6f, 1.6f, 0.5f), Sand);
                Box("Head", pivot, new Vector3(0, 1.85f, 0), new Vector3(0.5f, 0.5f, 0.5f), Sand);
                Box("Face", pivot, new Vector3(0, 1.85f, 0.26f), new Vector3(0.3f, 0.15f, 0.05f), DarkStone, false);
                var beam = Box("Beam", pivot, new Vector3(0, 0.15f, 1f), new Vector3(0.1f, 0.06f, 1.3f), Mat("Beam", new Color(1f, 0.8f, 0.4f), 0f, 0.5f, true), false);
                float a = statueAlign.targetAngles[i] * Mathf.Deg2Rad;
                var target = Prim(PrimitiveType.Cylinder, "Target", root, c + new Vector3(Mathf.Sin(a) * 1.7f, 0.04f, Mathf.Cos(a) * 1.7f), new Vector3(0.5f, 0.03f, 0.5f), Mat("Target" + i, GameManager.GlyphColors[i] * 0.5f, 0f, 0.5f, true), false);
                statueAlign.statues[i] = pivot; statueAlign.beams[i] = beam.GetComponent<Renderer>(); statueAlign.targets[i] = target.GetComponent<Renderer>();
            }

            Torch(root, new Vector3(-5.4f, 2.5f, 0f));
            Torch(root, new Vector3(5.4f, 2.5f, 0f));
            Torch(root, new Vector3(-5.4f, 2.5f, 5.5f));
            Torch(root, new Vector3(5.4f, 2.5f, 5.5f));

            var door = Door(root, new Vector3(3.5f, 1.5f, 7.3f), out var symText, out var plate);
            AddRoomManager(root, 1, door, symText, plate, compass, statueAlign);
            return scene;
        }

        // ================================================================== Salle 3 — Micro + Proximité

        static Scene BuildRoom3()
        {
            var (scene, root) = NewRoom("Room3", new Vector3(0, 7.5f, -9.5f), new Vector3(0, 1.8f, 2.5f), 60f, new Color(0.07f, 0.08f, 0.15f));
            Shell(root, 12f, 16f, 7.5f);

            // Gardien
            Box("Guardian Base", root, new Vector3(0, 0.4f, 5.5f), new Vector3(4f, 0.8f, 2f), DarkStone);
            Box("Guardian Body", root, new Vector3(0, 2.4f, 5.5f), new Vector3(2.8f, 3.2f, 1.4f), Sand);
            Box("Guardian Arm L", root, new Vector3(-1.8f, 2.4f, 5.2f), new Vector3(0.7f, 2.6f, 0.7f), Sand);
            Box("Guardian Arm R", root, new Vector3(1.8f, 2.4f, 5.2f), new Vector3(0.7f, 2.6f, 0.7f), Sand);
            var head = Empty("Guardian Head", root, new Vector3(0, 4.7f, 5.4f));
            Box("Head", head, Vector3.zero, new Vector3(1.9f, 1.4f, 1.4f), Sand);
            Box("Crown", head, new Vector3(0, 0.85f, 0), new Vector3(2.2f, 0.3f, 1.5f), Gold);
            var eyeMat = Mat("Eye", new Color(0.1f, 0.15f, 0.2f), 0.3f, 0.9f, true);
            var eyeL = Prim(PrimitiveType.Sphere, "Eye L", head, new Vector3(-0.45f, 0.1f, -0.7f), Vector3.one * 0.32f, eyeMat, false);
            var eyeR = Prim(PrimitiveType.Sphere, "Eye R", head, new Vector3(0.45f, 0.1f, -0.7f), Vector3.one * 0.32f, eyeMat, false);

            var mic = root.gameObject.AddComponent<MicPuzzle>();
            mic.sensor = SensorKind.Microphone;
            mic.objective = "Réveillez le gardien de pierre";
            mic.tutorial = "Le gardien dort. Soufflez sur le micro du téléphone (en bas de l'appareil) ou parlez fort pendant quelques secondes.\n\nSes yeux s'allument à mesure qu'il se réveille.";
            mic.hint = "Soufflez de façon continue près du bas du téléphone. Si rien ne se passe, augmentez la sensibilité du micro dans Pause.";
            mic.eyes = new[] { eyeL.GetComponent<Renderer>(), eyeR.GetComponent<Renderer>() };
            mic.guardianHead = head;
            mic.fxAnchor = head;

            // Runes et cachette
            var runeMat = Mat("Rune", new Color(0.12f, 0.13f, 0.16f), 0f, 0.6f, true);
            var runes = new Renderer[3];
            for (int i = 0; i < 3; i++)
                runes[i] = Prim(PrimitiveType.Cylinder, "Rune " + (i + 1), root, new Vector3(-2.2f + i * 2.2f, 0.03f, 0.5f), new Vector3(1.1f, 0.03f, 1.1f), runeMat, false).GetComponent<Renderer>();
            Box("Cache", root, new Vector3(0, 0.4f, 2.8f), new Vector3(1.4f, 0.8f, 1f), DarkStone);
            var lidPivot = Empty("Cache Lid Pivot", root, new Vector3(0, 0.82f, 3.3f));
            Box("Lid", lidPivot, new Vector3(0, 0.08f, -0.5f), new Vector3(1.5f, 0.16f, 1.05f), Carved);
            var treasure = Prim(PrimitiveType.Sphere, "Cache Glow", root, new Vector3(0, 0.75f, 2.8f), Vector3.one * 0.3f, Gold, false);
            treasure.transform.localScale = new Vector3(0.5f, 0.15f, 0.5f);

            var prox = root.gameObject.AddComponent<ProximityPuzzle>();
            prox.sensor = SensorKind.Proximity;
            prox.objective = "Activez les trois runes de la cachette";
            prox.tutorial = "Le capteur de proximité se trouve en haut du téléphone, près de l'écouteur.\n\nCouvrez-le avec la main, puis retirez-la. Recommencez trois fois : une rune s'allume à chaque fois.";
            prox.hint = "Passez la main à 2-3 cm au-dessus du haut de l'écran, puis éloignez-la complètement.";
            prox.runes = runes; prox.cacheLid = lidPivot;
            prox.fxAnchor = treasure.transform;

            Torch(root, new Vector3(-5.4f, 2.5f, 1f));
            Torch(root, new Vector3(5.4f, 2.5f, 1f));
            Torch(root, new Vector3(-3f, 2.5f, 6.9f));
            Torch(root, new Vector3(3f, 2.5f, 6.9f));

            var door = Door(root, new Vector3(-4f, 1.5f, 7.3f), out var symText, out var plate);
            AddRoomManager(root, 2, door, symText, plate, mic, prox);
            return scene;
        }

        // ================================================================== Salle 4 — Luminosité + code final

        static Scene BuildRoom4()
        {
            var (scene, root) = NewRoom("Room4", new Vector3(0, 6.5f, -8.5f), new Vector3(0, 2f, 3f), 60f, new Color(0.04f, 0.045f, 0.09f));
            Shell(root, 12f, 16f, 7.5f);

            var wallGlyph = Box("Symbol Wall", root, new Vector3(0, 3.3f, 7.44f), new Vector3(3.6f, 1.8f, 0.1f), Glyph, false).GetComponent<Renderer>();
            var hidden = Text3D("Hidden Symbol", root, new Vector3(0, 3.3f, 7.35f), "", 0.14f, new Color(1, 1, 1, 0));
            var rl = new GameObject("Reveal Light").AddComponent<Light>();
            rl.transform.SetParent(root, false);
            rl.transform.localPosition = new Vector3(0, 3.3f, 5.5f);
            rl.type = LightType.Point; rl.color = new Color(1f, 0.8f, 0.4f); rl.range = 5f; rl.intensity = 0f;

            // Les 6 symboles gravés de part et d'autre (rappel visuel du clavier)
            for (int i = 0; i < 6; i++)
            {
                float x = (i < 3 ? -4.6f : 2.6f) + (i % 3) * 1f;
                var g = Text3D("Glyph " + GameManager.GlyphNames[i], root, new Vector3(x, 1.4f, 7.4f), GameManager.GlyphNames[i], 0.035f, GameManager.GlyphColors[i] * 0.7f);
                g.transform.localPosition = new Vector3(x, 1.4f + (i % 2) * 0.5f, 7.4f);
            }

            Prim(PrimitiveType.Cylinder, "Pedestal", root, new Vector3(0, 0.6f, 2.5f), new Vector3(1.2f, 0.6f, 1.2f), Stone);
            var art = Box("Artefact", root, new Vector3(0, 1.75f, 2.5f), Vector3.one * 0.55f, Gold, false);
            art.transform.rotation = Quaternion.Euler(45, 0, 45);
            var al = new GameObject("Artefact Light").AddComponent<Light>();
            al.transform.SetParent(art.transform, true);
            al.transform.position = new Vector3(0, 2.4f, 2f);
            al.type = LightType.Point; al.color = new Color(1f, 0.75f, 0.35f); al.range = 6f; al.intensity = 2f;

            var light = root.gameObject.AddComponent<LightRevealPuzzle>();
            light.sensor = SensorKind.Light;
            light.objective = "Révélez le dernier symbole gravé sur le mur";
            light.tutorial = "Le dernier symbole n'apparaît qu'avec un changement de lumière.\n\nCouvrez le haut de l'écran avec la main pour faire l'obscurité, ou éclairez-le avec une lampe, et maintenez quelques secondes.";
            light.hint = "Le capteur de lumière est près de la caméra avant. Couvrez-le complètement ou approchez-le d'une lampe.";
            light.fxAnchor = wallGlyph.transform;
            light.hiddenSymbol = hidden; light.wallGlyph = wallGlyph; light.revealLight = rl;

            var code = root.gameObject.AddComponent<CodePuzzle>();
            code.usesSensor = false;
            code.objective = "Touchez les 4 symboles dans l'ordre des salles";
            code.tutorial = "Chaque salle vous a révélé un symbole. Touchez-les dans l'ordre : salle 1, salle 2, salle 3, puis celui du mur.\n\nUne erreur remet la séquence à zéro.";
            code.hint = "Le 4e symbole est celui qui vient d'apparaître sur le mur.";
            code.fxAnchor = art.transform;
            code.artefact = art.transform; code.artefactRenderer = art.GetComponent<Renderer>(); code.artefactLight = al;

            Torch(root, new Vector3(-5.4f, 2.5f, 1f));
            Torch(root, new Vector3(5.4f, 2.5f, 1f));

            AddRoomManager(root, 3, null, null, null, light, code);
            return scene;
        }
    }
}
