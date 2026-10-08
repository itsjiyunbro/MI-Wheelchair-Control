using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallElevatorDetails
    {
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        static Material steel,grooved,black,white,blue,red,yellow,etch,led;
        static Font font;
        static void Init()
        {
            steel=P.Mat("LiftBrushedSteel",new Color(.80f,.81f,.80f),.57f);steel.SetFloat("_Metallic",.40f);
            grooved=P.Mat("LiftVerticalGrooves",Color.white,.52f);grooved.SetFloat("_Metallic",.38f);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Mats+"LiftVerticalGroovesTexture.asset");
            var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Mats+"LiftVerticalGroovesNormal.asset");
            if(!texture||!normal)
            {
                texture=new Texture2D(1024,512,TextureFormat.RGBA32,true){name="LiftVerticalGroovesTexture",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
                normal=new Texture2D(1024,512,TextureFormat.RGBA32,true,true){name="LiftVerticalGroovesNormal",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
                var colors=new Color[1024*512];var normals=new Color[1024*512];var random=new System.Random(2018);
                for(int y=0;y<512;y++)for(int x=0;x<1024;x++)
                {
                    float wave=Mathf.Sin(x*Mathf.PI/3),grain=((float)random.NextDouble()-.5f)*.018f,reflection=.035f*Mathf.Pow(Mathf.Abs(Mathf.Cos(y/512f*Mathf.PI*5)),9);
                    float value=.56f+wave*.07f+grain+reflection;colors[y*1024+x]=new Color(value,value*1.015f,value*1.018f);
                    float nx=Mathf.Cos(x*Mathf.PI/3)*.20f;normals[y*1024+x]=new Color(.5f+nx*.5f,.5f,Mathf.Sqrt(1-nx*nx),1);
                }
                texture.SetPixels(colors);texture.Apply();normal.SetPixels(normals);normal.Apply();
                AssetDatabase.CreateAsset(texture,Mats+"LiftVerticalGroovesTexture.asset");AssetDatabase.CreateAsset(normal,Mats+"LiftVerticalGroovesNormal.asset");
            }
            grooved.SetTexture("_BaseMap",texture);grooved.SetTexture("_BumpMap",normal);grooved.SetFloat("_BumpScale",.30f);grooved.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(grooved);EditorUtility.SetDirty(steel);
            black=P.Mat("LiftDisplayBlack",new Color(.005f,.006f,.007f),.45f);white=P.Mat("LiftGraphicWhite",new Color(.94f,.95f,.95f),.20f);
            blue=P.Mat("LiftGraphicBlue",new Color(.015f,.065f,.58f),.22f);red=P.Mat("LiftWarningRed",new Color(.65f,.018f,.02f));yellow=P.Mat("LiftWarningYellow",new Color(.92f,.70f,.025f));
            led=P.Mat("LiftIndicatorLED",new Color(.93f,.96f,1f),.2f,true);
            etch=P.Mat("LiftEtchedSeal",new Color(.83f,.85f,.83f,.45f),.35f);etch.SetFloat("_Surface",1);etch.SetFloat("_SrcBlend",5);etch.SetFloat("_DstBlend",10);etch.SetFloat("_ZWrite",0);etch.SetFloat("_Cull",0);etch.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");etch.SetOverrideTag("RenderType","Transparent");etch.renderQueue=3000;etch.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(etch);
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");
        }
        static void Text(Transform p,string name,string value,Vector3 at,Vector2 size,int pixels,Color color,TextAnchor anchor=TextAnchor.MiddleCenter)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(Text));go.transform.SetParent(p,false);go.transform.localPosition=at;go.transform.localScale=Vector3.one*.001f;
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)go.transform).sizeDelta=size*1000;
            var text=go.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=pixels;text.resizeTextForBestFit=true;text.resizeTextMinSize=5;text.resizeTextMaxSize=pixels;text.color=color;text.alignment=anchor;text.raycastTarget=false;
        }
        static void Shape(Transform p,string name,Vector2[] points,float z,Material mat)
        {
            float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
            float area=0;for(int i=0;i<points.Length;i++)area+=points[i].x*points[(i+1)%points.Length].y-points[(i+1)%points.Length].x*points[i].y;
            if(area<0)Array.Reverse(points);
            var remaining=Enumerable.Range(0,points.Length).ToList();var tr=new List<int>();int guard=0;
            while(remaining.Count>3&&guard++<500)
            {
                bool found=false;
                for(int i=0;i<remaining.Count;i++)
                {
                    int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];
                    if(Cross(points[a],points[b],points[c])<=.0000000001f)continue;
                    bool occupied=remaining.Any(k=>k!=a&&k!=b&&k!=c&&Cross(points[a],points[b],points[k])>=0&&Cross(points[b],points[c],points[k])>=0&&Cross(points[c],points[a],points[k])>=0);
                    if(occupied)continue;tr.AddRange(new[]{a,c,b});remaining.RemoveAt(i);found=true;break;
                }
                if(!found)break;
            }
            if(remaining.Count==3)tr.AddRange(new[]{remaining[0],remaining[2],remaining[1]});
            if(tr.Count!=(points.Length-2)*3)throw new Exception("Elevator graphic triangulation failed: "+name);
            var mesh=new Mesh();mesh.vertices=points.Select(v=>new Vector3(v.x,v.y,z)).ToArray();mesh.triangles=tr.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
            var t=P.Group(p,name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("Lift_"+name,mesh);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;t.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void Line(Transform p,string name,Vector2 a,Vector2 b,float width,float z,Material mat)
        {var n=new Vector2(-(b-a).y,(b-a).x).normalized*width/2;Shape(p,name,new[]{a-n,b-n,b+n,a+n},z,mat);}
        static void Ring(Transform p,string name,Vector2 centre,float radius,float width,float z,Material mat,float from=0,float to=360)
        {
            var v=new List<Vector3>();var tr=new List<int>();const int count=64;
            for(int i=0;i<=count;i++){float a=Mathf.Lerp(from,to,i/(float)count)*Mathf.Deg2Rad;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));v.Add(new Vector3(centre.x+d.x*radius,centre.y+d.y*radius,z));v.Add(new Vector3(centre.x+d.x*(radius-width),centre.y+d.y*(radius-width),z));}
            for(int i=0;i<count;i++){int k=i*2;tr.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var t=P.Group(p,name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("Lift_"+name,mesh);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;t.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void RoundedFace(Transform p,string name,float width,float height,float radius,float z,Material material)
        {
            var points=new List<Vector2>();
            for(int corner=0;corner<4;corner++){float cx=(corner==0||corner==3?1:-1)*(width/2-radius),cy=(corner<2?1:-1)*(height/2-radius);for(int j=0;j<=8;j++){float a=(corner*90+j*11.25f)*Mathf.Deg2Rad;points.Add(new Vector2(cx+Mathf.Cos(a)*radius,cy+Mathf.Sin(a)*radius));}}
            Shape(p,name,points.ToArray(),z,material);
        }
        static void Dot(Transform p,string name,float x,float y,float z,float diameter,Material material)
        {P.Cylinder(p,name,new Vector3(x,y,z),new Vector3(diameter,.0015f,diameter),material,Quaternion.Euler(90,0,0));}
        static void OfficialSeal(Transform parent,Vector3 at)
        {
            var source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Meshes/ConvergenceHall/YonseiOfficialSymbol.asset");
            if(!source)throw new Exception("Official Yonsei symbol asset missing");
            var g=P.Group(parent,"UniversityDoorSeal");g.localPosition=at;
            var graphic=P.Group(g,"OfficialYonseiSymbol");graphic.localScale=Vector3.one*(.57f/.218f);
            graphic.gameObject.AddComponent<MeshFilter>().sharedMesh=source;
            var renderer=graphic.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=etch;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void Seal(Transform parent,float z)=>OfficialSeal(parent,new Vector3(0,1.00f,z-.1285f));
        public static void MatchUniversitySeals(GameObject root)
        {
            etch=AssetDatabase.LoadAssetAtPath<Material>(Mats+"LiftEtchedSeal.mat");
            foreach(bool east in new[]{true,false})
            {
                var photo=ConvergenceHallElevatorCabins.Door(root,east).Find("PhotoElevatorDetails");
                var old=photo.Find("UniversityDoorSeal");var at=old.localPosition;
                UnityEngine.Object.DestroyImmediate(old.gameObject);OfficialSeal(photo,at);
            }
        }
        static void Warnings(Transform parent,float z)
        {
            for(int i=0;i<2;i++)
            {
                var g=P.Group(parent,i==0?"PinchWarning":"HandWarning");g.localPosition=new Vector3(i==0?-.063f:.063f,1.62f,z-.130f);
                P.Box(g,"StickerBacking",Vector3.zero,new Vector3(.096f,.120f,.002f),white);
                P.Box(g,"RedHeader",new Vector3(0,.048f,-.002f),new Vector3(.094f,.022f,.001f),red);Text(g,"WarningTitle",i==0?"위험":"주의",new Vector3(0,.048f,-.003f),new Vector2(.08f,.022f),15,Color.white);
                P.Box(g,"CaptionBar",new Vector3(0,-.047f,-.002f),new Vector3(.094f,.023f,.001f),blue);Text(g,"Caption",i==0?"끼임 주의":"손대지 마세요",new Vector3(0,-.047f,-.003f),new Vector2(.09f,.022f),10,Color.white);
                if(i==0)
                {
                    P.Box(g,"YellowHazard",new Vector3(.020f,.002f,-.002f),new Vector3(.038f,.063f,.001f),yellow);
                    Line(g,"DoorEdge",new Vector2(-.019f,-.024f),new Vector2(-.019f,.029f),.004f,-.004f,black);Dot(g,"Head",-.006f,.022f,-.005f,.012f,black);
                    Line(g,"Body",new Vector2(-.006f,.014f),new Vector2(-.006f,-.008f),.005f,-.004f,black);Line(g,"LegA",new Vector2(-.006f,-.008f),new Vector2(-.015f,-.025f),.005f,-.004f,black);Line(g,"LegB",new Vector2(-.006f,-.008f),new Vector2(.009f,-.025f),.005f,-.004f,black);
                    Shape(g,"HazardArrow",new[]{new Vector2(.034f,.007f),new Vector2(.014f,.007f),new Vector2(.014f,.016f),new Vector2(.003f,.002f),new Vector2(.014f,-.012f),new Vector2(.014f,-.002f),new Vector2(.034f,-.002f)},-.005f,black);
                }
                else
                {
                    P.Box(g,"Palm",new Vector3(.007f,-.005f,-.003f),new Vector3(.030f,.026f,.001f),black);
                    for(int f=0;f<4;f++)P.Box(g,"Finger"+f,new Vector3(-.005f+f*.008f,.013f,-.003f),new Vector3(.006f,.028f-(f==3?.008f:0),.001f),black);
                    Line(g,"Thumb",new Vector2(-.005f,-.009f),new Vector2(-.024f,.006f),.009f,-.004f,black);Line(g,"NoTouchSlash",new Vector2(-.037f,.029f),new Vector2(.037f,-.027f),.005f,-.005f,red);
                }
            }
        }
        static void Display(Transform parent,float z)
        {
            var g=P.Group(parent,"PhotoFloorIndicator");g.localPosition=new Vector3(0,2.255f,z-.137f);RoundedFace(g,"IndicatorFace",.43f,.110f,.008f,0,black);
            Text(g,"Manufacturer","OTIS",new Vector3(-.152f,-.002f,-.003f),new Vector2(.055f,.025f),14,Color.white);
            string[][] glyphs={new[]{"11110","10001","10001","11110","10001","10001","11110"},new[]{"00100","01100","00100","00100","00100","00100","01110"}};
            for(int c=0;c<2;c++)for(int y=0;y<7;y++)for(int x=0;x<5;x++)if(glyphs[c][y][x]=='1')P.Box(g,"LED_"+c+"_"+x+"_"+y,new Vector3(-.030f+c*.050f+x*.008f,.027f-y*.009f,-.002f),new Vector3(.006f,.007f,.002f),led);
            Shape(g,"UpIndicator",new[]{new Vector2(-.074f,-.016f),new Vector2(-.063f,-.002f),new Vector2(-.063f,-.021f),new Vector2(-.053f,-.021f),new Vector2(-.053f,-.002f),new Vector2(-.042f,-.016f),new Vector2(-.058f,.022f)},-.004f,led);
        }
        static void Button(Transform parent,float z)
        {
            var panel=P.Group(parent,"PhotoCallPanel");panel.localPosition=new Vector3(.86f,.96f,z);
            P.Box(panel,"MountingBody",new Vector3(0,0,-.009f),new Vector3(.126f,.278f,.018f),steel);
            RoundedFace(panel,"RoundedMetalBezel",.145f,.305f,.014f,-.019f,steel);RoundedFace(panel,"InsetMetalPlate",.129f,.283f,.008f,-.022f,grooved);
            var badge=P.Group(panel,"AccessibleBadge");badge.localPosition=new Vector3(0,.092f,-.025f);P.Box(badge,"BlueSquare",Vector3.zero,new Vector3(.044f,.044f,.001f),blue);
            Ring(badge,"Wheel",new Vector2(-.005f,-.004f),.012f,.003f,-.002f,white,60,360);Dot(badge,"PersonHead",-.003f,.015f,-.002f,.007f,white);
            Line(badge,"Back",new Vector2(-.003f,.010f),new Vector2(-.002f,-.001f),.003f,-.002f,white);Line(badge,"Seat",new Vector2(-.002f,-.001f),new Vector2(.010f,-.001f),.003f,-.002f,white);Line(badge,"Leg",new Vector2(.010f,-.001f),new Vector2(.014f,-.011f),.003f,-.002f,white);Line(badge,"Foot",new Vector2(.014f,-.011f),new Vector2(.020f,-.010f),.003f,-.002f,white);
            var button=P.Group(panel,"RaisedUpButton");button.localPosition=new Vector3(0,-.020f,-.028f);RoundedFace(button,"ButtonRim",.061f,.047f,.005f,0,steel);RoundedFace(button,"ButtonFace",.053f,.039f,.003f,-.003f,black);RoundedFace(button,"SteelButtonInset",.049f,.035f,.002f,-.004f,steel);
            Shape(button,"BlueUpArrow",new[]{new Vector2(-.021f,-.006f),new Vector2(-.014f,.014f),new Vector2(-.007f,-.006f),new Vector2(-.014f,0)},-.006f,blue);
            int[] braille={37,15};for(int c=0;c<2;c++)for(int d=0;d<6;d++)if((braille[c]&(1<<d))!=0)Dot(button,"BrailleUP_"+c+"_"+d,.007f+c*.008f+(d/3)*.0028f,-.006f-(d%3)*.0028f,-.006f,.0018f,white);
            P.Box(panel,"InspectionSticker",new Vector3(-.037f,-.117f,-.024f),new Vector3(.032f,.035f,.001f),white);Text(panel,"InspectionMark","ID",new Vector3(-.037f,-.117f,-.026f),new Vector2(.028f,.028f),14,Color.black);
        }
        public static void ApplyTo(Transform door,float z)
        {
            Init();foreach(string n in new[]{"FloorDisplay","FloorDisplayBacking","CallPanel","CallButton","CallButtonBacking"}){var old=door.Find(n);if(old)old.gameObject.SetActive(false);}
            door.Find("LiftFrame").GetComponent<Renderer>().sharedMaterial=steel;door.Find("Header").GetComponent<Renderer>().sharedMaterial=steel;
            for(int i=0;i<2;i++){var leaf=door.Find("SlidingLeaf"+i);leaf.GetComponent<Renderer>().sharedMaterial=grooved;leaf.localScale=new Vector3(.628f,2.06f,.025f);}
            var seam=door.Find("CentreSeam");seam.localScale=new Vector3(.003f,2.06f,.010f);seam.localPosition=new Vector3(0,1.03f,z-.134f);
            var group=door.Find("PhotoElevatorDetails");if(!group)group=P.Group(door,"PhotoElevatorDetails");foreach(Transform old in group.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            P.Box(group,"DoorTopShadow",new Vector3(0,2.075f,z-.118f),new Vector3(1.27f,.027f,.007f),black);
            Seal(group,z);Warnings(group,z);Display(group,z);Button(group,z);
        }
        static void Validate(GameObject root)
        {
            foreach(string path in new[]{"CoreHalls/B112ElevatorHall","CoreHalls/B109ElevatorHall/LeftWallElevator"})
            {
                var door=root.transform.Find(path);var photo=door.Find("PhotoElevatorDetails");if(!photo||!photo.Find("PhotoCallPanel/AccessibleBadge")||!photo.Find("PhotoFloorIndicator")||!photo.Find("UniversityDoorSeal"))throw new Exception("Photo elevator details missing: "+path);
                if(photo.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Elevator decoration gained collision.");
                if(door.Find("SlidingLeaf0").GetComponent<Renderer>().sharedMaterial.name!="LiftVerticalGrooves"||door.Find("CallPanel").gameObject.activeSelf)throw new Exception("Old elevator style remains.");
            }
            Debug.Log("PHOTO_ELEVATORS_OK: both grooved steel doors, etched seal/warnings, B1 dot indicators and rounded accessible UP panels; decoration has no colliders.");
        }
        public static void ApplyAndCapture()
        {
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Elevator details duplicated.");
            SessionState.SetBool("EEG.ConvergenceQA.FocusElevators",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
