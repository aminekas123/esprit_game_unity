using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LostTemple.EditorTools
{
    /// <summary>Module 2 — Gyroscope : construction de la salle 3 (statues) et du rocher géant.</summary>
    public static partial class LostTempleSceneBuilder
    {
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

        // ================================================================== Salle 3 — Gyroscope

        [RoomBuilder(2, "Room3_Statues", "2_Gyroscope")]
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

            Relic(root, new Vector3(-6f, 0.8f, -10.5f));
            Relic(root, new Vector3(6f, 0.8f, -6f));
            Relic(root, new Vector3(6f, 0.8f, 7.5f));

            foreach (var t in new[] { new Vector3(-6.7f, 2.6f, -9f), new Vector3(6.7f, 2.6f, -9f), new Vector3(-6.7f, 2.6f, 2f), new Vector3(6.7f, 2.6f, 2f), new Vector3(-6.7f, 2.6f, 10f), new Vector3(6.7f, 2.6f, 10f) })
                Torch(root, t);

            var door = Door(root, new Vector3(0f, 1.5f, 11.75f));
            var exit = Exit(root, new Vector3(0f, 0f, 10.7f));
            var st = MakeStation(root, "Statues", new Vector3(0f, 0f, 1.6f), statueAlign);
            var player = Explorer(root, new Vector3(0f, 0.05f, -10.5f), 0f, true);
            FollowCamera(cam, player);
            Secret(root, new Vector3(-5.5f, 0.9f, 8f), 2);

            // Amélioration : miroirs à orienter pour guider un rayon de lumière jusqu'au cristal
            var mirrorMat = Mat("Mirror", new Color(0.85f, 0.9f, 0.95f), 1f, 0.95f);
            var ringMat = Mat("MirrorRing", new Color(0.2f, 0.18f, 0.12f), 0.3f, 0.5f, true);
            var mirrors = new Transform[2];
            var rings = new Renderer[2];
            var mirrorPos = new[] { new Vector3(-3.5f, 0f, 7.5f), new Vector3(-3.5f, 0f, 10.5f) };
            var mirrorYaw = new[] { 25f, 100f };
            for (int i = 0; i < 2; i++)
            {
                Prim(PrimitiveType.Cylinder, "Mirror Base", root, mirrorPos[i] + new Vector3(0, 0.15f, 0), new Vector3(0.9f, 0.15f, 0.9f), Carved);
                rings[i] = Prim(PrimitiveType.Cylinder, "Mirror Ring", root, mirrorPos[i] + new Vector3(0, 0.31f, 0), new Vector3(1.1f, 0.01f, 1.1f), ringMat, false).GetComponent<Renderer>();
                var pivot = Empty("Mirror " + (i + 1), root, mirrorPos[i]);
                pivot.rotation = Quaternion.Euler(0, mirrorYaw[i], 0);
                Box("Mirror Glass", pivot, new Vector3(0, 1.3f, 0), new Vector3(1.3f, 1.4f, 0.08f), mirrorMat);
                Box("Mirror Frame", pivot, new Vector3(0, 1.3f, -0.06f), new Vector3(1.45f, 1.55f, 0.05f), Gold, false);
                mirrors[i] = pivot;
            }
            var source = Empty("Beam Source", root, new Vector3(-6.3f, 1.3f, 7.5f));
            source.rotation = Quaternion.LookRotation(Vector3.right);
            Box("Beam Emitter", root, new Vector3(-6.55f, 1.3f, 7.5f), new Vector3(0.4f, 0.4f, 0.4f), FlameLit, false);
            var crystal = Box("Beam Crystal", root, new Vector3(5.6f, 1.3f, 10.5f), new Vector3(0.5f, 0.9f, 0.5f), Mat("CrystalTarget", new Color(0.9f, 0.8f, 0.5f), 0.3f, 0.95f, true));
            crystal.transform.rotation = Quaternion.Euler(0, 45, 0);
            Prim(PrimitiveType.Cylinder, "Crystal Base", root, new Vector3(5.6f, 0.4f, 10.5f), new Vector3(0.8f, 0.4f, 0.8f), Carved);
            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(root, false);
            var lr = beamGo.AddComponent<LineRenderer>();
            lr.sharedMaterial = MarkerMat();
            lr.widthMultiplier = 0.08f;
            lr.useWorldSpace = true;
            lr.startColor = lr.endColor = new Color(1f, 0.85f, 0.45f, 1f);
            lr.positionCount = 0;

            var mirrorsPuzzle = root.gameObject.AddComponent<MirrorBeamPuzzle>();
            mirrorsPuzzle.sensor = SensorKind.Gyroscope;
            mirrorsPuzzle.objective = "Guidez le rayon de lumière jusqu'au cristal avec les miroirs";
            mirrorsPuzzle.tutorial = "Un rayon jaillit du mur. Faites pivoter le téléphone comme un volant : le miroir sélectionné (anneau doré) tourne avec lui.\n\nUtilisez le bouton « Miroir suivant » pour changer de miroir. Quand le rayon touche le cristal, gardez-le une seconde.";
            mirrorsPuzzle.hint = "Le premier miroir doit renvoyer le rayon vers le fond de la salle, le second vers le cristal à droite.";
            mirrorsPuzzle.source = source; mirrorsPuzzle.mirrors = mirrors; mirrorsPuzzle.mirrorHighlights = rings;
            mirrorsPuzzle.target = crystal.GetComponent<Collider>(); mirrorsPuzzle.targetRenderer = crystal.GetComponent<Renderer>();
            mirrorsPuzzle.beam = lr;
            mirrorsPuzzle.fxAnchor = crystal.transform;
            mirrorsPuzzle.cameraPose = Pose("Cam Mirrors", root, new Vector3(0f, 9.5f, 3.5f), new Vector3(0f, 1f, 9f));
            var st2 = MakeStation(root, "Miroirs", new Vector3(-1.2f, 0f, 6.8f), mirrorsPuzzle);

            var rm3 = AddRoomManager(root, 2, door, exit, new PuzzleStep[] { statueAlign, mirrorsPuzzle }, new[] { st, st2 });
            rm3.roomDoneListeners = new MonoBehaviour[] { MakeBoulder(root, 9.4f) };
            return scene;
        }
    }
}
