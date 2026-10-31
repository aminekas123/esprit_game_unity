using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static LostTemple.UIStyle;

namespace LostTemple
{
    /// <summary>
    /// Module Caméra — viseur plein écran : caméra arrière du téléphone, cadre doré, scanner de couleur en direct,
    /// prise de photo. Si la caméra est absente ou refusée, des boutons de couleur simulent la photo.
    /// La photo est analysée puis oubliée : rien n'est enregistré ni envoyé.
    /// </summary>
    public class PhotoCameraUI : MonoBehaviour
    {
        static PhotoCameraUI instance;

        public static PhotoCameraUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UIManager.Instance.gameObject.AddComponent<PhotoCameraUI>();
                    instance.BuildCameraView();
                    instance.cameraView.SetActive(false);
                    UIManager.Instance.OverlaysHidden += instance.Hide;
                }
                return instance;
            }
        }

        GameObject cameraView;


        RawImage cameraImage;
        AspectRatioFitter cameraAspect;
        Text cameraInstruction;
        GameObject cameraShootButton, cameraFallbackRow;
        Image cameraFlash;
        // Scanner en direct : part de la couleur demandée au centre du cadre, mise à jour 4 fois par seconde
        GameObject scanBar;
        Image scanFill;
        Text scanText;
        PhotoTarget scanTarget;
        const float ScanThreshold = 0.3f;
        WebCamTexture webcam;
        Action<Color32[]> cameraCallback;

        void BuildCameraView()
        {
            var bg = Panel("Camera", UIManager.Instance.Root, Color.black);
            Stretch(bg.rectTransform);
            cameraView = bg.gameObject;
            var viewport = Rect("Viewport", cameraView.transform);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            cameraImage = Rect("Feed", viewport).gameObject.AddComponent<RawImage>();
            cameraImage.rectTransform.anchorMin = cameraImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            cameraAspect = cameraImage.gameObject.AddComponent<AspectRatioFitter>();
            cameraAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            // Cadre de visée : seule la zone centrale est analysée
            var frame = Rect("Frame", cameraView.transform);
            frame.anchorMin = new Vector2(0.3f, 0.4f); frame.anchorMax = new Vector2(0.7f, 0.6f);
            frame.offsetMin = frame.offsetMax = Vector2.zero;
            foreach (var (amin, amax) in new[] { (new Vector2(0, 0), new Vector2(1, 0)), (new Vector2(0, 1), new Vector2(1, 1)), (new Vector2(0, 0), new Vector2(0, 1)), (new Vector2(1, 0), new Vector2(1, 1)) })
            {
                var line = Panel("Edge", frame, Gold);
                line.rectTransform.anchorMin = amin; line.rectTransform.anchorMax = amax;
                line.rectTransform.sizeDelta = new Vector2(8, 8);
                line.raycastTarget = false;
            }

            var top = Panel("Instruction", cameraView.transform, new Color(0, 0, 0, 0.6f));
            top.rectTransform.anchorMin = new Vector2(0, 1); top.rectTransform.anchorMax = Vector2.one; top.rectTransform.pivot = new Vector2(0.5f, 1);
            top.rectTransform.sizeDelta = new Vector2(0, 260);
            cameraInstruction = Label(top.transform, "", 46);
            Stretch(cameraInstruction.rectTransform, 30, 20, 30, 20);

            var bottom = Rect("Bottom", cameraView.transform);
            bottom.anchorMin = new Vector2(0, 0); bottom.anchorMax = new Vector2(1, 0); bottom.pivot = new Vector2(0.5f, 0);
            bottom.sizeDelta = new Vector2(0, 520);
            Column(bottom, 20, 40).childAlignment = TextAnchor.LowerCenter;
            cameraShootButton = MakeButton(bottom, "Prendre la photo", TakePhoto, 170, new Color(0.55f, 0.38f, 0.1f)).gameObject;
            cameraFallbackRow = Rect("Fallback", bottom).gameObject;
            Size(cameraFallbackRow.transform as RectTransform, 150);
            Size(cameraFallbackRow.transform as RectTransform, 300);
            var hl = cameraFallbackRow.AddComponent<GridLayoutGroup>();
            hl.cellSize = new Vector2(235, 135); hl.spacing = new Vector2(12, 12);
            hl.childAlignment = TextAnchor.MiddleCenter; hl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; hl.constraintCount = 4;
            foreach (PhotoTarget t in Enum.GetValues(typeof(PhotoTarget)))
            {
                var tt = t;
                string label = t == PhotoTarget.Bright ? "Clair" : t == PhotoTarget.Dark ? "Sombre" : t == PhotoTarget.Detailed ? "Motifs" : PhotoPuzzle.Describe(t).Substring(0, 1) + PhotoPuzzle.Describe(t).Substring(1).ToLower();
                var b = MakeButton(cameraFallbackRow.transform, label, () => SimulatedPhoto(tt), 135, PhotoPuzzle.Sample(t) * 0.8f);
                b.GetComponentInChildren<Text>().fontSize = 36;
            }

            var sb = Panel("Scanner", cameraView.transform, new Color(0, 0, 0, 0.6f));
            sb.rectTransform.anchorMin = new Vector2(0.15f, 0.33f); sb.rectTransform.anchorMax = new Vector2(0.85f, 0.37f);
            sb.rectTransform.offsetMin = sb.rectTransform.offsetMax = Vector2.zero;
            scanFill = Panel("Fill", sb.transform, Gold);
            scanFill.rectTransform.anchorMin = Vector2.zero; scanFill.rectTransform.anchorMax = new Vector2(0, 1);
            scanFill.rectTransform.offsetMin = scanFill.rectTransform.offsetMax = Vector2.zero;
            scanText = Label(sb.transform, "", 34);
            Stretch(scanText.rectTransform);
            scanBar = sb.gameObject;

            cameraFlash = Panel("Flash", cameraView.transform, new Color(1, 1, 1, 0));
            Stretch(cameraFlash.rectTransform);
            cameraFlash.raycastTarget = false;
        }

        /// <summary>
        /// Ouvre l'appareil photo (caméra arrière du téléphone). Si la caméra est absente ou refusée,
        /// des boutons de couleur simulent la photo (commande alternative).
        /// </summary>
        public void Show(string instruction, PhotoTarget target, Action<Color32[]> onShot)
        {
            cameraCallback = onShot;
            scanTarget = target;
            cameraView.SetActive(true);
            cameraView.transform.SetAsLastSibling();
            bool fallback = SensorManager.Instance.UsesFallback(SensorKind.Camera);
            cameraInstruction.text = instruction + (fallback ? "\n<size=34><color=#F2B847>Caméra indisponible : choisissez la couleur à « photographier ».</color></size>" : "\n<size=34>Visez l'objet dans le cadre doré.</size>");
            cameraShootButton.SetActive(!fallback);
            scanBar.SetActive(!fallback);
            scanText.text = "Visez l'objet…";
            scanFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            cameraFallbackRow.SetActive(fallback);
            cameraImage.color = fallback ? new Color(0.1f, 0.1f, 0.15f) : Color.white;
            if (!fallback) StartCoroutine(StartWebcam());
        }

        IEnumerator StartWebcam()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                for (float t = 0; t < 10f && !UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera); t += Time.unscaledDeltaTime)
                    yield return null;
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                {
                    cameraShootButton.SetActive(false);
                    cameraFallbackRow.SetActive(true);
                    UIManager.Instance.ShowToast("Caméra refusée : utilisez les boutons de couleur.");
                    yield break;
                }
            }
