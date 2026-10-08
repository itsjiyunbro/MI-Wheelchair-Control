using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallB112LiftWallFinish
    {
        const string MaterialPath="Assets/Art/Materials/ConvergenceHall/B112ExposedConcrete.mat";
        static readonly string[] HallWalls={"LeftReturn","RightReturn","RearWall","FrontLeftPier","FrontRightPier","Lintel"};
        static bool WallFinish(Material m)=>m&&(m.name=="WarmWhite"||m.name=="CoreConcrete"||m.name=="B112ElevatorRoomConcrete"||m.name=="B112ElevatorRoomWhite"||m.name=="B112ExposedConcrete");
        static Material ExposedConcrete()
        {
            const string texturePath="Assets/Art/Materials/ConvergenceHall/B112ConcreteGrain.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(!texture)
            {
                const int size=1024;texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name="B112ConcreteGrain",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
                var pixels=new Color[size*size];var random=new System.Random(112);
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    // Periodic multiscale clouds produce soft cast-concrete mottling without tile borders.
                    float Cloud(float frequency)
                    {
                        float u=x/(float)size,v=y/(float)size;
                        float a=Mathf.PerlinNoise(u*frequency+7.1f,v*frequency+3.4f),b=Mathf.PerlinNoise((u-1)*frequency+7.1f,v*frequency+3.4f);
                        float c=Mathf.PerlinNoise(u*frequency+7.1f,(v-1)*frequency+3.4f),d=Mathf.PerlinNoise((u-1)*frequency+7.1f,(v-1)*frequency+3.4f);
                        return Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v)-.5f;
                    }
                    float value=.88f+Cloud(6)*.18f+Cloud(28)*.09f+Cloud(140)*.035f+((float)random.NextDouble()-.5f)*.032f;
                    pixels[y*size+x]=new Color(value,value,value*.985f,1);
                }
                // Sparse, irregular pores; most of the wall remains a softly mottled concrete surface.
                for(int i=0;i<2100;i++)
                {
                    int cx=random.Next(size),cy=random.Next(size),radius=random.NextDouble()<.87?1:2;
                    float darkness=.16f+(float)random.NextDouble()*.17f;
                    for(int y=-radius;y<=radius;y++)for(int x=-radius;x<=radius;x++)
                    {
                        float distance=x*x+y*y;if(distance>radius*radius)continue;
                        int index=((cy+y+size)%size)*size+(cx+x+size)%size;var colour=pixels[index];float amount=darkness*(1-distance/(radius*radius+1));
                        colour.r-=amount;colour.g-=amount;colour.b-=amount;pixels[index]=colour;
                    }
                }
                texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,texturePath);
            }
            var shader=Shader.Find("ConvergenceHall/B112 Exposed Concrete");if(!shader)throw new Exception("Concrete surface shader missing");
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(!material){material=new Material(shader){name="B112ExposedConcrete"};AssetDatabase.CreateAsset(material,MaterialPath);}else material.shader=shader;
            material.SetColor("_BaseColor",new Color(.82f,.83f,.80f,1));material.SetTexture("_BaseMap",texture);material.SetFloat("_Surface",0);
            material.SetFloat("_TextureMetres",1.6f);material.SetFloat("_PanelWidth",1.6f);material.SetFloat("_PanelHeight",1.15f);material.SetFloat("_JointWidth",.003f);
            EditorUtility.SetDirty(material);return material;
        }
        public static void ApplyTo(GameObject root)
        {
            var finish=ExposedConcrete();
            var hall=root.transform.Find("CoreHalls/B112ElevatorHall");
            // Include the original perimeter surfaces which are exposed beyond the inset returns.
            var original=root.transform.Find("RoomsAndCores/EastLift").GetComponentsInChildren<Renderer>(true);
            var walls=HallWalls.Select(n=>hall.Find(n).GetComponent<Renderer>()).Concat(hall.Find("RecessedWallSurround").GetComponentsInChildren<Renderer>(true)).Concat(original);
            int changed=0;
            foreach(var r in walls.Distinct())
            {
                var materials=r.sharedMaterials;bool dirty=false;
                for(int i=0;i<materials.Length;i++)if(WallFinish(materials[i])){dirty|=materials[i]!=finish;materials[i]=finish;}
                if(dirty){r.sharedMaterials=materials;changed++;}
            }
            Debug.Log("B112_LIFT_WALL_FINISH_APPLIED: "+changed+" wall renderers share photo-based exposed concrete, world-aligned mottling, pores and panel joints.");
        }
        static string CollisionState(GameObject root)=>(string)typeof(ConvergenceHallWallSurfaceCleanup).GetMethod("ColliderState",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{root});
        static string GeometryState(GameObject root)=>string.Join("\n",root.GetComponentsInChildren<MeshFilter>(true).Select(f=>AnimationUtility.CalculateTransformPath(f.transform,root.transform)+"|"+AssetDatabase.GetAssetPath(f.sharedMesh)+"|"+f.transform.localPosition.ToString("F5")+"|"+f.transform.localRotation.ToString("F5")+"|"+f.transform.localScale.ToString("F5")).OrderBy(s=>s));
        static void Validate(GameObject root)
        {
            var hall=root.transform.Find("CoreHalls/B112ElevatorHall");var finish=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            foreach(string name in HallWalls)if(hall.Find(name).GetComponent<Renderer>().sharedMaterial!=finish)throw new Exception("Hall wall finish mismatch: "+name);
            foreach(string name in new[]{"LeftWall","RightWall","OverDoorWall"})if(hall.Find("RecessedWallSurround/"+name).GetComponent<Renderer>().sharedMaterial!=finish)throw new Exception("Elevator rear surround finish mismatch");
            if(finish.shader.name!="ConvergenceHall/B112 Exposed Concrete"||!finish.GetTexture("_BaseMap"))throw new Exception("Concrete texture missing");
            if(root.transform.Find("RoomsAndCores/EastLift").GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>WallFinish(m)&&m!=finish)))throw new Exception("A different perimeter wall finish remains in B112 elevator room");
            foreach(string name in new[]{"LeftWall","RightWall","RearWall"})if(hall.Find("MeasuredCabin/"+name).GetComponent<Renderer>().sharedMaterial.name!="LiftCabinSteel")throw new Exception("Cabin steel changed");
            ConvergenceHallWallSurfaceCleanup.Validate(root);
            Debug.Log("B112_LIFT_WALL_FINISH_OK: matching textured concrete on both side walls, elevator surround and exposed perimeter; white service doors and cabin steel retained; existing wall overlaps zero.");
        }
        [MenuItem("Tools/EEG Wheelchair/Unify B112 Elevator Room Wall Finish")]
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                string collision=CollisionState(root),geometry=GeometryState(root);SessionState.SetString("EEG.WallSurfaceCollision",collision);
                ApplyTo(root);
                if(CollisionState(root)!=collision||GeometryState(root)!=geometry)throw new Exception("Wall material update changed geometry or collision");
                Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
            SessionState.SetBool("EEG.ConvergenceQA.FocusB112LiftWalls",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            var hall=GameObject.Find("ConvergenceHallB1").transform.Find("CoreHalls/B112ElevatorHall");
            var eyes=new[]{new Vector3(0,1.5f,-.75f),new Vector3(.72f,1.5f,.80f),new Vector3(-.72f,1.5f,.80f)};
            var targets=new[]{new Vector3(0,1.40f,3.15f),new Vector3(-1.15f,1.40f,2.8f),new Vector3(1.15f,1.40f,2.8f)};
            string[] names={"B112ElevatorRoomThreeWalls","B112ElevatorRoomLeftWall","B112ElevatorRoomRightWall"};
            for(int i=0;i<eyes.Length;i++)
            {
                cam.fieldOfView=i==0?78:67;cam.transform.position=hall.TransformPoint(eyes[i]);cam.transform.LookAt(hall.TransformPoint(targets[i]));double ready=EditorApplication.timeSinceStartup+3;
                while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,names[i]+".png");var requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("B112 wall finish screenshot");yield return null;}
            }
            Debug.Log("B112_LIFT_WALL_CAPTURE_OK: entrance overview and both side wall views.");
        }
    }
}
