using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LostTemple.EditorTools
{
    /// <summary>Module 5 — Micro : construction de la salle 4 (monstre, boule poussée par la voix, zone de silence).</summary>
    public static partial class LostTempleSceneBuilder
    {
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

        // ================================================================== Salle 4 — Micro : le monstre

        [RoomBuilder(3, "Room4_Monstre", "5_Micro")]
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

            // Épreuve 3 : lire une phrase en anglais à voix haute pour ouvrir la porte
            var plaqueBox = Box("Speech Plaque", root, new Vector3(6.95f, 2.4f, 6f), new Vector3(0.1f, 2.2f, 3.6f), Mat("SpeechPlaque", new Color(0.08f, 0.07f, 0.07f), 0.1f, 0.3f, true), false);
            Box("Speech Plaque Frame", root, new Vector3(6.98f, 2.4f, 6f), new Vector3(0.05f, 2.5f, 3.9f), Gold, false);
            var plaqueText = Text3D("Speech Text", root, new Vector3(6.85f, 2.45f, 6f), "The golden key\nbelongs to me", 0.05f, new Color(1f, 0.95f, 0.8f));
            plaqueText.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var speak = root.gameObject.AddComponent<SpeakSentencePuzzle>();
            speak.sensor = SensorKind.Microphone;
            speak.objective = "Lisez à voix haute la phrase en anglais";
            speak.tutorial = "Une phrase en ANGLAIS apparaît sur la plaque. Lisez-la à voix haute, clairement, près du micro du téléphone.\n\nSur le téléphone, la reconnaissance vocale vérifie les mots prononcés (au moins 70 % de bons mots). Si elle ne comprend pas, elle vous réécoute.\n\nQuand la phrase est reconnue, la porte s'ouvre.";
            speak.hint = "Parlez lentement et articulez chaque mot. Approchez le bas du téléphone de votre bouche.";
            speak.plaque = plaqueText;
            speak.plaqueRenderer = plaqueBox.GetComponent<Renderer>();
            speak.fxAnchor = plaqueText.transform;
            speak.cameraPose = Pose("Cam Speech", root, new Vector3(0.8f, 3.2f, 6f), new Vector3(6.9f, 2.4f, 6f));
            var stSpeak = MakeStation(root, "Phrase", new Vector3(4.4f, 0f, 9f), speak);   // sur le côté : l'explorateur ne cache pas la plaque

            AddRoomManager(root, 3, door, exit, new PuzzleStep[] { wake, push, speak }, new[] { st, null, stSpeak });
            return scene;
        }
    }
}
