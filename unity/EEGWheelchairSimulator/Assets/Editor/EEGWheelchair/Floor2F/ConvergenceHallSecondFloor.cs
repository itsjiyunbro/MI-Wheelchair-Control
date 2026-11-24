using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using P = EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;
using Hlp = EEGWheelchairSimulator.Editor.ConvergenceHallUpperFloorHelpers;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallSecondFloor
    {
        public const string Prefab = "Assets/Art/Prefabs/Environment/ConvergenceHall/ConvergenceHall2F.prefab";

        static Transform root, architecture, roofs, details;

        public static GameObject BuildPrefab(GameObject basement)
        {
            Hlp.LoadMaterials();
            var go = new GameObject("ConvergenceHall2F");
            root = go.transform;
            architecture = P.Group(root, "RoomsAndCores");
            roofs = P.Group(root, "Ceilings");
            details = P.Group(root, "Details");

            FloorAndRoof();
            Rooms(basement);
            Containers201();
            LoungeAndFixtures(basement);

            foreach (var section in new[] { root.Find("Floors"), roofs })
                P.Combine(section, "2F_" + section.name);

            var asset = PrefabUtility.SaveAsPrefabAsset(go, Prefab);
            UnityEngine.Object.DestroyImmediate(go);
            return asset;
        }

        static bool IsAtrium(float u, float v)
            => (u >= -1.0f && u <= 7.2f && v >= 11.5f && v <= 49.0f)
            || (u >= 7.2f && u <= 19.0f && v >= 25.5f && v <= 34.8f);

        static void FloorAndRoof()
        {
            var domains = new[]
            {
                new Hlp.R(-24.0f, 51.5f,  0.0f, 66.0f), // MDF, 방재실, 전기실, 203-1, 206..208, 205, West graduate corridor
                new Hlp.R(-14.0f, 40.0f,  0.0f, 51.5f), // 204 Lounge, Open2F3FStair, WC-W-M, WC-W-W, 204
                new Hlp.R(-24.0f, 26.0f,  0.0f, 40.0f), // 물탱크실, 기계실, 소화용수, 209 West
                new Hlp.R( -5.5f,  0.0f,  0.0f, 26.0f), // 210, WC-E-M, WC-E-W, 209 East
                new Hlp.R(  0.0f,  0.0f, 20.5f, 26.0f), // 211 Lounge, 212..219 RC Wing, EastLift, EastStair, 216
                new Hlp.R(  0.0f, 26.0f, 19.0f, 48.5f), // UpperBluebell landing, VOID2F, 201 corridor
                new Hlp.R(  0.0f, 48.5f, 17.5f, 66.0f), // WestStair, WestLift, 201-W, 202, 203, West corridor
            };
            var holes = new[]
            {
                new Hlp.R(  3.6f, 29.0f, 14.8f, 34.616f), // 1F->2F UpperBluebell stair void
                new Hlp.R(  7.2f, 34.8f, 19.0f, 48.5f),   // 2F central atrium VOID
                new Hlp.R(-10.25f, 46.8f, -6.75f, 51.5f), // Open2F3FStair foot pocket check (floor exists on 2F under lower flight)
                new Hlp.R( 13.0f, 18.4f, 17.0f, 25.5f),   // NorthStairwell shaft
                new Hlp.R(  0.0f, 59.4f,  3.6f, 66.0f),   // SouthStairwell shaft
            };
            // On 2F, floor exists beneath Open2F3FStair lower approach, so only omit true open shafts
            var floorHoles = new[] { holes[0], holes[1], holes[3], holes[4] };

            var uBreaks = new[] { -24f, -14f, -5.5f, 0f, 3.6f, 7.2f, 13f, 14.8f, 17f, 17.5f, 19f, 20.5f };
            var vBreaks = new[] { 0f, 18.4f, 25.5f, 26f, 29f, 34.616f, 34.8f, 40f, 48.5f, 51.5f, 59.4f, 66f };
            var floors = P.Group(root, "Floors");
            for (int i = 0; i < uBreaks.Length - 1; i++)
            {
                for (int j = 0; j < vBreaks.Length - 1; j++)
                {
                    float u = (uBreaks[i] + uBreaks[i + 1]) / 2f;
                    float v = (vBreaks[j] + vBreaks[j + 1]) / 2f;
                    if (!domains.Any(r => r.Contains(u, v)) || floorHoles.Any(r => r.Contains(u, v))) continue;
                    var rect = new Hlp.R(uBreaks[i], vBreaks[j], uBreaks[i + 1], vBreaks[j + 1]);
                    var slab = Hlp.Rect(floors, $"Floor_{i}_{j}", rect, -.09f, .18f, IsAtrium(u, v) ? Hlp.stone : Hlp.tile);
                    slab.gameObject.AddComponent<BoxCollider>();
                }
            }

            // Corridor ceilings (exclude Open2F3FStair opening u=-10.25..-6.75, v=46.8..51.5 AND UpperBluebell slats 1F already builds)
            var whiteCeilings = new[]
            {
                new Hlp.R(-24f, 56.2f, 0f, 59.4f),         // Graduate & 전기실 West corridor
                new Hlp.R(-14f, 51.5f, -10.5f, 56.2f),     // North approach past Open2F3FStair
                new Hlp.R(-6.5f, 51.5f, 0f, 51.5f),
                new Hlp.R(-14f, 40f, -10.3f, 51.5f),       // Lounge204 north of OpenStair
                new Hlp.R(-6.7f, 40f, 0f, 46.8f),          // Lounge204 south/east corridor
                new Hlp.R(-13f, 26f, -5.5f, 40f),          // Mechanical service corridor + courtyard turn
                new Hlp.R(-10.3f, 40f, -6.7f, 46.8f),      // East of OpenStair
                new Hlp.R(0f, 11.5f, 8f, 26f),             // 211 Lobby
                new Hlp.R(8f, 11.5f, 20.5f, 18.4f),        // 211 South corridor past EastLift/EastStair
                new Hlp.R(0f, 0f, 2f, 11.5f),              // North-East 210/212 corridor
                new Hlp.R(0f, 34.8f, 7.2f, 48.5f),         // 201 corridor (over containers)
                new Hlp.R(0f, 48.5f, 9.5f, 59.4f),         // 202/203/WestLift lobby
                new Hlp.R(9.5f, 54f, 17.5f, 59.4f),        // Corridor between 202 and 203
                new Hlp.R(14.8f, 25.5f, 19f, 34.8f),       // South of UpperBluebell
                new Hlp.R(3.6f, 25.5f, 8f, 29f),           // Beside UpperBluebell / EastLift
                new Hlp.R(8f, 25.5f, 14.8f, 29f),          // Front of EastLift/EastStair
            };
            foreach (var r in whiteCeilings)
                Hlp.Rect(roofs, "WhiteCorridorCeiling", r, Hlp.H + .05f, .10f, Hlp.white);

            // Navy slatted atrium ceiling zones in 2F lobby
            Hlp.Slats(roofs, new Hlp.R(0f, 25.5f, 7.2f, 29f), Hlp.H, "2F_GalleryEast");

            // Watertight exterior perimeter & courtyard walls (zero open exterior exits or drop-offs)
            Hlp.LockedExitFacade(architecture, "CourtyardGlassExit", Hlp.At(-14.0f, 41.5f), 180, 3.0f, "2F", "출입구");
            Hlp.Wall(architecture, "LoungeCourtyardWindow", Hlp.At(-14.0f, 47.25f), 180, 8.5f, "2F", glazed: true);
            Hlp.Wall(architecture, "NorthGradCorridorEnd", Hlp.At(-24.0f, 57.8f), 180, 3.2f, "2F");
            Hlp.Wall(architecture, "LoungeWestStepWall", Hlp.At(-12.0f, 51.5f), 90, 3.0f, "2F");
            Hlp.Wall(architecture, "MechCourtyardStep", Hlp.At(-13.5f, 40.0f), 90, 1.0f, "2F");
            Hlp.Wall(architecture, "MechEastStepWall", Hlp.At(-11.75f, 26.0f), -90, 2.5f, "2F");
            Hlp.LockedExitFacade(architecture, "East210Exit", Hlp.At(1.0f, 0.0f), -90, 2.0f, "2F", "비상구");
            Hlp.LockedExitFacade(architecture, "SouthEast216Exit", Hlp.At(20.5f, 14.95f), 0, 6.9f, "2F", "비상구");
            Hlp.Wall(architecture, "South216StepWall", Hlp.At(19.75f, 25.5f), 90, 1.5f, "2F");
            Hlp.LockedExitFacade(architecture, "SouthBluebellExit", Hlp.At(19.0f, 27.25f), 0, 3.5f, "2F", "비상구");
            Hlp.Wall(architecture, "UpperBluebellSouthCap", Hlp.At(19.0f, 31.9f), 0, 5.8f, "2F");
            Hlp.Wall(architecture, "SouthWest202CorridorEnd", Hlp.At(17.5f, 55.25f), 0, 2.5f, "2F");

            // Void guards & wheelchair stair-mouth barriers around UpperBluebell (u=3.6..14.8, v=29..34.616) and Void2F (u=7.2..19, v=34.8..48.5)
            Hlp.VoidGuard(details, new Hlp.R(3.6f, 29.0f, 14.8f, 34.616f), skipU0: true);
            Hlp.StairMouthGuard(details, "UpperBluebell2FMouthGuard", Hlp.At(3.6f, 31.808f), 180f, 5.616f);
            Hlp.VoidGuard(details, new Hlp.R(7.2f, 34.8f, 19.0f, 48.5f));
            Hlp.Wall(architecture, "Void2FSouthExterior", Hlp.At(19.0f, 41.65f), 0, 13.7f, "2F", glazed: true);
        }

        static void Rooms(GameObject basement)
        {
            // East Wing (212..219 RC Convergence College) — entrance at v=11.5 locked so player cannot enter 212..219
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "214", new Hlp.R(2.0f, 0.0f, 7.0f, 3.7f), "u1");
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "213", new Hlp.R(2.0f, 3.7f, 7.0f, 7.5f), "u1");
            var r212 = Hlp.Room(architecture, roofs, "2F", "212", new Hlp.R(2.0f, 7.5f, 7.0f, 11.5f), "v1", "RC융합대학", "RC Convergence College");
            UnityEngine.Object.DestroyImmediate(r212.Find("V1/Door_212").GetComponent<InteractiveDoor>());

            var r215 = Hlp.Room(architecture, roofs, "2F", "215", new Hlp.R(7.0f, 0.0f, 15.0f, 11.5f), "v1", "RC라운지", "RC Lounge", glaze: true, doubles: true, clearDoor: true);
            UnityEngine.Object.DestroyImmediate(r215.Find("V1/Door_215").GetComponent<InteractiveDoor>());

            Hlp.TeachingRoom(architecture, roofs, details, "2F", "219", new Hlp.R(15.0f, 0.0f, 20.5f, 3.7f), "u0");
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "218", new Hlp.R(15.0f, 3.7f, 20.5f, 7.5f), "u0");
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "217", new Hlp.R(15.0f, 7.5f, 20.5f, 11.5f), "u0");

            Hlp.Room(architecture, roofs, "2F", "216", new Hlp.R(17.0f, 18.4f, 20.5f, 25.5f), "v0", "서버실", "Server Room");
            Hlp.Room(architecture, roofs, "2F", "210", new Hlp.R(-5.5f, 0.0f, 0.0f, 6.5f), "u1", "관리실", "Management Office");
            Hlp.Room(architecture, roofs, "2F", "WC-E-M", new Hlp.R(-5.5f, 6.5f, 0.0f, 11.5f), "u1", "남자 화장실", "Men");
            Hlp.Room(architecture, roofs, "2F", "WC-E-W", new Hlp.R(-5.5f, 11.5f, 0.0f, 16.5f), "u1", "여자 화장실", "Women");

            // Center & North Mechanical Rooms (aligned above 211/201 corridor)
            Hlp.Room(architecture, roofs, "2F", "209", new Hlp.R(-5.5f, 16.5f, 0.0f, 40.0f), "u1", "커리어플래닝룸", "Career Planning Room", glaze: true, doorOffset: -9.5f);
            Hlp.Room(architecture, roofs, "2F", "소화용수", new Hlp.R(-10.5f, 26.0f, -5.5f, 38.0f), "v1", "소화용수", "Fire Water");
            Hlp.Room(architecture, roofs, "2F", "물탱크실", new Hlp.R(-24.0f, 26.0f, -13.0f, 33.5f), "u1", "물탱크실", "Water Tank");
            Hlp.Room(architecture, roofs, "2F", "기계실", new Hlp.R(-24.0f, 33.5f, -13.0f, 40.0f), "u1", "기계실", "Mechanical Room");

            // North-West & West Wing (aligned cleanly to WestLift & WestStair)
            Hlp.Room(architecture, roofs, "2F", "MDF실", new Hlp.R(-24.0f, 59.4f, -20.5f, 66.0f), "v0", "MDF실", "MDF Room");
            Hlp.Room(architecture, roofs, "2F", "방재실", new Hlp.R(-20.5f, 59.4f, -16.5f, 66.0f), "v0", "방재실", "Disaster Prevention");
            Hlp.Room(architecture, roofs, "2F", "전기실", new Hlp.R(-16.5f, 59.4f, -5.0f, 66.0f), "v0", "전기실", "Electrical Room", doorOffset: 3.5f);
            Hlp.Room(architecture, roofs, "2F", "203-1", new Hlp.R(-5.0f, 59.4f, 0.0f, 66.0f), "u1", "의료AI프로젝트실", "Healthcare AI Project Room", doorOffset: 1.5f);

            Hlp.Room(architecture, roofs, "2F", "WC-W-M", new Hlp.R(-5.0f, 52.0f, 0.0f, 56.2f), "u1", "남자 화장실", "Men");
            Hlp.Room(architecture, roofs, "2F", "WC-W-W", new Hlp.R(-5.0f, 48.0f, 0.0f, 52.0f), "u1", "여자 화장실", "Women");
            Hlp.Room(architecture, roofs, "2F", "204", new Hlp.R(-5.0f, 43.0f, 0.0f, 48.0f), "v0", "미래캠퍼스 직원노동조합", "Staff Union", glaze: true);

            Hlp.Room(architecture, roofs, "2F", "205", new Hlp.R(-10.5f, 51.5f, -5.0f, 56.2f), "v1", "대학원 문서보존실", "Graduate Archive");
            Hlp.Room(architecture, roofs, "2F", "206", new Hlp.R(-17.0f, 51.5f, -13.5f, 56.2f), "v1", "대학원 미래혁신지원센터", "Future Innovation Center");
            Hlp.Room(architecture, roofs, "2F", "207", new Hlp.R(-20.5f, 51.5f, -17.0f, 56.2f), "v1", "대학원 교학처장실", "Dean of Graduate School");
            Hlp.Room(architecture, roofs, "2F", "208", new Hlp.R(-24.0f, 51.5f, -20.5f, 56.2f), "v1", "대학원 행정팀", "Graduate School Admin");

            // South-West Wing around 201 & WestLift (202, 203)
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "202", new Hlp.R(11.2f, 48.5f, 17.5f, 54.0f), "v1", "학생창업보육실", "SW Startups Incubator", glazed: true);
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "203", new Hlp.R(8.4f, 56.5f, 17.5f, 66.0f), "v0", "피지컬AI프로젝트실", "Physical AI Project Room", glazed: true);

            // Match all 4 restrooms to B1 milky-glass doors & handles
            Hlp.MatchBasementRestrooms(basement, architecture, "2F",
                new[] { "WC-E-M", "WC-E-W", "WC-W-W", "WC-W-M" },
                new[] { new Hlp.R(-5.5f, 6.5f, 0f, 11.5f), new Hlp.R(-5.5f, 11.5f, 0f, 16.5f), new Hlp.R(-5f, 48f, 0f, 52f), new Hlp.R(-5f, 52f, 0f, 56.2f) },
                new[] { "U1", "U1", "U1", "U1" });

            // East & West Elevator Halls + Stairwells (aligned vertically with B1 & 1F)
            Hlp.CopyNorthElevatorHall(basement, architecture, roofs, details, "2F", "2");
            Hlp.BuildNorthStairwellShell(details, "2F", new Hlp.R(13, 18.4f, 17, 25.5f), withCeiling: false);
            Hlp.CopySouthElevatorAndStairwell(basement, architecture, details, "2F", "2", buildCeiling: false);

            // Glass double partition across 201 corridor (at 201_Gray / 201_White seam, default open, photo 111425776_01.jpg)
            var part = Hlp.Face(architecture, "Corridor201GlassPartition", Hlp.At(2.1f, 39.4f), 90);
            Hlp.Panel(part, "Left", -2.1f, -.9f, true);
            Hlp.Panel(part, "Right", .9f, 2.1f, true);
            var partDoor = P.Group(part, "Door_201Partition");
            Hlp.Door(partDoor, "2F", "201-P", "", "", 1.7f, clear: true, glazedWall: true, addPlaque: false, initial: 1f);
            Hlp.ExitSign(partDoor, new Vector3(0, 2.65f, -.035f));
        }

        static void Containers201()
        {
            var group = P.Group(details, "Containers201");
            var specs = new[]
            {
                ("201_Gray",  4.2f, 35.0f,  7.0f, 39.2f, Hlp.contGray,  true),
                ("201_White", 4.2f, 39.5f,  7.0f, 43.7f, Hlp.contWhite, false),
                ("201_Blue",  4.2f, 44.0f,  7.0f, 48.2f, Hlp.contBlue,  false),
                ("201_BlueW", 7.2f, 48.8f, 10.8f, 51.5f, Hlp.contNavy,  false),
            };
            foreach (var (name, u0, v0, u1, v1, mat, hasSign) in specs)
            {
                var c = P.Group(group, name);
                float cu = (u0 + u1) / 2f, cv = (v0 + v1) / 2f;
                float du = u1 - u0, dv = v1 - v0;
                const float ch = 2.55f;
                Hlp.Box(c, "Roof", Hlp.At(cu, cv, ch), new Vector3(dv, .08f, du), mat, true);
                Hlp.Box(c, "WallU1", Hlp.At(u1, cv, ch / 2), new Vector3(dv, ch, .10f), mat, true);
                Hlp.Box(c, "WallV0", Hlp.At(cu, v0, ch / 2), new Vector3(.10f, ch, du), mat, true);
                Hlp.Box(c, "WallV1", Hlp.At(cu, v1, ch / 2), new Vector3(.10f, ch, du), mat, true);

                // Corridor-facing wall (u0 facing North toward 209 corridor) with frosted viewing window
                float winW = 1.5f;
                Hlp.Box(c, "FrontL", Hlp.At(u0, v0 + (dv - winW) / 4f, ch / 2), new Vector3((dv - winW) / 2f, ch, .10f), mat, true);
                Hlp.Box(c, "FrontR", Hlp.At(u0, v1 - (dv - winW) / 4f, ch / 2), new Vector3((dv - winW) / 2f, ch, .10f), mat, true);
                Hlp.Box(c, "FrontLow", Hlp.At(u0, cv, .32f), new Vector3(winW, .64f, .10f), mat, true);
                Hlp.Box(c, "FrontHigh", Hlp.At(u0, cv, 2.25f), new Vector3(winW, .60f, .10f), mat, true);
                Hlp.Box(c, "WindowGlass", Hlp.At(u0, cv, 1.30f), new Vector3(winW, 1.30f, .025f), Hlp.glass);

                for (float v = v0 + .25f; v < v1 - .2f; v += .35f)
                    Hlp.Box(c, "Rib", Hlp.At(u0 - .05f, v, ch / 2), new Vector3(.05f, ch, .04f), mat);

                var desk = P.Instance(c, "TwoSeatFoldingDesk", "Desk", Hlp.At(cu, cv, 0f));
                desk.localRotation = Quaternion.Euler(0, 90, 0);
                P.Instance(c, "BlueMeshChair", "Chair0", Hlp.At(cu + .55f, cv - .35f, 0f));
                P.Instance(c, "BlueMeshChair", "Chair1", Hlp.At(cu + .55f, cv + .35f, 0f));

                if (hasSign)
                {
                    var plaqueParent = Hlp.Face(c, "PlaqueMount", Hlp.At(u0 - .06f, v1, 0f), 90);
                    Hlp.Plaque(plaqueParent, "201", "학생창업보육실", "Startup Incubator", .4f);
                }
            }

            // Overhead mesh & sphere pendant lights along 201-209 corridor
            for (float v = 29f; v <= 48f; v += 1.8f)
                Hlp.Rect(group, "MeshStripV", new Hlp.R(0.2f, v, 7.0f, v + .035f), Hlp.H - .05f, .025f, Hlp.meshCeil);
            for (float u = 0.5f; u <= 6.8f; u += 1.6f)
                Hlp.Rect(group, "MeshStripU", new Hlp.R(u, 29f, u + .035f, 48f), Hlp.H - .05f, .025f, Hlp.meshCeil);
            for (float v = 31f; v <= 46f; v += 4.2f)
            {
                var p = Hlp.At(2.1f, v, 2.38f);
                P.Tube(group, "PendantCord", p + Vector3.up * .45f, p, .012f, Hlp.black);
                var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = "PendantSphere";
                UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());
                bulb.transform.SetParent(group, false);
                bulb.transform.localPosition = p;
                bulb.transform.localScale = Vector3.one * .26f;
                bulb.GetComponent<Renderer>().sharedMaterial = Hlp.lamp;
            }
        }

        static void LoungeAndFixtures(GameObject basement)
        {
            // 211 Lounge round tables + YONSEI / RC융합대학 logo wall (photo 111425776_05.jpg)
            var lobby211 = P.Group(details, "Lounge211");
            Hlp.AddRoundTableSet(lobby211, "Table211_A", 5.5f, 15.2f);
            Hlp.AddRoundTableSet(lobby211, "Table211_B", 7.8f, 15.2f);
            var sign211 = Hlp.Face(lobby211, "RCYonseiWallSign", Hlp.At(17.75f, 11.42f, 0f), -90);
            Hlp.Label(sign211, "RCTitle", "RC융합대학\nYONSEI", new Vector3(0, 1.35f, 0), new Vector2(4.2f, 1.4f), new Color(.032f, .052f, .10f), 90);

            // 204/205 Lounge round tables + high white table + graduate directional sign (photo 22)
            var lounge = P.Group(details, "Lounge204");
            Hlp.AddRoundTableSet(lounge, "RoundTable0", -11.5f, 45.2f);
            Hlp.AddRoundTableSet(lounge, "RoundTable1", -11.5f, 47.8f);

            var hi = Hlp.Face(lounge, "HighTable", Hlp.At(-13.3f, 45.5f), 180);
            Hlp.Box(hi, "Top", new Vector3(0, 1.02f, 0), new Vector3(1.5f, .04f, .75f), Hlp.white, true);
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                    Hlp.Box(hi, "Leg", new Vector3(sx * .68f, .51f, sz * .32f), new Vector3(.04f, 1.02f, .04f), Hlp.white);

            // Armchairs along 전기실 corridor wall (photo 21)
            Hlp.AddArmchairRow(lounge, "CorridorArmchairs206", -15.5f, 59.0f, 90, 4, .82f);

            // Standing graduate school banner at 201 T-junction (photo 111425776_01)
            var banner = Hlp.Face(details, "GraduateSignBanner", Hlp.At(0.4f, 40.3f), 90);
            Hlp.Box(banner, "StandBase", new Vector3(0, .02f, 0), new Vector3(.56f, .04f, .35f), Hlp.black);
            Hlp.Box(banner, "Board", new Vector3(0, .92f, 0), new Vector3(.56f, 1.70f, .03f), Hlp.navy, true);
            Hlp.Label(banner, "Text", "대학원 행정지원팀 (208호)\n대학원 미래혁신지원센터 (206호) →", new Vector3(0, 1.0f, -.02f), new Vector2(.50f, 1.2f), Color.white, 34);

            // Recycling station next to 201_Gray (photo 17)
            Hlp.AddVendingAndRecycling(basement, details, Hlp.At(5.5f, 33.8f), 90, includeVending: false);

            // Corridor downlights & fire extinguishers
            for (float v = 13f; v <= 40f; v += 3.5f) Hlp.Downlight(details, 2.1f, v);
            for (float u = -22f; u <= 2f; u += 3.5f) Hlp.Downlight(details, u, 57.8f);
            for (float u = -12f; u <= 0f; u += 3.5f) Hlp.Downlight(details, u, 41.5f);
            Hlp.FireExtinguisher(details, 7.6f, 26.2f);
            Hlp.FireExtinguisher(details, 0.5f, 49.0f);
            Hlp.FireExtinguisher(details, -13.2f, 57.0f);
        }
    }
}
