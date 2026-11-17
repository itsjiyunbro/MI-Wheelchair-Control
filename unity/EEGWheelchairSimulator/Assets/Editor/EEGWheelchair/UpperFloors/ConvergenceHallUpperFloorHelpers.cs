using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using P = EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallUpperFloorHelpers
    {
        public const float H = 2.85f;
        public const float StoreyRise = 4.50f;
        public const string Mats = "Assets/Art/Materials/ConvergenceHall/";

        public struct R
        {
            public float u0, v0, u1, v1;
            public R(float a, float b, float c, float d) { u0 = a; v0 = b; u1 = c; v1 = d; }
            public bool Contains(float u, float v) => u > u0 + .0001f && u < u1 - .0001f && v > v0 + .0001f && v < v1 - .0001f;
        }

        public static Material white, navy, black, silver, stone, tile, wood, concrete, mint, glass, lamp, red, green, brick, stairTile, yellow;
        public static Material contBlue, contGray, contNavy, contWhite, meshCeil, chairOak, chairCream;
        public static Font font;
        public static int obstacle;

        public static Vector3 At(float u, float v, float h = 0) => new Vector3(-v, h, -u);
        public static Vector3 World(Transform floor, float u, float v, float h = 0) => floor.TransformPoint(At(u, v, h));

        public static Material Surface(string name, Color color, string texture = null, float repeat = .6f)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(Mats + name + ".mat");
            if (!m)
            {
                var shader = Shader.Find("ConvergenceHall/Lounge Matte Surface");
                if (!shader) shader = Shader.Find("Universal Render Pipeline/Lit");
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, Mats + name + ".mat");
            }
            m.SetColor("_BaseColor", color);
            if (m.HasProperty("_TextureMetres")) m.SetFloat("_TextureMetres", repeat);
            if (m.HasProperty("_BaseMap"))
                m.SetTexture("_BaseMap", texture == null ? Texture2D.whiteTexture : AssetDatabase.LoadAssetAtPath<Texture2D>(Mats + texture + ".asset"));
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static void LoadMaterials()
        {
            white = Surface("1F_WhitePaint", new Color(.91f, .91f, .88f));
            navy = Surface("1F_NavySlat", new Color(.032f, .052f, .10f));
            black = Surface("1F_Charcoal", new Color(.035f, .039f, .043f));
            silver = Surface("1F_SatinAluminium", new Color(.64f, .66f, .66f));
            stone = Surface("1F_SpeckledStone", Color.white, "MeasuredSpeckledStoneTexture");
            tile = Surface("1F_GrayTile", new Color(1.12f, 1.10f, 1.05f), "MeasuredGrayFloorTileTexture");
            wood = Surface("1F_Oak", new Color(1.13f, 1.08f, 1), "BluebellOakTexture", 1.4f);
            concrete = Surface("1F_Concrete", new Color(.79f, .80f, .77f), "B112ConcreteGrain", 2.4f);
            mint = Surface("1F_PrivacyFilm", new Color(.57f, .72f, .68f));
            glass = AssetDatabase.LoadAssetAtPath<Material>(Mats + "BluebellRailGlass.mat");
            lamp = Surface("1F_LampDiffuser", new Color(.98f, .98f, .95f));
            red = Surface("1F_ExtinguisherRed", new Color(.64f, .035f, .03f));
            green = Surface("1F_ExitGreen", new Color(.025f, .38f, .23f));
            brick = AssetDatabase.LoadAssetAtPath<Material>(Mats + "1F_ExitBrick.mat") ?? Surface("1F_ExitBrick", new Color(.52f, .25f, .17f));
            stairTile = AssetDatabase.LoadAssetAtPath<Material>(Mats + "StairTile.mat") ?? tile;
            yellow = AssetDatabase.LoadAssetAtPath<Material>(Mats + "TactileYellow.mat") ?? Surface("TactileYellow", new Color(.85f, .68f, .12f));

            contBlue = Surface("2F_ContainerBlue", new Color(.11f, .22f, .62f));
            contGray = Surface("2F_ContainerGray", new Color(.52f, .54f, .56f));
            contNavy = Surface("2F_ContainerNavy", new Color(.09f, .12f, .32f));
            contWhite = Surface("2F_ContainerWhite", new Color(.88f, .89f, .88f));
            meshCeil = Surface("2F_MeshCeiling", new Color(.18f, .19f, .20f));
            chairOak = Surface("2F_ChairOak", new Color(.58f, .40f, .22f));
            chairCream = Surface("2F_ChairCream", new Color(.86f, .84f, .78f));

            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");
            if (!font) throw new Exception("NotoSansCJKkr-Regular.otf missing.");
            obstacle = LayerMask.NameToLayer("WheelchairObstacle");
            if (obstacle < 0) throw new Exception("WheelchairObstacle layer missing.");
        }

        public static Transform Box(Transform p, string n, Vector3 at, Vector3 size, Material m, bool collision = false)
        {
            var t = P.Box(p, n, at, size, m);
            if (collision) { t.gameObject.AddComponent<BoxCollider>(); t.gameObject.layer = obstacle; }
            if (m == glass) t.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return t;
        }

        public static Transform Rect(Transform p, string n, R r, float y, float thick, Material m, bool collision = false)
            => Box(p, n, At((r.u0 + r.u1) / 2, (r.v0 + r.v1) / 2, y), new Vector3(r.v1 - r.v0, thick, r.u1 - r.u0), m, collision);

        public static void Label(Transform p, string n, string value, Vector3 at, Vector2 size, Color color, int fontSize = 60, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = new GameObject(n, typeof(RectTransform), typeof(Canvas), typeof(Text)).transform;
            t.SetParent(p, false);
            t.localPosition = at;
            t.localScale = Vector3.one * .001f;
            ((RectTransform)t).sizeDelta = size / .001f;
            t.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var text = t.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = fontSize;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
        }

        public static Transform Face(Transform p, string name, Vector3 centre, float yaw)
        {
            var g = P.Group(p, name);
            g.localPosition = centre;
            g.localRotation = Quaternion.Euler(0, yaw, 0);
            return g;
        }

        public static void Panel(Transform g, string name, float a, float b, bool glazed, Material solid = null)
        {
            if (b - a < .01f) return;
            float mid = (a + b) / 2, w = b - a;
            if (!glazed)
                Box(g, name, new Vector3(mid, H / 2, .065f), new Vector3(w, H, .13f), solid ? solid : white, true);
            else
            {
                Box(g, name + "GlassLow", new Vector3(mid, .14f, .062f), new Vector3(w, .25f, .020f), glass);
                Box(g, name + "Privacy", new Vector3(mid, 1.31f, .063f), new Vector3(w, 2.08f, .022f), mint);
                Box(g, name + "GlassHigh", new Vector3(mid, 2.60f, .062f), new Vector3(w, .49f, .020f), glass);
                var barrier = g.gameObject.AddComponent<BoxCollider>();
                barrier.center = new Vector3(mid, H / 2, .065f);
                barrier.size = new Vector3(w, H, .04f);
                g.gameObject.layer = obstacle;
                int count = Mathf.Max(1, Mathf.CeilToInt(w / 1.05f));
                for (int i = 0; i <= count; i++)
                    Box(g, name + "Mullion" + i, new Vector3(Mathf.Lerp(a, b, i / (float)count), H / 2, .06f), new Vector3(.018f, H, .05f), silver);
                Box(g, name + "GlassHead", new Vector3(mid, H - .02f, .06f), new Vector3(w, .04f, .05f), silver);
            }
            Box(g, name + "Skirting", new Vector3(mid, .035f, -.006f), new Vector3(w, .07f, .014f), glazed ? silver : black);
        }

        public static bool IsNumberedRoom(string code)
            => !string.IsNullOrEmpty(code) && code.All(c => (c >= '0' && c <= '9') || c == '-') && code.Any(char.IsDigit);

        public static void Plaque(Transform g, string code, string korean, string english, float x, bool glazedWall = false)
        {
            if (IsNumberedRoom(code))
            {
                ConvergenceHallClassroomSigns.CreateRoomPlaque(g, "Sign_" + code, code, korean ?? "", english ?? "",
                    new Vector3(x, 1.49f, glazedWall ? .048f : -.004f));
            }
            else
            {
                var sign = P.Group(g, "Sign_" + code);
                sign.localPosition = new Vector3(x, 1.58f, -.034f);
                Box(sign, "Navy", Vector3.zero, new Vector3(.19f, .25f, .014f), navy);
                Box(sign, "NumberBand", new Vector3(0, .089f, -.009f), new Vector3(.19f, .070f, .008f), white);
                Label(sign, "RoomNumber", code, new Vector3(0, .089f, -.015f), new Vector2(.18f, .059f), Color.black, 42);
                if (!string.IsNullOrEmpty(korean))
                    Label(sign, "RoomTitle", korean, new Vector3(0, -.01f, -.014f), new Vector2(.18f, .095f), Color.white, 26);
            }
            Box(g, "CardReader", new Vector3(x, 1.15f, -.043f), new Vector3(.075f, .13f, .04f), black);
        }

        public static InteractiveDoor Door(Transform p, string floorTag, string code, string korean, string english, float width, bool clear = false, bool glazedWall = false, bool addPlaque = true, float initial = 0f, bool auto = false)
        {
            const float dh = 2.30f;
            float leafWidth = width > 1.3f ? width / 2 : width;
            for (int side = -1; side <= 1; side += 2)
                Box(p, "Jamb" + side, new Vector3(side * (width / 2 + .025f), dh / 2, 0), new Vector3(.05f, dh, .13f), clear ? black : white);
            Box(p, "Header", new Vector3(0, dh + .028f, 0), new Vector3(width + .10f, .056f, .13f), clear ? black : white);
            Box(p, "OpaqueTransom", new Vector3(0, (dh + .06f + H) / 2, .06f), new Vector3(width + .10f, H - dh - .06f, .12f), white, true);
            var motions = new List<InteractiveDoor.Motion>();
            var colliders = new List<BoxCollider>();
            int leaves = width > 1.3f ? 2 : 1;
            for (int i = 0; i < leaves; i++)
            {
                float x = leaves == 1 ? 0 : (i == 0 ? -1 : 1) * width / 4;
                var leaf = P.Group(p, "Leaf" + i);
                leaf.localPosition = new Vector3(x, 0, 0);
                Box(leaf, "Surface", new Vector3(0, dh / 2, .025f), new Vector3(leafWidth - .014f, dh - .01f, .045f), clear ? glass : white);
                if (clear)
                {
                    foreach (int s in new[] { -1, 1 }) Box(leaf, "Stile" + s, new Vector3(s * (leafWidth / 2 - .025f), dh / 2, 0), new Vector3(.05f, dh, .075f), black);
                    foreach (float y in new[] { .035f, dh - .035f }) Box(leaf, "Rail" + y, new Vector3(0, y, 0), new Vector3(leafWidth, .07f, .075f), black);
                }
                float handle = (i == 0 ? 1 : -1) * (leafWidth / 2 - .12f);
                foreach (int s in new[] { -1, 1 })
                {
                    P.Cylinder(leaf, "Rose" + s, new Vector3(handle, 1.0f, s * .046f), new Vector3(.060f, .009f, .060f), silver, Quaternion.Euler(90, 0, 0));
                    P.Tube(leaf, "Lever" + s, new Vector3(handle, 1, s * .075f), new Vector3(handle - .10f * (i == 0 ? 1 : -1), 1, s * .075f), .020f, silver);
                }
                foreach (float y in new[] { .22f, 1.20f, 2.10f })
                    P.Cylinder(leaf, "Hinge" + y, new Vector3((i == 0 ? -1 : 1) * leafWidth / 2, y, -.018f), new Vector3(.022f, .065f, .022f), silver, Quaternion.identity);
                var c = leaf.gameObject.AddComponent<BoxCollider>();
                c.center = new Vector3(0, dh / 2, .025f);
                c.size = new Vector3(leafWidth, dh, .055f);
                leaf.gameObject.layer = obstacle;
                colliders.Add(c);
                motions.Add(new InteractiveDoor.Motion
                {
                    target = leaf,
                    closedPosition = leaf.localPosition,
                    closedRotation = Quaternion.identity,
                    hinge = new Vector3(x + (i == 0 ? -1 : 1) * leafWidth / 2, 0, 0),
                    angle = i == 0 ? -95 : 95
                });
            }
            var door = p.gameObject.AddComponent<InteractiveDoor>();
            string doorTitle = string.IsNullOrEmpty(korean) ? $"{floorTag} {code}" : $"{floorTag} {code} {korean}";
            door.Configure(doorTitle, motions.ToArray(), colliders.ToArray(), new Vector3(0, 1.1f, -.1f), auto: auto, initial: initial);
            if (addPlaque && !string.IsNullOrEmpty(code)) Plaque(p, code, korean, english, -width / 2 - .17f, glazedWall);
            return door;
        }

        public static void Wall(Transform parent, string name, Vector3 at, float yaw, float length, string floorTag = "2F",
            string code = null, string korean = "", string english = "", bool glazed = false, float doorOffset = 0, bool doubleDoor = false, Material solid = null, bool clearDoor = false)
        {
            var f = Face(parent, name, at, yaw);
            if (code == null) { Panel(f, "Wall", -length / 2, length / 2, glazed, solid); return; }
            float w = doubleDoor ? 1.7f : 1.0f, half = w / 2 + .05f;
            Panel(f, "Left", -length / 2, doorOffset - half, glazed, solid);
            Panel(f, "Right", doorOffset + half, length / 2, glazed, solid);
            var d = P.Group(f, "Door_" + code);
            d.localPosition = new Vector3(doorOffset, 0, 0);
            Door(d, floorTag, code, korean, english, w, clearDoor, glazedWall: glazed);
        }

        public static Transform Room(Transform architecture, Transform roofs, string floorTag, string code, R r, string edge,
            string korean = "", string english = "", bool glaze = false, bool doubles = false, Material front = null, float doorOffset = 0f, bool clearDoor = false)
        {
            var room = P.Group(architecture, "Room" + code);
            float u = (r.u0 + r.u1) / 2, v = (r.v0 + r.v1) / 2;
            Wall(room, "U0", At(r.u0, v), 180, r.v1 - r.v0, floorTag, edge == "u0" ? code : null, korean, english, edge == "u0" && glaze, doorOffset, doubles, edge == "u0" ? front : null, clearDoor);
            Wall(room, "U1", At(r.u1, v), 0, r.v1 - r.v0, floorTag, edge == "u1" ? code : null, korean, english, edge == "u1" && glaze, doorOffset, doubles, edge == "u1" ? front : null, clearDoor);
            Wall(room, "V0", At(u, r.v0), -90, r.u1 - r.u0, floorTag, edge == "v0" ? code : null, korean, english, edge == "v0" && glaze, doorOffset, doubles, edge == "v0" ? front : null, clearDoor);
            Wall(room, "V1", At(u, r.v1), 90, r.u1 - r.u0, floorTag, edge == "v1" ? code : null, korean, english, edge == "v1" && glaze, doorOffset, doubles, edge == "v1" ? front : null, clearDoor);
            Rect(roofs, "Room" + code + "Ceiling", r, H + .05f, .10f, white);
            return room;
        }

        public static void TeachingRoom(Transform architecture, Transform roofs, Transform details, string floorTag, string code, R bounds,
            string entrance, string korean = "", string english = "", bool glazed = false, bool doubleDoor = false, bool sideEntry = false, bool computers = false)
        {
            var shell = Room(architecture, roofs, floorTag, code, bounds, entrance, korean, english, glazed, doubleDoor);
            bool frontAlongV = sideEntry || entrance[0] == 'v';
            float uSize = bounds.u1 - bounds.u0, vSize = bounds.v1 - bounds.v0;
            float width = frontAlongV ? uSize : vSize, depth = frontAlongV ? vSize : uSize;
            float yaw = sideEntry || entrance == "v0" ? -90 : entrance == "v1" ? 90 : entrance == "u0" ? 180 : 0;
            var interior = Face(shell, "ClassroomInterior", At((bounds.u0 + bounds.u1) / 2, (bounds.v0 + bounds.v1) / 2), yaw);

            const float aisleHalfWidth = .75f;
            float deskWidth = 1.5f * P.FurnitureScale;
            float columnPitch = deskWidth + .17f;
            int columnsPerSide = Mathf.FloorToInt((width / 2 - aisleHalfWidth - .36f + .17f) / columnPitch);
            if (computers) columnsPerSide = Mathf.Min(3, columnsPerSide);
            bool useSideAisle = sideEntry || columnsPerSide <= 0;
            float aisleX = useSideAisle ? Mathf.Max(0, width / 2 - .78f) : 0;
            int columns = useSideAisle ? Mathf.Max(1, Mathf.FloorToInt((width - 2.0f) / columnPitch)) : columnsPerSide * 2;
            float firstDesk = depth / 2 - 2.02f;
            float lastDesk = -depth / 2 + (sideEntry ? 1.10f : useSideAisle ? 2.52f : 1.58f);
            float rowPitch = computers ? 1.75f : depth <= 6f ? 1.38f : 1.55f;
            if (width >= 2.6f && depth >= 3.8f)
            {
                int rows = 0;
                for (float z = firstDesk; z >= lastDesk - .001f; z -= rowPitch)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        float x;
                        if (useSideAisle) x = -width / 2 + .42f + deskWidth / 2 + column * columnPitch;
                        else
                        {
                            int side = column < columnsPerSide ? -1 : 1;
                            x = side * (aisleHalfWidth + deskWidth / 2 + (column % columnsPerSide) * columnPitch);
                        }
                        if (Mathf.Abs(x) + deskWidth / 2 > width / 2 - .15f) continue;
                        var desk = P.Instance(interior, "TwoSeatFoldingDesk", $"Desk_{rows}_{column}", new Vector3(x, 0, z));
                        foreach (int seat in new[] { -1, 1 })
                            P.Instance(interior, "BlueMeshChair", $"Chair_{rows}_{column}_{seat}", new Vector3(x + seat * .36f, 0, z - .64f));
                    }
                    rows++;
                }
            }

            float boardWidth = Mathf.Min(5.6f, Mathf.Max(1.2f, width - .65f));
            var board = P.Group(interior, "FrontWhiteboard");
            board.localPosition = new Vector3(0, 1.57f, depth / 2 - .18f);
            Box(board, "AluminiumFrame", Vector3.zero, new Vector3(boardWidth, 1.15f, .043f), silver);
            Box(board, "WritingSurface", new Vector3(0, 0, -.023f), new Vector3(boardWidth - .038f, 1.11f, .010f), white);
            Box(board, "MarkerTray", new Vector3(0, -.586f, -.048f), new Vector3(boardWidth, .027f, .092f), silver);

            if (width >= 2.8f && depth >= 3.5f)
            {
                float lecternX = useSideAisle ? -width / 2 + .85f : -Mathf.Min(width * .25f, 2.1f);
                var lectern = P.Instance(interior, "ElectronicLectern", "Lectern", new Vector3(lecternX, 0, depth / 2 - .82f));
                lectern.localRotation = Quaternion.Euler(0, 180, 0);
            }

            int lightColumns = Mathf.Max(1, Mathf.RoundToInt(width / 3f));
            int lightRows = Mathf.Max(1, Mathf.RoundToInt(depth / 2.7f));
            for (int row = 0; row < lightRows; row++)
                for (int column = 0; column < lightColumns; column++)
                {
                    var at = new Vector3(width * ((column + .5f) / lightColumns - .5f), H - .039f, depth * ((row + .5f) / lightRows - .5f));
                    Box(interior, "CeilingPanelFrame", at, new Vector3(1.20f, .032f, .31f), silver);
                    Box(interior, "CeilingPanelDiffuser", at + Vector3.down * .019f, new Vector3(1.15f, .011f, .266f), lamp);
                }
        }

        public static void Guard(Transform details, string n, Vector3 p, Vector3 size)
        {
            Box(details, n, p, size, white, true);
            Box(details, n + "Coping", p + Vector3.up * (size.y / 2 + .015f), new Vector3(size.x + .025f, .03f, size.z + .025f), silver);
        }

        public static void VoidGuard(Transform details, R r, bool skipU0 = false, bool skipU1 = false, bool skipV0 = false, bool skipV1 = false)
        {
            if (!skipU0) Guard(details, "VoidGuardU0", At(r.u0, (r.v0 + r.v1) / 2, .55f), new Vector3(r.v1 - r.v0, 1.1f, .13f));
            if (!skipU1) Guard(details, "VoidGuardU1", At(r.u1, (r.v0 + r.v1) / 2, .55f), new Vector3(r.v1 - r.v0, 1.1f, .13f));
            if (!skipV0) Guard(details, "VoidGuardV0", At((r.u0 + r.u1) / 2, r.v0, .55f), new Vector3(.13f, 1.1f, r.u1 - r.u0));
            if (!skipV1) Guard(details, "VoidGuardV1", At((r.u0 + r.u1) / 2, r.v1, .55f), new Vector3(.13f, 1.1f, r.u1 - r.u0));
        }

        public static void Slats(Transform roofs, R r, float height, string name)
        {
            var group = P.Group(roofs, "Navy_" + name);
            Rect(group, "Backing", r, height + .15f, .08f, black);
            for (float u = r.u0 + .04f; u < r.u1; u += .17f)
                Rect(group, "Slat", new R(u, r.v0, Mathf.Min(u + .055f, r.u1), r.v1), height, .17f, navy);
        }

        public static void Downlight(Transform details, float u, float v, bool onNavy = false)
        {
            var g = P.Group(details, "CeilingFitting");
            g.localPosition = At(u, v, H - .025f);
            P.Cylinder(g, "Trim", Vector3.zero, new Vector3(.18f, .018f, .18f), onNavy ? black : silver, Quaternion.identity);
            P.Cylinder(g, "Diffuser", new Vector3(0, -.021f, 0), new Vector3(.139f, .008f, .139f), lamp, Quaternion.identity);
        }

        public static void FireExtinguisher(Transform details, float u, float v)
        {
            var g = P.Group(details, "FireExtinguisher");
            g.localPosition = At(u, v);
            Box(g, "Base", new Vector3(0, .025f, 0), new Vector3(.28f, .05f, .28f), red);
            P.Cylinder(g, "Bottle", new Vector3(0, .22f, 0), new Vector3(.15f, .20f, .15f), red, Quaternion.identity);
            P.Tube(g, "Hose", new Vector3(.06f, .42f, 0), new Vector3(.10f, .16f, 0), .02f, black);
            Box(g, "Handle", new Vector3(0, .45f, 0), new Vector3(.13f, .03f, .04f), black);
        }

        public static void ExitSign(Transform parent, Vector3 localPos, string text = "↑ EXIT")
        {
            Box(parent, "ExitSignBody", localPos, new Vector3(.27f, .21f, .065f), white);
            Box(parent, "ExitSignFace", localPos + new Vector3(0, 0, -.036f), new Vector3(.235f, .175f, .008f), green);
            Label(parent, "ExitSignText", text, localPos + new Vector3(0, 0, -.044f), new Vector2(.225f, .15f), Color.white, 58);
        }

        public static void MatchBasementRestrooms(GameObject basement, Transform architecture, string floorTag, string[] codes, R[] footprints, string[] wallFaces)
        {
            var sources = new[] { "NorthWC_South_Women", "NorthWC_West_Men", "WestWC_East_NorthWC", "WestWC_East_SouthWC" };
            for (int i = 0; i < codes.Length; i++)
            {
                var room = architecture.Find("Room" + codes[i]);
                var front = room.Find(wallFaces[i]);
                var old = front.Find("Door_" + codes[i]);
                var source = basement.transform.Find("PlanDoors/" + sources[i % sources.Length]);
                source.GetComponent<InteractiveDoor>().ResetDoor();
                var finish = basement.transform.Find("RoomsAndCores/" + (i < 2 ? "NorthWC" : "WestWC") + "/West/LowerWall").GetComponent<Renderer>().sharedMaterial;
                foreach (var r in room.GetComponentsInChildren<MeshRenderer>()) if (r.sharedMaterial == white) r.sharedMaterial = finish;
                var skirting = basement.transform.Find("RoomsAndCores/" + (i < 2 ? "NorthWC" : "WestWC") + "/West/Skirting").GetComponent<Renderer>().sharedMaterial;
                foreach (var r in room.GetComponentsInChildren<MeshRenderer>()) if (r.name.EndsWith("Skirting")) r.sharedMaterial = skirting;
                Rect(room, "RestroomGrayFloor", footprints[i], .004f, .008f, tile);
                var replacement = UnityEngine.Object.Instantiate(source.gameObject, front).transform;
                replacement.name = "Door_" + codes[i];
                replacement.localPosition = old.localPosition;
                replacement.localRotation = Quaternion.identity;
                replacement.localScale = Vector3.one;
                UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach (int sign in new[] { -1, 1 })
                    Box(front, "RestroomReveal" + sign, new Vector3(sign * .5125f, H / 2, .065f), new Vector3(.075f, H, .13f), finish, true);
                Box(front, "RestroomTransom", new Vector3(0, (2.25f + H) / 2, .065f), new Vector3(1.1f, H - 2.25f, .13f), finish, true);
                var label = replacement.Find("RoomLabel").GetComponent<Text>();
                label.font = font;
                label.text = codes[i].EndsWith("-W") ? "여자 화장실" : "남자 화장실";
                var serial = new SerializedObject(replacement.GetComponent<InteractiveDoor>());
                serial.FindProperty("doorLabel").stringValue = floorTag + " " + label.text + " 문";
                serial.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static Transform CopyNorthElevatorHall(GameObject basement, Transform architecture, Transform roofs, Transform details, string floorTag, string displayCode)
        {
            var source = basement.transform.Find("CoreHalls/B112ElevatorHall");
            foreach (var d in source.GetComponentsInChildren<InteractiveDoor>(true)) d.ResetDoor();
            var g = UnityEngine.Object.Instantiate(source.gameObject, architecture).transform;
            g.name = "NorthElevatorHall";
            g.localPosition = At(10.5f, 25.5f);
            g.localRotation = Quaternion.Euler(0, 90, 0);
            g.localScale = Vector3.one;
            var finish = AssetDatabase.LoadAssetAtPath<Material>(Mats + "B112ExposedConcrete.mat");
            Wall(g, "CoreLeftConcrete", g.InverseTransformPoint(At(8, 21.95f)), 90, 7.1f, floorTag, solid: finish);
            Wall(g, "CoreRightConcrete", g.InverseTransformPoint(At(13, 21.95f)), -90, 7.1f, floorTag, solid: finish);
            Wall(g, "CoreRearConcrete", g.InverseTransformPoint(At(10.5f, 18.4f)), 180, 5, floorTag, solid: finish);
            Rect(roofs, "NorthElevatorHallCeiling", new R(8, 18.4f, 13, 25.5f), H + .05f, .1f, white);
            Downlight(details, 10.5f, 24.3f);
            Downlight(details, 10.5f, 22.9f);
            var display = g.Find("PhotoElevatorDetails/PhotoFloorIndicator");
            foreach (Transform glyph in display) if (glyph.name.StartsWith("LED_")) glyph.gameObject.SetActive(false);
            Label(display, "FloorNumber", displayCode, new Vector3(.006f, 0, -.006f), new Vector2(.13f, .08f), Color.white, 65);
            var serial = new SerializedObject(g.GetComponent<InteractiveDoor>());
            serial.FindProperty("doorLabel").stringValue = floorTag + " 동쪽 엘리베이터 문";
            serial.ApplyModifiedPropertiesWithoutUndo();
            foreach (string n in new[] { "FrontLeftPier", "FrontRightPier" })
            {
                var pier = g.Find(n);
                float side = n.Contains("Left") ? -1 : 1;
                pier.localPosition = new Vector3(side * 1.825f, H / 2, .06f);
                pier.localScale = new Vector3(1.35f, H, .12f);
                var col = g.Find(n + "Collision").GetComponent<BoxCollider>();
                col.center = pier.localPosition;
                col.size = pier.localScale;
            }
            return g;
        }

        public static void CopySouthElevatorAndStairwell(GameObject basement, Transform architecture, Transform details, string floorTag, string displayCode, bool buildCeiling = true)
        {
            var source = basement.transform.Find("CoreHalls/B109ElevatorHall");
            foreach (var d in source.GetComponentsInChildren<InteractiveDoor>(true)) d.ResetDoor();
            var hall = UnityEngine.Object.Instantiate(source.gameObject, architecture).transform;
            hall.name = "SouthElevatorHall";
            hall.localPosition = At(6, 59.4f);
            hall.localRotation = Quaternion.Euler(0, -90, 0);
            hall.localScale = Vector3.one;
            var finish = AssetDatabase.LoadAssetAtPath<Material>(Mats + "WestLiftPhotoWhite.mat");
            const float depth = 5.976f, width = 4.32f;
            Box(hall, "CoreLeftWall", new Vector3(-width / 2 + .06f, H / 2, depth / 2), new Vector3(.12f, H, depth), finish, true);
            Box(hall, "CoreRightWall", new Vector3(width / 2 - .06f, H / 2, depth / 2), new Vector3(.12f, H, depth), finish, true);
            Box(hall, "CoreBackWall", new Vector3(0, H / 2, depth - .06f), new Vector3(width - .24f, H, .12f), finish, true);
            OpenWestHallFront(hall, finish);
            Box(hall, "HallCeiling", new Vector3(0, H + .05f, depth / 2), new Vector3(width, .1f, depth), white);
            var display = hall.Find("LeftWallElevator/PhotoElevatorDetails/PhotoFloorIndicator");
            foreach (Transform glyph in display) if (glyph.name.StartsWith("LED_")) glyph.gameObject.SetActive(false);
            Label(display, "FloorNumber", displayCode, new Vector3(.006f, 0, -.006f), new Vector2(.13f, .08f), Color.white, 65);
            var leftDoor = hall.Find("LeftWallElevator").GetComponent<InteractiveDoor>();
            var serial = new SerializedObject(leftDoor);
            serial.FindProperty("doorLabel").stringValue = floorTag + " 서쪽 엘리베이터 문";
            serial.ApplyModifiedPropertiesWithoutUndo();

            Wall(architecture, "WestLiftRightFill", At(8.28f, 59.4f), -90, .24f, floorTag, solid: finish);
            Wall(architecture, "WestCoreDivider", At(3.72f, 59.4f), -90, .24f, floorTag, solid: finish);
            Rect(details, "WestCoreGrayFloor", new R(3.6f, 59.4f, 8.4f, 66), -.045f, .08f, tile);
            Downlight(details, 6.4f, 60.4f);
            Downlight(details, 6.4f, 63.7f);

            source = basement.transform.Find("CoreHalls/B109Stairwell");
            foreach (var d in source.GetComponentsInChildren<InteractiveDoor>(true)) d.ResetDoor();
            var stair = UnityEngine.Object.Instantiate(source.gameObject, details).transform;
            stair.name = "SouthStairwell";
            stair.localPosition = At(1.8f, 59.4f);
            stair.localRotation = Quaternion.Euler(0, -90, 0);
            stair.localScale = Vector3.one;
            foreach (string name in new[] { "SwitchbackStairs", "UnderStairStorage", "Landing", "StairFlightBlocker" })
                stair.Find(name).gameObject.SetActive(false);
            foreach (string name in new[] { "ExitMarker", "ExitMarkerBacking" })
            {
                var sign = stair.Find(name);
                if (sign) sign.gameObject.SetActive(false);
            }
            const float sw = 3.528f, sh = 4.55f, stairDepth = 6.60f;
            var frameTop = stair.Find("DoorFrameTop");
            var originalLintel = stair.Find("Lintel");
            float headBottom = frameTop.localPosition.y + frameTop.localScale.y / 2 - .018f;
            var doorSurroundFinish = stair.Find("FrontLeftPier").GetComponent<Renderer>().sharedMaterial;
            architecture.Find("WestCoreDivider/Wall").GetComponent<Renderer>().sharedMaterial = doorSurroundFinish;
            Box(stair, "StairDoorHeadWall", new Vector3(0, (H + headBottom) / 2, .06f),
                new Vector3(originalLintel.localScale.x, H - headBottom, .12f), doorSurroundFinish, true);
            originalLintel.gameObject.SetActive(false);
            stair.Find("LintelCollision").gameObject.SetActive(false);
            const float facadeDepth = .12f;
            float sideCentre = (stairDepth + facadeDepth) / 2;
            float sideDepth = stairDepth - facadeDepth;
            Box(stair, "LeftWall", new Vector3(-sw / 2 + .06f, sh / 2, sideCentre), new Vector3(.12f, sh, sideDepth), white, true);
            Box(stair, "RightWall", new Vector3(sw / 2 - .06f, sh / 2, sideCentre), new Vector3(.12f, sh, sideDepth), white, true);
            Box(stair, "BackWall", new Vector3(0, sh / 2, stairDepth - .06f), new Vector3(sw - .24f, sh, .12f), white, true);
            Box(stair, "HighFrontWall", new Vector3(0, (H + sh) / 2, .06f), new Vector3(sw, sh - H, .12f), white, true);
            if (buildCeiling) Box(stair, "StairwellCeiling", new Vector3(0, sh + .05f, stairDepth / 2), new Vector3(sw, .1f, stairDepth), white);
            var stairDoor = stair.GetComponent<InteractiveDoor>();
            if (stairDoor)
            {
                var sSerial = new SerializedObject(stairDoor);
                sSerial.FindProperty("doorLabel").stringValue = floorTag + " 서쪽 계단실 문";
                sSerial.ApplyModifiedPropertiesWithoutUndo();
            }
            Wall(architecture, "WestStairOuterFill", At(.018f, 59.4f), -90, .036f, floorTag);
            Wall(architecture, "WestStairInnerFill", At(3.582f, 59.4f), -90, .036f, floorTag, solid: doorSurroundFinish);
        }

        static void OpenWestHallFront(Transform hall, Material finish)
        {
            foreach (string name in new[] { "FrontLeftPier", "FrontRightPier", "Lintel" })
            {
                hall.Find(name).gameObject.SetActive(false);
                hall.Find(name + "Collision").gameObject.SetActive(false);
            }
            foreach (int side in new[] { -1, 1 })
                Box(hall, "FrontCorner" + side, new Vector3(side * 2.10f, H / 2, .06f), new Vector3(.12f, H, .12f), finish, true);
        }

        public static void BuildNorthStairwellShell(Transform details, string floorTag, R r, bool withCeiling = true)
        {
            var g = P.Group(details, "NorthStairwell");
            const float enclosure = 4.55f;
            Rect(g, "BackWall", new R(r.u0 - .07f, r.v0 - .07f, r.u1 + .07f, r.v0 + .07f), enclosure / 2, enclosure, white, true);
            Rect(g, "LeftWall", new R(r.u0 - .07f, r.v0, r.u0 + .07f, r.v1 + .07f), enclosure / 2, enclosure, white, true);
            Rect(g, "RightWall", new R(r.u1 - .07f, r.v0, r.u1 + .07f, r.v1 + .07f), enclosure / 2, enclosure, white, true);
            Rect(g, "HighFrontWall", new R(r.u0, r.v1 - .07f, r.u1, r.v1 + .07f), (H + enclosure) / 2, enclosure - H, white, true);
            if (withCeiling) Rect(g, "StairwellCeiling", new R(r.u0 - .07f, r.v0 - .07f, r.u1 + .07f, r.v1 + .07f), enclosure + .05f, .1f, white);
            var f = Face(g, "StairDoor", At((r.u0 + r.u1) / 2, r.v1), 90);
            Door(f, floorTag, "STAIR", "계단실", "Stair", 1.0f);
            Panel(f, "StairFrontLeft", -(r.u1 - r.u0) / 2, -.55f, false);
            Panel(f, "StairFrontRight", .55f, (r.u1 - r.u0) / 2, false);
        }

        public static void AddStairRail(Transform parent, Vector3 start, Vector3 end)
        {
            var height = Vector3.up * .92f;
            P.Tube(parent, "Handrail", start + height, end + height, .037f, black);
            int posts = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, end) / 1.40f));
            for (int i = 0; i <= posts; i++)
            {
                var foot = Vector3.Lerp(start, end, i / (float)posts);
                P.Tube(parent, "RailPost", foot, foot + height, .029f, black);
            }
        }

        public static void AddVendingAndRecycling(GameObject basement, Transform details, Vector3 pos, float yaw, bool includeVending = true)
        {
            var src = basement.transform.Find("Landmarks/BluebellAmenities");
            if (!src) return;
            var group = P.Group(details, "FloorAmenities");
            group.localPosition = pos;
            group.localRotation = Quaternion.Euler(0, yaw, 0);
            if (includeVending)
            {
                var greenVm = UnityEngine.Object.Instantiate(src.Find("GreenVendingMachine").gameObject, group);
                greenVm.name = "GreenVendingMachine";
                greenVm.transform.localPosition = new Vector3(-1.3f, 0, 0);
                greenVm.transform.localRotation = Quaternion.identity;

                var whiteVm = UnityEngine.Object.Instantiate(src.Find("WhiteVendingMachine").gameObject, group);
                whiteVm.name = "WhiteVendingMachine";
                whiteVm.transform.localPosition = new Vector3(-0.15f, 0, 0);
                whiteVm.transform.localRotation = Quaternion.identity;
            }
            var bins = UnityEngine.Object.Instantiate(src.Find("WoodRecyclingStation").gameObject, group);
            bins.name = "WoodRecyclingStation";
            bins.transform.localPosition = new Vector3(includeVending ? 1.85f : 0f, 0, 0);
            bins.transform.localRotation = Quaternion.identity;
        }

        public static void AddArmchairRow(Transform parent, string name, float uStart, float v, float yaw, int count, float stepU = .78f)
        {
            var g = P.Group(parent, name);
            for (int i = 0; i < count; i++)
            {
                var c = Face(g, "Armchair" + i, At(uStart + i * stepU, v), yaw);
                Box(c, "Seat", new Vector3(0, .40f, 0), new Vector3(.56f, .10f, .54f), chairCream, true);
                Box(c, "Back", new Vector3(0, .64f, -.22f), new Vector3(.56f, .38f, .10f), chairCream);
                foreach (int s in new[] { -1, 1 })
                {
                    Box(c, "Arm" + s, new Vector3(s * .29f, .54f, -.02f), new Vector3(.05f, .05f, .50f), chairOak);
                    Box(c, "LegF" + s, new Vector3(s * .29f, .27f, .21f), new Vector3(.045f, .54f, .045f), chairOak);
                    Box(c, "LegB" + s, new Vector3(s * .29f, .27f, -.24f), new Vector3(.045f, .54f, .045f), chairOak);
                }
            }
        }

        public static void AddRoundTableSet(Transform parent, string name, float u, float v)
        {
            var t = P.Group(parent, name);
            t.localPosition = At(u, v);
            P.Cylinder(t, "WhiteTop", new Vector3(0, .73f, 0), new Vector3(.90f, .018f, .90f), white, Quaternion.identity);
            P.Cylinder(t, "Pedestal", new Vector3(0, .37f, 0), new Vector3(.06f, .35f, .06f), silver, Quaternion.identity);
            P.Cylinder(t, "Base", new Vector3(0, .025f, 0), new Vector3(.49f, .018f, .49f), white, Quaternion.identity);
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, .38f, 0);
            col.size = new Vector3(.90f, .76f, .90f);
            t.gameObject.layer = obstacle;
            for (int j = 0; j < 3; j++)
            {
                float angle = (j * 120) * Mathf.Deg2Rad;
                var ch = Face(t, "Chair" + j, new Vector3(.76f * Mathf.Cos(angle), 0, .76f * Mathf.Sin(angle)), 0);
                ch.localRotation = Quaternion.LookRotation(-ch.localPosition);
                Box(ch, "Seat", new Vector3(0, .42f, 0), new Vector3(.48f, .07f, .46f), j == 1 ? chairCream : black, true);
                Box(ch, "Back", new Vector3(0, .66f, -.20f), new Vector3(.48f, .36f, .07f), j == 1 ? chairCream : black);
                foreach (int s in new[] { -1, 1 })
                {
                    Box(ch, "LegF" + s, new Vector3(s * .22f, .21f, .18f), new Vector3(.04f, .42f, .04f), chairOak);
                    Box(ch, "LegB" + s, new Vector3(s * .22f, .21f, -.18f), new Vector3(.04f, .42f, .04f), chairOak);
                }
            }
        }
    }
}
