using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallRoomCurves
    {
        const float H=2.85f,R=24f;
        const string MeshFolder="Assets/Art/Meshes/ConvergenceHall";
        static Vector3 P(float x,float y,float h=0)=>ConvergenceHallB1Setup.Plan(x,y,h);
        static Material Mat(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/"+n+".mat");
        static Transform Group(Transform p,string n)
        {var t=p.Find(n);if(!t){t=new GameObject(n).transform;t.SetParent(p,false);}return t;}
        static void Pose(Transform t,Vector3 p,Quaternion q,Vector3 scale)
        {t.position=p;t.rotation=q;var s=t.parent.lossyScale;t.localScale=new Vector3(scale.x/s.x,scale.y/s.y,scale.z/s.z);}
        static void Edge(Transform room,string name,Vector3 a,Vector3 b,Vector3 inward)
        {
            var e=room.Find(name);float length=(b-a).magnitude;
            Pose(e,(a+b)/2+inward*.06f,Quaternion.LookRotation((b-a).normalized),Vector3.one);
            var hit=e.GetComponent<BoxCollider>();hit.size=new Vector3(.12f,H,length);hit.center=new Vector3(0,H/2,0);
            foreach(string n in new[]{"LowerWall","TopCap","Skirting"}){var t=e.Find(n);var s=t.localScale;s.z=length;t.localScale=s;}
        }
        static void Rect(Transform room,float x0,float y0,float x1,float y1)
        {
            Pose(room.Find("RoomFloor"),P((x0+x1)/2,(y0+y1)/2,.003f),Quaternion.identity,new Vector3((x1-x0)*.072f,.006f,(y1-y0)*.072f));
            Edge(room,"West",P(x0,y0),P(x0,y1),Vector3.right);Edge(room,"East",P(x1,y0),P(x1,y1),Vector3.left);
            Edge(room,"North",P(x0,y0),P(x1,y0),Vector3.back);Edge(room,"South",P(x0,y1),P(x1,y1),Vector3.forward);
            room.Find("PlanLabel").position=P((x0+x1)/2,(y0+y1)/2,.025f);
        }
        public static void Prepare(GameObject root)
        {
            var rooms=root.transform.Find("RoomsAndCores");
            Rect(rooms.Find("B114"),884,441.66667f,1054,549.33333f);
            Rect(rooms.Find("B115"),884,549.33333f,1054,657);
            Rect(rooms.Find("EastService"),1054,334,1110,657);
            rooms.Find("EastService2").gameObject.SetActive(false);
            var r=rooms.Find("B108");
            Edge(r,"West",P(258,548),P(258,733-R),Vector3.right);
            Edge(r,"South",P(258+R,733),P(374,733),Vector3.forward);
            BuildCurve(root,r);
        }
        static void Folder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;var parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));}
        static Mesh SaveMesh(string name,List<Vector3> v,List<int> t)
        {
            Folder(MeshFolder);string path=MeshFolder+"/"+name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!m){m=new Mesh{name=name};AssetDatabase.CreateAsset(m,path);}else m.Clear();
            m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);return m;
        }
        static void BuildCurve(GameObject root,Transform room)
        {
            var g=Group(root.transform,"B108RoundedCorner");Pose(g,Vector3.zero,Quaternion.identity,Vector3.one);
            var white=Mat("FrostedTeal");var dark=Mat("Navy");
            const int count=32;float radius=R*.072f;Vector3 centre=P(258+R,733-R);
            Vector3 Point(float angle,float rad,float h)=>centre+new Vector3(-Mathf.Cos(angle)*rad,h,-Mathf.Sin(angle)*rad);
            void Strip(string name,float lo,float hi,float outer,float inner,Material mat)
            {
                var v=new List<Vector3>();var tr=new List<int>();
                for(int i=0;i<=count;i++)
                {float a=i*Mathf.PI/2/count;v.Add(Point(a,outer,lo));v.Add(Point(a,outer,hi));v.Add(Point(a,inner,lo));v.Add(Point(a,inner,hi));}
                for(int i=0;i<count;i++)
                {int k=4*i;tr.AddRange(new[]{k,k+1,k+4,k+1,k+5,k+4,k+2,k+6,k+3,k+3,k+6,k+7,k+1,k+3,k+5,k+3,k+7,k+5});}
                var obj=Group(g,name);if(!obj.GetComponent<MeshFilter>())obj.gameObject.AddComponent<MeshFilter>();if(!obj.GetComponent<MeshRenderer>())obj.gameObject.AddComponent<MeshRenderer>();
                obj.GetComponent<MeshFilter>().sharedMesh=SaveMesh("B108"+name,v,tr);obj.GetComponent<Renderer>().sharedMaterial=mat;obj.GetComponent<Renderer>().shadowCastingMode=mat.HasProperty("_Surface")&&mat.GetFloat("_Surface")==1?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;
            }
            var old=g.Find("CurvedWall");if(old)old.gameObject.SetActive(false);
            Strip("ClearBottom",0,.50f,radius,radius-.018f,ConvergenceHallGlassBands.Glass(false));
            Strip("PrivacyFilm",.50f,2.35f,radius,radius-.018f,ConvergenceHallGlassBands.Glass(true));
            Strip("ClearTop",2.35f,H,radius,radius-.018f,ConvergenceHallGlassBands.Glass(false));
            Strip("CurvedSkirting",0,.03f,radius+.006f,radius-.036f,Mat("Metal"));
            var hits=Group(g,"CollisionSegments");
            for(int i=0;i<count;i++)
            {
                var a=Point(i*Mathf.PI/2/count,radius-.06f,0);var b=Point((i+1)*Mathf.PI/2/count,radius-.06f,0);
                var t=Group(hits,"Segment"+i);t.position=(a+b)/2;t.rotation=Quaternion.LookRotation(b-a);
                var c=t.GetComponent<BoxCollider>();if(!c)c=t.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,H/2,0);c.size=new Vector3(.12f,H,(b-a).magnitude+.002f);t.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");
            }
            // Replace the rectangular floor by its rounded footprint, keeping its bounding box for door placement.
            var f=room.Find("RoomFloor");var points=new List<Vector3>{P(258,548,.006f),P(374,548,.006f),P(374,733,.006f)};
            for(int i=count;i>=0;i--)points.Add(Point(i*Mathf.PI/2/count,radius,.006f));
            var verts=new List<Vector3>{f.InverseTransformPoint(P(316,630,.006f))};verts.AddRange(points.Select(f.InverseTransformPoint));
            var triangles=new List<int>();for(int i=0;i<points.Count;i++)triangles.AddRange(new[]{0,1+i,1+(i+1)%points.Count});
            f.GetComponent<MeshFilter>().sharedMesh=SaveMesh("B108RoundedFloor",verts,triangles);
        }
        static void Validate(GameObject root)
        {
            var rooms=root.transform.Find("RoomsAndCores");
            foreach(string n in new[]{"B113","B114","B115"})
            {var b=rooms.Find(n+"/RoomFloor").GetComponent<Renderer>().bounds;if(Mathf.Abs(b.max.x-P(1054,0).x)>.003f)throw new Exception("Classroom rear boundary mismatch: "+n);}
            if(rooms.Find("EastService2").gameObject.activeSelf)throw new Exception("Obsolete rear partition remains.");
            var floor=rooms.Find("B108/RoomFloor").GetComponent<MeshFilter>();if(floor.sharedMesh.vertexCount<30)throw new Exception("Rounded floor missing.");
            Physics.SyncTransforms();
            var rear=rooms.Find("EastService/RoomFloor").GetComponent<Renderer>().bounds;
            if(Mathf.Abs(rear.max.z-P(0,334).z)>.003f||Mathf.Abs(rear.min.z-P(0,657).z)>.003f)throw new Exception("Rear space does not span all three rooms.");
            if(Physics.Raycast(P(1082,340,1),Vector3.back,(657-346)*.072f,1<<LayerMask.NameToLayer("WheelchairObstacle")))throw new Exception("Partition obstructs continuous rear space.");
            var centre=P(282,709,1);
            for(int i=1;i<32;i++)
            {float a=i*Mathf.PI/2/32;var outward=new Vector3(-Mathf.Cos(a),0,-Mathf.Sin(a));if(!Physics.Raycast(centre+outward*2.2f,-outward,out var hit,1f,1<<LayerMask.NameToLayer("WheelchairObstacle"))||!hit.transform.IsChildOf(root.transform.Find("B108RoundedCorner")))throw new Exception("Curved collision gap: "+i);}
            Debug.Log("ROOM_CURVES_OK: B113/B114/B115 restored to equal widths; one continuous rear space; B108 rounded floor/wall and 31 collision probes passed.");
        }
        [MenuItem("Tools/EEG Wheelchair/Join East Rear Space And Round B108")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ConvergenceHallWallMaterials.ApplyTo(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
        }
        public static void ApplyAndValidate()
        {Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Duplicate correction objects.");ConvergenceHallB1Validation.BuildAndValidate();}
    }
}



