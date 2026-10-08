using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // Authoring only: no runtime behaviour, packages, physics or existing asset edits.
    public static class RehabJunctionSetup
    {
        public const string ScenePath = "Assets/Scenes/RehabJunctionScene.unity";
        public const string PrefabPath = "Assets/Art/Prefabs/Environment/RehabJunction/RehabJunctionCourse.prefab";
        const string Modules = "Assets/Art/Prefabs/Environment/RehabJunction/Modules/";
        const string Materials = "Assets/Art/Materials/Environment/";
        static Material floor, wall, door, accent, start, goal, band, guide;
        static GameObject wallModule, corridorModule, junctionModule, doorModule;

        [MenuItem("Tools/EEG Wheelchair/Create Rehab Junction Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                Debug.Log("REHAB_EXISTS: existing scene preserved and opened. No regeneration or duplicates.");
                return;
            }
            var source = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            var movement = UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            if (Vector3.Distance(movement.transform.position, new Vector3(0,.5f,0)) > .001f
                || Quaternion.Angle(movement.transform.rotation, Quaternion.identity) > .01f)
                throw new InvalidOperationException("MainScene start pose changed; course requires review before creation.");
            LoadMaterials(); Folder(Modules.TrimEnd('/'));
            wallModule = Asset("WallPanel2m", BuildWall);
            corridorModule = Asset("CorridorStraight5m", BuildCorridor);
            junctionModule = Asset("Junction8m", BuildJunction);
            doorModule = Asset("DecorativeDoor", BuildDoor);
            var course = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (course == null)
            {
                if (File.Exists(PrefabPath)) throw new IOException("Conflicting course asset; not overwritten.");
                var built = BuildCourse();
                try { course = PrefabUtility.SaveAsPrefabAsset(built, PrefabPath); }
                finally { UnityEngine.Object.DestroyImmediate(built); }
            }
            // Copy the full control/HUD/camera graph with Unity's own serializer; never edit MainScene.
            if (!EditorSceneManager.SaveScene(source, ScenePath, true)) throw new IOException("Scene copy failed.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var oldCourse = scene.GetRootGameObjects().Single(g => g.name == "IndoorTestCourse");
            UnityEngine.Object.DestroyImmediate(oldCourse); // Only the instance in the NEW scene.
            PrefabUtility.InstantiatePrefab(course, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("New scene save failed.");
            AssetDatabase.SaveAssets();
            Debug.Log("REHAB_CREATED: new scene, modular course, original control graph and start pose preserved.");
        }

        static void LoadMaterials()
        {
            Material Read(string name) => AssetDatabase.LoadAssetAtPath<Material>(Materials + name + ".mat")
                ?? throw new InvalidOperationException("Missing shared material: " + name);
            floor=Read("Floor"); wall=Read("Wall"); door=Read("Door"); accent=Read("Accent");
            start=Read("Start"); goal=Read("Goal"); band=Read("WallAccent"); guide=Read("RouteGuide");
        }
        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        static GameObject Asset(string name, Func<GameObject> build)
        {
            string path=Modules+name+".prefab";
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset!=null) return asset;
            if (File.Exists(path)) throw new IOException("Conflicting module: "+path);
            var root=build(); root.name=name;
            try { return PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static Transform Group(Transform parent,string name)
        {
            var t=new GameObject(name).transform; t.SetParent(parent,false); return t;
        }
        static Transform Box(Transform parent,string name,Vector3 p,Vector3 s,Material m,float yaw=0)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false); go.transform.localPosition=p;
            go.transform.localScale=s; go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            go.GetComponent<Renderer>().sharedMaterial=m; return go.transform;
        }
        static Transform Instance(GameObject prefab,Transform parent,string name,Vector3 p,float yaw=0)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name=name;
            go.transform.SetParent(parent,false); go.transform.localPosition=p;
            go.transform.localRotation=Quaternion.Euler(0,yaw,0); return go.transform;
        }
        // A wall's length is along local X; pivot at floor level and centre of its thickness.
        static void Wall(Transform parent,string name,Vector3 p,float length,float yaw=0)
        {
            var t=Instance(wallModule,parent,name,p,yaw); t.localScale=new Vector3(length/2f,1,1);
        }
        static void Label(Transform parent,string name,string value,Vector3 p,Quaternion r,float width,float height,int font=90)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(Text));
            var rect=(RectTransform)go.transform; rect.SetParent(parent,false); rect.localPosition=p;
            rect.localRotation=r; rect.localScale=Vector3.one*.0025f;
            rect.sizeDelta=new Vector2(width/.0025f,height/.0025f);
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var text=go.GetComponent<Text>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text=value; text.fontSize=font; text.fontStyle=FontStyle.Bold;
            text.alignment=TextAnchor.MiddleCenter; text.color=Color.white; text.raycastTarget=false;
        }
        static void Board(Transform parent,string name,string title,string subtitle,Vector3 p,float yaw,Material m,float width=2.7f)
        {
            var t=Group(parent,name); t.localPosition=p; t.localRotation=Quaternion.Euler(0,yaw,0);
            Box(t,"Panel",Vector3.zero,new Vector3(width,.78f,.04f),m);
            Label(t,"Title",title,new Vector3(0,.13f,-.026f),Quaternion.identity,width-.15f,.30f,94);
            Label(t,"Subtitle",subtitle,new Vector3(0,-.19f,-.026f),Quaternion.identity,width-.15f,.18f,44);
        }
        static GameObject BuildWall()
        {
            var root=new GameObject(); var t=root.transform;
            Box(t,"WallBody",new Vector3(0,1.308f,0),new Vector3(2,2.6f,.2f),wall);
            Box(t,"LowerBand",new Vector3(0,.31f,0),new Vector3(2,.60f,.216f),band);
            Box(t,"AccentStripe",new Vector3(0,.64f,0),new Vector3(2,.035f,.222f),guide);
            Box(t,"Skirting",new Vector3(0,.068f,0),new Vector3(2,.12f,.23f),accent);
            return root;
        }
        static void Floor(Transform parent,Vector3 p,Vector2 size)
        {
            Box(parent,"Floor",new Vector3(p.x,-.032f,p.z),new Vector3(size.x,.08f,size.y),floor);
            for (float x=-size.x/2+2.5f;x<size.x/2-.01f;x+=2.5f)
                Box(parent,"FloorJointX",p+new Vector3(x,.009f,0),new Vector3(.01f,.001f,size.y),band);
            for (float z=-size.y/2+2.5f;z<size.y/2-.01f;z+=2.5f)
                Box(parent,"FloorJointZ",p+new Vector3(0,.009f,z),new Vector3(size.x,.001f,.01f),band);
        }
        static GameObject BuildCorridor()
        {
            var root=new GameObject(); Floor(root.transform,Vector3.zero,new Vector2(5,5));
            Wall(root.transform,"LeftWall",new Vector3(-2.6f,0,0),5,90);
            Wall(root.transform,"RightWall",new Vector3(2.6f,0,0),5,90);
            return root;
        }
        static GameObject BuildJunction()
        {
            var root=new GameObject(); var t=root.transform; Floor(t,Vector3.zero,new Vector2(8,8));
            Wall(t,"NorthWall",new Vector3(0,0,4.1f),8.2f);
            foreach(int side in new[]{-1,1})
            {
                Wall(t,"EntryReturn"+side,new Vector3(side*3.25f,0,-4.1f),1.5f);
                Wall(t,"ArmReturnSouth"+side,new Vector3(side*4.1f,0,-3.25f),1.5f,90);
                Wall(t,"ArmReturnNorth"+side,new Vector3(side*4.1f,0,3.25f),1.5f,90);
            }
            return root;
        }
        static GameObject BuildDoor()
        {
            var root=new GameObject(); var t=root.transform;
            // Front is local -Z. The door is decorative and stays on the wall face.
            Box(t,"Panel",new Vector3(0,1.04f,0),new Vector3(1.30f,2.08f,.035f),door);
            foreach(int side in new[]{-1,1})
                Box(t,"Frame"+side,new Vector3(side*.72f,1.10f,0),new Vector3(.10f,2.2f,.09f),band);
            Box(t,"Lintel",new Vector3(0,2.16f,0),new Vector3(1.54f,.10f,.09f),band);
            Box(t,"WindowInset",new Vector3(0,1.5f,-.023f),new Vector3(.48f,.52f,.018f),accent);
            Box(t,"Window",new Vector3(0,1.5f,-.035f),new Vector3(.38f,.42f,.008f),band);
            Box(t,"KickPlate",new Vector3(0,.20f,-.024f),new Vector3(1.18f,.27f,.018f),band);
            Box(t,"Handle",new Vector3(.46f,.96f,-.065f),new Vector3(.045f,.25f,.07f),wall);
            return root;
        }
        static void Arrow(Transform parent,string name,Vector3 p,float yaw,Material m)
        {
            var t=Group(parent,name); t.localPosition=p; t.localRotation=Quaternion.Euler(0,yaw,0);
            Box(t,"Shaft",Vector3.zero,new Vector3(.09f,.003f,.65f),m);
            Box(t,"TipL",new Vector3(-.13f,0,.26f),new Vector3(.075f,.003f,.39f),m,45);
            Box(t,"TipR",new Vector3(.13f,0,.26f),new Vector3(.075f,.003f,.39f),m,-45);
        }
        static void Pad(Transform parent,string name,Vector3 p,float yaw,string text,Material m)
        {
            var t=Group(parent,name); t.localPosition=p; t.localRotation=Quaternion.Euler(0,yaw,0);
            foreach(int side in new[]{-1,1})
            {
                Box(t,"Side"+side,new Vector3(side*1.7f,.017f,0),new Vector3(.10f,.008f,2.8f),m);
                Box(t,"Line"+side,new Vector3(0,.017f,side*1.35f),new Vector3(3.5f,.008f,.10f),m);
            }
            Box(t,"LabelBacking",new Vector3(0,.018f,.66f),new Vector3(2.3f,.006f,.66f),m);
            Label(t,"FloorLabel",text,new Vector3(0,.023f,.66f),Quaternion.Euler(90,0,0),2.1f,.5f,120);
        }
        static void Bench(Transform parent,Vector3 p)
        {
            var t=Group(parent,"WaitingBench"); t.localPosition=p;
            Box(t,"Seat",new Vector3(0,.46f,0),new Vector3(1.7f,.12f,.50f),door);
            Box(t,"Back",new Vector3(0,.80f,.20f),new Vector3(1.7f,.58f,.08f),band);
            foreach(int side in new[]{-1,1})
            {
                Box(t,"Leg"+side,new Vector3(side*.66f,.23f,0),new Vector3(.07f,.46f,.39f),accent);
                Box(t,"Arm"+side,new Vector3(side*.83f,.64f,0),new Vector3(.07f,.07f,.5f),accent);
            }
        }
        static void Plant(Transform parent,string name,Vector3 p)
        {
            var t=Group(parent,name); t.localPosition=p;
            Box(t,"Planter",new Vector3(0,.25f,0),new Vector3(.48f,.5f,.48f),band);
            Box(t,"Rim",new Vector3(0,.51f,0),new Vector3(.52f,.06f,.52f),accent);
            for(int i=0;i<3;i++) Box(t,"Leaves"+i,new Vector3(0,.65f+i*.18f,0),new Vector3(.54f-i*.10f,.28f,.54f-i*.10f),start,i*30);
        }
        static GameObject BuildCourse()
        {
            var root=new GameObject("RehabJunctionCourse");
            var corridors=Group(root.transform,"Corridors");
            Instance(corridorModule,corridors,"Entry01",new Vector3(0,0,-1.5f));
            Instance(corridorModule,corridors,"Entry02",new Vector3(0,0,3.5f));
            Instance(junctionModule,corridors,"TJunction",new Vector3(0,0,10));
            foreach(int side in new[]{-1,1})
            {
                Instance(corridorModule,corridors,"Branch"+side+"_01",new Vector3(side*6.5f,0,10),90);
                Instance(corridorModule,corridors,"Branch"+side+"_02",new Vector3(side*11.5f,0,10),90);
            }
            var ends=Group(root.transform,"EndWalls");
            Wall(ends,"GoalAEnd",new Vector3(-14.1f,0,10),5.2f,90);
            Wall(ends,"GoalBEnd",new Vector3(14.1f,0,10),5.2f,90);
            var doors=Group(root.transform,"Doors");
            Instance(doorModule,doors,"RehabilitationDoor",new Vector3(-13.96f,.008f,10),-90);
            Instance(doorModule,doors,"ExaminationDoor",new Vector3(13.96f,.008f,10),90);
            Instance(doorModule,doors,"StaffDoor",new Vector3(2.46f,.008f,3.8f),90);
            var signs=Group(root.transform,"Signs");
            Board(signs,"StartBoard","START","EEG WHEELCHAIR TEST",new Vector3(2.46f,1.7f,.6f),90,start,2.5f);
            Board(signs,"LeftDestination","<  LEFT","A  /  REHABILITATION",new Vector3(-1.75f,1.86f,13.96f),0,accent,3.05f);
            Board(signs,"RightDestination","RIGHT  >","B  /  EXAMINATION",new Vector3(1.75f,1.86f,13.96f),0,accent,3.05f);
            Label(signs,"FacilityName","REHABILITATION CENTRE",new Vector3(0,2.48f,13.93f),Quaternion.identity,5.8f,.25f,68);
            Board(signs,"GoalABoard","GOAL A","REHABILITATION",new Vector3(-13.94f,2.25f,10),-90,goal,2.6f);
            Board(signs,"GoalBBoard","GOAL B","EXAMINATION",new Vector3(13.94f,2.25f,10),90,goal,2.6f);
            var pads=Group(root.transform,"StartAndGoals");
            Pad(pads,"Start",Vector3.zero,0,"START",start);
            Pad(pads,"GoalA",new Vector3(-11.5f,0,10),-90,"GOAL A",goal);
            Pad(pads,"GoalB",new Vector3(11.5f,0,10),90,"GOAL B",goal);
            var route=Group(root.transform,"RouteGuides");
            Box(route,"EntryLine",new Vector3(0,.013f,5.8f),new Vector3(.07f,.003f,8.4f),guide);
            Box(route,"BranchLine",new Vector3(0,.013f,10),new Vector3(19.5f,.003f,.07f),guide);
            foreach(float z in new[]{3f,6f,8.3f}) Arrow(route,"Forward",new Vector3(0,.016f,z),0,guide);
            foreach(int side in new[]{-1,1})
                foreach(float x in new[]{2.3f,6f,9f}) Arrow(route,"Turn"+side,new Vector3(side*x,.016f,10),side*90,guide);
            var props=Group(root.transform,"Props");
            Bench(props,new Vector3(-2.65f,0,13.45f));
            Plant(props,"CornerPlant",new Vector3(3.45f,0,13.35f));
            var desk=Group(props,"InformationDesk"); desk.localPosition=new Vector3(1.9f,0,13.45f);
            Box(desk,"Base",new Vector3(0,.48f,0),new Vector3(1.65f,.96f,.66f),band);
            Box(desk,"Top",new Vector3(0,1,0),new Vector3(1.8f,.08f,.78f),wall);
            Box(desk,"Front",new Vector3(0,.59f,-.34f),new Vector3(1.43f,.5f,.025f),accent);
            Label(desk,"Information","INFORMATION",new Vector3(0,.59f,-.357f),Quaternion.identity,1.3f,.20f,46);
            return root;
        }
    }
}
