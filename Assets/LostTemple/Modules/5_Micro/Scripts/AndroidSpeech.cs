using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Pont vers la reconnaissance vocale d'Android (android.speech.SpeechRecognizer), appelée depuis C#
    /// avec AndroidJavaObject. Les résultats arrivent sur le fil Android : on les stocke ici et l'énigme
    /// les lit dans son Update (fil de Unity). Sur PC / éditeur, Available vaut faux.
    /// </summary>
    public static class AndroidSpeech
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject activity, recognizer;
        static Listener listener;
#endif
        static readonly object gate = new object();
        static string result;
        static string partial;
        static int error = -1;

        /// <summary>Vrai si le téléphone propose un service de reconnaissance vocale (ex. Google).</summary>
        public static bool Available
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    if (activity == null) activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
                    return new AndroidJavaClass("android.speech.SpeechRecognizer").CallStatic<bool>("isRecognitionAvailable", activity);
                }
                catch (System.Exception e) { Debug.LogWarning("[Speech] indisponible : " + e.Message); return false; }
#else
                return false;
#endif
            }
        }

        /// <summary>Lance une écoute en anglais. Le résultat sera lu avec TakeResult / TakeError.</summary>
        public static void StartListening(string language = "en-US")
        {
            lock (gate) { result = null; partial = null; error = -1; }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (activity == null) activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    if (recognizer == null)
                    {
                        recognizer = new AndroidJavaClass("android.speech.SpeechRecognizer").CallStatic<AndroidJavaObject>("createSpeechRecognizer", activity);
                        listener = new Listener();
                        recognizer.Call("setRecognitionListener", listener);
                    }
                    var intent = new AndroidJavaObject("android.content.Intent", "android.speech.action.RECOGNIZE_SPEECH");
                    intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE_MODEL", "free_form");
                    intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE", language);
                    intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.PARTIAL_RESULTS", true);
                    intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.MAX_RESULTS", 5);
                    recognizer.Call("startListening", intent);
                }
                catch (System.Exception e) { Debug.LogWarning("[Speech] démarrage impossible : " + e.Message); lock (gate) error = 99; }
            }));
#endif
        }

        public static void Stop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (activity == null || recognizer == null) return;
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try { recognizer.Call("destroy"); } catch { }
                recognizer = null;
            }));
#endif
        }

        /// <summary>Phrases reconnues (alternatives séparées par « | »), une seule fois ; null s'il n'y a rien de nouveau.</summary>
        public static string TakeResult() { lock (gate) { var r = result; result = null; return r; } }
        public static string Partial { get { lock (gate) return partial; } }
        /// <summary>Code d'erreur Android (ex. 7 = rien reconnu, 6 = silence), -1 s'il n'y en a pas.</summary>
        public static int TakeError() { lock (gate) { int e = error; error = -1; return e; } }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>Implémente l'interface Java android.speech.RecognitionListener.</summary>
        class Listener : AndroidJavaProxy
        {
            public Listener() : base("android.speech.RecognitionListener") { }
            public void onReadyForSpeech(AndroidJavaObject bundle) { }
            public void onBeginningOfSpeech() { }
            public void onRmsChanged(float rmsdB) { }
            public void onBufferReceived(byte[] buffer) { }
            public void onEndOfSpeech() { }
            public void onError(int code) { lock (gate) error = code; Debug.Log("[Speech] erreur " + code); }
            public void onResults(AndroidJavaObject bundle) { string r = Extract(bundle); lock (gate) result = r ?? ""; Debug.Log("[Speech] résultat : " + r); }
            public void onPartialResults(AndroidJavaObject bundle) { string r = Extract(bundle); lock (gate) partial = r; }
            public void onEvent(int type, AndroidJavaObject bundle) { }

            static string Extract(AndroidJavaObject bundle)
            {
                if (bundle == null) return null;
                var list = bundle.Call<AndroidJavaObject>("getStringArrayList", "results_recognition");
                if (list == null) return null;
                int n = list.Call<int>("size");
                var parts = new string[n];
                for (int i = 0; i < n; i++) parts[i] = list.Call<string>("get", i);
                return string.Join("|", parts);
            }
        }
#endif
    }
}
