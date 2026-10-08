using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallClassroomInteriors
    {
        public static readonly string[] Rooms={"B101","B102","B103","B104","B105","B106","B107","B108","B109","B110","B112","B113","B114","B115"};
        const float H=2.85f,DeskW=1.50f,RowPitch=1.35f;
        static Material board,frame,black,screen,light,ceiling,white;
        static Transform Group(Transform p,string name){var t=p.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(p,false);}return t;}
        static bool North(string name)=>name=="B104"||name=="B105"||name=="B108"||name=="B109"||name=="B112";
        static void Materials()
        {
            board=P.Mat("InteriorWhiteboard",new Color(.84f,.88f,.85f),.58f);frame=P.Mat("InteriorBoardFrame",new Color(.47f,.49f,.48f),.5f);
            black=P.Mat("FurnitureBlack",new Color(.018f,.021f,.025f),.25f);white=P.Mat("FurnitureWhite",new Color(.91f,.92f,.91f),.40f);
            screen=P.Mat("ProjectionScreen",new Color(.79f,.83f,.86f),.20f);light=P.Mat("ClassroomLED",new Color(.94f,.98f,1f),.15f);
            ceiling=P.Mat("ClassroomAcousticCeiling",new Color(.87f,.88f,.87f),.05f);
            const string path="Assets/Art/Materials/ConvergenceHall/ClassroomCeilingTexture.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!texture){texture=new Texture2D(256,256,TextureFormat.RGBA32,true){name="ClassroomCeilingTexture",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};var pixels=new Color[256*256];var random=new System.Random(161);for(int y=0;y<256;y++)for(int x=0;x<256;x++){float n=(float)random.NextDouble();float v=x<1||y<1?.68f:n<.08f?.80f:.96f+n*.04f;pixels[y*256+x]=new Color(v,v,v);}texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,path);}
            ceiling.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(ceiling);
        }
        static void Board(Transform parent,string name,float width,Vector3 at,float yaw)
        {
            var g=P.Group(parent,name);g.localPosition=at;g.localRotation=Quaternion.Euler(0,yaw,0);
            P.Box(g,"BoardFrame",Vector3.zero,new Vector3(width,1.22f,.035f),frame);
            P.Box(g,"WhiteboardFace",new Vector3(0,0,-.025f),new Vector3(width-.035f,1.18f,.012f),board);
            P.Box(g,"MarkerTray",new Vector3(0,-.615f,-.055f),new Vector3(width,.026f,.09f),frame);
            for(int i=0;i<3;i++)P.Box(g,"BoardSectionJoint"+i,new Vector3(-width/2+width*(i+1)/4,0,-.033f),new Vector3(.003f,1.18f,.003f),frame);
            P.Box(g,"Eraser",new Vector3(width*.34f,-.594f,-.060f),new Vector3(.11f,.04f,.035f),white);
            for(int i=0;i<3;i++)P.Box(g,"Marker"+i,new Vector3(width*.15f+i*.13f,-.597f,-.070f),new Vector3(.10f,.008f,.009f),i==0?black:i==1?P.Mat("BoardMarkerBlue",new Color(.025f,.10f,.46f)):P.Mat("BoardMarkerRed",new Color(.48f,.025f,.025f)));
        }
        static void Teaching(Transform room,string name,float width,float depth)
        {
            float z=depth/2-.15f;
            Board(room,"FrontWhiteboard",width-1.30f,new Vector3(0,1.64f,z),0);
            bool sideRight=name=="B108"||name=="B109"||name=="B101"||name=="B102"||name=="B103";
            float sign=sideRight?1:-1;
            Board(room,"SideWhiteboard",Mathf.Min(depth-2.2f,8f),new Vector3(sign*(width/2-.15f),1.64f,0),sideRight?90:-90);
            float sw=Mathf.Min(3.35f,width-2.0f),sx=width<6?-.25f:-.75f;
            var projection=P.Group(room,"ProjectionSystem");projection.localPosition=new Vector3(sx,1.65f,z-.070f);
            P.Box(projection,"ScreenBorder",Vector3.zero,new Vector3(sw+ .075f,1.93f,.025f),black);
            P.Box(projection,"ScreenSurface",new Vector3(0,0,-.020f),new Vector3(sw,1.855f,.006f),screen);
            P.Box(projection,"ScreenRoller",new Vector3(0,.995f,0),new Vector3(sw+.12f,.055f,.085f),white);
            var lectern=P.Instance(room,"ElectronicLectern","Lectern",new Vector3(.30f,.010f,depth/2-1.12f));
            if(name!="B106")P.Instance(room,"BlueMeshChair","TeacherChair",new Vector3((name=="B109"?-1:1)*(width/2-.90f),.010f,depth/2-1.1f));
            // Projector is suspended below the real wall/ceiling height.
            var projector=P.Group(room,"CeilingProjector");projector.localPosition=new Vector3(sx,H-.26f,depth/2-3.5f);
            P.Cylinder(projector,"Mount",new Vector3(0,.11f,0),new Vector3(.025f,.11f,.025f),frame,Quaternion.identity);
            P.Box(projector,"Body",Vector3.zero,new Vector3(.38f,.145f,.30f),white);
            P.Cylinder(projector,"Lens",new Vector3(.09f,0,.17f),new Vector3(.075f,.035f,.075f),black,Quaternion.Euler(90,0,0));
            for(int i=0;i<8;i++)P.Box(projector,"Vent"+i,new Vector3(-.195f,.025f-i*.010f,0),new Vector3(.006f,.003f,.22f),frame);
            foreach(float x in new[]{-width/2+.28f,width/2-.28f})
            {
                var speaker=P.Box(room,"FrontSpeaker",new Vector3(x,2.55f,depth/2-.26f),new Vector3(.22f,.25f,.22f),black);speaker.localRotation=Quaternion.Euler(18,0,0);
            }
            P.Box(room,"WallSwitchPanel",new Vector3(width/2-.43f,1.1f,z-.05f),new Vector3(.23f,.12f,.025f),white);
            for(int i=0;i<3;i++)P.Box(room,"Switch"+i,new Vector3(width/2-.51f+i*.075f,1.10f,z-.070f),new Vector3(.045f,.07f,.010f),frame);
        }
        static void Ceiling(Transform room,Transform floor,float width,float depth,string name)
        {
            var source=floor.GetComponent<MeshFilter>().sharedMesh;var vertices=source.vertices;var triangles=source.triangles;var v=new List<Vector3>();var tr=new List<int>();var uv=new List<Vector2>();float top=floor.GetComponent<Renderer>().bounds.max.y;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=floor.TransformPoint(vertices[triangles[i]]);var b=floor.TransformPoint(vertices[triangles[i+1]]);var c=floor.TransformPoint(vertices[triangles[i+2]]);
                if(Mathf.Abs(a.y-top)>.002f||Mathf.Abs(b.y-top)>.002f||Mathf.Abs(c.y-top)>.002f)continue;
                if(Vector3.Cross(b-a,c-a).y>0){var swap=b;b=c;c=swap;}
                foreach(var world in new[]{a,b,c}){var p=room.InverseTransformPoint(world);p.y=H-.012f;tr.Add(v.Count);v.Add(p);uv.Add(new Vector2(world.x/.6f,world.z/.6f));}
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var t=P.Group(room,"CeilingUnderside");t.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("Interior_"+name+"Ceiling",mesh);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=ceiling;
            t.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            int cols=Mathf.Max(2,Mathf.RoundToInt(width/2.7f)),rows=Mathf.Max(2,Mathf.RoundToInt(depth/2.5f));
            for(int y=0;y<rows;y++)for(int x=0;x<cols;x++)
            {
                float px=-width/2+width*(x+.5f)/cols,pz=-depth/2+depth*(y+.5f)/rows;
                P.Box(room,"LEDFrame_"+x+"_"+y,new Vector3(px,H-.045f,pz),new Vector3(1.20f,.025f,.275f),frame);
                P.Box(room,"LEDPanel_"+x+"_"+y,new Vector3(px,H-.061f,pz),new Vector3(1.16f,.008f,.235f),light);
            }
            var ac=P.Group(room,"CassetteAirConditioner");ac.localPosition=new Vector3(0,H-.048f,depth/2-3.0f);
            P.Box(ac,"CassetteFrame",Vector3.zero,new Vector3(.82f,.045f,.82f),white);P.Box(ac,"Intake",new Vector3(0,-.028f,0),new Vector3(.50f,.014f,.50f),frame);
            for(int i=0;i<4;i++)P.Box(ac,"Louver"+i,new Vector3(0,-.038f,-.35f+i*.022f),new Vector3(.68f,.004f,.012f),black);
            for(int i=0;i<3;i++)P.Cylinder(room,"CeilingVent"+i,new Vector3(width*.28f,H-.040f,-depth*.25f+i*depth*.25f),new Vector3(.20f,.009f,.20f),white,Quaternion.identity);
            P.Cylinder(room,"SmokeDetector",new Vector3(-width*.25f,H-.04f,0),new Vector3(.10f,.016f,.10f),white,Quaternion.identity);
        }
        static bool FloorPoint(Transform floor,Vector3 world)
        {
            var mesh=floor.GetComponent<MeshFilter>().sharedMesh;var v=mesh.vertices;var tr=mesh.triangles;var p=floor.InverseTransformPoint(world);
            float Cross(Vector3 a,Vector3 b,Vector3 c)=>(b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x);
            for(int i=0;i<tr.Length;i+=3)
            {
                var a=v[tr[i]];var b=v[tr[i+1]];var c=v[tr[i+2]];float area=Cross(a,b,c);if(Mathf.Abs(area)<.00001f)continue;
                float x=Cross(a,b,p),y=Cross(b,c,p),z=Cross(c,a,p);if(area>0?x>=-.00001f&&y>=-.00001f&&z>=-.00001f:x<=.00001f&&y<=.00001f&&z<=.00001f)return true;
            }
            return false;
        }
        static bool Fits(Transform floor,Transform room,float x,float z)
        {
            foreach(float dx in new[]{-.78f,.78f})foreach(float dz in new[]{-.83f,.305f})if(!FloorPoint(floor,room.TransformPoint(new Vector3(x+dx,0,z+dz))))return false;return true;
        }
        static void BuildRoom(GameObject root,Transform parent,string name)
        {
            var floor=root.transform.Find("RoomsAndCores/"+name+"/RoomFloor");var b=floor.GetComponent<Renderer>().bounds;bool north=North(name);
            float width=north?b.size.x:b.size.z,depth=north?b.size.z:b.size.x;
            var room=Group(parent,name);room.position=new Vector3(b.center.x,0,b.center.z);room.rotation=Quaternion.Euler(0,name=="B109"?180:north?0:90,0);
            int columns=width>=7.70f?3:width>=5.50f?2:1;float[] gaps=columns==3?new[]{1.20f,.70f}:new[]{1.20f};
            if(name=="B112")
            {
                columns=3;float gap=(width-.24f-columns*DeskW)/2;
                if(gap<=0)throw new Exception("B112 three desk columns do not fit between the side walls.");
                gaps=new[]{gap,gap};
            }
            float occupied=columns*DeskW+(columns>1?gaps.Take(columns-1).Sum():0),start=-occupied/2+DeskW/2;
            int rows=Mathf.Max(1,Mathf.FloorToInt((depth-4.03f)/RowPitch)+1);var furniture=P.Group(room,"Seating");int desks=0;
            int firstRow=0;if(name=="B112"){firstRow=1;rows=7;}
            for(int row=0;row<rows;row++)
            {
                float z=depth/2-2.2f-(row+firstRow)*RowPitch,x=start;
                for(int col=0;col<columns;col++)
                {
                    if(Fits(floor,room,x,z))
                    {
                        float placedX=x;
                        // Keep the previously measured B112 outer rows against their side walls.
                        if(name=="B112"&&col!=1)placedX+=(col==0?-1:1)*DeskW*(1-P.FurnitureScale)/2;
                        P.Instance(furniture,"TwoSeatFoldingDesk","Desk_"+row+"_"+col,new Vector3(placedX,.010f,z));
                        P.Instance(furniture,"BlueMeshChair","Chair_"+row+"_"+col+"_A",new Vector3(placedX-.37f,.010f,z-.52f));
                        P.Instance(furniture,"BlueMeshChair","Chair_"+row+"_"+col+"_B",new Vector3(placedX+.37f,.010f,z-.52f));desks++;
                    }
                    if(col<columns-1)x+=DeskW+gaps[col];
                }
            }
            Teaching(room,name,width,depth);Ceiling(room,floor,width,depth,name);
            // Combine by material to keep repeated desk/chair geometry inexpensive to draw.
            P.Combine(room,"Interior_"+name+"Combined");
            Debug.Log("CLASSROOM_LAYOUT: "+name+" desks="+desks+" student seats="+(desks*2)+" columns="+columns+" rows="+rows+" front="+(north?"North":"East"));
        }
        static void WhiteTeachingWall(GameObject root,string roomName,string side)
        {
            var edge=root.transform.Find("RoomsAndCores/"+roomName+"/"+side);
            foreach(string name in new[]{"CorridorGlazing","MeasuredGlassPanels","DoorOpeningSurfaces"}){var old=edge.Find(name);if(old)old.gameObject.SetActive(false);}
            var wall=edge.Find("LowerWall");wall.gameObject.SetActive(true);wall.GetComponent<Renderer>().enabled=true;wall.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/WarmWhite.mat");
            var size=wall.localScale;size.y=H;wall.localScale=size;var at=wall.localPosition;at.y=H/2;wall.localPosition=at;
            edge.Find("Skirting").gameObject.SetActive(true);
        }
        public static void BuildSpecific(GameObject root,string name)
        {
            Materials();var parent=Group(root.transform,"ClassroomInteriors");var old=parent.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            BuildRoom(root,parent,name);
        }
        public static void ApplyTo(GameObject root)
        {
            WhiteTeachingWall(root,"B101","East");WhiteTeachingWall(root,"B109","South");
            P.BuildAssets();Materials();var group=Group(root.transform,"ClassroomInteriors");foreach(Transform child in group.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach(string room in Rooms)BuildRoom(root,group,room);
        }
        static void Validate(GameObject root)
        {
            if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/ClassroomLED.mat").IsKeywordEnabled("_EMISSION")||root.transform.Find("ClassroomInteriors").GetComponentsInChildren<Light>().Length>0)throw new Exception("Artificial classroom glow remains.");
            var group=root.transform.Find("ClassroomInteriors");if(!group||group.childCount!=Rooms.Length)throw new Exception("Expected "+Rooms.Length+" classroom interiors.");
            int total=0;
            foreach(string name in Rooms)
            {
                var room=group.Find(name);var floor=root.transform.Find("RoomsAndCores/"+name+"/RoomFloor");var seating=room.Find("Seating");int desks=seating.Cast<Transform>().Count(t=>t.name.StartsWith("Desk_"));int chairs=seating.Cast<Transform>().Count(t=>t.name.StartsWith("Chair_"));
                if(desks<3||chairs!=desks*2||!room.Find("Lectern")||!room.Find("FrontWhiteboard")||!room.Find("ProjectionSystem")||!room.Find("CeilingUnderside"))throw new Exception("Incomplete interior: "+name);
                foreach(var desk in seating.Cast<Transform>().Where(t=>t.name.StartsWith("Desk_")))
                {var local=room.InverseTransformPoint(desk.position);if(!Fits(floor,room,local.x,local.z))throw new Exception("Furniture crosses room floor: "+name);}
                var visible=room.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(visible.Length!=1)throw new Exception("Room furniture was not combined for rendering.");
                var rendered=visible[0];var merged=rendered.GetComponent<MeshFilter>().sharedMesh;int boardIndex=Array.FindIndex(rendered.sharedMaterials,m=>m.name=="InteriorWhiteboard");
                Bounds expected=new Bounds();bool first=true;
                foreach(var source in room.GetComponentsInChildren<MeshRenderer>().Where(r=>r!=rendered&&r.sharedMaterials.Any(m=>m.name=="InteriorWhiteboard")))
                    foreach(var point in source.GetComponent<MeshFilter>().sharedMesh.vertices){var p=room.InverseTransformPoint(source.transform.TransformPoint(point));if(first){expected=new Bounds(p,Vector3.zero);first=false;}else expected.Encapsulate(p);}
                var vertices=merged.vertices;Bounds actual=new Bounds();first=true;
                foreach(int index in merged.GetTriangles(boardIndex)){var p=vertices[index];if(first){actual=new Bounds(p,Vector3.zero);first=false;}else actual.Encapsulate(p);}
                if(Vector3.Distance(expected.min,actual.min)>.004f||Vector3.Distance(expected.max,actual.max)>.004f)throw new Exception("Rendered whiteboard geometry differs from layout: "+name);
                if(room.Find("CeilingUnderside").GetComponent<MeshFilter>().sharedMesh.normals.Any(n=>n.y>-.9f))throw new Exception("Ceiling must face downward for overhead visibility.");total+=chairs;
            }
            Physics.SyncTransforms();
            foreach(var item in new[]{new[]{"B101","East"},new[]{"B109","South"}})
            {
                var edge=root.transform.Find("RoomsAndCores/"+item[0]+"/"+item[1]);var wall=edge.Find("LowerWall");
                if(!wall.gameObject.activeInHierarchy||wall.GetComponent<Renderer>().sharedMaterial.name!="WarmWhite"||edge.Find("MeasuredGlassPanels")&&edge.Find("MeasuredGlassPanels").gameObject.activeInHierarchy)throw new Exception("Teaching wall is not solid white: "+item[0]);
                var direction=item[0]=="B101"?Vector3.right:Vector3.back;if(Vector3.Dot(group.Find(item[0]).forward,direction)<.999f)throw new Exception("Teaching direction mismatch.");
            }
            if(root.transform.Find("CoreHalls").GetComponentsInChildren<Light>().Length!=0)throw new Exception("Elevator supplemental lighting remains.");
            var b112=group.Find("B112");var b112Seating=b112.Find("Seating");var b112Floor=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;
            var columns=b112Seating.Cast<Transform>().Where(t=>t.name.StartsWith("Desk_")).GroupBy(t=>t.name.Split('_')[2]).OrderBy(g=>g.Key).ToArray();
            if(columns.Length!=3)throw new Exception("B112 must have three desk columns.");
            if(columns.Any(c=>c.Count()!=7))throw new Exception("B112 must keep seven rows after removing the front row.");
            float expectedFront=b112Floor.size.z/2-2.2f-RowPitch;
            if(Mathf.Abs(b112.InverseTransformPoint(b112Seating.Find("Desk_0_0").position).z-expectedFront)>.002f)throw new Exception("B112 seating starts at the wrong original row.");
            foreach(var desk in columns[0])if(Mathf.Abs(desk.GetComponent<BoxCollider>().bounds.min.x-(b112Floor.min.x+.12f))>.002f)throw new Exception("B112 left desk column is not against the wall.");
            foreach(var desk in columns[2])if(Mathf.Abs(desk.GetComponent<BoxCollider>().bounds.max.x-(b112Floor.max.x-.12f))>.002f)throw new Exception("B112 right desk column is not against the wall.");
            foreach(var desk in columns[1])if(Mathf.Abs(b112.InverseTransformPoint(desk.position).x)>.002f)throw new Exception("B112 middle column is not centered.");
            Debug.Log("B112_THREE_COLUMNS_OK: three columns, outer tabletops meet the inner side-wall faces, middle column centered.");
            Debug.Log("CLASSROOM_INTERIORS_OK: "+Rooms.Length+" furnished rooms, "+total+" student seats; floor containment, rounded B108 footprint, downward ceilings and combined renderers passed.");
        }
        [MenuItem("Tools/EEG Wheelchair/Apply Photo Classroom Interiors")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ConvergenceHallWallMaterials.ApplyTo(root);Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
        }
        public static void ApplyAndValidate()
        {Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Repeated interior update duplicates furniture.");ConvergenceHallB1Validation.BuildAndValidate();}
        public static void ApplyAndCapture()
        {Apply();SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();}
        public static void ApplyAndCaptureB112()
        {Apply();SessionState.SetString("EEG.ConvergenceQA.FocusRoom","B112");SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();}
        public static void ApplyDirectionCorrection()
        {
            Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Direction correction duplicated geometry.");
            SessionState.SetString("EEG.ConvergenceQA.FocusRoom","B101,B109");SessionState.SetBool("EEG.ConvergenceQA.FocusElevators",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
