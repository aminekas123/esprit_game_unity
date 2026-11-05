using System.Collections.Generic;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LostTemple.EditorTools
{
    /// <summary>Module 1 — Accéléromètre : construction de la salle 1 (pont suspendu et boule d'or).</summary>
    public static partial class LostTempleSceneBuilder
    {
        // ================================================================== Salle 1 — Accéléromètre

        [RoomBuilder(0, "Room1_Entree", "1_Accelerometre")]
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

            // Parcours en double zigzag : 6 ponts étroits (1,1 m). Tous les morceaux sont enfants de « Course »,
            // qui porte la NavMeshSurface : la boule rivale (NavMeshAgent) ne peut rouler que là.
            var course = Empty("Course", root, Vector3.zero);
            Box("Start Platform", course, new Vector3(0, 0, -4.8f), new Vector3(2.2f, 0.4f, 2f), Stone);
            Box("Bridge 1", course, new Vector3(0f, 0, -2.85f), new Vector3(1.1f, 0.4f, 2.1f), Sand);
            Box("Bridge 2", course, new Vector3(1.6f, 0, -1.6f), new Vector3(4.3f, 0.4f, 1.1f), Sand);
            Box("Bridge 3", course, new Vector3(3.2f, 0, 0.25f), new Vector3(1.1f, 0.4f, 3f), Sand);
            Box("Bridge 4", course, new Vector3(1.6f, 0, 1.4f), new Vector3(4.3f, 0.4f, 1.1f), Sand);
            Box("Bridge 5", course, new Vector3(0f, 0, 3.05f), new Vector3(1.1f, 0.4f, 2.4f), Sand);
            Box("Bridge 6", course, new Vector3(1.5f, 0, 4.45f), new Vector3(4.1f, 0.4f, 1.1f), Sand);
            Box("Goal Platform", course, new Vector3(3.5f, 0, 5f), new Vector3(2.2f, 0.4f, 1.6f), Stone);
            var surface = course.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            foreach (var p in new[] { new Vector3(0f, 0, -2.85f), new Vector3(1.6f, 0, -1.6f), new Vector3(3.2f, 0, 0.25f), new Vector3(1.6f, 0, 1.4f), new Vector3(0f, 0, 3.05f), new Vector3(1.5f, 0, 4.45f) })
                Box("Support", root, new Vector3(p.x, -3.2f, p.z), new Vector3(0.5f, 6f, 0.5f), DarkStone, false);
            var goalPlate = Box("Goal Plate", root, new Vector3(3.5f, 0.22f, 5f), new Vector3(1.3f, 0.05f, 1.3f), Gold, false).GetComponent<Renderer>();
            var goal = new GameObject("Goal Trigger");
            goal.transform.SetParent(root, false);
            goal.transform.localPosition = new Vector3(3.5f, 0.7f, 5f);
            var gcol = goal.AddComponent<BoxCollider>();
            gcol.isTrigger = true; gcol.size = new Vector3(1.3f, 1f, 1.3f);

            // Piège sur le pont de la boule (4e pont)
            Prim(PrimitiveType.Cylinder, "Sweeper Axis", root, new Vector3(1.4f, 0.45f, 1.4f), new Vector3(0.3f, 0.25f, 0.3f), Gold, false);
            var sweeper = Box("Sweeper", root, new Vector3(1.4f, 0.5f, 1.4f), new Vector3(1.7f, 0.25f, 0.2f), Metal);
            sweeper.AddComponent<Rigidbody>();
            sweeper.AddComponent<Sweeper>().speed = 55f;

            SpikeTrap(root, new Vector3(-5f, 0, -1.5f), new Vector2(4f, 1.6f), 0f);
            SpikeTrap(root, new Vector3(-5f, 0, 2.5f), new Vector2(4f, 1.6f), 1.4f);
            Relic(root, new Vector3(-6.3f, 0.8f, 0.5f));
            Relic(root, new Vector3(6f, 0.8f, -10.5f));
            Relic(root, new Vector3(-6f, 0.8f, 8.5f));

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
            puzzle.objective = "Guidez la boule d'or jusqu'à la plaque et arrêtez la boule rouge";
            puzzle.tutorial = "Tenez le téléphone devant vous, comme pour lire.\n\nInclinez-le doucement : la boule d'or roule dans ce sens. Menez-la jusqu'à la plaque lumineuse au bout du pont.\n\nAttention : une boule rouge part de l'arrivée et roule vers VOTRE départ. Percutez-la avec élan pour la faire tomber avant qu'elle n'y arrive !";
            puzzle.hint = "Prenez de l'élan sur une ligne droite pour percuter la boule rouge. Redressez le téléphone pour freiner avant les virages.";
            puzzle.fxAnchor = goalPlate.transform;
            puzzle.cameraPose = Pose("Cam Ball", root, new Vector3(1.75f, 12.5f, -8.5f), new Vector3(1.75f, 0f, 0.8f));
            puzzle.ball = rb; puzzle.spawn = spawn; puzzle.goal = gcol; puzzle.goalPlate = goalPlate;
            goal.AddComponent<GoalTrigger>().puzzle = puzzle;

            // Boule rivale : NavMeshAgent qui va de l'arrivée vers le départ du joueur, en sens inverse
            var rivalMat = Mat("RivalBall", new Color(0.85f, 0.15f, 0.1f), 0.6f, 0.8f, true);
            rivalMat.SetColor("_EmissionColor", new Color(1f, 0.15f, 0.05f) * 1.2f);
            var rival = Prim(PrimitiveType.Sphere, "Rival Ball", root, new Vector3(3f, 0.45f, 5.1f), Vector3.one * 0.5f, rivalMat);
            var rrb = rival.AddComponent<Rigidbody>();
            rrb.isKinematic = true; rrb.mass = 1f; rrb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var agent = rival.AddComponent<NavMeshAgent>();
            agent.enabled = false;
            var rivalLight = new GameObject("Rival Light").AddComponent<Light>();
            rivalLight.transform.SetParent(rival.transform, false);
            rivalLight.type = LightType.Point; rivalLight.color = new Color(1f, 0.25f, 0.1f); rivalLight.range = 2.5f; rivalLight.intensity = 1.5f; rivalLight.shadows = LightShadows.None;
            var rivalBall = rival.AddComponent<RivalBall>();
            rivalBall.puzzle = puzzle; rivalBall.course = surface;
            rivalBall.rivalStart = Empty("Rival Start", root, new Vector3(3f, 0.2f, 5.1f));
            rivalBall.playerStart = Empty("Rival Target", root, new Vector3(0f, 0.2f, -4.6f));

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

            // Amélioration : plateau à trous (2e épreuve d'inclinaison), sur la droite du fond de la salle
            Box("Plateau", root, new Vector3(3.9f, 0.3f, 7.95f), new Vector3(4.8f, 0.6f, 3.3f), Sand);
            Box("Plateau Rim S", root, new Vector3(3.9f, 0.75f, 6.25f), new Vector3(5f, 0.3f, 0.15f), Carved);
            Box("Plateau Rim N", root, new Vector3(3.9f, 0.75f, 9.65f), new Vector3(5f, 0.3f, 0.15f), Carved);
            Box("Plateau Rim W", root, new Vector3(1.45f, 0.75f, 7.95f), new Vector3(0.15f, 0.3f, 3.5f), Carved);
            Box("Plateau Rim E", root, new Vector3(6.35f, 0.75f, 7.95f), new Vector3(0.15f, 0.3f, 3.5f), Carved);
            var spawn2 = Empty("Plateau Spawn", root, new Vector3(2.1f, 0.95f, 6.75f));
            var ball2 = Prim(PrimitiveType.Sphere, "Plateau Ball", root, spawn2.localPosition, Vector3.one * 0.5f, Gold);
            var rb2 = ball2.AddComponent<Rigidbody>();
            rb2.mass = 1f; rb2.linearDamping = 0.6f; rb2.angularDamping = 0.6f;
            rb2.interpolation = RigidbodyInterpolation.Interpolate;
            rb2.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb2.isKinematic = true;
            var goalPlate2 = Box("Plateau Goal", root, new Vector3(5.65f, 0.62f, 9.1f), new Vector3(0.8f, 0.04f, 0.8f), Gold, false).GetComponent<Renderer>();
            var goal2 = new GameObject("Plateau Goal Trigger");
            goal2.transform.SetParent(root, false);
            goal2.transform.localPosition = new Vector3(5.65f, 0.9f, 9.1f);
            var g2col = goal2.AddComponent<BoxCollider>();
            g2col.isTrigger = true; g2col.size = new Vector3(0.7f, 0.6f, 0.7f);

            var plateau = root.gameObject.AddComponent<TiltBallPuzzle>();
            plateau.sensor = SensorKind.Accelerometer;
            plateau.objective = "Menez la boule au coin doré sans tomber dans les trous";
            plateau.tutorial = "Deuxième épreuve : un plateau percé de trous noirs.\n\nInclinez doucement le téléphone pour guider la boule jusqu'au coin doré. Si elle passe sur un trou, elle revient au départ.";
            plateau.hint = "Longez les bords et redressez le téléphone avant chaque trou pour freiner.";
            plateau.acceleration = 9f;
            plateau.fxAnchor = goalPlate2.transform;
            plateau.cameraPose = Pose("Cam Plateau", root, new Vector3(3.9f, 7.5f, 3.6f), new Vector3(3.9f, 0.6f, 8.1f));
            plateau.ball = rb2; plateau.spawn = spawn2; plateau.goal = g2col; plateau.goalPlate = goalPlate2;
            goal2.AddComponent<GoalTrigger>().puzzle = plateau;
            foreach (var h in new[] { new Vector2(3.4f, 7.3f), new Vector2(2.4f, 8.6f), new Vector2(4.5f, 8.4f), new Vector2(5.3f, 7.0f), new Vector2(3.6f, 9.3f) })
            {
                Prim(PrimitiveType.Cylinder, "Hole", root, new Vector3(h.x, 0.605f, h.y), new Vector3(0.7f, 0.005f, 0.7f), Pit, false);
                var ht = new GameObject("Hole Trigger");
                ht.transform.SetParent(root, false);
                ht.transform.localPosition = new Vector3(h.x, 0.85f, h.y);
                var hc = ht.AddComponent<BoxCollider>();
                hc.isTrigger = true; hc.size = new Vector3(0.45f, 0.5f, 0.45f);
                ht.AddComponent<HoleTrigger>().puzzle = plateau;
            }
            var st2 = MakeStation(root, "Plateau", new Vector3(0.4f, 0f, 7.6f), plateau);

            AddRoomManager(root, 0, door, exit, new PuzzleStep[] { puzzle, plateau }, new[] { st, st2 });
            return scene;
        }
    }
}
