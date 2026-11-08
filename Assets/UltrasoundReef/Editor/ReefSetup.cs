using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UltrasoundReef;

public static class ReefSetup
{
    [MenuItem("Ultrasound Reef/Create Step 1 Scene")]
    public static void CreateScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Reef Flow - six stages", typeof(ReefFlow));
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/UltrasoundReef_Step1.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("Ultrasound Reef step 1 scene created.");
    }
}
