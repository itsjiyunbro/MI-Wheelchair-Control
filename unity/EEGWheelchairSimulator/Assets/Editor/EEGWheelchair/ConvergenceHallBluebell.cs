using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // Photo-based decorative props. Dimensions are estimates, not surveyed data.
    public static class ConvergenceHallBluebell
    {
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        static Material wood,green,screen,white,metal,navy,teal;
        static Transform Group(Transform p,string n)
        {var t=p.Find(n);if(!t){t=new GameObject(n).transform;t.SetParent(p,false);}return t;}
        static Material Existing(string n)=>AssetDatabase.LoadAssetAtPath<Material>(Mats+n+".mat");
        static Material Material(string n,Color color,float smooth=.2f)
        {
            var m=Existing(n);if(m)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=n};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);
            AssetDatabase.CreateAsset(m,Mats+n+".mat");return m;
        }
        static Transform Part(Transform p,string n,Vector3 at,Vector3 size,Material m,PrimitiveType type=PrimitiveType.Cube)
        {
            var t=p.Find(n);if(!t){var g=GameObject.CreatePrimitive(type);g.name=n;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());t=g.transform;t.SetParent(p,false);}
            t.localPosition=at;t.localRotation=Quaternion.identity;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=m;return t;
        }
        static void Label(Transform p,string n,string value,Vector3 at,float w,float h)
        {
            var t=p.Find(n);if(!t){var g=new GameObject(n,typeof(RectTransform),typeof(Canvas),typeof(Text));t=g.transform;t.SetParent(p,false);g.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;}
            t.localPosition=at;t.localRotation=Quaternion.identity;t.localScale=Vector3.one*.002f;((RectTransform)t).sizeDelta=new Vector2(w/.002f,h/.002f);
            var text=t.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;text.fontSize=30;text.resizeTextForBestFit=true;text.resizeTextMinSize=8;text.resizeTextMaxSize=30;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        }
        static void Collider(Transform t,Vector3 centre,Vector3 size)
        {var c=t.GetComponent<BoxCollider>();if(!c)c=t.gameObject.AddComponent<BoxCollider>();c.center=centre;c.size=size;t.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");}
        public static void ApplyTo(GameObject root)
        {
            var stand=root.transform.Find("BluebellStand")??root.transform.Find("BlueberryStand");
            if(!stand)throw new InvalidOperationException("Existing stand required.");stand.name="BluebellStand";
            var source=stand.Find("StandCollision").GetComponent<Renderer>().bounds;
            var room=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;
            float ratio=room.size.x/source.size.x;
            // Preserve existing objects and IDs; match both side boundaries, not just the width.
            foreach(Transform t in stand)
            {
                var p=t.position;p.x=room.min.x+(p.x-source.min.x)*ratio;t.position=p;
                var s=t.localScale;s.x*=ratio;t.localScale=s;
            }
            var block=stand.GetComponent<BoxCollider>();var centre=stand.TransformPoint(block.center);centre.x=room.center.x;
            block.center=stand.InverseTransformPoint(centre);var sz=block.size;sz.x=room.size.x/Mathf.Abs(stand.lossyScale.x);block.size=sz;
            stand.Find("StandLabel").GetComponent<Text>().text="BLUEBELL STAND";
            var landmarks=root.transform.Find("Landmarks");
            // Only the two obsolete generated placeholder meshes are removed.
            foreach(string n in new[]{"VendingMachine","VendingFront"}){var old=landmarks.Find(n);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            white=Existing("WarmWhite");metal=Existing("Metal");navy=Existing("Navy");teal=Existing("FrostedTeal");
            wood=Material("RecyclingOak",new Color(.65f,.49f,.29f));green=Material("VendingGreen",new Color(.035f,.20f,.12f),.3f);
            screen=Material("VendingDisplay",new Color(.38f,.56f,.74f),.28f);
            var row=Group(landmarks,"BluebellAmenities");row.localPosition=ConvergenceHallB1Setup.Plan(710,649);row.localRotation=Quaternion.Euler(0,-90,0);
            Vending(row,"GreenVendingMachine",.25f,green,"DRINKS");Vending(row,"WhiteVendingMachine",1.38f,white,"WATER");
            var bins=Group(row,"WoodRecyclingStation");bins.localPosition=new Vector3(-1.65f,0,0);
            Part(bins,"Cabinet",new Vector3(0,.46f,-.33f),new Vector3(2.5f,.92f,.60f),wood);
            Part(bins,"Plinth",new Vector3(0,.055f,-.33f),new Vector3(2.44f,.11f,.57f),navy);
            Part(bins,"Countertop",new Vector3(0,.945f,-.34f),new Vector3(2.56f,.05f,.66f),wood);
            string[] labels={"GENERAL","PLASTIC","CANS","GLASS"};
            for(int i=0;i<4;i++)
            {
                float x=-.9375f+i*.625f;
                Part(bins,"Door"+i,new Vector3(x,.48f,-.637f),new Vector3(.60f,.80f,.025f),wood);
                Part(bins,"Handle"+i,new Vector3(x-.245f,.72f,-.659f),new Vector3(.025f,.13f,.018f),metal);
                Part(bins,"Opening"+i,new Vector3(x,.973f,-.34f),new Vector3(.27f,.003f,.27f),navy,PrimitiveType.Cylinder);
                Part(bins,"LabelPlate"+i,new Vector3(x,.79f,-.654f),new Vector3(.39f,.13f,.012f),navy);
                Label(bins,"Category"+i,labels[i],new Vector3(x,.79f,-.662f),.36f,.11f);
            }
            Collider(bins,new Vector3(0,.485f,-.35f),new Vector3(2.56f,.97f,.66f));
            ConvergenceHallMeasuredBluebell.ApplyTo(root);
        }
        static void Vending(Transform row,string name,float x,Material body,string title)
        {
            var t=Group(row,name);t.localPosition=new Vector3(x,0,0);
            Part(t,"Body",new Vector3(0,.94f,-.445f),new Vector3(1.05f,1.88f,.82f),body);
            Part(t,"Base",new Vector3(0,.055f,-.445f),new Vector3(1.07f,.11f,.84f),navy);
            Part(t,"Display",new Vector3(0,1.31f,-.863f),new Vector3(.91f,.94f,.025f),screen);
            for(int shelf=0;shelf<3;shelf++)
            {
                float y=.96f+shelf*.285f;
                Part(t,"Shelf"+shelf,new Vector3(0,y-.025f,-.892f),new Vector3(.90f,.025f,.07f),metal);
                for(int col=0;col<7;col++)
                {
                    float bx=-.37f+col*.123f;Material bottle=(col+shelf)%3==0?teal:(col+shelf)%3==1?white:navy;
                    Part(t,"Product"+shelf+"_"+col,new Vector3(bx,y+.105f,-.895f),new Vector3(.075f,.085f,.075f),bottle,PrimitiveType.Cylinder);
                    Part(t,"Cap"+shelf+"_"+col,new Vector3(bx,y+.20f,-.895f),new Vector3(.039f,.012f,.039f),white,PrimitiveType.Cylinder);
                    Part(t,"Select"+shelf+"_"+col,new Vector3(bx,y-.051f,-.902f),new Vector3(.062f,.018f,.012f),navy);
                }
            }
            Part(t,"BrandPanel",new Vector3(-.16f,.60f,-.867f),new Vector3(.58f,.39f,.025f),body==white?navy:teal);
            Label(t,"Brand",title,new Vector3(-.16f,.60f,-.884f),.53f,.16f);
            Part(t,"PaymentPanel",new Vector3(.32f,.61f,-.884f),new Vector3(.22f,.38f,.04f),navy);
            Part(t,"PaymentScreen",new Vector3(.32f,.71f,-.909f),new Vector3(.16f,.07f,.012f),screen);
            Part(t,"CardSlot",new Vector3(.32f,.59f,-.912f),new Vector3(.13f,.021f,.018f),metal);
            Part(t,"CollectionSlot",new Vector3(-.04f,.22f,-.874f),new Vector3(.81f,.19f,.045f),navy);
            Part(t,"CollectionLip",new Vector3(-.04f,.12f,-.912f),new Vector3(.84f,.025f,.07f),metal);
            Collider(t,new Vector3(0,.94f,-.475f),new Vector3(1.07f,1.88f,.90f));
        }
        [MenuItem("Tools/EEG Wheelchair/Update Bluebell Stand And Amenities")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ConvergenceHallWallMaterials.ApplyTo(root);Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();var scene=EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);EditorSceneManager.SaveScene(scene);EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            Validate(GameObject.Find("ConvergenceHallB1"));
        }
        static void Validate(GameObject root)
        {
            ConvergenceHallMeasuredBluebell.Validate(root);
            var stand=root.transform.Find("BluebellStand");var a=stand.Find("StandCollision").GetComponent<Renderer>().bounds;var b=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;
            if(Mathf.Abs(a.min.x-b.min.x)>.002f||Mathf.Abs(a.max.x-b.max.x)>.002f)throw new Exception("Stand/B112 boundaries differ.");
            foreach(var r in stand.GetComponentsInChildren<MeshRenderer>())if(r.bounds.min.x<b.min.x-.003f||r.bounds.max.x>b.max.x+.003f)throw new Exception("Step protrudes: "+r.name);
            var props=root.transform.Find("Landmarks/BluebellAmenities");if(props.childCount!=3||props.GetComponentsInChildren<BoxCollider>().Length!=3)throw new Exception("Expected two vending machines and one recycling station.");
            foreach(var c in props.GetComponentsInChildren<BoxCollider>())if(Mathf.Abs(c.bounds.min.y)>.002f||c.bounds.max.x>ConvergenceHallB1Setup.Plan(745,0).x-.58f)throw new Exception("Prop floor/route clearance: "+c.name);
            Debug.Log("BLUEBELL_OK: width="+a.size.x+"m, B112 boundaries match; two machines/four-bin wooden cabinet; floor and route clearance; no duplicate props.");
        }
        public static void ApplyAndValidate()
        {
            Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Repeated authoring duplicated objects.");
            ConvergenceHallB1Validation.BuildAndValidate();
        }
    }
}
