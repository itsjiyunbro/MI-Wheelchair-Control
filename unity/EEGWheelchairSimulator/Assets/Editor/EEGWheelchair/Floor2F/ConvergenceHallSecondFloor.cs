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
            => (u <= 15.2f && v >= 25.5f && v <= 42f) || (u >= 15.2f && u <= 22f && v >= 26f && v <= 53f);

        static void FloorAndRoof()
        {
            var domains = new[]
            {
                new Hlp.R(0, 0, 27, 36),
                new Hlp.R(0, 35, 22, 66),
                new Hlp.R(22, 26, 42, 40),
                new Hlp.R(22, 46, 42, 66),
                new Hlp.R(20, 40, 27, 46),
            };
            var holes = new[]
            {
                new Hlp.R(3.6f, 29, 14.8f, 34.616f), // 1F->2F UpperBluebell stair void
                new Hlp.R(13, 18.4f, 17, 25.5f),     // NorthStairwell shaft
                new Hlp.R(0, 59.4f, 3.6f, 66),       // SouthStairwell shaft
            };
            var uBreaks = new[] { 0f, 3.6f, 8f, 13f, 14.8f, 15.2f, 17f, 20f, 22f, 27f, 42f };
            var vBreaks = new[] { 0f, 13f, 18.4f, 25.5f, 26f, 29f, 34.616f, 35f, 36f, 40f, 46f, 49f, 53f, 59.4f, 66f };
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

            // Corridor ceilings (keep UpperBluebell stair opening open vertically)
            var whiteCeilings = new[]
            {
                new Hlp.R(20, 0, 22, 18),
                new Hlp.R(14, 15, 22, 18.4f),
                new Hlp.R(0, 21, 8, 29),
                new Hlp.R(8, 25.5f, 15, 29),
                new Hlp.R(0, 34.616f, 15, 41),
                new Hlp.R(8, 41, 15.2f, 59.4f),
                new Hlp.R(18.2f, 18, 22, 53),
                new Hlp.R(14.5f, 53, 22, 59.4f),
                new Hlp.R(22, 52.5f, 42, 56),
                new Hlp.R(22, 40, 27, 48),
                new Hlp.R(27, 40, 29, 46),
            };
            foreach (var r in whiteCeilings)
                Hlp.Rect(roofs, "WhiteCorridorCeiling", r, Hlp.H + .05f, .10f, Hlp.white);

            // Navy slatted atrium ceiling zones (matching 02.jpg, 03.jpg, 04.jpg)
            Hlp.Slats(roofs, new Hlp.R(15, 25.5f, 22, 30), Hlp.H, "2F_LobbyNorth");
            Hlp.Slats(roofs, new Hlp.R(15, 48.5f, 22, 53.5f), Hlp.H, "2F_LobbyStair");

            // Exterior perimeter walls where rooms do not already enclose the footprint
            Hlp.Wall(architecture, "NorthAtriumOuter", Hlp.At(0, 31f), 180, 20f, "2F");
            Hlp.Wall(architecture, "North202To203Gap", Hlp.At(0, 49.5f), 180, 1f, "2F");
            Hlp.Wall(architecture, "North202To203Inner", Hlp.At(8, 49.5f), 0, 1f, "2F");
            Hlp.Wall(architecture, "SouthEastCorridorEnd", Hlp.At(27, 15.5f), 0, 5f, "2F");
            Hlp.Wall(architecture, "NotchEastReturn", Hlp.At(28, 46), -90, 2f, "2F");
            Hlp.Wall(architecture, "NotchWestEdge", Hlp.At(24.5f, 40), -90, 5f, "2F");
            Hlp.Wall(architecture, "WestCorridorSouthEnd", Hlp.At(42, 54.25f), 0, 3.5f, "2F");

            // Courtyard glass facade & exit double doors at u=27, v=40..46 (photo 22 & 24)
            Hlp.Wall(architecture, "CourtyardGlassExit", Hlp.At(27, 43f), 0, 6f, "2F", "EXIT", "출입구", "Exit", glazed: true, doubleDoor: true, clearDoor: true);

            // Void guards around UpperBluebell 1F->2F opening (u=3.6..14.8, v=29..34.616; u=3.6 is arrival edge)
            Hlp.VoidGuard(details, new Hlp.R(3.6f, 29, 14.8f, 34.616f), skipU0: true);
        }

        static void Rooms(GameObject basement)
        {
            // East wing (v=0..21)
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "219", new Hlp.R(0, 0, 6, 5), "u1");
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "218", new Hlp.R(0, 5, 6, 10), "u1");
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "217", new Hlp.R(0, 10, 6, 15), "u1");
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "216", new Hlp.R(0, 15, 6.5f, 21), "v1", glazed: true);
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "215", new Hlp.R(6, 0, 14, 15), "v1", glazed: true, doubleDoor: true);
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "214", new Hlp.R(14, 0, 20, 5), "u1");
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "213", new Hlp.R(14, 5, 20, 10), "u1");
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "212", new Hlp.R(14, 10, 20, 15), "u1");
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "210", new Hlp.R(22, 0, 27, 5), "u0");
            Hlp.Room(architecture, roofs, "2F", "WC-E-M", new Hlp.R(22, 5, 27, 9), "u0", "남자 화장실", "Men");
            Hlp.Room(architecture, roofs, "2F", "WC-E-W", new Hlp.R(22, 9, 27, 13), "u0", "여자 화장실", "Women");

            // Center & South-Center
            Hlp.Room(architecture, roofs, "2F", "209", new Hlp.R(22, 18, 27, 32f), "u0", "커리어플래닝룸", "Career Planning Room", glaze: true, doorOffset: -5.5f);
            Hlp.Room(architecture, roofs, "2F", "소화용수", new Hlp.R(27, 26, 32, 34), "u0", "소화용수", "Fire Water", doorOffset: 2.6f);
            Hlp.Room(architecture, roofs, "2F", "물탱크실", new Hlp.R(32, 26, 42, 34), "v0", "물탱크실", "Water Tank");
            Hlp.Room(architecture, roofs, "2F", "기계실", new Hlp.R(27, 34, 42, 40), "u0", "기계실", "Mechanical Room", doorOffset: 1.0f);

            // North-West wing (202, 203, 203-1, West WC)
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "202", new Hlp.R(0, 41, 8, 49), "u1", glazed: true);
            Hlp.TeachingRoom(architecture, roofs, details, "2F", "203", new Hlp.R(0, 50, 8, 59.4f), "u1", doubleDoor: true);
            Hlp.Room(architecture, roofs, "2F", "203-1", new Hlp.R(8.4f, 59.4f, 14.5f, 66), "v0");
            Hlp.Room(architecture, roofs, "2F", "WC-W-W", new Hlp.R(14.5f, 59.4f, 19, 66), "v0", "여자 화장실", "Women");
            Hlp.Room(architecture, roofs, "2F", "WC-W-M", new Hlp.R(19, 59.4f, 23.5f, 66), "v0", "남자 화장실", "Men");

            // South-West wing (204..208, 전기실, 방재실, MDF실)
            Hlp.Room(architecture, roofs, "2F", "204", new Hlp.R(22.8f, 46.0f, 27.5f, 52.2f), "u0", "미래캠퍼스 직원노동조합", "Staff Union", glaze: true, doorOffset: 1.7f);
            Hlp.Room(architecture, roofs, "2F", "205", new Hlp.R(27.5f, 56, 32.0f, 61), "v0", "대학원 문서보존실", "Graduate Archive");
            Hlp.Room(architecture, roofs, "2F", "206", new Hlp.R(29, 46, 33, 52.5f), "v1", "대학원 미래혁신지원센터", "Future Innovation Center");
            Hlp.Room(architecture, roofs, "2F", "207", new Hlp.R(33, 46, 37.5f, 52.5f), "v1", "대학원 교학처장실", "Dean of Graduate School");
            Hlp.Room(architecture, roofs, "2F", "208", new Hlp.R(37.5f, 46, 42, 52.5f), "v1", "대학원 행정팀", "Graduate School Admin");
            Hlp.Room(architecture, roofs, "2F", "전기실", new Hlp.R(23.5f, 61, 36, 66), "v0", "전기실", "Electrical Room", doorOffset: -4.0f);
            Hlp.Room(architecture, roofs, "2F", "방재실", new Hlp.R(36, 56, 42, 61), "v0", "방재실", "Disaster Prevention");
            Hlp.Room(architecture, roofs, "2F", "MDF실", new Hlp.R(36, 61, 42, 66), "u0", "MDF실", "MDF Room");

            // Match all 4 restrooms to B1 milky-glass doors & handles
            Hlp.MatchBasementRestrooms(basement, architecture, "2F",
                new[] { "WC-E-M", "WC-E-W", "WC-W-W", "WC-W-M" },
                new[] { new Hlp.R(22, 5, 27, 9), new Hlp.R(22, 9, 27, 13), new Hlp.R(14.5f, 59.4f, 19, 66), new Hlp.R(19, 59.4f, 23.5f, 66) },
                new[] { "U0", "U0", "V0", "V0" });

            // East & West Elevator Halls + Stairwells (aligned vertically with B1 & 1F)
            Hlp.CopyNorthElevatorHall(basement, architecture, roofs, details, "2F", "2");
            Hlp.BuildNorthStairwellShell(details, "2F", new Hlp.R(13, 18.4f, 17, 25.5f), withCeiling: false);
            Hlp.CopySouthElevatorAndStairwell(basement, architecture, details, "2F", "2", buildCeiling: false);

            // Glass double partition door across 201 corridor (photo 111425776_01.jpg, default open)
            var part = Hlp.Face(architecture, "Corridor201GlassPartition", Hlp.At(20.5f, 48.8f), 90);
            Hlp.Panel(part, "Left", -2.3f, -.9f, true);
            Hlp.Panel(part, "Right", .9f, 2.3f, true);
            var partDoor = P.Group(part, "Door_201Partition");
            Hlp.Door(partDoor, "2F", "201-P", "", "", 1.7f, clear: true, glazedWall: true, addPlaque: false, initial: 1f);
            Hlp.ExitSign(partDoor, new Vector3(0, 2.65f, -.035f));
        }

        static void Containers201()
        {
            var group = P.Group(details, "Containers201");
            var specs = new[]
            {
                ("201_Gray", 15.2f, 30.0f, 18.2f, 36.0f, Hlp.contGray, true),
                ("201_Blue1", 15.2f, 36.5f, 18.2f, 42.5f, Hlp.contBlue, false),
                ("201_Blue2", 15.2f, 43.0f, 18.2f, 48.5f, Hlp.contNavy, false),
            };
            foreach (var (name, u0, v0, u1, v1, mat, hasSign) in specs)
            {
                var c = P.Group(group, name);
                float cu = (u0 + u1) / 2f, cv = (v0 + v1) / 2f;
                float du = u1 - u0, dv = v1 - v0;
                const float ch = 2.55f;
                // Floor, roof, side walls with corrugated ribs & viewing windows
                Hlp.Box(c, "Roof", Hlp.At(cu, cv, ch), new Vector3(dv, .08f, du), mat, true);
                Hlp.Box(c, "WallU0", Hlp.At(u0, cv, ch / 2), new Vector3(dv, ch, .10f), mat, true);
                Hlp.Box(c, "WallV0", Hlp.At(cu, v0, ch / 2), new Vector3(.10f, ch, du), mat, true);
                Hlp.Box(c, "WallV1", Hlp.At(cu, v1, ch / 2), new Vector3(.10f, ch, du), mat, true);

                // Corridor-facing wall (u1) with frosted window cutout
                float winW = 1.6f;
                Hlp.Box(c, "FrontL", Hlp.At(u1, v0 + (dv - winW) / 4f, ch / 2), new Vector3((dv - winW) / 2f, ch, .10f), mat, true);
                Hlp.Box(c, "FrontR", Hlp.At(u1, v1 - (dv - winW) / 4f, ch / 2), new Vector3((dv - winW) / 2f, ch, .10f), mat, true);
                Hlp.Box(c, "FrontLow", Hlp.At(u1, cv, .32f), new Vector3(winW, .64f, .10f), mat, true);
                Hlp.Box(c, "FrontHigh", Hlp.At(u1, cv, 2.25f), new Vector3(winW, .60f, .10f), mat, true);
                Hlp.Box(c, "WindowGlass", Hlp.At(u1, cv, 1.30f), new Vector3(winW, 1.30f, .025f), Hlp.glass);

                // Vertical corrugated steel ribs along front/back walls
                for (float v = v0 + .25f; v < v1 - .2f; v += .35f)
                    Hlp.Box(c, "Rib", Hlp.At(u0 - .05f, v, ch / 2), new Vector3(.05f, ch, .04f), mat);

                // Interior desk & chairs visible inside container (photo 28)
                var desk = P.Instance(c, "TwoSeatFoldingDesk", "Desk", Hlp.At(cu, cv, 0f));
                desk.localRotation = Quaternion.Euler(0, 90, 0);
                P.Instance(c, "BlueMeshChair", "Chair0", Hlp.At(cu - .55f, cv - .35f, 0f));
                P.Instance(c, "BlueMeshChair", "Chair1", Hlp.At(cu - .55f, cv + .35f, 0f));

                if (hasSign)
                {
                    var plaqueParent = Hlp.Face(c, "PlaqueMount", Hlp.At(u1 + .06f, v1, 0f), 90);
                    Hlp.Plaque(plaqueParent, "201", "학생창업보육실", "Startup Incubator", -.4f);
                }
            }

            // Overhead black mesh grid & sphere pendant lights above 201 area (photo 111425776 & _02)
            for (float v = 29f; v <= 50f; v += 1.8f)
                Hlp.Rect(group, "MeshStripV", new Hlp.R(14.8f, v, 21.8f, v + .035f), Hlp.H - .05f, .025f, Hlp.meshCeil);
            for (float u = 15.2f; u <= 21.6f; u += 1.6f)
                Hlp.Rect(group, "MeshStripU", new Hlp.R(u, 29f, u + .035f, 50f), Hlp.H - .05f, .025f, Hlp.meshCeil);
            for (float v = 31f; v <= 48f; v += 4.2f)
            {
                var p = Hlp.At(19.5f, v, 2.38f);
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
            // 204/205 Lounge round tables + chairs + high table (photo 22)
            var lounge = P.Group(details, "Lounge204");
            Hlp.AddRoundTableSet(lounge, "RoundTable0", 23.2f, 42.5f);
            Hlp.AddRoundTableSet(lounge, "RoundTable1", 25.0f, 44.5f);

            // High white table at West wall of lounge
            var hi = Hlp.Face(lounge, "HighTable", Hlp.At(21.8f, 44.5f), 0);
            Hlp.Box(hi, "Top", new Vector3(0, 1.02f, 0), new Vector3(1.5f, .04f, .75f), Hlp.white, true);
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                    Hlp.Box(hi, "Leg", new Vector3(sx * .68f, .51f, sz * .32f), new Vector3(.04f, 1.02f, .04f), Hlp.white);

            // 4 wooden armchairs along 전기실 corridor wall (photo 21)
            Hlp.AddArmchairRow(lounge, "CorridorArmchairs206", 29.0f, 55.3f, 90, 4, .82f);

            // Standing graduate school banner outside 201 (photo 111425776_01)
            var banner = Hlp.Face(details, "GraduateSignBanner", Hlp.At(21.4f, 47.2f), 90);
            Hlp.Box(banner, "StandBase", new Vector3(0, .02f, 0), new Vector3(.56f, .04f, .35f), Hlp.black);
            Hlp.Box(banner, "Board", new Vector3(0, .92f, 0), new Vector3(.56f, 1.70f, .03f), Hlp.navy, true);
            Hlp.Label(banner, "Text", "대학원 행정지원팀 (208호)\n대학원 미래혁신지원센터 (206호) →", new Vector3(0, 1.0f, -.02f), new Vector2(.50f, 1.2f), Color.white, 34);

            // Recycling station next to 201 blue container (photo 17)
            Hlp.AddVendingAndRecycling(basement, details, Hlp.At(18.6f, 46.8f), 180, includeVending: false);

            // Corridor downlights & fire extinguishers
            for (float v = 30f; v <= 58f; v += 3.5f) Hlp.Downlight(details, 20.1f, v);
            for (float u = 24f; u <= 40f; u += 3.5f) Hlp.Downlight(details, u, 54.2f);
            for (float v = 2f; v <= 17f; v += 3.5f) Hlp.Downlight(details, 21.0f, v);
            Hlp.FireExtinguisher(details, 18.6f, 26.2f);
            Hlp.FireExtinguisher(details, 21.6f, 35.5f);
            Hlp.FireExtinguisher(details, 27.2f, 58.6f);
        }
    }
}
