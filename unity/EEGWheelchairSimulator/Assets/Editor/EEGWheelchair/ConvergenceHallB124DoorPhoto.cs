using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallB124DoorPhoto
    {
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        static Material white,metal,navy,clear,frost;
        static void Box(Transform p,string n,Vector3 at,Vector3 size,Material m)
        {
            var t=p.Find(n);if(!t)t=P.Box(p,n,at,size,m);
            t.localPosition=at;t.localScale=size;t.localRotation=Quaternion.identity;t.GetComponent<Renderer>().sharedMaterial=m;t.gameObject.SetActive(true);
        }
        static void Text(Transform p,string n,string value,Vector3 at,Vector2 metres,Color color,int size=30)
        {
            var t=p.Find(n);if(!t){t=new GameObject(n,typeof(RectTransform),typeof(Canvas),typeof(Text)).transform;t.SetParent(p,false);t.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;}
            t.gameObject.SetActive(true);t.localPosition=at;t.localRotation=Quaternion.identity;t.localScale=Vector3.one*.001f;((RectTransform)t).sizeDelta=metres/.001f;
            var tx=t.GetComponent<Text>();tx.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tx.text=value;tx.fontSize=size;tx.color=color;tx.alignment=TextAnchor.MiddleCenter;tx.raycastTarget=false;
        }
        public static void ApplyTo(GameObject root)
        {
            white=P.Mat("B124WhiteDoorPaint",new Color(.92f,.92f,.89f),.27f);metal=AssetDatabase.LoadAssetAtPath<Material>(Mats+"Metal.mat");navy=AssetDatabase.LoadAssetAtPath<Material>(Mats+"Navy.mat");clear=AssetDatabase.LoadAssetAtPath<Material>(Mats+"ClearCorridorGlass.mat");frost=AssetDatabase.LoadAssetAtPath<Material>(Mats+"FrostedCorridorGlass.mat");
            if(!frost)frost=ConvergenceHallGlassBands.Glass(true);
            var door=root.transform.Find("PlanDoors/B124Wing_West_Double");
            foreach(string n in new[]{"FrameLeft","FrameRight"})Box(door,n,new Vector3(n=="FrameLeft"?-1.05f:1.05f,1.425f,0),new Vector3(.05f,2.85f,.105f),white);
            Box(door,"FrameHeader",new Vector3(0,2.225f,0),new Vector3(2.15f,.05f,.105f),white);
            Box(door,"PhotoOpaqueTransom",new Vector3(0,2.535f,0),new Vector3(2.05f,.57f,.045f),white);
            Box(door,"PhotoTopFrame",new Vector3(0,2.835f,0),new Vector3(2.15f,.03f,.105f),white);
            // Use the same native plaque as all other classrooms, on the photographed right jamb.
            float signX=1.11f;
            ApplySignTo(root);
            Box(door,"PhotoAccessReader",new Vector3(signX,1.19f,-.092f),new Vector3(.065f,.13f,.04f),navy);
            Box(door,"PhotoReaderIndicator",new Vector3(signX,1.15f,-.115f),new Vector3(.022f,.004f,.005f),AssetDatabase.LoadAssetAtPath<Material>(Mats+"VendingGreen.mat"));
            foreach(Transform pivot in door.Cast<Transform>().Where(t=>t.name.Contains("_InwardY")))
            {
                pivot.localRotation=Quaternion.identity;var leaf=pivot.Find("Leaf");leaf.GetComponent<Renderer>().sharedMaterial=white;
                foreach(string n in new[]{"HandleBase","Closer"})pivot.Find(n).gameObject.SetActive(false);
                float x=pivot.Find("HandleBase").localPosition.x;
                var old=pivot.Find("PhotoHandleDetails");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var detail=P.Group(pivot,"PhotoHandleDetails");
                foreach(int face in new[]{-1,1})
                {
                    P.Cylinder(detail,"RoundRose"+face,new Vector3(x,.97f,face*.034f),new Vector3(.063f,.008f,.063f),metal,Quaternion.Euler(90,0,0));
                    P.Tube(detail,"HandleStem"+face,new Vector3(x,.98f,face*.036f),new Vector3(x,.98f,face*.069f),.024f,metal);
                    float direction=pivot.localPosition.x<0?-1:1;
                    P.Tube(detail,"Lever"+face,new Vector3(x,.98f,face*.069f),new Vector3(x+direction*.14f,.98f,face*.069f),.021f,metal);
                }
                pivot.Find("Lever").gameObject.SetActive(false);
                if(pivot.localPosition.x>0)
                {
                    P.Box(detail,"BottomFlushBolt",new Vector3(leaf.localPosition.x-leaf.localScale.x/2+.065f,.11f,-.030f),new Vector3(.023f,.15f,.016f),metal);
                    P.Cylinder(detail,"FlushBoltGrip",new Vector3(leaf.localPosition.x-leaf.localScale.x/2+.065f,.155f,-.040f),new Vector3(.030f,.006f,.030f),metal,Quaternion.Euler(90,0,0));
                }
            }
            // Replace only the entrance end wall visuals; original fixed collision still bounds the sidelights.
            var edge=root.transform.Find("RoomsAndCores/B124Wing/West");
            foreach(Transform t in edge)if(t.name!="DoorBoundarySegments")t.gameObject.SetActive(false);
            var facade=door.Find("PhotoFixedSidelights");if(facade)UnityEngine.Object.DestroyImmediate(facade.gameObject);facade=P.Group(door,"PhotoFixedSidelights");
            float width=root.transform.Find("RoomsAndCores/B124Wing/RoomFloor").GetComponent<Renderer>().bounds.size.z;
            float inner=1.075f,outer=width/2,glassWidth=outer-inner-.11f;
            foreach(int side in new[]{-1,1})
            {
                float centre=side*(inner+.055f+glassWidth/2);
                P.Box(facade,"SidelightFrost"+side,new Vector3(centre,1.19f,0),new Vector3(glassWidth,2.22f,.018f),frost);
                P.Box(facade,"SidelightClearTop"+side,new Vector3(centre,2.555f,0),new Vector3(glassWidth,.51f,.018f),clear);
                P.Box(facade,"InnerWhiteJamb"+side,new Vector3(side*(inner+.0275f),1.425f,0),new Vector3(.055f,2.85f,.105f),white);
                P.Box(facade,"OuterWhiteJamb"+side,new Vector3(side*(outer-.0275f),1.425f,0),new Vector3(.055f,2.85f,.105f),white);
                P.Box(facade,"GlassTopRail"+side,new Vector3(centre,2.83f,0),new Vector3(glassWidth,.04f,.105f),white);
                P.Box(facade,"GlassBottomRail"+side,new Vector3(centre,.045f,0),new Vector3(glassWidth,.09f,.105f),white);
            }
            // Photo 1 shows the existing neighbouring single door, B124-1.
            var single=root.transform.Find("PlanDoors/B124_North_West");single.Find("RoomLabel").GetComponent<Text>().text="B124-1";
            var singleSign=single.Find("PhotoRoomSign/RoomNumber");if(singleSign)singleSign.GetComponent<Text>().text="B124-1";
        }
        public static void ApplySignTo(GameObject root)
        {
            var door=root.transform.Find("PlanDoors/B124Wing_West_Double");
            foreach(string name in new[]{"RoomPlate","RoomPlateHeader","RoomLabel","PhotoPlateHeader","PhotoRoomTitle"})
            {var old=door.Find(name);if(old)old.gameObject.SetActive(false);}
            var sign=door.Find("PhotoRoomSign");if(!sign)throw new Exception("Existing standard B124 plaque missing");
            sign.gameObject.SetActive(true);sign.localPosition=new Vector3(1.11f,1.49f,-.0565f);sign.localRotation=Quaternion.identity;sign.localScale=Vector3.one*.65f;
            sign.Find("RoomNumber").GetComponent<Text>().text="B124";
            sign.Find("KoreanName").GetComponent<Text>().text="상담실라운지";
            sign.Find("EnglishName").GetComponent<Text>().text="Consulting Lounge";
        }
        static string Collisions(GameObject root)=>string.Join("\n",root.GetComponentsInChildren<Collider>(true).Select(c=>AnimationUtility.CalculateTransformPath(c.transform,root.transform)+"|"+c.enabled+"|"+c.bounds.ToString("F5")).OrderBy(s=>s));
        static void ValidateSign(GameObject root)
        {
            var sign=root.transform.Find("PlanDoors/B124Wing_West_Double/PhotoRoomSign");
            var standard=root.transform.Find("PlanDoors/B102_North_West/PhotoRoomSign");
            if(!sign.gameObject.activeInHierarchy||sign.localScale!=standard.localScale)throw new Exception("B124 plaque scale differs");
            foreach(string name in new[]{"NavyBody","WhiteHeader","Divider","BraillePlate"})
            {
                var a=sign.Find(name);var b=standard.Find(name);
                if(a.localScale!=b.localScale||a.GetComponent<Renderer>().sharedMaterial!=b.GetComponent<Renderer>().sharedMaterial)throw new Exception("B124 plaque geometry/material differs: "+name);
            }
            foreach(string name in new[]{"RoomNumber","KoreanName","EnglishName"})
            {
                var a=sign.Find(name).GetComponent<Text>();var b=standard.Find(name).GetComponent<Text>();
                if(a.font!=b.font||a.fontSize!=b.fontSize||a.alignment!=b.alignment)throw new Exception("B124 plaque typography differs: "+name);
            }
            Debug.Log("B124_STANDARD_SIGN_OK: same 13cm plaque, Korean font, white number header/navy body, Korean-English names and braille as other classroom signs; right jamb position.");
        }
        [MenuItem("Tools/EEG Wheelchair/Match B124 Standard Room Sign")]
        public static void ApplySignAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                string before=Collisions(root);ApplySignTo(root);ValidateSign(root);
                if(before!=Collisions(root))throw new Exception("Sign update changed collision");
                PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);ValidateSign(GameObject.Find("ConvergenceHallB1"));
            SessionState.SetBool("EEG.ConvergenceQA.FocusB124Sign",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureSignViews(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;cam.fieldOfView=60;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            var door=GameObject.Find("ConvergenceHallB1").transform.Find("PlanDoors/B124Wing_West_Double");
            for(int i=0;i<2;i++)
            {
                var target=door.TransformPoint(new Vector3(i==0?1.11f:0,i==0?1.49f:1.35f,0));
                cam.transform.position=target-door.forward*(i==0?.30f:2.25f);cam.transform.LookAt(target);
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,i==0?"B124StandardSignDetail.png":"B124StandardSignEntrance.png");var stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException(path);yield return null;}
            }
        }
        static void Validate(GameObject root)
        {
            var door=root.transform.Find("PlanDoors/B124Wing_West_Double");
            var leaves=door.Cast<Transform>().Where(t=>t.name.Contains("_InwardY")).Select(t=>t.Find("Leaf")).ToArray();
            if(leaves.Length!=2||leaves.Any(t=>t.GetComponent<Renderer>().sharedMaterial!=white)||!door.Find("PhotoOpaqueTransom"))throw new Exception("B124 photo door incomplete");
            if(root.GetComponentsInChildren<InteractiveDoor>().Length!=46)throw new Exception("Door control count changed");
            Debug.Log("B124_PHOTO_DOOR_OK: white double swing door, transom, two narrow glazed sidelights, permanent jamb sign/reader; no paper notices or plants.");
        }
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                ConvergenceHallWallSurfaceCleanup.Restore(root);foreach(var d in root.GetComponentsInChildren<InteractiveDoor>())d.ResetDoor();
                ApplyTo(root);ConvergenceHallWallSurfaceCleanup.ApplyTo(root);ConvergenceHallDoorInteraction.Build(root);Validate(root);ConvergenceHallWallSurfaceCleanup.Validate(root);
                PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.FocusB124Photo",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;cam.fieldOfView=65;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            var root=GameObject.Find("ConvergenceHallB1").transform;var door=root.Find("PlanDoors/B124Wing_West_Double");
            for(int i=0;i<3;i++)
            {
                var d=i==2?root.Find("PlanDoors/B124_North_West"):door;
                if(i==1){d.GetComponent<InteractiveDoor>().Toggle();double end=EditorApplication.timeSinceStartup+1.2;while(EditorApplication.timeSinceStartup<end)yield return null;}
                cam.transform.position=d.TransformPoint(new Vector3(i==2?-.60f:0,1.35f,i==2?-2.1f:-2.25f));cam.transform.LookAt(d.TransformPoint(new Vector3(0,1.35f,0)));
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,new[]{"B124WhiteDoubleDoor","B124DoorOpen","B124AdjacentSingleDoor"}[i]+".png");var stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException(path);yield return null;}
            }
        }
    }
}
