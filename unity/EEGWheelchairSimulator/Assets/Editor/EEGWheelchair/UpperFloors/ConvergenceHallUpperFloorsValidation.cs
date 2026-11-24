using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
                    throw new TimeoutException("Second floor validation timed out");
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
            else Debug.Log("SECOND_FLOOR_PLAY_OK");
            EditorApplication.ExitPlaymode();
            if (Application.isBatchMode) EditorApplication.Exit(ex == null ? 0 : 1);
        }

        static void Require(bool cond, string msg) { if (!cond) throw new Exception("SECOND_FLOOR: " + msg); }

        static IEnumerator Run()
        {
            for (int i = 0; i < 8; i++) yield return null;
            var b1 = GameObject.Find("ConvergenceHallB1");
            var f1 = GameObject.Find("ConvergenceHall1F");
            var f2 = GameObject.Find("ConvergenceHall2F");
            Require(b1 && f1 && f2, "Missing one of B1/1F/2F root objects");

            ValidateFloorGeometry(f2, new[] { "202", "203", "203-1", "204", "205", "206", "207", "208", "209", "210", "212", "213", "214", "215", "216", "217", "218", "219" }, "2F");
            Require(f2.transform.Find("Details/Containers201/201_Gray"), "Missing 201 container");

            var sim = UnityEngine.Object.FindFirstObjectByType<SimulationController>();
            var chair = UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var guard = chair.GetComponent<WheelchairCollisionGuard>();
            var nav = UnityEngine.Object.FindFirstObjectByType<ConvergenceFloorNavigation>();

            Require(chair.transform.position.y < 1f && !sim.IsRunning, "Default start must be on B1");

            nav.GoToSecondFloor();
            Require(Mathf.Abs(chair.transform.position.y - 9.5f) < .2f && !sim.IsRunning, "2F shortcut failed");
            sim.SetControlSource(WheelchairControlSource.Keyboard);
            sim.StartSimulation();
            Vector3 begin2 = chair.transform.position;
            for (int i = 0; i < 30; i++) chair.ApplyDirectionalInput(1, 0, 1f / 30f, WheelchairControlSource.Keyboard);
            Require(Vector3.Distance(begin2, chair.transform.position) > .4f, "2F wheelchair movement blocked");
            sim.StopSimulation();

            sim.ResetSimulation();
            Require(chair.transform.position.y < 1f && !sim.IsRunning, "RESET did not return to B1");

            var routes2 = new[]
            {
                new[] { new Vector2(1.8f, 15.0f), new Vector2(1.8f, 57.8f) },
                new[] { new Vector2(1.8f, 27.2f), new Vector2(15.5f, 27.2f) },
                new[] { new Vector2(3.5f, 13.0f), new Vector2(18.0f, 13.0f) },
                new[] { new Vector2(-12.5f, 41.5f), new Vector2(-1.0f, 41.5f) },
                new[] { new Vector2(-22.0f, 57.8f), new Vector2(6.0f, 57.8f) },
                new[] { new Vector2(10.0f, 55.2f), new Vector2(16.0f, 55.2f) },
            };
            CheckRoutes(f2.transform, routes2, guard, "2F");
            Validate2FContainment(f2, guard);

            foreach (var door in f2.GetComponentsInChildren<InteractiveDoor>())
            {
                foreach (var m in door.Motions)
                    Require(m.target && m.target.IsChildOf(f2.transform), "Door has external moving part: " + door.Label);
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

            ValidateThreeFloorElevators(sim, chair, guard);
            ValidateSecondFloorStairs(f2);

            sim.ResetSimulation();
            Debug.Log("SECOND_FLOOR_VALIDATION_OK: 2F geometry, corridor clearance, 3-floor elevators (B1<->1F<->2F), and 1F<->2F stairs passed.");
        }

        static void Validate2FContainment(GameObject f2, WheelchairCollisionGuard guard)
        {
            foreach (var door in f2.GetComponentsInChildren<InteractiveDoor>())
            {
                string label = door.Label ?? "";
                Require(!label.Contains("EXIT") && !label.Contains("비상구") && !label.Contains("출입구") && !label.Contains("212") && !label.Contains("215"),
                    "Exterior exit or locked RC entrance must not be openable: " + label);
            }
            var blockers2F = new[]
            {
                new Vector2(-14.0f, 41.5f),
                new Vector2(  1.0f,  0.0f),
                new Vector2( 20.5f, 14.95f),
                new Vector2( 19.0f, 27.25f),
                new Vector2(  3.6f, 31.8f),
                new Vector2( 13.0f, 34.8f),
                new Vector2(  7.2f, 41.0f),
                new Vector2(  4.5f, 11.5f),
                new Vector2( 11.0f, 11.5f),
            };
            foreach (var p in blockers2F)
                Require(!guard.CanOccupy(Hlp.World(f2.transform, p.x, p.y, .5f), Quaternion.identity),
                    $"2F barrier leaked wheelchair at (u={p.x}, v={p.y})");
            var hall = f2.transform.Find("RoomsAndCores/SouthElevatorHall");
            var cornerWall = hall.Find("WestLiftCornerWall");
            Require(cornerWall && cornerWall.GetComponent<BoxCollider>(), "2F missing WestLiftCornerWall collider");
            Debug.Log("SECOND_FLOOR_CONTAINMENT_OK: exterior exits locked, stairs/voids guarded, WestLift corner sealed.");
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

        static void ValidateThreeFloorElevators(SimulationController sim, WheelchairMovement chair, WheelchairCollisionGuard guard)
        {
            var lifts = UnityEngine.Object.FindObjectsByType<PassengerElevator>(FindObjectsSortMode.None);
            Require(lifts.Length == 2, "Expected 2 lifts (EastLift & WestLift)");
            var pairs = new[] { (0, 2), (2, 1), (1, 2), (2, 0) };
            foreach (var lift in lifts)
            {
                Require(lift.Stops.Length == 3, lift.name + " must have 3 stops (B1, 1F, 2F)");
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

                    sim.SetControlSource(WheelchairControlSource.Keyboard);
                    sim.StartSimulation();
                    for (int k = 0; k < 60; k++) chair.ApplyDirectionalInput(1, 0, 1f / 60f, WheelchairControlSource.Keyboard);
                    sim.StopSimulation();
                    Require(!lift.FullyInside(toFloor), $"{lift.name} blocked W-only exit at {toFloor}");
                }
            }
            sim.ResetSimulation();
            Debug.Log("THREE_FLOOR_ELEVATOR_OK: EastLift & WestLift verified across B1, 1F, and 2F.");
        }

        static void ValidateSecondFloorStairs(GameObject f2)
        {
            var free = Camera.main.GetComponent<DemoFreeCamera>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var toggle = typeof(DemoFreeCamera).GetMethod("ToggleStairWalk", flags);
            var walk = typeof(DemoFreeCamera).GetMethod("WalkStairs", flags);
            var active = typeof(DemoFreeCamera).GetField("stairRoute", flags);

            var routes = f2.GetComponentsInChildren<StairWalkRoute>().ToArray();
            Require(routes.Length == 2, "Expected 2 1F<->2F stair routes, got " + routes.Length);

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
                foreach (var d in f2.GetComponentsInChildren<InteractiveDoor>()) d.ResetDoor();
                free.ExitFreeView();
            }
            Debug.Log("SECOND_FLOOR_STAIRS_OK: 2 core stair routes (1F<->2F) physically supported and traversed both ways.");
        }
    }
}