#endif
            if (webcam == null)
            {
                string device = null;
                foreach (var d in WebCamTexture.devices) if (!d.isFrontFacing) { device = d.name; break; }
                if (device == null && WebCamTexture.devices.Length > 0) device = WebCamTexture.devices[0].name;
                webcam = new WebCamTexture(device, 1280, 720, 30);
            }
            cameraImage.texture = webcam;
            webcam.Play();
            for (float t = 0; t < 3f && webcam.width < 32; t += Time.unscaledDeltaTime) yield return null;
            float nextScan = 0f;
            while (cameraView.activeSelf && webcam != null && webcam.isPlaying)
            {
                if (Time.unscaledTime >= nextScan && webcam.width >= 32)
                {
                    nextScan = Time.unscaledTime + 0.25f;
                    float share = PhotoPuzzle.Share(CenterPixels(8), scanTarget);
                    bool ok = share >= ScanThreshold;
                    scanFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(share / (ScanThreshold * 2f)), 1f);
                    scanFill.color = ok ? new Color(0.35f, 0.85f, 0.45f) : Gold;
                    scanText.text = $"{PhotoPuzzle.Describe(scanTarget)} : {share * 100f:0} %" + (ok ? "  ✓ Prenez la photo !" : "");
                }
                cameraAspect.aspectRatio = webcam.height > 0 ? webcam.width / (float)webcam.height : 1.5f;
                cameraImage.rectTransform.localEulerAngles = new Vector3(0, 0, -webcam.videoRotationAngle);
                cameraImage.uvRect = webcam.videoVerticallyMirrored ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
                yield return null;
            }
        }

        /// <summary>Pixels de la zone centrale (40 % × 40 %), un sur <paramref name="step"/>.</summary>
        Color32[] CenterPixels(int step)
        {
            var all = webcam.GetPixels32();
            int w = webcam.width, h = webcam.height;
            var list = new System.Collections.Generic.List<Color32>();
            for (int y = (int)(h * 0.3f); y < h * 0.7f; y += step)
                for (int x = (int)(w * 0.3f); x < w * 0.7f; x += step)
                    list.Add(all[y * w + x]);
            return list.ToArray();
        }

        void TakePhoto()
        {
            if (webcam == null || !webcam.isPlaying || webcam.width < 32) { UIManager.Instance.ShowToast("La caméra démarre…"); return; }
            var all = webcam.GetPixels32();
            int w = webcam.width, h = webcam.height;
            // Zone centrale (40 % × 40 %), un pixel sur 4 : la photo n'est ni enregistrée ni envoyée.
            var list = new System.Collections.Generic.List<Color32>();
            for (int y = (int)(h * 0.3f); y < h * 0.7f; y += 4)
                for (int x = (int)(w * 0.3f); x < w * 0.7f; x += 4)
                    list.Add(all[y * w + x]);
            StartCoroutine(Shutter(list.ToArray()));
        }

        void SimulatedPhoto(PhotoTarget t)
        {
            var px = new Color32[64];
            Color c = PhotoPuzzle.Sample(t);
            for (int i = 0; i < px.Length; i++)
                px[i] = t == PhotoTarget.Detailed ? (((i + i / 8) & 1) == 0 ? Color.white : Color.black) : c;
            StartCoroutine(Shutter(px));
        }

        IEnumerator Shutter(Color32[] pixels)
        {
            AudioManager.Instance.Play(Sfx.Shutter);
            if (webcam != null && webcam.isPlaying) webcam.Pause();
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                cameraFlash.color = new Color(1, 1, 1, 1f - t / 0.35f);
                yield return null;
            }
            cameraFlash.color = Color.clear;
            yield return new WaitForSecondsRealtime(0.3f);
            Hide();
            var cb = cameraCallback;
            cameraCallback = null;
            cb?.Invoke(pixels);
        }

        public void Hide()
        {
            if (webcam != null) webcam.Stop();
            if (cameraView != null) cameraView.SetActive(false);
        }
    }
}
