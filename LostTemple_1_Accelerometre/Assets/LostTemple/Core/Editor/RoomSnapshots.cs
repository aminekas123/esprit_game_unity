using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LostTemple.EditorTools
{
    /// <summary>
    /// Outil de vérification : rend des images de la caméra d'une salle (cadrages d'énigme + suivi du joueur
    /// à plusieurs positions) en format portrait, pour repérer un mur qui masquerait la vue.
    /// Ligne de commande : -executeMethod LostTemple.EditorTools.RoomSnapshots.Run -snapScene <chemin> -snapOut <dossier>
    /// </summary>
    public static class RoomSnapshots
    {
        public static void Run()
        {
            string scene = Arg("-snapScene"), outDir = Arg("-snapOut"), points = Arg("-snapPoints");
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene(scene);
            var cam = Camera.main;
            var rig = cam.GetComponent<CameraRig>();
            var rt = new RenderTexture(540, 960, 24);
            cam.targetTexture = rt;
            cam.aspect = 540f / 960f;
            foreach (var t in Object.FindObjectsByType<Transform>())
                if (t.name.StartsWith("Cam ")) Shot(cam, rt, t.position, t.rotation, Path.Combine(outDir, t.name.Replace(' ', '_') + ".png"));
            if (rig != null && !string.IsNullOrEmpty(points))
                foreach (var p in points.Split(';'))
                {
                    var v = p.Split(',');
                    var player = new Vector3(float.Parse(v[0], System.Globalization.CultureInfo.InvariantCulture), 0f, float.Parse(v[1], System.Globalization.CultureInfo.InvariantCulture));
                    Vector3 focus = player + Vector3.up + Vector3.forward * rig.lookAhead;
                    Vector3 pos = focus + rig.offset;
                    Shot(cam, rt, pos, Quaternion.LookRotation(focus - pos), Path.Combine(outDir, $"Follow_{v[0]}_{v[1]}.png"));
                }
        }

        static void Shot(Camera cam, RenderTexture rt, Vector3 pos, Quaternion rot, string file)
        {
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(tex);
        }

        static string Arg(string name)
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
