using LastFare.Navigation;
using UnityEngine;

namespace LastFare.UI
{
    /// <summary>
    /// Placeholder IMGUI phone GPS, always on screen: a heading-up minimap of the streets with the route,
    /// the destination pin and the car, plus the next turn instruction. Everything comes from <see cref="GpsNavigator"/>.
    /// </summary>
    public class GpsView : MonoBehaviour
    {
        const float RefHeight = 1080f;
        const float MapSize = 400f;
        const float HeaderHeight = 96f;
        const float MetersShown = 240f;   // across the map
        const float CarOffset = 0.22f;    // car sits below centre so more road ahead is visible
        const float Frame = 12f;

        static readonly Color PanelColor = new(0.02f, 0.03f, 0.05f, 0.92f);
        static readonly Color MapColor = new(0.06f, 0.07f, 0.09f, 0.95f);
        static readonly Color RoadColor = new(0.24f, 0.26f, 0.3f);
        static readonly Color RouteColor = new(0.3f, 0.75f, 1f);
        static readonly Color PinColor = new(0.55f, 1f, 0.6f);

        [SerializeField] GpsNavigator gps;

        Texture2D arrow;
        Matrix4x4 baseMatrix;
        Rect mapRect;
        Vector2 carScreen;
        Vector3 carPos;
        float yawRad;
        float pxPerMeter;

        GUIStyle action;
        GUIStyle detail;
        GUIStyle north;

        void OnGUI()
        {
            if (gps == null || gps.Vehicle == null || gps.Map == null) return;
            EnsureAssets();

            var scale = Screen.height / RefHeight;
            baseMatrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.matrix = baseMatrix;

            mapRect = new Rect(20f + Frame, RefHeight - 20f - Frame - MapSize, MapSize, MapSize);
            var panel = new Rect(mapRect.x - Frame, mapRect.y - HeaderHeight, MapSize + Frame * 2f, HeaderHeight + MapSize + Frame);

            carPos = gps.Vehicle.position;
            yawRad = gps.Vehicle.eulerAngles.y * Mathf.Deg2Rad;
            pxPerMeter = MapSize / MetersShown;
            carScreen = mapRect.center + new Vector2(0f, MapSize * CarOffset);

            Fill(panel, PanelColor);
            Fill(mapRect, MapColor);
            DrawRoads();
            if (gps.HasDestination) DrawRoute();
            DrawMask(panel);
            DrawNorth();
            if (gps.HasDestination) DrawPin();
            DrawCar();
            DrawHeader(panel);

            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
        }

        // ---------------------------------------------------------------- Layers

        void DrawRoads()
        {
            var net = gps.Map.Network;
            var width = gps.Map.RoadWidth * pxPerMeter;
            for (var e = 0; e < net.EdgeCount; e++)
            {
                var (a, b) = net.Edge(e);
                Segment(ToMap(net.Node(a)), ToMap(net.Node(b)), width, RoadColor);
            }

            // Square off the intersections so corners don't look notched.
            for (var n = 0; n < net.NodeCount; n++) Dot(ToMap(net.Node(n)), width, RoadColor, true);
        }

        void DrawRoute()
        {
            var route = gps.Route;
            if (route.Count == 0) return;

            Segment(carScreen, ToMap(route[0]), 6f, RouteColor);
            for (var i = 1; i < route.Count; i++)
            {
                Segment(ToMap(route[i - 1]), ToMap(route[i]), 6f, RouteColor);
                Dot(ToMap(route[i]), 6f, RouteColor, false);
            }
        }

        void DrawPin()
        {
            var inner = new Rect(mapRect.x + 10f, mapRect.y + 10f, mapRect.width - 20f, mapRect.height - 20f);
            var p = ToMap(gps.Destination);
            p.x = Mathf.Clamp(p.x, inner.xMin, inner.xMax);
            p.y = Mathf.Clamp(p.y, inner.yMin, inner.yMax);

            GUI.matrix = baseMatrix;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(p.x - 10f, p.y - 10f, 20f, 20f), Texture2D.whiteTexture);
            GUI.color = PinColor;
            GUI.DrawTexture(new Rect(p.x - 7f, p.y - 7f, 14f, 14f), Texture2D.whiteTexture);
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
            var text = new Rect(panel.x + 18f, panel.y + 10f, panel.width - 36f, 44f);
            var sub = new Rect(panel.x + 18f, panel.y + 54f, panel.width - 36f, 30f);

            if (!gps.HasDestination)
            {
                GUI.Label(text, "GPS", action);
                GUI.Label(sub, "No active trip", detail);
                return;
            }

            var instruction = gps.Instruction;
            GUI.Label(text, $"{instruction.Action} {instruction.When}", action);
            GUI.Label(sub, $"{gps.DestinationLabel}  ·  {instruction.RemainingMeters:0} m", detail);
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
            GUI.matrix = baseMatrix
                         * Matrix4x4.TRS(a, Quaternion.Euler(0f, 0f, angle), Vector3.one);
            GUI.color = color;
            GUI.DrawTexture(new Rect(0f, -width * 0.5f, length, width), Texture2D.whiteTexture);
        }

        void Dot(Vector2 p, float size, Color color, bool alignToMap)
        {
            if (!mapRect.Contains(p)) return;
            GUI.matrix = baseMatrix * Matrix4x4.TRS(p, Quaternion.Euler(0f, 0f, alignToMap ? -yawRad * Mathf.Rad2Deg : 0f), Vector3.one);
            GUI.color = color;
            GUI.DrawTexture(new Rect(-size * 0.5f, -size * 0.5f, size, size), Texture2D.whiteTexture);
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

        void EnsureAssets()
        {
            if (arrow == null)
            {
                const int size = 64;
                arrow = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
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

                arrow.SetPixels32(pixels);
                arrow.Apply();
            }

            if (action != null) return;

            GUIStyle Make(int size, FontStyle fontStyle, TextAnchor anchor = TextAnchor.MiddleLeft)
            {
                var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fontStyle, alignment = anchor };
                s.normal.textColor = new Color(0.9f, 0.93f, 0.95f);
                return s;
            }

            action = Make(32, FontStyle.Bold);
            detail = Make(20, FontStyle.Normal);
            detail.normal.textColor = new Color(0.6f, 0.75f, 0.85f);
            north = Make(20, FontStyle.Bold, TextAnchor.MiddleCenter);
        }

        void OnDestroy()
        {
            if (arrow != null) Destroy(arrow);
        }
    }
}
