using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // User-annotated B1 plan, 2026-10-06. Along runs west->east or north->south.
    // Classroom leaves are measured at 0.95m; other entrance widths remain estimates.
    // Static closed doors only. Pivot names record the inward opening sign in degrees.
    public static class ConvergenceHallPlanDoors
    {
        const float H=2.85f,DoorH=2.20f;
        public const float ClassroomLeafWidth=.95f;
        public const float GlazedAdjacentDoorGap=1.15f,WhiteAdjacentDoorGap=.70f;
        const float LeafClearance=.012f;
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        struct Door
        {
            public string Room,Side,Id,Kind;public float Along,Width;public int Hinge;
            public Door(string room,string side,string id,float along,int hinge,float width=ClassroomLeafWidth+LeafClearance,string kind="Swing")
            {Room=room;Side=side;Id=id;Along=along;Hinge=hinge;Width=width;Kind=kind;}
        }
        static readonly Door[] Layout={
            new Door("B101","North","West",0,1),new Door("B101","North","East",1,-1),
            new Door("B102","North","West",0,1),new Door("B102","North","East",1,-1),
            new Door("B103","North","West",0,1),new Door("B103","North","East",1,-1),
            new Door("B104","East","Entry",.57f,1),new Door("B105","East","South",1,-1),
            new Door("B106","South","West",0,-1),new Door("B106","South","East",1,1),
            new Door("B107","South","West",0,-1),new Door("B107","South","East",1,1),
            new Door("B108","West","North",0,-1),new Door("B108","South","East",1,1),
            new Door("B109","West","North",0,-1),new Door("B109","West","South",1,1),
            new Door("B110","West","North",0,-1),
            // B111 already has the photo-based north-end door on its EastUpper face.
            new Door("B112","East","North",0,1),new Door("B112","East","South",1,-1),
            new Door("B113","West","South",1,1),
            new Door("B114","West","North",0,-1),new Door("B114","West","South",1,1),
            new Door("B115","West","North",0,-1),new Door("B115","West","South",1,1),
            new Door("WestWC","East","NorthWC",.08f,1,.85f),new Door("WestWC","East","SouthWC",.61f,1,.85f),
            new Door("NorthWC","West","Men",.53f,-1,.85f),new Door("NorthWC","South","Women",.72f,-1,.85f),
            new Door("NorthWC","South","AccessibleA",.24f,0,.95f,"Automatic"),new Door("NorthWC","South","AccessibleB",.50f,0,.95f,"Automatic"),
            new Door("Lounge","West","Automatic",.84f,0,1.05f,"Automatic"),
            new Door("B124","North","West",.11f,1),new Door("B124Wing","West","Double",.50f,0,2.05f,"DoubleSwing")
        };
        static Material white,metal,dark,glass;
        static Transform Group(Transform p,string name)
        {var t=p.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(p,false);}return t;}
        static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material m)
        {
            var t=p.Find(name);if(!t){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());t=g.transform;t.SetParent(p,false);}
            t.localPosition=at;t.localRotation=Quaternion.identity;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=m;t.GetComponent<Renderer>().shadowCastingMode=m.HasProperty("_Surface")&&m.GetFloat("_Surface")==1?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;t.gameObject.SetActive(true);return t;
        }
        static Vector3 Position(GameObject root,Door d)
        {
            var b=root.transform.Find("RoomsAndCores/"+d.Room+"/RoomFloor").GetComponent<Renderer>().bounds;
            float margin=d.Width/2+.17f;
            bool whitePair=d.Side=="North"&&(d.Room=="B102"||d.Room=="B101"&&d.Id=="West"||d.Room=="B103"&&d.Id=="East");
            bool glassPair=d.Side=="South"&&(d.Room=="B107"||d.Room=="B106"&&d.Id=="West"||d.Room=="B108"&&d.Id=="East");
            if(whitePair||glassPair)margin=(d.Width-LeafClearance+(whitePair?WhiteAdjacentDoorGap:GlazedAdjacentDoorGap))/2;
            if(d.Side=="West"||d.Side=="East")return new Vector3(d.Side=="West"?b.min.x:b.max.x,0,Mathf.Lerp(b.max.z-margin,b.min.z+margin,d.Along));
            return new Vector3(Mathf.Lerp(b.min.x+margin,b.max.x-margin,d.Along),0,d.Side=="North"?b.max.z:b.min.z);
        }
        static float Yaw(string side)=>side=="West"?90:side=="East"?-90:side=="North"?180:0;
        static bool PhotoDoor(Door d)=>d.Kind=="Swing"&&d.Room.StartsWith("B1");
        static void StylePhotoDoor(Transform p,Door d)
        {
            float side=-d.Hinge,x=side*(d.Width/2+.07f);
            Box(p,"FrameLeft",new Vector3(-d.Width/2-.025f,H/2,0),new Vector3(.05f,H,.105f),white);
            Box(p,"FrameRight",new Vector3(d.Width/2+.025f,H/2,0),new Vector3(.05f,H,.105f),white);
            Box(p,"FrameHeader",new Vector3(0,DoorH+.025f,0),new Vector3(d.Width+.10f,.05f,.105f),white);
            Box(p,"OpaqueTransom",new Vector3(0,(DoorH+.05f+H)/2,0),new Vector3(d.Width,H-DoorH-.05f,.045f),white);
            Box(p,"TransomTopFrame",new Vector3(0,H-.015f,0),new Vector3(d.Width+.10f,.03f,.105f),white);
            Box(p,"LatchSidePanel",new Vector3(x,H/2,0),new Vector3(.14f,H,.105f),white);
            Box(p,"RoomPlate",new Vector3(x,1.49f,-.064f),new Vector3(.13f,.23f,.018f),dark);
            Box(p,"RoomPlateHeader",new Vector3(x,1.57f,-.075f),new Vector3(.13f,.07f,.006f),white);
            var label=p.Find("RoomLabel");label.localPosition=new Vector3(x,1.57f,-.080f);label.localScale=Vector3.one*.001f;((RectTransform)label).sizeDelta=new Vector2(125,60);label.GetComponent<Text>().color=new Color(.03f,.04f,.05f);
            Box(p,"AccessReader",new Vector3(x,1.19f,-.087f),new Vector3(.07f,.14f,.04f),dark);
            Box(p,"ReaderIndicator",new Vector3(x,1.145f,-.110f),new Vector3(.022f,.006f,.004f),AssetDatabase.LoadAssetAtPath<Material>(Mats+"VendingGreen.mat"));
            var pivot=p.Find("Hinge_InwardY"+(d.Hinge<0?"-90":"+90"));
            pivot.Find("Closer").gameObject.SetActive(false);pivot.Find("HandleBase").gameObject.SetActive(false);
            var rose=pivot.Find("RoundHandleBase");
            if(!rose){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name="RoundHandleBase";UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());rose=go.transform;rose.SetParent(pivot,false);}
            rose.localPosition=new Vector3(-d.Hinge*(d.Width-.13f),.97f,-.035f);rose.localRotation=Quaternion.Euler(90,0,0);rose.localScale=new Vector3(.065f,.008f,.065f);rose.GetComponent<Renderer>().sharedMaterial=metal;
        }
        static void Label(Transform p,string text)
        {
            Box(p,"RoomPlate",new Vector3(0,2.38f,-.051f),new Vector3(.70f,.17f,.018f),dark);
            var t=p.Find("RoomLabel");if(!t){var g=new GameObject("RoomLabel",typeof(RectTransform),typeof(Canvas),typeof(Text));t=g.transform;t.SetParent(p,false);g.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;}
            t.localPosition=new Vector3(0,2.38f,-.063f);t.localScale=Vector3.one*.002f;((RectTransform)t).sizeDelta=new Vector2(335,75);
            var tx=t.GetComponent<Text>();tx.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tx.fontSize=30;tx.resizeTextForBestFit=true;tx.resizeTextMinSize=8;tx.resizeTextMaxSize=30;tx.text=text;tx.color=Color.white;tx.alignment=TextAnchor.MiddleCenter;tx.raycastTarget=false;
        }
        static void Swing(Transform door,string name,float centre,float width,int hingeSide)
        {
            var pivot=Group(door,name+"_InwardY"+(hingeSide<0?"-90":"+90"));
            pivot.localPosition=new Vector3(centre+hingeSide*width/2,0,0);pivot.localRotation=Quaternion.identity;
            float panelX=-hingeSide*width/2;
            Box(pivot,"Leaf",new Vector3(panelX,DoorH/2,0),new Vector3(width-LeafClearance,DoorH,.045f),white);
            Box(pivot,"HandleBase",new Vector3(-hingeSide*(width-.13f),.97f,-.032f),new Vector3(.035f,.11f,.018f),metal);
            Box(pivot,"Lever",new Vector3(-hingeSide*(width-.19f),.99f,-.064f),new Vector3(.15f,.025f,.065f),metal);
            for(int i=0;i<3;i++)Box(pivot,"Hinge"+i,new Vector3(0,.30f+i*.75f,-.016f),new Vector3(.034f,.075f,.055f),metal);
            Box(pivot,"Closer",new Vector3(panelX*.60f,2.09f,-.042f),new Vector3(.19f,.045f,.035f),metal);
        }
        static void BuildDoor(GameObject root,Door d)
        {
            var p=Group(Group(root.transform,"PlanDoors"),d.Room+"_"+d.Side+"_"+d.Id);
            p.position=Position(root,d);p.rotation=Quaternion.Euler(0,Yaw(d.Side),0);p.position-=p.forward*.017f;
            Box(p,"FrameLeft",new Vector3(-d.Width/2-.025f,1.125f,0),new Vector3(.05f,2.25f,.105f),metal);
            Box(p,"FrameRight",new Vector3(d.Width/2+.025f,1.125f,0),new Vector3(.05f,2.25f,.105f),metal);
            Box(p,"FrameHeader",new Vector3(0,2.225f,0),new Vector3(d.Width+.10f,.05f,.105f),metal);
            if(d.Kind=="Automatic")
            {
                for(int i=0;i<2;i++)
                {
                    float x=(i==0?-1:1)*d.Width/4;
                    Box(p,i==0?"SlidingLeafLeft":"SlidingLeafRight",new Vector3(x,1.1f,0),new Vector3(d.Width/2-.014f,2.2f,.035f),glass);
                    Box(p,"SlidingStile"+i,new Vector3((i==0?-.008f:.008f),1.1f,-.023f),new Vector3(.014f,2.2f,.014f),metal);
                }
                Box(p,"AutomaticTrack",new Vector3(0,2.28f,-.035f),new Vector3(d.Width+.18f,.12f,.13f),metal);
                Box(p,"Sensor",new Vector3(0,2.30f,-.112f),new Vector3(.09f,.04f,.035f),dark);
            }
            else if(d.Kind=="DoubleSwing")
            {Swing(p,"LeftHinge",-d.Width/4,d.Width/2,-1);Swing(p,"RightHinge",d.Width/4,d.Width/2,1);}
            else Swing(p,"Hinge",0,d.Width,d.Hinge);
            Label(p,d.Room=="NorthWC"||d.Room=="WestWC"?"WC":d.Room=="B124Wing"?"B124":d.Room.ToUpperInvariant());
            if(PhotoDoor(d))StylePhotoDoor(p,d);
        }
        struct Hole{public float Min,Max,Height;public Hole(float a,float b,float h){Min=a;Max=b;Height=h;}}
        static void CutEdge(Transform edge,Door[] doors,GameObject root)
        {
            var cuts=doors.Select(d=>{float z=edge.InverseTransformPoint(Position(root,d)).z;float w=(d.Width+.01f)/Mathf.Abs(edge.lossyScale.z);return new Hole(z-w/2,z+w/2,PhotoDoor(d)?H:DoorH+.02f);}).OrderBy(h=>h.Min).ToArray();
            var outGroup=Group(edge,"DoorOpeningSurfaces");foreach(Transform t in outGroup)t.gameObject.SetActive(false);
            var sources=new List<Transform>{edge.Find("LowerWall"),edge.Find("Skirting")};
            var glazing=edge.Find("CorridorGlazing");if(glazing)foreach(Transform t in glazing)if(t.gameObject.activeSelf)sources.Add(t);
            int n=0;
            foreach(var source in sources)
            {
                if(!source||!source.gameObject.activeSelf)continue;
                var p=source.localPosition;var s=source.localScale;float a=p.z-s.z/2,b=p.z+s.z/2,lo=p.y-s.y/2,hi=p.y+s.y/2;
                float cursor=a;Material mat=source.GetComponent<Renderer>().sharedMaterial;
                void Segment(float left,float right,float bottom,float top)
                {if(right-left<.001f||top-bottom<.001f)return;Box(outGroup,"Surface"+n++,new Vector3(p.x,(bottom+top)/2,(left+right)/2),new Vector3(s.x,top-bottom,right-left),mat);}
                foreach(var hole in cuts)
                {
                    float start=Mathf.Max(a,hole.Min),end=Mathf.Min(b,hole.Max);if(end<=start)continue;
                    Segment(cursor,start,lo,hi);Segment(start,end,Mathf.Max(lo,hole.Height),hi);cursor=Mathf.Max(cursor,end);
                }
                Segment(cursor,b,lo,hi);source.gameObject.SetActive(false);
            }
            // Closed doors retain the existing continuous collision boundary. No new input/door system.
        }
        public static void ApplyTo(GameObject root)
        {
            white=AssetDatabase.LoadAssetAtPath<Material>(Mats+"WarmWhite.mat");metal=AssetDatabase.LoadAssetAtPath<Material>(Mats+"Metal.mat");dark=AssetDatabase.LoadAssetAtPath<Material>(Mats+"Navy.mat");glass=AssetDatabase.LoadAssetAtPath<Material>(Mats+"FrostedTeal.mat");
            foreach(Transform old in root.transform.Find("DoorsAndSigns"))
                if(Layout.Any(d=>d.Room==old.name))old.gameObject.SetActive(false);
            foreach(var d in Layout)BuildDoor(root,d);
            foreach(var grouping in Layout.GroupBy(d=>d.Room+"/"+d.Side))
                CutEdge(root.transform.Find("RoomsAndCores/"+grouping.Key),grouping.ToArray(),root);
            // Preserve the photographed B111 sidelight facade. Its door is at the north end.
            var entrance=root.transform.Find("DoorsAndSigns/B111Entrance");
            var pivot=Group(entrance,"HingeNorth_InwardY+90");pivot.localPosition=new Vector3(33*ConvergenceHallB1Setup.MetresPerPlanUnit/2-.06f,0,0);
            for(int i=0;i<3;i++)Box(pivot,"Hinge"+i,new Vector3(0,.3f+i*.8f,-.02f),new Vector3(.035f,.08f,.06f),metal);
            // Photo: the two narrow sidelights are separated by a white partition cover.
            var separators=Group(root.transform,"AdjacentDoorPanels");
            var pairNames=new[]{new[]{"B108_South_East","B107_South_West"},new[]{"B107_South_East","B106_South_West"}};
            var entries=root.transform.Find("PlanDoors");
            for(int i=0;i<pairNames.Length;i++)
            {
                var at=(entries.Find(pairNames[i][0]).position+entries.Find(pairNames[i][1]).position)/2+Vector3.up*(H/2);
                Box(separators,"GlazedPairWhitePost"+i,separators.InverseTransformPoint(at),new Vector3(.20f,H,.10f),white);
            }
        }
        [MenuItem("Tools/EEG Wheelchair/Apply Full Height Walls And Annotated Doors")]
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
            if(root.transform.Find("UpperWalls"))throw new Exception("Split wall hierarchy remains.");
            if(root.GetComponentsInChildren<Transform>().Any(t=>t.name=="TopCap"&&t.gameObject.activeInHierarchy))throw new Exception("Mid-wall cap remains.");
            var group=root.transform.Find("PlanDoors");if(group.childCount!=Layout.Length)throw new Exception("Door count mismatch.");
            foreach(var d in Layout)
            {
                var t=group.Find(d.Room+"_"+d.Side+"_"+d.Id);var expected=Position(root,d)-t.forward*.017f;
                if(Vector3.Distance(t.position,expected)>.002f)throw new Exception("Door shifted off room boundary: "+t.name);
                if(Mathf.Abs(t.Find("FrameLeft").GetComponent<Renderer>().bounds.max.y-2.25f)>.002f)throw new Exception("Door height: "+t.name);
                var edge=root.transform.Find("RoomsAndCores/"+d.Room+"/"+d.Side);
                if(!edge.GetComponent<BoxCollider>().enabled&&!edge.Find("DoorBoundarySegments"))throw new Exception("Door boundary collision lost: "+t.name);
            }
            Debug.Log("FULL_WALLS_DOORS_OK: 2.85m continuous walls; no mid caps/UpperWalls; "+Layout.Length+" annotated entries + B111 preserved; 3 automatic, 1 double; static closed doors.");
        }
        public static void ApplyAndValidate()
        {
            Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Repeated door update duplicated objects.");
            ConvergenceHallB1Validation.BuildAndValidate();
        }
    }
}

