using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallBluebellInfill
    {
        const float ClassroomTop=2.85f,Thickness=.12f;
        static float Top(float z)=>z<=4.5f?z*.5f:z<=6.7f?2.25f:2.25f+(z-6.7f)*.5f;
        static Transform Panel(Transform parent,string name,float x0,float x1,float z0,float z1,float bottom,Material material)
        {
            var a=new Vector3(x0,bottom,z0);var b=new Vector3(x0,bottom,z1);
            var c=new Vector3(x0,Top(z1),z1);var d=new Vector3(x0,Top(z0),z0);
            var e=new Vector3(x1,bottom,z0);var f=new Vector3(x1,bottom,z1);
            var g=new Vector3(x1,Top(z1),z1);var h=new Vector3(x1,Top(z0),z0);
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            void Face(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3)
            {
                int start=vertices.Count;vertices.AddRange(new[]{p0,p1,p2,p3});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                if(Vector3.Cross(p1-p0,p2-p0).sqrMagnitude>1e-10f)triangles.AddRange(new[]{start,start+1,start+2});
                if(Vector3.Cross(p2-p0,p3-p0).sqrMagnitude>1e-10f)triangles.AddRange(new[]{start,start+2,start+3});
            }
            Face(a,b,c,d);Face(e,h,g,f);Face(a,d,h,e);Face(b,f,g,c);Face(a,e,f,b);Face(d,c,g,h);
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var t=P.Group(parent,name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("BluebellInfill_"+name,mesh);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            return t;
        }
        public static void Build(GameObject root,Transform stairs,float width)
        {
            var old=stairs.Find("UnderStairWall");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var wall=P.Group(stairs,"UnderStairWall");
            var room=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;
            float join=room.min.z-.06f-stairs.position.z;
            if(join<=6.7f||join>=ConvergenceHallMeasuredBluebell.Depth)throw new Exception("Unexpected Bluebell/classroom wall join");
            var white=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/WarmWhite.mat");
            var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/Metal.mat");
            // A 1mm face offset keeps the white finish visible over the existing timber sides.
            float x1=width/2+.001f,x0=x1-Thickness;
            Panel(wall,"LowerFlightWall",x0,x1,0,4.5f,0,white);
            Panel(wall,"LandingWall",x0,x1,4.5f,6.7f,0,white);
            Panel(wall,"UpperFlightWall",x0,x1,6.7f,join,0,white);
            // The existing B112 perimeter closes the lower part beyond this join.
            // Continue only above its wall height, leaving the classroom interior intact.
            Panel(wall,"AboveClassroomWall",x0,x1,join,ConvergenceHallMeasuredBluebell.Depth,ClassroomTop,white);
            float[] corners={0,4.5f,6.7f,ConvergenceHallMeasuredBluebell.Depth};
            for(int i=0;i<3;i++)P.Tube(wall,"MetalSlopeTrim"+i,new Vector3(x1-.028f,Top(corners[i])+.006f,corners[i]),new Vector3(x1-.028f,Top(corners[i+1])+.006f,corners[i+1]),.018f,metal);
        }
        public static void Validate(GameObject root)
        {
            var stairs=root.transform.Find("BluebellStand/MeasuredBluebell");var wall=stairs.Find("UnderStairWall");
            if(!wall||wall.GetComponentsInChildren<MeshFilter>().Count(m=>m.name.EndsWith("Wall"))!=4)throw new Exception("Missing Bluebell infill panels");
            if(wall.GetComponentsInChildren<Collider>().Length>0)throw new Exception("Bluebell decorative wall changed collision layout");
            var room=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;
            var lower=wall.Find("UpperFlightWall").GetComponent<Renderer>().bounds;
            var upper=wall.Find("AboveClassroomWall").GetComponent<Renderer>().bounds;
            if(Mathf.Abs(lower.max.z-(room.min.z-.06f))>.002f||Mathf.Abs(upper.min.y-ClassroomTop)>.002f||Mathf.Abs(upper.min.z-lower.max.z)>.002f)throw new Exception("Bluebell wall intrudes into classroom or has a join gap");
            foreach(var mesh in wall.GetComponentsInChildren<MeshFilter>().Where(m=>m.name.EndsWith("Wall")))
            {
                foreach(var p in mesh.sharedMesh.vertices)
                {
                    if(p.y>Top(p.z)+.001f)throw new Exception("Wall protrudes above stair profile");
                    var world=mesh.transform.TransformPoint(p);if(world.x<room.min.x-.002f||world.x>room.max.x+.002f)throw new Exception("Wall protrudes into corridor");
                }
                if(mesh.GetComponent<Renderer>().sharedMaterial.name!="WarmWhite")throw new Exception("White wall finish mismatch");
            }
            var blocker=root.transform.Find("BluebellStand").GetComponent<BoxCollider>();
            foreach(float z in new[]{1f,4f,5.6f,8.5f})
            {
                var origin=stairs.TransformPoint(new Vector3(room.size.x/2+.5f,Mathf.Min(.7f,Top(z)*.5f),z));
                if(!blocker.Raycast(new Ray(origin,-stairs.right),out var hit,1f))throw new Exception("Original stair blocker does not cover new wall");
            }
            Debug.Log("BLUEBELL_INFILL_OK: native white sloped corridor wall closes lower flight, 2.2m landing and upper flight; B112 join and above-wall continuation, original stair collision preserved.");
        }
        public static void ApplyAndCapture()
        {
            ConvergenceHallBluebell.Apply();Validate(GameObject.Find("ConvergenceHallB1"));
            int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            ConvergenceHallBluebell.Apply();Validate(GameObject.Find("ConvergenceHallB1"));
            if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Bluebell infill duplicate authoring");
            SessionState.SetBool("EEG.ConvergenceQA.FocusBluebellWall",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
