using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallLoungeDoor
    {
        const float Width=2.30f,Height=2.85f;
        static Material white,glass,navy,pink,ink;
        static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material mat)
        {var t=P.Box(p,name,at,size,mat);if(mat==glass)t.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return t;}
        static void Text(Transform parent,string name,string content,Vector3 at,Vector2 size,int pixels,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.Text));go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=Vector3.one*.001f;
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)go.transform).sizeDelta=size*1000;
            var t=go.GetComponent<UnityEngine.UI.Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");t.text=content;t.fontSize=pixels;t.resizeTextForBestFit=true;t.resizeTextMinSize=4;t.resizeTextMaxSize=pixels;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;
        }
        static void Symbol(Transform parent,string name,Vector3 at,float diameter)
        {
            var g=P.Group(parent,name);g.localPosition=at;g.localScale=Vector3.one*(diameter/.218f);
            g.gameObject.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Meshes/ConvergenceHall/YonseiOfficialSymbol.asset");g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/YonseiSymbolBlue.mat");
        }
        static void SideGlazing(Transform parent,float from,float to,string side)
        {
            if(to-from<.10f)return;int count=Mathf.Max(1,Mathf.RoundToInt(to-from));float width=(to-from)/count;
            for(int i=0;i<count;i++)Box(parent,side+"ClearPane"+i,new Vector3(from+(i+.5f)*width,1.425f,.015f),new Vector3(width-.014f,2.79f,.014f),glass);
            for(int i=0;i<=count;i++)Box(parent,side+"Joint"+i,new Vector3(from+i*width,1.425f,.01f),new Vector3(.025f,2.85f,.045f),white);
            Box(parent,side+"WindowBaseFrame",new Vector3((from+to)/2,.025f,.01f),new Vector3(to-from,.05f,.075f),white);
            Box(parent,side+"WindowTopFrame",new Vector3((from+to)/2,2.825f,.01f),new Vector3(to-from,.05f,.075f),white);
        }
        static void HoursOutline(Transform parent)
        {
            const float w=.49f,h=.195f,r=.055f;var points=new System.Collections.Generic.List<Vector3>();
            for(int c=0;c<4;c++)for(int i=0;i<=8;i++)
            {float angle=(c*90+i*90f/8)*Mathf.Deg2Rad;float x=(c==0||c==3?1:-1)*(w/2-r),y=(c<2?1:-1)*(h/2-r);points.Add(new Vector3(x+Mathf.Cos(angle)*r,.925f+y+Mathf.Sin(angle)*r,-.050f));}
            for(int i=0;i<points.Count;i++)P.Tube(parent,"HoursOutline"+i,points[i],points[(i+1)%points.Count],.0017f,white);
        }
        public static void Build(GameObject root)
        {
            var door=root.transform.Find("PlanDoors/Lounge_West_Automatic");var edge=root.transform.Find("RoomsAndCores/Lounge/West");
            var old=door.Find("PhotoLoungeEntrance");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(Transform child in door)child.gameObject.SetActive(false);
            foreach(Transform child in edge)child.gameObject.SetActive(false);
            white=P.Mat("LoungeDoorWhite",new Color(.86f,.87f,.85f),.20f);navy=P.Mat("LoungeStickerNavy",new Color(.06f,.025f,.10f),.12f);pink=P.Mat("LoungeStickerPalePink",new Color(.96f,.79f,.83f),.10f);ink=P.Mat("LoungeStickerInk",new Color(.08f,.07f,.09f),.10f);
            glass=P.Mat("LoungeClearGlass",Color.white,.55f);glass.CopyPropertiesFromMaterial(ConvergenceHallGlassBands.Glass(false));glass.SetColor("_BaseColor",new Color(.88f,.95f,.94f,.12f));glass.SetFloat("_Smoothness",.55f);EditorUtility.SetDirty(glass);
            var facade=P.Group(door,"PhotoLoungeEntrance");facade.localPosition=new Vector3(-.5625f,0,0);
            var bounds=root.transform.Find("RoomsAndCores/Lounge/RoomFloor").GetComponent<Renderer>().bounds;
            float from=facade.InverseTransformPoint(new Vector3(bounds.center.x,0,bounds.max.z)).x+.015f,to=facade.InverseTransformPoint(new Vector3(bounds.center.x,0,bounds.min.z)).x-.015f;
            SideGlazing(facade,from,-1.43f,"NorthSide");SideGlazing(facade,1.15f,to,"SouthSide");
            Box(facade,"WhiteColumn",new Vector3(-1.29f,Height/2,0),new Vector3(.28f,Height,.12f),white);
            foreach(float x in new[]{-1.125f,0,1.125f})Box(facade,"PortalPost"+x.ToString("F3"),new Vector3(x,Height/2,0),new Vector3(.05f,Height,.075f),white);
            Box(facade,"AutomaticHeaderWall",new Vector3(0,2.24f,0),new Vector3(Width,.20f,.10f),white);
            Box(facade,"PortalTopFrame",new Vector3(0,2.825f,0),new Vector3(Width,.05f,.075f),white);
            Box(facade,"UpperClearTransom",new Vector3(0,2.58f,.012f),new Vector3(2.20f,.48f,.014f),glass);
            Box(facade,"LeftFixedGlass",new Vector3(-.5625f,1.10f,.018f),new Vector3(1.075f,2.14f,.014f),glass);
            Box(facade,"LeftFixedBaseFrame",new Vector3(-.5625f,.035f,0),new Vector3(1.075f,.05f,.075f),white);
            var leaf=P.Group(facade,"RightSlidingLeaf");leaf.localPosition=new Vector3(.5625f,0,-.008f);
            Box(leaf,"ClearLeafGlass",new Vector3(0,1.11f,0),new Vector3(.995f,2.08f,.014f),glass);
            foreach(float x in new[]{-.5175f,.5175f})Box(leaf,"WhiteLeafStile"+x.ToString("F3"),new Vector3(x,1.095f,-.018f),new Vector3(.04f,2.12f,.06f),white);
            Box(leaf,"WhiteLeafTopRail",new Vector3(0,2.14f,-.018f),new Vector3(1.075f,.04f,.06f),white);
            Box(leaf,"WhiteLeafBottomRail",new Vector3(0,.055f,-.018f),new Vector3(1.075f,.04f,.06f),white);
            Box(facade,"AutomaticSensor",new Vector3(.56f,2.15f,-.070f),new Vector3(.075f,.027f,.024f),navy);
            var certificate=P.Group(facade,"UpperRightNotice");certificate.localPosition=new Vector3(1.035f,2.245f,-.055f);
            Box(certificate,"NoticeCard",Vector3.zero,new Vector3(.14f,.125f,.004f),white);Box(certificate,"NoticeNavyFooter",new Vector3(0,-.05f,-.003f),new Vector3(.14f,.018f,.001f),navy);
            Symbol(certificate,"SmallOfficialSeal",new Vector3(0,.036f,-.004f),.026f);Text(certificate,"NoticeTitle","자동문 안내",new Vector3(0,.009f,-.004f),new Vector2(.125f,.014f),8,ink.color);Text(certificate,"NoticeFooter","연세대학교",new Vector3(0,-.047f,-.005f),new Vector2(.12f,.010f),6,Color.white);
            Box(leaf,"PinkNameStrip",new Vector3(0,1.17f,-.014f),new Vector3(.985f,.21f,.003f),pink);
            Box(leaf,"NavyNameBand",new Vector3(0,1.177f,-.017f),new Vector3(.985f,.125f,.003f),navy);
            Text(leaf,"LoungeTagline","일상 속 건강을 이해하는 공간",new Vector3(0,1.256f,-.020f),new Vector2(.94f,.022f),17,ink.color);
            Text(leaf,"LoungeKoreanName","디지털헬스케어 흥업1 라운지",new Vector3(0,1.193f,-.020f),new Vector2(.945f,.060f),46,Color.white);
            Text(leaf,"LoungeEnglishName","Heungeop1 lounge",new Vector3(0,1.142f,-.020f),new Vector2(.90f,.023f),15,Color.white);
            Symbol(leaf,"StripOfficialSeal",new Vector3(-.075f,1.091f,-.020f),.027f);Text(leaf,"CampusName","연세대학교 미래캠퍼스",new Vector3(.066f,1.091f,-.020f),new Vector2(.245f,.017f),11,ink.color);
            HoursOutline(leaf);Text(leaf,"HoursHeading","운영시간",new Vector3(0,.974f,-.051f),new Vector2(.40f,.027f),23,Color.white);
            Text(leaf,"WeekdayHours","월–목 09:30–17:00",new Vector3(0,.926f,-.051f),new Vector2(.44f,.026f),21,Color.white);Text(leaf,"FridayHours","금 09:30–15:00",new Vector3(0,.883f,-.051f),new Vector2(.44f,.026f),21,Color.white);
        }
        public static void Validate(GameObject root)
        {
            var door=root.transform.Find("PlanDoors/Lounge_West_Automatic");var facade=door.Find("PhotoLoungeEntrance");if(!facade||door.Cast<Transform>().Count(t=>t.name=="PhotoLoungeEntrance")!=1)throw new Exception("Lounge facade missing/duplicated");
            foreach(string name in new[]{"SlidingLeafLeft","SlidingLeafRight","RoomPlate","RoomLabel","AutomaticTrack"})if(door.Find(name).gameObject.activeInHierarchy)throw new Exception("Old lounge entrance visible");
            var dynamicDoor=door.GetComponent<InteractiveDoor>();
            if(!dynamicDoor&&(!root.transform.Find("RoomsAndCores/Lounge/West").GetComponent<BoxCollider>().enabled||facade.GetComponentsInChildren<Collider>().Length>0))throw new Exception("Lounge door changed driving collision");
            var relative=door.InverseTransformPoint(facade.Find("RightSlidingLeaf").position);
            if(Mathf.Abs(relative.x)>.001f||Mathf.Abs(relative.y)>.001f||Mathf.Abs(relative.z+.008f)>.001f)throw new Exception("Sliding door location changed");
            if(facade.Find("RightSlidingLeaf/LoungeKoreanName").GetComponent<UnityEngine.UI.Text>().text!="디지털헬스케어 흥업1 라운지")throw new Exception("Lounge label missing");
            var panes=facade.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial.name=="LoungeClearGlass").ToArray();if(panes.Length<4||panes.Any(r=>r.sharedMaterial.GetFloat("_Surface")!=1))throw new Exception("Lounge glass is not transparent");
            if(facade.GetComponentsInChildren<Renderer>().Any(r=>r.bounds.max.y>2.851f||r.bounds.min.y<-.002f))throw new Exception("Lounge facade height changed");
            ConvergenceHallLecternLighting.Validate(root);
            Debug.Log("PHOTO_LOUNGE_DOOR_OK: white 2.30m portal, left fixed/right closed sliding clear glass, upper transom and clear side glazing; native name strip/hours; original right-leaf position and collision retained.");
        }
        public static void ApplyAndCapture()
        {
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Duplicate lounge photo geometry");
            SessionState.SetBool("EEG.ConvergenceQA.FocusLoungePhotoDoor",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static void VerifySaved()
        {
            var prefab=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);string signature;
            try{signature=(string)typeof(ConvergenceHallWallSurfaceCleanup).GetMethod("ColliderState",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{prefab});}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            SessionState.SetString("EEG.WallSurfaceCollision",signature);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
            Debug.Log("LOUNGE_SAVED_SCENE_OK: saved scene/prefab door geometry, clear glass/text, collision signature, no-glow and wall-surface checks passed.");
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
    }
}
