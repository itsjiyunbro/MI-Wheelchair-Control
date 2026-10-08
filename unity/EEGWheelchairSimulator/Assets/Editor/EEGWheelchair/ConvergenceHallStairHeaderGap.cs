using System;
using UnityEditor;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallStairHeaderGap
    {
        static bool Hit(Ray ray,Vector3 a,Vector3 b,Vector3 c)
        {
            var ab=b-a;var ac=c-a;var p=Vector3.Cross(ray.direction,ac);float det=Vector3.Dot(ab,p);if(Mathf.Abs(det)<1e-8f)return false;
            float inv=1/det;var t=ray.origin-a;float u=Vector3.Dot(t,p)*inv;if(u<-.00001f||u>1.00001f)return false;
            var q=Vector3.Cross(t,ab);float v=Vector3.Dot(ray.direction,q)*inv;if(v<-.00001f||u+v>1.00001f)return false;float d=Vector3.Dot(ac,q)*inv;return d>=0&&d<1;
        }
        public static void Validate(GameObject root)
        {
            var hall=root.transform.Find("CoreHalls/B112Stairwell");
            foreach(string n in new[]{"ExitMarker","ExitMarkerBacking"}){var t=hall.Find(n);if(t&&t.gameObject.activeInHierarchy)throw new Exception("B112 upper sign remains");}
            if(!hall.Find("ReferencePlacard").gameObject.activeInHierarchy||hall.Find("ReferencePlacard/Title").GetComponent<UnityEngine.UI.Text>().text!="계단실")throw new Exception("Door-side stair sign changed");
            var lintel=hall.Find("Lintel");var mesh=lintel.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var tr=mesh.triangles;
            foreach(float x in new[]{-.98f,-.5f,0,.5f,.98f})foreach(float y in new[]{2.27f,2.34f,2.44f,2.70f,2.83f})
            {
                var ray=new Ray(hall.TransformPoint(new Vector3(x,y,-.40f)),hall.forward);bool covered=false;
                for(int i=0;i<tr.Length;i+=3)if(Hit(ray,lintel.TransformPoint(vertices[tr[i]]),lintel.TransformPoint(vertices[tr[i+1]]),lintel.TransformPoint(vertices[tr[i+2]]))){covered=true;break;}
                if(!covered)throw new Exception("Opening remains above the stair frame");
            }
            if(Mathf.Abs(lintel.localPosition.y-lintel.localScale.y/2-2.245f)>.001f||hall.Find("ClosedLeafTopSeal").GetComponent<Renderer>().bounds.min.y>2.20f)throw new Exception("Stair header/leaf seal mismatch");
            ConvergenceHallJoinPrivacy.Validate(root);
            Debug.Log("STAIR_HEADER_GAP_OK: B112 upper EXIT text/backing hidden; side stair plaque kept; 25 actual header mesh rays blocked, old 18.5cm frame-to-lintel gap closed, closed-leaf top seal added; existing layout/no-glow/privacy/junction checks passed.");
        }
        public static void ApplyAndCapture()
        {
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));
            SessionState.SetBool("EEG.ConvergenceQA.FocusStairHeaderGap",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static void VerifySaved()
        {
            var prefab=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);string signature;
            try{signature=(string)typeof(ConvergenceHallWallSurfaceCleanup).GetMethod("ColliderState",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{prefab});}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            SessionState.SetString("EEG.WallSurfaceCollision",signature);UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));if(Application.isBatchMode)EditorApplication.Exit(0);
        }
    }
}
