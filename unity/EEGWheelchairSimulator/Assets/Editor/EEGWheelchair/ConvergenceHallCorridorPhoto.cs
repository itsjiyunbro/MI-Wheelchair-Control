using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace EEGWheelchairSimulator.Editor
{
    // Photo corrections. Only the 2.85m wall / 1.8m corridor have measured references.
    public static class ConvergenceHallCorridorPhoto
    {
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        static Transform Group(Transform p,string name)
        {var t=p.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(p,false);}return t;}
        static Material Mat(string n)=>AssetDatabase.LoadAssetAtPath<Material>(Mats+n+".mat");
        static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material m)
        {
            var t=p.Find(name);if(!t){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());t=g.transform;t.SetParent(p,false);}
            t.localPosition=at;t.localRotation=Quaternion.identity;t.localScale=size;var r=t.GetComponent<Renderer>();r.sharedMaterial=m;r.shadowCastingMode=m==Mat("ClearSidelight")?ShadowCastingMode.Off:ShadowCastingMode.On;t.gameObject.SetActive(true);return t;
        }
        public static void ApplyClerestory(GameObject root)
        {
            var tinted=Mat("ClerestoryGlass");
            if(!tinted)
            {
                tinted=new Material(Mat("ClearSidelight")){name="ClerestoryGlass"};
                tinted.SetColor("_BaseColor",new Color(.10f,.17f,.17f,.72f));tinted.SetFloat("_Smoothness",.48f);
                AssetDatabase.CreateAsset(tinted,Mats+"ClerestoryGlass.mat");
            }
            foreach(string room in new[]{"B101","B102","B103"})
            {
                var edge=root.transform.Find("RoomsAndCores/"+room+"/North");
                float length=edge.GetComponent<BoxCollider>().size.z;
                var body=edge.Find("LowerWall");var s=body.localScale;s.y=1.56f;body.localScale=s;var p=body.localPosition;p.y=.78f;body.localPosition=p;body.GetComponent<Renderer>().sharedMaterial=Mat("WarmWhite");
                var glazing=Group(edge,"CorridorGlazing");glazing.gameObject.SetActive(true);foreach(Transform t in glazing)t.gameObject.SetActive(false);
                // Opaque full-height door surrounds; the upper window is only between the two doors.
                float pier=1.21f,span=length-2*pier;
                foreach(int side in new[]{-1,1})Box(glazing,"OpaquePier"+side,new Vector3(0,1.425f,side*(length-pier)/2),new Vector3(.12f,2.85f,pier),Mat("WarmWhite"));
                Box(glazing,"WindowGlass",new Vector3(0,2.16f,0),new Vector3(.023f,1.15f,span),tinted).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                Box(glazing,"WindowSill",new Vector3(0,1.575f,0),new Vector3(.14f,.03f,span),Mat("Metal"));
                Box(glazing,"WindowHead",new Vector3(0,2.7525f,0),new Vector3(.14f,.035f,span),Mat("Metal"));
                Box(glazing,"OpaqueHeader",new Vector3(0,2.81f,0),new Vector3(.12f,.08f,span),Mat("WarmWhite"));
                int panes=Mathf.Max(1,Mathf.CeilToInt(span/1.1f));
                for(int i=0;i<=panes;i++)Box(glazing,"WindowMullion"+i,new Vector3(0,2.16f,-span/2+i*span/panes),new Vector3(.052f,1.15f,.018f),Mat("Metal"));
            }
        }
        public static void ApplyExit(GameObject root)
        {
            // The sub-entry lies across the south end of the 1.8m corridor, between B104/B103.
            var wall=root.transform.Find("ExteriorBoundary/SouthLeft");
            var centre=ConvergenceHallB1Setup.Plan(245.5f,878);
            float z=wall.InverseTransformPoint(centre).z;
            var parts=Group(wall,"SubEntranceSurfaces");
            foreach(Transform t in parts)t.gameObject.SetActive(false);
            foreach(string name in new[]{"LowerWall","Skirting"})
            {
                var old=wall.Find(name);var p=old.localPosition;var s=old.localScale;
                float lo=p.z-s.z/2,hi=p.z+s.z/2,cutA=z-.87f,cutB=z+.87f;
                void Piece(string suffix,float a,float b)
                {if(b-a>.001f)Box(parts,name+suffix,new Vector3(p.x,p.y,(a+b)/2),new Vector3(s.x,s.y,b-a),old.GetComponent<Renderer>().sharedMaterial);}
                Piece("Left",lo,cutA);Piece("Right",cutB,hi);
                if(name=="LowerWall")Box(parts,"Lintel",new Vector3(p.x,2.765f,z),new Vector3(s.x,.17f,1.74f),Mat("WarmWhite"));
                old.gameObject.SetActive(false);
            }
            var door=Group(root.transform,"WestSubEntrance");door.position=centre;door.rotation=Quaternion.Euler(0,180,0);
            var dark=Mat("Navy");var metal=Mat("Metal");var clear=Mat("ClearSidelight");
            Box(door,"LeftJamb",new Vector3(-.85f,1.34f,0),new Vector3(.04f,2.68f,.095f),dark);
            Box(door,"RightJamb",new Vector3(.85f,1.34f,0),new Vector3(.04f,2.68f,.095f),dark);
            Box(door,"HeadFrame",new Vector3(0,2.66f,0),new Vector3(1.74f,.04f,.095f),dark);
            Box(door,"TransomBar",new Vector3(0,2.20f,0),new Vector3(1.70f,.045f,.09f),dark);
            Box(door,"TransomGlass",new Vector3(0,2.43f,0),new Vector3(1.66f,.41f,.018f),clear);
            Box(door,"FixedSidelight",new Vector3(-.59f,1.085f,0),new Vector3(.48f,2.17f,.018f),clear);
            Box(door,"CentreMullion",new Vector3(-.33f,1.09f,0),new Vector3(.04f,2.18f,.08f),dark);
            var leaf=Group(door,"GlassDoorLeaf");
            Box(leaf,"Glass",new Vector3(.26f,1.09f,0),new Vector3(1.09f,2.10f,.025f),clear);
            foreach(float x in new[]{-.29f,.81f})Box(leaf,"Stile"+x,new Vector3(x,1.09f,-.009f),new Vector3(.025f,2.18f,.065f),dark);
            foreach(float y in new[]{.025f,2.155f})Box(leaf,"Rail"+y,new Vector3(.26f,y,-.009f),new Vector3(1.12f,.05f,.065f),dark);
            Box(leaf,"PullHandle",new Vector3(-.20f,1.05f,-.078f),new Vector3(.025f,.52f,.032f),metal);
            Box(leaf,"DoorCloser",new Vector3(.55f,2.12f,-.059f),new Vector3(.24f,.06f,.04f),metal);
            Box(door,"Threshold",new Vector3(0,.012f,0),new Vector3(1.72f,.02f,.15f),metal);
            Box(door,"ExitSignBacking",new Vector3(0,2.48f,-.06f),new Vector3(.33f,.17f,.03f),Mat("VendingGreen"));
            var text=door.Find("ExitLabel");if(!text){var g=new GameObject("ExitLabel",typeof(RectTransform),typeof(Canvas),typeof(Text));text=g.transform;text.SetParent(door,false);g.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;}
            text.localPosition=new Vector3(0,2.48f,-.079f);text.localScale=Vector3.one*.002f;((RectTransform)text).sizeDelta=new Vector2(160,70);
            var label=text.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=30;label.text="EXIT";label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
        }
        [MenuItem("Tools/EEG Wheelchair/Apply Corridor Photo Corrections")]
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
            var d=root.transform.Find("WestSubEntrance");if(!d||Vector3.Distance(d.position,ConvergenceHallB1Setup.Plan(245.5f,878))>.002f)throw new Exception("Sub-entry position mismatch.");
            if(root.transform.Find("ExteriorBoundary/SouthLeft/LowerWall").gameObject.activeSelf)throw new Exception("Opaque wall still covers glass entry.");
            var boundary=root.transform.Find("ExteriorBoundary/SouthLeft");
            if(!boundary.GetComponent<BoxCollider>().enabled&&!boundary.Find("DoorBoundarySegments"))throw new Exception("Boundary collision was lost.");
            foreach(string room in new[]{"B101","B102","B103"})
            {
                var edge=root.transform.Find("RoomsAndCores/"+room+"/North");
                var surface=edge.Find("DoorOpeningSurfaces");
                var windows=edge.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial==Mat("ClerestoryGlass")).ToArray();
                if(windows.Length==0||windows.Any(r=>Mathf.Abs(r.bounds.size.y-1.15f)>.002f))throw new Exception("Measured 1.15m upper glazing missing: "+room);
                if(surface.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterial==Mat("FrostedTeal")))throw new Exception("Old full-height teal remains: "+room);
            }
            Debug.Log("CORRIDOR_PHOTO_OK: west sub-entry at B103/B104 corridor end; closed glass door and sidelight; boundary collision retained.");
        }
        public static void ApplyAndValidate()
        {
            Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Duplicate corridor correction geometry.");
            ConvergenceHallB1Validation.BuildAndValidate();
        }
    }
}
