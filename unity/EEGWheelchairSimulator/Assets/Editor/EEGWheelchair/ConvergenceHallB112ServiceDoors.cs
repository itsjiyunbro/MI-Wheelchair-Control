using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallB112ServiceDoors
    {
        // Photo estimates; preserve the original hall and all existing driving collision.
        const float Near=1.40f,Far=2.95f;
        static Material white,outline,metal,navy;
        static Transform Service(GameObject root)=>root.transform.Find("CoreHalls/B112ElevatorHall/ServicePanels");
        static void Door(Transform parent,string name,float along,float width,float height,float bottom,string code)
        {
            var g=P.Group(parent,name);g.localPosition=new Vector3(along,0,0);
            float centre=bottom+height/2,frameWidth=width+.06f,frameHeight=height+.06f;
            P.Box(g,"InsetOutline",new Vector3(0,centre,-.004f),new Vector3(frameWidth+.006f,frameHeight+.006f,.007f),outline);
            P.Box(g,"WhiteLeaf",new Vector3(0,centre,-.013f),new Vector3(width,height,.006f),white);
            foreach(float x in new[]{-frameWidth/2+.014f,frameWidth/2-.014f})P.Box(g,"WhiteFrameStile",new Vector3(x,centre,-.012f),new Vector3(.028f,frameHeight,.014f),white);
            foreach(float y in new[]{bottom-.016f,bottom+height+.016f})P.Box(g,"WhiteFrameRail",new Vector3(0,y,-.012f),new Vector3(frameWidth,.028f,.014f),white);
            foreach(float y in new[]{.79f,.92f})P.Cylinder(g,"RoundLock",new Vector3(-width/2+.13f,y,-.034f),new Vector3(.036f,.011f,.036f),metal,Quaternion.Euler(90,0,0));
            foreach(float y in new[]{bottom+.24f,centre,bottom+height-.24f})P.Cylinder(g,"RightHinge",new Vector3(width/2+.022f,y,-.028f),new Vector3(.022f,.052f,.022f),metal,Quaternion.identity);
            P.Box(g,"SmallNavyPlacard",new Vector3(0,1.47f,-.019f),new Vector3(.115f,.17f,.004f),navy);
            var label=new GameObject("ServiceLabel",typeof(RectTransform),typeof(Canvas),typeof(Text));label.transform.SetParent(g,false);
            label.transform.localPosition=new Vector3(0,1.47f,-.023f);label.transform.localScale=Vector3.one*.001f;
            ((RectTransform)label.transform).sizeDelta=new Vector2(100,150);label.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var text=label.GetComponent<Text>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");text.fontSize=21;text.text=code;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        }
        public static void ApplyTo(GameObject root)
        {
            var service=Service(root);foreach(Transform t in service.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            service.localPosition=new Vector3(-1.145f,0,0);service.localRotation=Quaternion.Euler(0,-90,0);service.localScale=Vector3.one;
            white=P.Mat("B112ServiceDoorWhite",new Color(.94f,.945f,.93f),.10f);outline=P.Mat("B112ServiceDoorOutline",new Color(.48f,.49f,.48f),.10f);
            metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/Metal.mat");navy=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/Navy.mat");
            Door(service,"LargeEntranceSideDoor",Near,.72f,2.10f,.26f,"PS");
            Door(service,"SmallElevatorSideDoor",Far,.72f,2.10f,.26f,"EPS");
        }
        static string CollisionState(GameObject root)=>(string)typeof(ConvergenceHallWallSurfaceCleanup).GetMethod("ColliderState",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{root});
        static string OtherGeometry(GameObject root)
        {
            var service=Service(root);
            return string.Join("\n",root.GetComponentsInChildren<Transform>(true).Where(t=>!t.IsChildOf(service)).Select(t=>AnimationUtility.CalculateTransformPath(t,root.transform)+"|"+t.localPosition.ToString("F5")+"|"+t.localRotation.ToString("F5")+"|"+t.localScale.ToString("F5")+"|"+(t.GetComponent<MeshFilter>()?AssetDatabase.GetAssetPath(t.GetComponent<MeshFilter>().sharedMesh):"")).OrderBy(s=>s));
        }
        static void Validate(GameObject root)
        {
            var service=Service(root);var hall=service.parent;
            var near=service.Find("LargeEntranceSideDoor/WhiteLeaf").GetComponent<Renderer>();var far=service.Find("SmallElevatorSideDoor/WhiteLeaf").GetComponent<Renderer>();
            if(service.childCount!=2||Vector3.Distance(near.transform.localScale,far.transform.localScale)>.0001f||Mathf.Abs(near.bounds.min.y-far.bounds.min.y)>.0001f)throw new Exception("Service doors must have equal dimensions and elevation");
            foreach(var leaf in new[]{near,far})
            {
                var p=hall.InverseTransformPoint(leaf.bounds.center);
                if(p.z<.3f||p.z>3.4f||p.x< -1.15f||p.x> -1.10f||leaf.bounds.min.y<.20f||leaf.bounds.max.y>2.40f)throw new Exception("Service panel leaves its photo wall area");
            }
            if(Mathf.Abs(service.Find("LargeEntranceSideDoor").localPosition.x-Near)>.001f||Mathf.Abs(service.Find("SmallElevatorSideDoor").localPosition.x-Far)>.001f)throw new Exception("Service doors are reversed");
            ConvergenceHallWallSurfaceCleanup.Validate(root);
            Debug.Log("B112_SERVICE_DOORS_OK: two equal 0.72x2.10m leaves, both 0.26m above floor; centres 1.40/2.95m from entrance; PS/EPS labels and 0.83m clear gap.");
        }
        [MenuItem("Tools/EEG Wheelchair/Match B112 Service Doors To Photo")]
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            if(root.transform.Find("DoorDynamics")){PrefabUtility.UnloadPrefabContents(root);ConvergenceHallPhotoDoorFinishes.ApplyAndCapture();return;}
            try
            {
                string collision=CollisionState(root),other=OtherGeometry(root);SessionState.SetString("EEG.WallSurfaceCollision",collision);
                ApplyTo(root);
                if(CollisionState(root)!=collision||OtherGeometry(root)!=other)throw new Exception("Service door change moved other geometry or collision");
                Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
            SessionState.SetBool("EEG.ConvergenceQA.FocusB112LiftWalls",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
