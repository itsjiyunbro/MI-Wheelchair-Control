using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallMeasuredUpdates
    {
        public static readonly string[][] Pairs={
            new[]{"B108_South_East","B107_South_West"},new[]{"B107_South_East","B106_South_West"},
            new[]{"B103_North_East","B102_North_West"},new[]{"B102_North_East","B101_North_West"}};
        static Renderer Leaf(Transform door)=>door.GetComponentsInChildren<Transform>().Single(t=>t.name=="Leaf").GetComponent<Renderer>();
        static void Validate(GameObject root)
        {
            for(int i=0;i<Pairs.Length;i++)
            {
                var left=Leaf(root.transform.Find("PlanDoors/"+Pairs[i][0]));var right=Leaf(root.transform.Find("PlanDoors/"+Pairs[i][1]));float gap=right.bounds.min.x-left.bounds.max.x,target=i<2?1.15f:.70f;
                if(Mathf.Abs(gap-target)>.002f)throw new Exception("Measured adjacent leaf gap mismatch: "+Pairs[i][0]+"/"+Pairs[i][1]+" "+gap);
                Debug.Log("MEASURED_LEAF_GAP: "+Pairs[i][0]+"/"+Pairs[i][1]+" = "+gap.ToString("F3")+"m");
            }
            foreach(string room in new[]{"B101","B102","B103"})
            {
                var windows=root.transform.Find("RoomsAndCores/"+room+"/North").GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial.name=="ClerestoryGlass").ToArray();
                if(windows.Length==0||windows.Any(r=>Mathf.Abs(r.bounds.size.y-1.15f)>.002f))throw new Exception("Measured 1.15m corridor window height mismatch: "+room);
            }
            var desk=AssetDatabase.LoadAssetAtPath<GameObject>(ConvergenceHallClassroomProps.Assets+"TwoSeatFoldingDesk.prefab");var size=desk.GetComponentInChildren<Renderer>().bounds.size;
            if(Mathf.Abs(size.x-1.5f)>.002f||Mathf.Abs(size.z-.55f)>.002f)throw new Exception("Desk visual must measure 150x55cm.");
            Debug.Log("MEASURED_UPDATES_OK: corridor glass height 1.15m, desk visual 1.50x0.55m, adjacent leaf gaps 1.15m / 0.70m.");
        }
        public static void ApplyAndCapture()
        {
            ConvergenceHallClassroomInteriors.Apply();Validate(GameObject.Find("ConvergenceHallB1"));int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            ConvergenceHallClassroomInteriors.Apply();Validate(GameObject.Find("ConvergenceHallB1"));if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Measured update duplicated objects.");
            SessionState.SetBool("EEG.ConvergenceQA.FocusMeasuredUpdates",true);SessionState.SetString("EEG.ConvergenceQA.FocusRoom","B106");SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
