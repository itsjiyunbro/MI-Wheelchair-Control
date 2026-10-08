using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // B1 BLOCKOUT, not a surveyed architectural model. All plan coordinates refer to
    // the first supplied B1 diagram (1280x960). West corridor: 25 image units = 1.8 m.
    // Phone measurement is approximate; room dimensions and floor height are estimates.
    public static class ConvergenceHallB1Setup
    {
        public const string ScenePath = "Assets/Scenes/ConvergenceHallB1Scene.unity";
        public const string PrefabPath = "Assets/Art/Prefabs/Environment/ConvergenceHall/ConvergenceHallB1.prefab";
        const string MatFolder = "Assets/Art/Materials/ConvergenceHall";
        public const float MetresPerPlanUnit = 1.8f / 25f;
        static Material floor, tile, wall, glass, navy, wood, metal, ink;
        static Transform rooms, details;
        static int obstacle;
        public static Vector3 Plan(float x, float y, float height = 0) => new Vector3((x-705)*MetresPerPlanUnit,height,(878-y)*MetresPerPlanUnit);

        [MenuItem("Tools/EEG Wheelchair/Create Convergence Hall B1 Scene")]
        public static void Create()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); Debug.Log("CONVERGENCE_EXISTS: existing scene preserved, no duplicates."); return; }
            obstacle=LayerMask.NameToLayer("WheelchairObstacle");
            if(obstacle<0) throw new InvalidOperationException("Existing WheelchairObstacle layer required.");
            Folder(MatFolder); Folder(Path.GetDirectoryName(PrefabPath).Replace('\\','/'));
            floor=Mat("LobbyStone",new Color(.55f,.57f,.58f),.28f);
            tile=Mat("CorridorTile",new Color(.38f,.41f,.42f),.22f);
            wall=Mat("WarmWhite",new Color(.83f,.84f,.81f),.15f);
            glass=Mat("FrostedTeal",new Color(.29f,.49f,.49f),.3f);
            navy=Mat("Navy",new Color(.035f,.075f,.15f),.2f);
            wood=Mat("StandOak",new Color(.48f,.29f,.13f),.18f);
            metal=Mat("Metal",new Color(.48f,.52f,.54f),.35f,.5f);
            ink=Mat("FloorJoint",new Color(.24f,.29f,.30f),.1f);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if(asset==null)
            {
                if(File.Exists(PrefabPath)) throw new IOException("Conflicting prefab; preserved.");
                var root=Build();
                try { asset=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath); }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var source=EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            if(!EditorSceneManager.SaveScene(source,ScenePath,true)) throw new IOException("Scene copy failed.");
            var scene=EditorSceneManager.OpenScene(ScenePath);
            foreach(var go in scene.GetRootGameObjects().Where(g=>g.name=="IndoorTestCourse" || g.name=="RehabJunctionCourse"))
                UnityEngine.Object.DestroyImmediate(go); // Only copied environment instances in NEW scene.
            PrefabUtility.InstantiatePrefab(asset,scene);
            var movement=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            movement.transform.SetPositionAndRotation(Plan(745,830,.5f),Quaternion.identity);
            var visual=movement.transform.Find("WheelchairVisual");
            // School-only prefab instance override. Original prefab and other scenes retain size.
            visual.localScale*=.50f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
            var guard=new SerializedObject(movement.GetComponent<WheelchairCollisionGuard>());
            guard.FindProperty("clearanceRadius").floatValue=.58f;
            guard.ApplyModifiedPropertiesWithoutUndo();
            var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();
            sim.SetControlSource(WheelchairControlSource.Keyboard);
            var ground=scene.GetRootGameObjects().Single(g=>g.name=="Ground");
            // Base under the larger measured-plan footprint, only in the new scene.
            ground.transform.position=Plan(630,606,-.025f);
            ground.transform.localScale=new Vector3(7.3f,1,4.4f);
            var camera=UnityEngine.Object.FindFirstObjectByType<CameraFollow>(); camera.SnapToTarget();
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
            Debug.Log("CONVERGENCE_CREATED: B1 blockout, 1.8m west corridor, school-only 50% visual / .58m guard; runtime code and original scenes untouched.");
        }
        static void Folder(string p)
        {
            if(AssetDatabase.IsValidFolder(p))return;
            Folder(Path.GetDirectoryName(p).Replace('\\','/')); AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\','/'),Path.GetFileName(p));
        }
        static Material Mat(string name,Color color,float smooth,float metallic=0)
        {
            string path=MatFolder+"/"+name+".mat"; var existing=AssetDatabase.LoadAssetAtPath<Material>(path); if(existing!=null)return existing;
            if(File.Exists(path))throw new IOException("Conflicting material: "+path);
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);AssetDatabase.CreateAsset(m,path);return m;
        }
        static Transform Group(Transform parent,string name)
        { var t=new GameObject(name).transform;t.SetParent(parent,false);return t; }
        static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material m,bool collider=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=m;
            if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());else go.layer=obstacle;
            return go.transform;
        }
        static void Rect(Transform parent,string name,float x0,float y0,float x1,float y1,float h,float thick,Material m)
        { Box(parent,name,Plan((x0+x1)/2,(y0+y1)/2,h),new Vector3((x1-x0)*MetresPerPlanUnit,thick,(y1-y0)*MetresPerPlanUnit),m); }
        // Wall coordinates are WALKABLE FACE boundaries. Thickness extends inside rooms,
        // so the 25-unit measured corridor remains exactly 1.8m clear, not 1.6m.
        static void Edge(Transform parent,string name,Vector3 a,Vector3 b,Vector3 inward,Material material)
        {
            var group=Group(parent,name);var d=b-a; float length=d.magnitude;
            group.localPosition=(a+b)/2+inward*.06f;group.localRotation=Quaternion.LookRotation(d.normalized,Vector3.up);
            Box(group,"LowerWall",new Vector3(0,.53f,0),new Vector3(.12f,1.06f,length),material);
            Box(group,"TopCap",new Vector3(0,1.075f,0),new Vector3(.13f,.03f,length),metal);
            Box(group,"Skirting",new Vector3(0,.055f,0),new Vector3(.132f,.11f,length),navy);
            var hit=group.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,1.4f,0);hit.size=new Vector3(.12f,2.8f,length);group.gameObject.layer=obstacle;
            if(material==glass)
                for(float z=-length/2+.9f;z<length/2;z+=.9f)Box(group,"Mullion",new Vector3(0,.53f,z),new Vector3(.135f,1.06f,.025f),metal);
        }
        static void Room(string name,float x0,float y0,float x1,float y1,bool glazed=true)
        {
            var t=Group(rooms,name); Rect(t,"RoomFloor",x0,y0,x1,y1,.003f,.006f,wall);
            var m=glazed?glass:wall;
            Edge(t,"West",Plan(x0,y0),Plan(x0,y1),Vector3.right,m);
            Edge(t,"East",Plan(x1,y0),Plan(x1,y1),Vector3.left,m);
            Edge(t,"North",Plan(x0,y0),Plan(x1,y0),Vector3.back,m);
            Edge(t,"South",Plan(x0,y1),Plan(x1,y1),Vector3.forward,m);
            Label(t,"PlanLabel",name,Plan((x0+x1)/2,(y0+y1)/2,.025f),Quaternion.Euler(90,0,0),Mathf.Min(3,(x1-x0)*MetresPerPlanUnit*.85f),.45f);
        }
        static void Label(Transform parent,string name,string value,Vector3 p,Quaternion rotation,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(Text));var r=(RectTransform)go.transform;
            r.SetParent(parent,false);r.localPosition=p;r.localRotation=rotation;r.localScale=Vector3.one*.005f;r.sizeDelta=new Vector2(width/.005f,height/.005f);
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;var text=go.GetComponent<Text>();
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=60;text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=60;
            text.text=value;text.color=new Color(.055f,.10f,.17f);text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        }
        static void Door(string name,float x,float y,float yaw,bool elevator=false)
        {
            var t=Group(details,name);t.localPosition=Plan(x,y);t.localRotation=Quaternion.Euler(0,yaw,0);
            float w=elevator?1.15f:.88f;
            Box(t,"ClosedDoor",new Vector3(0,.52f,0),new Vector3(w,1.04f,.045f),elevator?metal:wall);
            Box(t,"FrameLeft",new Vector3(-w/2-.025f,.55f,0),new Vector3(.05f,1.1f,.065f),metal);
            Box(t,"FrameRight",new Vector3(w/2+.025f,.55f,0),new Vector3(.05f,1.1f,.065f),metal);
            if(elevator)Box(t,"CentreSeam",new Vector3(0,.52f,-.025f),new Vector3(.012f,1.02f,.006f),navy);
            else Box(t,"Handle",new Vector3(w*.32f,.63f,-.065f),new Vector3(.14f,.025f,.065f),metal);
            Label(t,"Number",name,new Vector3(0,1.27f,0),Quaternion.identity,elevator?1.8f:1,.23f);
        }
        static GameObject Build()
        {
            var root=new GameObject("ConvergenceHallB1");var floors=Group(root.transform,"Floors");
            Rect(floors,"B1Footprint",150,334,1110,878,-.04f,.08f,floor);
            Rect(floors,"WestMeasuredCorridor_1p8m",233,334,258,878,.002f,.004f,tile);
            Rect(floors,"EntryLeftCorridor",258,733,723,766,.002f,.004f,tile);
            Rect(floors,"WestNorthAlcove",233,334,374,367,.002f,.004f,tile);
            Rect(floors,"EastCorridor",852,406,884,766,.002f,.004f,tile);
            // Sparse joints suggest tiles; dimensions of actual tiles remain unmeasured.
            for(float y=346;y<878;y+=12.5f)Rect(floors,"WestTileJoint",233,y,258,y+.07f,.005f,.001f,ink);
            for(float x=264;x<723;x+=12.5f)Rect(floors,"EntryTileJoint",x,733,x+.07f,766,.005f,.001f,ink);
            rooms=Group(root.transform,"RoomsAndCores");details=Group(root.transform,"DoorsAndSigns");
            Room("B101",501,766,632,878);Room("B102",374,766,501,878);Room("B103",258,766,374,878);
            Room("B104",150,662,233,878);Room("B105",150,549,233,662);
            Room("WestWC",150,443,233,549,false);Room("WestLiftCore",150,383,233,443,false);Room("WestStair",150,334,233,383,false);
            Room("B108",258,548,374,733);Room("B109",258,367,374,548);
            Room("B107",374,625,480,733);Room("B106",480,625,632,733);
            Room("SunkenVoid",374,438,632,625,false);
            Room("B110",374,334,501,438);Room("B111",501,334,640,438);Room("B112",640,367,710,594,false);
            Room("NorthWC",742,334,884,406,false);
            Room("B113",884,334,1054,441.66667f);Room("B114",884,441.66667f,1054,549.33333f);Room("B115",884,549.33333f,1054,657);
            Room("EastService",1054,334,1110,549.33333f,false);Room("EastService2",1054,549.33333f,1110,657,false);
            for(int i=0;i<7;i++)Room("B"+(116+i),884+i*(226f/7),657,884+(i+1)*(226f/7),723);
            Room("B124",772,766,1043,878);Room("B124Wing",884,723,1110,766);Room("B123",1043,766,1110,878);
            Room("EastLift",772,438,852,501,false);Room("EastStair",772,501,852,550,false);
            Room("Lounge",772,550,852,716);
            // Closed decorative doors are backed by the existing room wall collider.
            Door("B101",545,765.6f,180);Door("B102",430,765.6f,180);Door("B103",306,765.6f,180);
            Door("B106",560,733.4f,0);Door("B107",421,733.4f,0);
            Door("B108",257.6f,630,90);Door("B109",257.6f,454,90);
            Door("B104",233.4f,792,-90);Door("B105",233.4f,610,-90);
            Door("WEST LIFT",233.4f,423,-90,true);Door("B110",373.6f,352,90);
            Door("B112",710.4f,560,-90);Door("EAST LIFT",771.6f,470,90,true);
            Door("STAIR",771.6f,526,90);Door("B113",883.6f,419,90);
            Door("B114",883.6f,493,90);Door("B115",883.6f,599,90);
            // Wood stand silhouette: no stair traversal or upper-floor implementation in B1 stage.
            var stand=Group(root.transform,"BluebellStand");
            Rect(stand,"StandCollision",632,594,723,728,.005f,.01f,wood);
            var blocker=stand.gameObject.AddComponent<BoxCollider>();blocker.center=Plan(677.5f,661,2);blocker.size=new Vector3(91*MetresPerPlanUnit,4,134*MetresPerPlanUnit);stand.gameObject.layer=obstacle;
            for(int i=0;i<6;i++)Rect(stand,"SeatingTier"+i,632,594+i*22.33f,679,594+(i+1)*22.33f,(6-i)*.30f,.60f*(6-i),wood);
            for(int i=0;i<20;i++)Rect(stand,"WalkingStep"+i,679,594+i*6.7f,723,594+(i+1)*6.7f,(20-i)*.09f,.18f*(20-i),wood);
            Label(stand,"StandLabel","BLUEBELL STAND",Plan(678,731,.03f),Quaternion.Euler(90,0,0),5,.5f);
            var bounds=Group(root.transform,"ExteriorBoundary");
            Edge(bounds,"West",Plan(150,334),Plan(150,878),Vector3.left,wall);
            Edge(bounds,"East",Plan(1110,334),Plan(1110,878),Vector3.right,wall);
            Edge(bounds,"North",Plan(150,334),Plan(1110,334),Vector3.forward,wall);
            Edge(bounds,"SouthLeft",Plan(150,878),Plan(632,878),Vector3.back,wall);
            Edge(bounds,"SouthRight",Plan(772,878),Plan(1110,878),Vector3.back,wall);
            // Entry is closed at the footprint boundary for this indoor-only driving prototype.
            Edge(bounds,"EntranceGlazing",Plan(632,878),Plan(772,878),Vector3.back,glass);
            Label(details,"Entrance","CONVERGENCE HALL / B1",Plan(702,864,.025f),Quaternion.Euler(90,0,0),8,.55f);
            Label(details,"PrototypeNote","B1 LAYOUT STUDY - APPROXIMATE DIMENSIONS",Plan(740,811,.025f),Quaternion.Euler(90,0,0),7,.4f);
            var landmarks=Group(root.transform,"Landmarks");
            Box(landmarks,"VendingMachine",Plan(724,681,.75f),new Vector3(.6f,1.5f,.65f),navy);
            Box(landmarks,"VendingFront",Plan(728.3f,681,.82f),new Vector3(.012f,.9f,.47f),glass);
            // Ceiling intentionally absent: existing elevated follow camera remains unchanged.
            ConvergenceHallB1Notch.ApplyTo(root);
            ConvergenceHallWallMaterials.ApplyTo(root);
            return root;
        }
    }
}

