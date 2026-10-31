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
    }
}
