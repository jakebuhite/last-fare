using LastFare.Driving;
using LastFare.Navigation;
using UnityEngine;

namespace LastFare.UI
{
    /// <summary>
    /// Placeholder IMGUI phone GPS, always on screen: a heading-up minimap of the roads with the route,
    /// the destination pin and the car, the next turn instruction and the current road. Zooms out at speed.
    /// Everything comes from <see cref="GpsNavigator"/>.
    /// </summary>
    public class GpsView : MonoBehaviour
    {
        const float RefHeight = 1080f;
        const float MapSize = 400f;
        const float HeaderHeight = 128f;
        const float FooterHeight = 34f;
        const float NearMeters = 260f;    // map width when stopped
        const float FarMeters = 700f;     // map width at highway speed
        const float FarSpeed = 25f;       // m/s at which the map is fully zoomed out
        const float CarOffset = 0.22f;    // car sits below centre so more road ahead is visible
        const float Frame = 12f;

        static readonly Color PanelColor = new(0.02f, 0.03f, 0.05f, 0.92f);
        static readonly Color MapColor = new(0.05f, 0.06f, 0.07f, 0.95f);
        static readonly Color StreetColor = new(0.24f, 0.26f, 0.3f);
        static readonly Color HighwayColor = new(0.42f, 0.38f, 0.28f);
        static readonly Color RouteColor = new(0.3f, 0.75f, 1f);
        static readonly Color PinColor = new(0.55f, 1f, 0.6f);
        static readonly Color LabelColor = new(0.75f, 0.8f, 0.9f, 0.55f);

        [SerializeField] GpsNavigator gps;

        Texture2D arrow;
        Texture2D disc;
        float[] nodeSize;
        RoadMap cachedMap;

        Matrix4x4 baseMatrix;
        Rect mapRect;
        Vector2 carScreen;
        Vector3 carPos;
        float yawRad;
        float pxPerMeter;
        float metersShown = NearMeters;
        Vector3 lastPos;
        float smoothedSpeed;

        GUIStyle action;
        GUIStyle onto;
        GUIStyle detail;
        GUIStyle small;
        GUIStyle north;
        GUIStyle label;

        void Update()
        {
            if (gps == null || gps.Vehicle == null) return;

            // Zoom out with speed so the next turn is still on screen on the highway.
            var pos = gps.Vehicle.position;
            var speed = Time.deltaTime > 0f ? Vector3.Distance(pos, lastPos) / Time.deltaTime : 0f;
            lastPos = pos;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, Mathf.Min(speed, 60f), 1f - Mathf.Exp(-2f * Time.deltaTime));
            var target = Mathf.Lerp(NearMeters, FarMeters, Mathf.Clamp01(smoothedSpeed / FarSpeed));
            metersShown = Mathf.Lerp(metersShown, target, 1f - Mathf.Exp(-1.5f * Time.deltaTime));
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (gps == null || gps.Vehicle == null || gps.Map == null) return;
            EnsureAssets();

            var scale = Screen.height / RefHeight;
            baseMatrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.matrix = baseMatrix;

            mapRect = new Rect(20f + Frame, RefHeight - 20f - FooterHeight - MapSize, MapSize, MapSize);
            var panel = new Rect(mapRect.x - Frame, mapRect.y - HeaderHeight, MapSize + Frame * 2f, HeaderHeight + MapSize + FooterHeight);

            carPos = gps.Vehicle.position;
            yawRad = gps.Vehicle.eulerAngles.y * Mathf.Deg2Rad;
            pxPerMeter = MapSize / metersShown;
            carScreen = mapRect.center + new Vector2(0f, MapSize * CarOffset);

            Fill(panel, PanelColor);
            Fill(mapRect, MapColor);
            DrawRoads();
            if (gps.HasDestination) DrawRoute();
            DrawMask(panel);
            DrawLabels();
            DrawNorth();
            if (gps.HasDestination) DrawPin();
            DrawCar();
            DrawHeader(panel);
            DrawFooter(panel);

            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
        }

        // ---------------------------------------------------------------- Layers

        void DrawRoads()
        {
            var map = gps.Map;
            CacheNodeSizes(map);

            // Streets first, highways on top.
            for (var pass = 0; pass < 2; pass++)
            {
                var kind = pass == 0 ? RoadKind.Street : RoadKind.Highway;
                var color = pass == 0 ? StreetColor : HighwayColor;
                for (var e = 0; e < map.RoadCount; e++)
                {
                    var road = map.Road(e);
                    if (road.Kind != kind) continue;
                    var width = Mathf.Max(road.Width * pxPerMeter, pass == 0 ? 2f : 4f);
                    Segment(ToMap(map.Node(road.A)), ToMap(map.Node(road.B)), width, color);
                }
            }

            // Round off joints and cul-de-sacs.
            for (var n = 0; n < map.NodeCount; n++)
            {
                if (nodeSize[n] <= 0f) continue;
                Blob(ToMap(map.Node(n)), Mathf.Max(nodeSize[n] * pxPerMeter, 2f), StreetColor);
            }
        }

