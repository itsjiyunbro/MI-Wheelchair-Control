using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallCorridorCeilings
    {
        const float H=2.85f,WhiteY=2.80f,NavyY=H-.065f,Pitch=.18f;
        struct Area
        {
            public float x0,x1,z0,z1;
            public Area(float a,float b,float c,float d){x0=a;x1=b;z0=c;z1=d;}
            public Area(Bounds b){x0=b.min.x;x1=b.max.x;z0=b.min.z;z1=b.max.z;}
            public bool Has(float x,float z)=>x>x0-.00001f&&x<x1+.00001f&&z>z0-.00001f&&z<z1+.00001f;
            public float Size=>(x1-x0)*(z1-z0);
        }
        sealed class Surface
        {
            readonly List<Vector3> v=new List<Vector3>();readonly List<int> t=new List<int>();
            public void Triangle(Vector3 a,Vector3 b,Vector3 c)
            {
                if(Vector3.Cross(b-a,c-a).y>0){var swap=b;b=c;c=swap;}
                int i=v.Count;v.AddRange(new[]{a,b,c});t.AddRange(new[]{i,i+1,i+2});
            }
            public void Quad(float x0,float x1,float z0,float z1,float y)
            {
                if(x1-x0<.0001f||z1-z0<.0001f)return;
                int i=v.Count;v.AddRange(new[]{new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1)});
                t.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            }
            public void Save(Transform parent,string name,Material material)
            {
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var g=P.Group(parent,name);g.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh(name,mesh);
                var r=g.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        static List<Area> whiteAreas,navyAreas;
        static Area Rectangle(float x0,float y0,float x1,float y1)
        {var a=ConvergenceHallB1Setup.Plan(x0,y1);var b=ConvergenceHallB1Setup.Plan(x1,y0);return new Area(a.x,b.x,a.z,b.z);}
        static bool Inside(List<Area> areas,float x,float z)=>areas.Any(a=>a.Has(x,z));
        static void Regions(GameObject root)
        {
            var footprint=new Area(root.transform.Find("MeasuredFloorFinishes/GrayFloorBase").GetComponent<Renderer>().bounds);
            var stone=root.transform.Find("MeasuredFloorFinishes").Cast<Transform>().Where(t=>t.name.StartsWith("Stone")).Select(t=>new Area(t.GetComponent<Renderer>().bounds)).ToList();
            var excluded=root.transform.Find("RoomsAndCores").GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("RoomFloor")).Select(r=>new Area(r.bounds)).ToList();
            // Keep both stair openings and the already-authored entrance/lightwell untouched.
            excluded.Add(Rectangle(632,594,723,728));
            var entry=root.transform.Find("MainEntranceUpgrade");
            float west=ConvergenceHallB1Setup.Plan(632,878).x,east=ConvergenceHallB1Setup.Plan(772,878).x;
            excluded.Add(new Area(west,east,0,8.064f));
            var boundaries=excluded.Concat(stone).Concat(new[]{footprint}).ToArray();
            var xs=boundaries.SelectMany(a=>new[]{a.x0,a.x1}).Where(x=>x>=footprint.x0&&x<=footprint.x1).Select(x=>Mathf.Round(x*10000)/10000).Distinct().OrderBy(x=>x).ToArray();
            var zs=boundaries.SelectMany(a=>new[]{a.z0,a.z1}).Where(z=>z>=footprint.z0&&z<=footprint.z1).Select(z=>Mathf.Round(z*10000)/10000).Distinct().OrderBy(z=>z).ToArray();
            var grid=new int[xs.Length-1,zs.Length-1];
            for(int x=0;x<xs.Length-1;x++)for(int z=0;z<zs.Length-1;z++)
            {
                float cx=(xs[x]+xs[x+1])/2,cz=(zs[z]+zs[z+1])/2;
                grid[x,z]=Inside(excluded,cx,cz)?0:Inside(stone,cx,cz)?2:1;
            }
            whiteAreas=new List<Area>();navyAreas=new List<Area>();
            // Merge equal cells into disjoint rectangles to keep the ceiling geometry compact.
            for(int z=0;z<zs.Length-1;z++)for(int x=0;x<xs.Length-1;x++)
            {
                int kind=grid[x,z];if(kind==0)continue;int xx=x+1,zz=z+1;
                while(xx<xs.Length-1&&grid[xx,z]==kind)xx++;
                while(zz<zs.Length-1&&Enumerable.Range(x,xx-x).All(i=>grid[i,zz]==kind))zz++;
                for(int a=x;a<xx;a++)for(int b=z;b<zz;b++)grid[a,b]=0;
                (kind==1?whiteAreas:navyAreas).Add(new Area(xs[x],xs[xx],zs[z],zs[zz]));
            }
        }
        static bool Fits(List<Area> areas,float x,float z,float half)
        {return new[]{-half,0,half}.All(dx=>new[]{-half,0,half}.All(dz=>Inside(areas,x+dx,z+dz)));}
        static Material white,trim,dark,lamp,metal;
        static Vector3[] B108Arc(GameObject root)
        {
            var filter=root.transform.Find("B108RoundedCorner/ClearTop").GetComponent<MeshFilter>();
            return filter.sharedMesh.vertices.Where((v,i)=>i%4==0).Select(v=>filter.transform.TransformPoint(v)).ToArray();
        }
        static Vector3 ArcCentre(Vector3[] arc)=>new Vector3(arc[arc.Length-1].x,WhiteY,arc[0].z);
        static void FillRoundedCorner(GameObject root,Surface ceiling,Transform parent)
        {
            var arc=B108Arc(root);var centre=ArcCentre(arc);float radius=Vector2.Distance(new Vector2(arc[0].x,arc[0].z),new Vector2(centre.x,centre.z));
            // The classroom ceiling follows its curved floor, while corridor rectangles stop at its bounding box.
            // Fill only the exterior corner, extending 12 mm beneath the glass to close the join.
            Vector3 Inside(int i){var d=arc[i]-centre;d.y=0;return centre+d.normalized*(radius-.012f);}
            Vector3 Outside(int i){var d=arc[i]-centre;d.y=0;return centre+d*(radius/Mathf.Max(Mathf.Abs(d.x),Mathf.Abs(d.z)));}
            var join=new Surface();
            for(int i=0;i<arc.Length-1;i++)
            {
                var a=Inside(i);var b=Inside(i+1);var c=Outside(i);var d=Outside(i+1);
                ceiling.Triangle(a,b,d);ceiling.Triangle(a,d,c);
                // Seal the 38mm height step to the acoustic classroom ceiling in oblique views.
                var highA=a;highA.y=H+.003f;var highB=b;highB.y=H+.003f;
                join.Triangle(a,b,highB);join.Triangle(a,highB,highA);
                join.Triangle(highB,b,a);join.Triangle(highA,highB,a);
            }
            join.Save(parent,"B108CeilingCurveJoin",white);
        }
        static void Downlight(Transform parent,float x,float z,bool slatted,int index)
        {
            var g=P.Group(parent,"RecessedDownlight"+index);g.localPosition=new Vector3(x,slatted?NavyY:WhiteY,z);
            if(slatted)P.Box(g,"SquareRecess",new Vector3(0,.008f,0),new Vector3(.18f,.016f,.16f),metal);
            P.Cylinder(g,"RecessRim",new Vector3(0,-.010f,0),new Vector3(.145f,.007f,.145f),slatted?metal:trim,Quaternion.identity);
            P.Cylinder(g,"LampDiffuser",new Vector3(0,-.018f,0),new Vector3(.116f,.003f,.116f),lamp,Quaternion.identity);
            foreach(var r in g.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
        }
        static void WhiteRoute(Transform parent,float x0,float y0,float x1,float y1,HashSet<string> used,ref int index)
        {
            var a=ConvergenceHallB1Setup.Plan(x0,y0);var b=ConvergenceHallB1Setup.Plan(x1,y1);var d=b-a;float length=d.magnitude;
            for(float at=.80f;at<length-.30f;at+=1.65f)
            {
                var p=a+d.normalized*at;string key=Mathf.RoundToInt(p.x*10)+","+Mathf.RoundToInt(p.z*10);
                if(!Fits(whiteAreas,p.x,p.z,.10f)||!used.Add(key))continue;
                Downlight(parent,p.x,p.z,false,index++);
                if(index%4==0)
                {
                    var centre=p+d.normalized*.70f;
                    if(Fits(whiteAreas,centre.x,centre.z,.31f))
                    {
                        var hatch=P.Group(parent,"CeilingAccessPanel"+index);hatch.localPosition=new Vector3(centre.x,WhiteY-.006f,centre.z);
                        P.Box(hatch,"PanelBorder",Vector3.zero,new Vector3(.60f,.012f,.60f),trim);
                        P.Box(hatch,"WhitePanel",new Vector3(0,-.008f,0),new Vector3(.562f,.006f,.562f),white);
                    }
                }
                else if(index%3==0)
                {
                    var centre=p+d.normalized*.65f;
                    if(Fits(whiteAreas,centre.x,centre.z,.12f))
                    {
                        P.Cylinder(parent,"CircularCeilingSpeaker"+index,new Vector3(centre.x,WhiteY-.008f,centre.z),new Vector3(.22f,.005f,.22f),trim,Quaternion.identity);
                        P.Cylinder(parent,"SpeakerCentre"+index,new Vector3(centre.x,WhiteY-.015f,centre.z),new Vector3(.177f,.003f,.177f),white,Quaternion.identity);
                    }
                }
            }
        }
        public static void ApplyTo(GameObject root)
        {
            Regions(root);var old=root.transform.Find("CorridorCeilings");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var group=P.Group(root.transform,"CorridorCeilings");
            white=P.Mat("CorridorPlainWhiteCeiling",new Color(.91f,.915f,.90f),.06f);
            trim=P.Mat("CorridorCeilingTrim",new Color(.69f,.71f,.70f),.14f);
            dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/EntryCeilingBlack.mat");
            var navy=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/EntryCeilingNavy.mat");
            lamp=P.Mat("CorridorLampDiffuser",new Color(.96f,.96f,.93f),.12f);
            metal=P.Mat("CorridorRecessMetal",new Color(.38f,.40f,.40f),.35f);
            var plaster=new Surface();var backing=new Surface();var slats=new Surface();
            foreach(var a in whiteAreas)plaster.Quad(a.x0,a.x1,a.z0,a.z1,WhiteY);
            FillRoundedCorner(root,plaster,group);
            foreach(var a in navyAreas)
            {
                backing.Quad(a.x0,a.x1,a.z0,a.z1,H-.018f);
                // Same global phase and .18m pitch as the main entrance.
                int first=Mathf.FloorToInt((a.z0-.08f)/Pitch),last=Mathf.CeilToInt((a.z1-.08f)/Pitch);
                for(int i=first;i<=last;i++){float z=.08f+i*Pitch;slats.Quad(a.x0,a.x1,Mathf.Max(a.z0,z),Mathf.Min(a.z1,z+.08f),NavyY);}
            }
            plaster.Save(group,"CorridorWhiteCeiling",white);backing.Save(group,"CorridorNavyCeilingBacking",dark);slats.Save(group,"CorridorNavySlatCeiling",navy);
            var fixtures=P.Group(group,"CeilingFixtures");var used=new HashSet<string>();int index=0;
            WhiteRoute(fixtures,245.5f,878,245.5f,334,used,ref index);
            WhiteRoute(fixtures,258,749.5f,632,749.5f,used,ref index);
            WhiteRoute(fixtures,245.5f,350.5f,374,350.5f,used,ref index);
            WhiteRoute(fixtures,868,766,868,406,used,ref index);
            WhiteRoute(fixtures,772,741,884,741,used,ref index);
            WhiteRoute(fixtures,742,422,884,422,used,ref index);
            for(float z=9.0f;z<40;z+=2.1f)for(float x=-4.8f;x<5;x+=1.75f)
                if(Fits(navyAreas,x,z,.13f))Downlight(fixtures,x,z,true,index++);
            // Native narrow linear fixture above the lounge-side marble corridor wall.
            float edge=ConvergenceHallB1Setup.Plan(772,878).x;
            var bar=P.Group(fixtures,"LoungeWallLinearFixture");bar.localPosition=new Vector3(edge-.09f,H-.26f,ConvergenceHallB1Setup.Plan(772,633).z);
            P.Box(bar,"MetalHousing",Vector3.zero,new Vector3(.055f,.07f,9.5f),metal);
            P.Box(bar,"WhiteDiffuser",new Vector3(-.012f,-.045f,0),new Vector3(.033f,.013f,9.43f),lamp);
            ConvergenceHallLecternLighting.DisableEmission(root);
        }
        static string CollisionState(GameObject root)=>(string)typeof(ConvergenceHallWallSurfaceCleanup).GetMethod("ColliderState",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{root});
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static bool Covers(Transform surface,float x,float z)
        {
            var mesh=surface.GetComponent<MeshFilter>().sharedMesh;var v=mesh.vertices;var indices=mesh.triangles;
            var ray=new Ray(new Vector3(x,0,z),Vector3.up);
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=surface.TransformPoint(v[indices[i]]);var b=surface.TransformPoint(v[indices[i+1]]);var c=surface.TransformPoint(v[indices[i+2]]);
                float cross=(b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x);
                if(Mathf.Abs(cross)<.0000001f)continue;
                float u=((x-a.x)*(c.z-a.z)-(z-a.z)*(c.x-a.x))/cross;
                float w=((b.x-a.x)*(z-a.z)-(b.z-a.z)*(x-a.x))/cross;
                if(u>=-.00001f&&w>=-.00001f&&u+w<=1.00001f)return true;
            }
            return false;
        }
        static bool Blocks(Transform surface,Ray ray,float maximum)
        {
            var mesh=surface.GetComponent<MeshFilter>().sharedMesh;var v=mesh.vertices;var tr=mesh.triangles;
            for(int i=0;i<tr.Length;i+=3)
            {
                var a=surface.TransformPoint(v[tr[i]]);var b=surface.TransformPoint(v[tr[i+1]]);var c=surface.TransformPoint(v[tr[i+2]]);
                var edge1=b-a;var edge2=c-a;var cross=Vector3.Cross(ray.direction,edge2);float determinant=Vector3.Dot(edge1,cross);
                if(Mathf.Abs(determinant)<1e-8f)continue;float inverse=1/determinant;
                var delta=ray.origin-a;float u=Vector3.Dot(delta,cross)*inverse;if(u<-.0001f||u>1.0001f)continue;
                var q=Vector3.Cross(delta,edge1);float w=Vector3.Dot(ray.direction,q)*inverse;if(w<-.0001f||u+w>1.0001f)continue;
                float distance=Vector3.Dot(edge2,q)*inverse;if(distance>=0&&distance<=maximum)return true;
            }
            return false;
        }
        static void Validate(GameObject root)
        {
            Regions(root);var group=root.transform.Find("CorridorCeilings");int checkedPoints=0;
            Check(group&&group.GetComponentsInChildren<Collider>().Length==0&&group.GetComponentsInChildren<Light>().Length==0,"Ceilings changed collision or added artificial lights");
            var plaster=group.Find("CorridorWhiteCeiling");var backing=group.Find("CorridorNavyCeilingBacking");
            foreach(var a in whiteAreas.Concat(navyAreas))for(int i=1;i<4;i++)for(int j=1;j<4;j++)
            {
                float x=Mathf.Lerp(a.x0,a.x1,i/4f),z=Mathf.Lerp(a.z0,a.z1,j/4f);bool isWhite=Inside(whiteAreas,x,z);
                Check(Covers(isWhite?plaster:backing,x,z)&&!Covers(isWhite?backing:plaster,x,z),"Ceiling missing or wrong finish at "+x+","+z);checkedPoints++;
            }
            foreach(var f in new[]{plaster,backing,group.Find("CorridorNavySlatCeiling")})Check(f.GetComponent<MeshFilter>().sharedMesh.normals.All(n=>n.y<-.99f),"Ceiling faces must point down");
            var opening=ConvergenceHallB1Setup.Plan(676,660);
            Check(!Covers(plaster,opening.x,opening.z)&&!Covers(backing,opening.x,opening.z),"Bluebell stair opening covered");
            var arc=B108Arc(root);var centre=ArcCentre(arc);float radius=(new Vector2(arc[0].x-centre.x,arc[0].z-centre.z)).magnitude;
            var roomCeiling=root.transform.Find("ClassroomInteriors/B108/CeilingUnderside");int arcChecks=0;
            for(int i=1;i<arc.Length-1;i++)
            {
                var d=arc[i]-centre;d.y=0;d.Normalize();float far=radius/Mathf.Max(Mathf.Abs(d.x),Mathf.Abs(d.z));
                foreach(float factor in new[]{.15f,.50f,.85f})
                {
                    var p=centre+d*Mathf.Lerp(radius,far,factor);
                    Check(Covers(plaster,p.x,p.z),"B108 curved corridor ceiling hole at arc "+i);arcChecks++;
                }
                var inside=centre+d*(radius-.045f);
                Check(Covers(roomCeiling,inside.x,inside.z),"B108 classroom ceiling does not reach curved wall");arcChecks++;
                var rayOrigin=centre+d*(radius-.20f);rayOrigin.y=(WhiteY+H-.012f)/2;
                Check(Blocks(group.Find("B108CeilingCurveJoin"),new Ray(rayOrigin,d),.40f),"B108 ceiling height step remains open at "+i);arcChecks++;
            }
            Debug.Log($"B108_CURVED_CEILING_OK: {arcChecks} inside/outside curved-wall probes; outside bounding-box corner filled at white corridor height with 12mm concealed overlap.");
            Check(group.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>!m.HasProperty("_EmissionColor")||m.GetColor("_EmissionColor").maxColorComponent==0)),"Ceiling fixtures are emissive");
            ConvergenceHallWallSurfaceCleanup.Validate(root);
            Debug.Log($"CORRIDOR_CEILINGS_OK: {whiteAreas.Sum(a=>a.Size):F2}m2 plain white; {navyAreas.Sum(a=>a.Size):F2}m2 navy extension; {checkedPoints} coverage samples; floor/material boundaries retained; stair void and entrance lightwell preserved; no added collision/light/emission.");
        }
        [MenuItem("Tools/EEG Wheelchair/Apply Photo Corridor Ceilings")]
        public static void Apply()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                string before=CollisionState(root);SessionState.SetString("EEG.WallSurfaceCollision",before);
                ApplyTo(root);Check(CollisionState(root)==before,"Corridor collision changed");Validate(root);
                PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
        }
        public static void ApplyAndCapture()
        {Apply();SessionState.SetBool("EEG.ConvergenceQA.FocusCorridorCeilings",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();}
        public static void ApplyB108AndCapture()
        {Apply();SessionState.SetBool("EEG.ConvergenceQA.FocusB108Ceiling",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();}
        public static IEnumerator CaptureB108Views(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;cam.fieldOfView=63;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            var eyes=new[]{ConvergenceHallB1Setup.Plan(245.5f,742,1.55f),ConvergenceHallB1Setup.Plan(290,699,1.65f),ConvergenceHallB1Setup.Plan(261,732,1.85f)};
            var targets=new[]{ConvergenceHallB1Setup.Plan(265,726,2.80f),ConvergenceHallB1Setup.Plan(265,726,2.82f),ConvergenceHallB1Setup.Plan(265,726,2.80f)};
            string[] names={"B108CurvedCeilingFromCorridor","B108CurvedCeilingFromRoom","B108CeilingJoinDetail"};
            for(int i=0;i<eyes.Length;i++)
            {
                cam.transform.position=eyes[i];cam.transform.LookAt(targets[i]);double ready=EditorApplication.timeSinceStartup+3;
                while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,names[i]+".png");var requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("B108 ceiling screenshot");yield return null;}
            }
            Debug.Log("B108_CEILING_CAPTURE_OK: corridor, classroom and close ceiling join captured.");
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;cam.fieldOfView=68;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            var eyes=new[]{ConvergenceHallB1Setup.Plan(741,746,1.45f),ConvergenceHallB1Setup.Plan(741,579,1.45f),ConvergenceHallB1Setup.Plan(320,749.5f,1.45f),ConvergenceHallB1Setup.Plan(245.5f,732,1.45f),ConvergenceHallB1Setup.Plan(868,691,1.45f),ConvergenceHallB1Setup.Plan(668,749.5f,1.45f)};
            var targets=new[]{ConvergenceHallB1Setup.Plan(741,645,2.04f),ConvergenceHallB1Setup.Plan(741,420,2.00f),ConvergenceHallB1Setup.Plan(520,749.5f,2.0f),ConvergenceHallB1Setup.Plan(245.5f,563,2.0f),ConvergenceHallB1Setup.Plan(868,517,2.0f),ConvergenceHallB1Setup.Plan(540,749.5f,2.0f)};
            string[] names={"NavyMarbleCorridorFromEntry","NavyMainCorridor","WhiteEntryLeftCorridor","WhiteWestCorridor","WhiteEastCorridor","CeilingMaterialTransition"};
            for(int i=0;i<eyes.Length;i++)
            {
                cam.transform.position=eyes[i];cam.transform.LookAt(targets[i]);double ready=EditorApplication.timeSinceStartup+3;
                while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,names[i]+".png");var requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Corridor ceiling screenshot");yield return null;}
            }
            Debug.Log("CORRIDOR_CEILINGS_CAPTURE_OK: marble passage, gray-floor corridors and finish transition captured.");
        }
    }
}
