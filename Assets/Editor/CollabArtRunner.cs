using System;
using System.IO;
using UnityEditor;

/// <summary>
/// Lets an external tool drive the already-open editor: drop Temp/collab-build.request (rebuild Main)
/// or Temp/collab-flow.request (run the flow smoke check). The outcome is written to Temp/collab-*.result.
/// Menu equivalents: UnderTheSea > Migration > Build Main With Imported Reef Art / Run Complete Flow Smoke Check.
/// </summary>
[InitializeOnLoad]
static class CollabArtRunner
{
    static CollabArtRunner() => EditorApplication.update += Poll;

    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Run("Temp/collab-build.request", "Temp/collab-build.result", BuildReefMainScene.Build);
        Run("Temp/collab-flow.request", "Temp/collab-flow.result", ReefMigrationChecks.RunFlow);
    }

    static void Run(string request, string result, Action action)
    {
        if (!File.Exists(request)) return;
        File.Delete(request);
        try { action(); File.WriteAllText(result, "OK " + DateTime.Now.ToString("s")); }
        catch (Exception e) { File.WriteAllText(result, "FAIL " + e); }
    }
}
