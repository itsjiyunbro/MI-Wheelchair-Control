using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    // Checks the saved scene after a clean import without rebuilding the environment.
    [InitializeOnLoad]
    public static class ConvergenceHallUploadValidation
    {
        static ConvergenceHallUploadValidation()
        {
            if(Application.isBatchMode&&Environment.GetCommandLineArgs().Any(a=>a.Contains("ConvergenceHallUploadValidation.")))
                EditorApplication.update+=SkipBatchSearchStartup;
        }
        static void SkipBatchSearchStartup()
        {
            // Unity 6000.3 can fail in the editor's empty search database during headless
            // startup. This callback is unrelated to scene, font, input or render checks.
            // Remove only that background callback for this explicit batch validation.
            var pending=EditorApplication.delayCall;if(pending==null)return;
            foreach(var callback in pending.GetInvocationList())
                if(callback.Method.DeclaringType?.FullName=="UnityEditor.Search.SearchInit"&&callback.Method.Name=="IndexationOnStartup")
                {
                    EditorApplication.delayCall-=(EditorApplication.CallbackFunction)callback;
                    Debug.Log("UPLOAD_BATCH_SEARCH: skipped editor background search indexing for headless validation.");
                }
        }
        public static void Run()=>CheckImport(false);
        public static void RunImportOnly()=>CheckImport(true);
        static void CheckImport(bool importOnly)
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            var scene=EditorSceneManager.GetActiveScene();
            foreach(var go in scene.GetRootGameObjects())
                foreach(var t in go.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)
                        throw new InvalidOperationException("Missing script after import: "+t.name);
            var root=GameObject.Find("ConvergenceHallB1");
            if(!root||root.GetComponentsInChildren<InteractiveDoor>().Length!=48)
                throw new InvalidOperationException("Saved environment or 48 interactive doors missing");
            ConvergenceHallB112DiagonalScreen.Validate(root);
            ConvergenceHallLoungePhoto.Validate(root);
            if(!UnityEngine.Object.FindFirstObjectByType<DemoFreeCamera>())
                throw new InvalidOperationException("Saved free camera missing");
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");
            if(!font||!font.HasCharacter('연')||!font.HasCharacter('세'))
                throw new InvalidOperationException("Bundled Korean font failed to import");
            Debug.Log("UPLOAD_FONT_OK: OFL Korean font imported with saved GUID references.");
            Debug.Log("UPLOAD_IMPORT_OK: saved B1 scene, no missing scripts, 48 doors, reduced diagonal screen, lounge and free camera loaded from a clean project copy.");
            SessionState.SetBool("EEG.ConvergenceQA.FocusDemoCamera",!importOnly);
            SessionState.SetBool("EEG.ConvergenceQA.FocusB124Sign",importOnly);
            SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",importOnly);
            ConvergenceHallB1Play.Begin();
        }
    }
}
