using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using P = EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;
using Hlp = EEGWheelchairSimulator.Editor.ConvergenceHallUpperFloorHelpers;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallThirdFloor
    {
        public const string Prefab = "Assets/Art/Prefabs/Environment/ConvergenceHall/ConvergenceHall3F.prefab";

        static Transform root, architecture, roofs, details;

        public static GameObject BuildPrefab(GameObject basement)
        {
            Hlp.LoadMaterials();
            var go = new GameObject("ConvergenceHall3F");
            root = go.transform;
            architecture = P.Group(root, "RoomsAndCores");
            roofs = P.Group(root, "Ceilings");
            details = P.Group(root, "Details");

            FloorAndRoof();
            Rooms(basement);
            Fixtures(basement);

            foreach (var section in new[] { root.Find("Floors"), roofs })
                P.Combine(section, "3F_" + section.name);

            var asset = PrefabUtility.SaveAsPrefabAsset(go, Prefab);
            UnityEngine.Object.DestroyImmediate(go);
            return asset;
        }

        static bool IsAtrium(float u, float v)
            => (u >= -15.5f && u <= -6.5f && v >= 17.0f && v <= 52.0f)
            || (u >= -6.5f && u <= 18.5f && v >= 17.0f && v <= 29.0f);

        static void FloorAndRoof()
        {
            var domains = new[]
            {
                new Hlp.R(-22.0f, 17.0f, -6.5f, 56.5f), // 316 terrace, Void3F, 318-1, 318-2, main corridor, 301..305, OpenStair
                new Hlp.R(-19.5f,  0.0f, -5.0f, 17.0f), // 319..322, WC-E-W, WC-E-M, East corridor
                new Hlp.R(-20.0f, 56.5f, 16.0f, 66.0f), // 314..316, 313, WestStair, WestLift, WC-W-M, WC-W-W
                new Hlp.R(-14.5f, 48.0f, 16.0f, 56.5f), // 306..312, West corridor, Open2F3FStair
                new Hlp.R(-14.5f,  0.0f, 18.5f, 29.0f), // East lobby, EastLift, EastStair, 323
            };
            var holes = new[]
            {
                new Hlp.R(-22.0f, 39.5f, -14.0f, 48.5f), // 3F VOID
                new Hlp.R(-10.25f, 46.8f, -6.75f, 51.5f), // Open2F3FStair vertical opening
                new Hlp.R( 13.0f, 18.4f,  17.0f, 25.5f), // NorthStairwell shaft
                new Hlp.R(  0.0f, 59.4f,   3.6f, 66.0f), // SouthStairwell shaft
            };
            var uBreaks = new[] { -22f, -20f, -19.5f, -15.5f, -14.5f, -14f, -10.5f, -10.25f, -6.75f, -6.5f, -5f, 0f, 3.6f, 13f, 16f, 17f, 18.5f };
            var vBreaks = new[] { 0f, 17f, 18.4f, 25.5f, 29f, 39.5f, 46.8f, 48f, 48.5f, 51.5f, 52f, 56.5f, 59.4f, 66f };
            var floors = P.Group(root, "Floors");
            for (int i = 0; i < uBreaks.Length - 1; i++)
            {
                for (int j = 0; j < vBreaks.Length - 1; j++)
                {
                    float u = (uBreaks[i] + uBreaks[i + 1]) / 2f;
                    float v = (vBreaks[j] + vBreaks[j + 1]) / 2f;
                    if (!domains.Any(r => r.Contains(u, v)) || holes.Any(r => r.Contains(u, v))) continue;
                    var rect = new Hlp.R(uBreaks[i], vBreaks[j], uBreaks[i + 1], vBreaks[j + 1]);
                    var slab = Hlp.Rect(floors, $"Floor_{i}_{j}", rect, -.09f, .18f, IsAtrium(u, v) ? Hlp.stone : Hlp.tile);
                    slab.gameObject.AddComponent<BoxCollider>();
                }
            }

            var whiteCeilings = new[]
            {
                new Hlp.R(-14f, 0f, -11f, 17f),         // 319..322 East corridor
                new Hlp.R(-15.5f, 48.5f, -10.5f, 56.5f),// North-West corridor by 315/316
                new Hlp.R(-10.5f, 51.5f, 16f, 56.5f),   // West corridor by 314..WC-W-W & 306..312
                new Hlp.R(-4f, 56.5f, 0f, 59.4f),       // Outside 313
                new Hlp.R(0f, 56.5f, 8.4f, 59.4f),      // Outside WestStair/WestLift
                new Hlp.R(8.4f, 56.5f, 16f, 59.4f),     // Outside WC-W
                new Hlp.R(-6.5f, 17f, 18.5f, 29f),      // East lobby past EastLift/EastStair to 323
                new Hlp.R(-15.5f, 17f, -10.5f, 20f),    // North-East Brick Portal approach
            };
            foreach (var r in whiteCeilings)
                Hlp.Rect(roofs, "WhiteCorridorCeiling", r, Hlp.H + .05f, .10f, Hlp.white);

            // Navy slatted atrium ceiling along main 318 / 301..305 corridor (photos 04, 05, 06, 07)
            Hlp.Slats(roofs, new Hlp.R(-14f, 20f, -10.5f, 48.5f), Hlp.H, "3F_MainLobby");
            Hlp.Slats(roofs, new Hlp.R(-10.5f, 17f, -6.5f, 29f), Hlp.H, "3F_EastLobbyHead");

            // Watertight exterior perimeter & terrace walls (no open exterior doors or drop-offs)
            Hlp.LockedExitFacade(architecture, "NorthWestTerraceExit", Hlp.At(-15.5f, 52.5f), 180, 8.0f, "3F", "출입구");
            Hlp.LockedExitFacade(architecture, "VoidTerraceGlazing", Hlp.At(-14.0f, 44.0f), 180, 9.0f, "3F", "테라스");
            Hlp.Wall(architecture, "NorthVoidEastStep", Hlp.At(-14.75f, 39.5f), 90, 1.5f, "3F");
            Hlp.Wall(architecture, "East318SideReturn", Hlp.At(-14.75f, 20.0f), -90, 1.5f, "3F");
            Hlp.LockedExitFacade(architecture, "EastMainCorridorExit", Hlp.At(-15.5f, 18.5f), 180, 3.0f, "3F", "비상구");
            Hlp.Wall(architecture, "East319ToExitStep", Hlp.At(-14.75f, 17.0f), 90, 1.5f, "3F");
            Hlp.Wall(architecture, "East320CorridorEnd", Hlp.At(-12.5f, 0.0f), -90, 3.0f, "3F");
            Hlp.Wall(architecture, "WCEMToEastLobbyStep", Hlp.At(-5.0f, 17.7f), 0, 1.4f, "3F");
            Hlp.Wall(architecture, "EastLobbyToTerraceWall", Hlp.At(-0.75f, 18.4f), -90, 8.5f, "3F");
            Hlp.Wall(architecture, "SouthEastStairFill", Hlp.At(17.75f, 18.4f), -90, 1.5f, "3F");
            Hlp.LockedExitFacade(architecture, "EastLiftSouthExit", Hlp.At(18.5f, 23.7f), 0, 10.6f, "3F", "비상구");
            Hlp.Wall(architecture, "EastLobbyCourtyardGlass", Hlp.At(6.0f, 29.0f), 90, 25.0f, "3F", glazed: true);
            Hlp.Wall(architecture, "OpenStair306Step", Hlp.At(-6.5f, 47.4f), 0, 1.2f, "3F");
            Hlp.LockedExitFacade(architecture, "SouthWestCorridorExit", Hlp.At(16.0f, 55.7f), 0, 7.4f, "3F", "비상구");

            // Brick portal framing at 319-322 corridor entrance (photo 06)
            Hlp.Box(architecture, "BrickPortalNorth", Hlp.At(-16.75f, 17.0f, Hlp.H / 2), new Vector3(.18f, Hlp.H, 5.5f), Hlp.brick, true);
            Hlp.Box(architecture, "BrickPortalSouth", Hlp.At(-8.0f, 17.0f, Hlp.H / 2), new Vector3(.18f, Hlp.H, 6.0f), Hlp.brick, true);

            // Void guards around 3F VOID and Open2F3FStair landing opening (photo 09)
            Hlp.VoidGuard(details, new Hlp.R(-22.0f, 39.5f, -14.0f, 48.5f));
            Hlp.VoidGuard(details, new Hlp.R(-10.25f, 46.8f, -6.75f, 51.5f), skipV0: true);
        }

        static void Rooms(GameObject basement)
        {
            // East wing (319..323, WC-E)
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "320", new Hlp.R(-19.5f, 0.0f, -14.0f, 5.8f), "u1", "강의실", "Lecture Room");
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "319", new Hlp.R(-19.5f, 5.8f, -14.0f, 11.6f), "u1", "강의실", "Lecture Room");
            Hlp.Room(architecture, roofs, "3F", "WC-E-W", new Hlp.R(-19.5f, 11.6f, -14.0f, 17.0f), "u1", "여자 화장실", "Women");
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "322", new Hlp.R(-11.0f, 0.0f, -5.0f, 5.8f), "u0", "강의실", "Lecture Room");
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "321", new Hlp.R(-11.0f, 5.8f, -5.0f, 11.6f), "u0", "강의실", "Lecture Room");
            Hlp.Room(architecture, roofs, "3F", "WC-E-M", new Hlp.R(-11.0f, 11.6f, -5.0f, 17.0f), "u0", "남자 화장실", "Men");
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "323", new Hlp.R(3.5f, 0.0f, 18.5f, 18.4f), "v1", "강의실", "Lecture Room", doubleDoor: true);

            // Center 318-1 (VOID side, 2 single doors) & 318-2 (Exit side, 1 single + 1 double door) + 301..305
            BuildSplit318("318-2", 20.0f, 29.5f, x0: -2.6f, double0: true,  x1: 2.2f, double1: false);
            BuildSplit318("318-1", 29.5f, 39.5f, x0: -2.4f, double0: false, x1: 2.4f, double1: false);

            Hlp.Room(architecture, roofs, "3F", "301", new Hlp.R(-10.5f, 25.0f, -6.5f, 28.5f), "u0", "창고", "Storage", glaze: false);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "302", new Hlp.R(-10.5f, 28.5f, -6.5f, 34.0f), "u0", "강의실", "Lecture Room", glazed: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "303", new Hlp.R(-10.5f, 34.0f, -6.5f, 38.5f), "u0", "SW Help Desk", "SW Help Desk", glazed: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "304", new Hlp.R(-10.5f, 38.5f, -6.5f, 43.0f), "u0", "워크샵실", "Workshop Room", glazed: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "305", new Hlp.R(-10.5f, 43.0f, -6.5f, 46.8f), "u0", "프로젝트실", "SW Project Room", glazed: true);

            // North-West wing (316, 315, 314 PC Labs; 313 storage; WC-W)
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "316", new Hlp.R(-20.0f, 56.5f, -15.0f, 66.0f), "v0", "PC실습실", "PC Lab", glazed: true, computers: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "315", new Hlp.R(-15.0f, 56.5f, -10.0f, 66.0f), "v0", "PC실습실", "PC Lab", glazed: true, computers: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "314", new Hlp.R(-10.0f, 56.5f,  -4.0f, 66.0f), "v0", "PC실습실", "PC Lab", glazed: true, computers: true);
            Hlp.Room(architecture, roofs, "3F", "313", new Hlp.R(-4.0f, 56.5f, 0.0f, 66.0f), "v0", "창고", "Storage");
            Hlp.Room(architecture, roofs, "3F", "WC-W-M", new Hlp.R(8.4f, 59.4f, 12.2f, 66.0f), "v0", "남자 화장실", "Men");
            Hlp.Room(architecture, roofs, "3F", "WC-W-W", new Hlp.R(12.2f, 59.4f, 16.0f, 66.0f), "v0", "여자 화장실", "Women");

            // West 306..312 Project Rooms along West corridor (photos 11, 12, 13)
            for (int i = 0; i < 7; i++)
            {
                string code = (306 + i).ToString();
                float u0 = -6.5f + i * (22.5f / 7f);
                float u1 = -6.5f + (i + 1) * (22.5f / 7f);
                Hlp.Room(architecture, roofs, "3F", code, new Hlp.R(u0, 48.0f, u1, 52.0f), "v1", "프로젝트실", "SW Project Room", glaze: true);
            }

            Hlp.MatchBasementRestrooms(basement, architecture, "3F",
                new[] { "WC-E-W", "WC-E-M", "WC-W-M", "WC-W-W" },
                new[] { new Hlp.R(-19.5f, 11.6f, -14f, 17f), new Hlp.R(-11f, 11.6f, -5f, 17f), new Hlp.R(8.4f, 59.4f, 12.2f, 66f), new Hlp.R(12.2f, 59.4f, 16f, 66f) },
                new[] { "U1", "U0", "V0", "V0" });

            // East & West Elevator Halls + Stairwells (aligned vertically with B1, 1F, 2F)
            Hlp.CopyNorthElevatorHall(basement, architecture, roofs, details, "3F", "3");
            Hlp.BuildNorthStairwellShell(details, "3F", new Hlp.R(13, 18.4f, 17, 25.5f), withCeiling: true);
            Hlp.CopySouthElevatorAndStairwell(basement, architecture, details, "3F", "3", buildCeiling: true);
        }

        static void Fixtures(GameObject basement)
        {
            // 2 Vending machines + Recycling station at East Lobby (photos 04, 05, 06)
            Hlp.AddVendingAndRecycling(basement, details, Hlp.At(-1.5f, 19.0f), 90, includeVending: true);

            // Folding exhibition tables outside 318 (photo 05)
            foreach (float tv in new[] { 24.5f, 34.5f })
            {
                var table = Hlp.Face(details, "LobbyTable_" + tv, Hlp.At(-13.4f, tv), 0);
                Hlp.Box(table, "Top", new Vector3(0, .74f, 0), new Vector3(1.05f, .04f, .65f), Hlp.white, true);
                foreach (int sx in new[] { -1, 1 })
                    Hlp.Box(table, "Leg" + sx, new Vector3(sx * .46f, .37f, 0), new Vector3(.04f, .74f, .55f), Hlp.silver);
            }

            // Downlights & fire extinguishers
            for (float v = 18f; v <= 54f; v += 3.5f) Hlp.Downlight(details, -12.25f, v);
            for (float u = -14f; u <= 15f; u += 3.5f) Hlp.Downlight(details, u, 54.2f);
            for (float u = -5f; u <= 17f; u += 3.5f) Hlp.Downlight(details, u, 27.2f);
            for (float v = 2f; v <= 16f; v += 3.5f) Hlp.Downlight(details, -12.5f, v);
            Hlp.FireExtinguisher(details, -6.2f, 45.8f);
            Hlp.FireExtinguisher(details, 15.3f, 26.2f);
            Hlp.FireExtinguisher(details, 15.2f, 58.6f);
        }

        static void BuildSplit318(string code, float v0, float v1, float x0, bool double0, float x1, bool double1)
        {
            Hlp.TeachingRoom(architecture, roofs, details, "3F", code, new Hlp.R(-22.0f, v0, -14.0f, v1), "u1", "강의실", "Lecture Room");
            var room = architecture.Find("Room" + code);
            var oldU1 = room.Find("U1");
            if (oldU1) UnityEngine.Object.DestroyImmediate(oldU1.gameObject);

            float len = v1 - v0;
            float w0 = double0 ? 1.7f : 1.0f, h0 = w0 / 2 + .05f;
            float w1 = double1 ? 1.7f : 1.0f, h1 = w1 / 2 + .05f;
            var u1 = Hlp.Face(room, "U1", Hlp.At(-14.0f, (v0 + v1) / 2), 0);
            Hlp.Panel(u1, "Left", -len / 2, x0 - h0, false);
            Hlp.Panel(u1, "Middle", x0 + h0, x1 - h1, false);
            Hlp.Panel(u1, "Right", x1 + h1, len / 2, false);

            var d0 = P.Group(u1, "Door_" + code + "_A");
            d0.localPosition = new Vector3(x0, 0, 0);
            Hlp.Door(d0, "3F", code, "강의실", "Lecture Room", w0);

            var d1 = P.Group(u1, "Door_" + code + "_B");
            d1.localPosition = new Vector3(x1, 0, 0);
            Hlp.Door(d1, "3F", code, "강의실", "Lecture Room", w1);
        }
    }
}
