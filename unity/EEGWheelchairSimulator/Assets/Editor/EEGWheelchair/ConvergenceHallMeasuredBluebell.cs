using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallMeasuredBluebell
    {
        const string Mats="Assets/Art/Materials/ConvergenceHall/",Meshes="Assets/Art/Meshes/ConvergenceHall/";
        public const float Rise=.45f,Tread=.90f,Landing=2.20f,Depth=11.20f;
        static Material Wood()
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Mats+"BluebellOakTexture.asset");
            if(!texture)
            {
                const int w=1024,h=512;texture=new Texture2D(w,h,TextureFormat.RGBA32,true){name="BluebellOakTexture",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
                var pixels=new Color[w*h];
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                {
                    float u=x/(float)w,v=y/(float)h;int row=Mathf.FloorToInt(v*6);float stagger=(row%2)*.5f;
                    int board=Mathf.FloorToInt(u*4+stagger);float random=Mathf.Repeat(Mathf.Sin(row*72.17f+board*31.83f)*43758.54f,1);
                    float grain=Mathf.Sin(v*890+Mathf.PerlinNoise(u*6,v*9)*24)*.025f+Mathf.PerlinNoise(u*17,v*150)*.055f;
                    float value=.82f+random*.22f+grain;
                    Color c=new Color(.68f*value,.46f*value,.25f*value);
                    if(Mathf.Repeat(v*6,1)<.012f||Mathf.Repeat(u*4+stagger,1)<.006f)c*=.55f;
                    pixels[y*w+x]=c;
                }
                texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,Mats+"BluebellOakTexture.asset");
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(Mats+"BluebellMeasuredOak.mat");
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="BluebellMeasuredOak"};AssetDatabase.CreateAsset(material,Mats+"BluebellMeasuredOak.mat");}
            material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.34f);material.SetFloat("_Metallic",0);EditorUtility.SetDirty(material);return material;
        }
        static Transform Group(Transform parent,string name)
        {var t=parent.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(parent,false);}return t;}
        static Mesh Save(string name,Mesh source)
        {
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Meshes+name+".asset");source.name=name;
            if(!existing){existing=source;AssetDatabase.CreateAsset(existing,Meshes+name+".asset");}
            else{EditorUtility.CopySerialized(source,existing);UnityEngine.Object.DestroyImmediate(source);EditorUtility.SetDirty(existing);}return existing;
        }
        static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material material,bool wood=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());var t=go.transform;t.SetParent(p,false);t.localPosition=at;t.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            if(wood)
            {
                var filter=go.GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
                for(int i=0;i<uv.Length;i++){var world=t.TransformPoint(vertices[i]);uv[i]=Mathf.Abs(normals[i].y)>.5f?new Vector2(world.x/2.4f,world.z/.72f):Mathf.Abs(normals[i].z)>.5f?new Vector2(world.x/2.4f,world.y/.72f):new Vector2(world.z/2.4f,world.y/.72f);}
                mesh.uv=uv;filter.sharedMesh=Save("Bluebell_"+name,mesh);
            }
            return t;
        }
        static void Beam(Transform parent,string name,Vector3 a,Vector3 b,Material material)
        {var d=b-a;var t=Box(parent,name,(a+b)/2,new Vector3(.035f,d.magnitude,.035f),material);t.localRotation=Quaternion.FromToRotation(Vector3.up,d.normalized);}
        public static void ApplyTo(GameObject root)
        {
            var stand=root.transform.Find("BluebellStand");var room=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;
            foreach(Transform t in stand)if(t.name.StartsWith("SeatingTier")||t.name.StartsWith("WalkingStep"))t.gameObject.SetActive(false);
            stand.Find("StandCollision").GetComponent<Renderer>().enabled=false;
            var group=Group(stand,"MeasuredBluebell");foreach(Transform old in group.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            group.position=new Vector3(room.center.x,0,ConvergenceHallB1Setup.Plan(0,728).z);group.rotation=Quaternion.identity;group.localScale=Vector3.one;
            float width=room.size.x,seat=width*.55f,walk=width-seat,seatX=-width/2+seat/2,walkX=width/2-walk/2;
            var wood=Wood();var metal=AssetDatabase.LoadAssetAtPath<Material>(Mats+"Metal.mat");
            for(int i=0;i<10;i++)
            {
                float top=(i+1)*Rise,z=i*Tread+(i>=5?Landing:0),baseY=i<5?0:top-Rise;
                Box(group,"SeatTier"+i,new Vector3(seatX,(baseY+top)/2,z+Tread/2),new Vector3(seat,top-baseY,Tread),wood,true);
            }
            Box(group,"MidLanding",new Vector3(0,2.25f-.09f,4.50f+Landing/2),new Vector3(width,.18f,Landing),wood,true);
            for(int i=0;i<30;i++)
            {
                float top=(i+1)*.15f,z=i*.30f+(i>=15?Landing:0),baseY=i<15?0:top-.15f;
                Box(group,"WalkingTread"+i,new Vector3(walkX,(baseY+top)/2,z+.15f),new Vector3(walk,top-baseY,.30f),wood,true);
                Box(group,"Nosing"+i,new Vector3(walkX,top+.003f,z+.018f),new Vector3(walk,.006f,.035f),metal);
            }
            // Upper stair slab remains above the B112 wall height where the extended run crosses it.
            Vector3 a=new Vector3(walkX,2.15f,6.70f),b=new Vector3(walkX,4.40f,Depth);
            var slab=Box(group,"UpperFlightSlab",(a+b)/2,new Vector3(walk,.12f,(b-a).magnitude),wood,true);slab.localRotation=Quaternion.LookRotation(b-a);
            var clear=AssetDatabase.LoadAssetAtPath<Material>(Mats+"BluebellRailGlass.mat");
            if(!clear){clear=new Material(ConvergenceHallGlassBands.Glass(false)){name="BluebellRailGlass"};clear.SetFloat("_Cull",0);AssetDatabase.CreateAsset(clear,Mats+"BluebellRailGlass.mat");}
            float x=width/2-.035f;var path=new[]{new Vector3(x,0,0),new Vector3(x,2.25f,4.5f),new Vector3(x,2.25f,6.7f),new Vector3(x,4.5f,Depth)};
            for(int j=0;j<3;j++)
            {
                var p0=path[j]+Vector3.up*.10f;var p1=path[j+1]+Vector3.up*.10f;
                var mesh=new Mesh();mesh.vertices=new[]{p0,p1,p1+Vector3.up*.85f,p0+Vector3.up*.85f};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
                var pane=Group(group,"RailGlass"+j);pane.gameObject.AddComponent<MeshFilter>().sharedMesh=Save("BluebellRailGlass"+j,mesh);pane.gameObject.AddComponent<MeshRenderer>().sharedMaterial=clear;pane.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                Beam(group,"Handrail"+j,path[j]+Vector3.up*.95f,path[j+1]+Vector3.up*.95f,metal);
            }
            for(int i=0;i<4;i++)Beam(group,"RailPost"+i,path[i],path[i]+Vector3.up*.95f,metal);
            var blocker=stand.GetComponent<BoxCollider>();var size=blocker.size;size.y=4.5f;blocker.size=size;var centre=blocker.center;centre.y=2.25f;blocker.center=centre;
            ConvergenceHallBluebellInfill.Build(root,group,width);
            ConvergenceHallBluebellDisplay.ApplyTo(root);
        }
        public static void Validate(GameObject root)
        {
            var group=root.transform.Find("BluebellStand/MeasuredBluebell");if(!group)throw new Exception("Measured Bluebell missing.");
            if(group.Cast<Transform>().Count(t=>t.name.StartsWith("SeatTier"))!=10||group.Cast<Transform>().Count(t=>t.name.StartsWith("WalkingTread"))!=30)throw new Exception("Bluebell tier count mismatch.");
            var room=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;
            for(int i=0;i<10;i++)
            {
                var tier=group.Find("SeatTier"+i);if(Mathf.Abs(tier.GetComponent<Renderer>().bounds.max.y-(i+1)*Rise)>.002f||Mathf.Abs(tier.localScale.z-Tread)>.001f)throw new Exception("Measured tier size mismatch.");
                var bounds=tier.GetComponent<Renderer>().bounds;if(bounds.max.z>room.min.z&&bounds.min.y<2.85f)throw new Exception("Extended Bluebell intrudes into B112 below ceiling level.");
            }
            if(Mathf.Abs(group.Find("MidLanding").localScale.z-Landing)>.001f||Mathf.Abs(group.Find("SeatTier9").localPosition.z+Tread/2-Depth)>.001f)throw new Exception("Landing/run depth mismatch.");
            if(group.GetComponentsInChildren<Collider>().Length>0)throw new Exception("Decorative upper Bluebell gained collision.");
            ConvergenceHallBluebellInfill.Validate(root);
            Debug.Log("MEASURED_BLUEBELL_OK: 10 seating tiers 0.45m x 0.90m, 2.20m mid-landing; 30 walking treads, 11.20m depth/4.50m height; upper extension above B112 walls, textured oak.");
        }
    }
}
