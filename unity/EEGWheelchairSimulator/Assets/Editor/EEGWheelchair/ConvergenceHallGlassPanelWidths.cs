using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallGlassPanelWidths
    {
        const float Target=1f,Joint=.012f;
        static int Count(float width)
        {
            int lo=Mathf.Max(1,Mathf.FloorToInt(width/Target)),hi=lo+1;
            return Mathf.Abs(width/lo-Target)<=Mathf.Abs(width/hi-Target)?lo:hi;
        }
        static Transform Group(Transform p,string name)
        {var t=p.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(p,false);}return t;}
        static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            var t=go.transform;t.SetParent(p,false);t.localPosition=at;t.localScale=size;var r=go.GetComponent<Renderer>();r.sharedMaterial=material;
            r.shadowCastingMode=material.HasProperty("_Surface")&&material.GetFloat("_Surface")==1?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;
            return t;
        }
        static bool Glass(Renderer r)=>r&&r.sharedMaterial&&(r.sharedMaterial.name=="ClearCorridorGlass"||r.sharedMaterial.name=="PrivacyFilmGlass"||r.sharedMaterial.name=="ClerestoryGlass");
        public static void ApplyTo(GameObject root)
        {
            var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/Metal.mat");
            foreach(var hit in root.transform.Find("RoomsAndCores").GetComponentsInChildren<BoxCollider>())
            {
                var edge=hit.transform;var source=edge.Find("DoorOpeningSurfaces");if(!source)source=edge.Find("CorridorGlazing");if(!source)continue;
                var panes=source.Cast<Transform>().Where(t=>t.gameObject.activeSelf&&Glass(t.GetComponent<Renderer>())).ToArray();if(panes.Length==0)continue;
                var group=Group(edge,"MeasuredGlassPanels");foreach(Transform child in group.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                // Replace the old evenly spaced bars only after doors have cut the glass spans.
                foreach(Transform old in source)
                    if(old.gameObject.activeSelf&&old.GetComponent<Renderer>()&&old.GetComponent<Renderer>().sharedMaterial==metal&&old.localScale.y>.30f&&Mathf.Abs(old.localScale.z*edge.lossyScale.z)<.06f)old.gameObject.SetActive(false);
                int index=0;
                foreach(var pane in panes)
                {
                    var p=pane.localPosition;var s=pane.localScale;float world=Mathf.Abs(s.z*edge.lossyScale.z);int count=Count(world);float width=s.z/count;
                    for(int i=0;i<count;i++)Box(group,"Panel_"+index+"_"+i,new Vector3(p.x,p.y,p.z-s.z/2+width*(i+.5f)),new Vector3(s.x,s.y,width),pane.GetComponent<Renderer>().sharedMaterial);
                    for(int i=0;i<=count;i++)Box(group,"Joint_"+index+"_"+i,new Vector3(p.x,p.y,p.z-s.z/2+width*i),new Vector3(.025f,s.y,Joint/Mathf.Abs(edge.lossyScale.z)),metal);
                    pane.gameObject.SetActive(false);index++;
                }
            }
            // The rounded B108 corner uses arc distance rather than straight chord width.
            var curve=root.transform.Find("B108RoundedCorner");var curveGroup=Group(curve,"MeasuredGlassPanels");foreach(Transform child in curveGroup.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            float radius=24*.072f;int segments=Count(radius*Mathf.PI/2);var centre=ConvergenceHallB1Setup.Plan(282,709);
            for(int i=0;i<=segments;i++)
            {
                float a=i*Mathf.PI/2/segments;var at=centre+new Vector3(-Mathf.Cos(a)*radius,1.425f,-Mathf.Sin(a)*radius);
                var post=Box(curveGroup,"ArcJoint"+i,curve.InverseTransformPoint(at),new Vector3(Joint,2.85f,.025f),metal);post.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
            }
        }
        static void Validate(GameObject root)
        {
            var groups=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="MeasuredGlassPanels").ToArray();if(groups.Length<10)throw new Exception("Measured glass panels missing.");
            int panels=0;
            foreach(var group in groups)
            {
                if(group.GetComponentsInChildren<Collider>().Length>0)throw new Exception("Glass seams changed collisions.");
                foreach(var set in group.Cast<Transform>().Where(t=>t.name.StartsWith("Panel_")).GroupBy(t=>t.name.Split('_')[1]))
                {
                    var widths=set.Select(t=>t.TransformVector(Vector3.forward).magnitude).ToArray();float sum=widths.Sum();
                    if(widths.Length!=Count(sum)||widths.Max()-widths.Min()>.001f)throw new Exception("Glass span was not evenly distributed near 1m.");panels+=widths.Length;
                }
            }
            Debug.Log("MEASURED_GLASS_PANELS_OK: "+panels+" equal-width band panels distributed near 1m across door-trimmed spans; B108 arc joints included.");
        }
        public static void Apply()
        {
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ConvergenceHallWallMaterials.ApplyTo(root);Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
        }
        public static void ApplyAndValidate()
        {Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Glass panel update duplicates geometry.");ConvergenceHallB1Validation.BuildAndValidate();}
    }
}
