using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // Asset-only polish. Named additions are reused; existing course geometry and runtime code are preserved.
    public static class IndoorTestCoursePolish
    {
        const string ScenePath = "Assets/Scenes/MainScene.unity";
        const string PrefabPath = "Assets/Art/Prefabs/Environment/IndoorTestCourse.prefab";
        const string Materials = "Assets/Art/Materials/Environment/";
        static Material floor, wall, door, accent, start, goal, band, route;

        [MenuItem("Tools/EEG Wheelchair/Polish Indoor Test Course")]
        public static void Polish()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
            var instance = scene.GetRootGameObjects().Single(g => g.name == "IndoorTestCourse");
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance) != PrefabPath)
                throw new InvalidOperationException("Expected the existing course prefab instance; nothing rebuilt.");
            floor = Mat("Floor", new Color(.56f,.61f,.63f), .30f);
            wall = Mat("Wall", new Color(.84f,.86f,.86f), .12f);
            door = Mat("Door", new Color(.19f,.28f,.33f), .22f);
            accent = Mat("Accent", new Color(.075f,.22f,.24f), .18f);
            start = Mat("Start", new Color(.12f,.43f,.29f), .18f);
            goal = Mat("Goal", new Color(.78f,.35f,.09f), .18f);
            band = Mat("WallAccent", new Color(.43f,.49f,.49f), .18f);
            route = Mat("RouteGuide", new Color(.065f,.35f,.34f), .20f);
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { Decorate(root.transform); PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var light = scene.GetRootGameObjects().Single(g => g.name == "Directional Light").GetComponent<Light>();
            Undo.RecordObject(light, "Polish course light");
            light.color = new Color(1f,.99f,.98f); light.intensity = 1.65f; light.shadowStrength = .65f;
            // Retain the existing light direction, camera settings, URP pipeline and Global Volume.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.55f,.60f,.64f);
            RenderSettings.ambientEquatorColor = new Color(.42f,.46f,.50f);
            RenderSettings.ambientGroundColor = new Color(.29f,.32f,.35f);
            EditorUtility.SetDirty(light); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
            Debug.Log("POLISH_SAVED: existing prefab updated, 8 shared materials; runtime components and course transforms retained.");
        }

        static Material Mat(string name, Color color, float smooth)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(Materials + name + ".mat");
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name = name; AssetDatabase.CreateAsset(m, Materials + name + ".mat"); }
            m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", 0); m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m); return m;
        }
        static Transform Group(Transform parent, string name)
        {
            var t = parent.Find(name); if (t != null) return t;
            t = new GameObject(name).transform; t.SetParent(parent, false); return t;
        }
        static Transform Box(Transform parent, string name, Vector3 p, Vector3 s, Material m, Quaternion? rotation = null)
        {
            var t = parent.Find(name);
            if (t == null) { var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; t = go.transform; t.SetParent(parent, false); UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); }
            t.localPosition = p; t.localScale = s; t.localRotation = rotation ?? Quaternion.identity;
            t.GetComponent<Renderer>().sharedMaterial = m; return t;
        }
        static void Label(Transform parent, string name, string value, Vector3 p, Quaternion r, float width, float height, int fontSize = 80)
        {
            var t = parent.Find(name);
            if (t == null) { var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(Text)); t = go.transform; t.SetParent(parent, false); }
            t.localPosition = p; t.localRotation = r; t.localScale = Vector3.one * .0025f;
            ((RectTransform)t).sizeDelta = new Vector2(width / .0025f, height / .0025f);
            t.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var text = t.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value; text.fontSize = fontSize; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white; text.raycastTarget = false;
        }
        static void Decorate(Transform root)
        {
            var owned = Group(root, "VisualPolish");
            var floors = Group(owned, "FloorDetails");
            foreach (Transform tile in root.Find("Floors"))
            {
                var p = tile.localPosition; var s = tile.localScale; int n = 0;
                for (float x = Mathf.Ceil((p.x-s.x/2)/2)*2; x < p.x+s.x/2-.02f; x += 2)
                    Box(floors, tile.name+"_X"+n++, new Vector3(x,.0095f,p.z), new Vector3(.012f,.001f,s.z), band);
                n = 0;
                for (float z = Mathf.Ceil((p.z-s.z/2)/2)*2; z < p.z+s.z/2-.02f; z += 2)
                    Box(floors, tile.name+"_Z"+n++, new Vector3(p.x,.0095f,z), new Vector3(s.x,.001f,.012f), band);
            }
            Box(floors,"EntryGuide",new Vector3(0,.012f,5.05f),new Vector3(.065f,.002f,3.9f),route);
            Box(floors,"CrossGuide",new Vector3(-4.5f,.012f,7),new Vector3(9,.002f,.065f),route);
            Box(floors,"ExitGuide",new Vector3(-9,.012f,10.5f),new Vector3(.065f,.002f,7),route);
            foreach (Transform arrow in root.Find("Signs"))
                if (arrow.name == "RouteArrow") foreach (var renderer in arrow.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = route;
            var walls = Group(owned,"WallDetails");
            foreach (Transform existing in root.Find("Walls"))
            {
                if (!existing.name.StartsWith("Wall_")) continue;
                var p = existing.localPosition; var s = existing.localScale; bool vertical = s.x < s.z;
                p.y = .38f; s.y = .56f; if (vertical) s.x = .215f; else s.z = .215f;
                Box(walls,existing.name+"_Lower",p,s,band);
                p.y = .72f; s.y = .035f; if (vertical) s.x = .22f; else s.z = .22f;
                Box(walls,existing.name+"_Stripe",p,s,route);
            }
            var doors = Group(owned,"DoorDetails");
            int index = 1;
            foreach (Transform room in root.Find("Doors"))
            {
                var panel = room.Find("Panel"); if (panel == null) continue;
                var p = panel.localPosition; float inward = p.x < 0 ? 1 : -1;
                Box(doors,room.name+"_KickPlate",new Vector3(p.x+inward*.025f,.25f,p.z),new Vector3(.012f,.28f,1.05f),band);
                Quaternion r = Quaternion.Euler(0, inward < 0 ? 90 : -90,0);
                Vector3 center = new Vector3(p.x+inward*.029f,1.7f,p.z);
                Box(doors,room.name+"_Plaque",center,new Vector3(.55f,.27f,.015f),accent,r);
                Label(doors,room.name+"_Number","R-10"+index++,center+r*Vector3.back*.013f,r,.5f,.22f,64);
            }
            var signs = root.Find("Signs");
            UpgradeSign(signs, owned,"Left", "LEFT TURN", "01  /  REHABILITATION", accent);
            UpgradeSign(signs, owned,"Right", "RIGHT TURN", "02  /  TEST CORRIDOR", accent);
            UpgradeSign(signs, owned,"Goal", "GOAL", "END OF TEST COURSE", goal);
            var startDetails = Group(owned,"StartDetails");
            Quaternion sr = Quaternion.Euler(0,90,0); Vector3 sp = new Vector3(2.465f,1.6f,1.9f);
            Box(startDetails,"StartBoard",sp,new Vector3(1.7f,.8f,.03f),start,sr);
            Label(startDetails,"Title","START",sp+sr*new Vector3(0,.12f,-.02f),sr,1.5f,.3f,100);
            Label(startDetails,"Subtitle","EEG WHEELCHAIR TEST",sp+sr*new Vector3(0,-.18f,-.02f),sr,1.55f,.22f,44);
            Pad(root.Find("StartArea/StartPad"), startDetails,start);
            Pad(root.Find("GoalArea/GoalPad"),Group(owned,"GoalDetails"),goal);
            var props = Group(owned,"PropDetails");
            root.Find("Props/BenchBack").GetComponent<Renderer>().sharedMaterial = band;
            root.Find("Props/PlanterBase").GetComponent<Renderer>().sharedMaterial = band;
            for (int i=0;i<3;i++) Box(props,"BenchSeatJoin"+i,new Vector3(3.25f,.542f,4.65f+i*.35f),new Vector3(.52f,.004f,.016f),accent);
            // Retain the original plant object; add compact leaf tiers inside its existing corner footprint.
            var plant = root.Find("Props/Plant"); plant.GetComponent<Renderer>().enabled = false;
            for(int i=0;i<3;i++) Box(props,"LeafTier"+i,new Vector3(-12.3f,.73f+i*.20f,9.8f),new Vector3(.58f-i*.10f,.25f,.58f-i*.10f),start,Quaternion.Euler(0,25+i*25,0));
        }
        static void Pad(Transform pad, Transform parent, Material m)
        {
            pad.GetComponent<Renderer>().sharedMaterial = floor;
            var p = pad.localPosition; var s = pad.localScale;
            for (int side=-1;side<=1;side+=2)
            {
                Box(parent,"BorderX"+side,new Vector3(p.x+side*(s.x/2-.06f),.023f,p.z),new Vector3(.12f,.006f,s.z),m);
                Box(parent,"BorderZ"+side,new Vector3(p.x,.023f,p.z+side*(s.z/2-.06f)),new Vector3(s.x,.006f,.12f),m);
            }
        }
        static void UpgradeSign(Transform signs, Transform owned, string prefix, string title, string subtitle, Material m)
        {
            var board = signs.Find(prefix+"SignBoard"); var text = signs.Find(prefix+"SignText");
            board.localScale = new Vector3(2.8f,.85f,.035f); board.GetComponent<Renderer>().sharedMaterial=m;
            var p=board.localPosition; var r=board.localRotation;
            Label(signs,text.name,title,p+r*new Vector3(0,.12f,-.025f),r,2.55f,.36f,104);
            Label(Group(owned,"SignDetails"),prefix+"Subtitle",subtitle,p+r*new Vector3(0,-.22f,-.025f),r,2.55f,.22f,48);
        }
        static string Arg(string name) { var a=Environment.GetCommandLineArgs(); int i=Array.IndexOf(a,name); return i<0?null:a[i+1]; }
        static void RenderRoute()
        {
            var root=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.parent==null&&t.name=="IndoorTestCourse");
            typeof(IndoorTestCourseSetup).GetMethod("CheckRouteAndPreview",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.gameObject});
        }
        public static void PolishAndValidate()
        {
            EditorSceneManager.OpenScene(ScenePath);
            string before=Arg("-beforePreviewFolder"), after=Arg("-coursePreviewFolder");
            if(before!=null && !Directory.Exists(before))
            {
                RenderRoute(); Directory.CreateDirectory(before);
                foreach(string path in Directory.GetFiles(after,"*.png")) File.Copy(path,Path.Combine(before,Path.GetFileName(path)),true);
            }
            Polish();
            string first=File.ReadAllText(PrefabPath); string scene=File.ReadAllText(ScenePath);
            Polish();
            if(first!=File.ReadAllText(PrefabPath)||scene!=File.ReadAllText(ScenePath)) throw new InvalidOperationException("Polish is not idempotent.");
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("POLISH_IDEMPOTENCE_OK: repeated polish and scene reload preserve identical serialized assets.");
            IndoorTestCourseSetup.ConfigureAndValidate();
        }
    }
}

