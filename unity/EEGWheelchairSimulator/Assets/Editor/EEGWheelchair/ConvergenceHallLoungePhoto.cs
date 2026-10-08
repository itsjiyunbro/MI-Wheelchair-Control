using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    // Interior proportions are estimated from the four lounge photographs; the plan footprint is retained.
    public static class ConvergenceHallLoungePhoto
    {
        static Material white,ivory,blue,navy,metal,dark,oak,green,greenLight,pot,soil,carpet,screen,gray;
        static int shapeIndex;
        static Transform G(Transform p,string name,Vector3 at,float yaw=0)
        {var t=P.Group(p,name);t.localPosition=at;t.localRotation=Quaternion.Euler(0,yaw,0);return t;}
        static void B(Transform p,string n,Vector3 at,Vector3 size,Material m)=>P.Box(p,n,at,size,m);
        static void C(Transform p,string n,Vector3 at,float diameter,float height,Material m)=>P.Cylinder(p,n,at,new Vector3(diameter,height/2,diameter),m,Quaternion.identity);
        static void Obstacle(Transform p,Vector3 at,Vector3 size)
        {var c=p.gameObject.AddComponent<BoxCollider>();c.center=at;c.size=size;p.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");}
        static void Label(Transform p,string n,string text,Vector3 at,Vector2 size,int font,Color color)
        {
            var t=new GameObject(n,typeof(RectTransform),typeof(Canvas),typeof(Text)).transform;t.SetParent(p,false);t.localPosition=at;t.localScale=Vector3.one*.001f;
            t.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)t).sizeDelta=size*1000;
            var tx=t.GetComponent<Text>();tx.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");tx.text=text;tx.fontSize=font;tx.resizeTextForBestFit=true;tx.resizeTextMinSize=5;tx.resizeTextMaxSize=font;tx.alignment=TextAnchor.MiddleCenter;tx.color=color;tx.raycastTarget=false;
        }
        // Beveled upholstery, with rounded corners and softened top and bottom edges.
        static Transform Soft(Transform p,string n,Vector3 at,Vector3 size,float radius,Material mat)
        {
            var v=new List<Vector3>();var tri=new List<int>();const int count=32;float bevel=Mathf.Min(.025f,size.y*.25f);
            for(int ring=0;ring<4;ring++)
            {
                float inset=ring==0||ring==3?bevel:0,y=ring==0?-size.y/2:ring==1?-size.y/2+bevel:ring==2?size.y/2-bevel:size.y/2;
                float r=Mathf.Max(.004f,radius-inset),hx=size.x/2-radius,hz=size.z/2-radius;
                for(int corner=0;corner<4;corner++)for(int j=0;j<8;j++)
                {float a=(corner*90+j*90f/7)*Mathf.Deg2Rad;v.Add(new Vector3((corner==0||corner==3?hx:-hx)+Mathf.Cos(a)*r,y,(corner<2?hz:-hz)+Mathf.Sin(a)*r));}
            }
            for(int ring=0;ring<3;ring++)for(int j=0;j<count;j++){int a=ring*count+j,b=ring*count+(j+1)%count;tri.AddRange(new[]{a,a+count,b+count,a,b+count,b});}
            v.Add(new Vector3(0,-size.y/2,0));v.Add(new Vector3(0,size.y/2,0));
            for(int j=0;j<count;j++){tri.AddRange(new[]{128,j,(j+1)%count,129,96+(j+1)%count,96+j});}
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var t=G(p,n,at);t.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("LoungeSoft"+shapeIndex++,mesh);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t;
        }
        static void Materials()
        {
            white=P.Mat("LoungeInteriorWhite",new Color(.90f,.91f,.89f),.30f);ivory=P.Mat("LoungeIvoryUpholstery",new Color(.85f,.85f,.76f),.13f);
            blue=P.Mat("LoungePowderBlueUpholstery",new Color(.46f,.63f,.71f),.09f);navy=P.Mat("LoungeHealthNavy",new Color(.035f,.075f,.25f),.17f);
            metal=P.Mat("LoungeBrushedAluminum",new Color(.56f,.59f,.58f),.46f);metal.SetFloat("_Metallic",.4f);
            dark=P.Mat("LoungeEquipmentBlack",new Color(.025f,.031f,.034f),.23f);oak=P.Mat("LoungeTableOak",new Color(.61f,.48f,.27f),.2f);
            green=P.Mat("LoungeLeavesDeepGreen",new Color(.07f,.22f,.065f),.20f);greenLight=P.Mat("LoungeLeavesSage",new Color(.27f,.40f,.15f),.19f);
            pot=P.Mat("LoungeStonePlanters",new Color(.52f,.51f,.43f),.05f);soil=P.Mat("LoungePottingSoil",new Color(.09f,.065f,.035f),.02f);
            carpet=P.Mat("LoungeWarmCarpet",new Color(.62f,.59f,.53f),.015f);screen=P.Mat("LoungeMedicalDisplayBlue",new Color(.13f,.40f,.64f),.32f);
            gray=P.Mat("LoungeExposedConcrete",new Color(.61f,.62f,.59f),.08f);
        }
        static void FinishSurfaces(GameObject root)
        {
            P.Folder("Assets/Art/Textures/ConvergenceHall");
            const string path="Assets/Art/Textures/ConvergenceHall/LoungeFineGrain.asset";
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!tex)
            {
                tex=new Texture2D(128,128,TextureFormat.RGB24,true){name="LoungeFineGrain",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
                var random=new System.Random(1248);var pixels=new Color[128*128];
                for(int y=0;y<128;y++)for(int x=0;x<128;x++){float v=.89f+(float)random.NextDouble()*.10f+((x+y)%2)*.01f;pixels[y*128+x]=new Color(v,v,v);}
                tex.SetPixels(pixels);tex.Apply();AssetDatabase.CreateAsset(tex,path);
            }
            var shader=Shader.Find("ConvergenceHall/Lounge Matte Surface");if(!shader)throw new Exception("Lounge matte shader missing");
            foreach(var m in new[]{white,ivory,blue,carpet,pot,oak,gray,green,greenLight,screen})
            {
                m.shader=shader;m.SetFloat("_SrcBlend",1);m.SetFloat("_DstBlend",0);m.SetFloat("_ZWrite",1);m.SetFloat("_Surface",0);m.renderQueue=2000;
                m.SetTexture("_BaseMap",m==carpet||m==blue||m==ivory||m==pot||m==gray?tex:null);m.SetFloat("_TextureMetres",m==gray?.70f:m==pot?.25f:.18f);EditorUtility.SetDirty(m);
            }
            var film=P.Mat("LoungeMintPrivacyGlass",new Color(.74f,.85f,.80f,.95f),.1f);film.shader=shader;film.SetFloat("_Surface",1);film.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);film.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);film.SetFloat("_ZWrite",0);film.SetFloat("_TextureMetres",1);film.renderQueue=3000;film.SetOverrideTag("RenderType","Transparent");film.SetShaderPassEnabled("ShadowCaster",false);film.SetShaderPassEnabled("DepthOnly",false);
            foreach(var r in root.transform.Find("RoomsAndCores/Lounge").GetComponentsInChildren<Renderer>(true))
                if(r.sharedMaterial&&(r.sharedMaterial.name=="PrivacyFilmGlass"||r.sharedMaterial.name=="FrostedTeal"||r.sharedMaterial.name=="LoungeMintPrivacyGlass")){r.sharedMaterial=film;r.shadowCastingMode=ShadowCastingMode.Off;}
            EditorUtility.SetDirty(film);
            var entryWhite=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/LoungeDoorWhite.mat");entryWhite.shader=shader;entryWhite.SetColor("_BaseColor",white.GetColor("_BaseColor"));entryWhite.SetFloat("_TextureMetres",1);entryWhite.SetFloat("_ZWrite",1);entryWhite.SetFloat("_SrcBlend",1);entryWhite.SetFloat("_DstBlend",0);EditorUtility.SetDirty(entryWhite);
        }
        static void SofaModule(Transform p,string n,Vector3 at,float yaw,bool arm)
        {
            var t=G(p,n,at,yaw);
            Soft(t,"Plinth",new Vector3(0,.095f,0),new Vector3(.81f,.14f,.83f),.09f,ivory);
            Soft(t,"SeatCushion",new Vector3(0,.32f,-.04f),new Vector3(.79f,.31f,.71f),.095f,blue);
            var back=Soft(t,"CurvedIvoryBack",new Vector3(0,.63f,.34f),new Vector3(.81f,.54f,.18f),.072f,ivory);back.localRotation=Quaternion.Euler(-5,0,0);
            if(arm)Soft(t,"SoftEndArm",new Vector3(-.39f,.48f,0),new Vector3(.15f,.66f,.83f),.07f,ivory);
            foreach(float x in new[]{-.29f,.29f})foreach(float z in new[]{-.28f,.28f})C(t,"Foot",new Vector3(x,.036f,z),.055f,.05f,dark);
            Obstacle(t,new Vector3(0,.45f,0),new Vector3(.84f,.90f,.88f));
        }
        static void Seating(Transform p)
        {
            var g=G(p,"SeatingCorner",new Vector3(3.5f,0,1.67f));
            for(int i=0;i<3;i++)SofaModule(g,"BackSofa"+i,new Vector3((i-1)*.81f,0,.50f),0,i==0);
            SofaModule(g,"LeftChaise",new Vector3(-1.18f,0,-.29f),-90,false);
            SofaModule(g,"RightSofa",new Vector3(1.18f,0,-.29f),90,false);
            var table=G(g,"OvalCoffeeTable",new Vector3(0,0,-.49f));
            Soft(table,"OakOvalTop",new Vector3(0,.47f,0),new Vector3(1.03f,.045f,.62f),.29f,oak);
            foreach(float x in new[]{-.31f,.31f})foreach(float z in new[]{-.18f,.18f})P.Tube(table,"SplayedLeg",new Vector3(x,.445f,z),new Vector3(x*1.12f,.04f,z*1.15f),.028f,metal);
            C(table,"Tray",new Vector3(-.20f,.499f,0),.22f,.012f,ivory);
            for(int i=0;i<6;i++)Soft(table,"WrappedSweet"+i,new Vector3(-.2f+Mathf.Sin(i*2.3f)*.065f,.52f,Mathf.Cos(i*2.3f)*.06f),new Vector3(.032f,.018f,.020f),.008f,i%2==0?blue:oak);
            B(table,"TableCard",new Vector3(.23f,.60f,.03f),new Vector3(.12f,.22f,.012f),ivory);Label(table,"CardText","이용 안내",new Vector3(.23f,.62f,.022f),new Vector2(.105f,.10f),17,navy.color);
            Obstacle(table,new Vector3(0,.25f,0),new Vector3(1.04f,.50f,.63f));
        }
        static void Leaf(Transform p,string n,Vector3 from,Vector3 to,float width,Material mat)
        {
            var axis=to-from;var side=Vector3.Cross(axis,Vector3.up).normalized;if(side.sqrMagnitude<.1f)side=Vector3.right;
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int j=0;j<=6;j++)
            {
                float u=j/6f,bulge=Mathf.Sin(u*Mathf.PI),half=width*.5f*bulge;var centre=from+axis*u+Vector3.up*(.025f*bulge);
                vertices.Add(centre-side*half);vertices.Add(centre+Vector3.up*(.009f*bulge));vertices.Add(centre+side*half);
                if(j==0)continue;int a=(j-1)*3,b=j*3;
                foreach(var triangle in new[]{new[]{a,b,a+1},new[]{b,b+1,a+1},new[]{a+1,b+1,a+2},new[]{b+1,b+2,a+2}}){indices.AddRange(triangle);}
            }
            int frontVertexCount=vertices.Count;var frontIndices=indices.ToArray();vertices.AddRange(vertices.ToArray());for(int j=0;j<frontIndices.Length;j+=3)indices.AddRange(new[]{frontIndices[j+2]+frontVertexCount,frontIndices[j+1]+frontVertexCount,frontIndices[j]+frontVertexCount});
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();
            var t=G(p,n,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("LoungeLeaf"+shapeIndex++,mesh);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        static void Plant(Transform parent,string name,Vector3 at,float scale,bool tree=false)
        {
            var p=G(parent,name,at);p.localScale=Vector3.one*scale;
            // Tapered stone pot made with a closed lathed profile.
            var v=new List<Vector3>();var tr=new List<int>();float[] heights={0,.025f,.26f,.28f,.28f,.24f};float[] radii={.105f,.112f,.15f,.15f,.133f,.128f};
            for(int j=0;j<heights.Length;j++)for(int i=0;i<24;i++){float a=i*Mathf.PI/12;v.Add(new Vector3(Mathf.Cos(a)*radii[j],heights[j],Mathf.Sin(a)*radii[j]));}
            for(int j=0;j<5;j++)for(int i=0;i<24;i++){int a=j*24+i,b=j*24+(i+1)%24;tr.AddRange(new[]{a,a+24,b,b,a+24,b+24});}
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();var body=G(p,"TaperedStonePot",Vector3.zero);body.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("LoungePot"+shapeIndex++,mesh);body.gameObject.AddComponent<MeshRenderer>().sharedMaterial=pot;
            C(p,"Soil",new Vector3(0,.25f,0),.25f,.015f,soil);
            if(tree)P.Tube(p,"WoodyTrunk",new Vector3(0,.25f,0),new Vector3(.012f,.91f,0),.018f,oak);
            int count=tree?15:10;
            for(int i=0;i<count;i++)
            {
                float a=i*2.39996f,h=tree?.75f+(i%5)*.085f:.43f+(i%4)*.05f,reach=tree?.28f:.24f;
                var start=new Vector3(0,tree?.65f+(i%4)*.065f:.25f,0);var tip=new Vector3(Mathf.Cos(a)*reach,h,Mathf.Sin(a)*reach);
                P.Tube(p,"Stem"+i,start,tip,.006f,green);
                if(tree){for(int k=0;k<5;k++){float b=a+(k-2)*.33f;Leaf(p,"NarrowLeaf"+i+"_"+k,tip,tip+new Vector3(Mathf.Cos(b)*.30f,-.12f+k*.027f,Mathf.Sin(b)*.30f),.035f,green);}}
                else for(int k=0;k<4;k++)
                {var baseAt=Vector3.Lerp(start,tip,.42f+k*.16f);float length=.19f-k*.024f;foreach(int sign in new[]{-1,1}){float b=a+sign*1.05f;Leaf(p,"SplitLeaf"+i+"_"+k+"_"+sign,baseAt,baseAt+new Vector3(Mathf.Cos(b)*length,.04f,Mathf.Sin(b)*length),.065f,i%3==0?greenLight:green);}}
            }
            if(tree)Obstacle(p,new Vector3(0,.30f,0),new Vector3(.31f,.60f,.31f));
        }
        static void Shelf(Transform p,string name,Vector3 at,float width,bool plants)
        {
            var t=G(p,name,at);
            foreach(float y in new[]{.12f,.55f,1.02f})B(t,"Shelf"+y,new Vector3(0,y,0),new Vector3(width,.033f,.43f),white);
            foreach(float x in new[]{-width/2+.025f,width/2-.025f})foreach(float z in new[]{-.195f,.195f})B(t,"SquareLeg",new Vector3(x,.52f,z),new Vector3(.028f,1.04f,.028f),white);
            if(plants){Plant(t,"FernPot",new Vector3(-width*.23f,1.04f,0),.90f);Plant(t,"SagePot",new Vector3(width*.23f,1.04f,0),.72f);}
            for(int i=0;i<3;i++)
            {
                float x=-width*.31f+i*width*.31f;
                Soft(t,"FoldedTowel"+i,new Vector3(x,.585f,.015f),new Vector3(width*.26f,.075f,.30f),.035f,i%2==0?ivory:carpet);
                B(t,"StorageBasket"+i,new Vector3(x,.25f,0),new Vector3(width*.26f,.23f,.32f),ivory);
                for(int j=0;j<6;j++)B(t,"BasketSlot"+i+"_"+j,new Vector3(x-width*.10f+j*width*.04f,.27f,-.163f),new Vector3(.009f,.09f,.003f),gray);
            }
            Obstacle(t,new Vector3(0,.52f,0),new Vector3(width,1.04f,.45f));
        }
        static void ScreenUI(Transform t,float width,float height,string title)
        {
            B(t,"BlackBezel",Vector3.zero,new Vector3(width+.035f,height+.035f,.028f),dark);
            B(t,"BlueDisplay",new Vector3(0,0,-.017f),new Vector3(width,height,.005f),screen);
            Label(t,"DisplayHeading",title,new Vector3(0,height*.37f,-.023f),new Vector2(width*.90f,height*.14f),24,Color.white);
            for(int y=0;y<2;y++)for(int x=0;x<2;x++)
            {float px=(x-.5f)*width*.44f,py=(.5f-y)*height*.23f;B(t,"HealthTile"+x+y,new Vector3(px,py,-.023f),new Vector3(width*.36f,height*.19f,.004f),white);Label(t,"HealthTileText"+x+y,new[]{"건강 측정","측정 기록","이용 안내","시작하기"}[y*2+x],new Vector3(px,py,-.027f),new Vector2(width*.34f,height*.17f),18,navy.color);}
            B(t,"StartBar",new Vector3(0,-height*.34f,-.023f),new Vector3(width*.72f,height*.09f,.004f),navy);
        }
        static void Kiosk(Transform p,string n,Vector3 at,bool tall)
        {
            var t=G(p,n,at);float h=tall?1.38f:1.03f;
            Soft(t,"StableFoot",new Vector3(0,.035f,0),new Vector3(tall?.52f:.39f,.055f,.46f),.035f,white);
            B(t,"Pedestal",new Vector3(0,h*.44f,.06f),new Vector3(tall?.30f:.12f,h*.82f,.10f),white);
            var display=G(t,"TouchHead",new Vector3(0,h,0));display.localRotation=Quaternion.Euler(12,0,0);
            B(display,"WhiteHousing",Vector3.zero,new Vector3(tall?.49f:.34f,tall?.87f:.49f,.075f),white);
            var ui=G(display,"Interface",new Vector3(0,0,-.043f));ScreenUI(ui,tall?.40f:.28f,tall?.70f:.41f,tall?"Biogram":"건강 라운지");
            B(display,"CameraSlot",new Vector3(0,tall?.397f:.223f,-.044f),new Vector3(.055f,.016f,.01f),dark);
            Obstacle(t,new Vector3(0,.84f,0),new Vector3(tall?.54f:.40f,1.68f,.50f));
        }
        static void Stool(Transform p,Vector3 at)
        {
            var t=G(p,"MeasuringStool",at);C(t,"PaddedSeat",new Vector3(0,.49f,0),.34f,.08f,dark);C(t,"PneumaticStem",new Vector3(0,.25f,0),.04f,.43f,metal);
            for(int i=0;i<5;i++){float a=i*Mathf.PI*.4f;var foot=new Vector3(Mathf.Cos(a)*.20f,.05f,Mathf.Sin(a)*.20f);P.Tube(t,"RadialFoot"+i,new Vector3(0,.10f,0),foot,.024f,dark);C(t,"Caster"+i,foot,.045f,.05f,dark);}Obstacle(t,new Vector3(0,.28f,0),new Vector3(.43f,.56f,.43f));
        }
        static void CurvedHealthBackdrop(Transform parent)
        {
            var v=new List<Vector3>();var tr=new List<int>();
            for(int i=0;i<=32;i++)
            {
                float x=Mathf.Lerp(-1.15f,1.15f,i/32f),t=Mathf.Clamp01((x+.32f)/.88f),top=1.57f+.57f*t*t*(3-2*t);
                v.AddRange(new[]{new Vector3(x,.09f,.345f),new Vector3(x,top,.345f),new Vector3(x,.09f,.41f),new Vector3(x,top,.41f)});
                if(i==0)continue;int a=(i-1)*4,b=i*4;
                tr.AddRange(new[]{a,a+1,b+1,a,b+1,b,a+2,b+3,a+3,a+2,b+2,b+3,a+1,a+3,b+3,a+1,b+3,b+1,a,b+2,a+2,a,b,b+2});
            }
            tr.AddRange(new[]{0,2,3,0,3,1,128,131,130,128,129,131});
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();var t0=G(parent,"CurvedHealthBackdrop",Vector3.zero);t0.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("LoungeBiogramCurvedBackdrop",mesh);t0.gameObject.AddComponent<MeshRenderer>().sharedMaterial=white;
        }
        static void MedicalStations(Transform p)
        {
            var station=G(p,"BiogramHealthStation",new Vector3(-1.8f,0,1.95f));
            CurvedHealthBackdrop(station);
            Label(station,"BiogramBrand","Biogram",new Vector3(.53f,1.86f,.341f),new Vector2(1.10f,.32f),150,navy.color);
            Label(station,"HealthCaption","건강을 기록하는 공간",new Vector3(.5f,2.02f,.341f),new Vector2(.9f,.07f),24,navy.color);
            B(station,"BlueCounter",new Vector3(0,.77f,.06f),new Vector3(2.34f,.08f,.59f),navy);
            foreach(float x in new[]{-.97f,.97f})B(station,"CounterLeg",new Vector3(x,.39f,.12f),new Vector3(.065f,.75f,.46f),white);
            var pressure=G(station,"BloodPressureMonitor",new Vector3(-.68f,.83f,-.02f));Soft(pressure,"MachineCase",new Vector3(0,.14f,0),new Vector3(.39f,.28f,.31f),.065f,white);
            var cuff=P.Cylinder(pressure,"ArmCuff",new Vector3(.10f,.20f,-.08f),new Vector3(.17f,.09f,.17f),dark,Quaternion.Euler(90,0,0));
            B(pressure,"ReadingPanel",new Vector3(-.09f,.29f,-.045f),new Vector3(.12f,.015f,.11f),screen);
            var measure=G(station,"BodyCompositionColumn",new Vector3(.45f,0,-.18f));B(measure,"Upright",new Vector3(0,.82f,0),new Vector3(.07f,1.48f,.07f),metal);
            var panel=G(measure,"SmallScreen",new Vector3(0,1.42f,-.045f));ScreenUI(panel,.23f,.18f,"측정 결과");
            Soft(measure,"ScalePlatform",new Vector3(0,.055f,-.36f),new Vector3(.53f,.09f,.55f),.09f,white);
            foreach(float x in new[]{-.15f,.15f})Soft(measure,"FootElectrode",new Vector3(x,.104f,-.36f),new Vector3(.09f,.008f,.29f),.04f,metal);
            foreach(int side in new[]{-1,1}){P.Tube(measure,"ArmSensor"+side,new Vector3(0,1.04f,0),new Vector3(side*.31f,1.01f,-.08f),.022f,metal);P.Tube(measure,"Grip"+side,new Vector3(side*.31f,1.01f,-.08f),new Vector3(side*.31f,.91f,-.12f),.037f,dark);}
            Obstacle(station,new Vector3(0,1.05f,.22f),new Vector3(2.36f,2.1f,.78f));Stool(p,new Vector3(-2.52f,0,1.18f));
            Kiosk(p,"TallHealthKiosk",new Vector3(-3.90f,0,1.76f),true);
            var screenStand=G(p,"MeasurementComputer",new Vector3(-5.08f,0,1.95f));B(screenStand,"Foot",new Vector3(0,.035f,0),new Vector3(.49f,.05f,.43f),white);B(screenStand,"Post",new Vector3(0,.66f,0),new Vector3(.05f,1.25f,.05f),metal);var monitor=G(screenStand,"Monitor",new Vector3(0,1.28f,0));ScreenUI(monitor,.51f,.32f,"건강 정보");B(screenStand,"InputShelf",new Vector3(0,.81f,-.12f),new Vector3(.43f,.025f,.30f),white);Obstacle(screenStand,new Vector3(0,.70f,0),new Vector3(.55f,1.4f,.46f));
            Stool(p,new Vector3(-5.12f,0,.95f));
        }
        static void Poster(Transform p,string n,Vector3 at,string heading)
        {
            var t=G(p,n,at);B(t,"BlueFrame",Vector3.zero,new Vector3(.40f,.59f,.012f),navy);B(t,"Paper",new Vector3(0,-.025f,-.01f),new Vector3(.365f,.49f,.003f),white);
            Label(t,"Title",heading,new Vector3(0,.24f,-.011f),new Vector2(.36f,.055f),20,Color.white);
            for(int i=0;i<4;i++){B(t,"Diagram"+i,new Vector3(-.105f,.15f-i*.11f,-.014f),new Vector3(.075f,.070f,.002f),i%2==0?blue:ivory);Label(t,"Caption"+i,new[]{"1. 회원 등록","2. 기기 선택","3. 건강 측정","4. 결과 확인"}[i],new Vector3(.055f,.15f-i*.11f,-.016f),new Vector2(.22f,.07f),15,navy.color);}
        }
        static void Ceiling(Transform root,Transform p,float w,float d)
        {
            var old=root.Find("CompletedRoomCeilings/LoungeCeiling");if(old)old.gameObject.SetActive(false);
            var roof=G(p,"ExposedCeiling",Vector3.zero);B(roof,"ConcreteSoffit",new Vector3(0,2.866f,0),new Vector3(w,.055f,d),gray);
            for(float x=-w/2+1;x<w/2;x+=2.4f)B(roof,"ConcreteBeam",new Vector3(x,2.76f,0),new Vector3(.16f,.19f,d),gray);
            for(int i=0;i<2;i++)P.Tube(roof,"ExposedPipe"+i,new Vector3(-w/2,2.66f,.3f+i*.95f),new Vector3(w/2,2.66f,.3f+i*.95f),.07f,gray);
            for(int i=0;i<3;i++)
            {
                var ring=G(roof,"SuspendedRectangle"+i,new Vector3(-3.9f+i*3.9f,2.48f,0));float rw=3.18f,rd=3.95f;
                foreach(float x in new[]{-rw/2,rw/2}){B(ring,"BlackLongRail",new Vector3(x,0,0),new Vector3(.052f,.065f,rd),dark);B(ring,"MatteDiffuser",new Vector3(x,-.035f,0),new Vector3(.028f,.006f,rd-.055f),white);}
                foreach(float z in new[]{-rd/2,rd/2}){B(ring,"BlackCrossRail",new Vector3(0,0,z),new Vector3(rw,.065f,.052f),dark);B(ring,"MatteDiffuser",new Vector3(0,-.035f,z),new Vector3(rw-.055f,.006f,.028f),white);}
                foreach(float x in new[]{-1.35f,1.35f})foreach(float z in new[]{-1.65f,1.65f})P.Tube(ring,"SuspensionWire",new Vector3(x,.033f,z),new Vector3(x,.32f,z),.004f,metal);
                var ac=G(roof,"CassetteAC"+i,new Vector3(-3.9f+i*3.9f,2.72f,.7f));B(ac,"Case",Vector3.zero,new Vector3(.72f,.10f,.72f),white);B(ac,"Intake",new Vector3(0,-.055f,0),new Vector3(.44f,.02f,.44f),gray);for(int j=0;j<8;j++)B(ac,"Grille"+j,new Vector3(0,-.067f,-.19f+j*.054f),new Vector3(.44f,.008f,.01f),white);
            }
            P.Combine(roof,"LoungePhotoCeiling");roof.Find("CombinedVisual").GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        public static void ApplyTo(GameObject root)
        {
            var previous=root.transform.Find("LoungePhotoInterior");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);shapeIndex=0;Materials();
            var floor=root.transform.Find("RoomsAndCores/Lounge/RoomFloor").GetComponent<Renderer>().bounds;
            var p=G(root.transform,"LoungePhotoInterior",new Vector3(floor.center.x,0,floor.center.z),90);float w=floor.size.z,d=floor.size.x;
            if(w<11.5f||d<5.5f)throw new Exception("Unexpected lounge footprint: "+floor.size);
            var contents=G(p,"FurnitureAndEquipment",Vector3.zero);
            B(contents,"WarmCarpetInset",new Vector3(0,.010f,.36f),new Vector3(w-.15f,.006f,d-.85f),carpet);
            FinishSurfaces(root);Seating(contents);MedicalStations(contents);
            for(int i=0;i<3;i++)Shelf(contents,"PlantDisplayShelf"+i,new Vector3(-3.7f+i*1.6f,0,-1.64f),1.55f,true);
            Kiosk(contents,"RegistrationTerminalA",new Vector3(.85f,0,-1.64f),false);Kiosk(contents,"RegistrationTerminalB",new Vector3(1.40f,0,-1.64f),false);
            Shelf(contents,"SmallSofaShelf",new Vector3(1.30f,0,1.90f),.45f,false);
            Plant(contents,"TallCornerPlant",new Vector3(5.28f,0,1.50f),1.45f,true);
            var cabinet=G(contents,"InformationCabinet",new Vector3(5.45f,0,-.15f),70);
            B(cabinet,"Base",new Vector3(0,.35f,0),new Vector3(.82f,.70f,.36f),white);B(cabinet,"OpenCubby",new Vector3(0,.36f,-.184f),new Vector3(.71f,.45f,.004f),dark);B(cabinet,"MiddleShelf",new Vector3(0,.34f,-.20f),new Vector3(.73f,.024f,.38f),white);
            B(cabinet,"DisplayFoot",new Vector3(0,.735f,0),new Vector3(.39f,.03f,.23f),dark);B(cabinet,"DisplayStand",new Vector3(0,.90f,0),new Vector3(.055f,.3f,.05f),dark);
            var tv=G(cabinet,"HealthInformationDisplay",new Vector3(0,1.26f,0));ScreenUI(tv,.89f,.52f,"디지털헬스케어 라운지");Obstacle(cabinet,new Vector3(0,.75f,0),new Vector3(.95f,1.50f,.40f));
            var banner=G(contents,"MembershipBanner",new Vector3(2.24f,0,-1.51f),-12);
            B(banner,"BlueFabric",new Vector3(0,1.03f,0),new Vector3(.57f,1.69f,.012f),navy);foreach(float y in new[]{.18f,1.88f})B(banner,"SilverBar",new Vector3(0,y,0),new Vector3(.63f,.026f,.03f),metal);B(banner,"StandFoot",new Vector3(0,.035f,0),new Vector3(.54f,.04f,.29f),metal);P.Tube(banner,"RearPole",new Vector3(0,.05f,.05f),new Vector3(0,1.88f,.05f),.016f,metal);
            Label(banner,"Welcome","안녕하세요\n디지털의료기기\n방문을 환영합니다!",new Vector3(0,1.56f,-.009f),new Vector2(.50f,.28f),32,Color.white);Label(banner,"Membership","회원가입 및\n얼굴등록 방법",new Vector3(0,1.12f,-.009f),new Vector2(.51f,.32f),45,Color.white);Label(banner,"Steps","회원 등록 → 기기 선택\n측정 후 결과를 확인하세요",new Vector3(0,.57f,-.009f),new Vector2(.49f,.25f),23,Color.white);Obstacle(banner,new Vector3(0,.93f,0),new Vector3(.63f,1.86f,.31f));
            var purifier=G(contents,"EntranceAirPurifier",new Vector3(1.84f,0,-2.22f));Soft(purifier,"WhiteShell",new Vector3(0,.31f,0),new Vector3(.31f,.60f,.25f),.095f,white);Soft(purifier,"TopVent",new Vector3(0,.616f,0),new Vector3(.26f,.01f,.20f),.075f,dark);Obstacle(purifier,new Vector3(0,.32f,0),new Vector3(.32f,.64f,.26f));
            foreach(float x in new[]{-4.7f,-.2f,3.8f})Poster(contents,"HealthPoster"+x,new Vector3(x,1.87f,d/2-.09f),"디지털헬스케어 이용안내");
            Ceiling(root.transform,p,w,d);
            P.Combine(contents,"LoungePhotoFurniture");Validate(root);
            Debug.Log("LOUNGE_PHOTO_BUILD: floor "+w+" x "+d+"m; 5 modular sofa sections, 3 plant shelves, 3 touch kiosks, Biogram measurement station and exposed ceiling.");
        }
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        public static void Validate(GameObject root)
        {
            var p=root.transform.Find("LoungePhotoInterior");Check(p!=null,"Lounge interior missing");
            Check(root.transform.Cast<Transform>().Count(t=>t.name=="LoungePhotoInterior")==1,"Duplicate lounge interior");
            Check(p.GetComponentsInChildren<Light>(true).Length==0,"Lounge added a light effect");
            Check(p.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).All(m=>m&&!m.IsKeywordEnabled("_EMISSION")),"Lounge contains emissive material");
            var furniture=p.Find("FurnitureAndEquipment");Check(furniture.Find("SeatingCorner").GetComponentsInChildren<BoxCollider>().Length==6,"Sofa/table collision missing");
            Check(furniture.Find("BiogramHealthStation")&&furniture.Find("RegistrationTerminalA")&&furniture.Find("RegistrationTerminalB"),"Photo equipment missing");
            Check(!root.transform.Find("CompletedRoomCeilings/LoungeCeiling").gameObject.activeSelf,"Duplicate lounge ceiling");
            var bound=root.transform.Find("RoomsAndCores/Lounge/RoomFloor").GetComponent<Renderer>().bounds;
            foreach(var c in p.GetComponentsInChildren<BoxCollider>()){var a=c.transform.TransformPoint(c.center);Check(a.x>bound.min.x&&a.x<bound.max.x&&a.z>bound.min.z&&a.z<bound.max.z,"Furniture outside lounge: "+c.name);}
        }
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ApplyTo(root);int count=root.GetComponentsInChildren<Transform>(true).Length;ApplyTo(root);Check(count==root.GetComponentsInChildren<Transform>(true).Length,"Lounge build duplicates objects");PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.FocusLoungeInterior",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CaptureViews(string folder)
        {
            var root=GameObject.Find("ConvergenceHallB1");Validate(root);var p=root.transform.Find("LoungePhotoInterior");
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();var guard=chair.GetComponent<WheelchairCollisionGuard>();var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();sim.StopSimulation();
            var door=root.transform.Find("PlanDoors/Lounge_West_Automatic").GetComponent<InteractiveDoor>();door.enabled=false;door.SetProgressInstant(1);Physics.SyncTransforms();
            var entry=p.InverseTransformPoint(door.InteractionPoint);Debug.Log("LOUNGE_ENTRY_LOCAL "+entry);
            // A continuous route from the open doorway, to the seating approach, and along the measurement aisle.
            var route=new[]{new Vector3(entry.x,.5f,entry.z-.55f),new Vector3(entry.x,.5f,-.48f),new Vector3(2.85f,.5f,-.48f),new Vector3(.2f,.5f,-.48f),new Vector3(-4.8f,.5f,-.48f)};
            int samples=0;
            for(int i=1;i<route.Length;i++)for(int j=0;j<=30;j++){var pos=p.TransformPoint(Vector3.Lerp(route[i-1],route[i],j/30f));Check(guard.CanOccupy(pos,p.rotation),"Lounge route obstructed at "+p.InverseTransformPoint(pos));samples++;}
            foreach(int fps in new[]{30,120})
            {
                sim.SetControlSource(WheelchairControlSource.Keyboard);chair.transform.SetPositionAndRotation(p.TransformPoint(route[0]),p.rotation);door.ResetDoor();
                for(int j=0;j<fps;j++)door.Step(1f/fps);Check(door.Progress>.99f,"Lounge automatic entrance did not open");sim.StartSimulation();follow.enabled=false;
                for(int j=0;j<fps;j++)chair.ApplyInput(true,0,1f/fps);
                Check(p.InverseTransformPoint(chair.transform.position).z>entry.z+1.2f,"Wheelchair cannot enter furnished lounge");
                for(int j=0;j<fps*2;j++)chair.ApplyDirectionalInput(-1,0,1f/fps);
                Check(p.InverseTransformPoint(chair.transform.position).z<entry.z-.70f,"Wheelchair cannot reverse out of lounge");sim.StopSimulation();
            }
            chair.transform.position=new Vector3(180,.5f,180);for(int j=0;j<120;j++)door.Step(1f/120);
            Check(door.Progress<.01f,"Lounge automatic door does not close after exit");Check(!guard.CanOccupy(p.TransformPoint(new Vector3(entry.x,.5f,entry.z)),p.rotation),"Closed lounge door collision missing");door.SetProgressInstant(1);
            Debug.Log("LOUNGE_ACCESS_OK: "+samples+" wheelchair footprint samples through open entrance and furniture aisles.");
            var eyes=new[]{new Vector3(entry.x,1.55f,entry.z-1.15f),new Vector3(4.35f,1.5f,-.85f),new Vector3(.6f,1.55f,-.53f),new Vector3(-4.5f,1.5f,-.57f),new Vector3(3.3f,1.5f,.02f)};
            var targets=new[]{new Vector3(2.9f,1.05f,1.6f),new Vector3(-1.6f,1.2f,1.35f),new Vector3(-2.5f,1.05f,1.9f),new Vector3(-1.7f,1.12f,-1.64f),new Vector3(3.5f,.61f,1.7f)};
            var names=new[]{"LoungeEntrance","LoungeInteriorOverview","LoungeHealthStations","LoungePlantShelves","LoungeSeating"};
            for(int i=0;i<eyes.Length;i++)
            {
                cam.fieldOfView=i==0?76:78;cam.transform.position=p.TransformPoint(eyes[i]);cam.transform.LookAt(p.TransformPoint(targets[i]));
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string path=Path.Combine(folder,names[i]+".png");DateTime stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException(path);yield return null;}
            }
            door.ResetDoor();door.enabled=true;Debug.Log("LOUNGE_PHOTO_OK: native photo interior, duplicate-free rebuild, no emission and wheelchair routes validated; five views saved.");
        }
    }
}
