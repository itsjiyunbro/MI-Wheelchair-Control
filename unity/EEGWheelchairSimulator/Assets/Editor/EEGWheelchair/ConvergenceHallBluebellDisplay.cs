using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    // Photo proportions are estimates; preserve the measured stair geometry and B1 collision.
    public static class ConvergenceHallBluebellDisplay
    {
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        static string Collision(GameObject root)=>string.Join("\n",root.GetComponentsInChildren<Collider>(true).Select(c=>AnimationUtility.CalculateTransformPath(c.transform,root.transform)+"|"+c.enabled+"|"+c.bounds.ToString("F4")).OrderBy(s=>s));
        public static void ApplyTo(GameObject root)
        {
            var stair=root.transform.Find("BluebellStand/MeasuredBluebell");
            var old=stair.Find("PhotoRailingAndDisplay");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(Transform t in stair)if(t.name.StartsWith("RailGlass")||t.name.StartsWith("Handrail")||t.name.StartsWith("RailPost"))t.gameObject.SetActive(false);
            var g=P.Group(stair,"PhotoRailingAndDisplay");
            float width=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds.size.x;
            var metal=P.Mat("BluebellBrushedRail",new Color(.59f,.61f,.61f),.37f);metal.SetFloat("_Metallic",.65f);
            var black=P.Mat("BluebellScreenOff",new Color(.016f,.018f,.020f),.12f);
            var casing=P.Mat("BluebellDisplayCasing",new Color(.055f,.057f,.060f),.20f);
            var white=AssetDatabase.LoadAssetAtPath<Material>(Mats+"WarmWhite.mat");
            var glass=AssetDatabase.LoadAssetAtPath<Material>(Mats+"BluebellRailGlass.mat");
            float x=width/2-.035f;
            var path=new[]{new Vector3(x,0,0),new Vector3(x,2.25f,4.5f),new Vector3(x,2.25f,6.7f),new Vector3(x,4.5f,11.2f)};
            int panel=0;
            for(int flight=0;flight<3;flight++)
            {
                int count=flight==1?2:4;
                for(int i=0;i<count;i++)
                {
                    var a=Vector3.Lerp(path[flight],path[flight+1],(i+.008f)/count)+Vector3.up*.08f;
                    var b=Vector3.Lerp(path[flight],path[flight+1],(i+1-.008f)/count)+Vector3.up*.08f;
                    var mesh=new Mesh();mesh.vertices=new[]{a,b,b+Vector3.up*.98f,a+Vector3.up*.98f};mesh.triangles=new[]{0,1,2,0,2,3};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.RecalculateNormals();mesh.RecalculateBounds();
                    var pane=P.Group(g,"GlassPanel"+panel);pane.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("BluebellPhotoGlass"+panel,mesh);pane.gameObject.AddComponent<MeshRenderer>().sharedMaterial=glass;pane.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                    P.Tube(g,"PaneCap"+panel,a+Vector3.up*.98f,b+Vector3.up*.98f,.019f,metal);
                    var mid=(a+b)/2+Vector3.up*.70f;
                    P.Cylinder(g,"GlassBracketDisc"+panel,mid,new Vector3(.058f,.007f,.058f),metal,Quaternion.Euler(0,0,90));
                    P.Tube(g,"BracketArm"+panel,mid,mid+Vector3.left*.11f,.019f,metal);
                    P.Tube(g,"BracketUpturn"+panel,mid+Vector3.left*.11f,mid+Vector3.left*.11f+Vector3.up*.10f,.019f,metal);
                    P.Tube(g,"BaseChannel"+panel,a-Vector3.up*.05f,b-Vector3.up*.05f,.035f,metal);
                    panel++;
                }
                P.Tube(g,"RoundHandrail"+flight,path[flight]+new Vector3(-.11f,.88f,0),path[flight+1]+new Vector3(-.11f,.88f,0),.042f,metal);
            }
            foreach(int end in new[]{0,3})
            {
                var point=path[end]+new Vector3(-.11f,.88f,0);
                P.Tube(g,"HandrailReturn"+end,point,point+Vector3.down*.12f,.042f,metal);
            }
            // A screen faces the ascending/descending stair, above the B1 head clearance.
            float screenWidth=width-.28f,screenHeight=screenWidth/2.40f,bottom=3.05f,top=bottom+screenHeight;
            var display=P.Group(g,"SuspendedDisplay");display.localPosition=new Vector3(0,(bottom+top)/2,-.24f);
            P.Box(display,"RearHousing",new Vector3(0,0,-.06f),new Vector3(screenWidth+.12f,screenHeight+.12f,.18f),casing);
            P.Box(display,"BlackLedFace",new Vector3(0,0,.043f),new Vector3(screenWidth,screenHeight,.025f),black);
            foreach(int side in new[]{-1,1})
            {
                P.Box(display,"SideSilverTrim"+side,new Vector3(side*(screenWidth/2+.027f),0,.035f),new Vector3(.054f,screenHeight+.108f,.065f),metal);
                P.Box(display,"HorizontalSilverTrim"+side,new Vector3(0,side*(screenHeight/2+.027f),.035f),new Vector3(screenWidth,.054f,.065f),metal);
                P.Box(display,"SideSpeaker"+side,new Vector3(side*(width/2-.02f),screenHeight/2-.45f,.09f),new Vector3(.11f,.81f,.16f),black);
                P.Box(display,"MountingBracket"+side,new Vector3(side*(width/2-.035f),0,-.09f),new Vector3(.09f,screenHeight+.15f,.25f),metal);
            }
            // White solid upper gallery guard/fascia beside the stair opening, as in the photo.
            P.Box(g,"UpperGalleryFascia",new Vector3(width/2+.09f,(3.05f+top+.07f)/2,5.51f),new Vector3(.18f,top+.07f-3.05f,11.50f),white);
            P.Box(g,"GalleryTopCoping",new Vector3(width/2+.09f,top+.08f,5.51f),new Vector3(.205f,.026f,11.50f),metal);
            // Local upper-volume enclosure prevents a floating screen against the sky.
            // The 1F floor is 4.50m; use the existing 2.85m storey height for its soffit.
            float roof=4.50f+2.85f;
            P.Box(g,"UpperWestReturn",new Vector3(-width/2-.08f,(2.85f+roof)/2,5.5f),new Vector3(.16f,roof-2.85f,11.6f),white);
            P.Box(g,"AboveDisplayReturn",new Vector3(0,(top+.08f+roof)/2,-1.95f),new Vector3(width+.32f,roof-top-.08f,.14f),white);
            P.Box(g,"GalleryUpperGlazing",new Vector3(width/2+.09f,(top+.10f+roof)/2,5.51f),new Vector3(.018f,roof-top-.10f,11.50f),glass);
            P.Box(g,"GalleryUpperFloor",new Vector3(width/2+1.0f,4.44f,4.85f),new Vector3(1.82f,.12f,13.8f),white);
            P.Box(g,"GalleryOuterReturn",new Vector3(width/2+1.96f,(4.5f+roof)/2,4.85f),new Vector3(.10f,roof-4.5f,13.8f),white);
            P.Box(g,"GalleryFrontReturn",new Vector3(width/2+1.02f,(4.5f+roof)/2,-2.0f),new Vector3(1.90f,roof-4.5f,.10f),white);
            P.Box(g,"UpperRearReturn",new Vector3(.96f,(4.5f+roof)/2,11.72f),new Vector3(width+2.24f,roof-4.5f,.10f),white);
            var navy=P.Mat("BluebellUpperNavySlat",new Color(.025f,.045f,.095f),.20f);
            P.Box(g,"UpperSoffitBacking",new Vector3(.92f,roof+.10f,4.85f),new Vector3(width+2.24f,.08f,13.8f),casing);
            for(int i=0;i<77;i++)P.Box(g,"UpperNavySlat"+i,new Vector3(.92f,roof, -1.96f+i*.18f),new Vector3(width+2.24f,.16f,.08f),navy);
            EditorUtility.SetDirty(metal);
        }
        static void Validate(GameObject root)
        {
            ConvergenceHallMeasuredBluebell.Validate(root);
            var g=root.transform.Find("BluebellStand/MeasuredBluebell/PhotoRailingAndDisplay");
            if(g.GetComponentsInChildren<Collider>().Length!=0||g.GetComponentsInChildren<Light>().Length!=0)throw new Exception("Decorative stair addition changed collision or added a light");
            if(g.Cast<Transform>().Count(t=>t.name.StartsWith("GlassPanel"))!=10)throw new Exception("Expected 10 glass panels");
            var face=g.Find("SuspendedDisplay/BlackLedFace").GetComponent<Renderer>();
            if(face.bounds.min.y<3.0f||face.sharedMaterial.IsKeywordEnabled("_EMISSION"))throw new Exception("Screen clearance/emission mismatch");
            Debug.Log("BLUEBELL_DISPLAY_OK: 10 glass panels, round rails and brackets; silver-framed inactive display above 3m; measured treads and B1 collision retained.");
        }
        [MenuItem("Tools/EEG Wheelchair/Update Bluebell Railing And Display")]
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                string before=Collision(root);ApplyTo(root);Validate(root);
                int count=root.GetComponentsInChildren<Transform>(true).Length;ApplyTo(root);Validate(root);
                if(count!=root.GetComponentsInChildren<Transform>(true).Length||before!=Collision(root))throw new Exception("Duplicate geometry or collision changes");
                PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.FocusBluebellDisplay",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;cam.fieldOfView=73;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;ShaderUtil.allowAsyncCompilation=false;
            var stair=GameObject.Find("ConvergenceHallB1").transform.Find("BluebellStand/MeasuredBluebell");
            var positions=new[]{new Vector3(-1.3f,4.15f,7.4f),new Vector3(-1.1f,3.55f,5.6f),new Vector3(6f,1.6f,-1.5f)};
            var targets=new[]{new Vector3(0,3.4f,-.24f),new Vector3(3f,2.35f,2.8f),new Vector3(1.6f,2.3f,4.2f)};
            var names=new[]{"BluebellDisplayFromStairs","BluebellGlassRailing","BluebellFromB1Corridor"};
            for(int i=0;i<names.Length;i++)
            {
                cam.transform.position=stair.TransformPoint(positions[i]);cam.transform.LookAt(stair.TransformPoint(targets[i]));
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,names[i]+".png");var stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException(names[i]);yield return null;}
            }
        }
    }
}
