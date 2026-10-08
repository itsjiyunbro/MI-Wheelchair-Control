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
    public static class ConvergenceHallWestLiftPhoto
    {
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        static Material white,metal,navy,gold;
        static Material Load(string n)=>AssetDatabase.LoadAssetAtPath<Material>(Mats+n+".mat");
        static bool WallFinish(Material m)=>m&&(m.name=="WarmWhite"||m.name=="CoreConcrete"||m.name.StartsWith("B112ElevatorRoom")||m.name=="B112ExposedConcrete"||m.name=="WestLiftPhotoWhite");
        static void Text(Transform parent,string name,string value,Vector3 at,Vector2 size,int pixels,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(Text));go.transform.SetParent(parent,false);
            go.transform.localPosition=at;go.transform.localScale=Vector3.one*.0005f;
            ((RectTransform)go.transform).sizeDelta=size/.0005f;go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var label=go.GetComponent<Text>();label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");
            label.text=value;label.fontSize=pixels;label.resizeTextForBestFit=true;label.resizeTextMinSize=8;label.resizeTextMaxSize=pixels;
            label.alignment=TextAnchor.MiddleLeft;label.color=color;label.raycastTarget=false;
        }
        static void Panel(Transform parent,string name,Vector3 at,float yaw,string code,string english)
        {
            const float width=.78f,height=2.10f,bottom=.22f;
            var p=P.Group(parent,name);p.localPosition=at;p.localRotation=Quaternion.Euler(0,yaw,0);
            float middle=bottom+height/2;
            foreach(int side in new[]{-1,1})P.Box(p,"FrameStile"+side,new Vector3(side*(width/2+.018f),middle,-.008f),new Vector3(.036f,height+.07f,.040f),white);
            foreach(int side in new[]{-1,1})P.Box(p,"FrameRail"+side,new Vector3(0,middle+side*(height/2+.018f),-.008f),new Vector3(width+.072f,.036f,.040f),white);
            var moving=P.Group(p,"DoorLeaf");
            P.Box(moving,"WhiteLeaf",new Vector3(0,middle,-.012f),new Vector3(width,height,.026f),white);
            foreach(float y in new[]{.89f,1.02f})
            {
                P.Cylinder(moving,"RoundLock"+y,new Vector3(width/2-.12f,y,-.039f),new Vector3(.040f,.014f,.040f),metal,Quaternion.Euler(90,0,0));
                P.Box(moving,"LockSlot"+y,new Vector3(width/2-.12f,y,-.048f),new Vector3(.015f,.003f,.003f),Load("FurnitureBlack"));
            }
            foreach(float y in new[]{bottom+.24f,middle+.30f,bottom+height-.24f})P.Cylinder(moving,"LeftHinge"+y,new Vector3(-width/2-.020f,y,-.026f),new Vector3(.022f,.080f,.022f),metal,Quaternion.identity);
            var sign=P.Group(moving,"ServicePlacard");sign.localPosition=new Vector3(0,1.54f,-.029f);
            P.Box(sign,"NavyBacking",Vector3.zero,new Vector3(.145f,.170f,.004f),navy);
            Text(sign,"Warning","관계자 외 출입금지",new Vector3(0,.052f,-.003f),new Vector2(.127f,.028f),30,new Color(.94f,.78f,.06f));
            Text(sign,"Code",code,new Vector3(0,-.033f,-.003f),new Vector2(.127f,.036f),57,Color.white);
            P.Box(sign,"Divider",new Vector3(.004f,-.055f,-.003f),new Vector3(.136f,.0008f,.001f),gold);
            Text(sign,"English",english,new Vector3(0,-.068f,-.003f),new Vector2(.127f,.022f),22,Color.white);
        }
        public static void ApplyTo(GameObject root)
        {
            var hall=root.transform.Find("CoreHalls/B109ElevatorHall");if(!hall)return;
            var lift=hall.Find("LeftWallElevator");var surround=lift.Find("RecessedWallSurround");if(!surround)return;
            white=P.Mat("WestLiftPhotoWhite",new Color(.91f,.915f,.89f),.12f);metal=Load("Metal");navy=Load("CoreSignNavy");gold=Load("CoreSignDivider");
            var core=root.transform.Find("RoomsAndCores/WestLiftCore");
            foreach(var r in core.GetComponentsInChildren<Renderer>(true).Concat(hall.Cast<Transform>().Where(t=>t.GetComponent<Renderer>()&&t.name!="HallFloor").Select(t=>t.GetComponent<Renderer>())).Concat(surround.GetComponentsInChildren<Renderer>(true)))
            {
                var materials=r.sharedMaterials;
                for(int i=0;i<materials.Length;i++)if(WallFinish(materials[i]))materials[i]=white;
                r.sharedMaterials=materials;
            }
            core.Find("RoomFloor").GetComponent<Renderer>().sharedMaterial=Load("MeasuredGrayFloorTile");hall.Find("HallFloor").GetComponent<Renderer>().sharedMaterial=Load("MeasuredGrayFloorTile");
            var service=hall.Find("ServicePanels");foreach(Transform t in service.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            service.localPosition=Vector3.zero;service.localRotation=Quaternion.identity;service.localScale=Vector3.one;
            float depth=hall.Find("HallFloor").localScale.z;
            // EPS shares the recessed elevator wall; TPS is on the perpendicular rear wall.
            float sideX=hall.InverseTransformPoint(surround.TransformPoint(new Vector3(0,0,-.16f))).x;
            Panel(service,"EPSPanel",new Vector3(sideX+.012f,0,depth-.96f),-90,"EPS","Electrical Pipe Shaft");
            float left=sideX,right=hall.Find("HallFloor").localPosition.x+hall.Find("HallFloor").localScale.x/2;
            Panel(service,"TPSPanel",new Vector3(Mathf.Lerp(left,right,.62f),0,depth-.012f),0,"TPS","Telecommunication Pipe Shaft");
            var old=hall.Find("PhotoWhiteSkirting");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var skirting=P.Group(hall,"PhotoWhiteSkirting");var trim=P.Mat("WestLiftSkirting",new Color(.24f,.25f,.24f),.10f);
            P.Box(skirting,"BackSkirting",new Vector3((left+right)/2,.046f,depth-.010f),new Vector3(right-left,.085f,.009f),trim);
            P.Box(skirting,"SideSkirting",new Vector3(right-.012f,.046f,depth/2),new Vector3(.009f,.085f,depth-.20f),trim);
            foreach(var r in surround.GetComponentsInChildren<Renderer>())if(r.name.StartsWith("Skirting"))r.sharedMaterial=trim;
            ConvergenceHallLecternLighting.DisableEmission(root);
            Debug.Log("WEST_LIFT_PHOTO_APPLIED: white perimeter, dark tile floor, EPS on lift wall / TPS on perpendicular rear wall; native yellow warning and bilingual navy signs.");
        }
        static void Validate(GameObject root)
        {
            var hall=root.transform.Find("CoreHalls/B109ElevatorHall");var service=hall.Find("ServicePanels");
            if(service.childCount!=2)throw new Exception("West service panels duplicated");
            foreach(var entry in new[]{new[]{"EPSPanel","EPS","Electrical Pipe Shaft"},new[]{"TPSPanel","TPS","Telecommunication Pipe Shaft"}})
            {
                var panel=service.Find(entry[0]);var sign=panel.Find("DoorLeaf/ServicePlacard");
                if(sign.Find("Warning").GetComponent<Text>().text!="관계자 외 출입금지"||sign.Find("Code").GetComponent<Text>().text!=entry[1]||sign.Find("English").GetComponent<Text>().text!=entry[2])throw new Exception("West placard text wrong");
                if(!panel.GetComponent<InteractiveDoor>())throw new Exception("West inspection door not interactive");
            }
            if(Mathf.Abs(Vector3.Dot(service.Find("EPSPanel").forward,service.Find("TPSPanel").forward))>.001f)throw new Exception("Inspection panels must be perpendicular");
            if(root.transform.Find("RoomsAndCores/WestLiftCore").GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>WallFinish(m)&&m.name!="WestLiftPhotoWhite")))throw new Exception("West perimeter still concrete");
            if(root.transform.Find("RoomsAndCores/EastLift").GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>WallFinish(m)&&m.name!="B112ExposedConcrete")))throw new Exception("East lift concrete changed");
            if(hall.Find("HallFloor").GetComponent<Renderer>().sharedMaterial.name!="MeasuredGrayFloorTile")throw new Exception("West hall tile missing");
            ConvergenceHallFinalFinishes.Validate(root);ConvergenceHallWallSurfaceCleanup.Validate(root);
            Debug.Log("WEST_LIFT_PHOTO_OK: native EPS/TPS signs, perpendicular equal white inspection doors, white west walls/dark floor, east concrete retained; cabin/route/wall checks passed.");
        }
        [MenuItem("Tools/EEG Wheelchair/Match West Elevator Hall To Photos")]
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                foreach(var door in root.GetComponentsInChildren<InteractiveDoor>())door.ResetDoor();
                ConvergenceHallWallSurfaceCleanup.Restore(root);ApplyTo(root);ApplyTo(root);
                ConvergenceHallWallSurfaceCleanup.ApplyTo(root);ConvergenceHallDoorInteraction.Build(root);Validate(root);
                PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.FocusWestLiftPhoto",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;cam.fieldOfView=76;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            var hall=GameObject.Find("ConvergenceHallB1").transform.Find("CoreHalls/B109ElevatorHall");var service=hall.Find("ServicePanels");
            float x=service.Find("EPSPanel").localPosition.x,depth=hall.Find("HallFloor").localScale.z;
            var targets=new[]{hall.TransformPoint(new Vector3(x+.5f,1.45f,depth-1.20f)),service.Find("EPSPanel/DoorLeaf/ServicePlacard").position,service.Find("TPSPanel/DoorLeaf/ServicePlacard").position};
            var points=new[]{hall.TransformPoint(new Vector3(x+1.95f,1.55f,depth-3.35f)),targets[1]-service.Find("EPSPanel").forward*.42f,targets[2]-service.Find("TPSPanel").forward*.42f};
            var names=new[]{"WestLiftPhotoOverview","WestLiftEPSPlacard","WestLiftTPSPlacard"};
            for(int i=0;i<names.Length;i++)
            {
                cam.fieldOfView=i==0?76:42;cam.transform.position=points[i];cam.transform.LookAt(targets[i]);
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,names[i]+".png");var stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException(path);yield return null;}
            }
        }
    }
}
