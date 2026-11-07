using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LostTemple.EditorTools
{
    /// <summary>Module 6 — Luminosité (+ proximité) : construction des salles 5 (runes, momies) et 6 (pont de cristal).</summary>
    public static partial class LostTempleSceneBuilder
    {
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

        /// <summary>Grand brasier : bûches, flammes, braises, vapeur (jouée à l'extinction), lumière et collider qui bloque le passage.</summary>
        static (Transform root, ParticleSystem[] flames, ParticleSystem steam, Light light, Collider blocker) BigFire(Transform parent, Vector3 pos)
        {
            var go = Empty("Big Fire", parent, pos);
            var logMat = Mat("FireLog", new Color(0.18f, 0.1f, 0.06f), 0f, 0.2f, false);
            var emberMat = Mat("FireEmbers", new Color(0.3f, 0.08f, 0.02f), 0f, 0.4f, true);
            emberMat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.05f) * 2f);
            Prim(PrimitiveType.Cylinder, "Ember Bed", go, new Vector3(0, 0.05f, 0), new Vector3(2.8f, 0.05f, 2f), emberMat, false);
            for (int i = 0; i < 5; i++)
            {
                var log = Prim(PrimitiveType.Cylinder, "Log " + (i + 1), go, new Vector3(0, 0.22f, 0), new Vector3(0.28f, 1.2f, 0.28f), logMat, false);
                log.transform.localRotation = Quaternion.Euler(0f, i * 36f, 80f);
            }
            var blockGo = new GameObject("Fire Blocker");
            blockGo.transform.SetParent(go, false);
            var blocker = blockGo.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0, 1f, 0); blocker.size = new Vector3(3f, 2.5f, 1.8f);

            ParticleSystem Fx(string name, float lifeMin, float lifeMax, float speedMin, float speedMax, float sizeMin, float sizeMax, Color c1, Color c2, float rate, float radius, Color end)
            {
                var fx = new GameObject(name);
                fx.transform.SetParent(go, false);
                fx.transform.localPosition = new Vector3(0, 0.2f, 0);
                fx.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                var ps = fx.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
                main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
                main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
                main.startColor = new ParticleSystem.MinMaxGradient(c1, c2);
                main.maxParticles = 600;
                var em = ps.emission; em.rateOverTime = rate;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = radius;
                var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1f, 1, 0.5f));
                var col = ps.colorOverLifetime; col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(end, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
                col.color = g;
                fx.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
                return ps;
            }
            var flames = Fx("Flames", 1f, 1.5f, 2.6f, 4f, 1.6f, 2.6f, new Color(1f, 0.75f, 0.3f), new Color(1f, 0.4f, 0.1f), 220f, 1.1f, new Color(1f, 0.2f, 0.03f));
            var core = Fx("Flame Core", 0.6f, 0.9f, 2f, 3f, 1f, 1.7f, new Color(1f, 0.95f, 0.6f), new Color(1f, 0.8f, 0.35f), 130f, 0.6f, new Color(1f, 0.5f, 0.1f));
            var sparks = Fx("Sparks", 1f, 1.8f, 2.5f, 4.5f, 0.05f, 0.12f, new Color(1f, 0.8f, 0.3f), new Color(1f, 0.5f, 0.1f), 25f, 0.7f, new Color(1f, 0.3f, 0.05f));
            var steam = Fx("Steam", 1.5f, 2.5f, 1f, 2f, 1f, 2f, new Color(0.6f, 0.6f, 0.65f), new Color(0.45f, 0.45f, 0.5f), 0f, 0.9f, new Color(0.2f, 0.2f, 0.22f));
            var steamMain = steam.main; steamMain.loop = false; steamMain.duration = 0.5f; steamMain.playOnAwake = false;
            var steamEm = steam.emission; steamEm.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            foreach (var f in new[] { flames, core, sparks }) { var m = f.main; m.playOnAwake = true; }

            var l = new GameObject("Fire Light").AddComponent<Light>();
            l.transform.SetParent(go, false);
            l.transform.localPosition = new Vector3(0, 1.4f, -0.6f);
            l.type = LightType.Point; l.color = new Color(1f, 0.5f, 0.15f); l.range = 10f; l.intensity = 8f; l.shadows = LightShadows.None;
            return (go, new[] { flames, core, sparks }, steam, l, blocker);
        }

        // ================================================================== Salle 5 — Proximité

        [RoomBuilder(4, "Room5_Runes", "6_Luminosite")]
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
            // Épreuve 2 : luminosité du jeu inversée (lumière forte → jeu sombre, obscurité → jeu au maximum)
            var screenBox = Box("Brightness Screen", root, new Vector3(6.95f, 2.4f, 5f), new Vector3(0.1f, 1.6f, 2.4f), Mat("BrightnessScreen", new Color(0.07f, 0.07f, 0.08f), 0.1f, 0.4f, true), false);
            Box("Brightness Frame", root, new Vector3(6.98f, 2.4f, 5f), new Vector3(0.05f, 1.8f, 2.6f), Gold, false);
            var screenText = Text3D("Brightness Text", root, new Vector3(6.85f, 2.45f, 5f), "Luminosite du jeu\n50 %", 0.045f, new Color(1f, 0.95f, 0.8f));
            screenText.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var lampMat = Mat("BrightnessLamp", new Color(0.12f, 0.13f, 0.16f), 0.2f, 0.8f, true);
            var brightLamp = Prim(PrimitiveType.Sphere, "Lamp Bright", root, new Vector3(6.8f, 1.15f, 4.3f), Vector3.one * 0.35f, lampMat, false).GetComponent<Renderer>();
            var darkLamp = Prim(PrimitiveType.Sphere, "Lamp Dark", root, new Vector3(6.8f, 1.15f, 5.7f), Vector3.one * 0.35f, lampMat, false).GetComponent<Renderer>();
            var lt1 = Text3D("Lamp Bright Label", root, new Vector3(6.85f, 0.8f, 4.3f), "LAMPE", 0.03f, new Color(1f, 0.85f, 0.4f));
            lt1.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var lt2 = Text3D("Lamp Dark Label", root, new Vector3(6.85f, 0.8f, 5.7f), "NOIR", 0.03f, new Color(0.6f, 0.8f, 1f));
            lt2.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var bright = root.gameObject.AddComponent<GameBrightnessPuzzle>();
            bright.sensor = SensorKind.Light;
            bright.objective = "Testez la luminosité automatique inversée du temple";
            bright.tutorial = "Le capteur de lumière règle la luminosité du JEU, à l'envers :\n\n• Lumière forte (lampe, torche du téléphone d'un ami, fenêtre) → le jeu devient SOMBRE, au minimum.\n• Lumière faible (cachez le capteur avec la main) → le jeu devient LUMINEUX, au maximum.\n\nTenez chaque extrême 2 secondes pour allumer les deux lampes.";
            bright.hint = "Le capteur de lumière est en haut de l'écran, près de la caméra avant. Couvrez-le entièrement pour l'obscurité, puis approchez une lampe.";
            bright.screenText = screenText;
            bright.screen = screenBox.GetComponent<Renderer>();
            bright.brightLamp = brightLamp; bright.darkLamp = darkLamp;
            bright.fxAnchor = screenText.transform;
            bright.cameraPose = Pose("Cam Brightness", root, new Vector3(1.2f, 3.2f, 5f), new Vector3(6.9f, 2.1f, 5f));
            var stBright = MakeStation(root, "Luminosité", new Vector3(4.4f, 0f, 3f), bright);

            AddRoomManager(root, 4, door, exit, new PuzzleStep[] { prox, bright }, new[] { st, stBright });
            return scene;
        }

        // ================================================================== Salle 6 — Luminosité : le pont invisible

        [RoomBuilder(5, "Room6_Lumiere", "6_Luminosite")]
        static Scene BuildRoom6()
        {
            var (scene, root, cam) = NewRoom("Room6", new Vector3(0, 10f, -16f), new Vector3(0, 0, -6f), 60f, new Color(0.06f, 0.065f, 0.12f));
            Box("Floor", root, new Vector3(0, -0.25f, 0f), new Vector3(14f, 0.5f, 24f), Stone);
            Shell(root, 14f, -12f, 12f, false);

            SpikeTrap(root, new Vector3(0, 0, -7f), new Vector2(14f, 1.6f), 0f, 3f);

            Relic(root, new Vector3(-6f, 0.8f, -10.5f));
            Relic(root, new Vector3(-5f, 0.8f, 3f));
            Relic(root, new Vector3(6f, 0.8f, 10.5f));

            foreach (var t in new[] { new Vector3(-6.7f, 2.6f, -9f), new Vector3(6.7f, 2.6f, -9f), new Vector3(-6.7f, 2.6f, 9f), new Vector3(6.7f, 2.6f, 9f) })
                Torch(root, t);

            var door = Door(root, new Vector3(0f, 1.5f, 11.75f));
            var exit = Exit(root, new Vector3(0f, 0f, 10.7f));
            var player = Explorer(root, new Vector3(0f, 0.05f, -10.5f), 0f, true, true);
            Secret(root, new Vector3(5.5f, 0.9f, -4f), 5);
            FollowCamera(cam, player);

            // Unique épreuve : un grand brasier devant la porte, éteint en couvrant le capteur de lumière 5 secondes
            var fire = BigFire(root, new Vector3(0f, 0f, 10.2f));
            var firePuzzle = root.gameObject.AddComponent<BigFirePuzzle>();
            firePuzzle.sensor = SensorKind.Light;
            firePuzzle.coverTime = 5f;
            firePuzzle.objective = "Éteignez le grand brasier qui bloque la porte";
            firePuzzle.tutorial = "Un énorme brasier bloque la sortie.\n\nPosez toute la main sur le capteur de lumière (en haut de l'écran) pour le priver de lumière : le feu faiblit. Gardez la main 5 secondes et il disparaît.\n\nSi vous retirez la main trop tôt, le feu reprend !";
            firePuzzle.hint = "Couvrez bien tout le haut du téléphone, sans laisser passer de lumière, et ne bougez plus.";
            firePuzzle.flames = fire.flames; firePuzzle.steam = fire.steam; firePuzzle.fireLight = fire.light; firePuzzle.blocker = fire.blocker;
            firePuzzle.fxAnchor = fire.root;
            firePuzzle.cameraPose = Pose("Cam Fire", root, new Vector3(1.6f, 2.6f, 6.4f), new Vector3(0f, 1.6f, 10.2f));
            var st = MakeStation(root, "Brasier", new Vector3(3f, 0f, 8.6f), firePuzzle);

            AddRoomManager(root, 5, door, exit, new PuzzleStep[] { firePuzzle }, new[] { st });
            return scene;
        }
    }
}
