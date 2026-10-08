using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallJoinPrivacy
    {
        public static void Build(GameObject root)
        {
            var previous=root.transform.Find("BluebellB112Connection");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var group=P.Group(root.transform,"BluebellB112Connection");
            var end=root.transform.Find("BluebellStand/MeasuredBluebell/UnderStairWall/UpperFlightWall").GetComponent<Renderer>().bounds;
            var frame=root.transform.Find("PlanDoors/B112_East_South/FrameLeft").GetComponent<Renderer>().bounds;
            // Join the stair cladding to the door-frame depth, with concealed edge overlaps.
            // Keep the face 1mm behind the frame to avoid coincident front surfaces.
            float from=end.max.z-.003f,to=frame.min.z+.003f,back=end.min.x,front=frame.max.x-.001f;
            if(to<=from||front<=back)throw new Exception("Unexpected Bluebell/B112 junction dimensions");
            P.Box(group,"ConnectionWall",new Vector3((back+front)/2,1.425f,(from+to)/2),new Vector3(front-back,2.85f,to-from),AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/WarmWhite.mat"));
            Debug.Log($"BLUEBELL_B112_JOIN: cladding spans {to-from:F4}m to the door frame; front face depth corrected, no extra collider.");
        }
        static bool TriangleHit(Ray ray,Vector3 a,Vector3 b,Vector3 c)
        {
            var ab=b-a;var ac=c-a;var p=Vector3.Cross(ray.direction,ac);float det=Vector3.Dot(ab,p);if(Mathf.Abs(det)<1e-8f)return false;
            float inv=1/det;var t=ray.origin-a;float u=Vector3.Dot(t,p)*inv;if(u<-.00001f||u>1.00001f)return false;
            var q=Vector3.Cross(t,ab);float v=Vector3.Dot(ray.direction,q)*inv;if(v<-.00001f||u+v>1.00001f)return false;
            float distance=Vector3.Dot(ac,q)*inv;return distance>=0&&distance<1;
        }
        public static void Validate(GameObject root)
        {
            var group=root.transform.Find("BluebellB112Connection");var fill=group.Find("ConnectionWall");var renderer=fill.GetComponent<Renderer>();var mesh=fill.GetComponent<MeshFilter>().sharedMesh;
            if(group.GetComponentsInChildren<Collider>(true).Length>0)throw new Exception("Junction finish changed collision");
            var stairEnd=root.transform.Find("BluebellStand/MeasuredBluebell/UnderStairWall/UpperFlightWall").GetComponent<Renderer>().bounds;
            var frame=root.transform.Find("PlanDoors/B112_East_South/FrameLeft").GetComponent<Renderer>().bounds;
            if(renderer.bounds.min.z>stairEnd.max.z-.002f||renderer.bounds.max.z<frame.min.z+.002f||Mathf.Abs(renderer.bounds.max.x-(frame.max.x-.001f))>.0005f)throw new Exception("B112 junction does not meet both edges");
            var vertices=mesh.vertices.Select(fill.TransformPoint).ToArray();var indices=mesh.triangles;
            for(int i=0;i<=8;i++)foreach(float y in new[]{.08f,.8f,1.8f,2.7f})
            {
                float z=Mathf.Lerp(stairEnd.max.z,frame.min.z,i/8f);var ray=new Ray(new Vector3(renderer.bounds.max.x+.35f,y,z),Vector3.left);bool hit=false;
                for(int j=0;j<indices.Length;j+=3)if(TriangleHit(ray,vertices[indices[j]],vertices[indices[j+1]],vertices[indices[j+2]])){hit=true;break;}
                if(!hit)throw new Exception("Visible opening remains at the B112 junction");
            }
            var film=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/PrivacyFilmGlass.mat");var clear=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/ClearCorridorGlass.mat");
            if(Mathf.Abs(film.GetColor("_BaseColor").a-.94f)>.001f||Mathf.Abs(clear.GetColor("_BaseColor").a-.12f)>.001f)throw new Exception("Privacy/clear glass opacity mismatch");
            var roomWalls=root.transform.Find("RoomsAndCores");
            foreach(var pane in root.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial==film&&r.transform.IsChildOf(roomWalls)))if(pane.bounds.min.y<.499f||pane.bounds.max.y>2.351f)throw new Exception("Privacy band height changed: "+pane.name);
            ConvergenceHallLoungeDoor.Validate(root);
            Debug.Log("JOIN_PRIVACY_OK: 36 actual mesh sight lines across Bluebell/B112 seam blocked; 1mm frame-depth separation; no new colliders; privacy alpha .94, clear alpha .12, middle-band heights retained.");
        }
        public static void ApplyAndCapture()
        {
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));
            SessionState.SetBool("EEG.ConvergenceQA.FocusJoinPrivacy",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static void VerifySaved()
        {
            var prefab=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);string signature;
            try{signature=(string)typeof(ConvergenceHallWallSurfaceCleanup).GetMethod("ColliderState",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{prefab});}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            SessionState.SetString("EEG.WallSurfaceCollision",signature);UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
    }
}
