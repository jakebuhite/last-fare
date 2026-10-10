using LastFare.Driving;
using LastFare.Rides;
using UnityEngine;

namespace LastFare.UI
{
    /// <summary>
    /// Placeholder IMGUI HUD: rating, objective, ride request card, captions and the result screen.
    /// Swap for UI Toolkit later; everything it shows comes from <see cref="FareDirector"/>.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        const float RefHeight = 1080f;

        [SerializeField] FareDirector director;
        [SerializeField] CarController car;

        string caption = "";
        float captionTime;

        GUIStyle small;
        GUIStyle normal;
        GUIStyle big;
        GUIStyle title;
        GUIStyle center;

        public void Caption(string text, float seconds)
        {
            caption = text;
            captionTime = seconds;
        }

        void Update()
        {
            if (captionTime > 0f) captionTime -= Time.deltaTime;
        }

        void OnGUI()
        {
            if (director == null || director.Controller == null) return;

            EnsureStyles();
            var scale = Screen.height / RefHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var w = Screen.width / scale;
            var h = RefHeight;

            if (!director.ShowResult || director.Outcome != RideOutcome.Attacked)
            {
                DrawStatus(w, h);
            }

            if (director.Controller.State == RideState.Requested && !director.ShowResult) DrawRequestCard(w, h);

            if (captionTime > 0f && !string.IsNullOrEmpty(caption)) DrawCaption(w, h);

            DrawOverlays(w, h);
            if (director.ShowResult) DrawResult(w, h);
        }

        void DrawStatus(float w, float h)
        {
            Box(new Rect(20, 20, 330, 92), 0.45f);
            GUI.Label(new Rect(34, 26, 300, 40), $"Rating  {director.Rating.Value:0.00} ★", big);
            GUI.Label(new Rect(34, 66, 300, 30), $"Earned  ${director.Earnings}", normal);

            if (!string.IsNullOrEmpty(director.Objective))
            {
                Box(new Rect(w / 2 - 330, 20, 660, 40), 0.45f);
                GUI.Label(new Rect(w / 2 - 330, 24, 660, 34), director.Objective, center);
            }

            var speedText = $"{Mathf.Abs(car.Speed) * 3.6f:0} km/h{(car.Reversing ? "   R" : "")}{(car.OffRoad ? "   off-road" : "")}";
            Box(new Rect(w - 440, h - 120, 420, 100), 0.4f);
            GUI.Label(new Rect(w - 430, h - 114, 400, 30), speedText, normal);
            GUI.Label(new Rect(w - 430, h - 84, 410, 64),
                "W gas   S brake/reverse   A/D steer   Space handbrake\nMouse look   Hold Right Mouse: mirror", small);

            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.DrawTexture(new Rect(w / 2 - 2, h / 2 - 2, 4, 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void DrawRequestCard(float w, float h)
        {
            var ride = director.Ride;
            var p = ride.Passenger;
            var rect = new Rect(w - 500, 100, 460, 470);
            Box(rect, 0.88f);

            var y = rect.y + 14;
            GUI.Label(new Rect(rect.x + 20, y, 420, 30), "NEW RIDE REQUEST", small);
            y += 30;
            GUI.Label(new Rect(rect.x + 20, y, 420, 44), $"{p.DisplayName}   {p.ShownRating:0.0} ★", big);
            y += 56;
            GUI.Label(new Rect(rect.x + 20, y, 420, 28), $"From:  {ride.PickupLabel}", normal);
            y += 30;
            GUI.Label(new Rect(rect.x + 20, y, 420, 28), $"To:      {ride.DropoffLabel}", normal);
            y += 30;
            GUI.Label(new Rect(rect.x + 20, y, 420, 28), $"Fare:   ${ride.Fare}", normal);
            y += 42;

            GUI.color = new Color(1f, 0.55f, 0.45f);
            foreach (var flag in p.RedFlags)
            {
                GUI.Label(new Rect(rect.x + 20, y, 420, 50), "• " + flag, small);
                y += 44;
            }

            GUI.color = Color.white;

            var bar = new Rect(rect.x + 20, rect.yMax - 76, 420, 8);
            GUI.color = new Color(1, 1, 1, 0.2f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = new Color(0.4f, 0.8f, 1f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * director.RequestTimeLeft01, bar.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(rect.x + 20, rect.yMax - 58, 420, 40), "[E] Accept      [Q] Decline", normal);
        }

        void DrawCaption(float w, float h)
        {
            var alpha = Mathf.Clamp01(captionTime / 0.5f);
            GUI.color = new Color(1, 1, 1, alpha);
            Box(new Rect(w / 2 - 480, h - 150, 960, 56), 0.5f * alpha);
            GUI.Label(new Rect(w / 2 - 480, h - 146, 960, 48), caption, center);
            GUI.color = Color.white;
        }

        void DrawOverlays(float w, float h)
        {
            if (director.Fade > 0f)
            {
                GUI.color = new Color(0, 0, 0, director.Fade);
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            }

            if (director.Flash > 0f)
            {
                GUI.color = new Color(1, 1, 1, director.Flash * 0.9f);
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        void DrawResult(float w, float h)
        {
            GUI.Label(new Rect(0, h / 2 - 190, w, 70), director.ResultTitle, title);
            GUI.Label(new Rect(0, h / 2 - 100, w, 120), director.ResultBody, center);

            var line = $"Fare ${director.FarePaid}      Rating {director.Rating.Value:0.00} ★  ({director.RatingChange:+0.00;-0.00;0.00})";
            GUI.Label(new Rect(0, h / 2 + 50, w, 40), line, center);
            GUI.Label(new Rect(0, h / 2 + 120, w, 40), "Press R to take another fare", small.Centered());
        }

        static void Box(Rect rect, float alpha)
        {
            var prev = GUI.color;
            GUI.color = new Color(0.02f, 0.03f, 0.05f, alpha);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        void EnsureStyles()
        {
            if (normal != null) return;

            GUIStyle Make(int size, TextAnchor anchor, FontStyle fontStyle = FontStyle.Normal)
            {
                var s = new GUIStyle(GUI.skin.label)
                {
                    fontSize = size,
                    alignment = anchor,
                    fontStyle = fontStyle,
                    wordWrap = true
                };
                s.normal.textColor = new Color(0.9f, 0.93f, 0.95f);
                return s;
            }

            small = Make(18, TextAnchor.UpperLeft);
            normal = Make(24, TextAnchor.UpperLeft);
            big = Make(32, TextAnchor.UpperLeft, FontStyle.Bold);
            title = Make(60, TextAnchor.MiddleCenter, FontStyle.Bold);
            center = Make(26, TextAnchor.MiddleCenter);
        }
    }

    static class GuiStyleExtensions
    {
        public static GUIStyle Centered(this GUIStyle style)
        {
            var copy = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter };
            return copy;
        }
    }
}
