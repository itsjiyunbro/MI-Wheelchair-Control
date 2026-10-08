using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallCoreSigns
    {
        public static readonly string[] Halls={"B112ElevatorHall","B109ElevatorHall","B112Stairwell","B109Stairwell"};
        const float Size=.26f;
        static void Label(Transform parent,string name,string content,Vector3 at,Vector2 size,int pixels)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(Text));go.transform.SetParent(parent,false);
            go.transform.localPosition=at;go.transform.localScale=Vector3.one*.001f;
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)go.transform).sizeDelta=size*1000;
            var t=go.GetComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");
            t.text=content;t.fontSize=pixels;t.resizeTextForBestFit=true;t.resizeTextMinSize=10;t.resizeTextMaxSize=pixels;t.color=new Color(.94f,.94f,.90f);t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;
        }
        static void Braille(Transform parent,string content,Material silver)
        {
            int[] cells={1,3,9,25,17,11,27,19,10,26,5,7,13,29,21,15,31,23,14,30,37,39,58,45,61,53};
            float spacing=.0095f,start=-.072f;
            for(int i=0;i<content.Length;i++)
            {
                char letter=content[i];if(letter==' ')continue;int bits=cells[letter-'a'];
                for(int dot=0;dot<6;dot++)if((bits&(1<<dot))!=0)
                {
                    var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="BrailleDot"+i+"_"+dot;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                    go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(start+i*spacing+(dot/3)*.0032f,-.105f-(dot%3)*.0032f,-.0095f);go.transform.localScale=new Vector3(.0022f,.0022f,.0014f);go.GetComponent<Renderer>().sharedMaterial=silver;
                }
            }
        }
        public static void ApplyTo(GameObject root)
        {
            var navy=P.Mat("CoreSignNavy",new Color(.012f,.022f,.060f),.18f);
            var white=P.Mat("CoreSignWhite",new Color(.87f,.88f,.87f),.22f);
            var gold=P.Mat("CoreSignDivider",new Color(.52f,.48f,.29f),.3f);
            var silver=P.Mat("CoreSignBrailleMetal",new Color(.59f,.61f,.58f),.48f);
            for(int i=0;i<Halls.Length;i++)
            {
                var hall=root.transform.Find("CoreHalls/"+Halls[i]);bool lift=i<2;
                string oldName=lift?"HallSign":i==2?"DoorSign":"StairSign";var old=hall.Find(oldName);
                var position=old.localPosition;position.z=i==2?-.0525f:-.0045f;
                old.gameObject.SetActive(false);hall.Find(oldName+"Backing").gameObject.SetActive(false);
                var previous=hall.Find("ReferencePlacard");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var sign=P.Group(hall,"ReferencePlacard");sign.localPosition=position;
                P.Box(sign,"ThinBacking",Vector3.zero,new Vector3(Size,Size,.009f),white);
                P.Box(sign,"BlankWhiteHeader",new Vector3(0,.0975f,-.0049f),new Vector3(Size,.065f,.001f),white);
                P.Box(sign,"NavyBody",new Vector3(0,-.0325f,-.0049f),new Vector3(Size,.195f,.001f),navy);
                Label(sign,"Title",lift?"Elevator Hall":"계단실",new Vector3(0,-.038f,-.006f),new Vector2(.224f,.046f),lift?27:33);
                P.Box(sign,"GoldDivider",new Vector3(.009f,-.070f,-.006f),new Vector3(.242f,.0011f,.001f),gold);
                Label(sign,"Subtitle",lift?"Elevator Hall":"Stair",new Vector3(0,-.084f,-.006f),new Vector2(.224f,.023f),17);
                P.Box(sign,"BraillePlate",new Vector3(-.034f,-.113f,-.0069f),new Vector3(.155f,.020f,.003f),silver);
                Braille(sign,lift?"elevator hall":"stair",silver);
            }
        }
        public static void Validate(GameObject root)
        {
            for(int i=0;i<Halls.Length;i++)
            {
                var hall=root.transform.Find("CoreHalls/"+Halls[i]);var sign=hall.Find("ReferencePlacard");
                if(!sign||hall.Cast<Transform>().Count(t=>t.name=="ReferencePlacard")!=1)throw new Exception("Missing/duplicate core placard "+Halls[i]);
                if(sign.Find("Title").GetComponent<Text>().text!=(i<2?"Elevator Hall":"계단실")||sign.Find("Subtitle").GetComponent<Text>().text!=(i<2?"Elevator Hall":"Stair"))throw new Exception("Core sign wording mismatch");
                if(sign.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Sign must not change collisions");
                foreach(var r in sign.GetComponentsInChildren<Renderer>())if(r.sharedMaterial&&r.sharedMaterial.HasProperty("_BaseMap")&&r.sharedMaterial.GetTexture("_BaseMap"))throw new Exception("Photo texture on native core sign");
                string old=i<2?"HallSign":i==2?"DoorSign":"StairSign";
                if(hall.Find(old).gameObject.activeSelf||hall.Find(old+"Backing").gameObject.activeSelf)throw new Exception("Legacy sign still visible");
                if(Mathf.Abs(sign.Find("ThinBacking").localScale.z-.009f)>.0001f||sign.GetComponentsInChildren<Text>().Length!=2)throw new Exception("Core sign profile mismatch");
            }
            Debug.Log("CORE_SIGNS_OK: four native thin square placards; blank white header, navy body, correct titles/subtitles, divider and modeled braille; no photos or colliders.");
        }
        public static void ApplyAndCapture()
        {
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));
            int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));
            if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Core sign authoring duplicate");
            SessionState.SetBool("EEG.ConvergenceQA.FocusCoreSigns",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