        void DrawRoute()
        {
            var route = gps.Route;
            if (route.Count == 0) return;

            Segment(carScreen, ToMap(route[0]), 6f, RouteColor);
            for (var i = 1; i < route.Count; i++)
            {
                Segment(ToMap(route[i - 1]), ToMap(route[i]), 6f, RouteColor);
                Blob(ToMap(route[i]), 6f, RouteColor);
            }
        }

        void DrawLabels()
        {
            GUI.matrix = baseMatrix;
            foreach (var l in gps.Map.Labels)
            {
                var p = ToMap(l.Position);
                if (!mapRect.Contains(p)) continue;
                GUI.color = LabelColor;
                GUI.Label(new Rect(p.x - 100f, p.y - 14f, 200f, 28f), l.Text, label);
            }

            GUI.color = Color.white;
        }

        void DrawPin()
        {
            var inner = new Rect(mapRect.x + 10f, mapRect.y + 10f, mapRect.width - 20f, mapRect.height - 20f);
            var p = ToMap(gps.Destination);
            p.x = Mathf.Clamp(p.x, inner.xMin, inner.xMax);
            p.y = Mathf.Clamp(p.y, inner.yMin, inner.yMax);

            GUI.matrix = baseMatrix;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(p.x - 11f, p.y - 11f, 22f, 22f), disc);
            GUI.color = PinColor;
            GUI.DrawTexture(new Rect(p.x - 8f, p.y - 8f, 16f, 16f), disc);
            GUI.color = Color.white;
        }

        void DrawCar()
        {
            GUI.matrix = baseMatrix;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(carScreen.x - 13f, carScreen.y - 15f, 26f, 30f), arrow);
        }

        void DrawNorth()
        {
            // "N" orbits the map edge so the player can tell which way the map is turned.
            var dir = new Vector2(-Mathf.Sin(yawRad), -Mathf.Cos(yawRad));
            var p = mapRect.center + dir * (MapSize * 0.5f - 16f);
            GUI.matrix = baseMatrix;
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(new Rect(p.x - 15f, p.y - 15f, 30f, 30f), "N", north);
            GUI.color = Color.white;
        }

        void DrawHeader(Rect panel)
        {
            GUI.matrix = baseMatrix;
            var line1 = new Rect(panel.x + 18f, panel.y + 8f, panel.width - 36f, 42f);
            var line2 = new Rect(panel.x + 18f, panel.y + 50f, panel.width - 36f, 34f);
            var line3 = new Rect(panel.x + 18f, panel.y + 88f, panel.width - 36f, 30f);

            if (!gps.HasDestination)
            {
                GUI.Label(line1, "GPS", action);
                GUI.Label(line3, "No active trip", detail);
                return;
            }

            var instruction = gps.Instruction;
            GUI.Label(line1, $"{instruction.Action}  ·  {instruction.When}", action);
            if (!string.IsNullOrEmpty(instruction.Onto) && instruction.Maneuver != Maneuver.Arrive)
                GUI.Label(line2, $"onto {instruction.Onto}", onto);
            GUI.Label(line3, $"{gps.DestinationLabel}  ·  {GpsDirections.FormatDistance(instruction.RemainingMeters)}", detail);
        }

        void DrawFooter(Rect panel)
        {
            GUI.matrix = baseMatrix;
            var rect = new Rect(panel.x, mapRect.yMax + 2f, panel.width, FooterHeight - 4f);
            GUI.Label(rect, gps.CurrentRoad, small);
        }

        /// <summary>Covers the map border so wide road strokes don't spill past the edge.</summary>
        void DrawMask(Rect panel)
        {
            GUI.matrix = baseMatrix;
            Fill(new Rect(panel.x, mapRect.y - Frame, panel.width, Frame), PanelColor);
            Fill(new Rect(panel.x, mapRect.yMax, panel.width, Frame), PanelColor);
            Fill(new Rect(panel.x, mapRect.y, Frame, MapSize), PanelColor);
            Fill(new Rect(mapRect.xMax, mapRect.y, Frame, MapSize), PanelColor);
        }

        // --------------------------------------------------------------- Drawing

        Vector2 ToMap(Vector3 world)
        {
            var dx = world.x - carPos.x;
            var dz = world.z - carPos.z;
            var cos = Mathf.Cos(yawRad);
            var sin = Mathf.Sin(yawRad);
            var right = dx * cos - dz * sin;
            var forward = dx * sin + dz * cos;
            return carScreen + new Vector2(right, -forward) * pxPerMeter;
        }

        /// <summary>A thick line, clipped to the map. Uses its own rotation matrix so it works with the HUD scale.</summary>
        void Segment(Vector2 a, Vector2 b, float width, Color color)
        {
            if (!Clip(ref a, ref b, mapRect)) return;

            var d = b - a;
            var length = d.magnitude;
            if (length < 0.01f) return;

            var angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            GUI.matrix = baseMatrix * Matrix4x4.TRS(a, Quaternion.Euler(0f, 0f, angle), Vector3.one);
            GUI.color = color;
            GUI.DrawTexture(new Rect(0f, -width * 0.5f, length, width), Texture2D.whiteTexture);
        }

        void Blob(Vector2 p, float size, Color color)
        {
            if (!mapRect.Contains(p)) return;
            GUI.matrix = baseMatrix;
            GUI.color = color;
            GUI.DrawTexture(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), disc);
        }

        static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        /// <summary>Liang-Barsky: trims segment a-b to the rectangle; false if it lies entirely outside.</summary>
        static bool Clip(ref Vector2 a, ref Vector2 b, Rect r)
        {
            float t0 = 0f, t1 = 1f;
            var d = b - a;

            bool Edge(float p, float q)
            {
                if (Mathf.Approximately(p, 0f)) return q >= 0f;
                var t = q / p;
                if (p < 0f) { if (t > t1) return false; if (t > t0) t0 = t; }
                else { if (t < t0) return false; if (t < t1) t1 = t; }
                return true;
            }

            if (!Edge(-d.x, a.x - r.xMin) || !Edge(d.x, r.xMax - a.x) ||
                !Edge(-d.y, a.y - r.yMin) || !Edge(d.y, r.yMax - a.y))
                return false;

            var start = a;
            a = start + d * t0;
            b = start + d * t1;
            return true;
        }

        /// <summary>Joint size per node: the widest road meeting there, or the cul-de-sac circle.</summary>
        void CacheNodeSizes(RoadMap map)
        {
            if (cachedMap == map && nodeSize != null && nodeSize.Length == map.NodeCount) return;
            cachedMap = map;
            nodeSize = new float[map.NodeCount];
            for (var e = 0; e < map.RoadCount; e++)
            {
                var road = map.Road(e);
                if (road.Kind != RoadKind.Street) continue; // highway joints are drawn by the wider strokes
                nodeSize[road.A] = Mathf.Max(nodeSize[road.A], road.Width);
                nodeSize[road.B] = Mathf.Max(nodeSize[road.B], road.Width);
            }

            for (var n = 0; n < map.NodeCount; n++) nodeSize[n] = Mathf.Max(nodeSize[n], map.NodeRadius(n) * 2f);
        }

        void EnsureAssets()
        {
            if (arrow == null) arrow = MakeArrow();
            if (disc == null) disc = MakeDisc();
            if (action != null) return;

            GUIStyle Make(int size, FontStyle fontStyle, TextAnchor anchor = TextAnchor.MiddleLeft)
            {
                var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fontStyle, alignment = anchor, clipping = TextClipping.Clip };
                s.normal.textColor = new Color(0.9f, 0.93f, 0.95f);
                return s;
            }

            action = Make(30, FontStyle.Bold);
            onto = Make(24, FontStyle.Normal);
            detail = Make(20, FontStyle.Normal);
            detail.normal.textColor = new Color(0.6f, 0.75f, 0.85f);
            small = Make(18, FontStyle.Normal, TextAnchor.MiddleCenter);
            small.normal.textColor = new Color(0.7f, 0.75f, 0.8f);
            north = Make(20, FontStyle.Bold, TextAnchor.MiddleCenter);
            label = Make(16, FontStyle.Italic, TextAnchor.MiddleCenter);
            label.normal.textColor = Color.white;
        }

        static Texture2D MakeArrow()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Texture rows go bottom-up, so the tip is at the top row.
                    var v = y / (float)(size - 1);
                    var halfWidth = (1f - v) * 0.5f;
                    var u = Mathf.Abs(x / (float)(size - 1) - 0.5f);
                    var notch = v < 0.25f && u < (0.25f - v) * 1.2f; // tail notch
                    var inside = u <= halfWidth && !notch;
                    var edge = u > halfWidth - 0.06f || v < 0.06f;
                    pixels[y * size + x] = inside
                        ? (edge ? new Color32(10, 20, 30, 255) : new Color32(255, 255, 255, 255))
                        : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeDisc()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            var c = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    var a = (byte)(Mathf.Clamp01((1f - d) * c) * 255f); // 1px soft edge
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        void OnDestroy()
        {
            if (arrow != null) Destroy(arrow);
            if (disc != null) Destroy(disc);
        }
    }
}
