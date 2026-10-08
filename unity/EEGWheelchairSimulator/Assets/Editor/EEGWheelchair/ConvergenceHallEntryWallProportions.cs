using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallEntryWallProportions
    {
        static string Collisions(GameObject root)=>(string)typeof(ConvergenceHallWallSurfaceCleanup).GetMethod("ColliderState",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{root});
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Validate(GameObject root)
        {
            typeof(ConvergenceHallMainEntrance).GetMethod("Validate",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{root});
            var entry=root.transform.Find("MainEntranceUpgrade");
            var stone=entry.Find("OppositeInformationWall/VerticalStoneSkin").GetComponent<Renderer>();
            var seating=entry.Find("SeatingWallTreatment/VerticalStoneSkin").GetComponent<Renderer>();
            Check(Mathf.Abs(stone.bounds.min.z)<.002f&&Mathf.Abs(stone.bounds.max.z-8.064f)<.002f,"Directory finish does not cover vestibule through corridor");
            Check(stone.sharedMaterial==seating.sharedMaterial,"Stone finish mismatch");
            var donor=entry.Find("SeatingCorner/DonorWallPanel/FullHeightWhiteDonorWall").GetComponent<Renderer>();
            Check(Mathf.Abs(donor.bounds.size.z-4.6f)<.002f&&Mathf.Abs(seating.bounds.min.z-donor.bounds.max.z)<.002f,"Donor wall and stone transition mismatch");
            Check(Mathf.Abs(entry.Find("SeatingLightwell/TallWhiteSideWall").GetComponent<Renderer>().bounds.max.y-5.35f)<.002f,"Tall side wall missing");
            var directory=entry.Find("OppositeInformationWall/FourFloorDirectory/DirectoryBacking").GetComponent<Renderer>();
            Check(directory.bounds.min.z>Depth()&&directory.bounds.max.z<7.55f,"Directory does not fit between vestibule and escape plan");
            foreach(var r in entry.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m&&m.HasProperty("_EmissionColor"))Check(m.GetColor("_EmissionColor").maxColorComponent==0,"Entrance emission restored");
            ConvergenceHallWallSurfaceCleanup.Validate(root);
            Debug.Log("ENTRY_WALL_PROPORTIONS_OK: continuous 8.064m directory finish includes both doorbanks; 4.6m donor panel joins 3.464m stone; 5.35m upper wall/lightwell; directory fits; 6.75x3m doors and collision unchanged; no emission or TV.");
        }
        static float Depth()=>ConvergenceHallMainEntrance.Depth;
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                string before=Collisions(root);
                var chairs=root.transform.Find("ClassroomInteriors").GetComponentsInChildren<MeshFilter>(true).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).ToArray();
                ConvergenceHallWallSurfaceCleanup.Restore(root);
                ConvergenceHallMainEntrance.ApplyTo(root);
                ConvergenceHallLecternLighting.DisableEmission(root);
                ConvergenceHallWallSurfaceCleanup.ApplyTo(root);
                Check(Collisions(root)==before,"Entrance update changed existing collision");
                Check(chairs.SequenceEqual(root.transform.Find("ClassroomInteriors").GetComponentsInChildren<MeshFilter>(true).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh))),"Classroom meshes changed");
                Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
            SessionState.SetBool("EEG.ConvergenceQA.FocusEntryWalls",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;
            var cam=follow.GetComponent<Camera>();cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            var entry=GameObject.Find("ConvergenceHallB1").transform.Find("MainEntranceUpgrade");
            var eyes=new[]{new Vector3(1.8f,1.55f,6.5f),new Vector3(.9f,1.5f,7.3f),new Vector3(.9f,1.5f,1.1f),new Vector3(3.6f,1.45f,6.8f)};
            var targets=new[]{new Vector3(6.69f,1.65f,3.8f),new Vector3(-3.36f,1.45f,4.5f),new Vector3(-3.36f,1.45f,2.8f),new Vector3(5.3f,3.55f,1.8f)};
            string[] names={"DonorWallProportions","DirectoryWallProportions","ContinuousVestibuleWall","SeatingUpperWall"};
            for(int i=0;i<eyes.Length;i++)
            {
                cam.fieldOfView=i==3?75:67;cam.transform.position=entry.TransformPoint(eyes[i]);cam.transform.LookAt(entry.TransformPoint(targets[i]));
                double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,names[i]+".png");var requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Entry wall screenshot");yield return null;}
            }
            Debug.Log("ENTRY_WALL_CAPTURE_OK: both side walls, continuous vestibule finish and upper seating wall.");
        }
    }
}
