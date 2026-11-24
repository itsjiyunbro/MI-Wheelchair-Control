using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using P = EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;
using Hlp = EEGWheelchairSimulator.Editor.ConvergenceHallUpperFloorHelpers;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallUpperFloors
    {
        [MenuItem("Tools/EEG Wheelchair/Build Convergence Hall 2F And 3F")]
        public static void BuildAndValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Hlp.LoadMaterials();
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);

            var b1 = GameObject.Find("ConvergenceHallB1");
            var first = GameObject.Find("ConvergenceHall1F");
            if (!b1 || !first)
                throw new InvalidOperationException("ConvergenceHallB1 and ConvergenceHall1F must exist in scene first.");

            // Destroy old 2F / 3F instances if rebuilding
            foreach (string oldName in new[] { "ConvergenceHall2F", "ConvergenceHall3F" })
            {
                var old = GameObject.Find(oldName);
                if (old) UnityEngine.Object.DestroyImmediate(old);
            }

            var asset2 = ConvergenceHallSecondFloor.BuildPrefab(b1);
            var asset3 = ConvergenceHallThirdFloor.BuildPrefab(b1);

            Vector3 firstOrigin = first.transform.position;
            var second = (GameObject)PrefabUtility.InstantiatePrefab(asset2);
            second.transform.position = firstOrigin + Vector3.up * Hlp.StoreyRise;
            PrefabUtility.RecordPrefabInstancePropertyModifications(second.transform);

            var third = (GameObject)PrefabUtility.InstantiatePrefab(asset3);
            third.transform.position = firstOrigin + Vector3.up * (Hlp.StoreyRise * 2f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(third.transform);

            PrepareFirstFloorConnections(first);
            ConnectUpperStairs(first, second, third);
            ConfigureAllFloorNavigation(firstOrigin);
            BuildFourFloorElevators(b1, first, second, third);

            // Reset wheelchair to B1 default start
            var movement = UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var basementSpawn = ConvergenceHallB1Setup.Plan(745, 830, .5f);
            movement.transform.SetPositionAndRotation(basementSpawn, Quaternion.identity);
            UnityEngine.Object.FindFirstObjectByType<CameraFollow>().SnapToTarget();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.SaveScene(scene, ConvergenceHallFirstFloor.Scene, true);
            AssetDatabase.SaveAssets();

            Debug.Log("UPPER_FLOORS_BUILT: 2F (201-219, 203-1, containers, lounge, services) and 3F (301-323, auditorium 318, project rooms) connected to B1/1F.");
            ConvergenceHallUpperFloorsValidation.BeginChecks();
        }

        public static void ReconnectIfPresent()
        {
            var b1 = GameObject.Find("ConvergenceHallB1");
            var first = GameObject.Find("ConvergenceHall1F");
            var second = GameObject.Find("ConvergenceHall2F");
            var third = GameObject.Find("ConvergenceHall3F");
            if (!b1 || !first || !second || !third) return;
            Hlp.LoadMaterials();
            PrepareFirstFloorConnections(first);
            ConnectUpperStairs(first, second, third);
            ConfigureAllFloorNavigation(first.transform.position);
            BuildFourFloorElevators(b1, first, second, third);
        }

        static void PrepareFirstFloorConnections(GameObject first)
        {
            // Open 1F stairwell ceilings and UpperBluebell 2F-height baffles in the unified scene
            var upperBack = first.transform.Find("Details/UpperStairBack");
            if (upperBack)
            {
                upperBack.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(upperBack.gameObject);
            }
            var yonsei = first.transform.Find("Details/UpperBluebell/YonseiLetters");
            if (yonsei)
            {
                yonsei.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(yonsei.gameObject);
            }
            foreach (string path in new[]
            {
                "Details/NorthStairwell/StairwellCeiling",
                "Details/NorthStairwell/HighFrontWall",
                "Details/SouthStairwell/StairwellCeiling",
                "Details/UpperStairHighReturn",
                "Details/UpperGalleryEnd",
                "Details/UpperStairLandingHeader",
                "Details/UpperGalleryStub",
                "Details/UpperBluebell/LandingSofa",
            })
            {
                var obj = first.transform.Find(path);
                if (obj)
                {
                    obj.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(obj.gameObject);
                }
            }
        }

        static void ConnectUpperStairs(GameObject first, GameObject second, GameObject third)
        {
            // Remove previous upper stair connectors
            foreach (var floor in new[] { second, third })
            {
                var details = floor.transform.Find("Details");
                foreach (var old in details.Cast<Transform>().Where(t => t.name.EndsWith("ConnectedStair") || t.name == "Open2F3FStair").ToArray())
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
            }

            // 1F -> 2F & 2F -> 3F North & South Core Stairwells
            ConnectCoreStairRun(first, second, "1F2F_NorthConnectedStair", east: true);
            ConnectCoreStairRun(second, third, "2F3F_NorthConnectedStair", east: true);
            ConnectCoreStairRun(first, second, "1F2F_SouthConnectedStair", east: false);
            ConnectCoreStairRun(second, third, "2F3F_SouthConnectedStair", east: false);

            // 2F -> 3F Open Switchback Stair (photos 04 & 09)
            BuildOpenSwitchback2F3F(second, third);
        }

        static void ConnectCoreStairRun(GameObject lowerFloor, GameObject upperFloor, string name, bool east)
        {
            var upperDetails = upperFloor.transform.Find("Details");
            var connected = P.Group(upperDetails, name);
            Vector3 anchor = east ? Hlp.At(15f, 25.5f) : Hlp.At(1.8f, 59.4f);
            float yaw = east ? 90f : -90f;
            connected.position = lowerFloor.transform.TransformPoint(anchor);
            connected.rotation = lowerFloor.transform.rotation * Quaternion.Euler(0, yaw, 0);

            float width = east ? 3.80f : 3.26f;
            float depth = east ? 6.95f : 6.40f;
            const int count = 13;
            const float start = .95f, landingDepth = .95f, mid = Hlp.StoreyRise / 2f;
            float turn = depth - landingDepth;
            float tread = (turn - start) / count;
            float rise = mid / count;
            float laneWidth = (width - .12f) / 2f;
            float lowerX = (east ? 1 : -1) * (laneWidth + .12f) / 2f;
            float upperX = -lowerX;

            void Surface(string n, Vector3 pos, Vector3 size)
            {
                var s = Hlp.Box(connected, n, pos, size, Hlp.stairTile);
                s.gameObject.AddComponent<BoxCollider>();
            }

            for (int i = 0; i < count; i++)
            {
                float lh = (i + 1) * rise;
                float lz = start + (i + .5f) * tread;
                Surface("LowerTread" + i, new Vector3(lowerX, lh / 2f, lz), new Vector3(laneWidth, lh, tread));
                float uh = mid + (i + 1) * rise;
                float uz = turn - (i + .5f) * tread;
                Surface("ReturnTread" + i, new Vector3(upperX, uh - rise / 2f, uz), new Vector3(laneWidth, rise, tread));
                Hlp.Box(connected, "LowerNosing" + i, new Vector3(lowerX, lh + .003f, lz - tread / 2f + .012f), new Vector3(laneWidth, .006f, .023f), Hlp.silver);
                Hlp.Box(connected, "ReturnNosing" + i, new Vector3(upperX, uh + .003f, uz + tread / 2f - .012f), new Vector3(laneWidth, .006f, .023f), Hlp.silver);
            }
            Surface("MidLanding", new Vector3(0, mid - .075f, turn + landingDepth / 2f), new Vector3(width, .15f, landingDepth));
            var slabBottom = new Vector3(upperX, mid - .16f, turn);
            var slabTop = new Vector3(upperX, Hlp.StoreyRise - .16f, start);
            var slab = Hlp.Box(connected, "ReturnSupport", (slabBottom + slabTop) / 2f, new Vector3(laneWidth, .15f, Vector3.Distance(slabBottom, slabTop)), Hlp.stairTile);
            slab.localRotation = Quaternion.LookRotation(slabTop - slabBottom);

            // Lower & Upper landing slabs bridging door thresholds
            Surface("LowerApproach", new Vector3(0, -.09f, start / 2f), new Vector3(width, .18f, start));
            Surface("UpperLanding", new Vector3(upperX, Hlp.StoreyRise - .09f, start / 2f), new Vector3(laneWidth, .18f, start));
            Surface("UpperBridge", new Vector3(0, Hlp.StoreyRise - .09f, .16f), new Vector3(width, .18f, .32f));

            foreach (int side in new[] { -1, 1 })
            {
                float x = lowerX + side * (laneWidth / 2f - .055f);
                Hlp.AddStairRail(connected, new Vector3(x, .03f, start), new Vector3(x, mid, turn));
                x = upperX + side * (laneWidth / 2f - .055f);
                Hlp.AddStairRail(connected, new Vector3(x, mid, turn), new Vector3(x, Hlp.StoreyRise, start));
                Hlp.AddStairRail(connected, new Vector3(x, Hlp.StoreyRise, start), new Vector3(x, Hlp.StoreyRise, .32f));
            }
            Hlp.AddStairRail(connected, new Vector3(-width / 2f + .05f, mid, depth - .065f), new Vector3(width / 2f - .05f, mid, depth - .065f));

            var upperDoorObj = upperDetails.Find(east ? "NorthStairwell/StairDoor" : "SouthStairwell");
            var upperDoor = upperDoorObj ? upperDoorObj.GetComponent<InteractiveDoor>() : null;
            var path = new[]
            {
                new Vector3(lowerX * .50f, 0, -.35f),
                new Vector3(lowerX * .50f, 0, .43f),
                new Vector3(lowerX, 0, .68f),
                new Vector3(lowerX, 0, start),
                new Vector3(lowerX, mid, turn),
                new Vector3(lowerX, mid, turn + .46f),
                new Vector3(upperX, mid, turn + .46f),
                new Vector3(upperX, mid, turn),
                new Vector3(upperX, Hlp.StoreyRise, start),
                new Vector3(upperX, Hlp.StoreyRise, .22f),
                new Vector3(upperX * .50f, Hlp.StoreyRise, .05f),
                new Vector3(upperX * .50f, Hlp.StoreyRise, -.82f),
            };
            connected.gameObject.AddComponent<StairWalkRoute>().Configure(path, upperDoor);
        }

        static void BuildOpenSwitchback2F3F(GameObject second, GameObject third)
        {
            // Foot at 2F Lounge (u=-8.5, v=46.8), climbing West (+v to 51.5) & returning East (-v to 46.8) at 3F (beside 305/306).
            var g = P.Group(third.transform.Find("Details"), "Open2F3FStair");
            g.position = second.transform.TransformPoint(Hlp.At(-8.5f, 46.8f));
            g.rotation = second.transform.rotation * Quaternion.Euler(0, -90f, 0);

            const float width = 3.50f, depth = 4.70f, start = .55f, landingDepth = .95f, mid = Hlp.StoreyRise / 2f;
            const int count = 11;
            float turn = depth - landingDepth;
            float tread = (turn - start) / count;
            float rise = mid / count;
            float laneWidth = (width - .16f) / 2f;
            float lowerX = -(laneWidth + .16f) / 2f;
            float upperX = -lowerX;

            void Surface(string n, Vector3 pos, Vector3 size)
            {
                var s = Hlp.Box(g, n, pos, size, Hlp.stairTile);
                s.gameObject.AddComponent<BoxCollider>();
            }

            for (int i = 0; i < count; i++)
            {
                float lh = (i + 1) * rise;
                float lz = start + (i + .5f) * tread;
                Surface("LowerTread" + i, new Vector3(lowerX, lh / 2f, lz), new Vector3(laneWidth, lh, tread));
                float uh = mid + (i + 1) * rise;
                float uz = turn - (i + .5f) * tread;
                Surface("ReturnTread" + i, new Vector3(upperX, uh - rise / 2f, uz), new Vector3(laneWidth, rise, tread));
            }
            Surface("MidLanding", new Vector3(0, mid - .075f, turn + landingDepth / 2f), new Vector3(width, .15f, landingDepth));
            var sb = new Vector3(upperX, mid - .15f, turn);
            var st = new Vector3(upperX, Hlp.StoreyRise - .15f, start);
            var slab = Hlp.Box(g, "ReturnSupport", (sb + st) / 2f, new Vector3(laneWidth, .14f, Vector3.Distance(sb, st)), Hlp.stairTile);
            slab.localRotation = Quaternion.LookRotation(st - sb);
            Surface("UpperLanding", new Vector3(upperX, Hlp.StoreyRise - .09f, start / 2f), new Vector3(laneWidth, .18f, start));
            Surface("LowerApproach", new Vector3(lowerX, -.09f, start / 2f), new Vector3(laneWidth, .18f, start));

            // Enclose 2F stair sides below 3F slab (y=0..4.42m); 3F opening gets VoidGuard + StairMouthGuard
            float shaftH = Hlp.StoreyRise - .08f;
            Hlp.Box(g, "StairWallNorth", new Vector3(width / 2f + .06f, shaftH / 2f, depth / 2f), new Vector3(.12f, shaftH, depth), Hlp.white, true);
            Hlp.Box(g, "StairWallSouth", new Vector3(-width / 2f - .06f, shaftH / 2f, depth / 2f), new Vector3(.12f, shaftH, depth), Hlp.white, true);
            Hlp.Box(g, "StairWallWest", new Vector3(0, shaftH / 2f, depth + .06f), new Vector3(width + .24f, shaftH, .12f), Hlp.white, true);
            var guard2F = Hlp.Box(g, "WheelchairGuard2F", new Vector3(0, .55f, .08f), new Vector3(width + .2f, 1.10f, .12f), Hlp.white, true);
            guard2F.GetComponent<Renderer>().enabled = false;
            var guard3F = Hlp.Box(g, "WheelchairGuard3F", new Vector3(0, Hlp.StoreyRise + .55f, .08f), new Vector3(width + .2f, 1.10f, .12f), Hlp.white, true);
            guard3F.GetComponent<Renderer>().enabled = false;

            Hlp.AddStairRail(g, new Vector3(lowerX - laneWidth / 2f + .05f, 0, start), new Vector3(lowerX - laneWidth / 2f + .05f, mid, turn));
            Hlp.AddStairRail(g, new Vector3(lowerX + laneWidth / 2f - .05f, 0, start), new Vector3(lowerX + laneWidth / 2f - .05f, mid, turn));
            Hlp.AddStairRail(g, new Vector3(upperX - laneWidth / 2f + .05f, mid, turn), new Vector3(upperX - laneWidth / 2f + .05f, Hlp.StoreyRise, 0));
            Hlp.AddStairRail(g, new Vector3(upperX + laneWidth / 2f - .05f, mid, turn), new Vector3(upperX + laneWidth / 2f - .05f, Hlp.StoreyRise, 0));
            Hlp.AddStairRail(g, new Vector3(-width / 2f + .05f, mid, depth - .06f), new Vector3(width / 2f - .05f, mid, depth - .06f));

            var path = new[]
            {
                new Vector3(lowerX, 0, -.65f),
                new Vector3(lowerX, 0, start),
                new Vector3(lowerX, mid, turn),
                new Vector3(lowerX, mid, turn + .45f),
                new Vector3(upperX, mid, turn + .45f),
                new Vector3(upperX, mid, turn),
                new Vector3(upperX, Hlp.StoreyRise, start),
                new Vector3(upperX, Hlp.StoreyRise, -.65f),
            };
            g.gameObject.AddComponent<StairWalkRoute>().Configure(path, null);
        }

        static void ConfigureAllFloorNavigation(Vector3 firstOrigin)
        {
            var movement = UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var sim = UnityEngine.Object.FindFirstObjectByType<SimulationController>();
            var follow = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            var navigation = sim.GetComponent<ConvergenceFloorNavigation>();
            if (!navigation) navigation = sim.gameObject.AddComponent<ConvergenceFloorNavigation>();

            var hud = UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();
            var canvas = hud.GetComponent<Canvas>();
            var old = canvas.transform.Find("FloorShortcuts");
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);

            var panel = new GameObject("FloorShortcuts", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one;
            panel.anchoredPosition = new Vector2(-18, -18);
            panel.sizeDelta = new Vector2(260, 162);
            panel.GetComponent<Image>().color = new Color(.035f, .065f, .11f, .94f);

            var label = new GameObject("FloorLabel", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(panel, false);
            label.font = Hlp.font;
            label.fontSize = 16;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            var lr = (RectTransform)label.transform;
            lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(.5f, 1);
            lr.anchoredPosition = new Vector2(0, -6);
            lr.sizeDelta = new Vector2(245, 49);

            var source = hud.transform.Find("StatusPanel/StartButton");
            var specs = new (string name, string text, Vector2 pos, UnityEngine.Events.UnityAction action)[]
            {
                ("BasementButton", "B1 · PgDn", new Vector2(10, -64), navigation.GoToBasement),
                ("FirstFloorButton", "1F", new Vector2(134, -64), navigation.GoToFirstFloor),
                ("SecondFloorButton", "2F", new Vector2(10, -110), navigation.GoToSecondFloor),
                ("ThirdFloorButton", "3F · PgUp", new Vector2(134, -110), navigation.GoToThirdFloor),
            };
            foreach (var (name, text, pos, action) in specs)
            {
                var t = (RectTransform)UnityEngine.Object.Instantiate(source.gameObject, panel).transform;
                t.name = name;
                t.anchorMin = t.anchorMax = t.pivot = new Vector2(0, 1);
                t.anchoredPosition = pos;
                t.sizeDelta = new Vector2(116, 38);
                var button = t.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddPersistentListener(button.onClick, action);
                var tx = t.GetComponentInChildren<Text>();
                tx.font = Hlp.font;
                tx.fontSize = 15;
                tx.text = text;
            }

            Vector3 b1Spawn = ConvergenceHallB1Setup.Plan(745, 830, .5f);
            Vector3 f1Spawn = firstOrigin + Hlp.At(17, 27, .5f);
            Vector3 f2Spawn = firstOrigin + Vector3.up * Hlp.StoreyRise + Hlp.At(1.8f, 27f, .5f);
            Vector3 f3Spawn = firstOrigin + Vector3.up * (Hlp.StoreyRise * 2f) + Hlp.At(-12.25f, 22f, .5f);
            navigation.ConfigureAllFloors(sim, movement, follow, label, b1Spawn, f1Spawn, f2Spawn, f3Spawn, 0, 90, 90, 90);
        }

        static Text UiText(Transform p, string name, string value, Font font, Vector2 size, Vector2 offset)
        {
            var t = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(p, false);
            t.font = font;
            t.text = value;
            t.fontSize = 15;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            var r = (RectTransform)t.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, 1);
            r.anchoredPosition = offset;
            r.sizeDelta = size;
            return t;
        }

        static Button UiButton(Transform panel, Transform template, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var r = (RectTransform)UnityEngine.Object.Instantiate(template.gameObject, panel).transform;
            r.name = name;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(149, 34);
            var b = r.GetComponent<Button>();
            b.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(b.onClick, action);
            var t = r.GetComponentInChildren<Text>();
            t.font = Hlp.font;
            t.fontSize = 14;
            t.text = label;
            return b;
        }

        static void BuildFourFloorElevators(GameObject b1, GameObject first, GameObject second, GameObject third)
        {
            var sim = UnityEngine.Object.FindFirstObjectByType<SimulationController>();
            var actor = UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var follow = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            var hud = UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();

            var old = sim.transform.Find("PassengerElevators");
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            old = hud.transform.Find("ElevatorControls");
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach (string n in new[] { "EastTransitFade", "WestTransitFade" })
            {
                old = hud.transform.Find(n);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }

            var root = P.Group(sim.transform, "PassengerElevators");
            var ui = new GameObject("ElevatorControls", typeof(RectTransform)).GetComponent<RectTransform>();
            ui.SetParent(hud.transform, false);
            ui.anchorMin = Vector2.zero;
            ui.anchorMax = Vector2.one;
            ui.offsetMin = ui.offsetMax = Vector2.zero;
            var template = hud.transform.Find("StatusPanel/StartButton");

            foreach (bool east in new[] { true, false })
            {
                string name = east ? "동쪽 엘리베이터" : "서쪽 엘리베이터";
                var g = P.Group(root, east ? "EastLift" : "WestLift");
                var lift = g.gameObject.AddComponent<PassengerElevator>();

                var lower = ConvergenceHallElevatorCabins.Door(b1, east);
                string hallPath = east ? "RoomsAndCores/NorthElevatorHall" : "RoomsAndCores/SouthElevatorHall/LeftWallElevator";
                var s1 = first.transform.Find(hallPath);
                var s2 = second.transform.Find(hallPath);
                var s3 = third.transform.Find(hallPath);

                var stops = new[]
                {
                    new PassengerElevator.Stop { cabin = lower.Find("MeasuredCabin"), door = lower.GetComponent<InteractiveDoor>() },
                    new PassengerElevator.Stop { cabin = s1.Find("MeasuredCabin"), door = s1.GetComponent<InteractiveDoor>(), indicator = s1.Find("PhotoElevatorDetails/PhotoFloorIndicator/FloorNumber").GetComponent<Text>() },
                    new PassengerElevator.Stop { cabin = s2.Find("MeasuredCabin"), door = s2.GetComponent<InteractiveDoor>(), indicator = s2.Find("PhotoElevatorDetails/PhotoFloorIndicator/FloorNumber").GetComponent<Text>() },
                    new PassengerElevator.Stop { cabin = s3.Find("MeasuredCabin"), door = s3.GetComponent<InteractiveDoor>(), indicator = s3.Find("PhotoElevatorDetails/PhotoFloorIndicator/FloorNumber").GetComponent<Text>() },
                };

                var panel = new GameObject(east ? "EastControls" : "WestControls", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                panel.SetParent(ui, false);
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, 1);
                panel.anchoredPosition = new Vector2(0, -18);
                panel.sizeDelta = new Vector2(330, 200);
                panel.GetComponent<Image>().color = new Color(.035f, .065f, .11f, .97f);

                var label = UiText(panel, "Status", name, Hlp.font, new Vector2(314, 62), new Vector2(0, -5));
                var b = UiButton(panel, template, "B1", "B1 · 지하 1층", new Vector2(12, -70), lift.GoToBasement);
                var f1 = UiButton(panel, template, "1F", "1F · 1층", new Vector2(169, -70), lift.GoToFirstFloor);
                var f2 = UiButton(panel, template, "2F", "2F · 2층", new Vector2(12, -112), lift.GoToSecondFloor);
                var f3 = UiButton(panel, template, "3F", "3F · 3층", new Vector2(169, -112), lift.GoToThirdFloor);
                var o = UiButton(panel, template, "Open", "문 열기 / 호출", new Vector2(12, -154), lift.OpenDoors);
                var c = UiButton(panel, template, "Close", "문 닫기", new Vector2(169, -154), lift.CloseDoors);

                var fade = new GameObject(east ? "EastTransitFade" : "WestTransitFade", typeof(RectTransform), typeof(Canvas), typeof(Image));
                var rect = (RectTransform)fade.transform;
                rect.SetParent(hud.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var fc = fade.GetComponent<Canvas>();
                fc.overrideSorting = true;
                fc.sortingOrder = 200;
                var image = fade.GetComponent<Image>();
                image.color = Color.clear;
                image.raycastTarget = false;
                var ft = UiText(rect, "TravelMessage", "엘리베이터 이동 중", Hlp.font, new Vector2(400, 60), Vector2.zero);
                var fr = (RectTransform)ft.transform;
                fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(.5f, .5f);
                fade.SetActive(false);
                panel.gameObject.SetActive(false);

                lift.Configure(name, stops, sim, actor, follow, panel.gameObject, label, b, f1, o, c, image, f2, f3);

                var metal = s1.Find("CallPanel").GetComponent<Renderer>().sharedMaterial;
                var dark = s1.Find("FloorDisplayBacking").GetComponent<Renderer>().sharedMaterial;
                foreach (var stop in stops)
                {
                    var existing = stop.cabin.Find("PassengerControlPanel");
                    if (existing) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    var p = P.Group(stop.cabin, "PassengerControlPanel");
                    p.localPosition = new Vector3(.75f, 1.13f, .65f);
                    p.localRotation = Quaternion.Euler(0, 90, 0);
                    P.Box(p, "Plate", Vector3.zero, new Vector3(.2f, .56f, .02f), metal);
                    for (int i = 0; i < 4; i++)
                        P.Box(p, "Button" + i, new Vector3(0, i * .11f - .165f, -.016f), new Vector3(.065f, .065f, .014f), dark);
                }
            }
        }
    }
}
