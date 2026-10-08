using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallClassroomProps
    {
        public const float FurnitureScale=.95f;
        public const string Assets="Assets/Art/Prefabs/Environment/ConvergenceHall/Classrooms/";
        const string Meshes="Assets/Art/Meshes/ConvergenceHall/",Mats="Assets/Art/Materials/ConvergenceHall/";
        static Material white,steel,dark,blue,meshFabric,display;
        public static void Folder(string path)
        {path=path.TrimEnd('/');if(AssetDatabase.IsValidFolder(path))return;var parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));}
        public static Material Mat(string name,Color color,float smooth=.25f,bool emissive=false)
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(Mats+name+".mat");if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,Mats+name+".mat");}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;
            if(emissive){m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.6f);}
            else{m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;}
            EditorUtility.SetDirty(m);return m;
        }
        public static Transform Group(Transform p,string name)
        {var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
        public static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(p,false);go.transform.localPosition=at;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;
        }
        public static Transform Cylinder(Transform p,string name,Vector3 at,Vector3 size,Material mat,Quaternion rotation)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(p,false);go.transform.localPosition=at;go.transform.localScale=size;go.transform.localRotation=rotation;go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;
        }
        public static void Tube(Transform p,string name,Vector3 a,Vector3 b,float diameter,Material mat)
        {var d=b-a;Cylinder(p,name,(a+b)/2,new Vector3(diameter,d.magnitude/2,diameter),mat,Quaternion.FromToRotation(Vector3.up,d.normalized));}
        public static Mesh SaveMesh(string name,Mesh source)
        {
            var m=AssetDatabase.LoadAssetAtPath<Mesh>(Meshes+name+".asset");source.name=name;
            if(!m){m=source;AssetDatabase.CreateAsset(m,Meshes+name+".asset");}
            else
            {
                // Update the mesh buffers explicitly so repeated layout edits refresh GPU geometry.
                m.Clear();m.indexFormat=source.indexFormat;m.vertices=source.vertices;m.normals=source.normals;m.uv=source.uv;m.subMeshCount=source.subMeshCount;
                for(int i=0;i<source.subMeshCount;i++)m.SetTriangles(source.GetTriangles(i),i,false);
                m.RecalculateBounds();UnityEngine.Object.DestroyImmediate(source);EditorUtility.SetDirty(m);
            }
            return m;
        }
        static void RoundedBox(Transform p,string name,Vector3 at,float w,float d,float h,float radius,Material mat)
        {
            var points=new List<Vector2>();
            for(int corner=0;corner<4;corner++)
            {
                float cx=(corner==0||corner==3?1:-1)*(w/2-radius),cz=(corner<2?1:-1)*(d/2-radius);
                for(int j=0;j<=5;j++){float a=(corner*90+j*18)*Mathf.Deg2Rad;points.Add(new Vector2(cx+Mathf.Cos(a)*radius,cz+Mathf.Sin(a)*radius));}
            }
            var v=new List<Vector3>();var tr=new List<int>();
            void Tri(Vector3 a,Vector3 b,Vector3 c){int i=v.Count;v.AddRange(new[]{a,b,c});tr.AddRange(new[]{i,i+1,i+2});}
            for(int i=0;i<points.Count;i++)
            {
                var a=points[i];var b=points[(i+1)%points.Count];var lo=new Vector3(a.x,-h/2,a.y);var hi=new Vector3(a.x,h/2,a.y);var blo=new Vector3(b.x,-h/2,b.y);var bhi=new Vector3(b.x,h/2,b.y);
                Tri(new Vector3(0,h/2,0),bhi,hi);Tri(new Vector3(0,-h/2,0),lo,blo);Tri(lo,hi,bhi);Tri(lo,bhi,blo);
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var t=Group(p,name);t.localPosition=at;t.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh("Interior_"+name,mesh);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        static void Collider(Transform p,Vector3 centre,Vector3 size)
        {var c=p.gameObject.AddComponent<BoxCollider>();c.center=centre;c.size=size;p.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");}
        static void Wheel(Transform p,string name,float x,float z,bool chair)
        {
            float r=chair?.027f:.035f;
            Box(p,name+"Fork",new Vector3(x,r*2,z),new Vector3(.025f,.045f,.030f),steel);
            Cylinder(p,name,new Vector3(x,r+.010f,z),new Vector3(r*2,.014f,r*2),dark,Quaternion.Euler(0,0,90));
            if(!chair)Cylinder(p,name+"Hub",new Vector3(x+.016f,r+.010f,z),new Vector3(r*1.5f,.003f,r*1.5f),white,Quaternion.Euler(0,0,90));
        }
        static void Desk(Transform p)
        {
            RoundedBox(p,"DeskTop",new Vector3(0,.735f,0),1.50f,.55f,.035f,.045f,white);
            Box(p,"ModestyPanel",new Vector3(0,.455f,.220f),new Vector3(1.27f,.34f,.024f),white);
            Tube(p,"UnderTopCrossbar",new Vector3(-.65f,.68f,0),new Vector3(.65f,.68f,0),.026f,steel);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*.64f;
                Tube(p,"ForwardLeg"+side,new Vector3(x,.685f,-.13f),new Vector3(x,.085f,.23f),.036f,white);
                Tube(p,"RearLeg"+side,new Vector3(x,.685f,.13f),new Vector3(x,.085f,-.23f),.036f,white);
                Cylinder(p,"TiltPivot"+side,new Vector3(x,.68f,-.13f),new Vector3(.075f,.015f,.075f),steel,Quaternion.Euler(0,0,90));
                Wheel(p,"FrontCaster"+side,x,.23f,false);Wheel(p,"RearCaster"+side,x,-.23f,false);
                for(int i=0;i<3;i++)Box(p,"PanelVent"+side+"_"+i,new Vector3(side*.53f,.355f+i*.055f,.235f),new Vector3(.065f,.003f,.002f),dark);
            }
            Collider(p,new Vector3(0,.38f,0),new Vector3(1.50f,.75f,.59f));
        }
        static void Chair(Transform p)
        {ConvergenceHallChairGeometry.Build(p,steel,dark,blue,meshFabric);}
        static void Lectern(Transform p)
        {
            RoundedBox(p,"LecternBase",new Vector3(0,.09f,0),.72f,.58f,.13f,.035f,steel);
            Box(p,"LecternCabinet",new Vector3(0,.58f,0),new Vector3(.63f,.92f,.49f),steel);
            Box(p,"BlackFrontPanel",new Vector3(0,.59f,-.255f),new Vector3(.48f,.77f,.018f),dark);
            RoundedBox(p,"ControlConsole",new Vector3(0,1.075f,0),1.02f,.70f,.09f,.04f,dark);
            Box(p,"SlidingCover",new Vector3(-.245f,1.127f,0),new Vector3(.46f,.012f,.55f),dark);
            Box(p,"IntegratedMonitorFrame",new Vector3(.095f,1.13f,.015f),new Vector3(.34f,.025f,.44f),dark);
            Box(p,"IntegratedMonitor",new Vector3(.095f,1.145f,.015f),new Vector3(.29f,.003f,.37f),display);
            Box(p,"TouchPanelFrame",new Vector3(.395f,1.135f,.18f),new Vector3(.18f,.02f,.17f),steel);
            Box(p,"TouchDisplay",new Vector3(.395f,1.149f,.18f),new Vector3(.14f,.003f,.13f),display);
            Box(p,"Keyboard",new Vector3(0,1.142f,.30f),new Vector3(.36f,.015f,.055f),white);
            for(int i=0;i<16;i++)Box(p,"Key"+i,new Vector3(-.165f+i*.022f,1.152f,.3f),new Vector3(.018f,.004f,.036f),steel);
            RoundedBox(p,"Mouse",new Vector3(.39f,1.144f,-.15f),.055f,.09f,.025f,.02f,dark);
            Tube(p,"MicStemA",new Vector3(.32f,1.14f,0),new Vector3(.30f,1.43f,.11f),.012f,dark);
            Tube(p,"MicStemB",new Vector3(.30f,1.43f,.11f),new Vector3(.17f,1.60f,.18f),.012f,dark);
            Cylinder(p,"Microphone",new Vector3(.17f,1.60f,.18f),new Vector3(.035f,.040f,.035f),dark,Quaternion.Euler(40,0,0));
            for(int i=0;i<3;i++)Cylinder(p,"FrontButton"+i,new Vector3(0,.31f+i*.095f,-.270f),new Vector3(.018f,.003f,.018f),white,Quaternion.Euler(90,0,0));
            ConvergenceHallYonseiSymbol.AddToLectern(p);
            Collider(p,new Vector3(0,.80f,0),new Vector3(1.02f,1.60f,.75f));
        }
        public static void Combine(Transform root,string assetName)
        {
            var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
            var materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();var submeshes=new List<Mesh>();
            foreach(var material in materials)
            {
                var instances=new List<CombineInstance>();
                foreach(var r in renderers)
                {
                    var mesh=r.GetComponent<MeshFilter>().sharedMesh;
                    for(int i=0;i<r.sharedMaterials.Length;i++)if(r.sharedMaterials[i]==material)instances.Add(new CombineInstance{mesh=mesh,subMeshIndex=i,transform=root.worldToLocalMatrix*r.localToWorldMatrix});
                }
                var merged=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};merged.CombineMeshes(instances.ToArray(),true,true);submeshes.Add(merged);
            }
            var combined=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};combined.CombineMeshes(submeshes.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),false,false);combined.RecalculateBounds();
            foreach(var r in renderers)r.enabled=false;
            var t=Group(root,"CombinedVisual");t.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh(assetName,combined);t.gameObject.AddComponent<MeshRenderer>().sharedMaterials=materials;
            foreach(var m in submeshes)UnityEngine.Object.DestroyImmediate(m);
        }
        static void ChairMaterials()
        {
            Folder(Assets);steel=Mat("FurnitureSilver",new Color(.52f,.55f,.56f),.52f);steel.SetFloat("_Metallic",.40f);
            dark=Mat("FurnitureBlack",new Color(.018f,.021f,.025f),.25f);blue=Mat("ChairBlueFabric",new Color(.035f,.35f,.50f),.08f);
            meshFabric=Mat("ChairWovenMesh",new Color(.03f,.035f,.04f),.08f);
        }
        public static void BuildChairAsset()
        {
            ChairMaterials();var go=new GameObject("BlueMeshChair");
            try
            {
                Chair(go.transform);Combine(go.transform,"Interior_BlueMeshChair");
                foreach(Transform child in go.transform.Cast<Transform>().Where(t=>t.name!="CombinedVisual").ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                PrefabUtility.SaveAsPrefabAsset(go,Assets+"BlueMeshChair.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        public static void BuildAssets()
        {
            ChairMaterials();white=Mat("FurnitureWhite",new Color(.91f,.92f,.91f),.40f);display=Mat("LecternScreen",new Color(.16f,.22f,.27f),.35f);
            foreach(string name in new[]{"TwoSeatFoldingDesk","BlueMeshChair","ElectronicLectern"})
            {
                var go=new GameObject(name);try{if(name=="TwoSeatFoldingDesk")Desk(go.transform);else if(name=="BlueMeshChair")Chair(go.transform);else Lectern(go.transform);Combine(go.transform,"Interior_"+name);foreach(Transform child in go.transform.Cast<Transform>().Where(t=>t.name!="CombinedVisual").ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);PrefabUtility.SaveAsPrefabAsset(go,Assets+name+".prefab");}finally{UnityEngine.Object.DestroyImmediate(go);}
            }
        }
        public static Transform Instance(Transform parent,string prefab,string name,Vector3 at)
        {var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Assets+prefab+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);go.name=name;go.transform.localPosition=at;if(prefab=="TwoSeatFoldingDesk"||prefab=="BlueMeshChair")go.transform.localScale=Vector3.one*FurnitureScale;return go.transform;}
    }
}
