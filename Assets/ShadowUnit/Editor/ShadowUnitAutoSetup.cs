using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShadowUnit.EditorTools
{
    /// <summary>
    /// Sets the project up automatically the first time Unity opens it after the ShadowUnit folder is copied in:
    /// creates the base assets and the Frontline scene and opens it, then sets Active Input Handling to Both and
    /// installs the Input System package (gamepads). Runs once per project on this computer; the
    /// Shadow Unit menu still has every step if you need to redo one.
    /// </summary>
    [InitializeOnLoad]
    static class ShadowUnitAutoSetup
    {
        static string Key => "ShadowUnit.AutoSetup.v1." + Application.dataPath;

        static ShadowUnitAutoSetup()
        {
            if (EditorPrefs.GetBool(Key, false)) return;
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Shadow Unit/Run First-Time Setup Again", priority = 40)]
        static void RunAgain()
        {
            EditorPrefs.DeleteKey(Key);
            Run();
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run; // try again once Unity is idle
                return;
            }

            // 1. scene and assets
            bool sceneExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ShadowUnitSetup.ScenePath) != null;
            if (!sceneExists && !ShadowUnitSetup.BuildPrototypeScene(false)) return; // user cancelled saving their scene: ask again next time
            if (sceneExists) ShadowUnitSetup.CreateBaseAssets();
            if (EditorSceneManager.GetActiveScene().path != ShadowUnitSetup.ScenePath && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ShadowUnitSetup.ScenePath);

            // 2. input: Both + Input System package (needs a restart)
            bool packageInstalled =
#if SU_INPUT_SYSTEM
                true;
#else
                false;
#endif
            bool inputReady = packageInstalled && ShadowUnitSetup.InputHandlingIsBoth();
            if (!inputReady) ShadowUnitSetup.ApplyInputSetup(false);

            EditorPrefs.SetBool(Key, true);
            EditorUtility.DisplayDialog("Shadow Unit is set up",
                inputReady
                    ? "The Frontline scene is open and ready.\n\nPress Play (the ▶ button at the top), then choose DEPLOY.\n\nIf any red errors appear in the Console window, copy them to Claude."
                    : "The Frontline scene is created and open.\n\nOne last thing: the Input System package (for gamepads) is installing now. " +
                      "Wait until the progress bar in the bottom-right corner finishes, then close and reopen Unity.\n\n" +
                      "If Unity asks to enable the new input backends, choose Yes. After reopening, press Play (▶) and choose DEPLOY.",
                "OK");
        }
    }
}
