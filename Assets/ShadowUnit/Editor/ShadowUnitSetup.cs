using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowUnit.EditorTools
{
    /// <summary>Menu commands that create the prototype scene and the assets it loads at runtime.</summary>
    public static class ShadowUnitSetup
    {
        const string Root = "Assets/ShadowUnit";
        const string Res = "Assets/ShadowUnit/Resources/ShadowUnit";
        const string ScenePath = "Assets/ShadowUnit/Scenes/Frontline.unity";

        [MenuItem("Shadow Unit/Create Prototype Scene", priority = 0)]
        public static void CreatePrototypeScene()
        {
            CreateBaseAssets();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("ShadowUnit");
            var boot = go.AddComponent<GameBootstrap>();
            boot.primaryWeapon = AssetDatabase.LoadAssetAtPath<WeaponData>(Res + "/Weapons/VK7_Carbine.asset");

            EnsureFolder(Root + "/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.productName = "Shadow Unit Frontline";

            EditorUtility.DisplayDialog("Shadow Unit",
                "Prototype scene created at " + ScenePath + " and added to Build Settings.\n\n" +
                "Press Play to test. In the Editor, click the right half of the Game view to use mouse and keyboard (needs Active Input Handling set to 'Both').",
                "OK");
        }

        [MenuItem("Shadow Unit/Create Base Assets", priority = 1)]
        public static void CreateBaseAssets()
        {
            EnsureFolder(Res);
            EnsureFolder(Res + "/Weapons");
            bool urp = GraphicsSettings.currentRenderPipeline != null;

            Shader litShader = urp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            CreateOrUpdateMaterial(Res + "/SU_Lit.mat", litShader);
            CreateOrUpdateMaterial(Res + "/SU_Unlit.mat", Shader.Find("Sprites/Default"));

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material sky = CreateOrUpdateMaterial(Res + "/SU_Sky.mat", skyShader);
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.035f);
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1.15f);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.55f, 0.6f, 0.68f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.36f, 0.34f, 0.3f));
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.15f);
                EditorUtility.SetDirty(sky);
            }

            string weaponPath = Res + "/Weapons/VK7_Carbine.asset";
            if (AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath) == null)
            {
                var w = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(w, weaponPath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Shadow Unit/Set Input Handling To Both (restarts needed)", priority = 20)]
        public static void SetInputHandlingBoth()
        {
            Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings == null || settings.Length == 0) { ShowManualInputHelp(); return; }
            var so = new SerializedObject(settings[0]);
            SerializedProperty p = so.FindProperty("activeInputHandler");
            if (p == null) { ShowManualInputHelp(); return; }
            p.intValue = 2; // 0 = Input Manager (old), 1 = Input System, 2 = Both
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Shadow Unit", "Active Input Handling set to 'Both'. Restart Unity for it to take effect.", "OK");
        }

        static void ShowManualInputHelp()
        {
            EditorUtility.DisplayDialog("Shadow Unit", "Set it manually: Edit > Project Settings > Player > Other Settings > Active Input Handling = Both, then restart Unity.", "OK");
        }

        static Material CreateOrUpdateMaterial(string path, Shader shader)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (shader == null) { Debug.LogError("Shadow Unit: shader not found for " + path); return m; }
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader)
            {
                m.shader = shader; // render pipeline changed since the asset was made
                EditorUtility.SetDirty(m);
            }
            return m;
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
