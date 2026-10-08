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
    public static class ConvergenceHallB112DiagonalScreen
    {
        // Estimated from the ceiling cassette in the B112 photograph, relative to the front wall.
        const float Yaw=12f,ScreenWidthRatio=.54f;
        static Material Load(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/"+name+".mat");
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Remove(Transform p,string name){var t=p.Find(name);if(t)UnityEngine.Object.DestroyImmediate(t.gameObject);}
        static void Part(Transform p,string name,Vector3 position,Vector3 size,Material material)
        {
            var t=p.Find(name);if(!t)t=P.Box(p,name,position,size,material);
            t.localPosition=position;t.localRotation=Quaternion.identity;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=material;
        }
        static string ColliderState(GameObject root)=>string.Join("\n",root.GetComponentsInChildren<Collider>(true).Select(c=>AnimationUtility.CalculateTransformPath(c.transform,root.transform)+"|"+EditorJsonUtility.ToJson(c)+"|"+c.transform.position.ToString("R")+"|"+c.transform.rotation.ToString("R")+"|"+c.transform.lossyScale.ToString("R")));
        static void Recombine(Transform room)
        {
            Remove(room,"CombinedVisual");Remove(room,"CeilingFittingsVisual");
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())r.enabled=true;
            var fittings=room.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.name!="CeilingUnderside"&&room.InverseTransformPoint(r.bounds.min).y>2.3f).ToArray();
            foreach(var r in fittings)r.enabled=false;
            P.Combine(room,"Interior_B112Combined");var interior=room.Find("CombinedVisual").GetComponent<MeshRenderer>();interior.enabled=false;
            foreach(var r in fittings)r.enabled=true;
            P.Combine(room,"Interior_B112CeilingFittings");var ceiling=room.Cast<Transform>().Last(t=>t.name=="CombinedVisual");ceiling.name="CeilingFittingsVisual";ceiling.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;interior.enabled=true;
        }
        public static void ApplyTo(GameObject root,bool recombine=true)
        {
            var room=root.transform.Find("ClassroomInteriors/B112");var projection=room.Find("ProjectionSystem");
            var floor=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;float width=floor.size.x*ScreenWidthRatio-.14f;float clothHeight=width*9f/16f,borderHeight=clothHeight+.165f;float screenY=2.7925f-borderHeight/2f,caseY=2.805f-screenY;
            projection.localPosition=new Vector3(0,screenY,floor.size.z/2-.72f);projection.localRotation=Quaternion.Euler(0,Yaw,0);
            var black=Load("FurnitureBlack");var white=Load("B112PhotoWhitePaint");
            var canvas=P.Mat("B112MatteScreenCloth",new Color(.93f,.935f,.91f),.05f);
            Part(projection,"ScreenBorder",Vector3.zero,new Vector3(width+.14f,borderHeight,.022f),black);
            Part(projection,"ScreenSurface",new Vector3(0,-.045f,-.016f),new Vector3(width,clothHeight,.006f),canvas);
            Part(projection,"ScreenRoller",new Vector3(0,caseY,0),new Vector3(width+.21f,.065f,.17f),white);
            Part(projection,"CassetteDarkSlot",new Vector3(0,caseY-.037f,0),new Vector3(width+.09f,.009f,.06f),black);
            Part(projection,"ScreenBottomWeight",new Vector3(0,-borderHeight/2f,-.006f),new Vector3(width+.17f,.028f,.037f),black);
            foreach(int side in new[]{-1,1})Part(projection,"CassetteEndCap"+side,new Vector3(side*(width/2+.10f),caseY,0),new Vector3(.026f,.070f,.18f),white);
            var fittings=room.Find("PhotoPresentationDetails");
            // The photo has a board continuing behind the smaller screen, visible on both sides.
            var frontBoard=fittings?fittings.Find("FrontPhotoBoard"):null;
            if(frontBoard)
            {
                float boardWidth=floor.size.x*.75f;
                foreach(string name in new[]{"SilverRim","WritingSurface","MarkerTray"})
                {var part=frontBoard.Find(name);var size=part.localScale;size.x=boardWidth-(name=="WritingSurface"?.028f:0);part.localScale=size;}
            }
            if(fittings)for(int col=0;col<3;col++)foreach(string name in new[]{"LightHousing6_"+col,"LightDiffuser6_"+col})
            {var light=fittings.Find(name);if(light){var at=light.localPosition;at.z=floor.size.z/2-2.0f;light.localPosition=at;}}
            if(fittings)foreach(int side in new[]{-1,1})foreach(string name in new[]{"VentRim"+side+"_5","VentCore"+side+"_5"})
            {var vent=fittings.Find(name);if(vent){var at=vent.localPosition;at.z=floor.size.z/2-1.65f;vent.localPosition=at;}}
            if(recombine){Recombine(room);Validate(root);}
        }
        public static void Validate(GameObject root)
        {
            var room=root.transform.Find("ClassroomInteriors/B112");var projection=room.Find("ProjectionSystem");var floor=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;
            Check(Quaternion.Angle(projection.localRotation,Quaternion.Euler(0,Yaw,0))<.01f,"Screen cassette is not at the photo diagonal");
            Check(Mathf.Abs(Vector3.Dot(projection.forward,Vector3.up))<.0001f,"Lowered screen is not vertical");
            var roller=projection.Find("ScreenRoller");float top=room.InverseTransformPoint(roller.TransformPoint(Vector3.up*.5f)).y;
            Check(Mathf.Abs(top-2.8375f)<.001f,"Screen cassette does not meet the ceiling");
            float outerWidth=projection.Find("ScreenBorder").localScale.x;
            Check(Mathf.Abs(outerWidth/floor.size.x-ScreenWidthRatio)<.0001f,"Screen is not slightly wider than half the room");
            var rearBoard=room.Find("PhotoPresentationDetails/FrontPhotoBoard");
            Check(rearBoard&&rearBoard.Find("SilverRim").localScale.x>outerWidth+.8f,"Board is hidden behind screen width");
            var black=Load("FurnitureBlack");Check(projection.Find("ScreenBorder").GetComponent<Renderer>().sharedMaterial==black&&projection.Find("ScreenBottomWeight").GetComponent<Renderer>().sharedMaterial==black,"Photo black borders missing");
            var screenBounds=projection.Find("ScreenBorder").GetComponent<Renderer>().bounds;
            Check(screenBounds.min.x>floor.min.x+.10f&&screenBounds.max.x<floor.max.x-.10f&&screenBounds.max.z<floor.max.z-.12f,"Diagonal screen intersects room perimeter");
            Check(projection.GetComponentsInChildren<Collider>().Length==0&&projection.GetComponentsInChildren<Light>().Length==0,"Screen added collision or a light effect");
            Check(projection.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).All(m=>!m.IsKeywordEnabled("_EMISSION")),"Screen glow introduced");
            var fabricBox=new Bounds(Vector3.zero,projection.Find("ScreenBorder").localScale);
            foreach(var c in room.GetComponentsInChildren<BoxCollider>())
            {
                var local=new Bounds();bool first=true;
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                {var point=projection.InverseTransformPoint(c.transform.TransformPoint(c.center+Vector3.Scale(c.size*.5f,new Vector3(x,y,z))));if(first){local=new Bounds(point,Vector3.zero);first=false;}else local.Encapsulate(point);}
                Check(!fabricBox.Intersects(local),"Screen intersects furniture: "+c.name);
            }
            var seating=room.Find("Seating");Check(seating.Cast<Transform>().Count(t=>t.name.StartsWith("Desk_"))==21&&seating.Cast<Transform>().Count(t=>t.name.StartsWith("Chair_"))==42,"B112 furniture changed");
        }
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                string before=ColliderState(root);ApplyTo(root);int count=root.GetComponentsInChildren<Transform>(true).Length;ApplyTo(root);
                Check(count==root.GetComponentsInChildren<Transform>(true).Length,"Repeated screen update duplicated geometry");Check(before==ColliderState(root),"Screen update changed collision state");
                PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
            SessionState.SetBool("EEG.ConvergenceQA.FocusB112DiagonalScreen",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var root=GameObject.Find("ConvergenceHallB1");Validate(root);var room=root.transform.Find("ClassroomInteriors/B112");var projection=room.Find("ProjectionSystem");
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>().transform.position=new Vector3(180,.5f,180);
            float z=projection.localPosition.z;
            var eyes=new[]{new Vector3(-.15f,1.45f,z-3.55f),new Vector3(-1.1f,1.90f,z-2.15f),new Vector3(1.65f,1.55f,z-2.65f)};
            var targets=new[]{new Vector3(0,1.82f,z),new Vector3(.20f,2.66f,z),new Vector3(0,2.04f,z)};
            var names=new[]{"B112DiagonalScreen","B112CeilingCassette","B112ScreenSideView"};
            for(int i=0;i<eyes.Length;i++)
            {
                cam.fieldOfView=i==1?68:66;cam.transform.position=room.TransformPoint(eyes[i]);cam.transform.LookAt(room.TransformPoint(targets[i]));
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,names[i]+".png");DateTime stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException(path);yield return null;}
            }
            Debug.Log("B112_DIAGONAL_SCREEN_OK: 54 percent room-width screen with proportional 16:9 fabric and wider visible rear board; photo-estimated 12 degree cassette, vertical lowered fabric, ceiling junction, black border/weight, furniture clearance, identical collider state and duplicate-free rebuild; three screenshots saved.");
        }
    }
}
