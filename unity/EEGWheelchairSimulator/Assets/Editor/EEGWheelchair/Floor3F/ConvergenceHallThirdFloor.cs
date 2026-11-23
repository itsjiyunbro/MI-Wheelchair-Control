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
            => u <= 16f && v >= 17f && v <= 46f;

        static void FloorAndRoof()
        {
            var domains = new[]
            {
                new Hlp.R(0, 0, 37, 26),
                new Hlp.R(0, 26, 21.5f, 66),
                new Hlp.R(21.5f, 52, 26, 66),
                new Hlp.R(26, 52, 36, 60.5f),
            };
            var holes = new[]
            {
                new Hlp.R(0, 39, 12.5f, 45.5f),   // 3F VOID
                new Hlp.R(15.5f, 49.5f, 20.3f, 53.2f), // 2F->3F open switchback stair void
                new Hlp.R(13, 18.4f, 17, 25.5f), // NorthStairwell shaft
                new Hlp.R(0, 59.4f, 3.6f, 66),   // SouthStairwell shaft
            };
            var uBreaks = new[] { 0f, 3.6f, 6f, 8f, 9f, 12.5f, 13f, 15.5f, 17f, 20.3f, 21.5f, 26f, 36f, 37f };
            var vBreaks = new[] { 0f, 17f, 18.4f, 25.5f, 26f, 39f, 45.5f, 46f, 49.5f, 52f, 53.2f, 57.5f, 59.4f, 60.5f, 66f };
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
                new Hlp.R(6, 0, 9, 17),
                new Hlp.R(16, 17, 37, 26),
                new Hlp.R(0, 17, 8, 28.5f),
                new Hlp.R(8, 25.5f, 12.5f, 28.5f),
                new Hlp.R(12.5f, 17, 16, 59.4f),
                new Hlp.R(16, 57.5f, 36, 60.5f),
                new Hlp.R(8.4f, 57.5f, 16, 59.4f),
                new Hlp.R(12.5f, 53, 16, 59.4f),
            };
            foreach (var r in whiteCeilings)
                Hlp.Rect(roofs, "WhiteCorridorCeiling", r, Hlp.H + .05f, .10f, Hlp.white);

            // Navy slatted atrium ceiling in front of 318 / 319 portal (photos 05, 06, 07)
            Hlp.Slats(roofs, new Hlp.R(12.5f, 17, 16f, 42f), Hlp.H, "3F_MainLobby");

            // Exterior walls & glass exit terraces
            Hlp.Wall(architecture, "NorthEastLiftGap", Hlp.At(0, 22.75f), 180, 11.5f, "3F");
            Hlp.Wall(architecture, "NorthVoidWall", Hlp.At(0, 42.25f), 180, 6.5f, "3F", "EXIT", "출입구", "Terrace Exit", glazed: true, doubleDoor: true, clearDoor: true);
            Hlp.Wall(architecture, "NorthWestGap", Hlp.At(0, 57.95f), 180, 2.9f, "3F");
            Hlp.Wall(architecture, "South301To305Wall", Hlp.At(21.5f, 39f), 0, 26f, "3F");
            Hlp.Wall(architecture, "EastWingSouthEnd", Hlp.At(37, 21.5f), 0, 9f, "3F");
            Hlp.Wall(architecture, "EastWingTerraceEdge", Hlp.At(29, 26), 90, 16f, "3F");
            Hlp.Wall(architecture, "WestWingInnerEnd", Hlp.At(28.75f, 52), -90, 14.5f, "3F");
            Hlp.Wall(architecture, "WestWingOuterEnd", Hlp.At(31f, 60.5f), 90, 10f, "3F");
            // Glass fire exit door at bottom of 306-312 corridor (photo 13)
            Hlp.Wall(architecture, "SouthWestCorridorExit", Hlp.At(36f, 59.0f), 0, 3.0f, "3F", "EXIT", "비상구", "Exit", glazed: true, doubleDoor: true, clearDoor: true);
            Hlp.Wall(architecture, "SouthWestRoomEnd", Hlp.At(36f, 55.35f), 0, 4.3f, "3F");

            // Brick portal framing at 319-322 corridor entrance (photo 06)
            Hlp.Box(architecture, "BrickPortalNorth", Hlp.At(3.0f, 17.0f, Hlp.H / 2), new Vector3(.18f, Hlp.H, 6.0f), Hlp.brick, true);
            Hlp.Box(architecture, "BrickPortalSouth", Hlp.At(12.5f, 17.0f, Hlp.H / 2), new Vector3(.18f, Hlp.H, 7.0f), Hlp.brick, true);

            // Void guards around 3F VOID and 2F->3F open stair landing (photo 09)
            Hlp.VoidGuard(details, new Hlp.R(0, 39, 12.5f, 45.5f), skipU0: true);
            Hlp.VoidGuard(details, new Hlp.R(15.5f, 49.5f, 20.3f, 53.2f), skipU0: true);
        }

        static void Rooms(GameObject basement)
        {
            // East wing (319..323, WC-E)
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "320", new Hlp.R(0, 0, 6, 6), "u1", "강의실", "Lecture Room");
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "319", new Hlp.R(0, 6, 6, 12), "u1", "강의실", "Lecture Room");
            Hlp.Room(architecture, roofs, "3F", "WC-E-W", new Hlp.R(0, 12, 6, 17), "u1", "여자 화장실", "Women");
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "322", new Hlp.R(9, 0, 16, 6), "u0", "강의실", "Lecture Room");
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "321", new Hlp.R(9, 6, 16, 12), "u0", "강의실", "Lecture Room");
            Hlp.Room(architecture, roofs, "3F", "WC-E-M", new Hlp.R(9, 12, 16, 17), "u0", "남자 화장실", "Men");
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "323", new Hlp.R(20, 0, 37, 17), "v1", "강의실", "Lecture Room", doubleDoor: true);

            // Center 318 & 301..305 Lecture Rooms
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "318", new Hlp.R(0, 28.5f, 12.5f, 38.5f), "u1", "강의실", "Lecture Room", doubleDoor: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "301", new Hlp.R(16, 26, 21, 30), "u0", "강의실", "Lecture Room", glazed: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "302", new Hlp.R(16, 30, 21, 35.5f), "u0", "강의실", "Lecture Room", glazed: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "303", new Hlp.R(16, 35.5f, 21, 40.5f), "u0", "강의실", "Lecture Room", glazed: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "304", new Hlp.R(16, 40.5f, 21, 45), "u0", "강의실", "Lecture Room", glazed: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "305", new Hlp.R(16, 45, 21, 49), "u0", "강의실", "Lecture Room", glazed: true);

            // North-West wing (316, 315, 314 PC Labs; 313 enclosed utility/core behind solid corridor wall)
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "316", new Hlp.R(0, 46, 12.5f, 49.5f), "u1", "PC실습실", "PC Lab", glazed: true, computers: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "315", new Hlp.R(0, 49.5f, 12.5f, 53.0f), "u1", "PC실습실", "PC Lab", glazed: true, computers: true);
            Hlp.TeachingRoom(architecture, roofs, details, "3F", "314", new Hlp.R(0, 53.0f, 12.5f, 56.5f), "u1", "PC실습실", "PC Lab", glazed: true, computers: true);
            Hlp.Room(architecture, roofs, "3F", "313", new Hlp.R(8.4f, 59.4f, 13, 66), "v0", "설비실", "Utility Room");

            // West Restrooms & 306..312 Project Rooms (photos 11, 12, 13)
            Hlp.Room(architecture, roofs, "3F", "WC-W-M", new Hlp.R(14, 60.5f, 20, 66), "v0", "남자 화장실", "Men");
            Hlp.Room(architecture, roofs, "3F", "WC-W-W", new Hlp.R(20, 60.5f, 26, 66), "v0", "여자 화장실", "Women");

            for (int i = 0; i < 7; i++)
            {
                string code = (306 + i).ToString();
                float u0 = 17f + i * (19f / 7f);
                float u1 = 17f + (i + 1) * (19f / 7f);
                Hlp.Room(architecture, roofs, "3F", code, new Hlp.R(u0, 53.2f, u1, 57.5f), "v1", "프로젝트실", "SW Project Room", glaze: true);
            }

            Hlp.MatchBasementRestrooms(basement, architecture, "3F",
                new[] { "WC-E-W", "WC-E-M", "WC-W-M", "WC-W-W" },
                new[] { new Hlp.R(0, 12, 6, 17), new Hlp.R(9, 12, 16, 17), new Hlp.R(14, 60.5f, 20, 66), new Hlp.R(20, 60.5f, 26, 66) },
                new[] { "U1", "U0", "V0", "V0" });

            // East & West Elevator Halls + Stairwells (aligned vertically with B1, 1F, 2F)
            Hlp.CopyNorthElevatorHall(basement, architecture, roofs, details, "3F", "3");
            Hlp.BuildNorthStairwellShell(details, "3F", new Hlp.R(13, 18.4f, 17, 25.5f), withCeiling: true);
            Hlp.CopySouthElevatorAndStairwell(basement, architecture, details, "3F", "3", buildCeiling: true);
        }

        static void Fixtures(GameObject basement)
        {
            // 2 Vending machines + Recycling station near 318/319 lobby (photo 05 & 06)
            Hlp.AddVendingAndRecycling(basement, details, Hlp.At(18.5f, 18.2f), 90, includeVending: true);

            // Folding exhibition tables outside 318 (photo 05)
            for (int i = 0; i < 3; i++)
            {
                var table = Hlp.Face(details, "LobbyTable" + i, Hlp.At(13.1f, 29.5f + i * 2.4f), 0);
                Hlp.Box(table, "Top", new Vector3(0, .74f, 0), new Vector3(1.8f, .04f, .70f), Hlp.white, true);
                foreach (int sx in new[] { -1, 1 })
                    Hlp.Box(table, "Leg" + sx, new Vector3(sx * .82f, .37f, 0), new Vector3(.04f, .74f, .60f), Hlp.silver);
            }

            // Downlights & fire extinguishers
            for (float v = 18f; v <= 58f; v += 3.5f) Hlp.Downlight(details, 14.2f, v);
            for (float u = 18f; u <= 35f; u += 3.5f) Hlp.Downlight(details, u, 59.0f);
            for (float v = 2f; v <= 16f; v += 3.5f) Hlp.Downlight(details, 7.5f, v);
            Hlp.FireExtinguisher(details, 15.3f, 25.8f);
            Hlp.FireExtinguisher(details, 15.3f, 52.8f);
            Hlp.FireExtinguisher(details, 26.5f, 60.0f);
        }
    }
}
