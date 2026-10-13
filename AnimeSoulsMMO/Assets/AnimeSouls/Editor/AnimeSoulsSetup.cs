using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnimeSouls.EditorTools
{
    /// <summary>
    /// One-click project setup: enables the legacy Input Manager (the game uses it), creates the main scene
    /// and registers it in Build Settings. Runs automatically the first time the project is opened.
    /// </summary>
    [InitializeOnLoad]
    public static class AnimeSoulsSetup
    {
        const string ScenePath = "Assets/AnimeSouls/Scenes/Main.unity";
        const string DoneKey = "AnimeSouls.SetupDone.v1";

        static AnimeSoulsSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool(DoneKey, false)) return;
                EditorPrefs.SetBool(DoneKey, true);
                Setup();
            };
        }

        [MenuItem("Anime Souls/Setup Project")]
        public static void Setup()
        {
            bool restart = EnsureLegacyInput();
            EnsureScene();
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.productName = "Anime Souls MMO";
            PlayerSettings.companyName = "AnimeSouls";
            AssetDatabase.SaveAssets();
            if (restart)
            {
                EditorUtility.DisplayDialog("Anime Souls",
                    "Active Input Handling switched to \"Both\". Please restart the Unity editor, then open Assets/AnimeSouls/Scenes/Main.unity and press Play.",
                    "OK");
            }
            else Debug.Log("[AnimeSouls] Project ready. Open Assets/AnimeSouls/Scenes/Main.unity and press Play.");
        }

        [MenuItem("Anime Souls/Reset Save")]
        public static void ResetSave()
        {
            var path = Path.Combine(Application.persistentDataPath, "animesouls_save.json");
            if (File.Exists(path)) File.Delete(path);
            Debug.Log("[AnimeSouls] Save deleted: " + path);
        }

        /// <summary>Returns true when the setting was changed (editor restart required).</summary>
        static bool EnsureLegacyInput()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return false;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null) return false;
            // 0 = Input Manager (Old), 1 = Input System (New), 2 = Both
            if (prop.intValue == 1)
            {
                prop.intValue = 2;
                so.ApplyModifiedProperties();
                return true;
            }
            return false;
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var marker = new GameObject("README — the game is generated at runtime by AnimeSouls.Game");
                    marker.transform.position = Vector3.zero;
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
            }

            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                if (s.path == ScenePath) return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        [MenuItem("Anime Souls/Open Main Scene")]
        public static void OpenMain()
        {
            if (!File.Exists(ScenePath)) EnsureScene();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Anime Souls/Print Content Summary")]
        public static void Summary()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var a in AnimeDatabase.Anime)
            {
                sb.AppendLine($"{a.name} (tier {a.tier}) — {a.campName}");
                foreach (var s in a.souls)
                    sb.AppendLine($"   {s.name} [{s.rarity}] : {s.skills[0].name} / {s.skills[1].name} / {s.skills[2].name}");
            }
            Debug.Log(sb.ToString());
        }
    }
}
