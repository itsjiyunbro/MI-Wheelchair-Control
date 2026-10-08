using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallWallSurfaceCleanup
    {
        const float PlaneTolerance=.0001f,MinArea=.0001f;
        const string MeshFolder="Assets/Art/Meshes/ConvergenceHall/",SourceFile=MeshFolder+"WallSurfaceSources.json";
        class Vertex {public Vector3 local,world,normal;public Vector2 uv;public Vertex Lerp(Vertex b,float t)=>new Vertex{local=Vector3.Lerp(local,b.local,t),world=Vector3.Lerp(world,b.world,t),normal=Vector3.Lerp(normal,b.normal,t).normalized,uv=Vector2.Lerp(uv,b.uv,t)};}
        class Triangle {public int sub,axis;public float plane;public Vector3 normal;public Vertex[] vertices;}
        class Surface {public MeshFilter filter;public Renderer renderer;public string path,material;public int axis,sign,sub;public float plane;public List<Triangle> triangles=new List<Triangle>();}
        class Pair {public Surface a,b,owner,loser;public float area;}
        class Rule {public int sub;public Triangle cutter;}
        [Serializable] class Source {public string path,asset,name;public bool cube,enabled;}
        [Serializable] class Sources {public List<Source> items=new List<Source>();}
        static Mesh cube;
        static Mesh Cube(){if(cube)return cube;var g=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=g.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(g);return cube;}
        static string PathOf(Transform t,Transform root){var parts=new List<string>();while(t!=root&&t){parts.Add(t.name);t=t.parent;}parts.Reverse();return string.Join("/",parts);}
        static bool Eligible(Renderer r)
        {
            string n=r.name.ToLowerInvariant();return n.Contains("wall")||n.Contains("pier")||n.Contains("lintel")||n.Contains("transom")||n.Contains("header")||n=="leftreturn"||n=="rightreturn"||n.Contains("floor")||n.Contains("ceiling");
        }
        static Vector2 Project(Vector3 p,int axis)=>axis==0?new Vector2(p.z,p.y):axis==1?new Vector2(p.x,p.z):new Vector2(p.x,p.y);
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static float Area(List<Vertex> p,int axis){if(p.Count<3)return 0;float a=0;var origin=Project(p[0].world,axis);for(int i=1;i<p.Count-1;i++)a+=Mathf.Abs(Cross(Project(p[i].world,axis)-origin,Project(p[i+1].world,axis)-origin))*.5f;return a;}
        static List<Vertex> Clip(List<Vertex> polygon,Vector2 a,Vector2 b,int axis,bool inside)
        {
            var result=new List<Vertex>();if(polygon.Count==0)return result;var edge=b-a;
            float Distance(Vertex v)=>Cross(edge,Project(v.world,axis)-a)*(inside?1:-1);
            var previous=polygon[polygon.Count-1];float pd=Distance(previous);bool pin=pd>=-1e-7f;
            foreach(var current in polygon)
            {
                float cd=Distance(current);bool cin=cd>=-1e-7f;
                if(pin!=cin)result.Add(previous.Lerp(current,Mathf.Clamp01(pd/(pd-cd))));
                if(cin)result.Add(current);previous=current;pd=cd;pin=cin;
            }
            return result;
        }
        static Vector2[] Cutter(Triangle t)
        {
            var p=t.vertices.Select(v=>Project(v.world,t.axis)).ToArray();if(Cross(p[1]-p[0],p[2]-p[0])<0)Array.Reverse(p);return p;
        }
        static float Intersection(Triangle a,Triangle b)
        {
            var p=a.vertices.ToList();var cut=Cutter(b);for(int i=0;i<3;i++)p=Clip(p,cut[i],cut[(i+1)%3],a.axis,true);return Area(p,a.axis);
        }
        static List<List<Vertex>> Subtract(List<Vertex> polygon,Triangle cutter,int axis)
        {
            var output=new List<List<Vertex>>();var remaining=polygon;var cut=Cutter(cutter);
            for(int i=0;i<3&&remaining.Count>0;i++)
            {
                var outside=Clip(remaining,cut[i],cut[(i+1)%3],axis,false);if(Area(outside,axis)>1e-8f)output.Add(outside);
                remaining=Clip(remaining,cut[i],cut[(i+1)%3],axis,true);
            }
            return output;
        }
        static List<Surface> Scan(GameObject root)
        {
            var output=new List<Surface>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var r=filter.GetComponent<Renderer>();var mesh=filter.sharedMesh;if(!r||!r.enabled||!mesh||!mesh.isReadable||!Eligible(r))continue;
                var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var groups=new Dictionary<string,Surface>();
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    var mat=r.sharedMaterials[Mathf.Min(sub,r.sharedMaterials.Length-1)];if(!mat||(mat.HasProperty("_Surface")&&mat.GetFloat("_Surface")>0.5f))continue;
                    int[] indices=mesh.GetTriangles(sub);
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var triangle=new Vertex[3];for(int j=0;j<3;j++){int index=indices[i+j];triangle[j]=new Vertex{local=vertices[index],world=filter.transform.TransformPoint(vertices[index]),normal=normals.Length==vertices.Length?normals[index]:Vector3.up,uv=uv.Length==vertices.Length?uv[index]:Vector2.zero};}
                        var cross=Vector3.Cross(triangle[1].world-triangle[0].world,triangle[2].world-triangle[0].world);if(cross.magnitude<1e-8f)continue;var n=cross.normalized;
                        int axis=Mathf.Abs(n.x)>.9999f?0:Mathf.Abs(n.y)>.9999f?1:Mathf.Abs(n.z)>.9999f?2:-1;if(axis<0)continue;
                        float plane=triangle[0].world[axis];int sign=n[axis]>0?1:-1;string key=axis+":"+sign+":"+Mathf.RoundToInt(plane*1000)+":"+sub;
                        if(!groups.TryGetValue(key,out var surface)){surface=new Surface{filter=filter,renderer=r,path=PathOf(filter.transform,root.transform),material=mat.name,axis=axis,sign=sign,sub=sub,plane=plane};groups[key]=surface;output.Add(surface);}
                        surface.triangles.Add(new Triangle{sub=sub,axis=axis,plane=plane,normal=n,vertices=triangle});
                    }
                }
            }
            return output;
        }
        static int Priority(Surface s)=>s.path.StartsWith("CoreHalls/")?100:s.path.StartsWith("PlanDoors/")?95:s.path.StartsWith("MainEntranceUpgrade/")?90:s.path.StartsWith("BluebellStand/")?85:s.path.StartsWith("RoomsAndCores/")?10:50;
        static List<Pair> Pairs(List<Surface> surfaces)
        {
            var pairs=new List<Pair>();
            foreach(var bucket in surfaces.GroupBy(s=>s.axis+":"+s.sign+":"+Mathf.RoundToInt(s.plane*1000)))
            {
                var items=bucket.ToArray();for(int i=0;i<items.Length;i++)for(int j=i+1;j<items.Length;j++)
                {
                    var a=items[i];var b=items[j];if(a.filter==b.filter||Mathf.Abs(a.plane-b.plane)>PlaneTolerance)continue;
                    float area=0;foreach(var ta in a.triangles)foreach(var tb in b.triangles)area+=Intersection(ta,tb);if(area<MinArea)continue;
                    bool aWins=Priority(a)!=Priority(b)?Priority(a)>Priority(b):string.CompareOrdinal(a.path,b.path)<=0;
                    pairs.Add(new Pair{a=a,b=b,area=area,owner=aWins?a:b,loser=aWins?b:a});
                }
            }
            return pairs;
        }
        // Floor underside/edge contacts are recorded in the broad audit, but are outside wall repair.
        static List<Pair> WallPairs(List<Surface> surfaces)=>Pairs(surfaces).Where(p=>!p.a.filter.name.ToLowerInvariant().Contains("floor")&&!p.b.filter.name.ToLowerInvariant().Contains("floor")).ToList();
        static Sources LoadSources()=>File.Exists(SourceFile)?JsonUtility.FromJson<Sources>(File.ReadAllText(SourceFile)):new Sources();
        public static void Restore(GameObject root)
        {
            foreach(var source in LoadSources().items)
            {
                var t=root.transform.Find(source.path);if(!t)continue;var filter=t.GetComponent<MeshFilter>();
                if(!filter||!filter.sharedMesh||!filter.sharedMesh.name.StartsWith("WallSurface_"))continue;
                var original=source.cube?Cube():AssetDatabase.LoadAssetAtPath<Mesh>(source.asset);
                if(!original)throw new Exception("Original wall surface mesh unavailable: "+source.path);
                filter.sharedMesh=original;t.GetComponent<Renderer>().enabled=source.enabled;
            }
        }
        static string Digest(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","");}
        static string ColliderState(GameObject root)
        {
            return Digest(string.Join("\n",root.GetComponentsInChildren<Collider>(true).Select(c=>
            {
                var t=c.transform;string value=PathOf(t,root.transform)+"|"+c.GetType().Name+"|"+c.enabled+"|"+c.isTrigger+"|"+t.gameObject.activeSelf+"|"+t.gameObject.layer+"|"+t.localPosition.ToString("F6")+"|"+t.localRotation.ToString("F6")+"|"+t.localScale.ToString("F6");
                if(c is BoxCollider box)value+="|"+box.center.ToString("F6")+"|"+box.size.ToString("F6");
                if(c is MeshCollider mesh)value+="|"+AssetDatabase.GetAssetPath(mesh.sharedMesh)+"|"+mesh.convex;
                return value;
            }).OrderBy(v=>v)));
        }
        static List<Triangle> AllTriangles(MeshFilter filter)
        {
            var mesh=filter.sharedMesh;var v=mesh.vertices;var n=mesh.normals;var uv=mesh.uv;var output=new List<Triangle>();
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                var indices=mesh.GetTriangles(sub);for(int i=0;i<indices.Length;i+=3)
                {
                    var vertices=new Vertex[3];for(int j=0;j<3;j++){int index=indices[i+j];vertices[j]=new Vertex{local=v[index],world=filter.transform.TransformPoint(v[index]),normal=n.Length==v.Length?n[index]:Vector3.up,uv=uv.Length==v.Length?uv[index]:Vector2.zero};}
                    var normal=Vector3.Cross(vertices[1].world-vertices[0].world,vertices[2].world-vertices[0].world).normalized;
                    int axis=Mathf.Abs(normal.x)>.9999f?0:Mathf.Abs(normal.y)>.9999f?1:Mathf.Abs(normal.z)>.9999f?2:-1;
                    output.Add(new Triangle{sub=sub,axis=axis,normal=normal,plane=axis<0?0:vertices[0].world[axis],vertices=vertices});
                }
            }
            return output;
        }
        static Mesh SaveClipped(MeshFilter filter,List<Rule> rules,string name)
        {
            var positions=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();
            int subCount=filter.sharedMesh.subMeshCount;var indices=Enumerable.Range(0,subCount).Select(i=>new List<int>()).ToArray();
            foreach(var triangle in AllTriangles(filter))
            {
                var polygons=new List<List<Vertex>>{triangle.vertices.ToList()};
                if(triangle.axis>=0)foreach(var rule in rules.Where(r=>r.sub==triangle.sub&&r.cutter.axis==triangle.axis&&Vector3.Dot(r.cutter.normal,triangle.normal)>.9999f&&Mathf.Abs(r.cutter.plane-triangle.plane)<=PlaneTolerance))
                {
                    var next=new List<List<Vertex>>();foreach(var polygon in polygons)next.AddRange(Subtract(polygon,rule.cutter,triangle.axis));polygons=next;if(polygons.Count==0)break;
                }
                foreach(var polygon in polygons)for(int i=1;i<polygon.Count-1;i++)
                {
                    if(Vector3.Cross(polygon[i].world-polygon[0].world,polygon[i+1].world-polygon[0].world).magnitude<1e-8f)continue;
                    foreach(var vertex in new[]{polygon[0],polygon[i],polygon[i+1]}){indices[triangle.sub].Add(positions.Count);positions.Add(vertex.local);normals.Add(vertex.normal);uv.Add(vertex.uv);}
                }
            }
            var mesh=new Mesh{name=name};if(positions.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=subCount;for(int sub=0;sub<subCount;sub++)mesh.SetTriangles(indices[sub],sub);mesh.RecalculateBounds();if(positions.Count>0)mesh.RecalculateTangents();
            string path=MeshFolder+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!existing){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            existing.Clear();existing.indexFormat=mesh.indexFormat;existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;existing.tangents=mesh.tangents;existing.subMeshCount=subCount;
            for(int sub=0;sub<subCount;sub++)existing.SetTriangles(mesh.GetTriangles(sub),sub,false);existing.RecalculateBounds();EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(mesh);return existing;
        }
        public static void ApplyTo(GameObject root)
        {
            Restore(root);string collision=ColliderState(root);var sources=LoadSources();var before=WallPairs(Scan(root));Report(before,"wall-overlap-repaired.tsv");
            foreach(var group in before.GroupBy(p=>p.loser.filter))
            {
                var filter=group.Key;string path=PathOf(filter.transform,root.transform);var renderer=filter.GetComponent<Renderer>();var source=filter.sharedMesh;
                var record=sources.items.FirstOrDefault(s=>s.path==path);if(record==null){record=new Source{path=path};sources.items.Add(record);}
                record.asset=AssetDatabase.GetAssetPath(source);record.name=source.name;record.cube=source.name=="Cube";record.enabled=renderer.enabled;
                if(!record.cube&&string.IsNullOrEmpty(record.asset))throw new Exception("Cannot preserve original wall mesh: "+path);
                var rules=group.SelectMany(p=>p.owner.triangles.Select(t=>new Rule{sub=p.loser.sub,cutter=t})).ToList();
                filter.sharedMesh=SaveClipped(filter,rules,"WallSurface_"+Digest(path).Substring(0,14)+"_"+filter.name);renderer.enabled=filter.sharedMesh.vertexCount>0;
            }
            File.WriteAllText(SourceFile,JsonUtility.ToJson(sources,true));AssetDatabase.ImportAsset(SourceFile);
            var remaining=WallPairs(Scan(root));Report(remaining,"wall-overlap-after.tsv");
            if(remaining.Count>0)throw new Exception("Coplanar wall overlaps remain: "+remaining.Count);
            if(collision!=ColliderState(root))throw new Exception("Surface repair changed collision geometry");
            SessionState.SetString("EEG.WallSurfaceCollision",collision);
            Debug.Log($"WALL_SURFACES_CLEAN: {before.Count} overlapping wall surface pairs ({before.Count(p=>p.a.material!=p.b.material)} contrasting materials), {before.Select(p=>p.loser.filter).Distinct().Count()} renderers repaired; zero remaining audited wall overlaps; collider state identical.");
        }
        public static void Validate(GameObject root)
        {
            var remaining=WallPairs(Scan(root));Report(remaining,"wall-overlap-reloaded.tsv");if(remaining.Count>0)throw new Exception("Saved wall overlaps reappeared: "+remaining.Count);
            if(ColliderState(root)!=SessionState.GetString("EEG.WallSurfaceCollision",""))throw new Exception("Collision state changed after save/reload");
            ConvergenceHallStairStorage.Validate(root);
            Debug.Log("WALL_SURFACE_RELOAD_OK: audited wall overlaps zero, collider signature retained; core/storage/cabin/floor-label/Bluebell checks passed.");
        }
        public static void ApplyAndCapture()
        {
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));
            int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Wall repair created duplicate nodes");
            SessionState.SetBool("EEG.ConvergenceQA.FocusWallSurfaceCleanup",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        static string Folder()=>RehabJunctionValidation.Argument("-convergencePreviewFolder");
        static void Report(List<Pair> pairs,string file)
        {
            string folder=Folder();if(string.IsNullOrEmpty(folder))return;Directory.CreateDirectory(folder);
            var lines=new List<string>{"axis\tplane\tarea_m2\tmaterial_a\tmaterial_b\towner\tclipped_renderer"};
            lines.AddRange(pairs.OrderByDescending(p=>p.a.material!=p.b.material).ThenByDescending(p=>p.area).Select(p=>$"{p.a.axis}\t{p.a.plane:F6}\t{p.area:F6}\t{p.a.material}\t{p.b.material}\t{p.owner.path}\t{p.loser.path}"));File.WriteAllLines(System.IO.Path.Combine(folder,file),lines);
        }
        public static void AuditOnly()
        {
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{var surfaces=Scan(root);var pairs=Pairs(surfaces);Report(pairs,"wall-overlap-before.tsv");Debug.Log($"WALL_SURFACE_AUDIT: {surfaces.Count} structural surfaces, {pairs.Count} overlapping pairs, {pairs.Count(p=>p.a.material!=p.b.material)} contrasting material pairs.");}
            finally{PrefabUtility.UnloadPrefabContents(root);}if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        static bool BoundsOverlap(List<Vertex> polygon,Triangle cutter,int axis)
        {
            var a=polygon.Select(v=>Project(v.world,axis)).ToArray();var b=cutter.vertices.Select(v=>Project(v.world,axis)).ToArray();
            return a.Max(v=>v.x)>b.Min(v=>v.x)+1e-7f&&b.Max(v=>v.x)>a.Min(v=>v.x)+1e-7f&&a.Max(v=>v.y)>b.Min(v=>v.y)+1e-7f&&b.Max(v=>v.y)>a.Min(v=>v.y)+1e-7f;
        }
        public static void AuditSavedCoverage()
        {
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                string collision=ColliderState(root);
                var saved=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&f.sharedMesh.name.StartsWith("WallSurface_")).Select(f=>new{filter=f,mesh=f.sharedMesh,enabled=f.GetComponent<Renderer>().enabled}).ToArray();
                Restore(root);var original=Scan(root).Where(s=>!s.filter.name.ToLowerInvariant().Contains("floor")).ToArray();
                foreach(var item in saved){item.filter.sharedMesh=item.mesh;item.filter.GetComponent<Renderer>().enabled=item.enabled;}
                var current=Scan(root).Where(s=>!s.filter.name.ToLowerInvariant().Contains("floor")).ToArray();
                int checkedSurfaces=0;float worstMissing=0;
                foreach(var surface in original)
                {
                    var cutters=current.Where(s=>s.axis==surface.axis&&s.sign==surface.sign&&Mathf.Abs(s.plane-surface.plane)<=PlaneTolerance).SelectMany(s=>s.triangles).ToArray();float missing=0;
                    foreach(var triangle in surface.triangles)
                    {
                        var polygons=new List<List<Vertex>>{triangle.vertices.ToList()};
                        foreach(var cutter in cutters)
                        {
                            var next=new List<List<Vertex>>();foreach(var polygon in polygons){if(BoundsOverlap(polygon,cutter,surface.axis))next.AddRange(Subtract(polygon,cutter,surface.axis));else next.Add(polygon);}polygons=next;if(polygons.Count==0)break;
                        }
                        missing+=polygons.Sum(p=>Area(p,surface.axis));
                    }
                    worstMissing=Mathf.Max(worstMissing,missing);checkedSurfaces++;
                    if(missing>MinArea)throw new Exception($"Wall surface coverage lost: {surface.path}, axis={surface.axis}, plane={surface.plane}, area={missing}");
                }
                if(WallPairs(current.ToList()).Count>0||collision!=ColliderState(root))throw new Exception("Saved overlap/collision audit failed");
                string line=$"WALL_COVERAGE_OK: {checkedSurfaces} original wall surfaces remain fully covered by the repaired wall union; maximum numerical missing area={worstMissing:G6} m2; saved overlaps zero; collision state identical.";
                Debug.Log(line);File.WriteAllText(System.IO.Path.Combine(Folder(),"wall-coverage.txt"),line);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}if(Application.isBatchMode)EditorApplication.Exit(0);
        }
    }
}
