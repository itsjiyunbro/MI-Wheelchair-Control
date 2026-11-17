using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Hlp = EEGWheelchairSimulator.Editor.ConvergenceHallUpperFloorHelpers;

namespace EEGWheelchairSimulator.Editor
{
    [InitializeOnLoad]
    public static class ConvergenceHallUpperFloorsValidation
    {
        static IEnumerator checks;
        static int frame = -1;
        static string error;
        static double deadline;

        static ConvergenceHallUpperFloorsValidation()
        {
            if (SessionState.GetBool("UpperFloors.Checking", false))
                EditorApplication.update += Tick;
            if (Application.isBatchMode && Environment.GetCommandLineArgs().Any(a => a.Contains("ConvergenceHallUpperFloors")))
                EditorApplication.update += SkipSearch;
        }

        static void SkipSearch()
        {
            var pending = EditorApplication.delayCall;
            if (pending == null) return;
            foreach (var c in pending.GetInvocationList())
                if (c.Method.DeclaringType?.FullName == "UnityEditor.Search.SearchInit" && c.Method.Name == "IndexationOnStartup")
                    EditorApplication.delayCall -= (EditorApplication.CallbackFunction)c;
        }

        public static void CaptureAndCheck()
        {
            Hlp.LoadMaterials();
            BeginChecks();
        }

