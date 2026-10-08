using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallLecternLighting
    {
        const string ProfilePath="Assets/Settings/ConvergenceHallNoGlowProfile.asset";
        public static void DisableEmission(GameObject root)
        {
            foreach(var m in root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.HasProperty("_EmissionColor")).Distinct())
            {m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;EditorUtility.SetDirty(m);}
        }
        static void SceneNoBloom()
        {
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            var volumes=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            if(!profile)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="ConvergenceHallNoGlowProfile";AssetDatabase.CreateAsset(profile,ProfilePath);
                var source=volumes.Select(v=>v.sharedProfile).FirstOrDefault(p=>p);
                if(source)foreach(var component in source.components){var copy=UnityEngine.Object.Instantiate(component);copy.name=component.name;profile.components.Add(copy);AssetDatabase.AddObjectToAsset(copy,profile);}
            }
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=ScriptableObject.CreateInstance<Bloom>();bloom.name="Bloom";profile.components.Add(bloom);AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.active=true;bloom.intensity.Override(0);EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(profile);
            foreach(var volume in volumes){volume.sharedProfile=profile;EditorUtility.SetDirty(volume);}
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        public static void Validate(GameObject root)
        {
            var rooms=root.transform.Find("ClassroomInteriors");if(rooms.GetComponentsInChildren<Light>(true).Length>0)throw new Exception("Central classroom light remains");
            foreach(var m in root.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct())
                if(m.IsKeywordEnabled("_EMISSION")||m.HasProperty("_EmissionColor")&&m.GetColor("_EmissionColor").maxColorComponent>.0001f)throw new Exception("Visible model emission remains: "+m.name);
            foreach(var volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))if(volume.sharedProfile&&volume.sharedProfile.TryGet<Bloom>(out var bloom)&&bloom.intensity.value>0)throw new Exception("Bloom remains");
            var official=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Meshes/ConvergenceHall/YonseiOfficialSymbol.asset");if(!official||official.vertexCount<10000)throw new Exception("Official detailed symbol geometry missing");
            foreach(string name in ConvergenceHallClassroomInteriors.Rooms)
            {
                var room=rooms.Find(name);var merged=room.Find("CombinedVisual").GetComponent<Renderer>();
                int blue=Array.FindIndex(merged.sharedMaterials,m=>m.name=="YonseiSymbolBlue");if(blue<0||merged.GetComponent<MeshFilter>().sharedMesh.GetTriangles(blue).Length!=official.triangles.Length)throw new Exception("Room lectern does not contain the official symbol: "+name);
                if(room.Find("Seating").Cast<Transform>().Where(t=>t.name.StartsWith("Desk_")||t.name.StartsWith("Chair_")).Any(t=>Mathf.Abs(t.localScale.x-.95f)>.001f))throw new Exception("Furniture resize reverted");
            }
            ConvergenceHallWallSurfaceCleanup.Validate(root);
            Debug.Log("LECTERN_NO_GLOW_OK: 12 lecterns contain detailed official Yonsei symbol geometry; classroom fill lights removed, all model emission off, B1 bloom intensity zero; 95% furniture and wall cleanup retained.");
        }
        public static void ApplyAndCapture()
        {
            ConvergenceHallClassroomInteriors.Apply();SceneNoBloom();
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);Validate(GameObject.Find("ConvergenceHallB1"));
            SessionState.SetBool("EEG.ConvergenceQA.FocusLecternNoGlow",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
