using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallMainEntrance
    {
        public const float Width=6.75f,Depth=3f;
        const float H=2.85f,Frame=.055f,BankThickness=.085f;
        static Material metal,glass,film,wood,blue,yellow,navy,white,black,green,gold;
        static Font font;
        static Transform Group(Transform p,string name){var t=p.Find(name);if(!t)t=P.Group(p,name);return t;}
        static void Init()
        {
            metal=P.Mat("MainEntryStainless",new Color(.70f,.72f,.72f),.60f);metal.SetFloat("_Metallic",.32f);EditorUtility.SetDirty(metal);
            glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/MainEntryClearGlass.mat");
            if(!glass){glass=new Material(ConvergenceHallGlassBands.Glass(false)){name="MainEntryClearGlass"};glass.SetColor("_BaseColor",new Color(.92f,.96f,.96f,.08f));AssetDatabase.CreateAsset(glass,"Assets/Art/Materials/ConvergenceHall/MainEntryClearGlass.mat");}
            film=ConvergenceHallGlassBands.Glass(true);wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/BluebellMeasuredOak.mat");
            blue=P.Mat("EntryBlueGrayUpholstery",new Color(.28f,.39f,.45f),.16f);yellow=P.Mat("EntryYellowUpholstery",new Color(.96f,.64f,.025f),.16f);
            navy=P.Mat("EntryCeilingNavy",new Color(.018f,.055f,.12f),.14f);white=P.Mat("EntryWhite",new Color(.90f,.91f,.88f));black=P.Mat("EntryCeilingBlack",new Color(.006f,.010f,.017f));
            green=P.Mat("EntryExitGreen",new Color(.02f,.48f,.25f),.15f,true);gold=P.Mat("EntryDonorPlaqueGold",new Color(.62f,.49f,.25f),.40f);gold.SetFloat("_Metallic",.45f);
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");
        }
        static void Obstacle(Transform p,string name,Vector3 centre,Vector3 size)
        {
            var t=P.Group(p,name);var c=t.gameObject.AddComponent<BoxCollider>();c.center=centre;c.size=size;t.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");
        }
        static void Text(Transform p,string name,string value,Vector3 at,Vector2 size,int pixels,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(Text));var t=go.transform;t.SetParent(p,false);var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            t.localPosition=at;t.localScale=Vector3.one*.001f;((RectTransform)t).sizeDelta=size*1000;
            var text=go.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=pixels;text.resizeTextForBestFit=true;text.resizeTextMinSize=8;text.resizeTextMaxSize=pixels;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        }
        static void FloorPad(Transform p,string name,float x,float z)
        {
            var g=P.Group(p,name);g.localPosition=new Vector3(x,0,z);P.Box(g,"TactileBase",new Vector3(0,.018f,0),new Vector3(1.55f,.015f,.30f),yellow);
            for(int i=0;i<20;i++)for(int j=0;j<4;j++)P.Cylinder(g,"Dot"+i+"_"+j,new Vector3(-.71f+i*.075f,.028f,-.11f+j*.073f),new Vector3(.018f,.003f,.018f),yellow,Quaternion.identity);
        }
        static void ExitSign(Transform p,string name,float x)
        {
            var g=P.Group(p,name);g.localPosition=new Vector3(x,2.55f,.095f);g.localRotation=Quaternion.Euler(0,180,0);
            P.Box(g,"Housing",Vector3.zero,new Vector3(.34f,.20f,.045f),metal);P.Box(g,"GreenFace",new Vector3(0,0,-.025f),new Vector3(.30f,.165f,.004f),green);
            Text(g,"ExitLabel","EXIT",new Vector3(0,0,-.030f),new Vector2(.26f,.14f),75,Color.white);
        }
        static void Leaf(Transform bank,string name,float hinge,int side,float angle)
        {
            var g=P.Group(bank,name);g.localPosition=new Vector3(hinge,0,0);g.localRotation=Quaternion.Euler(0,angle,0);
            float centre=side*.50f;
            P.Box(g,"ClearDoorGlass",new Vector3(centre,1.13f,0),new Vector3(.925f,2.12f,.016f),glass);
            foreach(float x in new[]{side*.022f,side*.978f})P.Box(g,"LeafStile",new Vector3(x,1.13f,0),new Vector3(.040f,2.24f,.065f),metal);
            foreach(float y in new[]{.033f,2.227f})P.Box(g,"LeafRail",new Vector3(centre,y,0),new Vector3(1.00f,.050f,.065f),metal);
            for(int face=-1;face<=1;face+=2)
            {
                P.Tube(g,"PullHandle"+face,new Vector3(side*.86f,.80f,face*.095f),new Vector3(side*.86f,1.48f,face*.095f),.023f,metal);
                foreach(float y in new[]{.85f,1.43f})P.Tube(g,"HandleFixing"+face+"_"+y,new Vector3(side*.86f,y,face*.025f),new Vector3(side*.86f,y,face*.095f),.016f,metal);
            }
            P.Box(g,"Closer",new Vector3(side*.25f,2.20f,.050f),new Vector3(.23f,.045f,.065f),metal);
            Obstacle(g,"LeafCollision",new Vector3(centre,1.13f,0),new Vector3(1.00f,2.25f,.070f));
        }
        static void Bank(Transform entrance,string name,float z,bool inside)
        {
            var g=P.Group(entrance,name);g.localPosition=new Vector3(0,0,z);
            P.Box(g,"MeasuredHeadRail",new Vector3(0,H-.0275f,0),new Vector3(Width,.055f,BankThickness),metal);
            P.Box(g,"TransomRail",new Vector3(0,2.30f,0),new Vector3(Width,.060f,BankThickness),metal);
            P.Box(g,"TransomGlass",new Vector3(0,2.565f,0),new Vector3(Width-.06f,.47f,.016f),glass);
            float[] posts={-Width/2+Frame/2,-2.45f,-.45f,.45f,2.45f,Width/2-Frame/2};
            foreach(float x in posts)
            {P.Box(g,"FullHeightPost",new Vector3(x,H/2,0),new Vector3(Frame,H,BankThickness),metal);Obstacle(g,"PostCollision",new Vector3(x,H/2,0),new Vector3(Frame,H,BankThickness));}
            foreach(var span in new[]{new[]{-Width/2+Frame,-2.45f-Frame/2},new[]{-.45f+Frame/2,.45f-Frame/2},new[]{2.45f+Frame/2,Width/2-Frame}})
            {
                float w=span[1]-span[0],x=(span[0]+span[1])/2;
                P.Box(g,"FixedClearPane",new Vector3(x,1.14f,0),new Vector3(w,2.20f,.016f),glass);
                P.Box(g,"FixedPaneSill",new Vector3(x,.028f,0),new Vector3(w,.055f,BankThickness),metal);Obstacle(g,"FixedPaneCollision",new Vector3(x,1.14f,0),new Vector3(w,2.28f,BankThickness));
            }
            Leaf(g,"LeftBayLeafA",-2.45f,1,0);Leaf(g,"LeftBayLeafB",-.45f,-1,0);
            // Open the bay aligned with the existing indoor spawn, into the vestibule.
            Leaf(g,"RightBayLeafA",.45f,1,inside?70:0);Leaf(g,"RightBayLeafB",2.45f,-1,inside?-70:0);
            ExitSign(g,"LeftExitSign",-1.45f);ExitSign(g,"RightExitSign",1.45f);
            foreach(float x in new[]{-1.45f,1.45f})P.Box(g,"Threshold",new Vector3(x,.020f,0),new Vector3(2.00f,.022f,.17f),metal);
        }
        static void SideGlass(Transform entrance,float x)
        {
            var g=P.Group(entrance,"VestibuleGlassSide");g.localPosition=new Vector3(x,0,Depth/2);
            P.Box(g,"FullClearGlass",new Vector3(0,H/2,0),new Vector3(.016f,H-.08f,Depth-.085f),glass);
            foreach(float z in new[]{-Depth/2+Frame/2,0,Depth/2-Frame/2})P.Box(g,"SideMullion",new Vector3(0,H/2,z),new Vector3(BankThickness,H,Frame),metal);
            foreach(float y in new[]{.028f,H-.028f})P.Box(g,"SideRail",new Vector3(0,y,0),new Vector3(BankThickness,Frame,Depth),metal);
            Obstacle(g,"SideCollision",new Vector3(0,H/2,0),new Vector3(BankThickness,H,Depth));
        }
        static void DownQuad(Transform p,string name,float x0,float x1,float z0,float z1,float y,Material material)
        {
            var mesh=new Mesh();mesh.vertices=new[]{new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
            var g=P.Group(p,name);g.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("Entry_"+name,mesh);g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;g.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void SeatCorner(Transform root,float west,float seatWidth)
        {
            var area=P.Group(root,"SeatingCorner");float centre=west+seatWidth/2;
            var table=P.Group(area,"WoodCoffeeTable");table.localPosition=new Vector3(centre,0,1.38f);
            var top=P.Cylinder(table,"OvalOakTop",new Vector3(0,.43f,0),new Vector3(1.45f,.018f,.68f),wood,Quaternion.identity);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)P.Tube(table,"SplayedLeg",new Vector3(x*.43f,.41f,z*.17f),new Vector3(x*.51f,.014f,z*.24f),.045f,wood);
            Obstacle(table,"TableCollision",new Vector3(0,.23f,0),new Vector3(1.45f,.46f,.68f));
            var bench=P.Group(area,"RearBench");bench.localPosition=new Vector3(centre,0,.48f);
            P.Box(bench,"BenchTop",new Vector3(0,.435f,0),new Vector3(1.55f,.045f,.34f),wood);
            foreach(float x in new[]{-.65f,.65f}){P.Box(bench,"BenchLeg",new Vector3(x,.22f,0),new Vector3(.035f,.40f,.29f),metal);}
            Obstacle(bench,"BenchCollision",new Vector3(0,.24f,0),new Vector3(1.55f,.48f,.34f));
            Vector3[] seats={new Vector3(centre+.16f,0,2.15f),new Vector3(centre-1.15f,0,1.15f),new Vector3(centre-1.15f,0,.55f)};
            for(int i=0;i<seats.Length;i++)
            {
                var stool=P.Group(area,i==1?"BlueGrayRoundStool":"YellowRoundStool"+i);stool.localPosition=seats[i];
                P.Cylinder(stool,"BaseBand",new Vector3(0,.066f,0),new Vector3(.49f,.055f,.49f),blue,Quaternion.identity);
                P.Cylinder(stool,"UpholsteredBody",new Vector3(0,.28f,0),new Vector3(.50f,.17f,.50f),i==1?blue:yellow,Quaternion.identity);
                P.Cylinder(stool,"SeatCushion",new Vector3(0,.447f,0),new Vector3(.51f,.010f,.51f),i==1?blue:yellow,Quaternion.identity);
                Obstacle(stool,"StoolCollision",new Vector3(0,.23f,0),new Vector3(.51f,.46f,.51f));
            }
            // Photo-based donor wall treatment, without inventing donor names.
            var donor=P.Group(area,"DonorWallPanel");donor.localPosition=new Vector3(west+.03f,0,1.62f);donor.localRotation=Quaternion.Euler(0,-90,0);
            Text(donor,"DonorHeading","컨버전스홀 기부자 명예의 전당",new Vector3(0,2.23f,-.025f),new Vector2(2.55f,.16f),50,new Color(.16f,.17f,.18f));
            for(int r=0;r<7;r++)for(int c=0;c<8;c++)P.Box(donor,"DonorPlaque",new Vector3(-1.07f+c*.305f,1.85f-r*.165f,-.014f),new Vector3(.235f,.10f,.012f),gold);
        }
        static void MirrorLayout(Transform entry)
        {
            // Mirror placement and collider centres while keeping text readable and scales positive.
            foreach(var t in entry.GetComponentsInChildren<Transform>().Where(t=>t!=entry))
            {
                var p=t.localPosition;p.x=-p.x;t.localPosition=p;var q=t.localRotation;t.localRotation=new Quaternion(q.x,-q.y,-q.z,q.w);
                var collider=t.GetComponent<BoxCollider>();if(collider){var c=collider.center;c.x=-c.x;collider.center=c;}
                var filter=t.GetComponent<MeshFilter>();if(filter&&filter.sharedMesh.name.StartsWith("Entry_"))
                {
                    var m=filter.sharedMesh;var v=m.vertices;for(int i=0;i<v.Length;i++)v[i].x=-v[i].x;m.vertices=v;
                    var triangles=m.triangles;for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}m.triangles=triangles;m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);
                }
            }
        }
        // Photo-derived finish; repeat dimensions are in metres, independent of wall length.
        static Material WallStone()
        {
            const string path="Assets/Art/Materials/ConvergenceHall/EntryWallStoneTexture.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool fresh=!texture;
            if(fresh)texture=new Texture2D(512,512,TextureFormat.RGBA32,true){name="EntryWallStoneTexture"};
            else texture.Reinitialize(512,512,TextureFormat.RGBA32,true);
            texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Trilinear;texture.anisoLevel=4;
            var pixels=new Color[512*512];var random=new System.Random(64);
            for(int y=0;y<512;y++)for(int x=0;x<512;x++)
            {
                float broad=Mathf.PerlinNoise(x/35f,y/240f)-.5f;
                float grain=Mathf.PerlinNoise(x/2.8f,y/76f)-.5f;
                float value=.76f+broad*.12f+grain*.14f+((float)random.NextDouble()-.5f)*.035f;
                if(x%128<2)value-=.075f;
                pixels[y*512+x]=new Color(value,value*.985f,value*.95f);
            }
            texture.SetPixels(pixels);texture.Apply();
            if(fresh)AssetDatabase.CreateAsset(texture,path);else EditorUtility.SetDirty(texture);
            var material=P.Mat("EntryVerticalStoneWall",Color.white,.12f);
            material.SetTexture("_BaseMap",texture);material.SetTextureScale("_BaseMap",Vector2.one);EditorUtility.SetDirty(material);return material;
        }
        static void StoneSkin(Transform wall,float length,Material stone,string meshName)
        {
            var skin=P.Box(wall,"VerticalStoneSkin",new Vector3(0,H/2,0),new Vector3(length,H,.018f),stone);
            var filter=skin.GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);
            mesh.uv=mesh.vertices.Select(v=>new Vector2((v.x*length+length/2)/.8f,v.y+.5f)).ToArray();
            filter.sharedMesh=P.SaveMesh("Entry_"+meshName,mesh);
        }
        static void WidenDonorComposition(Transform donor,float ratio)
        {
            // Reposition columns and widen text rectangles without stretching glyphs or the crest.
            foreach(Transform t in donor)
            {
                var p=t.localPosition;p.x*=ratio;t.localPosition=p;
                var label=t.GetComponent<Text>();
                if(label){var rect=(RectTransform)t;rect.sizeDelta=new Vector2(rect.sizeDelta.x*ratio,rect.sizeDelta.y);}
                else if(t.name.StartsWith("DonorPlate_")||t.name=="ModeledUniversityEmblem")t.localScale*=1.18f;
                else if(t.name.StartsWith("DonationBandRule")){var scale=t.localScale;scale.x*=ratio;t.localScale=scale;}
            }
        }
        static void SeatingLightwell(Transform entry,float east)
        {
            // Upper dimensions inferred from reference photos; the measured vestibule remains 6.75 x 3 m.
            const float top=5.35f,end=4.6f;float start=Width/2,span=east-start,upper=top-H;
            var well=P.Group(entry,"SeatingLightwell");
            P.Box(well,"TallWhiteSideWall",new Vector3(east+.04f,H+upper/2,end/2),new Vector3(.12f,upper,end),white);
            P.Box(well,"UpperRearReturn",new Vector3((start+east)/2,H+upper/2,end+.05f),new Vector3(span,upper,.10f),white);
            P.Box(well,"LightwellLongRim",new Vector3(start+.04f,H+.11f,end/2),new Vector3(.16f,.26f,end),white);
            P.Box(well,"LightwellEndRim",new Vector3((start+east)/2,H+.11f,end),new Vector3(span,.26f,.16f),white);
            P.Box(well,"UpperWindowSpandrel",new Vector3((start+east)/2,H+.30f,.0425f),new Vector3(span,.60f,.085f),white);
            float bottom=H+.6f,windowH=top-bottom;int count=3;
            for(int i=0;i<count;i++)P.Box(well,"UpperClearWindow"+i,new Vector3(start+span*(i+.5f)/count,bottom+windowH/2,.0425f),new Vector3(span/count-.055f,windowH,.016f),glass);
            for(int i=0;i<=count;i++)P.Box(well,"UpperWindowMullion"+i,new Vector3(start+span*i/count,bottom+windowH/2,.0425f),new Vector3(Frame,windowH,.085f),metal);
            foreach(float y in new[]{bottom,top})P.Box(well,"UpperWindowRail",new Vector3((start+east)/2,y,.0425f),new Vector3(span,Frame,.085f),metal);
            DownQuad(well,"LightwellCeiling",start,east,0,end,top,black);
            for(int i=0;i<26;i++){float z=.03f+i*.175f;DownQuad(well,"HighSlat"+i,start,east,z,z+.07f,top-.06f,navy);}
        }
        static void WallDetails(GameObject building,Transform entry)
        {
            var donor=entry.Find("SeatingCorner/DonorWallPanel");foreach(Transform child in donor.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            donor.localPosition=new Vector3(donor.localPosition.x,0,2.3f);
            ConvergenceHallEntranceBoards.DonorWall(donor);WidenDonorComposition(donor,4.6f/3.24f);
            P.Box(donor,"FullHeightWhiteDonorWall",new Vector3(0,H/2,.010f),new Vector3(4.6f,H,.065f),white);
            P.Box(donor,"DonorTopLightHousing",new Vector3(0,2.52f,-.03f),new Vector3(4.04f,.035f,.055f),metal);
            P.Box(donor,"DonorTopLightFace",new Vector3(0,2.502f,-.060f),new Vector3(3.96f,.014f,.008f),P.Mat("EntryWallLightWhite",new Color(.94f,.94f,.83f),.2f,true));
            float east=ConvergenceHallB1Setup.Plan(772,878).x-entry.position.x,west=ConvergenceHallB1Setup.Plan(632,878).x-entry.position.x;
            var stone=WallStone();
            var eastWall=P.Group(entry,"SeatingWallTreatment");eastWall.localPosition=new Vector3(east-.016f,0,6.332f);eastWall.localRotation=Quaternion.Euler(0,90,0);
            StoneSkin(eastWall,3.464f,stone,"SeatingStoneSkin");
            SeatingLightwell(entry,east);
            var sconce=P.Group(entry,"SeatingWallSconce");sconce.localPosition=new Vector3(east-.055f,2.05f,6.33f);sconce.localRotation=Quaternion.Euler(0,90,0);
            P.Box(sconce,"BlackHousing",Vector3.zero,new Vector3(.27f,.035f,.075f),black);P.Box(sconce,"WarmStrip",new Vector3(0,-.020f,-.020f),new Vector3(.24f,.008f,.050f),P.Mat("EntrySconceWarm",new Color(1,.77f,.46f),.2f,true));
            var wall=P.Group(entry,"OppositeInformationWall");wall.localPosition=new Vector3(west+.020f,0,4.032f);wall.localRotation=Quaternion.Euler(0,-90,0);
            StoneSkin(wall,8.064f,stone,"ContinuousDirectoryStoneSkin");
            var directory=P.Group(wall,"FourFloorDirectory");directory.localPosition=new Vector3(1.82f,1.68f,-.025f);P.Box(directory,"DirectoryBacking",Vector3.zero,new Vector3(2.62f,1.28f,.03f),metal);
            ConvergenceHallEntranceBoards.Directory(directory);directory.localScale=Vector3.one*.88f;
            var escape=P.Group(wall,"B1EscapePlan");escape.localPosition=new Vector3(3.67f,1.44f,-.029f);P.Box(escape,"PlanBacking",Vector3.zero,new Vector3(.56f,.41f,.030f),black);
            P.Box(escape,"PlanPaper",new Vector3(0,0,-.020f),new Vector3(.525f,.375f,.004f),white);Text(escape,"EscapeHeading","피난 안내도 B1",new Vector3(0,.145f,-.024f),new Vector2(.49f,.055f),26,Color.black);
            var all=building.transform.Find("Floors/B1Footprint").GetComponent<Renderer>().bounds;
            foreach(Transform room in building.transform.Find("RoomsAndCores"))
            {
                var floor=room.Find("RoomFloor");if(!floor||!room.gameObject.activeSelf)continue;var b=floor.GetComponent<Renderer>().bounds;
                P.Box(escape,"PlanRoom",new Vector3((b.center.x-all.center.x)/all.size.x*.46f,(b.center.z-all.center.z)/all.size.z*.25f-.012f,-.026f),new Vector3(b.size.x/all.size.x*.44f,b.size.z/all.size.z*.24f,.002f),metal);
            }
            foreach(float x in new[]{-.60f,-.40f})P.Box(wall,"WallSwitch",new Vector3(x,1.12f,-.045f),new Vector3(.080f,.095f,.025f),white);
            var extinguisher=P.Group(wall,"FireExtinguisher");extinguisher.localPosition=new Vector3(3.68f,0,-.15f);
            var red=P.Mat("EntryFireExtinguisherRed",new Color(.68f,.025f,.025f),.30f);
            P.Cylinder(extinguisher,"RedCylinder",new Vector3(0,.25f,0),new Vector3(.13f,.22f,.13f),red,Quaternion.identity);P.Box(extinguisher,"Handle",new Vector3(0,.485f,0),new Vector3(.105f,.040f,.065f),black);
            P.Tube(extinguisher,"SignStand",new Vector3(.05f,.06f,.025f),new Vector3(.05f,.77f,.025f),.015f,red);P.Box(extinguisher,"FireSign",new Vector3(.05f,.74f,-.012f),new Vector3(.18f,.12f,.012f),red);Text(extinguisher,"FireLabel","소화기",new Vector3(.05f,.74f,-.021f),new Vector2(.16f,.10f),28,Color.white);
        }
        public static void ApplyTo(GameObject root)
        {
            Init();var old=root.transform.Find("ExteriorBoundary/EntranceGlazing");foreach(Transform child in old)child.gameObject.SetActive(false);
            var sign=root.transform.Find("DoorsAndSigns/Entrance");if(sign)sign.gameObject.SetActive(false);
            var note=root.transform.Find("DoorsAndSigns/PrototypeNote");if(note)note.gameObject.SetActive(false);
            var entrance=Group(root.transform,"MainEntranceUpgrade");foreach(Transform child in entrance.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            float east=ConvergenceHallB1Setup.Plan(772,878).x,west=ConvergenceHallB1Setup.Plan(632,878).x;
            entrance.position=new Vector3(east-Width/2,0,0);entrance.rotation=Quaternion.identity;entrance.localScale=Vector3.one;
            Bank(entrance,"ExteriorDoorBank",BankThickness/2,false);Bank(entrance,"InteriorDoorBank",Depth-BankThickness/2,true);
            SideGlass(entrance,-Width/2+BankThickness/2);
            var fixedWing=P.Group(entrance,"SeatingFixedGlazing");float min=west-entrance.position.x,max=-Width/2,span=max-min;int panes=Mathf.Max(1,Mathf.RoundToInt(span));
            for(int i=0;i<panes;i++)
            {
                float x=min+span*(i+.5f)/panes,w=span/panes-.03f;
                P.Box(fixedWing,"ClearUpperPane",new Vector3(x,1.91f,BankThickness/2),new Vector3(w,1.78f,.016f),glass);
                P.Box(fixedWing,"FrostedLowerPane",new Vector3(x,.515f,BankThickness/2),new Vector3(w,.95f,.016f),film);
            }
            for(int i=0;i<=panes;i++)P.Box(fixedWing,"FixedMullion",new Vector3(min+span*i/panes,H/2,BankThickness/2),new Vector3(Frame,H,BankThickness),metal);
            foreach(float y in new[]{.028f,1.0f,H-.028f})P.Box(fixedWing,"FixedHorizontalRail",new Vector3((min+max)/2,y,BankThickness/2),new Vector3(span,Frame,BankThickness),metal);
            SeatCorner(entrance,min,span);
            foreach(float z in new[]{.46f,Depth-.40f,Depth+.40f})foreach(float x in new[]{-1.45f,1.45f})FloorPad(entrance,"TactilePad",x,z);
            var roof=P.Group(entrance,"NavySlattedCeiling");DownQuad(roof,"LobbyCeilingBack",-Width/2,Width/2,0,8.064f,H-.018f,black);
            DownQuad(roof,"SeatingRearCeilingBack",min,-Width/2,4.6f,8.064f,H-.018f,black);
            for(int i=0;i<45;i++){float z=.08f+i*.18f;DownQuad(roof,"NavySlat"+i,z<4.6f?-Width/2:min,Width/2,z,z+.08f,H-.065f,navy);}
            DownQuad(entrance,"VestibuleCeiling",-Width/2,Width/2,0,Depth,H-.105f,P.Mat("VestibuleCeilingWarm",new Color(.65f,.62f,.53f),.10f));
            var downlight=P.Mat("EntryWarmDownlight",new Color(1,.86f,.61f),.2f,true);
            foreach(float x in new[]{-1.45f,1.45f})P.Cylinder(entrance,"VestibuleDownlight",new Vector3(x,H-.13f,1.5f),new Vector3(.10f,.008f,.10f),downlight,Quaternion.identity);
            MirrorLayout(entrance);entrance.position=new Vector3(west+Width/2,0,0);WallDetails(root,entrance);
        }
        static void Validate(GameObject root)
        {
            var entry=root.transform.Find("MainEntranceUpgrade");if(!entry)throw new Exception("Main entrance upgrade missing.");
            var outer=entry.Find("ExteriorDoorBank/MeasuredHeadRail").GetComponent<Renderer>().bounds;var inner=entry.Find("InteriorDoorBank/MeasuredHeadRail").GetComponent<Renderer>().bounds;
            if(Mathf.Abs(inner.size.x-Width)>.002f||Mathf.Abs(inner.max.z-outer.min.z-Depth)>.002f||Mathf.Abs(inner.min.x-ConvergenceHallB1Setup.Plan(632,878).x)>.002f)throw new Exception("Measured entrance 6.75x3m or opposite wall attachment mismatch.");
            if(entry.Find("SeatingFixedGlazing").GetComponentsInChildren<Transform>().Any(t=>t.name.Contains("Leaf")))throw new Exception("Seating side must have fixed glazing only.");
            if(entry.Find("SeatingCorner").Cast<Transform>().Count(t=>t.name.Contains("RoundStool"))!=3||!entry.Find("SeatingCorner/WoodCoffeeTable"))throw new Exception("Entrance seating incomplete.");
            var boundary=root.transform.Find("ExteriorBoundary/EntranceGlazing");
            if(!boundary.GetComponent<BoxCollider>().enabled&&!boundary.Find("DoorBoundarySegments"))throw new Exception("Exterior boundary collision lost.");
            if(root.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene())
            {
                Physics.SyncTransforms();var start=ConvergenceHallB1Setup.Plan(745,830,.70f);
                foreach(var c in entry.GetComponentsInChildren<Collider>())if(c.enabled&&c.gameObject.activeInHierarchy&&Vector3.Distance(c.ClosestPoint(start),start)<.578f)throw new Exception("Entrance overlaps the current wheelchair start: "+c.name);
            }
            if(entry.Find("PortableInformationMonitor")||!entry.Find("OppositeInformationWall/FourFloorDirectory")||!entry.Find("OppositeInformationWall/B1EscapePlan"))throw new Exception("Entrance wall details missing.");
            foreach(var renderer in entry.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
            {
                if(!material||!material.HasProperty("_BaseMap"))continue;var path=AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap"));
                if(path.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase)||path.EndsWith(".png",StringComparison.OrdinalIgnoreCase))throw new Exception("Entrance still uses a reference photograph: "+path);
            }
            var donor=entry.Find("SeatingCorner/DonorWallPanel");
            if(donor.Cast<Transform>().Count(t=>t.name.StartsWith("DonorPlate_"))<70||donor.GetComponentsInChildren<Text>().Count(t=>t.name=="DonorName")<70)throw new Exception("Native donor plaques/text missing.");
            if(entry.Find("OppositeInformationWall/FourFloorDirectory").GetComponentsInChildren<Text>().Count(t=>t.name=="RoomCode")<80)throw new Exception("Native directory cells missing.");
            Debug.Log("NATIVE_ENTRANCE_BOARDS_OK: monitor removed; modeled gold plaques and editable text; B1/1/2/3 directory cells; no photograph textures.");
            Debug.Log("MAIN_ENTRANCE_OK: mirrored to west/B101 wall; 6.75x3m preserved, native donor seating wall and opposite directory/escape wall; spawn clear.");
        }
        [MenuItem("Tools/EEG Wheelchair/Apply Photo Main Entrance")]
        public static void Apply()
        {
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ConvergenceHallWallMaterials.ApplyTo(root);Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
        }
        public static void ApplyAndValidate()
        {Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Entrance update duplicated parts.");SessionState.SetBool("EEG.ConvergenceQA.FocusMainEntrance",true);ConvergenceHallB1Validation.BuildAndValidate();}
        public static void ApplyAndCapture()
        {Apply();SessionState.SetBool("EEG.ConvergenceQA.FocusMainEntrance",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();}
    }
}