        public static void BeginChecks()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("UpperFloors.Checking", true);
            SessionState.SetBool("UpperFloors.Entered", false);
            checks = null;
            error = null;
            deadline = EditorApplication.timeSinceStartup + 240;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                {
                    if (SessionState.GetBool("UpperFloors.Entered", false)) Finish(null);
                    return;
                }
                SessionState.SetBool("UpperFloors.Entered", true);
                if (checks == null)
                {
                    deadline = EditorApplication.timeSinceStartup + 240;
                    Application.logMessageReceived += OnLog;
                    checks = Run();
                }
                if (EditorApplication.timeSinceStartup > deadline)
                    throw new TimeoutException("Upper floors validation timed out");
                if (Time.frameCount == frame) return;
                frame = Time.frameCount;
                if (!checks.MoveNext())
                {
                    if (error != null) throw new Exception(error);
                    Finish(null);
                }
            }
            catch (Exception ex) { Finish(ex); }
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error) error = message;
        }

        static void Finish(Exception ex)
        {
            SessionState.SetBool("UpperFloors.Checking", false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            if (ex != null) Debug.LogException(ex);
            else Debug.Log("UPPER_FLOORS_PLAY_OK");
            EditorApplication.ExitPlaymode();
            if (Application.isBatchMode) EditorApplication.Exit(ex == null ? 0 : 1);
        }

        static void Require(bool cond, string msg) { if (!cond) throw new Exception("UPPER_FLOORS: " + msg); }

        static IEnumerator Run()
        {
            for (int i = 0; i < 8; i++) yield return null;
            var b1 = GameObject.Find("ConvergenceHallB1");
            var f1 = GameObject.Find("ConvergenceHall1F");
            var f2 = GameObject.Find("ConvergenceHall2F");
            var f3 = GameObject.Find("ConvergenceHall3F");
            Require(b1 && f1 && f2 && f3, "Missing one of B1/1F/2F/3F root objects");

            ValidateFloorGeometry(f2, new[] { "202", "203", "203-1", "204", "205", "206", "207", "208", "209", "210", "212", "213", "214", "215", "216", "217", "218", "219" }, "2F");
            ValidateFloorGeometry(f3, new[] { "301", "302", "303", "304", "305", "306", "307", "308", "309", "310", "311", "312", "313", "314", "315", "316", "318", "319", "320", "321", "322", "323" }, "3F");
            Require(f2.transform.Find("Details/Containers201/201_Gray"), "Missing 201 container");

            var sim = UnityEngine.Object.FindFirstObjectByType<SimulationController>();
            var chair = UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var guard = chair.GetComponent<WheelchairCollisionGuard>();
            var nav = UnityEngine.Object.FindFirstObjectByType<ConvergenceFloorNavigation>();

            Require(chair.transform.position.y < 1f && !sim.IsRunning, "Default start must be on B1");

            // Check 2F & 3F navigation shortcuts & drive clearance
            nav.GoToSecondFloor();
            Require(Mathf.Abs(chair.transform.position.y - 9.5f) < .2f && !sim.IsRunning, "2F shortcut failed");
            sim.SetControlSource(WheelchairControlSource.Keyboard);
            sim.StartSimulation();
            Vector3 begin2 = chair.transform.position;
            for (int i = 0; i < 30; i++) chair.ApplyDirectionalInput(1, 0, 1f / 30f, WheelchairControlSource.Keyboard);
            Require(Vector3.Distance(begin2, chair.transform.position) > .4f, "2F wheelchair movement blocked");
            sim.StopSimulation();

            nav.GoToThirdFloor();
            Require(Mathf.Abs(chair.transform.position.y - 14.0f) < .2f && !sim.IsRunning, "3F shortcut failed");
            sim.StartSimulation();
            Vector3 begin3 = chair.transform.position;
            for (int i = 0; i < 30; i++) chair.ApplyDirectionalInput(1, 0, 1f / 30f, WheelchairControlSource.Keyboard);
            Require(Vector3.Distance(begin3, chair.transform.position) > .4f, "3F wheelchair movement blocked");
            sim.StopSimulation();

            sim.ResetSimulation();
            Require(chair.transform.position.y < 1f && !sim.IsRunning, "RESET did not return to B1");

            // Sample corridor routes on 2F & 3F with wheelchair footprint
            var routes2 = new[]
            {
                new[] { new Vector2(20.1f, 27f), new Vector2(20.1f, 47f) },   // 201 container & 209 corridor
                new[] { new Vector2(21.4f, 49.5f), new Vector2(21.4f, 58.2f) }, // West corridor past Open2F3FStair & 204
                new[] { new Vector2(23.5f, 54.2f), new Vector2(39f, 54.2f) },   // 205-208 graduate corridor
                new[] { new Vector2(21f, 2f), new Vector2(21f, 26f) },        // East corridor 210/212-214
            };
            CheckRoutes(f2.transform, routes2, guard, "2F");

            var routes3 = new[]
            {
                new[] { new Vector2(14.2f, 26.8f), new Vector2(14.2f, 58.2f) }, // Main 3F corridor
                new[] { new Vector2(7.0f, 27.0f), new Vector2(14.2f, 27.0f) },  // EastLift/Stair lobby cross-corridor
                new[] { new Vector2(7.0f, 16.5f), new Vector2(7.0f, 27.0f) },   // North of EastLift to 319-322 Brick Portal
                new[] { new Vector2(18f, 59f), new Vector2(34.5f, 59f) },       // 306-312 corridor
                new[] { new Vector2(7.5f, 2f), new Vector2(7.5f, 17f) },        // 319-322 East corridor
            };
            CheckRoutes(f3.transform, routes3, guard, "3F");

            // Verify all doors on 2F & 3F open/close at 30/120fps and stay isolated per floor
            foreach (var floor in new[] { f2, f3 })
            {
                foreach (var door in floor.GetComponentsInChildren<InteractiveDoor>())
                {
                    foreach (var m in door.Motions)
                        Require(m.target && m.target.IsChildOf(floor.transform), "Door has external moving part: " + door.Label);
                    foreach (int fps in new[] { 30, 120 })
                    {
                        door.SetProgressInstant(0);
                        door.SetOpen(true);
                        for (int i = 0; i < fps; i++) door.Step(1f / fps);
                        Require(door.Progress > .99f, "Door failed to open: " + door.Label);
                        door.SetOpen(false);
                        for (int i = 0; i < fps; i++) door.Step(1f / fps);
                        Require(door.Progress < .01f, "Door failed to close: " + door.Label);
                    }
                    door.ResetDoor();
                }
            }

            // 4-floor elevator validation on BOTH EastLift & WestLift
            ValidateFourFloorElevators(sim, chair, guard, nav);

            // StairWalkRoute validation on ALL upper stairs (1F<->2F, 2F<->3F, Open2F3FStair)
            ValidateUpperStairs(f2, f3);

            // Capture verification screenshots
            var args = Environment.GetCommandLineArgs();
            int output = Array.IndexOf(args, "-upperFloorCaptures");
            string dir = output >= 0 && output + 1 < args.Length ? args[output + 1] : Path.Combine(Directory.GetCurrentDirectory(), "UpperFloorCaptures");
            Directory.CreateDirectory(dir);
            var cap = CaptureScreenshots(f2, f3, dir);
            while (cap.MoveNext()) yield return cap.Current;

            sim.ResetSimulation();
            Debug.Log("UPPER_FLOORS_VALIDATION_OK: 2F & 3F geometry, corridor clearance, 4-floor elevators (B1<->1F<->2F<->3F), upper stairs, and captures passed.");
        }

        static void ValidateFloorGeometry(GameObject floor, string[] expectedRooms, string tag)
        {
            var a = floor.transform.Find("RoomsAndCores");
            foreach (string code in expectedRooms)
                Require(a.Find("Room" + code), $"{tag} missing Room{code}");
            foreach (var r in floor.GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials)
                    Require(m && !m.IsKeywordEnabled("_EMISSION"), $"{tag} missing/emissive material on {r.name}");
            Require(floor.GetComponentsInChildren<Light>().Length == 0, $"{tag} unexpected decorative lights");
            Debug.Log($"{tag}_GEOMETRY_OK: {expectedRooms.Length} rooms verified.");
        }

        static void CheckRoutes(Transform floor, Vector2[][] routes, WheelchairCollisionGuard guard, string tag)
        {
            int samples = 0;
            foreach (var route in routes)
            {
                var a = Hlp.World(floor, route[0].x, route[0].y, .5f);
                var b = Hlp.World(floor, route[1].x, route[1].y, .5f);
                var rot = Quaternion.LookRotation(b - a);
                for (float t = 0; t <= 1f; t += .04f)
                {
                    var p = Vector3.Lerp(a, b, t);
                    Require(guard.CanOccupy(p, rot), $"{tag} corridor collision at {floor.InverseTransformPoint(p)}");
                    samples++;
                }
            }
            Debug.Log($"{tag}_ROUTES_OK: {samples} wheelchair corridor samples passed.");
        }

        static void ValidateFourFloorElevators(SimulationController sim, WheelchairMovement chair, WheelchairCollisionGuard guard, ConvergenceFloorNavigation nav)
        {
            var lifts = UnityEngine.Object.FindObjectsByType<PassengerElevator>(FindObjectsSortMode.None);
            Require(lifts.Length == 2, "Expected 2 lifts (EastLift & WestLift)");
            var pairs = new[] { (0, 2), (2, 3), (3, 1), (1, 3), (3, 0) };
            foreach (var lift in lifts)
            {
                Require(lift.Stops.Length == 4, lift.name + " must have 4 stops (B1, 1F, 2F, 3F)");
                foreach (var (fromFloor, toFloor) in pairs)
                {
                    sim.ResetSimulation();
                    var s = lift.Stops[fromFloor];
                    s.door.SetProgressInstant(1);
                    chair.transform.SetPositionAndRotation(lift.BoardingPosition(fromFloor), s.cabin.rotation);
                    Physics.SyncTransforms();
                    Require(lift.FullyInside(fromFloor), $"{lift.name} not inside stop {fromFloor}");
                    Require(guard.CanOccupy(chair.transform.position, chair.transform.rotation), $"{lift.name} cabin blocked at stop {fromFloor}");
                    Require(lift.RequestFloor(toFloor), $"{lift.name} rejected trip {fromFloor}->{toFloor}");
                    for (int k = 0; k < 60 * 6; k++) lift.Step(1f / 60f);
                    Require(!lift.Busy && !sim.MovementLocked && !sim.IsRunning, $"{lift.name} stuck after {fromFloor}->{toFloor}");
                    Require(lift.FullyInside(toFloor), $"{lift.name} wrong arrival cabin for {fromFloor}->{toFloor}");
                    Require(lift.Stops[toFloor].door.Progress > .99f, $"{lift.name} arrival door closed at {toFloor}");
                    Require(Vector3.Dot(chair.transform.forward, -lift.Stops[toFloor].cabin.forward) > .999f, $"{lift.name} arrival not exit-facing at {toFloor}");

                    // W-only disembarkation check
                    sim.SetControlSource(WheelchairControlSource.Keyboard);
                    sim.StartSimulation();
                    for (int k = 0; k < 60; k++) chair.ApplyDirectionalInput(1, 0, 1f / 60f, WheelchairControlSource.Keyboard);
                    sim.StopSimulation();
                    Require(!lift.FullyInside(toFloor), $"{lift.name} blocked W-only exit at {toFloor}");
                }
            }
            sim.ResetSimulation();
            Debug.Log("FOUR_FLOOR_ELEVATOR_OK: EastLift & WestLift verified across B1, 1F, 2F, and 3F.");
        }

        static void ValidateUpperStairs(GameObject f2, GameObject f3)
        {
            var free = Camera.main.GetComponent<DemoFreeCamera>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var toggle = typeof(DemoFreeCamera).GetMethod("ToggleStairWalk", flags);
            var walk = typeof(DemoFreeCamera).GetMethod("WalkStairs", flags);
            var active = typeof(DemoFreeCamera).GetField("stairRoute", flags);

            var routes = f2.GetComponentsInChildren<StairWalkRoute>().Concat(f3.GetComponentsInChildren<StairWalkRoute>()).ToArray();
            Require(routes.Length == 5, "Expected 5 upper stair routes (4 core runs + Open2F3FStair), got " + routes.Length);

            var eye = Vector3.up * StairWalkRoute.EyeHeight;
            foreach (var route in routes)
            {
                Require(Mathf.Abs(route.PositionAt(route.Length).y - route.PositionAt(0).y - Hlp.StoreyRise) < .01f, route.name + " rise mismatch");
                for (float d = .8f; d < route.Length - .8f; d += .15f)
                {
                    var ground = route.PositionAt(d);
                    Require(Physics.Raycast(ground + Vector3.up * .30f, Vector3.down, .55f, ~LayerMask.GetMask("WheelchairObstacle"), QueryTriggerInteraction.Ignore),
                        route.name + " missing physical support at " + route.transform.InverseTransformPoint(ground));
                }
            }

            free.EnterFreeView();
            try
            {
                foreach (var route in routes)
                {
                    foreach (bool ascending in new[] { true, false })
                    {
                        var from = route.PositionAt(ascending ? 0 : route.Length) + eye;
                        var to = route.PositionAt(ascending ? route.Length : 0) + eye;
                        free.transform.position = from;
                        toggle.Invoke(free, null);
                        Require((StairWalkRoute)active.GetValue(free) == route, "Failed to join stair: " + route.name);
                        route.OpenUpperDoor();
                        int maxFrames = Mathf.CeilToInt(route.Length / 1.65f * 60) + 12;
                        for (int frame = 0; frame < maxFrames && (StairWalkRoute)active.GetValue(free); frame++)
                            walk.Invoke(free, new object[] { ascending ? 1f : -1f, 1f / 60f });
                        Require(Vector3.Distance(free.transform.position, to) < .01f && active.GetValue(free) == null,
                            "Stair walk failed to complete: " + route.name);
                    }
                }
            }
            finally
            {
                foreach (var d in f2.GetComponentsInChildren<InteractiveDoor>().Concat(f3.GetComponentsInChildren<InteractiveDoor>()))
                    d.ResetDoor();
                free.ExitFreeView();
            }
            Debug.Log("UPPER_STAIRS_OK: 5 stair routes (1F<->2F, 2F<->3F core + open) physically supported and traversed both ways.");
        }

        static IEnumerator CaptureScreenshots(GameObject f2, GameObject f3, string dir)
        {
            var cam = Camera.main;
            var follow = cam.GetComponent<CameraFollow>();
            var free = cam.GetComponent<DemoFreeCamera>();
            free.EnterFreeView();
            follow.enabled = false;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.renderMode != RenderMode.WorldSpace) canvas.enabled = false;
            ShaderUtil.allowAsyncCompilation = false;
            cam.fieldOfView = 70;

            var shots = new (Transform floor, Vector3 eye, Vector3 look, string file)[]
            {
                (f2.transform, new Vector3(20.1f, 47.5f, 1.6f), new Vector3(19.5f, 32.0f, 1.4f), "2F_201Containers.png"),
                (f2.transform, new Vector3(11.5f, 28.0f, 1.6f), new Vector3(11.5f, 20.0f, 1.4f), "2F_NorthLiftAndStair.png"),
                (f2.transform, new Vector3(6.5f, 54.0f, 1.6f), new Vector3(6.5f, 64.0f, 1.4f), "2F_WestLiftHall.png"),
                (f2.transform, new Vector3(24.0f, 42.0f, 1.6f), new Vector3(31.0f, 53.0f, 1.3f), "2F_Lounge204And206.png"),
                (f3.transform, new Vector3(14.2f, 20.0f, 1.6f), new Vector3(14.2f, 45.0f, 1.4f), "3F_MainLobbyAndOpenStair.png"),
                (f3.transform, new Vector3(22.0f, 59.0f, 1.6f), new Vector3(31.0f, 57.5f, 1.4f), "3F_309Corridor.png"),
                (f3.transform, new Vector3(6.2f, 55.0f, 1.6f), new Vector3(6.2f, 64.0f, 1.4f), "3F_WestLiftAndStair.png"),
                (f3.transform, new Vector3(15.0f, 23.0f, 1.6f), new Vector3(10.0f, 15.0f, 1.4f), "3F_319BrickPortalAndVending.png"),
            };

            foreach (var (floor, eye, look, file) in shots)
            {
                cam.transform.position = Hlp.World(floor, eye.x, eye.y, eye.z);
                cam.transform.LookAt(Hlp.World(floor, look.x, look.y, look.z));
                for (int i = 0; i < 4; i++) yield return null;
                Capture(cam, Path.Combine(dir, file));
            }

            // Top-down orthographic plan overviews (hide ceilings temporarily)
            foreach (var (floor, name) in new[] { (f2, "2F_PlanOverview.png"), (f3, "3F_PlanOverview.png") })
            {
                var ceil = floor.transform.Find("Ceilings");
                ceil.gameObject.SetActive(false);
                cam.orthographic = true;
                cam.orthographicSize = 37;
                cam.transform.position = Hlp.World(floor.transform, 19f, 33f, 75f);
                cam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.right);
                for (int i = 0; i < 4; i++) yield return null;
                Capture(cam, Path.Combine(dir, name));
                ceil.gameObject.SetActive(true);
                cam.orthographic = false;
            }
            free.ExitFreeView();
        }

        static void Capture(Camera camera, string path)
        {
            var rt = new RenderTexture(1600, 900, 24);
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float scale = pipeline ? pipeline.renderScale : 1;
            try
            {
                if (pipeline) pipeline.renderScale = 1;
                camera.targetTexture = rt;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log("UPPER_FLOOR_CAPTURE: " + path);
            }
            finally
            {
                if (pipeline) pipeline.renderScale = scale;
                camera.targetTexture = previous;
                RenderTexture.active = active;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
