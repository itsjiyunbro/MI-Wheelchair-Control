using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallFloorFinishes
    {
        const string Folder="Assets/Art/Materials/ConvergenceHall/";
        const string MeshFolder="Assets/Art/Meshes/ConvergenceHall/";
        // Tile module is a visual estimate; only the floor material boundary is supplied by the user.
        const float Module=.60f;
        static Material Finish(string name,bool stone)
        {
            string texturePath=Folder+name+"Texture.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(!texture)
            {
                const int size=512;
                texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name=name+"Texture",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
                var pixels=new Color[size*size];var random=new System.Random(stone?2184:9182);
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float n=(float)random.NextDouble();
                    if(stone)
                    {
                        float grain=n<.26f?.19f+n*.50f:n<.55f?.48f+n*.25f:.77f+n*.14f;
                        pixels[y*size+x]=new Color(grain*1.02f,grain,grain*.94f);
                    }
                    else
                    {
                        float clouds=Mathf.PerlinNoise(x/85f,y/85f)*.07f+Mathf.PerlinNoise(x/22f+9,y/22f)*.025f;
                        float gray=.34f+clouds+(n-.5f)*.020f;
                        pixels[y*size+x]=new Color(gray,gray*1.015f,gray*1.02f);
                    }
                    if(x<2||y<2)pixels[y*size+x]=stone?new Color(.40f,.39f,.35f):new Color(.22f,.235f,.235f);
                }
                texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,texturePath);
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+name+".mat");
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(material,Folder+name+".mat");}
            material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",texture);material.SetFloat("_Smoothness",stone?.62f:.27f);material.SetFloat("_Metallic",0);EditorUtility.SetDirty(material);return material;
        }
        static Transform Group(Transform p,string name)
        {var t=p.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(p,false);}return t;}
        static void Patch(Transform parent,string name,float x0,float y0,float x1,float y1,float height,Material material)
        {
            var t=Group(parent,name);t.localPosition=Vector3.zero;t.localRotation=Quaternion.identity;t.localScale=Vector3.one;
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder+name+".asset");
            if(!mesh){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,MeshFolder+name+".asset");}else mesh.Clear();
            var v=new[]{ConvergenceHallB1Setup.Plan(x0,y0,height),ConvergenceHallB1Setup.Plan(x1,y0,height),ConvergenceHallB1Setup.Plan(x1,y1,height),ConvergenceHallB1Setup.Plan(x0,y1,height)};
            mesh.vertices=v;mesh.triangles=new[]{0,1,2,0,2,3};mesh.uv=v.Select(p=>new Vector2(p.x/Module,p.z/Module)).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var filter=t.GetComponent<MeshFilter>();if(!filter)filter=t.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            var renderer=t.GetComponent<MeshRenderer>();if(!renderer)renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void FloorMaterial(Transform t,Material material)
        {
            var filter=t.GetComponent<MeshFilter>();if(!filter)return;
            string name="Finish_"+t.parent.name+"_"+t.name;
            string path=MeshFolder+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            // Copy, never modify a primitive's built-in mesh or a room's geometry asset.
            var source=UnityEngine.Object.Instantiate(filter.sharedMesh);
            source.name=name;source.uv=source.vertices.Select(p=>{var w=t.TransformPoint(p);return new Vector2(w.x/Module,w.z/Module);}).ToArray();
            if(!mesh){mesh=source;AssetDatabase.CreateAsset(mesh,path);}else{EditorUtility.CopySerialized(source,mesh);UnityEngine.Object.DestroyImmediate(source);EditorUtility.SetDirty(mesh);}
            filter.sharedMesh=mesh;t.GetComponent<Renderer>().sharedMaterial=material;
        }
        public static void ApplyTo(GameObject root)
        {
            var tile=Finish("MeasuredGrayFloorTile",false);var stone=Finish("MeasuredSpeckledStone",true);
            foreach(Transform t in root.transform.Find("Floors"))if(t.name.Contains("TileJoint"))t.gameObject.SetActive(false);
            foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("RoomFloor")||t.name=="B1Footprint"||t.name.Contains("Corridor")&&t.GetComponent<MeshFilter>()||t.name=="WestNorthAlcove"||t.name=="HallFloor"||t.name=="Landing").ToArray())
                if(t.GetComponent<Renderer>())FloorMaterial(t,tile);
            var patches=Group(root.transform,"MeasuredFloorFinishes");
            Patch(patches,"GrayFloorBase",150,334,1110,878,.008f,tile);
            Patch(patches,"StoneNorthEnd",640,334,742,367,.010f,stone);
            Patch(patches,"StoneNorthPassage",710,367,742,406,.010f,stone);
            Patch(patches,"StoneMainPassage",710,406,772,728,.010f,stone);
            Patch(patches,"StoneEntrance",632,728,772,878,.010f,stone);
        }
        static void Validate(GameObject root)
        {
            var patches=root.transform.Find("MeasuredFloorFinishes");if(!patches||patches.childCount!=5)throw new Exception("Floor finish regions missing.");
            foreach(var r in root.transform.Find("RoomsAndCores").GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("RoomFloor")))if(r.sharedMaterial.name!="MeasuredGrayFloorTile")throw new Exception("Room floor is not gray tile: "+r.transform.parent.name);
            if(patches.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Floor finishes changed driving collisions.");
            Debug.Log("FLOOR_FINISHES_OK: four red-plan stone rectangles, gray tiles throughout remaining corridors/classrooms; world-aligned approximate 60cm joints; no added colliders.");
        }
        [MenuItem("Tools/EEG Wheelchair/Apply Photo Floor Finishes")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ConvergenceHallWallMaterials.ApplyTo(root);Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
        }
        public static void ApplyAndValidate()
        {Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Duplicate floor geometry.");ConvergenceHallB1Validation.BuildAndValidate();}
    }
}
