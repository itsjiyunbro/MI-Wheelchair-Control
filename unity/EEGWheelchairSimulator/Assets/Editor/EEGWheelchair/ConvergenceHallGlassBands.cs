using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallGlassBands
    {
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        public static Material Glass(bool frosted)
        {
            string name=frosted?"PrivacyFilmGlass":"ClearCorridorGlass";
            var m=AssetDatabase.LoadAssetAtPath<Material>(Mats+name+".mat");
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,Mats+name+".mat");}
            m.SetColor("_BaseColor",frosted?new Color(.82f,.86f,.87f,.94f):new Color(.91f,.95f,.96f,.12f));
            m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha",1);m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",2);
            m.SetFloat("_Smoothness",frosted?.10f:.80f);m.SetFloat("_Metallic",0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;
            m.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(m);return m;
        }
        static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material m)
        {
            var t=parent.Find(name);if(!t){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());t=go.transform;t.SetParent(parent,false);}
            t.localPosition=p;t.localRotation=Quaternion.identity;t.localScale=size;t.gameObject.SetActive(true);var r=t.GetComponent<Renderer>();r.sharedMaterial=m;r.shadowCastingMode=m.HasProperty("_Surface")&&m.GetFloat("_Surface")==1?ShadowCastingMode.Off:ShadowCastingMode.On;return t;
        }
        public static void ApplyTo(GameObject root)
        {
            var clear=Glass(false);var film=Glass(true);var old=AssetDatabase.LoadAssetAtPath<Material>(Mats+"FrostedTeal.mat");var metal=AssetDatabase.LoadAssetAtPath<Material>(Mats+"Metal.mat");
            var clerestory=AssetDatabase.LoadAssetAtPath<Material>(Mats+"ClerestoryGlass.mat");
            if(clerestory){clerestory.CopyPropertiesFromMaterial(clear);EditorUtility.SetDirty(clerestory);}
            foreach(var collider in root.GetComponentsInChildren<BoxCollider>())
            {
                var edge=collider.transform;var group=edge.Find("CorridorGlazing");if(!group||!group.gameObject.activeInHierarchy)continue;
                var panes=group.Cast<Transform>().Where(t=>t.gameObject.activeSelf&&t.GetComponent<Renderer>()&&t.GetComponent<Renderer>().sharedMaterial==old).OrderBy(t=>t.localPosition.z).ToArray();
                if(panes.Length==0)continue;
                // Remove the opaque backing only behind glazing, retaining white partitions on other spans.
                foreach(string name in new[]{"LowerWall","Skirting"})
                {
                    var source=edge.Find(name);if(!source)continue;var p=source.localPosition;var size=source.localScale;float cursor=p.z-size.z/2,end=p.z+size.z/2;int index=0;
                    void Keep(float a,float b){if(b-a>.0005f)Box(group,"OpaqueRemainder"+name+index++,new Vector3(p.x,p.y,(a+b)/2),new Vector3(size.x,size.y,b-a),source.GetComponent<Renderer>().sharedMaterial);}
                    foreach(var pane in panes){float a=Mathf.Clamp(pane.localPosition.z-pane.localScale.z/2,cursor,end),b=Mathf.Clamp(pane.localPosition.z+pane.localScale.z/2,cursor,end);Keep(cursor,a);cursor=Mathf.Max(cursor,b);}
                    Keep(cursor,end);source.gameObject.SetActive(false);
                }
                foreach(var pane in panes)
                {
                    float z=pane.localPosition.z,length=pane.localScale.z;string n=pane.name;
                    Box(group,n+"ClearBottom",new Vector3(0,.25f,z),new Vector3(.018f,.50f,length),clear);
                    Box(group,n+"PrivacyFilm",new Vector3(0,1.425f,z),new Vector3(.018f,1.85f,length),film);
                    Box(group,n+"ClearTop",new Vector3(0,2.60f,z),new Vector3(.018f,.50f,length),clear);
                    Box(group,n+"BaseFrame",new Vector3(0,.015f,z),new Vector3(.042f,.03f,length),metal);
                    Box(group,n+"HeadFrame",new Vector3(0,2.835f,z),new Vector3(.042f,.03f,length),metal);
                    pane.gameObject.SetActive(false);
                }
            }
            // Preserve B111's half-glass / half-opaque entrance; update only its glass side.
            var facade=root.transform.Find("DoorsAndSigns/B111Entrance");var frost=facade.Find("FrostedSidelight");float x=frost.localPosition.x,w=frost.localScale.x;
            Box(facade,"FrostedSidelight",new Vector3(x,1.425f,0),new Vector3(w,1.85f,.018f),film);
            Box(facade,"ClearSidelight",new Vector3(x,2.60f,0),new Vector3(w,.50f,.018f),clear);
            Box(facade,"ClearBottomSidelight",new Vector3(x,.25f,0),new Vector3(w,.50f,.018f),clear);
        }
        static void Validate(GameObject root)
        {
            var clear=Glass(false);var film=Glass(true);
            var visible=root.GetComponentsInChildren<Renderer>();
            var bands=visible.Where(r=>r.sharedMaterial==clear||r.sharedMaterial==film).ToArray();
            if(bands.Length<30)throw new Exception("Glass bands missing.");
            foreach(var r in bands)
            {
                var b=r.bounds;
                if(r.sharedMaterial==film&&(b.min.y<.499f||b.max.y>2.351f))throw new Exception("Frosting outside the 1.85m middle band: "+r.name);
                if(r.sharedMaterial==clear&&!(b.max.y<=.501f||b.min.y>=2.349f))throw new Exception("Clear glass outside upper/lower 50cm: "+r.name);
            }
            var edge=root.transform.Find("RoomsAndCores/B107/South");
            var collider=edge.GetComponent<BoxCollider>();if(!collider.enabled||Mathf.Abs(collider.size.y-2.85f)>.001f)throw new Exception("Glass collision boundary lost.");
            foreach(string n in new[]{"B101","B102","B103"})if(!root.transform.Find("RoomsAndCores/"+n+"/North").GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterial.name=="ClerestoryGlass"))throw new Exception("White lower wall window layout changed.");
            foreach(Transform door in root.transform.Find("PlanDoors"))
            {
                var panel=door.Find("OpaqueTransom");if(!panel)continue;
                var leaf=door.GetComponentsInChildren<Transform>().Single(t=>t.name=="Leaf");
                if(Mathf.Abs(leaf.TransformVector(Vector3.right).magnitude-.95f)>.0001f)throw new Exception("Classroom leaf width is not 95cm: "+door.name);
                if(panel.GetComponent<Renderer>().sharedMaterial.name!="WarmWhite"||Mathf.Abs(panel.GetComponent<Renderer>().bounds.max.y-2.85f)>.002f||!door.Find("AccessReader"))throw new Exception("Photo door panel/reader missing.");
                var parts=door.name.Split('_');var edgeForDoor=root.transform.Find("RoomsAndCores/"+parts[0]+"/"+parts[1]);
                float centre=edgeForDoor.InverseTransformPoint(door.position).z;
                foreach(var pane in edgeForDoor.Find("DoorOpeningSurfaces").GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial==clear||r.sharedMaterial==film))
                {var t=pane.transform;float half=t.localScale.z/2;if(pane.bounds.max.y>2.21f&&t.localPosition.z-half<centre+.45f&&t.localPosition.z+half>centre-.45f)throw new Exception("Glass remains above classroom door.");}
            }
            var b111Leaf=root.transform.Find("DoorsAndSigns/B111Entrance/OpaqueDoor");
            if(Mathf.Abs(b111Leaf.TransformVector(Vector3.right).magnitude-.95f)>.0001f)throw new Exception("B111 leaf width is not 95cm.");
            Debug.Log("CLASSROOM_WIDTH_OK: measured 0.95m leaves, including B111, excluding frames.");
            Debug.Log("GLASS_BANDS_OK: clear 0-0.50m / privacy film 0.50-2.35m / clear 2.35-2.85m; opaque backing removed on glazed spans; collisions and clerestories preserved.");
        }
        [MenuItem("Tools/EEG Wheelchair/Apply Measured Glass Bands")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ConvergenceHallWallMaterials.ApplyTo(root);Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
        }
        public static void ApplyAndValidate()
        {Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Repeated glass update duplicated geometry.");ConvergenceHallB1Validation.BuildAndValidate();}
    }
}
