using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

/// <summary>Installs the artwork's dependencies through UPM before importing its Editor tools.</summary>
public static class ReefPackageInstaller
{
    static AddAndRemoveRequest request;
    static double deadline;
    static bool batch;

    [MenuItem("UnderTheSea/Migration/Install Reef Dependencies")]
    public static void Install()
    {
        batch = Application.isBatchMode;
        // The source uses Input System; pin the version already resolved by this project's Meta SDK.
        request = Client.AddAndRemove(new[] {
            "com.unity.render-pipelines.universal@17.3.0",
            "com.unity.inputsystem@1.20.1"
        }, Array.Empty<string>());
        deadline = EditorApplication.timeSinceStartup + 600;
        EditorApplication.update += Poll;
    }

    static void Poll()
    {
        if (!request.IsCompleted && EditorApplication.timeSinceStartup < deadline) return;
        EditorApplication.update -= Poll;
        bool success = request.IsCompleted && request.Status == StatusCode.Success;
        string message = success ? "URP 17.3.0 and Input System 1.20.1 resolved by UPM."
            : request.Error?.message ?? "UPM dependency installation timed out.";
        Directory.CreateDirectory("Migration~/2026-11-05");
        File.WriteAllText("Migration~/2026-11-05/package-install.txt", message);
        if (success) Debug.Log("[ReefMigration] " + message);
        else Debug.LogError("[ReefMigration] " + message);
        if (batch) EditorApplication.Exit(success ? 0 : 1);
    }
}
