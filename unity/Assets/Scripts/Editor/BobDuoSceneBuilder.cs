// Battle of the Brooms: bakes Assets/Scenes/DuoClean.unity (docs 32 M1). Run from the menu once; the scene itself only
// holds a camera, an EventSystem, a 1920x1080 canvas and the bootstrap object.
using Bob.Duo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bob.EditorTools
{
    public static class BobDuoSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/DuoClean.unity";

        [MenuItem("Bob/Build Duo Clean Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Bob.Game.BobUi.Bg;
            cam.orthographic = true;
            camGo.transform.position = new Vector3(0, 0, -10);

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("Duo", typeof(BobDuoBootstrap));

            EditorSceneManager.SaveScene(scene, ScenePath);

            // DuoClean sits after the three Task Race scenes (solo -> duo -> team -> duo co-op).
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath))
            {
                int at = scenes.FindIndex(s => s.path.EndsWith("TaskRace_Team.unity"));
                scenes.Insert(at < 0 ? scenes.Count : at + 1, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[DuoScene] " + ScenePath + " built and added to Build Settings");
        }

        [MenuItem("Bob/Open Duo Clean Scene")]
        public static void Open() => EditorSceneManager.OpenScene(ScenePath);

        [MenuItem("Bob/Run Duo Slice Check")]
        public static void RunCheck()
        {
            // Opening a scene during Play throws and would leave the devcheck flag armed for the next Play.
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[DuoCheck] stop Play mode first, then run Bob/Run Duo Slice Check");
                return;
            }
            PlayerPrefs.SetInt("bob.duo.devcheck", 1);
            PlayerPrefs.Save();
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
            catch (System.Exception e)
            {
                // Nothing will consume the flag if the scene is missing; clear it or the next Play of DuoClean arms a check.
                PlayerPrefs.SetInt("bob.duo.devcheck", 0);
                PlayerPrefs.Save();
                Debug.LogWarning("[DuoCheck] cannot open " + ScenePath + " — bake it first with Bob/Build Duo Clean Scene · " + e.Message);
                return;
            }
            EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem("Edit/Play");
        }
    }
}
