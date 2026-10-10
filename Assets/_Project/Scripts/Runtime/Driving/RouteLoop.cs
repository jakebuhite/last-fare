using UnityEngine;

namespace LastFare.Driving
{
    /// <summary>
    /// Scene handle for the road loop. Holds the shape parameters and lazily builds the
    /// <see cref="RoutePath"/> that the car, hazards and scene builder all measure distances along.
    /// </summary>
    public class RouteLoop : MonoBehaviour
    {
        [SerializeField] float radius = 200f;
        [SerializeField] int samples = 240;
        [SerializeField] float wobble = 0.18f;

        RoutePath path;

        public RoutePath Path => path ??= RoutePath.CreateLoop(radius, samples, wobble);

        void OnValidate() => path = null;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f);
            for (var i = 0; i < Path.PointCount; i++)
                Gizmos.DrawLine(Path.GetPoint(i) + Vector3.up * 0.2f, Path.GetPoint(i + 1) + Vector3.up * 0.2f);
        }
    }
}
