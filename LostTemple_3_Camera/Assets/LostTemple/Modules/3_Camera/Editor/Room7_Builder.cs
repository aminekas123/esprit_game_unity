using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LostTemple.EditorTools
{
    /// <summary>Module 3 — Caméra : construction de la salle 7 (esprit Coyote, photos, boss final, clé).</summary>
    public static partial class LostTempleSceneBuilder
    {
        // ================================================================== Salle 7 — Caméra : l'esprit Coyote

        static CoyoteSpirit Coyote(Transform root, Vector3 pos)
        {
            var mat = Mat("Spirit", new Color(0.4f, 0.6f, 0.8f), 0f, 0.7f, true);
            var dark = Mat("Black", new Color(0.05f, 0.05f, 0.05f), 0f, 0.6f);
            var eye = Mat("SpiritEye", new Color(0.9f, 0.95f, 1f), 0f, 0.9f, true);
            eye.SetColor("_EmissionColor", new Color(0.8f, 0.95f, 1f) * 3f);
            var spirit = new List<Renderer>();

            var go = new GameObject("Coyote Spirit");
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, 180f, 0);

            GameObject P(PrimitiveType t, string n, Transform parent, Vector3 p, Vector3 s, Vector3 rot)
            {
                var o = Prim(t, n, parent, p, s, mat, false);
                o.transform.localRotation = Quaternion.Euler(rot);
                spirit.Add(o.GetComponent<Renderer>());
                return o;
            }

            P(PrimitiveType.Sphere, "Haunch", go.transform, new Vector3(0, 0.45f, -0.25f), new Vector3(0.85f, 0.75f, 1f), Vector3.zero);
            P(PrimitiveType.Capsule, "Chest", go.transform, new Vector3(0, 0.95f, 0.2f), new Vector3(0.55f, 0.55f, 0.55f), new Vector3(-20, 0, 0));
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Capsule, "Front Leg", go.transform, new Vector3(s * 0.15f, 0.38f, 0.45f), new Vector3(0.14f, 0.38f, 0.14f), Vector3.zero);
                P(PrimitiveType.Sphere, "Back Paw", go.transform, new Vector3(s * 0.3f, 0.1f, 0.05f), new Vector3(0.22f, 0.15f, 0.4f), Vector3.zero);
            }

            var head = Empty("Head", go.transform, new Vector3(0, 1.5f, 0.4f));
            P(PrimitiveType.Sphere, "Skull", head, Vector3.zero, new Vector3(0.48f, 0.42f, 0.5f), Vector3.zero);
            P(PrimitiveType.Capsule, "Snout", head, new Vector3(0, -0.06f, 0.33f), new Vector3(0.2f, 0.22f, 0.2f), new Vector3(90, 0, 0));
            Prim(PrimitiveType.Sphere, "Nose", head, new Vector3(0, -0.03f, 0.56f), Vector3.one * 0.08f, dark, false);
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Cube, "Ear", head, new Vector3(s * 0.15f, 0.3f, -0.05f), new Vector3(0.12f, 0.32f, 0.05f), new Vector3(0, 0, -s * 15f));
                Prim(PrimitiveType.Sphere, "Eye", head, new Vector3(s * 0.11f, 0.07f, 0.21f), Vector3.one * 0.07f, eye, false);
            }
            var jaw = Empty("Jaw", head, new Vector3(0, -0.15f, 0.2f));
            P(PrimitiveType.Cube, "Jaw Bone", jaw, new Vector3(0, 0, 0.18f), new Vector3(0.15f, 0.05f, 0.32f), Vector3.zero);

            var tail = Empty("Tail", go.transform, new Vector3(0, 0.35f, -0.7f));
            P(PrimitiveType.Capsule, "Tail Fur", tail, new Vector3(0, 0.15f, -0.3f), new Vector3(0.2f, 0.42f, 0.2f), new Vector3(60, 0, 0));

            // Aura d'esprit
            var aura = new GameObject("Aura");
            aura.transform.SetParent(go.transform, false);
            aura.transform.localPosition = new Vector3(0, 0.9f, 0);
            var ps = aura.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = new Color(0.5f, 0.85f, 1f, 0.8f);
            main.gravityModifier = -0.05f;
            var em = ps.emission; em.rateOverTime = 30f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.8f;
            aura.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
            var al = new GameObject("Spirit Light").AddComponent<Light>();
            al.transform.SetParent(go.transform, false);
            al.transform.localPosition = new Vector3(0, 1.4f, 0.8f);
            al.type = LightType.Point; al.color = new Color(0.5f, 0.8f, 1f); al.range = 5f; al.intensity = 2f; al.shadows = LightShadows.None;

            var c = go.AddComponent<CoyoteSpirit>();
            c.head = head; c.jaw = jaw; c.tail = tail;
            c.spiritParts = spirit.ToArray();
            return c;
        }

        static Transform Key(Transform parent, string name, Vector3 pos)
        {
            var k = Empty(name, parent, pos);
            var ring = Prim(PrimitiveType.Cylinder, "Ring", k, new Vector3(0, 0.25f, 0), new Vector3(0.28f, 0.03f, 0.28f), Gold, false);
            ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Box("Shaft", k, new Vector3(0, -0.05f, 0), new Vector3(0.06f, 0.5f, 0.06f), Gold, false);
            Box("Tooth 1", k, new Vector3(0.07f, -0.22f, 0), new Vector3(0.1f, 0.05f, 0.05f), Gold, false);
            Box("Tooth 2", k, new Vector3(0.07f, -0.12f, 0), new Vector3(0.07f, 0.05f, 0.05f), Gold, false);
            return k;
        }

        [RoomBuilder(6, "Room7_Coyote", "3_Camera")]
        static Scene BuildRoom7()
        {
            var (scene, root, cam) = NewRoom("Room7", new Vector3(0, 10f, -16f), new Vector3(0, 0, -6f), 60f, new Color(0.13f, 0.14f, 0.24f));
            Shell(root, 14f, -12f, 12f);

            // Mur intérieur avec la dernière porte ; l'artefact est derrière
            Box("Inner Wall L", root, new Vector3(-4.3f, 2.5f, 6f), new Vector3(5.4f, 5f, 0.8f), Sand);
            Box("Inner Wall R", root, new Vector3(4.3f, 2.5f, 6f), new Vector3(5.4f, 5f, 0.8f), Sand);
            Box("Inner Wall Top", root, new Vector3(0, 4.1f, 6f), new Vector3(3.2f, 1.8f, 0.8f), Sand);
            var door = Door(root, new Vector3(0f, 1.5f, 6f));
            var lockPlate = Box("Lock", root, new Vector3(0.7f, 1.4f, 5.7f), new Vector3(0.35f, 0.45f, 0.08f), Glyph, false);
            var keyInLock = Key(root, "Key In Lock", new Vector3(0.7f, 1.4f, 5.55f));
            keyInLock.localRotation = Quaternion.Euler(-90, 0, 0);
            keyInLock.gameObject.SetActive(false);

            Prim(PrimitiveType.Cylinder, "Pedestal", root, new Vector3(0, 0.6f, 9.5f), new Vector3(1.2f, 0.6f, 1.2f), Carved);
            Prim(PrimitiveType.Cylinder, "Pedestal Ring", root, new Vector3(0, 1.22f, 9.5f), new Vector3(1.3f, 0.04f, 1.3f), Gold, false);
            var art = Box("Artefact", root, new Vector3(0, 1.8f, 9.5f), Vector3.one * 0.55f, Gold, false);
            art.transform.rotation = Quaternion.Euler(45, 0, 45);
            art.AddComponent<Spinner>();
            var artL = new GameObject("Artefact Light").AddComponent<Light>();
            artL.transform.SetParent(root, false);
            artL.transform.localPosition = new Vector3(0, 2.4f, 9f);
            artL.type = LightType.Point; artL.color = new Color(1f, 0.75f, 0.35f); artL.range = 6f; artL.intensity = 3f;

            // Esprit Coyote sur son rocher
            Prim(PrimitiveType.Sphere, "Coyote Rock", root, new Vector3(-4f, 0.1f, 1.5f), new Vector3(2.2f, 0.6f, 1.8f), DarkStone);
            var coyote = Coyote(root, new Vector3(-4f, 0.35f, 1.5f));
            coyote.keyProp = Key(root, "Key", new Vector3(-4f, 2.5f, 1.5f));

            var photos = root.gameObject.AddComponent<PhotoPuzzle>();
            photos.sensor = SensorKind.Camera;
            photos.objective = "Accomplissez les 3 missions photo du Coyote";
            photos.tutorial = "« Je suis l'esprit Coyote, gardien de l'artefact. Pour gagner ma clé, rapporte-moi trois images du monde des vivants. »\n\nLa caméra du téléphone s'ouvre : photographiez dans la vraie vie un objet de la couleur demandée, bien au centre du cadre doré.\n\nLes photos sont analysées sur le téléphone puis oubliées : rien n'est enregistré.";
            photos.hint = "Approchez-vous d'un objet de la bonne couleur pour qu'il remplisse le cadre doré, avec assez de lumière.";
            photos.coyote = coyote;
            photos.fxAnchor = coyote.head;
            photos.cameraPose = Pose("Cam Coyote", root, new Vector3(-2.6f, 2.6f, -2.2f), new Vector3(-4f, 1.5f, 1.5f));

            var password = root.gameObject.AddComponent<PasswordPuzzle>();
            password.usesSensor = false;
            password.objective = "Répétez au Coyote son mot de passe";
            password.tutorial = "Le Coyote veut vérifier que vous avez bien retenu son mot de passe.\n\nTouchez les syllabes dans l'ordre où il vous les a données.";
            password.hint = "Le mot de passe est formé des 3 fragments obtenus avec les photos, dans l'ordre des missions.";
            password.coyote = coyote; password.photos = photos;
            password.fxAnchor = coyote.head;
            password.cameraPose = photos.cameraPose;

            // Boss final : le gardien des ténèbres surgit devant la porte
            var boss = Monster(root, new Vector3(0, 0.3f, 4.2f), true);
            var bossFight = root.gameObject.AddComponent<BossFightPuzzle>();
            bossFight.sensor = SensorKind.Light;
            bossFight.objective = "Vainquez le gardien des ténèbres";
            bossFight.tutorial = "Le gardien des ténèbres surgit du sol pour protéger l'artefact !\n\n<b>1.</b> Éblouissez-le : couvrez ou éclairez le capteur de lumière.\n<b>2.</b> Esquivez ses coups : penchez le téléphone du côté opposé à la plaque rouge.\n<b>3.</b> Achevez-le : criez pour pousser la boule d'énergie jusqu'à lui.";
            bossFight.hint = "Regardez la plaque qui rougit : c'est là qu'il va frapper. Penchez-vous de l'autre côté.";
            bossFight.boss = boss; bossFight.coyote = coyote;
            bossFight.bossHidden = Empty("Boss Hidden", root, new Vector3(0, -5f, 4.2f));
            bossFight.bossStand = Empty("Boss Stand", root, new Vector3(0, 0.3f, 4.2f));
            bossFight.arenaCenter = Empty("Arena Center", root, new Vector3(0, 0.05f, -0.5f));
            var slamMat = Mat("SlamMarker", new Color(0.25f, 0.08f, 0.06f), 0.2f, 0.4f, true);
            bossFight.slamMarkerLeft = Box("Slam Marker L", root, new Vector3(-1.6f, 0.02f, -0.5f), new Vector3(2.2f, 0.03f, 2.2f), slamMat, false).GetComponent<Renderer>();
            bossFight.slamMarkerRight = Box("Slam Marker R", root, new Vector3(1.6f, 0.02f, -0.5f), new Vector3(2.2f, 0.03f, 2.2f), slamMat, false).GetComponent<Renderer>();
            var bball = Prim(PrimitiveType.Sphere, "Boss Energy Ball", root, new Vector3(0, 0.95f, 0.6f), Vector3.one * 0.9f, Mat("Energy", new Color(0.4f, 0.75f, 1f), 0.2f, 0.9f, true), false);
            var bbl = new GameObject("Ball Light").AddComponent<Light>();
            bbl.transform.SetParent(bball.transform, false);
            bbl.type = LightType.Point; bbl.color = new Color(0.4f, 0.8f, 1f); bbl.range = 5f; bbl.intensity = 2f; bbl.shadows = LightShadows.None;
            bossFight.ball = bball.transform; bossFight.ballRenderer = bball.GetComponent<Renderer>(); bossFight.ballLight = bbl;
            bossFight.ballStart = Empty("Boss Ball Start", root, new Vector3(0, 0.95f, 0.8f));
            bossFight.ballEnd = Empty("Boss Ball End", root, new Vector3(0, 0.95f, 3.1f));
            var bimpact = FxBurst(root, "Boss Impact", new Vector3(0, 1.5f, 3.2f), new Color(0.6f, 0.4f, 1f, 1f), 0.15f, 0.5f, 1.2f, 8f, 0.2f, 160, new Vector3(0.5f, 0.5f, 0.5f));
            bimpact.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
            bossFight.impactFx = bimpact;
            bossFight.fxAnchor = bossFight.bossStand;
            bossFight.cameraPose = Pose("Cam Boss", root, new Vector3(0, 4.2f, -5.8f), new Vector3(0, 2f, 2.8f));

            var key = root.gameObject.AddComponent<KeyDoorPuzzle>();
            key.usesSensor = false;
            key.objective = "Ouvrez la dernière porte avec la clé";
            key.tutorial = "Vous avez la clé dorée du Coyote. Glissez-la dans la serrure de la porte.";
            key.hint = "Approchez-vous de la porte au fond de la salle.";
            key.keyInLock = keyInLock; key.lockGlow = lockPlate.GetComponent<Renderer>();
            key.fxAnchor = keyInLock;
            key.cameraPose = Pose("Cam Door", root, new Vector3(2.5f, 3.5f, 0.5f), new Vector3(0, 1.8f, 6f));

            Relic(root, new Vector3(-6f, 0.8f, -10.5f));
            Relic(root, new Vector3(6f, 0.8f, -6f));
            Relic(root, new Vector3(-6f, 0.8f, 3f));
            SpikeTrap(root, new Vector3(0, 0, -6f), new Vector2(14f, 1.6f), 0f, 3f);

            foreach (var t in new[] { new Vector3(-6.7f, 2.6f, -9f), new Vector3(6.7f, 2.6f, -9f), new Vector3(-6.7f, 2.6f, 2f), new Vector3(6.7f, 2.6f, 2f), new Vector3(-3f, 2.6f, 5.5f), new Vector3(3f, 2.6f, 5.5f) })
                Torch(root, t);

            var exit = Exit(root, new Vector3(0f, 0f, 8.6f));
            var st1 = MakeStation(root, "Coyote", new Vector3(-4f, 0f, -1.2f), photos);
            var st3 = MakeStation(root, "Porte", new Vector3(0f, 0f, 4.6f), key);
            var player = Explorer(root, new Vector3(0f, 0.05f, -10.5f), 0f, true);
            FollowCamera(cam, player);
            Secret(root, new Vector3(5.5f, 0.9f, 0f), 6);
            AddRoomManager(root, 6, door, exit, new PuzzleStep[] { photos, password, bossFight, key }, new[] { st1, null, null, st3 });
            return scene;
        }
    }
}
