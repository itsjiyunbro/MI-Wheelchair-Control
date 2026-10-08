using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // Photo-based B1 facade correction only; no elevator/door/vertical locomotion logic.
    public static class ConvergenceHallCoreHalls
    {
        const float Height=2.85f,Cut=Height;
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        static Material white,concrete,metal,dark,tile,yellow;
        static Transform lower,upper;
        static Transform Group(Transform p,string n)
        {var t=p.Find(n);if(!t){t=new GameObject(n).transform;t.SetParent(p,false);}return t;}
        static Material Existing(string n)=>AssetDatabase.LoadAssetAtPath<Material>(Mats+n+".mat");
        static Material NewMaterial(string n,Color color,float smooth=.15f)
        {var m=Existing(n);if(m)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=n};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,Mats+n+".mat");return m;}
        static Transform Box(Transform p,string n,Vector3 at,Vector3 size,Material m)
        {
            var t=p.Find(n);if(!t){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());t=g.transform;t.SetParent(p,false);}
            t.localPosition=at;t.localRotation=Quaternion.identity;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=m;t.gameObject.SetActive(true);return t;
        }
        static void Solid(string n,Vector3 at,Vector3 size,Material m,bool collision=false)
        {
            float lo=at.y-size.y/2,hi=at.y+size.y/2;
            if(lo<Cut){var a=at;var s=size;s.y=Mathf.Min(hi,Cut)-lo;a.y=lo+s.y/2;Box(lower,n,a,s,m);}
            if(hi>Cut){var a=at;var s=size;s.y=hi-Mathf.Max(lo,Cut);a.y=hi-s.y/2;Box(upper,n,a,s,m);}
            if(collision)
            {
                var g=Group(lower,n+"Collision");g.localPosition=Vector3.zero;var c=g.GetComponent<BoxCollider>();if(!c)c=g.gameObject.AddComponent<BoxCollider>();c.center=at;c.size=size;g.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");
            }
        }
        static void Label(string n,string text,Vector3 at,float w,float h,Quaternion rotation)
        {
            var p=at.y>Cut?upper:lower;var t=p.Find(n);
            if(!t){var g=new GameObject(n,typeof(RectTransform),typeof(Canvas),typeof(Text));t=g.transform;t.SetParent(p,false);g.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;}
            t.localPosition=at;t.localRotation=rotation;t.localScale=Vector3.one*.002f;((RectTransform)t).sizeDelta=new Vector2(w/.002f,h/.002f);
            var label=t.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=text;label.fontSize=32;label.resizeTextForBestFit=true;label.resizeTextMinSize=8;label.resizeTextMaxSize=32;label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
        }
        static void Sign(string n,string text,float x,float y,float z,float w=.48f)
        {Solid(n+"Backing",new Vector3(x,y,z),new Vector3(w,.20f,.018f),dark);Label(n,text,new Vector3(x,y,z-.012f),w-.025f,.17f,Quaternion.identity);}
        static void Tactile(string n,float x,float z,float w=.60f)
        {
            Solid(n,new Vector3(x,.012f,z),new Vector3(w,.014f,.32f),yellow);
            for(int i=0;i<8;i++)for(int j=0;j<4;j++)
                Solid(n+"Dot"+i+"_"+j,new Vector3(x-w/2+.045f+i*(w-.09f)/7,.022f,z-.12f+j*.08f),new Vector3(.023f,.006f,.023f),yellow);
        }
        static void PanelDoor(string n,float x,float z,float width,bool vent=false)
        {
            Solid(n+"Frame",new Vector3(x,1.12f,z),new Vector3(width+.08f,2.24f,.06f),metal);
            Solid(n+"Leaf",new Vector3(x,1.10f,z-.038f),new Vector3(width,2.18f,.032f),white);
            Solid(n+"Handle",new Vector3(x+width*.33f,.94f,z-.07f),new Vector3(.13f,.024f,.04f),metal);
            Sign(n+"Plate","SERVICE",x,1.5f,z-.065f,.25f);
            if(vent)for(int i=0;i<6;i++)Solid(n+"Vent"+i,new Vector3(x,.24f+i*.043f,z-.06f),new Vector3(width*.6f,.018f,.008f),metal);
        }
        static void SetupCore(GameObject root,string room,string side,string name,float planX,float planY,float yaw,float opening,Material wall)
        {
            var existing=root.transform.Find("RoomsAndCores/"+room+"/"+side);
            existing.GetComponent<BoxCollider>().enabled=false;
            foreach(Transform t in existing)t.gameObject.SetActive(false);
            var oldUpper=root.transform.Find("UpperWalls/"+room+"_"+side);if(oldUpper)oldUpper.gameObject.SetActive(false);
            lower=Group(Group(root.transform,"CoreHalls"),name);upper=Group(Group(root.transform,"UpperWalls"),name);
            lower.localPosition=ConvergenceHallB1Setup.Plan(planX,planY);lower.localRotation=Quaternion.Euler(0,yaw,0);
            upper.position=lower.position;upper.rotation=lower.rotation;
            float length=existing.GetComponent<BoxCollider>().size.z;
            // Same outer room footprint, replace only the corridor-facing wall with opening piers.
            float wing=(length-opening)/2;
            Solid("FrontLeftPier",new Vector3(-(opening+wing)/2,Height/2,.06f),new Vector3(wing,Height,.12f),wall,true);
            Solid("FrontRightPier",new Vector3((opening+wing)/2,Height/2,.06f),new Vector3(wing,Height,.12f),wall,true);
            Solid("Lintel",new Vector3(0,2.65f,.06f),new Vector3(opening,.40f,.12f),wall,true);
        }
        static void Lift(GameObject root,bool east)
        {
            string room=east?"EastLift":"WestLiftCore",side=east?"West":"East";
            // B112 hall runs almost to the rear of the 5.76m-deep core.
            // Photo/plan estimates: B109 has an open vestibule with the lift on its left wall.
            float w=east?2.30f:2.40f,d=east?4.80f:2.80f;
            var footprint=root.transform.Find("RoomsAndCores/"+room+"/RoomFloor").GetComponent<Renderer>().bounds;
            float rightBoundary=east?w/2:footprint.size.z/2-.12f;
            if(!east)d=footprint.size.x-.12f;
            Material wall=east?concrete:white;
            SetupCore(root,room,side,east?"B112ElevatorHall":"B109ElevatorHall",east?772:233,east?469.5f:413,east?90:-90,w,wall);
            Solid("HallFloor",new Vector3(0,.007f,d/2),new Vector3(w,.012f,d),east?Existing("LobbyStone"):Existing("CorridorTile"));
            Solid("LeftReturn",new Vector3(-w/2-.06f,Height/2,d/2),new Vector3(.12f,Height,d),wall,true);
            Solid("RightReturn",new Vector3(w/2+.06f,Height/2,d/2),new Vector3(.12f,Height,d),wall,true);
            Solid("RearWall",new Vector3(0,Height/2,d+.06f),new Vector3(w+.24f,Height,.12f),wall,true);
            if(!east)
            {
                // Keep the elevator's left plane; fill only the opposite side and rear to the core perimeter.
                Solid("HallFloor",new Vector3((rightBoundary-w/2)/2,.007f,d/2),new Vector3(rightBoundary+w/2,.012f,d),Existing("CorridorTile"));
                foreach(string n in new[]{"RightReturn","RearWall","FrontRightPier","Lintel"})
                {lower.Find(n).gameObject.SetActive(false);lower.Find(n+"Collision").gameObject.SetActive(false);}
            }
            var hallLower=lower;var hallUpper=upper;
            if(!east)
            {
                var assembly=Group(lower,"LeftWallElevator");var assemblyUp=Group(upper,"LeftWallElevator");
                // Reuse the previous front-facing details, with no duplicate doorway left behind.
                string[] names={"LiftFrame","SlidingLeaf0","SlidingLeaf1","CentreSeam","Header","FloorDisplay","FloorDisplayBacking","Threshold","CallPanel","CallButton","CallButtonBacking"};
                foreach(var t in lower.Cast<Transform>().Where(t=>names.Contains(t.name)||t.name.StartsWith("TactilePad")).ToArray())t.SetParent(assembly,false);
                assembly.localPosition=new Vector3(-w/2,0,d/2);assembly.localRotation=Quaternion.Euler(0,-90,0);
                assemblyUp.localPosition=assembly.localPosition;assemblyUp.localRotation=assembly.localRotation;
                lower=assembly;upper=assemblyUp;
            }
            float doorX=0,doorZ=east?d:0;
            Solid("LiftFrame",new Vector3(doorX,1.20f,doorZ-.045f),new Vector3(1.40f,2.40f,.10f),metal);
            for(int i=0;i<2;i++)Solid("SlidingLeaf"+i,new Vector3(doorX+(i==0?-.315f:.315f),1.03f,doorZ-.11f),new Vector3(.62f,2.06f,.025f),metal);
            Solid("CentreSeam",new Vector3(doorX,1.03f,doorZ-.128f),new Vector3(.012f,2.06f,.01f),dark);
            Solid("Header",new Vector3(doorX,2.24f,doorZ-.103f),new Vector3(1.29f,.27f,.035f),metal);
            Sign("FloorDisplay","B1",doorX,2.26f,doorZ-.128f,.43f);
            Solid("Threshold",new Vector3(doorX,.014f,doorZ-.19f),new Vector3(1.32f,.02f,.17f),metal);
            Solid("CallPanel",new Vector3(doorX+.86f,.96f,doorZ-.04f),new Vector3(.13f,.28f,.035f),metal);
            Sign("CallButton","^",doorX+.86f,1.01f,doorZ-.065f,.085f);
            Tactile("TactilePad",doorX+.78f,doorZ-.42f,.54f);
            ConvergenceHallElevatorDetails.ApplyTo(lower,doorZ);
            lower=hallLower;upper=hallUpper;
            Sign("HallSign","ELEVATOR",-w/2-.28f,1.45f,-.017f,.52f);
            // Side wall service cabinets, kept within the existing hall footprint.
            var beforeL=lower;var beforeU=upper;
            var service=Group(lower,"ServicePanels");var serviceUp=Group(upper,"ServicePanels");
            service.localPosition=new Vector3(east?-w/2+.02f:rightBoundary-.02f,0,d*.52f);service.localRotation=Quaternion.Euler(0,east?-90:90,0);
            serviceUp.localPosition=service.localPosition;serviceUp.localRotation=service.localRotation;
            lower=service;upper=serviceUp;PanelDoor("InspectionA",-.32f,0,.49f);PanelDoor("InspectionB",.32f,0,.49f);lower=beforeL;upper=beforeU;
            if(east)
            {
                var board=Group(lower,"NoticeBoard");var boardUp=Group(upper,"NoticeBoard");board.localPosition=new Vector3(w/2-.025f,0,d-.95f);board.localRotation=Quaternion.Euler(0,90,0);boardUp.localPosition=board.localPosition;boardUp.localRotation=board.localRotation;
                lower=board;upper=boardUp;Solid("Backing",new Vector3(0,1.49f,0),new Vector3(1.0f,.72f,.024f),dark);
                for(int i=0;i<6;i++)Solid("Notice"+i,new Vector3(-.32f+(i%3)*.32f,1.67f-(i/3)*.34f,-.018f),new Vector3(.26f,.28f,.008f),i%2==0?white:metal);
                lower=beforeL;upper=beforeU;
                // Keep the original corridor-to-lift depth and translate toward the adjacent stairs.
                // Plan down is world -Z. Align the south side with the core perimeter's inner face.
                float offset=footprint.min.z+.12f-hallLower.Find("RightReturn").GetComponent<Renderer>().bounds.min.z;
                hallLower.position+=Vector3.forward*offset;hallUpper.position=hallLower.position;
                // Keep the front facade inside the original core after moving its opening.
                float leftWing=(footprint.size.z-w)/2-offset,rightWing=(footprint.size.z-w)/2+offset;
                Solid("FrontLeftPier",new Vector3(-(w+leftWing)/2,Height/2,.06f),new Vector3(leftWing,Height,.12f),wall,true);
                Solid("FrontRightPier",new Vector3((w+rightWing)/2,Height/2,.06f),new Vector3(rightWing,Height,.12f),wall,true);
            }
        }
        static void Stair(GameObject root,bool east)
        {
            float width=east?2.10f:1.80f;
            SetupCore(root,east?"EastStair":"WestStair",east?"West":"East",east?"B112Stairwell":"B109Stairwell",east?772:233,east?525.5f:358.5f,east?90:-90,width,east?concrete:white);
            Solid("Landing",new Vector3(0,.007f,2.0f),new Vector3(width,.012f,4.0f),tile);
            Solid("LeftInterior",new Vector3(-width/2-.06f,Height/2,2.0f),new Vector3(.12f,Height,4.0f),white,true);
            Solid("RightInterior",new Vector3(width/2+.06f,Height/2,2.0f),new Vector3(.12f,Height,4.0f),white,true);
            Solid("RearInterior",new Vector3(0,Height/2,4.06f),new Vector3(width+.24f,Height,.12f),white,true);
            Solid("DoorFrameLeft",new Vector3(-width/2+.025f,1.13f,-.02f),new Vector3(.05f,2.26f,.065f),metal);
            Solid("DoorFrameRight",new Vector3(width/2-.025f,1.13f,-.02f),new Vector3(.05f,2.26f,.065f),metal);
            Solid("DoorFrameTop",new Vector3(0,2.24f,-.02f),new Vector3(width,.05f,.065f),metal);
            if(east)
            {
                // Photo: wide fire-door opening with the right leaf closed, left side open.
                Solid("ClosedRightLeaf",new Vector3(width/4,1.10f,-.025f),new Vector3(width/2-.05f,2.20f,.045f),white,true);
                Sign("DoorSign","STAIR",width/4,1.43f,-.056f,.30f);
                Solid("DoorKnob",new Vector3(width/2-.17f,.91f,-.069f),new Vector3(.045f,.045f,.055f),metal);
            }
            else Sign("StairSign","STAIR",-width/2-.30f,1.44f,-.018f,.48f);
            if(east)
            {
                foreach(string n in new[]{"ExitMarker","ExitMarkerBacking"}){var old=lower.Find(n);if(old)old.gameObject.SetActive(false);}
                // The previous lintel began at 2.45m while this door frame ends at 2.265m.
                // Bring it down behind the frame with a concealed 20mm overlap.
                const float bottom=2.245f;
                Solid("Lintel",new Vector3(0,(bottom+Height)/2,.06f),new Vector3(width,Height-bottom,.12f),concrete,true);
                Solid("ClosedLeafTopSeal",new Vector3(width/4,2.209f,-.011f),new Vector3(width/2-.05f,.026f,.04f),dark);
            }
            else Sign("ExitMarker","EXIT",0,2.51f,-.016f,.44f);
            {
                // Use the original stair core perimeter instead of a small room nested inside it.
                var bounds=root.transform.Find("RoomsAndCores/"+(east?"EastStair":"WestStair")+"/RoomFloor").GetComponent<Renderer>().bounds;
                float clearWidth=bounds.size.z-.24f;
                float clearDepth=bounds.size.x-.12f;
                foreach(string n in new[]{"LeftInterior","RightInterior","RearInterior"})
                {lower.Find(n).gameObject.SetActive(false);lower.Find(n+"Collision").gameObject.SetActive(false);}
                Solid("Landing",new Vector3(0,.007f,clearDepth/2),new Vector3(clearWidth,.012f,clearDepth),tile);
                if(east)
                {
                    foreach(var t in lower.Cast<Transform>().Where(t=>t.name.StartsWith("InnerService")).ToArray())t.gameObject.SetActive(false);
                    var hallLower=lower;var hallUpper=upper;
                    var service=Group(lower,"SideServicePanel");var serviceUpper=Group(upper,"SideServicePanel");
                    service.localPosition=new Vector3(-clearWidth/2+.02f,0,.42f);service.localRotation=Quaternion.Euler(0,-90,0);
                    serviceUpper.localPosition=service.localPosition;serviceUpper.localRotation=service.localRotation;
                    lower=service;upper=serviceUpper;PanelDoor("Inspection",0,0,.76f,true);lower=hallLower;upper=hallUpper;
                }
                BuildSwitchback(clearWidth,clearDepth,east);
                ConvergenceHallStairStorage.Build(root,lower,east);
                return;
            }
        }
        static void BuildSwitchback(float width,float depth,bool east)
        {
            // Replace only the previously authored single flight. Keep the doorway and core shell.
            foreach(var t in lower.Cast<Transform>().Where(t=>t.name.StartsWith("Step")||t.name.StartsWith("Handrail")||t.name.StartsWith("StairTactile")).ToArray())t.gameObject.SetActive(false);
            var g=Group(lower,"SwitchbackStairs");
            const int count=10;
            const float front=.74f,landingDepth=.90f,mid=Height/2;
            float run=(depth-landingDepth-front)/count,first=front+run/2;
            float flight=(width-.10f)/2,centre=(flight+.10f)/2;
            float turn=depth-landingDepth,backRail=depth-.05f;
            float rise=mid/count;
            Transform Part(string n,Vector3 p,Vector3 s,Material m)=>Box(g,n,p,s,m);
            void Beam(string n,Vector3 a,Vector3 b,float thickness=.035f)
            {var d=b-a;var t=Part(n,(a+b)/2,new Vector3(thickness,d.magnitude,thickness),dark);t.localRotation=Quaternion.FromToRotation(Vector3.up,d.normalized);}
            for(int i=0;i<count;i++)
            {
                float h=(i+1)*rise,z=first+i*run;
                Part("RightLowerStep"+i,new Vector3(centre,h/2,z),new Vector3(flight,h,run),tile);
                Part("RightNosing"+i,new Vector3(centre,h+.004f,z-run/2+.008f),new Vector3(flight,.008f,.015f),metal);
                float upper=mid+(i+1)*rise,back=first+(count-1-i)*run;
                Part("LeftReturnStep"+i,new Vector3(-centre,upper-rise/2,back),new Vector3(flight,rise,run),tile);
                Part("LeftNosing"+i,new Vector3(-centre,upper+.004f,back+run/2-.008f),new Vector3(flight,.008f,.015f),metal);
            }
            Part("MidLanding",new Vector3(0,mid-.08f,depth-landingDepth/2),new Vector3(width,.16f,landingDepth),tile);
            Part("UpperLanding",new Vector3(-centre,Height-.08f,.50f),new Vector3(flight,.16f,.48f),tile);
            // Thin sloping slab supports the returning flight, leaving space beneath it.
            Vector3 a=new Vector3(-centre,mid-.06f,turn),b=new Vector3(-centre,Height-.06f,front);
            var slab=Part("ReturnSlab",(a+b)/2,new Vector3(flight,.16f,(b-a).magnitude),tile);slab.localRotation=Quaternion.LookRotation(b-a);
            foreach(int side in new[]{-1,1})
            {
                float rightX=centre+side*(flight/2-.04f),leftX=-centre+side*(flight/2-.04f);
                Beam("RightRail"+side,new Vector3(rightX,rise+.85f,first-.07f),new Vector3(rightX,mid+.85f,turn));
                Beam("LeftRail"+side,new Vector3(leftX,mid+rise+.85f,turn-.04f),new Vector3(leftX,Height+.85f,front));
                for(int j=0;j<3;j++)
                {
                    int i=j*(count-1)/2;float z=first+i*run,h=(i+1)*rise;
                    Beam("RightPost"+side+"_"+j,new Vector3(rightX,h,z),new Vector3(rightX,h+.85f,z));
                    float rz=first+(count-1-i)*run,rh=mid+(i+1)*rise;
                    Beam("LeftPost"+side+"_"+j,new Vector3(leftX,rh,rz),new Vector3(leftX,rh+.85f,rz));
                }
            }
            Beam("LandingBackRail",new Vector3(-width/2+.04f,mid+.85f,backRail),new Vector3(width/2-.04f,mid+.85f,backRail));
            for(int i=0;i<3;i++)
            {float x=(i-1)*(width/2-.04f);Beam("LandingPostWide"+i,new Vector3(x,mid,backRail),new Vector3(x,mid+.85f,backRail));}
            foreach(var t in g.Cast<Transform>().Where(t=>t.name.StartsWith("LandingPost")&&!t.name.StartsWith("LandingPostWide")))t.gameObject.SetActive(false);
            // The simulator remains a B1 driving scene; stairs are visual and ascent stays blocked.
            var gate=Group(lower,"StairFlightBlocker");var c=gate.GetComponent<BoxCollider>();if(!c)c=gate.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,Height/2,.70f);c.size=new Vector3(width,Height,.08f);gate.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");
            Tactile("StairTactile",east?-.50f:Mathf.Min(centre,.53f),.36f,.68f);
        }
        static void Rail(string name,Vector3 a,Vector3 b)
        {
            var split=Vector3.Lerp(a,b,Mathf.Clamp01((Cut-a.y)/(b.y-a.y)));
            void Beam(Transform parent,Vector3 from,Vector3 to)
            {var d=to-from;if(d.magnitude<.001f)return;var t=Box(parent,name,(from+to)/2,new Vector3(.04f,d.magnitude,.04f),dark);t.localRotation=Quaternion.FromToRotation(Vector3.up,d.normalized);}
            Beam(lower,a,split);Beam(upper,split,b);
        }
        public static void ApplyTo(GameObject root)
        {
            white=Existing("WarmWhite");metal=Existing("Metal");dark=Existing("Navy");
            concrete=NewMaterial("CoreConcrete",new Color(.66f,.65f,.61f));tile=NewMaterial("StairTile",new Color(.30f,.285f,.25f));yellow=NewMaterial("TactileYellow",new Color(.76f,.55f,.14f));
            foreach(string n in new[]{"EAST LIFT","WEST LIFT","STAIR"}){var t=root.transform.Find("DoorsAndSigns/"+n);if(t)t.gameObject.SetActive(false);}
            foreach(string n in new[]{"EastLift","WestLiftCore","EastStair","WestStair"})root.transform.Find("RoomsAndCores/"+n+"/PlanLabel").gameObject.SetActive(false);
            Lift(root,true);Lift(root,false);Stair(root,true);Stair(root,false);
            ConvergenceHallCoreSigns.ApplyTo(root);
            ConvergenceHallElevatorCabins.ApplyTo(root);
            ConvergenceHallFinalFinishes.RecessElevators(root);
            ConvergenceHallB112LiftWallFinish.ApplyTo(root);
            ConvergenceHallB112ServiceDoors.ApplyTo(root);
            ConvergenceHallWestLiftPhoto.ApplyTo(root);
        }
        [MenuItem("Tools/EEG Wheelchair/Update Convergence Elevator And Stair Halls")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ConvergenceHallWallMaterials.ApplyTo(root);Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();var scene=EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);EditorSceneManager.SaveScene(scene);EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
        }
        static void Validate(GameObject root)
        {
            if(root.transform.Find("CoreHalls").childCount!=4)throw new Exception("Expected four corrected facades.");
            if(root.transform.Find("UpperWalls"))throw new Exception("Legacy upper split remains.");
            foreach(Transform hall in root.transform.Find("CoreHalls"))
            {
                var stairs=hall.Find("SwitchbackStairs");
                var all=hall.GetComponentsInChildren<Renderer>();if(all.Any(r=>r.bounds.min.y<-.006f||r.bounds.max.y>(stairs&&r.transform.IsChildOf(stairs)?Height+.90f:Cut+.025f)))throw new Exception("Floor/wall/stair height mismatch "+hall.name);
                
            }
            var lift=root.transform.Find("CoreHalls/B112ElevatorHall");
            if(Mathf.Abs(lift.Find("HallFloor").localScale.z-4.8f)>.001f||lift.Find("RearWall").gameObject.activeSelf)throw new Exception("B112 hall floor/old rear partition mismatch.");
            var core=root.transform.Find("RoomsAndCores/EastLift/RoomFloor").GetComponent<Renderer>().bounds;
            var rear=lift.Find("MeasuredCabin/RearWall").GetComponent<Renderer>().bounds;
            if(rear.max.x>core.max.x+.001f||Mathf.Abs(lift.position.x-ConvergenceHallB1Setup.Plan(772,469.5f).x)>.001f)throw new Exception("B112 original lift approach depth/entrance plane changed.");
            var stairSide=lift.Find("RightReturn").GetComponent<Renderer>().bounds;
            if(Mathf.Abs(stairSide.min.z-(core.min.z+.12f))>.002f||Mathf.Abs(lift.Find("HallFloor").localScale.x-2.30f)>.001f)throw new Exception("B112 hall must meet the stair-side perimeter without resizing.");
            foreach(string name in new[]{"FrontLeftPier","FrontRightPier"}){var pier=lift.Find(name).GetComponent<Renderer>().bounds;if(pier.min.z<core.min.z-.002f||pier.max.z>core.max.z+.002f)throw new Exception("Shifted lift front pier overlaps adjacent rooms.");}
            Debug.Log("B112_LIFT_STAIR_SIDE_OK: original 2.30m x 4.80m hall and corridor entrance plane retained; translated plan-down toward adjacent stairwell.");
            Physics.SyncTransforms();
            ValidateEastSwitchback(root);
            var switchback=root.transform.Find("CoreHalls/B109Stairwell/SwitchbackStairs");
            if(!switchback)throw new Exception("B109 switchback missing.");
            var stairBounds=root.transform.Find("RoomsAndCores/WestStair/RoomFloor").GetComponent<Renderer>().bounds;
            float expectedFlight=(stairBounds.size.z-.24f-.10f)/2;
            for(int i=0;i<10;i++)
            {
                var right=switchback.Find("RightLowerStep"+i);var left=switchback.Find("LeftReturnStep"+i);
                if(right.localPosition.x<=0||left.localPosition.x>=0||Mathf.Abs(right.localScale.x-expectedFlight)>.002f||Mathf.Abs(left.localScale.x-expectedFlight)>.002f)throw new Exception("Flights must fill opposite halves of the core.");
                if(i>0&&(right.localPosition.z<=switchback.Find("RightLowerStep"+(i-1)).localPosition.z||left.localPosition.z>=switchback.Find("LeftReturnStep"+(i-1)).localPosition.z))throw new Exception("Flights must climb in opposite directions.");
            }
            var landingBounds=switchback.Find("MidLanding").GetComponent<Renderer>().bounds;
            if(Mathf.Abs(landingBounds.min.x-(stairBounds.min.x+.12f))>.002f||Mathf.Abs(landingBounds.min.z-(stairBounds.min.z+.12f))>.002f||Mathf.Abs(landingBounds.max.z-(stairBounds.max.z-.12f))>.002f)throw new Exception("Landing must meet the original perimeter walls.");
            foreach(string n in new[]{"LeftInterior","RightInterior","RearInterior"})if(switchback.parent.Find(n).gameObject.activeSelf||switchback.parent.Find(n+"Collision").gameObject.activeSelf)throw new Exception("Inset stair shell remains.");
            if(Mathf.Abs(switchback.Find("MidLanding").GetComponent<Renderer>().bounds.max.y-Height/2)>.002f||Mathf.Abs(switchback.Find("LeftReturnStep9").GetComponent<Renderer>().bounds.max.y-Height)>.002f)throw new Exception("Stair landing levels mismatch.");
            var west=root.transform.Find("CoreHalls/B109ElevatorHall");var sideLift=west.Find("LeftWallElevator");
            if(!sideLift||west.Find("LiftFrame")||sideLift.localPosition.x>=0||Vector3.Dot(-sideLift.forward,west.right)<.999f)throw new Exception("B109 lift must face into the hall from its left wall.");
            var vestibuleRay=new Ray(west.TransformPoint(new Vector3(0,1,-.3f)),west.forward);
            foreach(var c in west.GetComponentsInChildren<Collider>())if(c.Raycast(vestibuleRay,out var blocked,2.7f))throw new Exception("B109 open vestibule blocked by "+c.name);
            var westCore=root.transform.Find("RoomsAndCores/WestLiftCore/RoomFloor").GetComponent<Renderer>().bounds;
            var westFloor=west.Find("HallFloor").GetComponent<Renderer>().bounds;
            if(Mathf.Abs(westFloor.min.x-(westCore.min.x+.12f))>.002f||Mathf.Abs(westFloor.max.z-(westCore.max.z-.12f))>.002f)throw new Exception("B109 hall must reach side/rear perimeter walls.");
            if(Mathf.Abs(sideLift.localPosition.z-(westCore.size.x-.12f)/2)>.001f)throw new Exception("B109 elevator is not centered on the left hall wall.");
            foreach(string n in new[]{"RightReturn","RearWall","FrontRightPier","Lintel"})if(west.Find(n).gameObject.activeSelf||west.Find(n+"Collision").gameObject.activeSelf)throw new Exception("B109 inset shell or entrance projection remains.");
            var fullEntryRay=new Ray(west.TransformPoint(new Vector3(1.65f,1,-.3f)),west.forward);
            foreach(var c in west.GetComponentsInChildren<Collider>())if(c.Raycast(fullEntryRay,out var entryHit,.8f))throw new Exception("B109 expanded entrance blocked by "+c.name);
            foreach(float x in new[]{0f,1.5f})
            {
                var clearRay=new Ray(west.TransformPoint(new Vector3(x,1,.2f)),west.forward);
                foreach(var c in west.GetComponentsInChildren<Collider>())if(c.Raycast(clearRay,out var obstacle,5.4f))throw new Exception("Expanded B109 hall blocked by "+c.name);
            }
            foreach(float x in new[]{-.45f,0,.45f})
            {
                var ray=new Ray(lift.TransformPoint(new Vector3(x,1,.25f)),lift.forward);
                foreach(var c in lift.GetComponentsInChildren<Collider>())
                    if(c.Raycast(ray,out var hit,lift.Find("MeasuredCabin").localPosition.z-.43f))throw new Exception("Elevator approach blocked before the door by "+c.name);
            }
            ConvergenceHallElevatorCabins.Validate(root);
            Debug.Log("CORE_HALLS_OK: original hall boundaries retained, measured cabin partitions and clear approach rays passed; four facades, 2.85m walls.");
        }
        static void ValidateEastSwitchback(GameObject root)
        {
            var hall=root.transform.Find("CoreHalls/B112Stairwell");var stairs=hall.Find("SwitchbackStairs");
            if(!stairs)throw new Exception("B112 switchback missing.");
            var core=root.transform.Find("RoomsAndCores/EastStair/RoomFloor").GetComponent<Renderer>().bounds;
            var floor=hall.Find("Landing").GetComponent<Renderer>().bounds;
            var turn=stairs.Find("MidLanding").GetComponent<Renderer>().bounds;
            float flight=(core.size.z-.24f-.10f)/2;
            if(Mathf.Abs(floor.max.x-(core.max.x-.12f))>.002f||Mathf.Abs(floor.min.z-(core.min.z+.12f))>.002f||Mathf.Abs(floor.max.z-(core.max.z-.12f))>.002f||Mathf.Abs(turn.max.x-(core.max.x-.12f))>.002f||Mathf.Abs(turn.min.z-floor.min.z)>.002f||Mathf.Abs(turn.max.z-floor.max.z)>.002f)throw new Exception("B112 stairs must fill the side and exterior rear wall boundaries.");
            for(int i=0;i<10;i++)
            {
                var right=stairs.Find("RightLowerStep"+i);var left=stairs.Find("LeftReturnStep"+i);
                if(right.localPosition.x<=0||left.localPosition.x>=0||Mathf.Abs(right.localScale.x-flight)>.002f||Mathf.Abs(left.localScale.x-flight)>.002f)throw new Exception("B112 stair flights do not fill the two halves.");
                if(i>0&&(right.localPosition.z<=stairs.Find("RightLowerStep"+(i-1)).localPosition.z||left.localPosition.z>=stairs.Find("LeftReturnStep"+(i-1)).localPosition.z||left.GetComponent<Renderer>().bounds.max.y<=stairs.Find("LeftReturnStep"+(i-1)).GetComponent<Renderer>().bounds.max.y))throw new Exception("B112 return flight direction/height mismatch.");
            }
            foreach(string n in new[]{"LeftInterior","RightInterior","RearInterior"})if(hall.Find(n).gameObject.activeSelf||hall.Find(n+"Collision").gameObject.activeSelf)throw new Exception("B112 inset shell remains.");
            if(hall.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Step")||t.name.StartsWith("Handrail")))throw new Exception("B112 old single flight remains visible.");
            if(Mathf.Abs(turn.max.y-Height/2)>.002f||Mathf.Abs(stairs.Find("LeftReturnStep9").GetComponent<Renderer>().bounds.max.y-Height)>.002f)throw new Exception("B112 stair landing height mismatch.");
            var blocker=hall.Find("StairFlightBlocker").GetComponent<BoxCollider>();
            if(!blocker.enabled||Mathf.Abs(blocker.size.x-(core.size.z-.24f))>.002f)throw new Exception("B112 stair collision gate lost.");
            Debug.Log("B112_SWITCHBACK_OK: side/rear perimeter filled; two opposite flights and midpoint landing; old inset shell/single flight hidden; entrance and driving guard preserved.");
        }
        public static void ApplyAndValidate()
        {
            Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Duplicate hall geometry.");
            ConvergenceHallB1Validation.BuildAndValidate();
        }
        public static void ApplyAndCapture()
        {Apply();SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();}
    }
}



