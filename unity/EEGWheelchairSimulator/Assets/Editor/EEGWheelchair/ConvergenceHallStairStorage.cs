using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallStairStorage
    {
        public static readonly string[] Halls={"B112Stairwell","B109Stairwell"};
        const float Front=.80f,FrontTop=2.70f,BackTop=1.26f,LeafWidth=.82f,LeafHeight=2.08f;
        static Transform Solid(Transform parent,string name,Vector3 at,Vector3 size,Material material,bool collision=false)
        {
            var t=P.Box(parent,name,at,size,material);
            if(collision){t.gameObject.AddComponent<BoxCollider>();t.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");}return t;
        }
        static void Label(Transform parent,string name,string content,Vector3 at,Vector2 size,int pixels,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(Text));go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=Vector3.one*.001f;
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)go.transform).sizeDelta=size*1000;
            var t=go.GetComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");t.text=content;t.fontSize=pixels;t.resizeTextForBestFit=true;t.resizeTextMinSize=5;t.resizeTextMaxSize=pixels;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;
        }
        static Transform SlopeWall(Transform parent,string name,float x0,float x1,float back,Material white)
        {
            var a=new Vector3(x0,0,Front);var b=new Vector3(x0,0,back);var c=new Vector3(x0,BackTop,back);var d=new Vector3(x0,FrontTop,Front);
            var e=new Vector3(x1,0,Front);var f=new Vector3(x1,0,back);var g=new Vector3(x1,BackTop,back);var h=new Vector3(x1,FrontTop,Front);
            var v=new List<Vector3>();var tr=new List<int>();var uv=new List<Vector2>();
            void Face(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3){int n=v.Count;v.AddRange(new[]{p0,p1,p2,p3});tr.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});}
            Face(a,b,c,d);Face(e,h,g,f);Face(a,d,h,e);Face(b,f,g,c);Face(a,e,f,b);Face(d,c,g,h);
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var t=P.Group(parent,name);var saved=P.SaveMesh("StairStorage_"+name+"_"+parent.parent.name,mesh);t.gameObject.AddComponent<MeshFilter>().sharedMesh=saved;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=white;
            t.gameObject.AddComponent<MeshCollider>().sharedMesh=saved;t.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");return t;
        }
        public static void Build(GameObject root,Transform hall,bool east)
        {
            var old=hall.Find("UnderStairStorage");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            if(east&&hall.Find("SideServicePanel"))hall.Find("SideServicePanel").gameObject.SetActive(false);
            var storage=P.Group(hall,"UnderStairStorage");var flight=hall.Find("SwitchbackStairs/LeftReturnStep9");
            float width=hall.Find("Landing").localScale.x,back=hall.Find("Landing").localScale.z-.90f;
            float left=-width/2+.025f,right=.015f,doorX=flight.localPosition.x;
            var white=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/WarmWhite.mat");
            var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/Metal.mat");
            var navy=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/Navy.mat");
            var tile=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/StairTile.mat");
            var paint=P.Mat("StairStorageDoorPaint",new Color(.76f,.77f,.74f),.23f);var frame=P.Mat("StairStorageFrame",new Color(.80f,.81f,.79f),.26f);
            SlopeWall(storage,"LeftSlopedWall",left,left+.08f,back,white);SlopeWall(storage,"DividerSlopedWall",right-.08f,right,back,white);
            Solid(storage,"BackWall",new Vector3((left+right)/2,BackTop/2,back+.04f),new Vector3(right-left,BackTop,.08f),white,true);
            float holeLeft=doorX-LeafWidth/2-.04f,holeRight=doorX+LeafWidth/2+.04f,openingHeight=2.16f;
            Solid(storage,"FrontLeftPier",new Vector3((left+holeLeft)/2,FrontTop/2,Front),new Vector3(holeLeft-left,FrontTop,.12f),white,true);
            Solid(storage,"FrontRightPier",new Vector3((right+holeRight)/2,FrontTop/2,Front),new Vector3(right-holeRight,FrontTop,.12f),white,true);
            Solid(storage,"FrontHeader",new Vector3((left+right)/2,(openingHeight+FrontTop)/2,Front),new Vector3(right-left,FrontTop-openingHeight,.12f),white,true);
            Solid(storage,"StorageFloor",new Vector3((left+right)/2,.015f,(Front+back)/2),new Vector3(right-left-.16f,.02f,back-Front),tile);
            var roofA=new Vector3((left+right)/2,FrontTop-.035f,Front);var roofB=new Vector3((left+right)/2,BackTop-.035f,back);
            var roof=Solid(storage,"SlopedCeiling",(roofA+roofB)/2,new Vector3(right-left-.16f,.06f,(roofB-roofA).magnitude),white);roof.localRotation=Quaternion.LookRotation(roofB-roofA);
            var door=P.Group(storage,"StorageDoor");door.localPosition=new Vector3(doorX,0,Front);
            Solid(door,"ClosedLeaf",new Vector3(0,LeafHeight/2+.018f,-.02f),new Vector3(LeafWidth,LeafHeight,.032f),paint,true);
            for(int i=0;i<2;i++)Solid(door,"FrameJamb"+i,new Vector3((i==0?-1:1)*(LeafWidth/2+.025f),1.08f,-.062f),new Vector3(.05f,2.16f,.10f),frame);
            Solid(door,"FrameHead",new Vector3(0,2.14f,-.062f),new Vector3(LeafWidth+.10f,.05f,.10f),frame);
            Solid(door,"VentShadow",new Vector3(0,.29f,-.039f),new Vector3(.59f,.28f,.005f),navy);
            for(int i=0;i<7;i++)Solid(door,"VentLouver"+i,new Vector3(0,.17f+i*.04f,-.047f),new Vector3(.57f,.022f,.012f),frame);
            Solid(door,"VentLeftEdge",new Vector3(-.30f,.29f,-.047f),new Vector3(.015f,.30f,.012f),frame);
            Solid(door,"VentRightEdge",new Vector3(.30f,.29f,-.047f),new Vector3(.015f,.30f,.012f),frame);
            for(int i=0;i<2;i++)Solid(door,"VentHorizontalEdge"+i,new Vector3(0,i==0?.14f:.44f,-.047f),new Vector3(.61f,.015f,.012f),frame);
            P.Cylinder(door,"HandleRosette",new Vector3(.30f,.96f,-.054f),new Vector3(.065f,.006f,.065f),metal,Quaternion.Euler(90,0,0));
            P.Tube(door,"HandleStem",new Vector3(.30f,.96f,-.060f),new Vector3(.30f,.96f,-.090f),.018f,metal);
            P.Tube(door,"LeverHandle",new Vector3(.30f,.96f,-.090f),new Vector3(.18f,.96f,-.090f),.021f,metal);
            for(int i=0;i<3;i++)P.Cylinder(door,"Hinge"+i,new Vector3(-LeafWidth/2-.004f,.34f+i*.72f,-.037f),new Vector3(.018f,.029f,.018f),metal,Quaternion.identity);
            Solid(door,"StorageSignBacking",new Vector3(0,1.46f,-.039f),new Vector3(.115f,.14f,.006f),navy);
            Label(door,"StorageTitle","창고",new Vector3(0,1.477f,-.044f),new Vector2(.095f,.044f),22,Color.white);
            Label(door,"StorageEnglish","Storage",new Vector3(0,1.431f,-.044f),new Vector2(.095f,.022f),11,Color.white);
            Solid(door,"SignGoldDivider",new Vector3(0,1.450f,-.044f),new Vector3(.095f,.001f,.001f),metal);
            Solid(storage,"FrontSkirtingLeft",new Vector3((left+holeLeft)/2,.045f,Front-.064f),new Vector3(holeLeft-left,.09f,.008f),navy);
            Solid(storage,"FrontSkirtingRight",new Vector3((right+holeRight)/2,.045f,Front-.064f),new Vector3(right-holeRight,.09f,.008f),navy);
            Solid(storage,"DividerSkirting",new Vector3(right+.004f,.045f,(Front+back)/2),new Vector3(.008f,.09f,back-Front),navy);
        }
        public static void Validate(GameObject root)
        {
            foreach(string name in Halls)
            {
                var hall=root.transform.Find("CoreHalls/"+name);var storage=hall.Find("UnderStairStorage");var stairs=hall.Find("SwitchbackStairs");
                if(!storage||hall.Cast<Transform>().Count(t=>t.name=="UnderStairStorage")!=1)throw new Exception("Missing/duplicate stair storage");
                var door=storage.Find("StorageDoor");if(door.GetComponentInChildren<Text>().text!="창고"||door.Cast<Transform>().Count(t=>t.name.StartsWith("VentLouver"))!=7)throw new Exception("Storage door title/louver missing");
                if(Mathf.Abs(door.localPosition.x-stairs.Find("LeftReturnStep9").localPosition.x)>.001f||door.localPosition.z<.74f)throw new Exception("Storage is not beneath returning stair flight");
                var boundary=root.transform.Find("RoomsAndCores/"+(name==Halls[0]?"EastStair":"WestStair")+"/RoomFloor").GetComponent<Renderer>().bounds;
                foreach(var r in storage.GetComponentsInChildren<Renderer>())
                {
                    var b=r.bounds;if(b.min.x<boundary.min.x+.055f||b.max.x>boundary.max.x-.055f||b.min.z<boundary.min.z+.055f||b.max.z>boundary.max.z-.055f||b.min.y<-.002f||b.max.y>2.85f)throw new Exception("Storage crosses original stair core boundary: "+r.name);
                }
                float lowestRightEdge=stairs.Find("RightLowerStep0").localPosition.x-stairs.Find("RightLowerStep0").localScale.x/2;
                float storageRight=storage.Find("DividerSlopedWall").GetComponent<MeshFilter>().sharedMesh.bounds.max.x;
                if(storageRight>=lowestRightEdge)throw new Exception("Storage narrows ascending stair treads");
                if(!hall.Find("StairFlightBlocker").GetComponent<BoxCollider>().enabled||!door.Find("ClosedLeaf").GetComponent<BoxCollider>().enabled)throw new Exception("Stair/storage guard missing");
            }
            ConvergenceHallFinalFinishes.Validate(root);
            Debug.Log("STAIR_STORAGE_OK: both white enclosed rooms below left return flights; closed doors with 7 louvers, handles and native Korean/English signs; right stair width/perimeter and stair guards retained.");
        }
        public static void ApplyAndCapture()
        {
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));
            int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));
            if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Storage authoring duplicated geometry");
            SessionState.SetBool("EEG.ConvergenceQA.FocusStairStorage",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
