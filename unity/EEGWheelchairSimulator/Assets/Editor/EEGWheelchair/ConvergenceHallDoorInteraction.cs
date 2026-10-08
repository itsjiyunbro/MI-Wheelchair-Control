using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallDoorInteraction
    {
        struct Hole {public float min,max,height;}
        static Dictionary<Transform,List<Hole>> holes;
        static Transform dynamics;
        static InteractiveDoor.Motion Motion(Transform t,Vector3 hinge,float angle,Vector3 slide=default,Quaternion? closedRotation=null)
        {return new InteractiveDoor.Motion{target=t,closedPosition=t.localPosition,closedRotation=closedRotation??t.localRotation,hinge=hinge,angle=angle,slide=slide};}
        static InteractiveDoor.Motion Sliding(Transform t,Vector3 worldOffset)=>Motion(t,t.localPosition,0,t.parent.InverseTransformVector(worldOffset));
        static BoxCollider Body(Transform leaf)
        {
            var original=leaf.GetComponent<BoxCollider>();if(original)return original;
            var child=leaf.Find("GeneratedDoorCollider");if(!child)child=P.Group(leaf,"GeneratedDoorCollider");
            var c=child.GetComponent<BoxCollider>();if(!c)c=child.gameObject.AddComponent<BoxCollider>();
            c.center=Vector3.zero;c.size=Vector3.one;child.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");return c;
        }
        static InteractiveDoor Register(Transform root,string label,List<InteractiveDoor.Motion> parts,List<BoxCollider> bodies,Vector3 worldPoint,bool automatic=false,float initial=0,Collider[] barriers=null,float sensorWidth=.75f)
        {
            var door=root.gameObject.AddComponent<InteractiveDoor>();
            door.Configure(label,parts.ToArray(),bodies.ToArray(),root.InverseTransformPoint(worldPoint),automatic,initial,barriers,sensorWidth);return door;
        }
        static void HoleAt(Transform edge,Vector3 world,float width,float height)
        {
            float along=edge.InverseTransformPoint(world).z;float half=width/2/Mathf.Abs(edge.lossyScale.z);
            if(!holes.TryGetValue(edge,out var list)){list=new List<Hole>();holes.Add(edge,list);}
            list.Add(new Hole{min=along-half-.008f,max=along+half+.008f,height=height});
        }
        static void SplitWallCollision(Transform edge,List<Hole> openings)
        {
            var original=edge.GetComponent<BoxCollider>();if(!original)throw new Exception("Door boundary collider missing: "+edge.name);
            var parent=edge.Find("DoorBoundarySegments");if(parent)UnityEngine.Object.DestroyImmediate(parent.gameObject);parent=P.Group(edge,"DoorBoundarySegments");
            var size=original.size;var centre=original.center;float bottom=centre.y-size.y/2,top=centre.y+size.y/2,start=centre.z-size.z/2,end=centre.z+size.z/2,cursor=start;int n=0;
            void Segment(float a,float b,float low,float high)
            {
                if(b-a<.0001f||high-low<.0001f)return;
                var t=P.Group(parent,"SolidBoundary"+n++);t.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");
                var c=t.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(centre.x,(low+high)/2,(a+b)/2);c.size=new Vector3(size.x,high-low,b-a);
            }
            foreach(var hole in openings.OrderBy(h=>h.min))
            {
                float a=Mathf.Max(start,hole.min),b=Mathf.Min(end,hole.max);if(b<=a)continue;
                Segment(cursor,a,bottom,top);Segment(a,b,Mathf.Max(bottom,hole.height),top);cursor=Mathf.Max(cursor,b);
            }
            Segment(cursor,end,bottom,top);original.enabled=false;
        }
        static void PlanDoors(GameObject building)
        {
            foreach(Transform root in building.transform.Find("PlanDoors"))
            {
                var name=root.name.Split('_');string room=name[0],side=name[1];var edge=building.transform.Find("RoomsAndCores/"+room+"/"+side);
                var motions=new List<InteractiveDoor.Motion>();var bodies=new List<BoxCollider>();var points=new List<Vector3>();float width=0;
                bool automatic=false;
                if(room=="Lounge")
                {
                    var leaf=root.Find("PhotoLoungeEntrance/RightSlidingLeaf");motions.Add(Sliding(leaf,-root.right*1.105f));
                    var c=Body(leaf.Find("ClearLeafGlass"));c.size=new Vector3(1.075f/.995f,2.12f/2.08f,.070f/.014f);bodies.Add(c);
                    points.Add(leaf.TransformPoint(new Vector3(0,1.1f,0)));width=1.075f;automatic=true;
                }
                else
                {
                    foreach(Transform pivot in root.Cast<Transform>().Where(t=>t.name.Contains("_InwardY")&&t.gameObject.activeSelf))
                    {
                        float angle=pivot.name.EndsWith("-90")?-90:90;var leaf=pivot.Find("Leaf");
                        motions.Add(Motion(pivot,pivot.localPosition,angle));bodies.Add(Body(leaf));points.Add(leaf.position);width+=leaf.localScale.x+.012f;
                    }
                    if(motions.Count==0)
                    {
                        var left=root.Find("SlidingLeafLeft");var right=root.Find("SlidingLeafRight");if(!left||!right)continue;
                        float travel=left.localScale.x+.02f;motions.Add(Sliding(left,-root.right*travel));motions.Add(Sliding(right,root.right*travel));
                        foreach(var leaf in new[]{left,right}){bodies.Add(Body(leaf));points.Add(leaf.position);width+=leaf.localScale.x+.014f;}
                        foreach(int i in new[]{0,1})motions.Add(Sliding(root.Find("SlidingStile"+i),root.right*(i==0?-travel:travel)));
                        automatic=true;
                    }
                }
                var point=points.Aggregate(Vector3.zero,(sum,p)=>sum+p)/points.Count;
                Register(root,room+(automatic?" 자동문":" 출입문"),motions,bodies,point,automatic,0,null,width/2+.35f);
                HoleAt(edge,point,width,2.20f);
            }
        }
        static void B111(GameObject building)
        {
            var root=building.transform.Find("DoorsAndSigns/B111Entrance");var leaf=root.Find("OpaqueDoor");
            var hinge=new Vector3(leaf.localPosition.x+leaf.localScale.x/2,0,leaf.localPosition.z);
            var parts=new[]{"OpaqueDoor","HandleBase","LeverHandle"}.Select(n=>Motion(root.Find(n),hinge,90)).ToList();
            Register(root,"B111 출입문",parts,new List<BoxCollider>{Body(leaf)},leaf.position);
            HoleAt(building.transform.Find("RoomsAndCores/B111/EastUpper"),leaf.position,leaf.localScale.x,2.35f);
        }
        static void MainEntry(GameObject building)
        {
            var entry=building.transform.Find("MainEntranceUpgrade");var exterior=building.transform.Find("ExteriorBoundary/EntranceGlazing");
            foreach(string bankName in new[]{"ExteriorDoorBank","InteriorDoorBank"})
            {
                var bank=entry.Find(bankName);bool inside=bankName=="InteriorDoorBank";
                foreach(string bay in new[]{"LeftBay","RightBay"})
                {
                    var parts=new List<InteractiveDoor.Motion>();var bodies=new List<BoxCollider>();var centres=new List<Vector3>();float initial=0;
                    foreach(string suffix in new[]{"LeafA","LeafB"})
                    {
                        var pivot=bank.Find(bay+suffix);float x=pivot.Find("ClearDoorGlass").localPosition.x;
                        float angle=(x<0?1:-1)*(inside?-90:90);
                        initial=Mathf.Max(initial,Mathf.Abs(Mathf.DeltaAngle(0,pivot.localEulerAngles.y))/90);
                        parts.Add(Motion(pivot,pivot.localPosition,angle,Vector3.zero,Quaternion.identity));
                        bodies.Add(pivot.Find("LeafCollision").GetComponent<BoxCollider>());centres.Add(pivot.Find("ClearDoorGlass").position);
                    }
                    var centre=(centres[0]+centres[1])/2;
                    // Closed interaction centre stays at the bank plane even when a pair initially opens 70 degrees.
                    centre=bank.TransformPoint(new Vector3((bank.Find(bay+"LeafA").localPosition.x+bank.Find(bay+"LeafB").localPosition.x)/2,1.13f,0));
                    var control=P.Group(dynamics,bankName+"_"+bay);control.position=centre-Vector3.up*1.13f;control.rotation=bank.rotation;
                    Register(control,inside?"주출입구 안쪽 문":"주출입구 바깥 문",parts,bodies,centre,false,initial);
                    if(!inside)HoleAt(exterior,centre,2.0f,2.25f);
                }
            }
            var sub=building.transform.Find("WestSubEntrance");var leaf=sub.Find("GlassDoorLeaf");
            Register(sub,"부출입구",new List<InteractiveDoor.Motion>{Motion(leaf,new Vector3(.81f,0,0),90)},new List<BoxCollider>{Body(leaf.Find("Glass"))},leaf.Find("Glass").position);
            HoleAt(building.transform.Find("ExteriorBoundary/SouthLeft"),leaf.Find("Glass").position,1.12f,2.20f);
        }
        static void OtherSwingDoors(GameObject building)
        {
            foreach(string hallName in new[]{"B112Stairwell","B109Stairwell"})
            {
                var hall=building.transform.Find("CoreHalls/"+hallName);var right=hall.Find("ClosedRightLeaf");
                if(right)
                {
                    var hinge=new Vector3(right.localPosition.x+right.localScale.x/2,0,right.localPosition.z);var parts=new List<InteractiveDoor.Motion>();
                    foreach(string n in new[]{"ClosedRightLeaf","ClosedRightLeafCollision","DoorKnob","DoorSign","DoorSignBacking"})if(hall.Find(n))parts.Add(Motion(hall.Find(n),hinge,-90));
                    Register(hall,"B112 계단실 문",parts,new List<BoxCollider>{hall.Find("ClosedRightLeafCollision").GetComponent<BoxCollider>()},right.position);
                }
                var storage=hall.Find("UnderStairStorage/StorageDoor");var closed=storage.Find("ClosedLeaf");var pivot=new Vector3(-closed.localScale.x/2,0,closed.localPosition.z);
                var moving=storage.Cast<Transform>().Where(t=>!t.name.StartsWith("Frame")).Select(t=>Motion(t,pivot,90)).ToList();
                Register(storage,hallName.Substring(0,4)+" 계단 밑 창고",moving,new List<BoxCollider>{closed.GetComponent<BoxCollider>()},closed.position);
            }
            var service=building.transform.Find("CoreHalls/B112ElevatorHall/ServicePanels");
            foreach(Transform panel in service)
            {
                var leaf=panel.Find("WhiteLeaf");var hinge=new Vector3(leaf.localScale.x/2,0,leaf.localPosition.z);
                var moving=panel.Cast<Transform>().Where(t=>t.name=="WhiteLeaf"||t.name=="RoundLock"||t.name=="RightHinge"||t.name=="SmallNavyPlacard"||t.name=="ServiceLabel").Select(t=>Motion(t,hinge,-90)).ToList();
                Register(panel,"B112 점검문",moving,new List<BoxCollider>{Body(leaf)},leaf.position);
            }
        }
        static void WestInspectionDoors(GameObject building)
        {
            var service=building.transform.Find("CoreHalls/B109ElevatorHall/ServicePanels");
            if(!service||!service.Find("EPSPanel"))return;
            foreach(Transform panel in service)
            {
                var moving=panel.Find("DoorLeaf");var leaf=moving.Find("WhiteLeaf");
                var hinge=new Vector3(-leaf.localScale.x/2,0,leaf.localPosition.z);
                Register(panel,"서쪽 "+(panel.name=="EPSPanel"?"EPS":"TPS")+" 점검문",new List<InteractiveDoor.Motion>{Motion(moving,hinge,90)},new List<BoxCollider>{Body(leaf)},leaf.position);
            }
        }
        struct Vertex {public Vector3 point,normal;public Vector2 uv;public Vertex Lerp(Vertex b,float t)=>new Vertex{point=Vector3.Lerp(point,b.point,t),normal=Vector3.Lerp(normal,b.normal,t).normalized,uv=Vector2.Lerp(uv,b.uv,t)};}
        static Mesh HalfMesh(Mesh source,Transform sourceTransform,Transform door,int sign)
        {
            var vertices=source.vertices;var normals=source.normals;var uv=source.uv;var output=new List<Vector3>();var n=new List<Vector3>();var u=new List<Vector2>();var triangles=new List<int>();
            float Distance(Vertex p)=>door.InverseTransformPoint(sourceTransform.TransformPoint(p.point)).x*sign;
            var original=source.triangles;
            for(int i=0;i<original.Length;i+=3)
            {
                var polygon=new List<Vertex>();for(int j=0;j<3;j++){int index=original[i+j];polygon.Add(new Vertex{point=vertices[index],normal=normals.Length==vertices.Length?normals[index]:Vector3.back,uv=uv.Length==vertices.Length?uv[index]:Vector2.zero});}
                var clipped=new List<Vertex>();var previous=polygon[2];float pd=Distance(previous);
                foreach(var current in polygon)
                {
                    float cd=Distance(current);if((pd>=0)!=(cd>=0))clipped.Add(previous.Lerp(current,pd/(pd-cd)));
                    if(cd>=0)clipped.Add(current);previous=current;pd=cd;
                }
                for(int j=1;j+1<clipped.Count;j++)
                {
                    int at=output.Count;foreach(var p in new[]{clipped[0],clipped[j],clipped[j+1]}){output.Add(p.point);n.Add(p.normal);u.Add(p.uv);}triangles.AddRange(new[]{at,at+1,at+2});
                }
            }
            var mesh=new Mesh();mesh.SetVertices(output);mesh.SetNormals(n);mesh.SetUVs(0,u);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
        static void ElevatorGraphics(Transform door,List<InteractiveDoor.Motion> motions,float travel,string id)
        {
            var photo=door.Find("PhotoElevatorDetails");
            foreach(string name in new[]{"PinchWarning","HandWarning"})
            {var t=photo.Find(name);float x=door.InverseTransformPoint(t.position).x;motions.Add(Sliding(t,door.right*(x<0?-travel:travel)));}
            var seal=photo.Find("UniversityDoorSeal");var old=photo.Find("DoorSealHalves");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var halves=P.Group(photo,"DoorSealHalves");int count=0;
            foreach(var source in seal.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=source.GetComponent<MeshRenderer>();if(!renderer)continue;
                renderer.enabled=false;
                foreach(int sign in new[]{-1,1})
                {
                    var mesh=HalfMesh(source.sharedMesh,source.transform,door,sign);if(mesh.vertexCount==0){UnityEngine.Object.DestroyImmediate(mesh);continue;}
                    var copy=P.Group(halves,"SealPart"+count);copy.position=source.transform.position;copy.rotation=source.transform.rotation;copy.localScale=source.transform.lossyScale;
                    copy.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("DynamicDoor_"+id+"_Seal"+count,mesh);
                    copy.gameObject.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;copy.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    motions.Add(Sliding(copy,door.right*(sign*travel)));count++;
                }
            }
            foreach(var text in seal.GetComponentsInChildren<Text>(true))
            {
                var t=text.transform;float x=door.InverseTransformPoint(t.position).x;
                motions.Add(Sliding(t,door.right*(x<0?-travel:travel)));
            }
        }
        static void Elevators(GameObject building)
        {
            foreach(bool east in new[]{true,false})
            {
                var root=ConvergenceHallElevatorCabins.Door(building,east);var parts=new List<InteractiveDoor.Motion>();var bodies=new List<BoxCollider>();
                float travel=.66f;
                foreach(int i in new[]{0,1}){var leaf=root.Find("SlidingLeaf"+i);parts.Add(Sliding(leaf,root.right*(i==0?-travel:travel)));bodies.Add(Body(leaf));}
                parts.Add(Sliding(root.Find("CentreSeam"),-root.right*travel));ElevatorGraphics(root,parts,travel,east?"B112":"B109");
                var barrier=root.Find("MeasuredCabin/ClosedDoorCollision").GetComponent<Collider>();
                var point=(root.Find("SlidingLeaf0").position+root.Find("SlidingLeaf1").position)/2;
                Register(root,east?"B112 엘리베이터 문":"B109 엘리베이터 문",parts,bodies,point,false,0,new[]{barrier});
            }
        }
        public static void Build(GameObject building)
        {
            foreach(var component in building.GetComponentsInChildren<InteractiveDoor>(true)){component.ResetDoor();UnityEngine.Object.DestroyImmediate(component);}
            foreach(var t in building.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="GeneratedDoorCollider").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            var old=building.transform.Find("DoorDynamics");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);dynamics=P.Group(building.transform,"DoorDynamics");holes=new Dictionary<Transform,List<Hole>>();
            PlanDoors(building);B111(building);MainEntry(building);OtherSwingDoors(building);WestInspectionDoors(building);Elevators(building);
            foreach(var pair in holes)SplitWallCollision(pair.Key,pair.Value);
            var digest=typeof(ConvergenceHallWallSurfaceCleanup).GetMethod("ColliderState",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            SessionState.SetString("EEG.WallSurfaceCollision",(string)digest.Invoke(null,new object[]{building}));
            Debug.Log("INTERACTIVE_DOORS_BUILT: "+building.GetComponentsInChildren<InteractiveDoor>(true).Length+" door controls; annotated hinge directions, sensor sliders, entry pairs, storage/service and elevator sliding panels; "+holes.Count+" boundaries opened only at doors.");
        }
        public static void ConfigureScene()
        {
            var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();var guard=chair.GetComponent<WheelchairCollisionGuard>();var visual=chair.transform.Find("WheelchairVisual");float x=0,z=0;
            foreach(var filter in visual.GetComponentsInChildren<MeshFilter>())
            {
                var b=filter.sharedMesh.bounds;foreach(int a in new[]{-1,1})foreach(int c in new[]{-1,1})foreach(int d in new[]{-1,1})
                {var p=chair.transform.InverseTransformPoint(filter.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(a,c,d))));x=Mathf.Max(x,Mathf.Abs(p.x));z=Mathf.Max(z,Mathf.Abs(p.z));}
            }
            var settings=new SerializedObject(guard);settings.FindProperty("useOrientedFootprint").boolValue=true;settings.FindProperty("footprintHalfExtents").vector3Value=new Vector3(x+.012f,.58f,z+.012f);settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(guard);
            var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();var panel=(RectTransform)hud.transform.Find("StatusPanel");
            var reset=(RectTransform)panel.Find("ResetButton");float controlsTop=-reset.anchoredPosition.y+reset.sizeDelta.y+14;
            panel.sizeDelta=new Vector2(panel.sizeDelta.x,Mathf.Max(panel.sizeDelta.y,controlsTop+130));
            var cameraButton=panel.Find("CameraViewButton") as RectTransform;
            if(cameraButton){cameraButton.anchorMin=cameraButton.anchorMax=new Vector2(0,1);cameraButton.pivot=new Vector2(0,1);cameraButton.anchoredPosition=new Vector2(20,-controlsTop);cameraButton.sizeDelta=new Vector2(300,36);}
            var existing=panel.Find("DoorButton");var go=existing?existing.gameObject:UnityEngine.Object.Instantiate(panel.Find("StartButton").gameObject,panel);go.name="DoorButton";
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(20,-controlsTop-44);rect.sizeDelta=new Vector2(300,36);
            var button=go.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();button.navigation=new Navigation{mode=Navigation.Mode.None};button.interactable=false;
            var label=go.GetComponentInChildren<Text>();label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");label.fontSize=18;label.text="문 열기 / 닫기 (E)";
            var prompt=panel.Find("DoorPrompt");if(!prompt){var obj=new GameObject("DoorPrompt",typeof(RectTransform),typeof(Text));prompt=obj.transform;prompt.SetParent(panel,false);}
            var promptRect=(RectTransform)prompt;promptRect.anchorMin=promptRect.anchorMax=new Vector2(0,1);promptRect.pivot=new Vector2(0,1);promptRect.anchoredPosition=new Vector2(20,-controlsTop-88);promptRect.sizeDelta=new Vector2(300,28);
            var hint=prompt.GetComponent<Text>();hint.font=label.font;hint.fontSize=14;hint.color=new Color(.85f,.89f,.91f);hint.text="문 가까이에서 E 또는 버튼";hint.raycastTarget=false;
            var controller=GameObject.Find("DoorInteractionController");if(!controller)controller=new GameObject("DoorInteractionController");var interactor=controller.GetComponent<DoorInteractor>();if(!interactor)interactor=controller.AddComponent<DoorInteractor>();
            interactor.Configure(chair.transform,UnityEngine.Object.FindFirstObjectByType<SimulationController>(),button,label,hint);UnityEventTools.AddPersistentListener(button.onClick,interactor.ToggleNearest);EditorUtility.SetDirty(interactor);
            if(UnityEngine.Object.FindFirstObjectByType<DemoFreeCamera>())ConvergenceHallDemoCamera.ConfigureCurrentScene();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("DOOR_UI_SAVED: E and screen button; local wheelchair half width/depth="+x+"/"+z+"; oriented footprint enabled only in B1.");
        }
        [MenuItem("Tools/EEG Wheelchair/Enable B1 Interactive Doors")]
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{Build(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);ConfigureScene();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.FocusDoors",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
