using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LostTemple.EditorTools
{
    /// <summary>Module 1 — Accéléromètre : construction de la salle 1 (pont suspendu et boule d'or).</summary>
    public static partial class LostTempleSceneBuilder
    {
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
    }
}
