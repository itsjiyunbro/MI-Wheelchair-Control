using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallRoomCompletion
    {
        const float H=2.85f,CeilingY=H-.012f;
        public const bool IncludeCabinRearInConcrete=false;
        static Material white,trim,metal,black;
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        public static void SolidEdge(GameObject root,string roomName,string side)
        {
            var edge=root.transform.Find("RoomsAndCores/"+roomName+"/"+side);
            foreach(var r in edge.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            var wall=edge.Find("LowerWall");wall.gameObject.SetActive(true);wall.GetComponent<Renderer>().enabled=true;
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.GetComponent<MeshFilter>().sharedMesh=primitive.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(primitive);
            wall.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/WarmWhite.mat");
            var at=wall.localPosition;at.y=H/2;wall.localPosition=at;var size=wall.localScale;size.y=H;wall.localScale=size;
            var skirting=edge.Find("Skirting");if(skirting){skirting.gameObject.SetActive(true);skirting.GetComponent<Renderer>().enabled=true;}
            foreach(string name in new[]{"CorridorGlazing","MeasuredGlassPanels","DoorOpeningSurfaces"}){var group=edge.Find(name);if(group)group.gameObject.SetActive(false);}
        }
        static void Recombine(Transform room)
        {
            var combined=room.Find("CombinedVisual");if(combined)UnityEngine.Object.DestroyImmediate(combined.gameObject);
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())r.enabled=true;
            P.Combine(room,"Interior_"+room.name+"Combined");
        }
        static void CorrectClassrooms(GameObject root,bool createMissing)
        {
            if(createMissing)foreach(string name in new[]{"B104","B105"})if(!root.transform.Find("ClassroomInteriors/"+name))ConvergenceHallClassroomInteriors.BuildSpecific(root,name);
            var room=root.transform.Find("ClassroomInteriors/B109");room.localRotation=Quaternion.Euler(0,180,0);
            var teacher=room.Find("TeacherChair");if(teacher&&teacher.localPosition.x>0){var at=teacher.localPosition;at.x=-at.x;teacher.localPosition=at;Recombine(room);}
            var b106=root.transform.Find("ClassroomInteriors/B106");var chair=b106.Find("TeacherChair");
            if(chair){UnityEngine.Object.DestroyImmediate(chair.gameObject);Recombine(b106);}
            SolidEdge(root,"B106","East");SolidEdge(root,"B103","West");SolidEdge(root,"B109","South");SolidEdge(root,"B109","North");
            SolidEdge(root,"B104","North");SolidEdge(root,"B105","North");
        }
        static void Roof(Transform group,Transform room)
        {
            var floors=room.Cast<Transform>().Where(t=>t.name.StartsWith("RoomFloor")&&t.gameObject.activeSelf).ToArray();
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            foreach(var floor in floors)
            {
                var source=floor.GetComponent<MeshFilter>().sharedMesh;var v=source.vertices;var tr=source.triangles;float top=floor.GetComponent<Renderer>().bounds.max.y;
                for(int i=0;i<tr.Length;i+=3)
                {
                    var a=floor.TransformPoint(v[tr[i]]);var b=floor.TransformPoint(v[tr[i+1]]);var c=floor.TransformPoint(v[tr[i+2]]);
                    if(Mathf.Abs(a.y-top)>.002f||Mathf.Abs(b.y-top)>.002f||Mathf.Abs(c.y-top)>.002f)continue;
                    if(Vector3.Cross(b-a,c-a).y>0){var swap=b;b=c;c=swap;}
                    foreach(var p0 in new[]{a,b,c}){var p=p0;p.y=CeilingY;triangles.Add(vertices.Count);vertices.Add(p);uv.Add(new Vector2(p.x/.6f,p.z/.6f));}
                }
            }
            Check(vertices.Count>=6,"Roof footprint missing: "+room.name);
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var roof=P.Group(group,room.name+"Ceiling");roof.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("CompletedRoom_"+room.name+"Ceiling",mesh);
            roof.gameObject.AddComponent<MeshRenderer>().sharedMaterial=white;roof.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            var bounds=floors[0].GetComponent<Renderer>().bounds;
            if(room.name=="Lounge"||room.name=="EastLift"||room.name=="WestLiftCore")
            {
                // Concealed perimeter return connects the higher room ceiling to the 2.80m corridor ceiling.
                int index=0;foreach(float x in new[]{bounds.min.x,bounds.max.x})P.Box(roof,"CeilingEdgeReturnX"+index++,new Vector3(x,H-.024f,bounds.center.z),new Vector3(.014f,.056f,bounds.size.z),white);
                index=0;foreach(float z in new[]{bounds.min.z,bounds.max.z})P.Box(roof,"CeilingEdgeReturnZ"+index++,new Vector3(bounds.center.x,H-.024f,z),new Vector3(bounds.size.x,.056f,.014f),white);
            }
            if(room.name=="Lounge")LoungeStructure(roof,bounds);
            else if(room.name=="EastLift"||room.name=="WestLiftCore")HallFixtures(roof,group.parent.Find("CoreHalls/"+(room.name=="EastLift"?"B112ElevatorHall":"B109ElevatorHall")));
        }
        static void HallFixtures(Transform parent,Transform hall)
        {
            var floor=hall.Find("HallFloor");float cx=floor.localPosition.x,depth=floor.localScale.z;
            for(int i=0;i<2;i++)
            {
                var point=hall.TransformPoint(new Vector3(cx,CeilingY-.01f,Mathf.Min(depth-1.8f,i==0?1.0f:2.4f)));
                P.Cylinder(parent,"HallDownlightRim"+i,point,new Vector3(.15f,.006f,.15f),trim,Quaternion.identity);
                P.Cylinder(parent,"HallDownlightDiffuser"+i,point+Vector3.down*.008f,new Vector3(.12f,.003f,.12f),white,Quaternion.identity);
            }
            var centre=hall.TransformPoint(new Vector3(cx,CeilingY-.01f,1.65f));
            P.Box(parent,"CeilingAccessFrame",centre,new Vector3(.58f,.014f,.58f),trim);P.Box(parent,"CeilingAccessPanel",centre+Vector3.down*.01f,new Vector3(.54f,.007f,.54f),white);
        }
        static void LoungeStructure(Transform parent,Bounds b)
        {
            for(float z=b.min.z+1.0f;z<b.max.z;z+=2.1f)P.Box(parent,"WhiteStructuralBeam",new Vector3(b.center.x,H-.095f,z),new Vector3(b.size.x,.13f,.15f),white);
            P.Box(parent,"CeilingDuct",new Vector3(b.center.x-.8f,H-.15f,b.center.z),new Vector3(.22f,.18f,b.size.z),trim);
            for(int i=0;i<2;i++)
            {
                var centre=new Vector3(b.center.x,2.48f,Mathf.Lerp(b.min.z,b.max.z,i==0?.28f:.72f));var ring=P.Group(parent,"SuspendedRectangularFixture"+i);ring.localPosition=centre;
                const float w=3.15f,d=1.90f;
                foreach(float x in new[]{-w/2,w/2})
                {
                    P.Box(ring,"BlackLongRail",new Vector3(x,0,0),new Vector3(.055f,.06f,d),black);
                    P.Box(ring,"WhiteLongDiffuser",new Vector3(x,-.032f,0),new Vector3(.027f,.003f,d-.07f),white);
                }
                foreach(float z in new[]{-d/2,d/2})
                {
                    P.Box(ring,"BlackCrossRail",new Vector3(0,0,z),new Vector3(w,.06f,.055f),black);
                    P.Box(ring,"WhiteCrossDiffuser",new Vector3(0,-.032f,z),new Vector3(w-.07f,.003f,.027f),white);
                }
                foreach(float x in new[]{-w/2+.12f,w/2-.12f})foreach(float z in new[]{-d/2+.12f,d/2-.12f})P.Tube(ring,"Suspension",new Vector3(x,.03f,z),new Vector3(x,CeilingY-centre.y,z),.008f,metal);
            }
        }
        static void CompleteRoofs(GameObject root)
        {
            var old=root.transform.Find("CompletedRoomCeilings");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var group=P.Group(root.transform,"CompletedRoomCeilings");
            white=P.Mat("CompletedRoomPlainCeiling",new Color(.90f,.91f,.89f),.08f);trim=P.Mat("CompletedRoomCeilingTrim",new Color(.68f,.70f,.69f),.13f);
            metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/Metal.mat");black=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/EntryCeilingBlack.mat");
            foreach(Transform room in root.transform.Find("RoomsAndCores"))
            {
                if(!room.gameObject.activeSelf||room.name=="SunkenVoid"||room.name=="EastStair"||room.name=="WestStair"||root.transform.Find("ClassroomInteriors/"+room.name))continue;
                if(room.Find("RoomFloor"))Roof(group,room);
            }
        }
        static bool WallMaterial(Material m)=>m&&(m.name=="WarmWhite"||m.name=="CoreConcrete"||m.name=="B112ElevatorRoomConcrete"||m.name=="B112ElevatorRoomWhite"||m.name=="B112ExposedConcrete");
        static void CompleteLiftWalls(GameObject root)
        {
            var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/B112ExposedConcrete.mat");Check(concrete,"Photo concrete material missing");
            foreach(bool east in new[]{true,false})
            {
                var hall=root.transform.Find("CoreHalls/"+(east?"B112ElevatorHall":"B109ElevatorHall"));var core=root.transform.Find("RoomsAndCores/"+(east?"EastLift":"WestLiftCore"));
                var original=core.GetComponentsInChildren<Renderer>(true);
                var shells=hall.Cast<Transform>().Where(t=>t.GetComponent<Renderer>()&&t.name!="HallFloor").Select(t=>t.GetComponent<Renderer>());
                var surround=ConvergenceHallElevatorCabins.Door(root,east).Find("RecessedWallSurround").GetComponentsInChildren<Renderer>(true);
                foreach(var r in original.Concat(shells).Concat(surround))
                {var materials=r.sharedMaterials;for(int i=0;i<materials.Length;i++)if(WallMaterial(materials[i]))materials[i]=east?concrete:AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/WarmWhite.mat");r.sharedMaterials=materials;}
                if(IncludeCabinRearInConcrete)ConvergenceHallElevatorCabins.Door(root,east).Find("MeasuredCabin/RearWall").GetComponent<Renderer>().sharedMaterial=concrete;
            }
        }
        public static void ApplyTo(GameObject root,bool createMissing=false)
        {
            CorrectClassrooms(root,createMissing);CompleteRoofs(root);CompleteLiftWalls(root);ConvergenceHallLecternLighting.DisableEmission(root);
        }
        static bool CeilingCovers(Transform surface,float x,float z)
        {
            var mesh=surface.GetComponent<MeshFilter>().sharedMesh;var v=mesh.vertices;var tr=mesh.triangles;
            for(int i=0;i<tr.Length;i+=3)
            {
                var a=surface.TransformPoint(v[tr[i]]);var b=surface.TransformPoint(v[tr[i+1]]);var c=surface.TransformPoint(v[tr[i+2]]);
                float area=(b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x);if(Mathf.Abs(area)<.000001f)continue;
                float u=((x-a.x)*(c.z-a.z)-(z-a.z)*(c.x-a.x))/area,w=((b.x-a.x)*(z-a.z)-(b.z-a.z)*(x-a.x))/area;
                if(u>=-.0001f&&w>=-.0001f&&u+w<=1.0001f)return true;
            }
            return false;
        }
        static void Validate(GameObject root)
        {
            Check(root.transform.Find("ClassroomInteriors").childCount==14,"Fourteen classroom interiors required");
            foreach(string name in ConvergenceHallClassroomInteriors.Rooms)
            {
                var room=root.transform.Find("ClassroomInteriors/"+name);Check(room&&room.Find("FrontWhiteboard")&&room.Find("Lectern")&&room.Find("CeilingUnderside"),"Incomplete classroom: "+name);
                Check(room.Find("Seating").childCount>=6,"Classroom seating missing: "+name);
                var renderer=room.Find("CombinedVisual").GetComponent<MeshRenderer>();var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                int ceilingIndex=Array.FindIndex(renderer.sharedMaterials,m=>m.name=="ClassroomAcousticCeiling");Check(ceilingIndex>=0&&mesh.GetTriangles(ceilingIndex).Length>=6,"Classroom ceiling not in rendered mesh: "+name);
            }
            var b109=root.transform.Find("ClassroomInteriors/B109");Check(Vector3.Dot(b109.forward,Vector3.back)>.999f,"B109 does not face south");
            Check(!root.transform.Find("ClassroomInteriors/B106/TeacherChair"),"B106 front teacher chair remains");
            foreach(var pair in new[]{new[]{"B106","East"},new[]{"B103","West"}})
            {
                var edge=root.transform.Find("RoomsAndCores/"+pair[0]+"/"+pair[1]);Check(edge.Find("LowerWall").GetComponent<Renderer>().sharedMaterial.name=="WarmWhite","Requested opaque wall not white");
                Check(!edge.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.sharedMaterials.Any(m=>m&&m.HasProperty("_Surface")&&m.GetFloat("_Surface")>.5f)),"Transparent section remains: "+pair[0]);
            }
            int samples=0;
            foreach(Transform room in root.transform.Find("RoomsAndCores"))
            {
                if(!room.gameObject.activeSelf||room.name=="SunkenVoid"||room.name=="EastStair"||room.name=="WestStair")continue;
                var interior=root.transform.Find("ClassroomInteriors/"+room.name);var ceiling=interior?interior.Find("CeilingUnderside"):root.transform.Find("CompletedRoomCeilings/"+room.name+"Ceiling");
                Check(ceiling,"Room ceiling absent: "+room.name);
                foreach(Transform floor in room.Cast<Transform>().Where(t=>t.name.StartsWith("RoomFloor")&&t.gameObject.activeSelf))
                {
                    var b=floor.GetComponent<Renderer>().bounds;
                    for(int i=1;i<4;i++)for(int j=1;j<4;j++)
                    {
                        float x=Mathf.Lerp(b.min.x,b.max.x,i/4f),z=Mathf.Lerp(b.min.z,b.max.z,j/4f);
                        Check(CeilingCovers(ceiling,x,z),"Roof coverage missing: "+room.name+" at "+x+","+z);samples++;
                    }
                }
            }
            foreach(string core in new[]{"EastLift","WestLiftCore"})Check(root.transform.Find("RoomsAndCores/"+core).GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>!WallMaterial(m)||m.name==(core=="EastLift"?"B112ExposedConcrete":"WarmWhite"))),"Lift perimeter finish differs: "+core);
            Check(root.GetComponentsInChildren<InteractiveDoor>().Length==(root.transform.Find("CoreHalls/B109ElevatorHall/ServicePanels/EPSPanel")?48:46),"Interactive doors lost");
            typeof(ConvergenceHallClassroomInteriors).GetMethod("Validate",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{root});
            ConvergenceHallLecternLighting.Validate(root);
            Debug.Log("ROOM_COMPLETION_OK: 14 furnished/roofed classrooms; both elevator room roofs and continuous concrete perimeter; lounge beams and non-emissive suspended fixtures; B106 East/B103 West opaque; B109 south-facing; B106 teacher chair removed; "+samples+" ceiling coverage samples; 46 interactive doors retained.");
        }
        [MenuItem("Tools/EEG Wheelchair/Complete Missing Rooms And Roofs")]
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                ConvergenceHallWallSurfaceCleanup.Restore(root);ApplyTo(root,true);ConvergenceHallWallSurfaceCleanup.ApplyTo(root);ConvergenceHallDoorInteraction.Build(root);
                Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
            SessionState.SetBool("EEG.ConvergenceQA.FocusRoomCompletion",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var root=GameObject.Find("ConvergenceHallB1").transform;var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            string[] names={"B104CompletedClassroom","B105CompletedClassroom","B109SouthFacing","B106FrontWithoutChair","B103OpaqueRear","B106OpaqueBluebellWall","LoungeCompletedCeiling","B112ElevatorRoomCeiling","B109ElevatorRoomCeiling"};
            for(int i=0;i<names.Length;i++)
            {
                cam.fieldOfView=72;
                if(i<4)
                {
                    var room=root.Find("ClassroomInteriors/"+new[]{"B104","B105","B109","B106"}[i]);var floor=root.Find("RoomsAndCores/"+room.name+"/RoomFloor").GetComponent<Renderer>().bounds;
                    float depth=i<3?floor.size.z:floor.size.x;
                    cam.transform.position=room.TransformPoint(new Vector3(0,1.45f,-depth/2+1.0f));cam.transform.LookAt(room.TransformPoint(new Vector3(0,1.30f,depth/2-1.0f)));
                }
                else if(i==4||i==5)
                {
                    string roomName=i==4?"B103":"B106";var b=root.Find("RoomsAndCores/"+roomName+"/RoomFloor").GetComponent<Renderer>().bounds;
                    cam.transform.position=new Vector3(b.center.x,1.55f,b.center.z);cam.transform.LookAt(new Vector3(i==4?b.min.x:b.max.x,1.65f,b.center.z));
                }
                else if(i==6)
                {
                    var b=root.Find("RoomsAndCores/Lounge/RoomFloor").GetComponent<Renderer>().bounds;cam.fieldOfView=85;
                    cam.transform.position=new Vector3(b.center.x,1.25f,b.min.z+1.2f);cam.transform.LookAt(new Vector3(b.center.x,2.35f,b.center.z));
                }
                else
                {
                    var hall=root.Find("CoreHalls/"+(i==7?"B112ElevatorHall":"B109ElevatorHall"));cam.fieldOfView=85;
                    cam.transform.position=hall.TransformPoint(new Vector3(0,1.25f,-.45f));cam.transform.LookAt(hall.TransformPoint(new Vector3(0,2.10f,2.8f)));
                }
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                var path=Path.Combine(folder,names[i]+".png");var stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException(names[i]);yield return null;}
            }
            Debug.Log("ROOM_COMPLETION_CAPTURE_OK: added classrooms, direction, opaque walls, removed chair, lounge and elevator ceilings captured.");
        }
    }
}
