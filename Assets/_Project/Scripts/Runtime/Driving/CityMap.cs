using UnityEngine;

namespace LastFare.Driving
{
    /// <summary>
    /// Scene handle for the open map's street grid. The GameObject's position is the south-west
    /// intersection. Holds the grid layout and lazily builds the <see cref="RoadNetwork"/> the GPS routes on.
    /// </summary>
    public class CityMap : MonoBehaviour
    {
        [SerializeField] int columns = 7;
        [SerializeField] int rows = 7;
        [SerializeField] float blockSize = 60f;
        [SerializeField] float roadWidth = 10f;

        RoadNetwork network;

        public int Columns => columns;
        public int Rows => rows;
        public float BlockSize => blockSize;
        public float RoadWidth => roadWidth;
        public RoadNetwork Network => network ??= RoadNetwork.Grid(columns, rows, blockSize, transform.position);

        /// <summary>World position of intersection (x, z), counted from the south-west corner.</summary>
        public Vector3 Intersection(float x, float z) => transform.position + new Vector3(x * blockSize, 0f, z * blockSize);

        /// <summary>Centre of the block whose south-west corner is intersection (x, z).</summary>
        public Vector3 BlockCenter(int x, int z) => Intersection(x + 0.5f, z + 0.5f);

        void OnValidate() => network = null;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f);
            for (var e = 0; e < Network.EdgeCount; e++)
            {
                var (a, b) = Network.Edge(e);
                Gizmos.DrawLine(Network.Node(a) + Vector3.up * 0.2f, Network.Node(b) + Vector3.up * 0.2f);
            }
        }
    }
}
