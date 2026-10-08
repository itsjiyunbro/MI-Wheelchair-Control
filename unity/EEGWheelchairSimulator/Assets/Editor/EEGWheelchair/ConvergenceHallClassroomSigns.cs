using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallClassroomSigns
    {
        const string FontPath="Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf";
        static Font font;
        static Material white,navy,metal;
        static Transform Group(Transform p,string name)
        {var t=p.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(p,false);}return t;}
        static void Box(Transform p,string name,Vector3 at,Vector3 size,Material material)
        {
            var t=p.Find(name);if(!t){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());t=go.transform;t.SetParent(p,false);}
            t.localPosition=at;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=material;
        }
        static void Text(Transform p,string name,string value,Vector3 at,Vector2 size,int px,Color color,TextAnchor align)
        {
            var t=p.Find(name);if(!t){var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(Text));t=go.transform;t.SetParent(p,false);go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;}
            t.localPosition=at;t.localRotation=Quaternion.identity;t.localScale=Vector3.one*.001f;((RectTransform)t).sizeDelta=size*1000;
            var text=t.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=px;text.color=color;text.alignment=align;text.raycastTarget=false;
            text.resizeTextForBestFit=true;text.resizeTextMinSize=8;text.resizeTextMaxSize=px;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
        }
        static void Names(string room,out string korean,out string english)
        {
            korean="SLI 첨단강의실";english="SLI Class Room";
            if(room=="B104"){korean="RC프로그램활동실";english="RC Program Activity Room";}
            if(room=="B111"){korean="학생휴게실";english="Student Lounge";}
            if(room=="B112"||room=="B113"||room=="B114"||room=="B115"){korean="SU 첨단강의실";english="SU Class Room";}
            if(room=="B124"){korean="상담실라운지";english="Consulting Lounge";}
        }
        static void Plaque(Transform p,string room,Vector3 at)
        {
            Names(room,out var korean,out var english);var sign=Group(p,"PhotoRoomSign");sign.localPosition=at;sign.localRotation=Quaternion.identity;sign.localScale=Vector3.one*.65f;
            Box(sign,"NavyBody",Vector3.zero,new Vector3(.20f,.20f,.012f),navy);
            Box(sign,"WhiteHeader",new Vector3(0,.075f,-.007f),new Vector3(.20f,.05f,.003f),white);
            Text(sign,"RoomNumber",room,new Vector3(.002f,.075f,-.010f),new Vector2(.182f,.047f),43,Color.black,TextAnchor.MiddleRight);
            Text(sign,"KoreanName",korean,new Vector3(0,-.025f,-.010f),new Vector2(.180f,.039f),22,Color.white,TextAnchor.MiddleLeft);
            Box(sign,"Divider",new Vector3(0,-.049f,-.009f),new Vector3(.180f,.001f,.002f),metal);
            Text(sign,"EnglishName",english,new Vector3(0,-.063f,-.010f),new Vector2(.180f,.022f),14,Color.white,TextAnchor.MiddleLeft);
            Box(sign,"BraillePlate",new Vector3(-.022f,-.085f,-.010f),new Vector3(.136f,.016f,.003f),metal);
            // Standard uncontracted English braille: capital B, number sign, then room digits.
            int[] digits={26,1,3,9,25,17,11,27,19,10}; // bit 0..5 = braille dots 1..6
            var codes=new[]{32,3,60}.Concat(room.Substring(1).Select(c=>digits[c-'0'])).ToArray();
            var dots=Group(sign,"BrailleRoomNumber");foreach(Transform old in dots.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            for(int i=0;i<codes.Length;i++)for(int d=0;d<6;d++)if((codes[i]&(1<<d))!=0)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Cell"+i+"Dot"+(d+1);UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());var t=go.transform;t.SetParent(dots,false);
                t.localPosition=new Vector3(-.079f+i*.018f+(d/3)*.003f,-.082f-(d%3)*.003f,-.012f);t.localScale=Vector3.one*.0018f;t.GetComponent<Renderer>().sharedMaterial=metal;
            }
        }
        public static void ApplyTo(GameObject root)
        {
            font=AssetDatabase.LoadAssetAtPath<Font>(FontPath);if(!font)throw new Exception("Korean sign font missing.");
            const string mats="Assets/Art/Materials/ConvergenceHall/";white=AssetDatabase.LoadAssetAtPath<Material>(mats+"WarmWhite.mat");navy=AssetDatabase.LoadAssetAtPath<Material>(mats+"Navy.mat");metal=AssetDatabase.LoadAssetAtPath<Material>(mats+"Metal.mat");
            white=AssetDatabase.LoadAssetAtPath<Material>(mats+"RoomSignWhite.mat");
            if(!white){white=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="RoomSignWhite"};white.SetColor("_BaseColor",new Color(.94f,.945f,.94f));white.SetFloat("_Smoothness",.45f);AssetDatabase.CreateAsset(white,mats+"RoomSignWhite.mat");}
            foreach(Transform door in root.transform.Find("PlanDoors"))
            {
                string room=door.name.Split('_')[0];if(!room.StartsWith("B1"))continue;if(room=="B124Wing")room="B124";
                var plate=door.Find("RoomPlate");float x=door.Find("OpaqueTransom")?plate.localPosition.x:-1.18f;
                foreach(string name in new[]{"RoomPlate","RoomPlateHeader","RoomLabel"}){var old=door.Find(name);if(old)old.gameObject.SetActive(false);}
                Plaque(door,room,new Vector3(x,1.49f,-.0565f));
            }
            var facade=root.transform.Find("DoorsAndSigns/B111Entrance");var backing=facade.Find("RoomNumberBacking");
            Plaque(facade,"B111",new Vector3(backing.localPosition.x,1.49f,-.061f));backing.gameObject.SetActive(false);facade.Find("B111Number").gameObject.SetActive(false);
        }
        static void Validate(GameObject root)
        {
            var signs=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="PhotoRoomSign").ToArray();if(signs.Length<20)throw new Exception("Classroom signs missing.");
            foreach(var sign in signs)
            {
                string room=sign.Find("RoomNumber").GetComponent<Text>().text;Names(room,out var ko,out var en);
                if(sign.Find("KoreanName").GetComponent<Text>().text!=ko||sign.Find("EnglishName").GetComponent<Text>().text!=en)throw new Exception("Room sign contents mismatch.");
                font.RequestCharactersInTexture(ko,22,FontStyle.Normal);if(ko.Any(c=>!char.IsWhiteSpace(c)&&!font.HasCharacter(c)))throw new Exception("Korean glyph missing.");
                if(sign.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Sign changed collision.");
                if(sign.Find("NavyBody").TransformVector(Vector3.right).magnitude>.1401f)throw new Exception("Room sign extends beyond the jamb width.");
            }
            Debug.Log("PHOTO_ROOM_SIGNS_OK: "+signs.Length+" white number headers/navy Korean-English signs, Korean font and room content validated.");
        }
        public static void Apply()
        {
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ConvergenceHallWallMaterials.ApplyTo(root);Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
        }
        public static void ApplyAndValidate()
        {Apply();int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;Apply();if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Sign update duplicated geometry.");ConvergenceHallB1Validation.BuildAndValidate();}
    }
}
