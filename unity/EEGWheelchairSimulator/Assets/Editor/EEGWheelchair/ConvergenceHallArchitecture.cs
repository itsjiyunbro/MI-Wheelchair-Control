using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace EEGWheelchairSimulator.Editor
{
    // Measured wall height; all other facade dimensions remain photo-based estimates.
    // Walls are continuous and always visible. Legacy upper-cut renderers are removed.
    public static class ConvergenceHallArchitecture
    {
        public const float Height=2.85f;

        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        static Material white,blue,metal,navy,clear;
        static Transform Group(Transform p,string n)
        {var t=p.Find(n);if(t==null){t=new GameObject(n).transform;t.SetParent(p,false);}return t;}
        static Transform Box(Transform p,string n,Vector3 position,Vector3 size,Material material)
        {
            var t=p.Find(n);
            if(t==null){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());t=g.transform;t.SetParent(p,false);}
            t.localPosition=position;t.localScale=size;var r=t.GetComponent<Renderer>();r.sharedMaterial=material;
            r.shadowCastingMode=material==clear?ShadowCastingMode.Off:ShadowCastingMode.On;return t;
        }
        public static void ApplyTo(GameObject root)
        {
            white=AssetDatabase.LoadAssetAtPath<Material>(Mats+"WarmWhite.mat");blue=AssetDatabase.LoadAssetAtPath<Material>(Mats+"FrostedTeal.mat");
            metal=AssetDatabase.LoadAssetAtPath<Material>(Mats+"Metal.mat");navy=AssetDatabase.LoadAssetAtPath<Material>(Mats+"Navy.mat");
            clear=AssetDatabase.LoadAssetAtPath<Material>(Mats+"ClearSidelight.mat");
            if(clear==null)
            {
                clear=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="ClearSidelight"};
                clear.SetColor("_BaseColor",new Color(.64f,.80f,.82f,.24f));clear.SetFloat("_Surface",1);clear.SetFloat("_Blend",0);
                clear.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);clear.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                clear.SetFloat("_SrcBlendAlpha",1);clear.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
                clear.SetFloat("_ZWrite",0);clear.SetFloat("_Smoothness",.55f);clear.SetFloat("_Cull",0);
                clear.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");clear.SetOverrideTag("RenderType","Transparent");clear.renderQueue=3000;
                AssetDatabase.CreateAsset(clear,Mats+"ClearSidelight.mat");
            }
            // Full-height walls: replace the previous horizontal cut and cap, not just its visibility.
            foreach(var old in root.GetComponentsInChildren<BoxCollider>(true).Where(c=>c.transform.Find("LowerWall")!=null).ToArray())
            {
                var edge=old.transform;var size=old.size;size.y=Height;old.size=size;var centre=old.center;centre.y=Height/2;old.center=centre;
                var body=edge.Find("LowerWall");var p=body.localPosition;p.y=Height/2;body.localPosition=p;var s=body.localScale;s.y=Height;body.localScale=s;body.gameObject.SetActive(true);
                edge.Find("TopCap").gameObject.SetActive(false);edge.Find("Skirting").gameObject.SetActive(true);
                var glazing=edge.Find("CorridorGlazing");
                if(glazing!=null)foreach(Transform part in glazing)
                {p=part.localPosition;p.y=Height/2;part.localPosition=p;s=part.localScale;s.y=Height;part.localScale=s;}
            }
            var facade=Group(root.transform.Find("DoorsAndSigns"),"B111Entrance");
            facade.localPosition=ConvergenceHallB1Setup.Plan(640.4f,350.5f);facade.localRotation=Quaternion.Euler(0,-90,0);
            var facadeUpper=facade;
            // The original wall's collider remains; replace its visuals with the actual facade.
            var wall=root.transform.Find("RoomsAndCores/B111/EastUpper");
            foreach(string n in new[]{"LowerWall","TopCap","Skirting","CorridorGlazing"})wall.Find(n).gameObject.SetActive(false);
            foreach(Transform p in wall)if(p.name=="Mullion")p.gameObject.SetActive(false);
            void Tall(string n,float x,float z,float w,float thick,float lo,float hi,Material m)
            {
                Box(facade,n,new Vector3(x,(lo+hi)/2,z),new Vector3(w,hi-lo,thick),m);
                
            }
            float width=33*ConvergenceHallB1Setup.MetresPerPlanUnit;
            float left=-width/2+.06f,right=width/2-.06f;
            float doorWidth=ConvergenceHallPlanDoors.ClassroomLeafWidth,post=right-doorWidth-.08f;
            Tall("FrostedSidelight",(left+post-.08f)/2,0,post-.08f-left,.04f,0,2.26f,blue);
            Tall("ClearSidelight",(left+post-.08f)/2,0,post-.08f-left,.018f,2.26f,2.79f,clear);
            Tall("OpaqueDoor",right-doorWidth/2,0,doorWidth,.045f,0,2.35f,white);
            Tall("DoorTransom",right-doorWidth/2,0,doorWidth,.06f,2.35f,Height,white);
            Tall("CentrePost",post,-.012f,.16f,.09f,0,Height,white);
            Tall("LeftFrame",-width/2+.03f,0,.06f,.10f,0,Height,white);
            Tall("RightFrame",width/2-.03f,0,.06f,.10f,0,Height,white);
            Tall("Header",0,0,width,.10f,2.79f,Height,white);
            Box(facade,"HandleBase",new Vector3(post+.19f,.92f,-.045f),new Vector3(.055f,.12f,.025f),metal);
            Box(facade,"LeverHandle",new Vector3(post+.25f,.94f,-.087f),new Vector3(.18f,.028f,.065f),metal);
            Box(facadeUpper,"AccessReader",new Vector3(post,1.17f,-.08f),new Vector3(.065f,.13f,.035f),navy);
            Box(facadeUpper,"RoomNumberBacking",new Vector3(post,1.49f,-.073f),new Vector3(.14f,.23f,.025f),navy);
            var text=facadeUpper.Find("B111Number");
            if(text==null)
            {
                var go=new GameObject("B111Number",typeof(RectTransform),typeof(Canvas),typeof(Text));text=go.transform;text.SetParent(facadeUpper,false);
                go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
                var label=go.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text="B111";label.fontSize=30;label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
                ((RectTransform)text).sizeDelta=new Vector2(140,60);text.localScale=Vector3.one*.001f;
            }
            text.localPosition=new Vector3(post,1.54f,-.089f);
            ConvergenceHallCoreHalls.ApplyTo(root);
            var legacy=root.transform.Find("UpperWalls");if(legacy)UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            ConvergenceHallCorridorPhoto.ApplyClerestory(root);
            ConvergenceHallGlassBands.ApplyTo(root);
            ConvergenceHallPlanDoors.ApplyTo(root);
            ConvergenceHallGlassPanelWidths.ApplyTo(root);
            ConvergenceHallCorridorPhoto.ApplyExit(root);
            ConvergenceHallFloorFinishes.ApplyTo(root);
            ConvergenceHallClassroomSigns.ApplyTo(root);
            ConvergenceHallClassroomInteriors.ApplyTo(root);
            ConvergenceHallMainEntrance.ApplyTo(root);
            ConvergenceHallCorridorCeilings.ApplyTo(root);
            ConvergenceHallLoungeDoor.Build(root);
            ConvergenceHallJoinPrivacy.Build(root);
            ConvergenceHallRoomCompletion.ApplyTo(root);
            ConvergenceHallPhotoDoorFinishes.ApplyTo(root);
            ConvergenceHallB124DoorPhoto.ApplyTo(root);
            ConvergenceHallB112Presentation.ApplyTo(root);
            ConvergenceHallWestLiftPhoto.ApplyTo(root);
            ConvergenceHallLoungePhoto.ApplyTo(root);
            ConvergenceHallFinalFinishes.HideFloorText(root);
            ConvergenceHallLecternLighting.DisableEmission(root);
            ConvergenceHallWallSurfaceCleanup.ApplyTo(root);
            if(root.transform.Find("DoorDynamics"))ConvergenceHallDoorInteraction.Build(root);
            Debug.Log("B1_ARCHITECTURE_OK: continuous full-height walls 2.85m; B111 left sidelight/right opaque door.");
        }
        [MenuItem("Tools/EEG Wheelchair/Apply Convergence 2.85m Walls And B111 Entrance")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ConvergenceHallWallMaterials.ApplyTo(root);Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
        }
        static void Validate(GameObject root)
        {
            foreach(var c in root.GetComponentsInChildren<BoxCollider>(true).Where(c=>c.transform.Find("LowerWall")!=null))
                if(Mathf.Abs(c.size.y-Height)>.001f||Mathf.Abs(c.center.y-Height/2)>.001f)throw new Exception("Wrong wall height: "+c.name);
            var upper=root.transform;
            if(root.transform.Find("UpperWalls"))throw new Exception("Legacy upper split remains.");
            float max=root.GetComponentsInChildren<BoxCollider>(true).Where(c=>c.transform.Find("LowerWall")!=null).Max(c=>c.bounds.max.y);
            if(Mathf.Abs(max-Height)>.003f)throw new Exception("Visual height mismatch: "+max);
        }
        public static void ApplyAndValidate()
        {
            Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Duplicate upper walls.");
            ConvergenceHallB1Validation.BuildAndValidate();
        }
        public static void CaptureOnly()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}



