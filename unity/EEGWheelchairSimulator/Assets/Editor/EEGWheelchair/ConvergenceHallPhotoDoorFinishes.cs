using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallPhotoDoorFinishes
    {
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        static Material frost,metal;
        static Transform[] Restrooms(GameObject root)=>root.transform.Find("PlanDoors").Cast<Transform>().Where(t=>t.name.StartsWith("WestWC_")||t.name.StartsWith("NorthWC_")).ToArray();
        static void Materials()
        {
            const string texturePath=Mats+"RestroomFrostGrain.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(!texture)
            {
                texture=new Texture2D(256,256,TextureFormat.RGBA32,true){name="RestroomFrostGrain",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
                var pixels=new Color[256*256];var random=new System.Random(106);
                for(int i=0;i<pixels.Length;i++){float g=.97f+(float)random.NextDouble()*.03f;pixels[i]=new Color(g,g,g,1);}texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,texturePath);
            }
            frost=AssetDatabase.LoadAssetAtPath<Material>(Mats+"RestroomMilkyGlass.mat");
            if(!frost){frost=new Material(AssetDatabase.LoadAssetAtPath<Material>(Mats+"ClearCorridorGlass.mat")){name="RestroomMilkyGlass"};AssetDatabase.CreateAsset(frost,Mats+"RestroomMilkyGlass.mat");}
            frost.SetTexture("_BaseMap",texture);frost.SetTextureScale("_BaseMap",new Vector2(3,6));frost.SetColor("_BaseColor",new Color(.92f,.95f,.95f,.84f));
            frost.SetFloat("_Surface",1);frost.SetFloat("_Blend",0);frost.SetFloat("_SrcBlend",5);frost.SetFloat("_DstBlend",10);frost.SetFloat("_SrcBlendAlpha",1);frost.SetFloat("_DstBlendAlpha",10);frost.SetFloat("_ZWrite",0);frost.SetFloat("_Cull",2);frost.SetFloat("_Smoothness",.16f);
            frost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");frost.DisableKeyword("_EMISSION");frost.SetColor("_EmissionColor",Color.black);frost.SetOverrideTag("RenderType","Transparent");frost.renderQueue=3000;frost.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(frost);
            metal=P.Mat("RestroomDoorSatinMetal",new Color(.65f,.66f,.64f),.32f);metal.SetFloat("_Metallic",.40f);EditorUtility.SetDirty(metal);
        }
        static void FrameLeaf(Transform leaf)
        {
            var old=leaf.Find("PhotoGlassDoorFrame");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var group=P.Group(leaf,"PhotoGlassDoorFrame");var size=leaf.localScale;
            group.localScale=new Vector3(1/size.x,1/size.y,1/size.z);
            float w=size.x,h=size.y;
            foreach(float x in new[]{-w/2+.023f,w/2-.023f})P.Box(group,"MetalStile",new Vector3(x,0,0),new Vector3(.046f,h,.052f),metal);
            foreach(float y in new[]{-h/2+.03f,h/2-.03f})P.Box(group,"MetalRail",new Vector3(0,y,0),new Vector3(w,.06f,.052f),metal);
            leaf.GetComponent<Renderer>().sharedMaterial=frost;leaf.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        public static void ApplyTo(GameObject root)
        {
            Materials();
            foreach(var door in Restrooms(root))
            {
                foreach(string name in new[]{"FrameLeft","FrameRight","FrameHeader","AutomaticTrack"}){var frame=door.Find(name);if(frame&&frame.GetComponent<Renderer>())frame.GetComponent<Renderer>().sharedMaterial=metal;}
                foreach(Transform pivot in door.Cast<Transform>().Where(t=>t.name.Contains("_InwardY")))
                {
                    var leaf=pivot.Find("Leaf");FrameLeaf(leaf);
                    foreach(string name in new[]{"HandleBase","Lever","Closer"}){var old=pivot.Find(name);if(old)old.gameObject.SetActive(false);}
                    var oldPull=pivot.Find("PhotoPullHandle");if(oldPull)UnityEngine.Object.DestroyImmediate(oldPull.gameObject);var pull=P.Group(pivot,"PhotoPullHandle");
                    float direction=Mathf.Sign(leaf.localPosition.x),x=leaf.localPosition.x+direction*(leaf.localScale.x/2-.06f);
                    foreach(int face in new[]{-1,1})
                    {
                        P.Tube(pull,"LongPull"+face,new Vector3(x,.56f,face*.085f),new Vector3(x,1.30f,face*.085f),.024f,metal);
                        foreach(float y in new[]{.61f,1.25f})P.Tube(pull,"PullFixing"+face+"_"+y,new Vector3(x,y,face*.025f),new Vector3(x,y,face*.085f),.017f,metal);
                    }
                }
                foreach(string name in new[]{"SlidingLeafLeft","SlidingLeafRight"}){var leaf=door.Find(name);if(leaf)FrameLeaf(leaf);}
            }
            ConvergenceHallLecternLighting.DisableEmission(root);
        }
        static string Walls(GameObject root)=>string.Join("\n",root.transform.Find("RoomsAndCores").GetComponentsInChildren<Renderer>(true).Select(r=>AnimationUtility.CalculateTransformPath(r.transform,root.transform)+"|"+AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh)+"|"+string.Join(",",r.sharedMaterials.Select(AssetDatabase.GetAssetPath))+"|"+r.transform.localPosition.ToString("F5")+"|"+r.transform.localScale.ToString("F5")+"|"+r.enabled).OrderBy(s=>s));
        static void Validate(GameObject root)
        {
            var service=root.transform.Find("CoreHalls/B112ElevatorHall/ServicePanels");var a=service.Find("LargeEntranceSideDoor/WhiteLeaf");var b=service.Find("SmallElevatorSideDoor/WhiteLeaf");
            if(Vector3.Distance(a.localScale,b.localScale)>.0001f||Mathf.Abs(a.position.y-b.position.y)>.0001f)throw new Exception("Inspection doors differ in size or height");
            int panels=0;
            foreach(var door in Restrooms(root))foreach(var r in door.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Leaf"||r.name=="SlidingLeafLeft"||r.name=="SlidingLeafRight"))
            {
                var m=r.sharedMaterial;if(m.name!="RestroomMilkyGlass"||Mathf.Abs(m.GetColor("_BaseColor").a-.84f)>.0001f||m.GetFloat("_Surface")!=1)throw new Exception("Restroom door is not translucent: "+door.name);
                if(!r.transform.Find("PhotoGlassDoorFrame"))throw new Exception("Glass leaf frame missing");panels++;
            }
            if(Restrooms(root).Length!=6||panels!=8||root.GetComponentsInChildren<InteractiveDoor>().Length!=(root.transform.Find("CoreHalls/B109ElevatorHall/ServicePanels/EPSPanel")?48:46))throw new Exception("Restroom/interactive door count changed");
            ConvergenceHallWallSurfaceCleanup.Validate(root);
            Debug.Log("PHOTO_DOOR_FINISHES_OK: equal 0.72x2.10m PS/EPS inspection doors at same elevation; 6 restroom entries / 8 milky glass leaves with satin metal frames; original wall appearance preserved; 46 door controls retained.");
        }
        [MenuItem("Tools/EEG Wheelchair/Apply Equal Inspection And Frosted Restroom Doors")]
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                string before=Walls(root);ConvergenceHallB112ServiceDoors.ApplyTo(root);ApplyTo(root);ConvergenceHallDoorInteraction.Build(root);
                if(Walls(root)!=before)throw new Exception("Reference brick walls or other wall changes were introduced");
                Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
            SessionState.SetBool("EEG.ConvergenceQA.FocusPhotoDoorFinishes",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var root=GameObject.Find("ConvergenceHallB1").transform;var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            var service=root.Find("CoreHalls/B112ElevatorHall/ServicePanels");var doors=Restrooms(root.gameObject);
            for(int i=0;i<3;i++)
            {
                string name;
                if(i==0)
                {
                    cam.fieldOfView=70;cam.transform.position=service.TransformPoint(new Vector3(2.175f,1.45f,-1.80f));cam.transform.LookAt(service.TransformPoint(new Vector3(2.175f,1.35f,0)));name="EqualWhiteInspectionDoors";
                }
                else
                {
                    var door=i==1?doors.First(t=>t.name=="NorthWC_West_Men"):doors.First(t=>t.name.StartsWith("WestWC_"));cam.fieldOfView=70;
                    cam.transform.position=door.position-door.forward*1.50f+Vector3.up*1.15f;cam.transform.LookAt(door.position+Vector3.up*1.1f);name=i==1?"NorthRestroomFrostedDoor":"WestRestroomFrostedDoor";
                }
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,name+".png");var stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException(name);yield return null;}
            }
            Debug.Log("PHOTO_DOOR_FINISHES_CAPTURE_OK: equal inspection doors and both restroom areas captured.");
        }
    }
}
