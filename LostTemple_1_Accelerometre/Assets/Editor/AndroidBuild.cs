using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Construit l'APK Android du module (menu Lost Temple → Construire l'APK Android, ou en ligne de commande).
/// Utilise le SDK Android d'Android Studio s'il existe, sinon celui d'Unity.
/// </summary>
public static class AndroidBuild
{
    const string OutputDir = "Builds";

    [MenuItem("Lost Temple/Construire l'APK Android")]
    public static void Build()
    {
        string home = System.Environment.GetEnvironmentVariable("HOME");
        string studioSdk = Path.Combine(home, "Library/Android/sdk");
        if (Directory.Exists(Path.Combine(studioSdk, "platform-tools")))
            AndroidExternalToolsSettings.sdkRootPath = studioSdk;

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        EditorUserBuildSettings.buildAppBundle = false;

        Directory.CreateDirectory(OutputDir);
        string apk = Path.Combine(OutputDir, PlayerSettings.productName.Replace(" — ", "_").Replace(' ', '_') + ".apk");
        var scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apk,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });
        Debug.Log($"[Lost Temple] Build Android : {report.summary.result} → {Path.GetFullPath(apk)} ({report.summary.totalSize / (1024 * 1024)} Mo, {report.summary.totalErrors} erreur(s))");
        if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
