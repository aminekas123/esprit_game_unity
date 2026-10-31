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
    }
}
