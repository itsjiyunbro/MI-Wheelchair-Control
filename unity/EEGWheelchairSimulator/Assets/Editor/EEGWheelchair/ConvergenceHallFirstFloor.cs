using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    // Rectified schematic traced from the supplied 1F evacuation plan (26 Oct 2026).
    // u runs across the portrait diagram, v runs down it; metres are estimates.
    // The measured B1 Bluebell landing is the registration anchor, at y=4.50m.
    [InitializeOnLoad]
    public static partial class ConvergenceHallFirstFloor
    {
        public const string Prefab="Assets/Art/Prefabs/Environment/ConvergenceHall/ConvergenceHall1F.prefab";
        public const string Scene="Assets/Scenes/ConvergenceHall1FScene.unity";
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        const float H=2.85f,StairV=31.808f,StairBottomU=30.2f;
        static Material white,navy,black,silver,stone,tile,wood,concrete,mint,glass,lamp,red;
        static Font font;
        static Transform root,architecture,roofs,details;
        static int obstacle;
        static Vector3 At(float u,float v,float h=0)=>new Vector3(-v,h,-u);
        public static Vector3 World(Transform floor,float u,float v,float h=0)=>floor.TransformPoint(At(u,v,h));
        struct R
        {
            public float u0,v0,u1,v1;
            public R(float a,float b,float c,float d){u0=a;v0=b;u1=c;v1=d;}
            public bool Contains(float u,float v)=>u>u0+.0001f&&u<u1-.0001f&&v>v0+.0001f&&v<v1-.0001f;
        }
        static ConvergenceHallFirstFloor()
        {
            if(SessionState.GetBool("FirstFloor.Checking",false))EditorApplication.update+=Tick;
            if(Application.isBatchMode&&Environment.GetCommandLineArgs().Any(a=>a.Contains("ConvergenceHallFirstFloor.")))EditorApplication.update+=SkipSearch;
        }
        static void SkipSearch()
        {
            var pending=EditorApplication.delayCall;if(pending==null)return;
            foreach(var c in pending.GetInvocationList())if(c.Method.DeclaringType?.FullName=="UnityEditor.Search.SearchInit"&&c.Method.Name=="IndexationOnStartup")EditorApplication.delayCall-=(EditorApplication.CallbackFunction)c;
        }
        static Material Surface(string name,Color color,string texture=null,float repeat=.6f)
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(Mats+name+".mat");
            if(!m){m=new Material(Shader.Find("ConvergenceHall/Lounge Matte Surface")){name=name};AssetDatabase.CreateAsset(m,Mats+name+".mat");}
            m.SetColor("_BaseColor",color);m.SetFloat("_TextureMetres",repeat);
            m.SetTexture("_BaseMap",texture==null?Texture2D.whiteTexture:AssetDatabase.LoadAssetAtPath<Texture2D>(Mats+texture+".asset"));
            m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static void Materials()
        {
            white=Surface("1F_WhitePaint",new Color(.91f,.91f,.88f));
            navy=Surface("1F_NavySlat",new Color(.032f,.052f,.10f));black=Surface("1F_Charcoal",new Color(.035f,.039f,.043f));
            silver=Surface("1F_SatinAluminium",new Color(.64f,.66f,.66f));
            stone=Surface("1F_SpeckledStone",Color.white,"MeasuredSpeckledStoneTexture");
            tile=Surface("1F_GrayTile",new Color(1.12f,1.10f,1.05f),"MeasuredGrayFloorTileTexture");
            wood=Surface("1F_Oak",new Color(1.13f,1.08f,1),"BluebellOakTexture",1.4f);
            concrete=Surface("1F_Concrete",new Color(.79f,.80f,.77f),"B112ConcreteGrain",2.4f);
            mint=Surface("1F_PrivacyFilm",new Color(.57f,.72f,.68f));
            glass=AssetDatabase.LoadAssetAtPath<Material>(Mats+"BluebellRailGlass.mat");
            lamp=Surface("1F_LampDiffuser",new Color(.98f,.98f,.95f));red=Surface("1F_ExtinguisherRed",new Color(.64f,.035f,.03f));
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");
            obstacle=LayerMask.NameToLayer("WheelchairObstacle");
        }
        static Transform Box(Transform p,string n,Vector3 at,Vector3 size,Material m,bool collision=false)
        {
            var t=P.Box(p,n,at,size,m);
            if(collision){t.gameObject.AddComponent<BoxCollider>();t.gameObject.layer=obstacle;}
            if(m==glass)t.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            return t;
        }
        static Transform Rect(Transform p,string n,R r,float y,float thick,Material m,bool collision=false)
        {return Box(p,n,At((r.u0+r.u1)/2,(r.v0+r.v1)/2,y),new Vector3(r.v1-r.v0,thick,r.u1-r.u0),m,collision);}
        static void Label(Transform p,string n,string value,Vector3 at,Vector2 size,Color color,int fontSize=60)
        {
            var t=new GameObject(n,typeof(RectTransform),typeof(Canvas),typeof(Text)).transform;t.SetParent(p,false);t.localPosition=at;t.localScale=Vector3.one*.001f;
            ((RectTransform)t).sizeDelta=size/.001f;t.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var text=t.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=fontSize;text.resizeTextForBestFit=true;text.resizeTextMinSize=10;text.resizeTextMaxSize=fontSize;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        }
        // All wall local z+ points into the room, its corridor face is local z-.
        static Transform Face(Transform p,string name,Vector3 centre,float yaw)
        {var g=P.Group(p,name);g.localPosition=centre;g.localRotation=Quaternion.Euler(0,yaw,0);return g;}
        static void Panel(Transform g,string name,float a,float b,bool glazed,Material solid=null)
        {
            if(b-a<.01f)return;float mid=(a+b)/2,w=b-a;
            if(!glazed)Box(g,name,new Vector3(mid,H/2,.065f),new Vector3(w,H,.13f),solid?solid:white,true);
            else
            {
                Box(g,name+"GlassLow",new Vector3(mid,.14f,.062f),new Vector3(w,.25f,.020f),glass);
                Box(g,name+"Privacy",new Vector3(mid,1.31f,.063f),new Vector3(w,2.08f,.022f),mint);
                Box(g,name+"GlassHigh",new Vector3(mid,2.60f,.062f),new Vector3(w,.49f,.020f),glass);
                var barrier=g.gameObject.AddComponent<BoxCollider>();barrier.center=new Vector3(mid,H/2,.065f);barrier.size=new Vector3(w,H,.04f);g.gameObject.layer=obstacle;
                int count=Mathf.CeilToInt(w/1.05f);for(int i=0;i<=count;i++)Box(g,name+"Mullion"+i,new Vector3(Mathf.Lerp(a,b,i/(float)count),H/2,.06f),new Vector3(.018f,H,.05f),silver);
                Box(g,name+"GlassHead",new Vector3(mid,H-.02f,.06f),new Vector3(w,.04f,.05f),silver);
            }
            Box(g,name+"Skirting",new Vector3(mid,.035f,-.006f),new Vector3(w,.07f,.014f),glazed?silver:black);
        }
        static bool IsNumberedRoom(string code)=>int.TryParse(code,out _)||code=="107-1";
        static void Plaque(Transform g,string code,string title,float x,bool glazedWall=false)
        {
            if(IsNumberedRoom(code))
            {
                string english=code=="111"?"Computer Lab":code=="109"?"Convergence Research Lab":code=="106"?"Academic Advising":"";
                ConvergenceHallClassroomSigns.CreateRoomPlaque(g,"Sign_"+code,code,title,english,
                    new Vector3(x,1.49f,glazedWall?.048f:-.004f));
            }
            else
            {
                var sign=P.Group(g,"Sign_"+code);sign.localPosition=new Vector3(x,1.58f,-.034f);
                Box(sign,"Navy",Vector3.zero,new Vector3(.19f,.25f,.014f),navy);
                Box(sign,"NumberBand",new Vector3(0,.089f,-.009f),new Vector3(.19f,.070f,.008f),white);
                Label(sign,"RoomNumber",code,new Vector3(0,.089f,-.015f),new Vector2(.18f,.059f),Color.black,45);
                if(!string.IsNullOrEmpty(title))Label(sign,"RoomTitle",title,new Vector3(0,-.01f,-.014f),new Vector2(.18f,.095f),Color.white,27);
            }
            Box(g,"CardReader",new Vector3(x,1.15f,-.043f),new Vector3(.075f,.13f,.04f),black);
        }
        static void Door(Transform p,string code,string title,float width,bool clear=false,bool glazedWall=false)
        {
            const float dh=2.30f;float leafWidth=width>1.3f?width/2:width;
            for(int side=-1;side<=1;side+=2)Box(p,"Jamb"+side,new Vector3(side*(width/2+.025f),dh/2,0),new Vector3(.05f,dh,.13f),clear?black:white);
            Box(p,"Header",new Vector3(0,dh+.028f,0),new Vector3(width+.10f,.056f,.13f),clear?black:white);
            Box(p,"OpaqueTransom",new Vector3(0,(dh+.06f+H)/2,.06f),new Vector3(width+.10f,H-dh-.06f,.12f),white,true);
            var motions=new List<InteractiveDoor.Motion>();var colliders=new List<BoxCollider>();
            int leaves=width>1.3f?2:1;
            for(int i=0;i<leaves;i++)
            {
                float x=leaves==1?0:(i==0?-1:1)*width/4;
                var leaf=P.Group(p,"Leaf"+i);leaf.localPosition=new Vector3(x,0,0);
                Box(leaf,"Surface",new Vector3(0,dh/2,.025f),new Vector3(leafWidth-.014f,dh-.01f,.045f),clear?glass:white);
                if(clear)
                {
                    foreach(int s in new[]{-1,1})Box(leaf,"Stile"+s,new Vector3(s*(leafWidth/2-.025f),dh/2,0),new Vector3(.05f,dh,.075f),black);
                    foreach(float y in new[]{.035f,dh-.035f})Box(leaf,"Rail"+y,new Vector3(0,y,0),new Vector3(leafWidth,.07f,.075f),black);
                }
                float handle=(i==0?1:-1)*(leafWidth/2-.12f);
                foreach(int s in new[]{-1,1})
                {
                    P.Cylinder(leaf,"Rose"+s,new Vector3(handle,1.0f,s*.046f),new Vector3(.060f,.009f,.060f),silver,Quaternion.Euler(90,0,0));
                    P.Tube(leaf,"Lever"+s,new Vector3(handle,1,s*.075f),new Vector3(handle-.10f*(i==0?1:-1),1,s*.075f),.020f,silver);
                }
                foreach(float y in new[]{.22f,1.20f,2.10f})P.Cylinder(leaf,"Hinge"+y,new Vector3((i==0?-1:1)*leafWidth/2,y,-.018f),new Vector3(.022f,.065f,.022f),silver,Quaternion.identity);
                var c=leaf.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,dh/2,.025f);c.size=new Vector3(leafWidth,dh,.055f);leaf.gameObject.layer=obstacle;colliders.Add(c);
                motions.Add(new InteractiveDoor.Motion{target=leaf,closedPosition=leaf.localPosition,closedRotation=Quaternion.identity,hinge=new Vector3(x+(i==0?-1:1)*leafWidth/2,0,0),angle=i==0?-95:95});
            }
            var door=p.gameObject.AddComponent<InteractiveDoor>();door.Configure("1F "+code,motions.ToArray(),colliders.ToArray(),new Vector3(0,1.1f,-.1f));
            Plaque(p,code,title,-width/2-.17f,glazedWall);
        }
        static void Wall(Transform parent,string name,Vector3 at,float yaw,float length,string code=null,string title=null,bool glazed=false,float doorOffset=0,bool doubleDoor=false,Material solid=null)
        {
            var f=Face(parent,name,at,yaw);
            if(code==null){Panel(f,"Wall",-length/2,length/2,glazed,solid);return;}
            float w=doubleDoor?1.7f:1.0f,half=w/2+.05f;
            Panel(f,"Left",-length/2,doorOffset-half,glazed,solid);Panel(f,"Right",doorOffset+half,length/2,glazed,solid);
            var d=P.Group(f,"Door_"+code);d.localPosition=new Vector3(doorOffset,0,0);Door(d,code,title,w,glazedWall:glazed);
        }
        static void Room(string code,R r,string edge,string title="",bool glaze=false,bool doubles=false,Material front=null)
        {
            var room=P.Group(architecture,"Room"+code);float u=(r.u0+r.u1)/2,v=(r.v0+r.v1)/2;
            // Build the shell before adding any room-specific interior.
            Wall(room,"U0",At(r.u0,v),180,r.v1-r.v0,edge=="u0"?code:null,title,edge=="u0"&&glaze,0,doubles,edge=="u0"?front:null);
            Wall(room,"U1",At(r.u1,v),0,r.v1-r.v0,edge=="u1"?code:null,title,edge=="u1"&&glaze,0,doubles,edge=="u1"?front:null);
            Wall(room,"V0",At(u,r.v0),-90,r.u1-r.u0,edge=="v0"?code:null,title,edge=="v0"&&glaze,0,doubles,edge=="v0"?front:null);
            Wall(room,"V1",At(u,r.v1),90,r.u1-r.u0,edge=="v1"?code:null,title,edge=="v1"&&glaze,0,doubles,edge=="v1"?front:null);
            Rect(roofs,"Room"+code+"Ceiling",r,H+.05f,.10f,white);
        }
        static bool Atrium(float u,float v)=>v>=16&&(v<=35||(u>=30.8f&&v<=36.4f))&&!(u>28.8f&&v<25.5f);
        static void FloorAndRoof()
        {
            var holes=new[]{new R(0,0,23,4),new R(8,35,12.5f,52),new R(33,25.5f,40,29),new R(19,29,30.2f,34.616f),new R(13,18.4f,17,25.5f),new R(0,59.4f,3.6f,66)};
            var domains=new[]{new R(0,0,37,35),new R(0,35,15.8f,66),new R(20,35,37,66),new R(37,25.5f,40,36.4f)};
            var uBreaks=new[]{0f,2.4f,3,3.6f,8,12.5f,13,15.8f,17,19,20,23,27,28.8f,29.4f,30.2f,30.8f,31,33,33.2f,37,40};
            var vBreaks=new[]{0f,4,16,18.4f,25.5f,29,34.616f,35,36.4f,52,57,59.4f,61,66};
            var floors=P.Group(root,"Floors");
            for(int i=0;i<uBreaks.Length-1;i++)for(int j=0;j<vBreaks.Length-1;j++)
            {
                float u=(uBreaks[i]+uBreaks[i+1])/2,v=(vBreaks[j]+vBreaks[j+1])/2;
                if(!domains.Any(r=>r.Contains(u,v))||holes.Any(r=>r.Contains(u,v)))continue;
                var rect=new R(uBreaks[i],vBreaks[j],uBreaks[i+1],vBreaks[j+1]);
                var slab=Rect(floors,"Floor_"+i+"_"+j,rect,-.09f,.18f,Atrium(u,v)?stone:tile);slab.gameObject.AddComponent<BoxCollider>(); // Ground, excluded by obstacle mask.
            }
            // Corridor ceilings form continuous strips; room ceilings meet them exactly.
            var gray=new[]{new R(0,29,2.4f,59.4f),new R(0,57,15.8f,59.4f),new R(3.6f,59.4f,8.4f,66),new R(30.8f,36.4f,33.2f,66),new R(28.8f,18.4f,37,21),new R(15.8f,29,19,35)};
            foreach(var r in gray)Rect(roofs,"WhiteCorridorCeiling",r,H+.05f,.1f,white);
            // Navy slats stop at stair voids so the upper Bluebell is open vertically.
            var zones=new[]{new R(6,16,37,18.4f),new R(6,18.4f,8,27.5f),new R(8,25.5f,33,29),new R(17,18.4f,20,25.5f),new R(19,29,37,35),new R(30.8f,35,37,36.4f),new R(0,27.5f,19,29)};
            int id=0;foreach(var r in zones)Slats(r,H,"Lobby"+(id++));
            // West perimeter and ends of the long wings.
            Wall(architecture,"WestOuter",At(0,46),180,40);
            Wall(architecture,"NorthOuter",At(18.5f,0),-90,37);
            Wall(architecture,"EastOuterNorth",At(37,12.75f),0,25.5f);
            Wall(architecture,"EastOuterSouth",At(37,51.2f),0,29.6f);
            Wall(architecture,"LoungeWindowBacking",At(40,30.95f),0,10.9f);
            Wall(architecture,"LoungeNorthReturn",At(38.5f,25.5f),-90,3);
            Wall(architecture,"LoungePortalWallReturn",At(38.5f,36.4f),90,3);
            Slats(new R(37,25.5f,40,36.4f),H,"LoungeExtension");
            Slats(new R(33,25.5f,37,29),H,"LoungeVoidCover");
            Wall(architecture,"SouthWestOuter",At(9.7f,66),90,12.2f);
            Wall(architecture,"SouthEastExit",At(32f,66),90,2.4f,"EXIT","출입구");
            Wall(architecture,"WestNotchEnd",At(15.8f,62.7f),0,6.6f);
            Wall(architecture,"EastNotchEdge",At(20,50.5f),180,31,solid:concrete);
            foreach(var r in new[]{new R(8,35,12.5f,52),new R(33,25.5f,40,29)})VoidGuard(r);
            // The open shaft beneath the counter is the B1 stair, not a solid floor.
            Guard("BluebellOuterGuard",At(24.6f,34.7f,.55f),new Vector3(.14f,1.1f,11.2f));
            // Non-traversable stair access: no gravity/ramp shortcut in this wheelchair controller.
            var edge=Box(details,"StairEdgeSafety",At(19,31.808f,.55f),new Vector3(5.616f,1.1f,.05f),white,true);edge.GetComponent<Renderer>().enabled=false;
            var ascent=Box(details,"UpperStairSafety",At(14.8f,31.808f,.55f),new Vector3(5.616f,1.1f,.05f),white,true);ascent.GetComponent<Renderer>().enabled=false;
        }
        static void Guard(string n,Vector3 p,Vector3 size)
        {Box(details,n,p,size,white,true);Box(details,n+"Coping",p+Vector3.up*(size.y/2+.015f),new Vector3(size.x+.025f,.03f,size.z+.025f),silver);}
        static void VoidGuard(R r)
        {
            Guard("VoidGuardU0",At(r.u0,(r.v0+r.v1)/2,.55f),new Vector3(r.v1-r.v0,1.1f,.13f));
            Guard("VoidGuardU1",At(r.u1,(r.v0+r.v1)/2,.55f),new Vector3(r.v1-r.v0,1.1f,.13f));
            Guard("VoidGuardV0",At((r.u0+r.u1)/2,r.v0,.55f),new Vector3(.13f,1.1f,r.u1-r.u0));
            Guard("VoidGuardV1",At((r.u0+r.u1)/2,r.v1,.55f),new Vector3(.13f,1.1f,r.u1-r.u0));
        }
        static void Slats(R r,float height,string name)
        {
            var group=P.Group(roofs,"Navy_"+name);Rect(group,"Backing",r,height+.15f,.08f,black);
            for(float u=r.u0+.04f;u<r.u1;u+=.17f)Rect(group,"Slat",new R(u,r.v0,Mathf.Min(u+.055f,r.u1),r.v1),height,.17f,navy);
        }
        static void Downlight(float u,float v,bool onNavy=false)
        {
            var g=P.Group(details,"CeilingFitting");g.localPosition=At(u,v,H-.025f);
            P.Cylinder(g,"Trim",Vector3.zero,new Vector3(.18f,.018f,.18f),onNavy?black:silver,Quaternion.identity);
            P.Cylinder(g,"Diffuser",new Vector3(0,-.021f,0),new Vector3(.139f,.008f,.139f),lamp,Quaternion.identity);
        }
        static void Fixtures()
        {
            for(float v=30;v<65;v+=2.5f){Downlight(1.2f,v);Downlight(32f,v);}
            for(float u=2;u<15;u+=2.4f)Downlight(u,58.2f);
            for(float u=7.2f;u<36;u+=3f)Downlight(u,17.2f,true);
            for(float u=9;u<30;u+=3)Downlight(u,27.4f,true);
            for(float u=32;u<37;u+=2)for(float v=30;v<35;v+=3)Downlight(u,v,true);
            for(float v=32;v<64;v+=7)
            {
                Rect(details,"CeilingAccess",new R(.94f,v-.3f,1.46f,v+.3f),H-.012f,.016f,silver);
                Rect(details,"AccessInset",new R(.96f,v-.28f,1.44f,v+.28f),H-.023f,.012f,white);
                P.Cylinder(details,"Speaker",At(1.2f,v+1,H-.015f),new Vector3(.19f,.007f,.19f),silver,Quaternion.identity);
            }
            foreach(float v in new[]{32f,44f,56f})
            {
                var outlet=Face(details,"WallOutlet",At(.012f,v,.28f),180);Box(outlet,"Plate",Vector3.zero,new Vector3(.075f,.12f,.01f),white);
                P.Cylinder(outlet,"Socket",new Vector3(0,0,-.009f),new Vector3(.036f,.004f,.036f),silver,Quaternion.Euler(90,0,0));
            }
            foreach(var p in new[]{new Vector2(1.9f,30),new Vector2(29.9f,28),new Vector2(32.6f,60),new Vector2(15.23f,57.26f)})
            {
                var g=P.Group(details,"FireExtinguisher");g.localPosition=At(p.x,p.y);
                Box(g,"Base",new Vector3(0,.025f,0),new Vector3(.28f,.05f,.28f),red);
                P.Cylinder(g,"Bottle",new Vector3(0,.22f,0),new Vector3(.15f,.20f,.15f),red,Quaternion.identity);
                P.Tube(g,"Hose",new Vector3(.06f,.42f,0),new Vector3(.10f,.16f,0),.02f,black);Box(g,"Handle",new Vector3(0,.45f,0),new Vector3(.13f,.03f,.04f),black);
            }
        }
        static void Rooms()
        {
            TeachingRoom("111",new R(0,4,15,16),"v1","PC실습실",true,true,computers:true);
            TeachingRoom("112",new R(15,4,23,16),"v1","",true,true);
            TeachingRoom("113",new R(23,0,37,16),"v1","",true,true);
            Room("109",new R(2.4f,35,8,43),"u0","융합연구실");
            TeachingRoom("108",new R(2.4f,43,8,52),"u0",sideEntry:true);
            TeachingRoom("107",new R(2.4f,52,9.4f,57),"v1");
            Room("107-1",new R(11.8f,52,15.8f,57),"v1");
            Wall(architecture,"WallBetween107And107-1",At(10.6f,57),90,2.4f);
            TeachingRoom("106",new R(8.4f,59.4f,15.8f,66),"v0","학사지도상담실",true);
            TeachingRoom("104",new R(20,35,30.8f,46),"u1");
            TeachingRoom("105",new R(20,46,30.8f,57),"u1");
            TeachingRoom("101",new R(33.2f,36.4f,37,41),"u0",sideEntry:true);
            TeachingRoom("102",new R(33.2f,41,37,52),"u0",sideEntry:true);
            TeachingRoom("103",new R(33.2f,52,37,66),"u0",sideEntry:true);
            for(int i=0;i<3;i++)
            {
                Room((121+i).ToString(),new R(20+i*2.93f,18.4f,20+(i+1)*2.93f,21.95f),"v0","",true);
                Room((120-i).ToString(),new R(20+i*2.93f,21.95f,20+(i+1)*2.93f,25.5f),"v1","",true);
            }
            Room("117",new R(28.79f,21,31.5f,25.5f),"v0","",true);
            Room("116",new R(31.5f,21,34.2f,25.5f),"v0","",true);
            Room("115",new R(34.2f,21,37,25.5f),"v0","",true);
            // 114 is the end room on the short upper branch.
            Room("114",new R(35,18.4f,37,21),"u0");
            TeachingRoom("110",new R(2.4f,29,6.8f,35),"v0","",true);
            Room("WC-N-W",new R(0,16,6,21),"u1","여자 화장실");
            Room("WC-N-M",new R(0,21,6,27.5f),"u1","남자 화장실");
            Room("WC-S-W",new R(22,59.4f,30.8f,62.6f),"u1","여자 화장실");
            Room("WC-S-M",new R(22,62.6f,30.8f,66),"u1","남자 화장실");
            // Close recesses beside the sunken court with concrete, as reference 8.
            Wall(architecture,"ConcreteLongReturn",At(12.5f,43.5f),0,17,solid:concrete);
            Wall(architecture,"ConcretePCApproach",At(12.5f,18.4f),-90,9,solid:concrete);
            RoundedOfficeEnd();
        }
        static void RoundedOfficeEnd()
        {
            // Replace only the outer end of the two office shells with a continuous curve.
            foreach(string code in new[]{"120","121"})
            {
                var room=architecture.Find("Room"+code);UnityEngine.Object.DestroyImmediate(room.Find("U0").gameObject);
                var face=room.Find(code=="120"?"V1":"V0");
                // Keep door facades; a shallow curved end is outside the straight mullion line.
            }
            var g=P.Group(architecture,"RoundedOfficeEnd");
            for(int i=0;i<32;i++)
            {
                float a=Mathf.PI/2+Mathf.PI*i/32,b=Mathf.PI/2+Mathf.PI*(i+1)/32;
                // Half ellipse, tangent to both office frontages, bulges into the lobby.
                var p0=At(20+1.1f*Mathf.Cos(a),21.95f+3.55f*Mathf.Sin(a));
                var p1=At(20+1.1f*Mathf.Cos(b),21.95f+3.55f*Mathf.Sin(b));
                var d=p1-p0;var t=Box(g,"CurvedWhiteWall",(p0+p1)/2+Vector3.up*H/2,new Vector3(.13f,H,d.magnitude+.01f),white,true);t.localRotation=Quaternion.LookRotation(d);
                var sk=Box(g,"CurveSkirting",(p0+p1)/2+Vector3.up*.035f,new Vector3(.15f,.07f,d.magnitude+.01f),silver);sk.localRotation=t.localRotation;
            }
            Rect(roofs,"RoundedOfficeCap",new R(18.9f,18.4f,20.1f,25.5f),H+.05f,.1f,white);
        }
        static void MatchBasementRestrooms(GameObject basement)
        {
            var codes=new[]{"WC-N-W","WC-N-M","WC-S-W","WC-S-M"};
            var sources=new[]{"NorthWC_South_Women","NorthWC_West_Men","WestWC_East_NorthWC","WestWC_East_SouthWC"};
            var footprints=new[]{new R(0,16,6,21),new R(0,21,6,27.5f),new R(22,59.4f,30.8f,62.6f),new R(22,62.6f,30.8f,66)};
            for(int i=0;i<codes.Length;i++)
            {
                var room=architecture.Find("Room"+codes[i]);var front=room.Find("U1");var old=front.Find("Door_"+codes[i]);
                var source=basement.transform.Find("PlanDoors/"+sources[i]);source.GetComponent<InteractiveDoor>().ResetDoor();
                var finish=basement.transform.Find("RoomsAndCores/"+(i<2?"NorthWC":"WestWC")+"/West/LowerWall").GetComponent<Renderer>().sharedMaterial;
                foreach(var r in room.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial==white)r.sharedMaterial=finish;
                var skirting=basement.transform.Find("RoomsAndCores/"+(i<2?"NorthWC":"WestWC")+"/West/Skirting").GetComponent<Renderer>().sharedMaterial;
                foreach(var r in room.GetComponentsInChildren<MeshRenderer>())if(r.name.EndsWith("Skirting"))r.sharedMaterial=skirting;
                Rect(room,"RestroomGrayFloor",footprints[i],.004f,.008f,tile);
                var replacement=UnityEngine.Object.Instantiate(source.gameObject,front).transform;replacement.name="Door_"+codes[i];replacement.localPosition=old.localPosition;replacement.localRotation=Quaternion.identity;replacement.localScale=Vector3.one;
                UnityEngine.Object.DestroyImmediate(old.gameObject);
                // The B1 0.85m door replaces the generic 1m classroom leaf; close its reveals.
                foreach(int sign in new[]{-1,1})Box(front,"RestroomReveal"+sign,new Vector3(sign*.5125f,H/2,.065f),new Vector3(.075f,H,.13f),finish,true);
                Box(front,"RestroomTransom",new Vector3(0,(2.25f+H)/2,.065f),new Vector3(1.1f,H-2.25f,.13f),finish,true);
                var label=replacement.Find("RoomLabel").GetComponent<Text>();label.font=font;label.text=i%2==0?"여자 화장실":"남자 화장실";
                var serial=new SerializedObject(replacement.GetComponent<InteractiveDoor>());serial.FindProperty("doorLabel").stringValue="1F "+label.text+" 문";serial.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        static void CounterAndStools()
        {
            var g=P.Group(details,"BluebellCounter");
            Rect(g,"ConcreteParapet",new R(19,28.75f,30.2f,28.93f),.56f,1.12f,concrete,true);
            Rect(g,"StoneWorktop",new R(18.8f,28.42f,30.71f,29.06f),1.14f,.07f,stone);
            Rect(g,"RaisedBackRim",new R(18.8f,28.98f,30.71f,29.05f),1.20f,.05f,silver);
            for(int i=0;i<11;i++)
            {
                var stool=P.Group(g,"OakBarStool"+i);stool.localPosition=At(19.5f+i*.96f,27.95f);stool.localRotation=Quaternion.Euler(0,-90,0);
                // Thin bent oak seat, low back, black sled frame and footrest.
                Box(stool,"OakSeat",new Vector3(0,.74f,0),new Vector3(.43f,.035f,.38f),wood);
                var back=Box(stool,"OakBack",new Vector3(0,.91f,-.19f),new Vector3(.43f,.16f,.025f),wood);back.localRotation=Quaternion.Euler(-8,0,0);
                foreach(int s in new[]{-1,1})
                {
                    P.Tube(stool,"FrontLeg"+s,new Vector3(s*.19f,.72f,.14f),new Vector3(s*.24f,.025f,.22f),.016f,black);
                    P.Tube(stool,"RearLeg"+s,new Vector3(s*.19f,.94f,-.19f),new Vector3(s*.24f,.025f,-.23f),.016f,black);
                    P.Tube(stool,"Sled"+s,new Vector3(s*.24f,.025f,-.23f),new Vector3(s*.24f,.025f,.22f),.016f,black);
                }
                P.Tube(stool,"Footrest",new Vector3(-.22f,.28f,.19f),new Vector3(.22f,.28f,.19f),.016f,black);
                var c=stool.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.47f,0);c.size=new Vector3(.49f,.94f,.48f);stool.gameObject.layer=obstacle;
                var outlet=Face(g,"CounterOutlet",At(19.5f+i*.96f,28.73f,.94f),-90);Box(outlet,"OutletPlate",Vector3.zero,new Vector3(.09f,.06f,.015f),white);
            }

            // Continue the same worktop around the stair void, leaving the 101 aisle at u=32.
            // Wrap the 1F finish over the back of the B1 stair display.
            Rect(g,"ReturnParapet",new R(30.17f,28.93f,30.69f,34.77f),.56f,1.12f,concrete,true);
            Rect(g,"ReturnWorktop",new R(30.07f,29.06f,30.71f,34.77f),1.14f,.07f,stone);
            Rect(g,"ReturnRaisedRim",new R(30.07f,29.05f,30.14f,34.77f),1.20f,.05f,silver);
            var prototype=g.Find("OakBarStool0");
            for(int i=0;i<6;i++)
            {
                float v=29.65f+i*.89f;
                var stool=UnityEngine.Object.Instantiate(prototype.gameObject,g).transform;
                stool.name="ReturnBarStool"+i;
                stool.localPosition=At(31.02f,v);
                FaceDesk(stool,At(30.62f,v));
                var outlet=Face(g,"ReturnOutlet"+i,At(30.703f,v,.94f),0);
                Box(outlet,"OutletPlate",Vector3.zero,new Vector3(.09f,.06f,.015f),white);
            }
        }
        static void LoungeChair(Transform parent,string name,float u,float v,float yaw,Material finish,bool study=false)
        {
            var g=Face(parent,name,At(u,v),yaw);
            // Moulded seat/back: a continuous curved shell with tapered shoulders.
            var vertices=new List<Vector3>();var triangles=new List<int>();
            var rows=new[]{new Vector3(.225f,.455f,.22f),new Vector3(.235f,.435f,.08f),new Vector3(.22f,.445f,-.14f),new Vector3(.19f,.52f,-.22f),new Vector3(.22f,.76f,-.27f),new Vector3(.21f,.86f,-.29f)};
            foreach(var row in rows)for(int x=0;x<=8;x++){float t=(x-4)/4f;vertices.Add(new Vector3(row.x*t,row.y+.024f*t*t,row.z+.025f*t*t));}
            int frontCount=vertices.Count;vertices.AddRange(vertices.Select(v=>v-Vector3.up*.012f).ToArray());
            for(int r=0;r<rows.Length-1;r++)for(int x=0;x<8;x++)
            {
                int a=r*9+x,b=a+1,c=a+9,d=c+1;
                triangles.AddRange(new[]{a,b,c,b,d,c,a+frontCount,c+frontCount,b+frontCount,b+frontCount,c+frontCount,d+frontCount});
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var shell=P.Group(g,"MouldedShell");shell.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("1F_LoungeChairShell",mesh);shell.gameObject.AddComponent<MeshRenderer>().sharedMaterial=finish;
            foreach(int side in new[]{-1,1})
            {
                P.Tube(g,"FrontLeg"+side,new Vector3(side*.19f,.43f,.15f),new Vector3(side*.25f,.025f,.25f),.022f,study?black:silver);
                P.Tube(g,"RearLeg"+side,new Vector3(side*.19f,.43f,-.16f),new Vector3(side*.25f,.025f,-.25f),.022f,study?black:silver);
                if(study)P.Tube(g,"Arm"+side,new Vector3(side*.255f,.65f,-.2f),new Vector3(side*.255f,.65f,.16f),.026f,black);
            }
            var chairCollider=g.gameObject.AddComponent<BoxCollider>();chairCollider.center=new Vector3(0,.44f,-.02f);chairCollider.size=new Vector3(.54f,.88f,.57f);g.gameObject.layer=obstacle;
        }
        static void FaceDesk(Transform chair,Vector3 target)
        {
            var delta=target-chair.localPosition;delta.y=0;
            chair.localRotation=Quaternion.LookRotation(delta);
        }
        static void LoungeSeating()
        {
            var g=P.Group(details,"BluebellLoungeSeating");
            var paleBlue=Surface("1F_LoungeBlueChair",new Color(.40f,.56f,.58f));
            var upholstery=P.Mat("1F_StudyBlackFabric",new Color(.025f,.028f,.03f),.08f);
            var windowFinish=P.Mat("1F_LoungeDarkGlazing",new Color(.035f,.045f,.05f),.85f);
            var prototype=details.Find("BluebellCounter/OakBarStool0");
            // Photo orientation: the railing desk and portal-wall desk run along u.
            // Glazed side desk joins their far ends; keep u=32 clear to Room 101.
            Rect(g,"RailStoneDesktop",new R(33.10f,28.90f,39.85f,29.56f),1.14f,.07f,stone);
            Rect(g,"RailRaisedRim",new R(33.10f,28.93f,39.85f,29.02f),1.22f,.10f,stone);
            Rect(g,"RailConcreteFace",new R(33.10f,29.08f,39.85f,29.23f),.55f,1.10f,concrete,true);
            for(int i=0;i<7;i++)
            {
                float u=33.55f+i*.96f;
                var stool=UnityEngine.Object.Instantiate(prototype.gameObject,g).transform;stool.name="RailStool"+i;stool.localPosition=At(u,29.99f);FaceDesk(stool,At(u,29.2f));
                var socket=Face(g,"RailOutlet"+i,At(u,29.245f,.88f),90);
                Box(socket,"Plate",Vector3.zero,new Vector3(.09f,.06f,.012f),white);
            }
            Rect(g,"WallStudyDesktop",new R(33.23f,35.70f,39.64f,36.32f),.74f,.045f,white);
            for(int i=0;i<8;i++)
            {
                float u=33.61f+i*.80f;
                var chair=P.Instance(g,"BlueMeshChair","WallChair"+i,At(u,35.30f));FaceDesk(chair,At(u,36.05f));
                foreach(var r in chair.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m.name=="ChairBlueFabric"?upholstery:m.name=="FurnitureSteel"?black:m).ToArray();
                foreach(int side in new[]{-1,1})
                {
                    P.Tube(chair,"LoungeArmSupport"+side,new Vector3(side*.255f,.45f,-.13f),new Vector3(side*.255f,.64f,-.13f),.025f,black);
                    Box(chair,"LoungeArmPad"+side,new Vector3(side*.255f,.65f,0),new Vector3(.045f,.028f,.32f),black);
                }
                foreach(float v in new[]{35.79f,36.23f})Box(g,"StudyDeskLeg",At(u-.32f,v,.35f),new Vector3(.035f,.70f,.035f),white,true);
            }
            // Three chairs face each table centre, at different angles as in the photos.
            for(int i=0;i<3;i++)
            {
                float u=34.1f+i*1.95f,v=33.10f;
                var t=P.Group(g,"RoundTable"+i);t.localPosition=At(u,v);
                P.Cylinder(t,"WhiteTop",new Vector3(0,.73f,0),new Vector3(.90f,.018f,.90f),white,Quaternion.identity);
                P.Cylinder(t,"Pedestal",new Vector3(0,.37f,0),new Vector3(.06f,.35f,.06f),silver,Quaternion.identity);
                P.Cylinder(t,"Base",new Vector3(0,.025f,0),new Vector3(.49f,.018f,.49f),white,Quaternion.identity);
                var col=t.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(0,.38f,0);col.size=new Vector3(.90f,.76f,.90f);t.gameObject.layer=obstacle;
                foreach(int j in new[]{0,1,2})
                {
                    // Two opposing seats plus one side seat leave space between table sets.
                    float angle=(90+j*90)*Mathf.Deg2Rad;float cu=u+.78f*Mathf.Cos(angle),cv=v+.78f*Mathf.Sin(angle);
                    string name="CafeChair"+i+"_"+j;LoungeChair(g,name,cu,cv,0,j==1?paleBlue:white);FaceDesk(g.Find(name),t.localPosition);
                }
            }
            // Window desks rotate onto the side wall, with silver-frame stools facing glass.
            Rect(g,"WindowHighDesktop",new R(39.31f,29.68f,39.89f,35.09f),1.02f,.055f,white);
            for(int i=0;i<6;i++)
            {
                float v=30.70f+i*.77f;
                var stool=UnityEngine.Object.Instantiate(prototype.gameObject,g).transform;stool.name="WindowStool"+i;stool.localPosition=At(38.92f,v);FaceDesk(stool,At(39.7f,v));
                foreach(var r in stool.GetComponentsInChildren<Renderer>())if(r.sharedMaterial==black)r.sharedMaterial=silver;
                foreach(float u in new[]{39.39f,39.80f})Box(g,"HighDeskLeg",At(u,v-.30f,.5f),new Vector3(.035f,1f,.035f),white,true);
            }
            for(int i=0;i<4;i++)
            {
                float v=29.0f+i*1.81f;
                Box(g,"WindowPane",At(39.82f,v+.90f,1.47f),new Vector3(1.75f,2.63f,.018f),windowFinish);
                Box(g,"WindowMullion",At(39.79f,v,1.47f),new Vector3(.06f,2.76f,.07f),black);
            }
            Box(g,"WindowTop",At(39.79f,32.62f,2.82f),new Vector3(7.24f,.055f,.07f),black);
            Box(g,"WindowTransom",At(39.79f,32.62f,1.03f),new Vector3(7.24f,.05f,.07f),black);
            Rect(g,"StraightAisleLight",new R(31.68f,29.1f,31.73f,36.3f),H-.10f,.035f,lamp);
            Rect(g,"WallCoveDiffuser",new R(33.22f,36.27f,39.88f,36.34f),H-.10f,.035f,lamp);
            Rect(g,"AirConditionerFrame",new R(35.9f,31.9f,36.8f,32.8f),H-.12f,.07f,white);
            Rect(g,"AirConditionerIntake",new R(36.06f,32.06f,36.64f,32.64f),H-.16f,.015f,silver);
            foreach(float u in new[]{35.96f,36.74f})Rect(g,"CassetteVent",new R(u-.022f,32.01f,u+.022f,32.69f),H-.163f,.012f,black);
        }
        static void UpperBluebell()
        {
            // A second run, visible from 1F, rises to a capped 2F landing; no 2F rooms.
            var g=P.Group(details,"UpperBluebell");g.localPosition=At(14.8f,StairV);float width=5.616f,walk=2.5272f;
            Rect(details,"BluebellWoodLanding",new R(14.8f,29,19,34.616f),.014f,.028f,wood);
            for(int i=0;i<30;i++)
            {
                float top=(i+1)*.15f,z=i*.3f+(i>=15?2.2f:0);
                Box(g,"WalkingTread"+i,new Vector3(width/2-walk/2,top-.075f,z+.15f),new Vector3(walk,.15f,.3f),wood);
                Box(g,"AntiSlip"+i,new Vector3(width/2-walk/2,top+.004f,z+.018f),new Vector3(walk,.008f,.035f),silver);
            }
            for(int i=0;i<10;i++)
            {
                float top=(i+1)*.45f,z=i*.9f+(i>=5?2.2f:0);
                Box(g,"BroadOakSeat"+i,new Vector3(-walk/2,top-.225f,z+.45f),new Vector3(width-walk,.45f,.9f),wood);
            }
            Box(g,"MidLanding",new Vector3(0,2.16f,5.6f),new Vector3(width,.18f,2.2f),wood);
            Box(g,"UpperLanding",new Vector3(0,4.42f,12.2f),new Vector3(width,.16f,2),wood);
            var path=new[]{new Vector3(width/2,0,0),new Vector3(width/2,2.25f,4.5f),new Vector3(width/2,2.25f,6.7f),new Vector3(width/2,4.5f,11.2f)};
            for(int flight=0;flight<3;flight++)
            {
                P.Tube(g,"Handrail",path[flight]+new Vector3(-.1f,.92f,0),path[flight+1]+new Vector3(-.1f,.92f,0),.04f,silver);
                int num=flight==1?2:4;
                for(int i=0;i<num;i++)
                {
                    Vector3 a=Vector3.Lerp(path[flight],path[flight+1],(i+.01f)/num)+Vector3.up*.07f,b=Vector3.Lerp(path[flight],path[flight+1],(i+.99f)/num)+Vector3.up*.07f;
                    var mesh=new Mesh{vertices=new[]{a,b,b+Vector3.up*.98f,a+Vector3.up*.98f},triangles=new[]{0,1,2,0,2,3},uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up}};mesh.RecalculateNormals();
                    var pane=P.Group(g,"GlassGuard");pane.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("1F_StairGlass"+flight+"_"+i,mesh);pane.gameObject.AddComponent<MeshRenderer>().sharedMaterial=glass;pane.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                    P.Tube(g,"PanelCap",a+Vector3.up*.98f,b+Vector3.up*.98f,.015f,silver);
                    P.Tube(g,"RailBracket",(a+b)/2+Vector3.up*.76f,(a+b)/2+new Vector3(-.1f,.87f,0),.018f,silver);
                }
            }
            // Closed 12cm cheeks have separate inner/outer faces and reliable normals.
            foreach(float x in new[]{-width/2-.06f,width/2+.06f})
                ConvergenceHallFirstFloorRepairs.StairCheek(g,x,white);
            // Tall charcoal framed glazing along the oak seating side.
            var glazing=P.Group(g,"TallWindowWall");
            for(int i=0;i<5;i++)
            {
                float z=i*2.6f+1.25f;
                Box(glazing,"Window",new Vector3(-width/2-.095f,3.65f,z),new Vector3(.025f,7.3f,2.55f),glass);
                Box(glazing,"VerticalFrame",new Vector3(-width/2-.095f,3.65f,i*2.6f),new Vector3(.09f,7.3f,.09f),black);
            }
            Box(glazing,"Transom",new Vector3(-width/2-.095f,3.5f,6.5f),new Vector3(.09f,.12f,13),black);
            Slats(new R(1.4f,26,19,34.8f),7.35f,"UpperStair");
            // Enclose the high stair well above the low 1F corridor roof.
            Rect(details,"UpperGalleryStub",new R(1.4f,26,15,29),4.42f,.16f,stone);
            Rect(details,"UpperStairHighReturn",new R(1.4f,25.92f,19,26.08f),5.12f,4.6f,white);
            Rect(details,"UpperGalleryEnd",new R(1.42f,26,1.58f,29),5.12f,4.6f,white);
            Rect(details,"UpperStairLandingHeader",new R(18.92f,26,19.08f,34.8f),5.15f,4.4f,white);
            Wall(details,"UpperStairBack",At(1.5f,StairV,4.5f),180,width);
            var letters=Face(g,"YonseiLetters",new Vector3(0,4.5f,11.9f),0);
            Label(letters,"YONSEI","YONSEI",new Vector3(0,.5f,0),new Vector2(4.7f,1),new Color(.92f,.89f,.79f),1000);
            var yonsei=(RectTransform)letters.Find("YONSEI");yonsei.localScale=Vector3.one*.005f;yonsei.sizeDelta=new Vector2(940,200);yonsei.GetComponent<Text>().resizeTextMaxSize=250;
            var sofa=P.Group(g,"LandingSofa");sofa.localPosition=new Vector3(-1.5f,4.5f,11.7f);
            Box(sofa,"Seat",new Vector3(0,.25f,0),new Vector3(1.15f,.28f,.7f),white);Box(sofa,"Back",new Vector3(0,.59f,.28f),new Vector3(1.15f,.5f,.15f),white);
            // Dark framed door from the 1F wood landing into the adjoining circulation.
            var landingDoor=Face(details,"WoodLandingGlassDoor",At(16.6f,34.616f),90);
            Door(landingDoor,"1F","",1.05f,true);
            Panel(landingDoor,"SideGlassL",-2.1f,-.58f,true);Panel(landingDoor,"SideGlassR",.58f,2.1f,true);
        }
        static void Bluebell101Passage()
        {
            // The railing-side aisle and east corridor share u=32: no dogleg at the fire doors.
            var g=Face(architecture,"Bluebell101Passage",At(32f,36.4f),-90);
            // Room 101's north wall is the lounge end wall; the west reveal is Room 104.
            Door(g,"101-105","",2.30f);
            foreach(string n in new[]{"Sign_101-105","CardReader"}){var t=g.Find(n);if(t)UnityEngine.Object.DestroyImmediate(t.gameObject);}
            var door=g.GetComponent<InteractiveDoor>();
            door.Configure("1F 블루벨리–101호 복도 방화문",door.Motions,door.LeafColliders,new Vector3(0,1.1f,-.15f),initial:1);
            door.ResetDoor();
            for(int i=0;i<2;i++)
            {
                var leaf=g.Find("Leaf"+i);
                Box(leaf,"DoorCloser",new Vector3(0,2.19f,-.065f),new Vector3(.28f,.065f,.075f),silver);
                P.Tube(leaf,"CloserArm",new Vector3(0,2.21f,-.10f),new Vector3(i==0?-.28f:.28f,2.22f,-.21f),.014f,silver);
            }
            var green=Surface("1F_ExitGreen",new Color(.025f,.38f,.23f));
            Box(g,"ExitSignBody",new Vector3(0,2.65f,-.035f),new Vector3(.27f,.21f,.065f),white);
            Box(g,"ExitSignFace",new Vector3(0,2.65f,-.071f),new Vector3(.235f,.175f,.008f),green);
            Label(g,"ExitSignText","↑ EXIT",new Vector3(0,2.65f,-.079f),new Vector2(.225f,.15f),Color.white,58);
            for(float v=37.4f;v<65;v+=5)
            {
                Rect(details,"EastCorridorAccessFrame",new R(31.69f,v-.30f,32.31f,v+.30f),H-.012f,.016f,silver);
                Rect(details,"EastCorridorAccessInset",new R(31.71f,v-.28f,32.29f,v+.28f),H-.023f,.012f,white);
                P.Cylinder(details,"EastCorridorSpeaker",At(32f,v+.65f,H-.015f),new Vector3(.19f,.007f,.19f),silver,Quaternion.identity);
            }
        }
        static void Elevator(string name,R r,float entranceV)
        {
            var g=P.Group(architecture,name);float u=(r.u0+r.u1)/2;
            // Photo 10: white recess with grey floor, brushed steel opening and adjacent plan.
            Wall(g,"Left",At(r.u0,(r.v0+r.v1)/2),180,r.v1-r.v0);
            Wall(g,"Right",At(r.u1,(r.v0+r.v1)/2),0,r.v1-r.v0);
            var f=Face(g,"LiftFront",At(u,entranceV),90);float w=r.u1-r.u0;
            Panel(f,"LeftWhiteWall",-w/2,-.67f,false);Panel(f,"RightWhiteWall",.67f,w/2,false);
            Box(f,"UpperSteel",new Vector3(0,2.38f,0),new Vector3(1.34f,.3f,.15f),silver);
            Box(f,"WhiteHeader",new Vector3(0,2.705f,.06f),new Vector3(1.34f,.35f,.13f),white,true);
            var motions=new List<InteractiveDoor.Motion>();var cols=new List<BoxCollider>();
            foreach(int s in new[]{-1,1})
            {
                var leaf=P.Group(f,"SlidingLeaf"+s);leaf.localPosition=new Vector3(s*.315f,0,0);
                Box(leaf,"Steel",new Vector3(0,1.115f,.015f),new Vector3(.625f,2.23f,.05f),AssetDatabase.LoadAssetAtPath<Material>(Mats+"LiftBrushedSteel.mat"));
                var c=leaf.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,1.115f,.015f);c.size=new Vector3(.625f,2.23f,.05f);leaf.gameObject.layer=obstacle;cols.Add(c);
                motions.Add(new InteractiveDoor.Motion{target=leaf,closedPosition=leaf.localPosition,closedRotation=Quaternion.identity,slide=new Vector3(s*.65f,0,0)});
            }
            f.gameObject.AddComponent<InteractiveDoor>().Configure("1F 엘리베이터",motions.ToArray(),cols.ToArray(),new Vector3(0,1.1f,-.15f));
            Box(f,"FloorDisplay",new Vector3(0,2.39f,-.081f),new Vector3(.43f,.105f,.008f),black);Label(f,"FloorOne","1",new Vector3(0,2.39f,-.087f),new Vector2(.40f,.08f),Color.white,65);
            Box(f,"CallPlate",new Vector3(.86f,1.02f,-.022f),new Vector3(.075f,.20f,.025f),silver);
            Box(f,"CallButton",new Vector3(.86f,1.01f,-.04f),new Vector3(.035f,.035f,.012f),white);
            Plaque(f,"1F","엘리베이터",-.96f);
            // Closed cabin connects to B1 through PassengerElevator scene bindings.
            Box(f,"CabinBack",new Vector3(0,1.3f,1.5f),new Vector3(1.7f,2.6f,.08f),silver,true);
            foreach(int s in new[]{-1,1})Box(f,"CabinSide",new Vector3(s*.85f,1.3f,.75f),new Vector3(.08f,2.6f,1.5f),silver,true);
            Box(f,"CabinRoof",new Vector3(0,2.6f,.75f),new Vector3(1.7f,.08f,1.5f),white);
            Rect(roofs,name+"Ceiling",r,H+.05f,.1f,white);
            Downlight(u,r.v1-.65f);
        }
        static void BluebellElevator(GameObject basement)
        {
            // Keep the B1 hall's actual door motions, hatches, seal, reveals and finishes.
            var source=basement.transform.Find("CoreHalls/B112ElevatorHall");
            foreach(var d in source.GetComponentsInChildren<InteractiveDoor>(true))d.ResetDoor();
            var g=UnityEngine.Object.Instantiate(source.gameObject,architecture).transform;g.name="NorthElevatorHall";
            g.localPosition=At(10.5f,25.5f);g.localRotation=Quaternion.Euler(0,90,0);g.localScale=Vector3.one;
            var finish=AssetDatabase.LoadAssetAtPath<Material>(Mats+"B112ExposedConcrete.mat");
            // B1 also uses the original core perimeter outside its recessed hall.
            // Supply that perimeter in the 1F footprint so no white/empty rear is exposed.
            Wall(g,"CoreLeftConcrete",g.InverseTransformPoint(At(8,21.95f)),90,7.1f,solid:finish);
            Wall(g,"CoreRightConcrete",g.InverseTransformPoint(At(13,21.95f)),-90,7.1f,solid:finish);
            Wall(g,"CoreRearConcrete",g.InverseTransformPoint(At(10.5f,18.4f)),180,5,solid:finish);
            Rect(roofs,"NorthElevatorHallCeiling",new R(8,18.4f,13,25.5f),H+.05f,.1f,white);
            Downlight(10.5f,24.3f);Downlight(10.5f,22.9f);
            var display=g.Find("PhotoElevatorDetails/PhotoFloorIndicator");
            foreach(Transform glyph in display)if(glyph.name.StartsWith("LED_"))glyph.gameObject.SetActive(false);
            Label(display,"FloorNumber","1",new Vector3(.006f,0,-.006f),new Vector2(.13f,.08f),Color.white,65);
            var serial=new SerializedObject(g.GetComponent<InteractiveDoor>());serial.FindProperty("doorLabel").stringValue="1F 동쪽 엘리베이터 문";serial.ApplyModifiedPropertiesWithoutUndo();
            // Fit just the outside facade wings to the 1F core; the recess is unchanged.
            foreach(string n in new[]{"FrontLeftPier","FrontRightPier"})
            {
                var pier=g.Find(n);float side=n.Contains("Left")?-1:1;
                pier.localPosition=new Vector3(side*1.825f,H/2,.06f);pier.localScale=new Vector3(1.35f,H,.12f);
                var collider=g.Find(n+"Collision").GetComponent<BoxCollider>();collider.center=pier.localPosition;collider.size=pier.localScale;
            }
        }
        static void WestCores(GameObject basement)
        {
            var source=basement.transform.Find("CoreHalls/B109ElevatorHall");
            foreach(var d in source.GetComponentsInChildren<InteractiveDoor>(true))d.ResetDoor();
            var hall=UnityEngine.Object.Instantiate(source.gameObject,architecture).transform;hall.name="SouthElevatorHall";
            hall.localPosition=At(6,59.4f);hall.localRotation=Quaternion.Euler(0,-90,0);hall.localScale=Vector3.one;
            var finish=AssetDatabase.LoadAssetAtPath<Material>(Mats+"WestLiftPhotoWhite.mat");
            const float depth=5.976f,width=4.32f;
            Box(hall,"CoreLeftWall",new Vector3(-width/2+.06f,H/2,depth/2),new Vector3(.12f,H,depth),finish,true);
            Box(hall,"CoreRightWall",new Vector3(width/2-.06f,H/2,depth/2),new Vector3(.12f,H,depth),finish,true);
            Box(hall,"CoreBackWall",new Vector3(0,H/2,depth-.06f),new Vector3(width-.24f,H,.12f),finish,true);
            OpenWestHallFront(hall,finish);
            Box(hall,"HallCeiling",new Vector3(0,H+.05f,depth/2),new Vector3(width,.1f,depth),white);
            var display=hall.Find("LeftWallElevator/PhotoElevatorDetails/PhotoFloorIndicator");
            foreach(Transform glyph in display)if(glyph.name.StartsWith("LED_"))glyph.gameObject.SetActive(false);
            Label(display,"FloorNumber","1",new Vector3(.006f,0,-.006f),new Vector2(.13f,.08f),Color.white,65);
            // Close the small strips between the measured B1 core and the 1F allocation.
            Wall(architecture,"WestLiftRightFill",At(8.28f,59.4f),-90,.24f,solid:finish);
            Wall(architecture,"WestCoreDivider",At(3.72f,59.4f),-90,.24f,solid:finish);
            Rect(details,"WestCoreGrayFloor",new R(3.6f,59.4f,8.4f,66),-.045f,.08f,tile);
            Downlight(6.4f,60.4f);Downlight(6.4f,63.7f);
            source=basement.transform.Find("CoreHalls/B109Stairwell");
            foreach(var d in source.GetComponentsInChildren<InteractiveDoor>(true))d.ResetDoor();
            var stair=UnityEngine.Object.Instantiate(source.gameObject,details).transform;stair.name="SouthStairwell";
            stair.localPosition=At(1.8f,59.4f);stair.localRotation=Quaternion.Euler(0,-90,0);stair.localScale=Vector3.one;
            foreach(string name in new[]{"SwitchbackStairs","UnderStairStorage","Landing","StairFlightBlocker"})
                stair.Find(name).gameObject.SetActive(false);
            foreach(string name in new[]{"ExitMarker","ExitMarkerBacking"})
            {
                var sign=stair.Find(name);
                if(sign)sign.gameObject.SetActive(false);
            }
            const float sw=3.528f,sh=4.55f,stairDepth=9.90f;
            var frameTop=stair.Find("DoorFrameTop");
            var originalLintel=stair.Find("Lintel");
            float headBottom=frameTop.localPosition.y+frameTop.localScale.y/2-.018f;
            var doorSurroundFinish=stair.Find("FrontLeftPier").GetComponent<Renderer>().sharedMaterial;
            architecture.Find("WestCoreDivider/Wall").GetComponent<Renderer>().sharedMaterial=doorSurroundFinish;
            Box(stair,"StairDoorHeadWall",new Vector3(0,(H+headBottom)/2,.06f),
                new Vector3(originalLintel.localScale.x,H-headBottom,.12f),doorSurroundFinish,true);
            originalLintel.gameObject.SetActive(false);
            stair.Find("LintelCollision").gameObject.SetActive(false);
            const float facadeDepth=.12f;
            float sideCentre=(stairDepth+facadeDepth)/2;
            float sideDepth=stairDepth-facadeDepth;
            Box(stair,"LeftWall",new Vector3(-sw/2+.06f,sh/2,sideCentre),new Vector3(.12f,sh,sideDepth),white,true);
            Box(stair,"RightWall",new Vector3(sw/2-.06f,sh/2,sideCentre),new Vector3(.12f,sh,sideDepth),white,true);
            Box(stair,"BackWall",new Vector3(0,sh/2,stairDepth-.06f),new Vector3(sw-.24f,sh,.12f),white,true);
            Box(stair,"HighFrontWall",new Vector3(0,(H+sh)/2,.06f),new Vector3(sw,sh-H,.12f),white,true);
            Box(stair,"StairwellCeiling",new Vector3(0,sh+.05f,stairDepth/2),new Vector3(sw,.1f,stairDepth),white);
            Wall(architecture,"WestStairOuterFill",At(.018f,59.4f),-90,.036f);
            Wall(architecture,"WestStairInnerFill",At(3.582f,59.4f),-90,.036f,solid:doorSurroundFinish);
        }
        static void CoreStair(R r,string name)
        {
            var g=P.Group(details,name);
            const float enclosure=4.55f;
            Rect(g,"BackWall",new R(r.u0-.07f,r.v0-.07f,r.u1+.07f,r.v0+.07f),enclosure/2,enclosure,white,true);
            Rect(g,"LeftWall",new R(r.u0-.07f,r.v0,r.u0+.07f,r.v1+.07f),enclosure/2,enclosure,white,true);
            Rect(g,"RightWall",new R(r.u1-.07f,r.v0,r.u1+.07f,r.v1+.07f),enclosure/2,enclosure,white,true);
            Rect(g,"HighFrontWall",new R(r.u0,r.v1-.07f,r.u1,r.v1+.07f),(H+enclosure)/2,enclosure-H,white,true);
            Rect(g,"StairwellCeiling",new R(r.u0-.07f,r.v0-.07f,r.u1+.07f,r.v1+.07f),enclosure+.05f,.1f,white);
            var f=Face(g,"StairDoor",At((r.u0+r.u1)/2,r.v1),90);Door(f,"STAIR","계단실",1.0f);
            Panel(f,"StairFrontLeft",-(r.u1-r.u0)/2,-.55f,false);Panel(f,"StairFrontRight",.55f,(r.u1-r.u0)/2,false);
        }
        static void ConfigureNavigation(Vector3 first)
        {
            var movement=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            var navigation=sim.GetComponent<ConvergenceFloorNavigation>();if(!navigation)navigation=sim.gameObject.AddComponent<ConvergenceFloorNavigation>();
            var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();var canvas=hud.GetComponent<Canvas>();
            var old=canvas.transform.Find("FloorShortcuts");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var panel=new GameObject("FloorShortcuts",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();panel.SetParent(canvas.transform,false);panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one;panel.anchoredPosition=new Vector2(-18,-18);panel.sizeDelta=new Vector2(260,118);panel.GetComponent<Image>().color=new Color(.035f,.065f,.11f,.94f);
            var label=new GameObject("FloorLabel",typeof(RectTransform),typeof(Text)).GetComponent<Text>();label.transform.SetParent(panel,false);label.font=font;label.fontSize=16;label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;var lr=(RectTransform)label.transform;lr.anchorMin=lr.anchorMax=lr.pivot=new Vector2(.5f,1);lr.anchoredPosition=new Vector2(0,-6);lr.sizeDelta=new Vector2(245,49);
            foreach(bool upper in new[]{false,true})
            {
                var source=hud.transform.Find("StatusPanel/StartButton");var t=(RectTransform)UnityEngine.Object.Instantiate(source.gameObject,panel).transform;t.name=upper?"FirstFloorButton":"BasementButton";t.anchorMin=t.anchorMax=t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(upper?134:10,-65);t.sizeDelta=new Vector2(116,38);
                var button=t.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();if(upper)UnityEventTools.AddPersistentListener(button.onClick,navigation.GoToFirstFloor);else UnityEventTools.AddPersistentListener(button.onClick,navigation.GoToBasement);
                var tx=t.GetComponentInChildren<Text>();tx.font=font;tx.fontSize=15;tx.text=upper?"1F · PgUp":"B1 · PgDn";
            }
            navigation.Configure(sim,movement,follow,label,ConvergenceHallB1Setup.Plan(745,830,.5f),first,0,90);
        }
        [MenuItem("Tools/EEG Wheelchair/Build Photo Based First Floor")]
        public static void BuildAndValidate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit play mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Materials();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            var b1=GameObject.Find("ConvergenceHallB1");var stair=b1.transform.Find("BluebellStand/MeasuredBluebell");
            Vector3 origin=stair.position+new Vector3(StairV,4.5f,StairBottomU);
            var old=GameObject.Find("ConvergenceHall1F");if(old){ConvergenceHallFirstFloorPassageChecks.AuditBefore(old);UnityEngine.Object.DestroyImmediate(old);}
            var go=new GameObject("ConvergenceHall1F");root=go.transform;
            architecture=P.Group(root,"RoomsAndCores");roofs=P.Group(root,"Ceilings");details=P.Group(root,"Details");
            FloorAndRoof();Rooms();CounterAndStools();LoungeSeating();UpperBluebell();Fixtures();Bluebell101Passage();
            MatchBasementRestrooms(b1);
            BluebellElevator(b1);
            WestCores(b1);
            BuildRoom106Exit();
            CoreStair(new R(13,18.4f,17,25.5f),"NorthStairwell");
            ConnectCoreStairs(b1,origin);
            // Static batches are material grouped meshes. Keep glass separate for sorting.
            foreach(var section in new[]{root.Find("Floors"),roofs})P.Combine(section,"1F_"+section.name);
            var asset=PrefabUtility.SaveAsPrefabAsset(go,Prefab);UnityEngine.Object.DestroyImmediate(go);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset);instance.transform.position=origin;PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            // Replace the old temporary 1F enclosure in this scene only; B1 prefab stays intact.
            var photo=stair.Find("PhotoRailingAndDisplay");
            foreach(Transform t in photo)
                if(t.name.StartsWith("Gallery")||t.name.StartsWith("Upper")||t.name=="AboveDisplayReturn")
                {t.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
            var spawn=World(instance.transform,17,27,.5f);ConfigureNavigation(spawn);
            ConvergenceHallElevatorTravel.Build(b1,instance,font);
            ConvergenceHallFirstFloorRepairs.RemoveOverlaps(b1,instance);
            PrefabUtility.SaveAsPrefabAsset(instance,Prefab);
            ValidateGeometry(instance,b1);
            var movement=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var basementSpawn=ConvergenceHallB1Setup.Plan(745,830,.5f);
            movement.transform.SetPositionAndRotation(basementSpawn,Quaternion.identity);UnityEngine.Object.FindFirstObjectByType<CameraFollow>().SnapToTarget();
            var scene=EditorSceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            // Both saved entry scenes and RESET start on B1. PgUp remains an inspection shortcut.
            EditorSceneManager.SaveScene(scene,Scene,true);AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ConvergenceHallB1Setup.ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ConvergenceHallB1Setup.ScenePath)).ToArray();
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ConvergenceHallB1Setup.ScenePath);
            Debug.Log("FIRST_FLOOR_BUILT: plan rooms 101-123 and 107-1; registered Bluebell landing; 11 stools; B1-style north lift hall; enclosed core stairwells; B1 default start.");
            BeginChecks();
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void ValidateGeometry(GameObject floor,GameObject b1)
        {
            Physics.SyncTransforms();
            var a=floor.transform.Find("RoomsAndCores");for(int i=101;i<=123;i++)Require(a.Find("Room"+i),"Missing room "+i);
            Require(a.Find("Room107-1"),"Missing 107-1");
            var reference=b1.transform.Find("PlanDoors/B102_North_West/PhotoRoomSign");
            var roomSigns=a.GetComponentsInChildren<Transform>()
                .Where(t=>t.name.StartsWith("Sign_",StringComparison.Ordinal)&&IsNumberedRoom(t.name.Substring(5))).ToArray();
            Require(roomSigns.Length==24,"Numbered 1F room plaques missing");
            foreach(var sign in roomSigns)
            {
                Require(sign.Find("RoomNumber").GetComponent<Text>().text==sign.name.Substring(5),"Incorrect plaque number: "+sign.name);
                foreach(string piece in new[]{"NavyBody","WhiteHeader","Divider","BraillePlate"})
                {
                    var part=sign.Find(piece);
                    var referencePiece=reference.Find(piece);
                    Require(part.GetComponent<Renderer>().sharedMaterial==referencePiece.GetComponent<Renderer>().sharedMaterial
                        &&part.localScale==referencePiece.localScale&&sign.localScale==reference.localScale,"Plaque style mismatch: "+sign.name);
                }
            }
            Debug.Log("FIRST_FLOOR_ROOM_SIGNS_OK: 24 numbered plaques match B1 dimensions and materials.");
            var stair=b1.transform.Find("BluebellStand/MeasuredBluebell");Require(Vector3.Distance(World(floor.transform,19,StairV),stair.TransformPoint(new Vector3(0,4.5f,11.2f)))<.005f,"B1/1F stair landing mismatch");
            Require(floor.GetComponentsInChildren<Light>().Length==0,"Unexpected decorative lights");
            Require(floor.GetComponentsInChildren<InteractiveDoor>().Length>=30,"Door shells missing");
            Require(floor.transform.Find("Details/BluebellCounter").Cast<Transform>().Count(t=>t.name.StartsWith("OakBarStool"))==11,"Stool count");
            Require(!floor.transform.Find("Details/RefreshmentCabinet"),"Wooden cabinet beside elevator remains");
            foreach(string code in new[]{"WC-N-W","WC-N-M","WC-S-W","WC-S-M"})
            {
                var entry=a.Find("Room"+code+"/U1/Door_"+code);var leaves=entry.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Leaf").ToArray();
                Require(leaves.Length==1&&leaves[0].sharedMaterial==AssetDatabase.LoadAssetAtPath<Material>(Mats+"RestroomMilkyGlass.mat"),"B1 frosted restroom leaf missing "+code);
                Require(leaves[0].transform.Find("PhotoGlassDoorFrame")&&entry.GetComponentsInChildren<Transform>().Any(t=>t.name=="PhotoPullHandle"),"B1 frame/pull missing "+code);
                Require(entry.Find("RoomLabel").GetComponent<Text>().text==(code.EndsWith("-W")?"여자 화장실":"남자 화장실"),"Restroom sign mismatch "+code);
                Require(a.Find("Room"+code+"/RestroomGrayFloor").GetComponent<Renderer>().sharedMaterial==AssetDatabase.LoadAssetAtPath<Material>(Mats+"1F_GrayTile.mat"),"Restroom gray floor missing "+code);
            }
            foreach(var door in floor.GetComponentsInChildren<InteractiveDoor>())foreach(var motion in door.Motions)
                Require(motion.target&&motion.target.IsChildOf(floor.transform),"1F door has external moving part: "+door.Label);
            var north=a.Find("NorthElevatorHall");var original=b1.transform.Find("CoreHalls/B112ElevatorHall");
            foreach(string name in new[]{"LeftReturn","RightReturn","RecessedWallSurround/LeftWall","RecessedWallSurround/RightWall","RecessedWallSurround/OverDoorWall"})
                Require(north.Find(name).GetComponent<Renderer>().sharedMaterial==original.Find(name).GetComponent<Renderer>().sharedMaterial,"B1/1F lift finish differs: "+name);
            Require(north.Find("ServicePanels")&&north.Find("MeasuredCabin")&&north.Find("PhotoElevatorDetails/DoorSealHalves"),"B1-style lift details missing");
            var west=a.Find("SouthElevatorHall");
            Require(west.Find("LeftWallElevator/MeasuredCabin")&&west.Find("ServicePanels/EPSPanel/DoorLeaf/ServicePlacard")&&west.Find("ServicePanels/TPSPanel/DoorLeaf/ServicePlacard"),"West B1 lift cabin and bilingual service signs missing");
            foreach(string n in new[]{"CoreLeftWall","CoreRightWall","CoreBackWall"})Require(west.Find(n).GetComponent<Renderer>().sharedMaterial==AssetDatabase.LoadAssetAtPath<Material>(Mats+"WestLiftPhotoWhite.mat"),"West lift wall differs from B1");
            var westStair=floor.transform.Find("Details/SouthStairwell");
            var infill=a.Find("WallBetween107And107-1/Wall").GetComponent<BoxCollider>();
            foreach(float u in new[]{9.42f,10.6f,11.78f})
                Require(infill.Raycast(new Ray(World(floor.transform,u,57.3f,1.5f),floor.transform.right),out _,.5f),"107-1 corridor wall has a gap");
            var stairHead=westStair.Find("StairDoorHeadWall").GetComponent<BoxCollider>();
            foreach(float x in new[]{-.86f,0,.86f})
                Require(stairHead.Raycast(new Ray(westStair.TransformPoint(new Vector3(x,2.30f,-.3f)),westStair.forward),out _,.6f),"Gap above the west stair door frame");
            ValidateConnectedStairs(floor);
            Require(Vector3.Distance(westStair.position,World(floor.transform,1.8f,59.4f))<.001f,"West stair entrance disconnected from corridor");
            foreach(string name in new[]{"NorthStairwell","SouthStairwell"})
            {
                var stairwell=floor.transform.Find("Details/"+name);
                foreach(string wall in new[]{"BackWall","LeftWall","RightWall"})
                {
                    var surface=stairwell.Find(wall);var c=surface.GetComponent<BoxCollider>();
                    Require(surface.GetComponent<Renderer>().enabled&&surface.GetComponent<Renderer>().sharedMaterial==AssetDatabase.LoadAssetAtPath<Material>(Mats+"1F_WhitePaint.mat"),"Stair wall visual missing "+name+wall);
                    foreach(float y in new[]{.3f,1.6f,3.1f,4.3f})
                    {
                        var centre=surface.position;centre.y=floor.transform.position.y+y;
                        var direction=name=="SouthStairwell"?(wall=="BackWall"?stairwell.forward:stairwell.right):(wall=="BackWall"?-floor.transform.right:wall=="LeftWall"?floor.transform.forward:-floor.transform.forward);
                        Require(c.Raycast(new Ray(centre-direction,direction),out var hit,2),"Stair wall hole "+name+wall+" y="+y);
                    }
                }
                Require(stairwell.Find("StairwellCeiling"),"Stairwell roof missing");
            }
            foreach(var r in floor.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)Require(m&&!m.IsKeywordEnabled("_EMISSION"),"Missing/emissive material "+r.name);
            Debug.Log("FIRST_FLOOR_GEOMETRY_OK: all numbered shells/doors/ceilings, 11 bar stools, no glow, B1 stair height/position aligned.");
            Debug.Log("FIRST_FLOOR_FIXES_GEOMETRY_OK: three full-height walls and roof on both core stairwells; B1 concrete lift hall, service hatches and cabin copied without external motion references.");
        }
        static IEnumerator checks;static int frame=-1;static string error;static double deadline;
        public static void AuditCurrent()=>ConvergenceHallFirstFloorRepairs.AuditCurrent();
        public static void CaptureAndCheck(){Materials();BeginChecks();}
        static void BeginChecks()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);SessionState.SetBool("FirstFloor.Checking",true);SessionState.SetBool("FirstFloor.Entered",false);checks=null;error=null;deadline=EditorApplication.timeSinceStartup+240;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying){if(SessionState.GetBool("FirstFloor.Entered",false))Finish(null);return;}
                SessionState.SetBool("FirstFloor.Entered",true);
                if(checks==null){deadline=EditorApplication.timeSinceStartup+240;Application.logMessageReceived+=OnLog;checks=Run();}
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("First floor validation timed out");
                if(Time.frameCount==frame)return;frame=Time.frameCount;
                if(!checks.MoveNext()){if(error!=null)throw new Exception(error);Finish(null);}
            }
            catch(Exception ex){Finish(ex);}
        }
        static void OnLog(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)error=message;}
        static void Finish(Exception ex)
        {
            SessionState.SetBool("FirstFloor.Checking",false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;
            if(ex!=null)Debug.LogException(ex);else Debug.Log("FIRST_FLOOR_PLAY_OK");
            EditorApplication.ExitPlaymode();if(Application.isBatchMode)EditorApplication.Exit(ex==null?0:1);
        }
        static IEnumerator Run()
        {
            for(int i=0;i<8;i++)yield return null;
            var floor=GameObject.Find("ConvergenceHall1F");var b1=GameObject.Find("ConvergenceHallB1");ValidateGeometry(floor,b1);
            var navigation=UnityEngine.Object.FindFirstObjectByType<ConvergenceFloorNavigation>();var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();var guard=chair.GetComponent<WheelchairCollisionGuard>();
            Require(guard.CanOccupy(World(floor.transform,8.5f,26.25f,.5f),Quaternion.identity),"Removed cabinet still blocks passage");
            Require(chair.transform.position.y<1&&!sim.IsRunning,"Default scene must start on B1");
            navigation.GoToFirstFloor();sim.ResetSimulation();Require(chair.transform.position.y<1&&!sim.IsRunning,"RESET must return to B1");
            navigation.GoToFirstFloor();Require(chair.transform.position.y>4.9f&&!sim.IsRunning,"Floor shortcut failed");
            // Sample corridor centre lines with the actual wheelchair footprint.
            var routes=new[]{new[]{new Vector2(1.2f,30),new Vector2(1.2f,58.2f)},new[]{new Vector2(1.2f,58.2f),new Vector2(14.5f,58.2f)},new[]{new Vector2(32f,37.2f),new Vector2(32f,65)},new[]{new Vector2(7.2f,17.2f),new Vector2(34,17.2f)},new[]{new Vector2(9.4f,26.8f),new Vector2(29.7f,26.8f)}};
            int samples=0;foreach(var route in routes)
            {
                var a=World(floor.transform,route[0].x,route[0].y,.5f);var b=World(floor.transform,route[1].x,route[1].y,.5f);var rot=Quaternion.LookRotation(b-a);
                for(float t=0;t<=1;t+=.02f){var p=Vector3.Lerp(a,b,t);Require(guard.CanOccupy(p,rot),"Corridor collision at "+floor.transform.InverseTransformPoint(p));samples++;}
            }
            foreach(var door in floor.GetComponentsInChildren<InteractiveDoor>())
            {
                foreach(int fps in new[]{30,120}){door.SetProgressInstant(0);door.SetOpen(true);for(int i=0;i<fps;i++)door.Step(1f/fps);Require(door.Progress>.99f,"1F door open "+door.Label);door.SetOpen(false);for(int i=0;i<fps;i++)door.Step(1f/fps);Require(door.Progress<.01f,"1F door close "+door.Label);}
            }
            foreach(var door in floor.GetComponentsInChildren<InteractiveDoor>())door.ResetDoor();
            foreach(var door in floor.GetComponentsInChildren<InteractiveDoor>().Where(d=>d.Label.Contains("화장실")))
            {
                door.SetProgressInstant(1);
                for(float z=-.8f;z<.81f;z+=.1f)Require(guard.CanOccupy(door.transform.TransformPoint(new Vector3(0,.5f,z)),door.transform.rotation),"Restroom threshold blocked "+door.Label+" z="+z);
                door.SetProgressInstant(0);
            }
            Debug.Log("FIRST_FLOOR_RESTROOMS_OK: four B1 milky-glass entrances, satin frames and long pulls; open doorway clearance verified; elevator-side wooden cabinet and collision removed.");
            var testDoor=floor.GetComponentsInChildren<InteractiveDoor>().First();var saved=chair.transform.position;
            chair.transform.position=testDoor.InteractionPoint+Vector3.down*4.5f;Require(!testDoor.InReach(chair.transform),"Door reachable from wrong floor");chair.transform.position=saved;
            navigation.GoToBasement();Require(chair.transform.position.y<1&&!sim.IsRunning,"B1 return failed");navigation.GoToFirstFloor();
            sim.SetControlSource(WheelchairControlSource.Keyboard);sim.StartSimulation();var begin=chair.transform.position;for(int i=0;i<30;i++)chair.ApplyDirectionalInput(1,0,1f/30,WheelchairControlSource.Keyboard);Require(Vector3.Distance(begin,chair.transform.position)>.5f&&Mathf.Abs(chair.transform.position.y-5)<.01f,"1F movement/height failed");sim.StopSimulation();navigation.GoToFirstFloor();
            ValidateRoom106Exit(floor,guard);
            Debug.Log("FIRST_FLOOR_ROUTES_OK: "+samples+" footprint samples; all 1F doors 30/120fps; inter-floor reach isolation; floor shortcuts and 1F drive.");
            var passageChecks=ConvergenceHallFirstFloorPassageChecks.Validate(floor);while(passageChecks.MoveNext())yield return passageChecks.Current;
            ValidateClassroomAccess(floor,guard);
            ConvergenceHallElevatorTravel.Validate();
            ValidateStairWalk(floor);
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-firstFloorCaptures");string dir=output>=0?args[output+1]:Path.Combine(Directory.GetCurrentDirectory(),"FirstFloorCaptures");Directory.CreateDirectory(dir);
            var elevatorShots=ConvergenceHallElevatorTravel.CaptureJourney(dir,(camera,path)=>Capture(camera,path,true));while(elevatorShots.MoveNext())yield return elevatorShots.Current;
            var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();var free=cam.GetComponent<DemoFreeCamera>();free.EnterFreeView();follow.enabled=false;cam.fieldOfView=68;
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.renderMode!=RenderMode.WorldSpace)canvas.enabled=false;
            ShaderUtil.allowAsyncCompilation=false;
            cam.fieldOfView=58;
            cam.transform.position=World(floor.transform,11.2f,58.15f,1.60f);
            cam.transform.LookAt(World(floor.transform,15.8f,58.2f,1.50f));
            for(int i=0;i<4;i++)yield return null;
            Capture(cam,Path.Combine(dir,"Room106Exit.png"));
            cam.fieldOfView=74;
            cam.transform.position=World(floor.transform,10.35f,59.23f,1.55f);
            cam.transform.LookAt(World(floor.transform,10.9f,57,1.43f));
            for(int i=0;i<4;i++)yield return null;
            Capture(cam,Path.Combine(dir,"Room107-1CorridorWall.png"));
            cam.transform.position=World(floor.transform,1.8f,57.48f,1.53f);
            cam.transform.LookAt(World(floor.transform,1.8f,59.4f,1.47f));
            for(int i=0;i<4;i++)yield return null;
            Capture(cam,Path.Combine(dir,"WestStairExitFrame.png"));
            cam.fieldOfView=75;
            cam.transform.position=World(floor.transform,32.62f,26.05f,1.62f);
            cam.transform.LookAt(World(floor.transform,28.95f,29.62f,1.04f));
            for(int i=0;i<4;i++)yield return null;
            Capture(cam,Path.Combine(dir,"BluebellCornerCounter.png"));
            foreach(var route in floor.GetComponentsInChildren<StairWalkRoute>())
            {
                cam.fieldOfView=72;
                var upperDoor=route.name.StartsWith("East")
                    ?floor.transform.Find("Details/NorthStairwell/StairDoor").GetComponent<InteractiveDoor>():null;
                if(upperDoor)upperDoor.SetProgressInstant(1);
                foreach(bool upperFlight in new[]{false,true})
                {
                    float distance=upperFlight?route.Length-(route.name.StartsWith("East")?3.0f:5.9f):2.9f;
                    cam.transform.position=route.PositionAt(distance)+Vector3.up*StairWalkRoute.EyeHeight;
                    cam.transform.LookAt(route.PositionAt(distance+1f)+Vector3.up*1.32f);
                    for(int i=0;i<4;i++)yield return null;
                    Capture(cam,Path.Combine(dir,route.name+(upperFlight?"Return.png":"Ascent.png")));
                }
                if(upperDoor)upperDoor.ResetDoor();
            }
            cam.fieldOfView=68;
            var positions=new[]{new Vector3(29.3f,26.7f,1.65f),new Vector3(31.3f,27.2f,1.65f),new Vector3(17.5f,32.4f,1.65f),new Vector3(1.2f,36,1.6f),new Vector3(32f,37,1.6f),new Vector3(30.8f,19.7f,1.6f),new Vector3(10.5f,26.3f,1.6f),new Vector3(10.1f,24.5f,1.6f),new Vector3(15,27,1.6f),new Vector3(1.8f,58.4f,1.6f)};
            var targets=new[]{new Vector3(18,27.8f,1.2f),new Vector3(19,23.2f,1.6f),new Vector3(7,31.8f,3.2f),new Vector3(1.2f,55,1.5f),new Vector3(32f,55,1.5f),new Vector3(34.7f,19.7f,1.5f),new Vector3(10.5f,22.1f,1.4f),new Vector3(9.2f,22.5f,1.4f),new Vector3(15,21,2),new Vector3(1.5f,64,2)};
            var names=new[]{"01_CounterAndAtrium","02_BluebellLobby","03_UpperBluebell","04_LongWhiteCorridor","05_GlassCorridor","06_OfficeCorridor","07_ElevatorHall","11_ElevatorServiceWall","12_NorthStairwell","13_SouthStairwell"};
            for(int i=0;i<names.Length;i++)
            {
                cam.transform.position=World(floor.transform,positions[i].x,positions[i].y,positions[i].z);cam.transform.LookAt(World(floor.transform,targets[i].x,targets[i].y,targets[i].z));
                double until=EditorApplication.timeSinceStartup+1.5;while(EditorApplication.timeSinceStartup<until||ShaderUtil.anythingCompiling)yield return null;
                Capture(cam,Path.Combine(dir,names[i]+".png"));
            }
            foreach(bool north in new[]{true,false})
            {
                cam.transform.position=World(floor.transform,north?15.5f:2.2f,north?24.6f:59.8f,1.65f);
                cam.transform.LookAt(World(floor.transform,north?14:1,north?19.7f:63.7f,2.1f));
                for(int i=0;i<3;i++)yield return null;
                Capture(cam,Path.Combine(dir,north?"14_NorthStairwellInterior.png":"15_SouthStairwellInterior.png"));
            }
            var passagePoints=new[]{new Vector3(32,29.65f,1.6f),new Vector3(32,33.1f,1.6f),new Vector3(32,35.55f,1.6f),new Vector3(32,40.2f,1.6f),new Vector3(32.1f,33.1f,1.6f),new Vector3(32.5f,30.7f,1.6f),new Vector3(35.4f,34.3f,1.6f),new Vector3(36.7f,32.4f,1.6f)};
            var passageLooks=new[]{new Vector3(32,46,1.4f),new Vector3(32,46,1.5f),new Vector3(32,47,1.5f),new Vector3(32,30,1.5f),new Vector3(37.2f,33.1f,1.25f),new Vector3(36.1f,35.7f,1.25f),new Vector3(35.4f,29.15f,1.1f),new Vector3(39.9f,32.4f,1.2f)};
            for(int j=0;j<passagePoints.Length;j++){cam.fieldOfView=72;cam.transform.position=World(floor.transform,passagePoints[j].x,passagePoints[j].y,passagePoints[j].z);cam.transform.LookAt(World(floor.transform,passageLooks[j].x,passageLooks[j].y,passageLooks[j].z));for(int i=0;i<4;i++)yield return null;Capture(cam,Path.Combine(dir,(27+j)+"_Bluebell101Passage.png"));}
            var west=floor.transform.Find("RoomsAndCores/SouthElevatorHall");
            var shots=new[]{new Vector3(1.30f,1.55f,-1.7f),new Vector3(1.3f,1.55f,1.35f),new Vector3(1.55f,1.55f,3.7f)};
            var looks=new[]{new Vector3(-.45f,1.27f,2.82f),new Vector3(-.5f,1.25f,2.93f),new Vector3(-.1f,1.45f,5.75f)};
            for(int j=0;j<shots.Length;j++){cam.fieldOfView=75;cam.transform.position=west.TransformPoint(shots[j]);cam.transform.LookAt(west.TransformPoint(looks[j]));for(int i=0;i<3;i++)yield return null;Capture(cam,Path.Combine(dir,(21+j)+"_WestElevatorHall.png"));}
            var blue=floor.transform.Find("Details/UpperBluebell");
            for(int j=0;j<3;j++){cam.fieldOfView=70;cam.transform.position=blue.TransformPoint(new Vector3(4.1f,1.55f,1.4f+j*3.5f));cam.transform.LookAt(blue.TransformPoint(new Vector3(2.85f,1.4f,4.3f+j*2)));for(int i=0;i<3;i++)yield return null;Capture(cam,Path.Combine(dir,(24+j)+"_BluebellOuterWall.png"));}
            var restroomCodes=new[]{"WC-N-W","WC-N-M","WC-S-W","WC-S-M"};
            for(int j=0;j<restroomCodes.Length;j++)
            {
                var entry=floor.transform.Find("RoomsAndCores/Room"+restroomCodes[j]+"/U1/Door_"+restroomCodes[j]);
                cam.fieldOfView=78;cam.transform.position=entry.TransformPoint(new Vector3(.10f,1.25f,-1.65f));cam.transform.LookAt(entry.TransformPoint(new Vector3(0,1.25f,0)));
                for(int i=0;i<3;i++)yield return null;Capture(cam,Path.Combine(dir,(16+j)+"_"+restroomCodes[j]+".png"));
                if(j==0){entry.GetComponent<InteractiveDoor>().SetProgressInstant(1);for(int i=0;i<3;i++)yield return null;Capture(cam,Path.Combine(dir,"20_RestroomOpen.png"));entry.GetComponent<InteractiveDoor>().SetProgressInstant(0);}
            }
            foreach(string code in new[]{"101","104","111","112"})
            {
                var room=floor.transform.Find("RoomsAndCores/Room"+code+"/ClassroomInterior");
                float back=room.InverseTransformPoint(room.Find("AisleFront").position).z;
                float aisle=room.InverseTransformPoint(room.Find("AisleEntry").position).x;
                cam.fieldOfView=code=="101"?78:70;
                cam.transform.position=room.TransformPoint(new Vector3(aisle,1.60f,-back-.2f));
                cam.transform.LookAt(room.TransformPoint(new Vector3(0,1.3f,back)));
                for(int i=0;i<4;i++)yield return null;
                Capture(cam,Path.Combine(dir,"Classroom"+code+".png"));
            }
            foreach(string code in new[]{"101","111"})
            {
                var sign=floor.transform.Find("RoomsAndCores/Room"+code).GetComponentsInChildren<Transform>()
                    .First(t=>t.name=="Sign_"+code);
                cam.fieldOfView=39;
                cam.transform.position=sign.position+sign.right*.10f+Vector3.up*.025f-sign.forward*.52f;
                cam.transform.LookAt(sign.position+Vector3.up*.01f);
                for(int i=0;i<4;i++)yield return null;
                Capture(cam,Path.Combine(dir,"RoomSign"+code+".png"));
            }
            floor.transform.Find("Ceilings").gameObject.SetActive(false);cam.orthographic=true;cam.orthographicSize=37;cam.transform.position=World(floor.transform,18.5f,33,75);cam.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.right);
            for(int i=0;i<5;i++)yield return null;Capture(cam,Path.Combine(dir,"08_PlanOverview.png"));
        }
        static void Capture(Camera camera,string path,bool includeHud=false)
        {
            var rt=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            var previous=camera.targetTexture;var active=RenderTexture.active;
            var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>();var mode=hud.renderMode;var oldCamera=hud.worldCamera;float distance=hud.planeDistance;
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;float scale=pipeline?pipeline.renderScale:1;
            try
            {
                if(pipeline)pipeline.renderScale=1;
                if(includeHud){hud.renderMode=RenderMode.ScreenSpaceCamera;hud.worldCamera=camera;hud.planeDistance=.5f;}
                camera.targetTexture=rt;Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
                Debug.Log("FIRST_FLOOR_CAPTURE: "+path);
            }
            finally{hud.renderMode=mode;hud.worldCamera=oldCamera;hud.planeDistance=distance;if(pipeline)pipeline.renderScale=scale;camera.targetTexture=previous;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
