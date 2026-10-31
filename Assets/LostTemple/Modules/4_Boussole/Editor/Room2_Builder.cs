using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LostTemple.EditorTools
{
    /// <summary>Module 4 — Boussole : construction de la salle 2 (labyrinthe et aiguille magique).</summary>
    public static partial class LostTempleSceneBuilder
    {
        // ================================================================== Salle 2 — Boussole

        [RoomBuilder(1, "Room2_Boussole", "4_Boussole")]
        static Scene BuildRoom2()
        {
            var (scene, root, cam) = NewRoom("Room2", new Vector3(0, 10f, -16f), new Vector3(0, 0, -6f), 60f, new Color(0.15f, 0.16f, 0.25f));
            Shell(root, 14f, -12f, 12f);

            // Labyrinthe
            Box("Maze Wall 1", root, new Vector3(-2f, 1.5f, -8.5f), new Vector3(10f, 3f, 0.7f), Sand);
            Box("Maze Wall 2", root, new Vector3(4.5f, 1.5f, -4.8f), new Vector3(5f, 3f, 0.7f), Sand);

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
            // Amélioration : trois directions successives, une lampe par direction trouvée
            compass.directions = 3;
            var lampMat = Mat("StepLamp", new Color(0.2f, 0.18f, 0.15f), 0.2f, 0.8f, true);
            compass.stepLamps = new Renderer[3];
            for (int i = 0; i < 3; i++)
                compass.stepLamps[i] = Prim(PrimitiveType.Sphere, "Direction Lamp " + (i + 1), root, new Vector3(-3.55f + i * 0.55f, 1.08f, -6.25f), Vector3.one * 0.2f, lampMat, false).GetComponent<Renderer>();
            compass.objective = "Trouvez les 3 directions du passage secret";
            compass.tutorial = "L'aiguille magique indique de quel côté tourner.\n\nTenez le téléphone à plat ou droit devant vous et tournez sur vous-même lentement, jusqu'à ce que l'aiguille pointe vers le fond de la salle et devienne dorée. Restez immobile un instant.\n\nTrois directions successives sont à trouver : une lampe s'allume à chacune.";

            FireJet(root, new Vector3(-4.5f, 0, 8.5f), 0f);
            FireJet(root, new Vector3(-1.5f, 0, 8.5f), 1.6f);
            FireJet(root, new Vector3(1.5f, 0, 8.5f), 0.8f);
            FireJet(root, new Vector3(4.5f, 0, 8.5f), 2.4f);

            Relic(root, new Vector3(-6f, 0.8f, -10.5f));
            Relic(root, new Vector3(6.3f, 0.8f, 10.8f));
            Relic(root, new Vector3(-6f, 0.8f, 6.5f));

            foreach (var t in new[] { new Vector3(-6.7f, 2.6f, -6f), new Vector3(6.7f, 2.6f, -9f), new Vector3(-6.7f, 2.6f, 2f), new Vector3(6.7f, 2.6f, 2f), new Vector3(-6.7f, 2.6f, 10f), new Vector3(6.7f, 2.6f, 10f) })
                Torch(root, t);

            var door = Door(root, new Vector3(0f, 1.5f, 11.75f));
            var exit = Exit(root, new Vector3(0f, 0f, 10.7f));
            var st = MakeStation(root, "Boussole", new Vector3(-1.4f, 0f, -5.5f), compass);
            var player = Explorer(root, new Vector3(-4f, 0.05f, -10.5f), 0f, true);
            FollowCamera(cam, player);
            Secret(root, new Vector3(5f, 0.9f, -10.5f), 1);

            // ---------- Épreuve 2 : la rose des vents (vrais points cardinaux)
            Prim(PrimitiveType.Cylinder, "Wind Rose", root, new Vector3(-4f, 0.02f, 1.8f), new Vector3(2.8f, 0.02f, 2.8f), DarkStone, false);
            var dial = Empty("Rose Dial", root, new Vector3(-4f, 0.05f, 1.8f));
            var roseLampMat = Mat("RoseLamp", new Color(0.15f, 0.15f, 0.18f), 0.2f, 0.8f, true);
            var cardinal = root.gameObject.AddComponent<CardinalPuzzle>();
            string[] letters = { "N", "E", "S", "O" };
            Vector3[] dirs = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
            for (int i = 0; i < 4; i++)
            {
                var letter = Text3D("Letter " + letters[i], dial, dirs[i] * 1.15f + Vector3.up * 0.03f, letters[i], 0.09f, i == 0 ? new Color(1f, 0.4f, 0.3f) : new Color(0.95f, 0.9f, 0.75f));
                letter.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                cardinal.lamps[i] = Prim(PrimitiveType.Sphere, "Lamp " + letters[i], dial, dirs[i] * 0.75f + Vector3.up * 0.08f, Vector3.one * 0.25f, roseLampMat, false).GetComponent<Renderer>();
            }
            Box("Rose Arrow", dial, new Vector3(0f, 0.05f, 0.45f), new Vector3(0.1f, 0.04f, 0.9f), Gold, false);
            cardinal.northMarker = dial;
            cardinal.sensor = SensorKind.Compass;
            cardinal.objective = "Tournez-vous vers les vrais points cardinaux";
            cardinal.tutorial = "Cette rose des vents connaît le VRAI nord : sa flèche dorée et le « N » rouge pointent vers le nord magnétique.\n\nLe temple vous demande de vous tourner vers 3 points cardinaux (Nord, Est, Sud ou Ouest). Tenez le téléphone droit devant vous, tournez-vous, puis restez immobile un instant.";
            cardinal.hint = "Le N rouge de la rose montre le nord. L'est est à droite du nord, l'ouest à gauche, le sud derrière.";
            cardinal.fxAnchor = dial;
            cardinal.cameraPose = Pose("Cam Rose", root, new Vector3(-4f, 5.5f, -2.3f), new Vector3(-4f, 0f, 2f));
            var stRose = MakeStation(root, "Rose", new Vector3(-1.6f, 0f, 1.8f), cardinal);

            // ---------- Épreuve 3 : le coffre aimanté (détecteur de métaux)
            Box("Magnet Chest", root, new Vector3(4.5f, 0.35f, 0.8f), new Vector3(1.2f, 0.7f, 0.8f), Carved);
            var lidPivot = Empty("Chest Lid Pivot", root, new Vector3(4.5f, 0.72f, 1.2f));
            Box("Chest Lid", lidPivot, new Vector3(0f, 0.07f, -0.4f), new Vector3(1.3f, 0.14f, 0.85f), Gold, false);
            var seal = Prim(PrimitiveType.Sphere, "Magnet Seal", root, new Vector3(4.5f, 0.45f, 0.36f), Vector3.one * 0.26f, Mat("MagnetSeal", new Color(0.25f, 0.35f, 0.6f), 0.8f, 0.9f, true), false);
            var ironSparks = FxBurst(root, "Magnet Sparks", new Vector3(4.5f, 0.9f, 0.8f), new Color(0.5f, 0.8f, 1f, 1f), 0.03f, 0.08f, 0.6f, 1.5f, -0.3f, 0, new Vector3(1f, 0.1f, 0.6f));
            var em = ironSparks.emission; em.rateOverTime = 40f; em.SetBursts(new ParticleSystem.Burst[0]);
            var main = ironSparks.main; main.loop = true;
            ironSparks.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
            var chest = root.gameObject.AddComponent<MagnetChestPuzzle>();
            chest.sensor = SensorKind.Compass;
            chest.objective = "Ouvrez le coffre aimanté avec le champ magnétique";
            chest.tutorial = "Ce coffre est scellé par un aimant ancien. Le capteur de la boussole (magnétomètre) mesure aussi la force du champ magnétique.\n\nGardez d'abord le téléphone loin du métal pendant 1 seconde, puis approchez-le d'un objet métallique réel (clé, ciseaux, ordinateur, aimant de frigo) jusqu'à remplir la jauge.";
            chest.hint = "Approchez le haut du téléphone tout près d'un objet en fer ou d'un aimant et ne bougez plus.";
            chest.lid = lidPivot; chest.seal = seal.GetComponent<Renderer>(); chest.sparks = ironSparks;
            chest.fxAnchor = seal.transform;
            chest.cameraPose = Pose("Cam Chest", root, new Vector3(2.6f, 3.2f, -2.2f), new Vector3(4.5f, 0.5f, 0.8f));
            var stChest = MakeStation(root, "Coffre", new Vector3(2.4f, 0f, 0.8f), chest);

            // ---------- Épreuve 4 : la luciole dans le labyrinthe au sol
            var wallMat = Carved;
            var mazeWalls = new System.Collections.Generic.List<Collider>();
            void MazeWall(Vector3 c, Vector3 sz) => mazeWalls.Add(Box("Firefly Wall", root, c, sz, wallMat).GetComponent<Collider>());
            Box("Firefly Floor", root, new Vector3(3f, 0.01f, 5.6f), new Vector3(6f, 0.02f, 4f), Mat("FireflyFloor", new Color(0.12f, 0.14f, 0.2f), 0.1f, 0.5f), false);
            MazeWall(new Vector3(3f, 0.175f, 3.6f), new Vector3(6.12f, 0.35f, 0.12f));
            MazeWall(new Vector3(3f, 0.175f, 7.6f), new Vector3(6.12f, 0.35f, 0.12f));
            MazeWall(new Vector3(0f, 0.175f, 5.6f), new Vector3(0.12f, 0.35f, 4f));
            MazeWall(new Vector3(6f, 0.175f, 5.6f), new Vector3(0.12f, 0.35f, 4f));
            MazeWall(new Vector3(2f, 0.175f, 5f), new Vector3(0.12f, 0.35f, 2.8f));
            MazeWall(new Vector3(4f, 0.175f, 6.2f), new Vector3(0.12f, 0.35f, 2.8f));
            var fireflyStart = Empty("Firefly Start", root, new Vector3(0.6f, 0.3f, 4.1f));
            var fireflyGoal = Empty("Firefly Goal", root, new Vector3(5.2f, 0.3f, 7f));
            Prim(PrimitiveType.Cylinder, "Firefly Goal Ring", root, new Vector3(5.2f, 0.03f, 7f), new Vector3(0.8f, 0.01f, 0.8f), MarkerMat(), false);
            var fireflyMat = Mat("Firefly", new Color(0.8f, 1f, 0.5f), 0f, 0.8f, true);
            fireflyMat.SetColor("_EmissionColor", new Color(0.7f, 1f, 0.35f) * 4f);
            var orb = Prim(PrimitiveType.Sphere, "Firefly", root, fireflyStart.localPosition, Vector3.one * 0.35f, fireflyMat, false);
            var orbLight = new GameObject("Firefly Light").AddComponent<Light>();
            orbLight.transform.SetParent(orb.transform, false);
            orbLight.type = LightType.Point; orbLight.color = new Color(0.75f, 1f, 0.4f); orbLight.range = 3f; orbLight.intensity = 2f; orbLight.shadows = LightShadows.None;
            var arrow = Empty("Firefly Arrow", orb.transform, Vector3.zero);
            Box("Arrow Tip", arrow, new Vector3(0f, 0f, 1.6f), new Vector3(0.3f, 0.1f, 1.2f), fireflyMat, false);
            var maze = root.gameObject.AddComponent<FireflyMazePuzzle>();
            maze.sensor = SensorKind.Compass;
            maze.objective = "Guidez la luciole jusqu'à l'anneau doré";
            maze.tutorial = "La luciole avance toute seule dans le labyrinthe au sol. Sa flèche suit la direction dans laquelle vous êtes tourné.\n\nTournez lentement sur vous-même pour la faire virer à gauche ou à droite, sans toucher les murets. Au départ, elle part tout droit vers le fond.";
            maze.hint = "Pivotez d'un quart de tour à chaque virage, juste avant le muret.";
            maze.orb = orb.transform; maze.arrow = arrow; maze.start = fireflyStart; maze.goal = fireflyGoal; maze.walls = mazeWalls.ToArray();
            maze.fxAnchor = fireflyGoal;
            maze.cameraPose = Pose("Cam Firefly", root, new Vector3(3f, 7.5f, 1.2f), new Vector3(3f, 0f, 5.6f));
            var stMaze = MakeStation(root, "Luciole", new Vector3(-1.1f, 0f, 5.6f), maze);

            AddRoomManager(root, 1, door, exit, new PuzzleStep[] { compass, cardinal, chest, maze }, new[] { st, stRose, stChest, stMaze });
            return scene;
        }
    }
}
