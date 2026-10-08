using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    // Furniture measurements are preserved; photo-only fittings use proportional estimates.
    public static class ConvergenceHallB112Presentation
    {
        const float H=2.85f;
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        static Material white,metal,dark,board,cloth,light;
        static Material Load(string n)=>AssetDatabase.LoadAssetAtPath<Material>(Mats+n+".mat");
        static void Remove(Transform p,string n){var t=p.Find(n);if(t)UnityEngine.Object.DestroyImmediate(t.gameObject);}
        static void Whiteboard(Transform p,string name,Vector3 at,float width,float yaw)
        {
            var g=P.Group(p,name);g.localPosition=at;g.localRotation=Quaternion.Euler(0,yaw,0);
            P.Box(g,"SilverRim",Vector3.zero,new Vector3(width,1.18f,.028f),metal);
            P.Box(g,"WritingSurface",new Vector3(0,0,-.020f),new Vector3(width-.028f,1.15f,.012f),board);
            P.Box(g,"MarkerTray",new Vector3(0,-.597f,-.045f),new Vector3(width,.017f,.075f),metal);
            P.Box(g,"Eraser",new Vector3(-width*.38f,-.573f,-.047f),new Vector3(.12f,.035f,.040f),white);
            for(int i=0;i<2;i++)P.Tube(g,"Marker"+i,new Vector3(-width*.30f+i*.13f,-.578f,-.05f),new Vector3(-width*.30f+i*.13f+.095f,-.578f,-.05f),.009f,i==0?dark:Load("BoardMarkerBlue"));
        }
        static void Outlet(Transform p,string name,Vector3 at,float yaw)
        {
            var g=P.Group(p,name);g.localPosition=at;g.localRotation=Quaternion.Euler(0,yaw,0);
            P.Box(g,"Plate",Vector3.zero,new Vector3(.095f,.12f,.012f),white);
            foreach(float y in new[]{-.025f,.025f})
            {
                P.Cylinder(g,"Socket"+y,new Vector3(0,y,-.010f),new Vector3(.035f,.002f,.035f),metal,Quaternion.Euler(90,0,0));
                foreach(float x in new[]{-.007f,.007f})P.Box(g,"Hole"+y+"_"+x,new Vector3(x,y,-.014f),new Vector3(.005f,.008f,.003f),dark);
            }
        }
        static void AC(Transform p,float z,int index)
        {
            var g=P.Group(p,"CeilingCassette"+index);g.localPosition=new Vector3(0,H-.045f,z);
            P.Box(g,"Frame",Vector3.zero,new Vector3(.86f,.04f,.86f),white);
            P.Box(g,"Intake",new Vector3(0,-.025f,0),new Vector3(.52f,.008f,.52f),metal);
            for(int i=0;i<12;i++)P.Box(g,"IntakeSlot"+i,new Vector3(-.23f+i*.042f,-.031f,0),new Vector3(.010f,.004f,.48f),dark);
            for(int side=0;side<4;side++)
            {
                var vent=P.Group(g,"Outlet"+side);vent.localRotation=Quaternion.Euler(0,side*90,0);
                P.Box(vent,"Recess",new Vector3(0,-.026f,.35f),new Vector3(.67f,.012f,.07f),dark);
                P.Box(vent,"Louver",new Vector3(0,-.038f,.35f),new Vector3(.65f,.008f,.035f),white);
            }
        }
        static void Windows(Transform p,GameObject root,float w,float d)
        {
            var edge=root.transform.Find("RoomsAndCores/B112/West");
            foreach(Transform t in edge)if(t.name!="DoorBoundarySegments")t.gameObject.SetActive(false);
            float x=-w/2,windowW=1.18f;
            var windows=new[]{d*.19f,d*.36f};float cursor=-d/2;
            for(int i=0;i<2;i++)
            {
                float z=windows[i],lo=z-windowW/2,hi=z+windowW/2;
                P.Box(p,"WestPier"+i,new Vector3(x,H/2,(cursor+lo)/2),new Vector3(.12f,H,lo-cursor),white);
                P.Box(p,"WindowSill"+i,new Vector3(x,.085f,z),new Vector3(.16f,.17f,windowW),white);
                P.Box(p,"WindowHead"+i,new Vector3(x,2.815f,z),new Vector3(.15f,.07f,windowW),white);
                P.Box(p,"ClearWindow"+i,new Vector3(x,1.48f,z),new Vector3(.018f,2.61f,windowW-.06f),Load("ClearCorridorGlass"));
                foreach(int side in new[]{-1,1})P.Box(p,"WindowJamb"+i+"_"+side,new Vector3(x+.025f,1.48f,z+side*(windowW/2-.025f)),new Vector3(.08f,2.65f,.05f),metal);
                P.Box(p,"WindowCrossbar"+i,new Vector3(x+.035f,.44f,z),new Vector3(.08f,.035f,windowW),metal);
                var blind=P.Group(p,"RollerBlind"+i);blind.localPosition=new Vector3(x+.095f,0,z);
                P.Box(blind,"BlindCloth",new Vector3(0,1.59f,0),new Vector3(.016f,2.36f,windowW-.11f),cloth);
                P.Box(blind,"RollerCase",new Vector3(0,2.78f,0),new Vector3(.075f,.055f,windowW),white);
                P.Box(blind,"BottomBar",new Vector3(0,.405f,0),new Vector3(.031f,.025f,windowW-.10f),metal);
                P.Tube(blind,"PullChainA",new Vector3(.03f,2.77f,windowW/2-.015f),new Vector3(.03f,1.05f,windowW/2-.015f),.005f,white);
                P.Tube(blind,"PullChainB",new Vector3(.055f,2.77f,windowW/2-.015f),new Vector3(.055f,1.05f,windowW/2-.015f),.005f,white);
                if(i==0)
                {
                    var crest=P.Group(blind,"YonseiBlindCrest");crest.localPosition=new Vector3(.014f,1.72f,0);crest.localRotation=Quaternion.Euler(0,-90,0);
                    var mark=P.Group(crest,"OfficialSymbol");mark.gameObject.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Meshes/ConvergenceHall/YonseiOfficialSymbol.asset");mark.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Load("YonseiSymbolBlue");mark.localScale=Vector3.one*1.6f;
                }
                cursor=hi;
            }
            P.Box(p,"WestEndPier",new Vector3(x,H/2,(cursor+d/2)/2),new Vector3(.12f,H,d/2-cursor),white);
        }
        static Bounds FurnitureBounds(Transform item,Transform room)
        {
            var bounds=new Bounds();bool first=true;
            foreach(var c in item.GetComponentsInChildren<BoxCollider>())
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                {
                    var v=room.InverseTransformPoint(c.transform.TransformPoint(c.center+Vector3.Scale(c.size*.5f,new Vector3(x,y,z))));
                    if(first){bounds=new Bounds(v,Vector3.zero);first=false;}else bounds.Encapsulate(v);
                }
            return bounds;
        }
        public static void ApplyTo(GameObject root)
        {
            white=P.Mat("B112PhotoWhitePaint",new Color(.92f,.92f,.89f),.18f);metal=Load("InteriorBoardFrame");dark=Load("FurnitureBlack");board=Load("InteriorWhiteboard");light=Load("ClassroomLED");cloth=P.Mat("B112BlindIvory",new Color(.78f,.77f,.70f),.03f);
            var room=root.transform.Find("ClassroomInteriors/B112");var floor=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;float w=floor.size.x,d=floor.size.z;
            Remove(room,"CombinedVisual");Remove(room,"CeilingFittingsVisual");Remove(room,"PhotoPresentationDetails");
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())r.enabled=true;
            foreach(string n in new[]{"FrontWhiteboard","SideWhiteboard","CassetteAirConditioner","TeacherChair"})Remove(room,n);
            foreach(Transform t in room.Cast<Transform>().ToArray())if(t.name.StartsWith("LEDFrame_")||t.name.StartsWith("LEDPanel_")||t.name.StartsWith("CeilingVent")||t.name=="SmokeDetector"||t.name=="FrontSpeaker")UnityEngine.Object.DestroyImmediate(t.gameObject);
            var p=P.Group(room,"PhotoPresentationDetails");
            Whiteboard(p,"FrontPhotoBoard",new Vector3(0,1.65f,d/2-.105f),Mathf.Min(3.55f,w-.8f),0);
            Whiteboard(p,"RearPhotoBoard",new Vector3(.45f,1.65f,-d/2+.10f),Mathf.Min(2.85f,w-1.8f),180);
            var seating=room.Find("Seating");
            float a=Mathf.Min(FurnitureBounds(seating.Find("Chair_2_2_A"),room).min.z,FurnitureBounds(seating.Find("Chair_2_2_B"),room).min.z);
            float b=FurnitureBounds(seating.Find("Desk_3_2"),room).max.z;
            float spacing=Mathf.Max(0,.305f-(a-b));
            foreach(string n in new[]{"Desk_3_2","Chair_3_2_A","Chair_3_2_B"})seating.Find(n).localPosition+=Vector3.back*spacing;
            b-=spacing;float partitionZ=(a+b)/2;
            float frontBoardLength=Mathf.Min(5.0f,d/2-partitionZ-1.6f),backBoardLength=Mathf.Min(5.0f,d/2+partitionZ-1.6f);
            Whiteboard(p,"EastFrontBoard",new Vector3(w/2-.13f,1.64f,partitionZ+.65f+frontBoardLength/2),frontBoardLength,90);
            Whiteboard(p,"EastRearBoard",new Vector3(w/2-.13f,1.64f,partitionZ-.65f-backBoardLength/2),backBoardLength,90);
            var partition=P.Group(p,"FoldedPartition");partition.localPosition=new Vector3(w/2-.54f,0,partitionZ);
            for(int i=0;i<10;i++)
            {
                float z=(i-4.5f)*.025f;
                P.Box(partition,"Panel"+i,new Vector3(0,1.40f,z),new Vector3(.88f,2.77f,.018f),white);
                P.Box(partition,"Seal"+i,new Vector3(-.446f,1.40f,z),new Vector3(.012f,2.77f,.021f),dark);
                P.Box(partition,"UpperHanger"+i,new Vector3(0,2.795f,z),new Vector3(.035f,.06f,.016f),metal);
            }
            var collision=partition.gameObject.AddComponent<BoxCollider>();collision.center=new Vector3(0,1.40f,0);collision.size=new Vector3(.90f,2.80f,.245f);partition.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");
            P.Box(p,"PartitionCeilingTrack",new Vector3(0,2.809f,partitionZ),new Vector3(w-.12f,.045f,.035f),metal);
            ConvergenceHallB112DiagonalScreen.ApplyTo(root,false);
            var projector=room.Find("CeilingProjector");projector.localPosition=new Vector3(0,2.58f,d/2-3.4f);
            room.Find("Lectern").localPosition=new Vector3(-w*.24f,.01f,d/2-1.05f);
            Windows(p,root,w,d);
            for(int row=0;row<7;row++)for(int col=0;col<3;col++)
            {
                float x=(col-1)*w*.33f,z=Mathf.Lerp(-d/2+.8f,d/2-.8f,row/6f);
                if(Mathf.Abs(z-partitionZ)<.72f)z=partitionZ+.77f;
                P.Box(p,"LightHousing"+row+"_"+col,new Vector3(x,2.797f,z),new Vector3(.31f,.024f,1.20f),white);
                P.Box(p,"LightDiffuser"+row+"_"+col,new Vector3(x,2.781f,z),new Vector3(.28f,.008f,1.16f),light);
            }
            AC(p,d/2-2.65f,0);AC(p,-d/2+3.0f,1);
            for(int i=0;i<6;i++)foreach(int side in new[]{-1,1})
            {
                var at=new Vector3(side*w*.17f,2.805f,Mathf.Lerp(-d/2+1.1f,d/2-1.1f,i/5f));
                P.Cylinder(p,"VentRim"+side+"_"+i,at,new Vector3(.22f,.009f,.22f),white,Quaternion.identity);
                P.Cylinder(p,"VentCore"+side+"_"+i,at+Vector3.down*.010f,new Vector3(.16f,.003f,.16f),metal,Quaternion.identity);
            }
            foreach(int end in new[]{-1,1})foreach(int side in new[]{-1,1})
            {
                var speaker=P.Box(p,"CornerSpeaker"+end+"_"+side,new Vector3(side*(w/2-.24f),2.60f,end*(d/2-.27f)),new Vector3(.23f,.23f,.21f),dark);speaker.localRotation=Quaternion.Euler(end*15,side*end*20,0);
            }
            var rack=P.Group(p,"AVRack");rack.localPosition=new Vector3(w/2-.52f,0,d/2-1.65f);
            P.Box(rack,"Case",new Vector3(0,.38f,0),new Vector3(.64f,.72f,.55f),metal);
            P.Box(rack,"DarkFront",new Vector3(0,.40f,-.286f),new Vector3(.55f,.60f,.020f),dark);
            for(int i=0;i<5;i++)
            {
                P.Box(rack,"EquipmentFace"+i,new Vector3(0,.18f+i*.105f,-.302f),new Vector3(.48f,.080f,.012f),metal);
                for(int j=0;j<6;j++)P.Cylinder(rack,"Knob"+i+"_"+j,new Vector3(-.16f+j*.052f,.18f+i*.105f,-.313f),new Vector3(.012f,.004f,.012f),dark,Quaternion.Euler(90,0,0));
            }
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})P.Cylinder(rack,"Caster"+side+"_"+end,new Vector3(side*.25f,.036f,end*.20f),new Vector3(.06f,.020f,.06f),dark,Quaternion.Euler(0,0,90));
            var rc=rack.gameObject.AddComponent<BoxCollider>();rc.center=new Vector3(0,.40f,0);rc.size=new Vector3(.64f,.8f,.59f);rack.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");
            Outlet(p,"FrontOutlet",new Vector3(0,.35f,d/2-.13f),0);
            Outlet(p,"RearOutlet",new Vector3(1.0f,.35f,-d/2+.13f),180);
            for(int row=0;row<4;row++)
            {
                float z=Mathf.Lerp(-d/2+1,d/2-1,row/3f);
                P.Box(p,"FloorSocketFrame"+row,new Vector3(0,.014f,z),new Vector3(.18f,.008f,.18f),metal);
                P.Box(p,"FloorSocketLid"+row,new Vector3(0,.019f,z),new Vector3(.145f,.004f,.145f),Load("CoreConcrete"));
            }
            var service=P.Group(p,"RearServicePanel");service.localPosition=new Vector3(-w/2+.65f,0,-d/2+.09f);service.localRotation=Quaternion.Euler(0,180,0);
            P.Box(service,"WhiteServiceLeaf",new Vector3(0,1.04f,0),new Vector3(.80f,2.08f,.035f),white);
            foreach(int side in new[]{-1,1})P.Box(service,"Jamb"+side,new Vector3(side*.423f,1.075f,0),new Vector3(.046f,2.15f,.065f),white);
            P.Box(service,"Header",new Vector3(0,2.125f,0),new Vector3(.89f,.05f,.065f),white);
            P.Box(service,"Transom",new Vector3(0,2.49f,0),new Vector3(.89f,.66f,.040f),white);
            P.Box(service,"SmallNavyPlate",new Vector3(0,1.60f,-.025f),new Vector3(.08f,.10f,.012f),Load("Navy"));
            P.Tube(service,"Lever",new Vector3(.29f,.97f,-.07f),new Vector3(.17f,.97f,-.07f),.022f,metal);
            ConvergenceHallB112DiagonalScreen.ApplyTo(root,false);
            ConvergenceHallLecternLighting.DisableEmission(root);
            // The existing overhead view uses a downward-only ceiling. Keep small ceiling
            // fittings from casting exterior sunlight silhouettes across the projection screen.
            var fittings=room.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.name!="CeilingUnderside"&&room.InverseTransformPoint(r.bounds.min).y>2.3f).ToArray();
            foreach(var r in fittings)r.enabled=false;
            P.Combine(room,"Interior_B112Combined");
            var interior=room.Find("CombinedVisual").GetComponent<MeshRenderer>();interior.enabled=false;
            foreach(var r in fittings)r.enabled=true;
            P.Combine(room,"Interior_B112CeilingFittings");
            var ceilingVisual=room.Cast<Transform>().Last(t=>t.name=="CombinedVisual");ceilingVisual.name="CeilingFittingsVisual";
            ceilingVisual.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            interior.enabled=true;
            Debug.Log("B112_PRESENTATION_LAYOUT: width="+w+" depth="+d+" partitionZ="+partitionZ+"; 21 desks/42 student chairs; screen lowered; photo boards, folded partition, windows/blinds and fittings.");
        }
        static void Validate(GameObject root)
        {
            var room=root.transform.Find("ClassroomInteriors/B112");var p=room.Find("PhotoPresentationDetails");var seating=room.Find("Seating");
            if(seating.Cast<Transform>().Count(t=>t.name.StartsWith("Desk_"))!=21||seating.Cast<Transform>().Count(t=>t.name.StartsWith("Chair_"))!=42)throw new Exception("B112 seating changed");
            var partition=p.Find("FoldedPartition").GetComponent<BoxCollider>();Physics.SyncTransforms();
            foreach(var c in seating.GetComponentsInChildren<BoxCollider>())if(partition.bounds.Intersects(c.bounds))throw new Exception("Folded partition overlaps furniture: "+c.name);
            if(room.GetComponentsInChildren<Light>().Length!=0||room.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterials.Any(m=>m&&m.IsKeywordEnabled("_EMISSION"))))throw new Exception("B112 artificial glow added");
            if(p.GetComponentsInChildren<Transform>().Count(t=>t.name=="FoldedPartition")!=1||p.Find("FoldedPartition").Cast<Transform>().Count(t=>t.name.StartsWith("Panel"))!=10)throw new Exception("Partition missing or duplicated");
            ConvergenceHallB112DiagonalScreen.Validate(root);
            Debug.Log("B112_PRESENTATION_OK: preserved seating, partition clear of furniture, no emissive light; details saved.");
        }
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ConvergenceHallWallSurfaceCleanup.Restore(root);ApplyTo(root);ConvergenceHallWallSurfaceCleanup.ApplyTo(root);ConvergenceHallDoorInteraction.Build(root);Validate(root);ConvergenceHallWallSurfaceCleanup.Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.FocusB112Presentation",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;cam.fieldOfView=76;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            var root=GameObject.Find("ConvergenceHallB1");var room=root.transform.Find("ClassroomInteriors/B112");var floor=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;float w=floor.size.x,d=floor.size.z;
            var points=new[]{new Vector3(0,1.55f,-d/2+.60f),new Vector3(-w*.10f,1.55f,d/2-1.75f),new Vector3(w/2-2.2f,1.65f,-1.1f),new Vector3(0,1.55f,d/2-4.4f)};
            var targets=new[]{new Vector3(0,1.50f,d/2),new Vector3(0,1.50f,-d/2),new Vector3(w/2-.5f,1.7f,1.0f),new Vector3(0,1.8f,d/2)};
            var names=new[]{"B112FromRear","B112FromFront","B112FoldedPartition","B112PresentationFront"};
            for(int i=0;i<names.Length;i++)
            {
                cam.transform.position=room.TransformPoint(points[i]);cam.transform.LookAt(room.TransformPoint(targets[i]));
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,names[i]+".png");var stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException(path);yield return null;}
            }
        }
    }
}
