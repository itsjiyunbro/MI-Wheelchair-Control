using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // Authoring and explicit QA only. No runtime dependency and no replacement of bound Text/Button objects.
    public static class DemoHudPolish
    {
        const string ScenePath = "Assets/Scenes/MainScene.unity";
        static readonly Color Ink = new Color(.91f,.95f,.96f);
        static readonly Color Muted = new Color(.60f,.71f,.76f);
        static readonly Color Teal = new Color(.38f,.82f,.78f);
        static readonly string[] Fields = {"simulationText","connectionText","predictionText","confidenceText","controlText","moveText","steeringText","headingText","startButton","stopButton","resetButton","movement","simulation","receiver"};
        static WheelchairStatusUI Hud => UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();

        [MenuItem("Tools/EEG Wheelchair/Polish Demo HUD")]
        public static void Polish()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=ScenePath) scene=EditorSceneManager.OpenScene(ScenePath);
            var hud=Hud; var data=new SerializedObject(hud);
            var refs=Fields.Select(f=>data.FindProperty(f).objectReferenceValue).ToArray();
            if(refs.Any(o=>o==null)) throw new InvalidOperationException("Existing HUD reference missing; nothing rebuilt.");
            var panel=(RectTransform)hud.transform.Find("StatusPanel");
            Undo.RecordObjects(hud.GetComponentsInChildren<Component>(true),"Polish existing HUD");
            var scaler=hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            Place(panel,24,24,340,622);
            panel.GetComponent<Image>().color=new Color(.035f,.065f,.085f,.94f);
            panel.GetComponent<Image>().raycastTarget=false;
            // All bound objects remain direct children, preserving existing authoring/test paths as well as references.
            var decor=Rect(panel,"HudChrome",0,0,340,622); decor.SetAsFirstSibling();
            ImageRect(decor,"TopAccent",0,0,340,3,Teal);
            ImageRect(decor,"LeftBorder",0,3,1,619,new Color(.35f,.58f,.62f,.40f));
            ImageRect(decor,"RightBorder",339,3,1,619,new Color(.35f,.58f,.62f,.40f));
            ImageRect(decor,"BottomBorder",0,621,340,1,new Color(.35f,.58f,.62f,.40f));
            StaticText(decor,"Kicker","BCI / SIMULATION",20,18,300,20,14,Teal);
            Style(panel.Find("Title").GetComponent<Text>(),20,42,300,31,24,Ink,true);
            panel.Find("Title").GetComponent<Text>().text="EEG WHEELCHAIR";
            StaticText(decor,"Subtitle","CONTROL DASHBOARD",20,77,300,20,14,Muted);
            Card(decor,"SystemCard","SYSTEM",112,100);
            Card(decor,"ModelCard","MODEL",224,112);
            Card(decor,"WheelchairCard","WHEELCHAIR",348,158);
            Row(data,"simulationText",32,143,276,27,20,Ink,true);
            ImageRect(decor,"ConnectionBadge",30,177,280,25,new Color(.13f,.23f,.27f,.9f));
            Row(data,"connectionText",40,177,260,25,15,new Color(.73f,.84f,.87f),false);
            Row(data,"predictionText",32,258,276,32,22,Teal,true);
            Row(data,"confidenceText",32,300,276,25,18,Ink,false);
            Row(data,"controlText",32,381,276,26,19,Ink,false);
            Row(data,"moveText",32,410,276,26,19,Ink,false);
            Row(data,"steeringText",32,439,276,26,19,Ink,false);
            Row(data,"headingText",32,468,276,26,19,Ink,false);
            ButtonStyle((Button)data.FindProperty("startButton").objectReferenceValue,20,522,144,40,new Color(.12f,.39f,.34f));
            ButtonStyle((Button)data.FindProperty("stopButton").objectReferenceValue,176,522,144,40,new Color(.46f,.25f,.19f));
            ButtonStyle((Button)data.FindProperty("resetButton").objectReferenceValue,20,574,300,32,new Color(.16f,.24f,.29f));
            hud.RefreshDisplay();
            data.Update();
            for(int i=0;i<Fields.Length;i++) if(data.FindProperty(Fields[i]).objectReferenceValue!=refs[i]) throw new InvalidOperationException("HUD binding changed: "+Fields[i]);
            foreach(var c in hud.GetComponentsInChildren<Component>(true)) EditorUtility.SetDirty(c);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new IOException("HUD scene save failed.");
            Debug.Log("HUD_POLISH_SAVED: existing Text/Button references and callbacks retained; presentation only.");
        }
        static void Place(RectTransform r,float x,float y,float w,float h)
        {
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.localScale=Vector3.one;
            r.localRotation=Quaternion.identity; r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h);
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=parent.Find(name) as RectTransform;
            if(r==null){var go=new GameObject(name,typeof(RectTransform));go.layer=5;r=(RectTransform)go.transform;r.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(go,"Add HUD decoration");}
            Place(r,x,y,w,h);return r;
        }
        static void ImageRect(Transform p,string n,float x,float y,float w,float h,Color color)
        {
            var r=Rect(p,n,x,y,w,h);var image=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;
        }
        static void StaticText(Transform p,string n,string value,float x,float y,float w,float h,int size,Color color)
        {
            var r=Rect(p,n,x,y,w,h);var text=r.GetComponent<Text>()??r.gameObject.AddComponent<Text>();
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;Style(text,x,y,w,h,size,color,true);
        }
        static void Card(Transform decor,string name,string title,float y,float h)
        {
            var r=Rect(decor,name,20,y,300,h);var image=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();
            image.color=new Color(.085f,.13f,.16f,.95f);image.raycastTarget=false;
            StaticText(r,"SectionTitle",title,12,9,276,19,14,Muted);
            ImageRect(r,"Rule",12,31,276,1,new Color(.25f,.36f,.40f,.7f));
        }
        static void Row(SerializedObject d,string field,float x,float y,float w,float h,int size,Color color,bool bold)
            =>Style((Text)d.FindProperty(field).objectReferenceValue,x,y,w,h,size,color,bold);
        static void Style(Text t,float x,float y,float w,float h,int size,Color color,bool bold)
        {
            Place(t.rectTransform,x,y,w,h);t.fontSize=size;t.color=color;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
            t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Overflow;
            t.verticalOverflow=VerticalWrapMode.Truncate;t.resizeTextForBestFit=false;t.raycastTarget=false;
        }
        static void ButtonStyle(Button button,float x,float y,float w,float h,Color normal)
        {
            Place((RectTransform)button.transform,x,y,w,h);button.image.color=Color.white;
            var c=button.colors;c.normalColor=normal;c.highlightedColor=Color.Lerp(normal,Color.white,.15f);
            c.selectedColor=c.normalColor;c.pressedColor=normal*.72f;c.pressedColor=new Color(c.pressedColor.r,c.pressedColor.g,c.pressedColor.b,1);
            c.disabledColor=new Color(.11f,.14f,.17f,.48f);c.fadeDuration=.10f;button.colors=c;
            var t=button.GetComponentInChildren<Text>();t.fontSize=16;t.fontStyle=FontStyle.Bold;t.color=Ink;t.alignment=TextAnchor.MiddleCenter;
        }
        static string Bindings(){var d=new SerializedObject(Hud);return string.Join(";",Fields.Select(f=>GlobalObjectId.GetGlobalObjectIdSlow(d.FindProperty(f).objectReferenceValue).ToString()));}
        static string Arg(string name){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);return i<0?null:a[i+1];}
        public static void PolishAndValidate()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use batch mode for automated validation.");
            EditorSceneManager.OpenScene(ScenePath);
            string output=Arg("-hudOutputFolder");
            if(!File.Exists(Path.Combine(output,"Before-1920x1080.png"))) Render(Hud.GetComponent<Canvas>(),Path.Combine(output,"Before-1920x1080.png"),1920,1080,false);
            string bindings=Bindings();
            Polish();string once=File.ReadAllText(ScenePath);Polish();
            if(once!=File.ReadAllText(ScenePath))throw new InvalidOperationException("HUD polish not idempotent.");
            EditorSceneManager.OpenScene(ScenePath);
            if(Bindings()!=bindings)throw new InvalidOperationException("HUD component data/reference changed.");
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720),new Vector2Int(1024,768)})
                Render(Hud.GetComponent<Canvas>(),Path.Combine(output,$"After-{size.x}x{size.y}.png"),size.x,size.y,true);
            var movement=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            Vector3 mp=movement.transform.position,cp=follow.transform.position;Quaternion mr=movement.transform.rotation,cr=follow.transform.rotation;
            try
            {
                var poses=new[]{new Vector3(0,.5f,7),new Vector3(-9,.5f,7),new Vector3(-9,.5f,15)};
                var yaws=new[]{270f,0f,0f};var names=new[]{"Left","Right","Goal"};
                for(int i=0;i<poses.Length;i++)
                {
                    movement.transform.SetPositionAndRotation(poses[i],Quaternion.Euler(0,yaws[i],0));follow.SnapToTarget();Hud.RefreshDisplay();
                    Render(Hud.GetComponent<Canvas>(),Path.Combine(output,"Course-"+names[i]+".png"),1600,900,true);
                }
            }
            finally{movement.transform.SetPositionAndRotation(mp,mr);follow.transform.SetPositionAndRotation(cp,cr);}
            EditorSceneManager.OpenScene(ScenePath); // Discard preview-only poses; never save them.
            Debug.Log("HUD_POLISH_QA_OK: bindings, idempotence, reload, 3 requested resolutions + 4:3, course views.");
            Step07PredictionValidation.Begin();
        }
        static void Render(Canvas canvas,string path,int width,int height,bool validate)
        {
            var camera=Camera.main;var scaler=canvas.GetComponent<CanvasScaler>();
            var rt=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;
            float oldPlane=canvas.planeDistance,oldScale=canvas.scaleFactor;bool oldEnabled=scaler.enabled;
            try
            {
                camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                scaler.enabled=false;
                // Same log-average scale used by CanvasScaler MatchWidthOrHeight (match=.5).
                canvas.scaleFactor=Mathf.Sqrt(width/scaler.referenceResolution.x*height/scaler.referenceResolution.y);
                Canvas.ForceUpdateCanvases();
                if(validate)
                {
                    foreach(var text in canvas.GetComponentsInChildren<Text>())
                        if(text.preferredWidth>text.rectTransform.rect.width+.1f||text.preferredHeight>text.rectTransform.rect.height+.1f)
                            throw new InvalidOperationException("Text does not fit: "+text.name+" "+text.preferredWidth);
                    var corners=new Vector3[4];((RectTransform)canvas.transform.Find("StatusPanel")).GetWorldCorners(corners);
                    foreach(var corner in corners){var v=camera.WorldToViewportPoint(corner);if(v.x<0||v.x>1||v.y<0||v.y>1)throw new InvalidOperationException("Panel outside viewport.");}
                }
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;canvas.scaleFactor=oldScale;scaler.enabled=oldEnabled;
                camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
                UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}






namespace EEGWheelchairSimulator.Editor
{
    // Explicit native Game View capture only; no saved Scene or runtime dependency.
    [InitializeOnLoad]
    public static class HudOverlayCapture
    {
        const string Key="EEG.HudOverlayCapture";
        static int stage,frames,lastFrame=-1;
        static bool requested;
        static double deadline;
        static DateTime requestedAt;
        static readonly int[] Widths={1920,1600,1280,1024,1600,1600,1600};
        static readonly int[] Heights={1080,900,720,768,900,900,900};
        static readonly string[] Names={"After-1920x1080.png","After-1600x900.png","After-1280x720.png","After-1024x768.png","Course-Left.png","Course-Right.png","Course-Goal.png"};
        static Type WindowType=>typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeWindow");
        static readonly System.Reflection.BindingFlags Flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
        static void Resolution(uint w,uint h)=>WindowType.GetMethod("SetCustomRenderingResolution",Flags).Invoke(null,new object[]{w,h,"HUD QA"});
        static HudOverlayCapture(){if(SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+80;EditorApplication.update+=Tick;}}
        public static void Begin()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Native QA requires batch mode.");
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            object[] previous={(uint)0,(uint)0};WindowType.GetMethod("GetRenderingResolution",Flags).Invoke(null,previous);
            SessionState.SetInt(Key+"Width",(int)(uint)previous[0]);SessionState.SetInt(Key+"Height",(int)(uint)previous[1]);
            SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        static void Finish(int result)
        {
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
            Resolution((uint)SessionState.GetInt(Key+"Width",1920),(uint)SessionState.GetInt(Key+"Height",1080));
            EditorApplication.Exit(result);
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup>deadline){Debug.LogError("Native HUD capture timed out.");Finish(1);return;}
            if(!EditorApplication.isPlaying||Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            if(frames==0)
            {
                Resolution((uint)Widths[stage],(uint)Heights[stage]);
                if(stage>=4)
                {
                    var m=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
                    var p=stage==4?new Vector3(0,.5f,7):stage==5?new Vector3(-9,.5f,7):new Vector3(-9,.5f,15);
                    m.transform.SetPositionAndRotation(p,Quaternion.Euler(0,stage==4?270:0,0));
                    UnityEngine.Object.FindFirstObjectByType<CameraFollow>().SnapToTarget();
                }
            }
            if(++frames<20)return;
            string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-nativeHudPath");
            string path=Path.Combine(Path.GetDirectoryName(args[i+1]),Names[stage]);
            if(!requested)
            {
                if(Screen.width!=Widths[stage]||Screen.height!=Heights[stage]){Debug.LogError("Unexpected native resolution.");Finish(1);return;}
                requestedAt=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);requested=true;return;
            }
            if(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<requestedAt)return;
            Debug.Log("HUD_NATIVE_CAPTURE_OK: "+Screen.width+"x"+Screen.height+" "+path);
            stage++;frames=0;requested=false;
            if(stage==Names.Length)Finish(0);
        }
    }
}
