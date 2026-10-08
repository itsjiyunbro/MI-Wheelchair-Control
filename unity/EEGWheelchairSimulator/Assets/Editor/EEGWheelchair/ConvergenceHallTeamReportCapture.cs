using UnityEditor;
using UnityEditor.SceneManagement;
namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallTeamReportCapture
    {
        public static void CaptureWest()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.FocusWestLiftPhoto",true);
            SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);
            ConvergenceHallB1Play.Begin();
        }
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.FocusB112Presentation",true);
            SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);
            ConvergenceHallB1Play.Begin();
        }
    }
}
