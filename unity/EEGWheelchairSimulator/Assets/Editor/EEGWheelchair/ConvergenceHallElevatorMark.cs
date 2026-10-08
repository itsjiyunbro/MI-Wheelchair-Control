using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallElevatorMark
    {
        static string CollisionState(GameObject root)=>(string)typeof(ConvergenceHallWallSurfaceCleanup).GetMethod("ColliderState",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{root});
        static double Area(MeshFilter f,Transform door)
        {
            var v=f.sharedMesh.vertices;var tr=f.sharedMesh.triangles;double total=0;
            for(int i=0;i<tr.Length;i+=3)
            {
                var a=door.InverseTransformPoint(f.transform.TransformPoint(v[tr[i]]));var b=door.InverseTransformPoint(f.transform.TransformPoint(v[tr[i+1]]));var c=door.InverseTransformPoint(f.transform.TransformPoint(v[tr[i+2]]));
                total+=Math.Abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))*.5;
            }
            return total;
        }
        static void Validate(GameObject root)
        {
            foreach(bool east in new[]{true,false})
            {
                var door=ConvergenceHallElevatorCabins.Door(root,east);var control=door.GetComponent<InteractiveDoor>();control.SetProgressInstant(0);
                var photo=door.Find("PhotoElevatorDetails");var source=photo.Find("UniversityDoorSeal/OfficialYonseiSymbol").GetComponent<MeshFilter>();
                if(source.sharedMesh!=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Meshes/ConvergenceHall/YonseiOfficialSymbol.asset")||photo.Find("UniversityDoorSeal").childCount!=1||photo.Find("UniversityDoorSeal").GetComponentsInChildren<Text>(true).Length!=0)throw new Exception("Elevator mark still simplified");
                var halves=photo.Find("DoorSealHalves").GetComponentsInChildren<MeshFilter>();
                if(halves.Length!=2||halves.Any(f=>!f.GetComponent<Renderer>().enabled))throw new Exception("Official mark halves missing");
                double expected=Area(source,door),actual=halves.Sum(f=>Area(f,door));
                if(Math.Abs(expected-actual)>expected*.0001)throw new Exception("Splitting the mark lost geometry");
                var starts=halves.Select(f=>f.transform.position).ToArray();control.SetProgressInstant(1);
                for(int i=0;i<halves.Length;i++)
                {
                    float direction=door.InverseTransformPoint(starts[i]).x; // both source origins are at centre; use actual mesh side.
                    float side=halves[i].sharedMesh.vertices.Average(p=>p.x)<0?-1:1;
                    if(Vector3.Distance(halves[i].transform.position,starts[i]+door.right*(side*.66f))>.0001f)throw new Exception("Mark half does not follow sliding door");
                }
                control.SetProgressInstant(0);
                if(photo.Find("UniversityDoorSeal").GetComponentsInChildren<Collider>(true).Length!=0||halves.Any(f=>f.GetComponent<Renderer>().sharedMaterial.IsKeywordEnabled("_EMISSION")))throw new Exception("Logo changed collision/glow");
            }
            Debug.Log("ELEVATOR_MARK_OK: both official 57cm Yonsei symbols; source contour preserved by two moving halves, 66cm sliding offsets correct, no legacy lettering or added colliders/glow.");
        }
        [MenuItem("Tools/EEG Wheelchair/Correct Elevator Yonsei Marks")]
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                foreach(var door in root.GetComponentsInChildren<InteractiveDoor>())door.ResetDoor();string collision=CollisionState(root);
                ConvergenceHallElevatorDetails.MatchUniversitySeals(root);ConvergenceHallDoorInteraction.Build(root);Validate(root);
                if(collision!=CollisionState(root))throw new Exception("Elevator mark update changed collision");
                PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.FocusElevatorMark",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            var root=GameObject.Find("ConvergenceHallB1");
            foreach(bool east in new[]{true,false})
            {
                var door=ConvergenceHallElevatorCabins.Door(root,east);var control=door.GetComponent<InteractiveDoor>();control.enabled=false;
                var seal=door.Find("PhotoElevatorDetails/UniversityDoorSeal");var centre=seal.position;
                for(int shot=0;shot<3;shot++)
                {
                    control.SetProgressInstant(shot==2?1:0);cam.fieldOfView=shot==1?40:60;
                    cam.transform.position=centre-door.forward*(shot==1?.80f:1.5f)+Vector3.up*(shot==1?0:.25f);cam.transform.LookAt(centre+Vector3.up*(shot==1?0:.12f));
                    double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string path=Path.Combine(folder,(east?"East":"West")+"ElevatorYonsei"+(shot==0?"Closed":shot==1?"Detail":"Open")+".png");var stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException(path);yield return null;}
                }
                control.SetProgressInstant(0);control.enabled=true;
            }
        }
    }
}
